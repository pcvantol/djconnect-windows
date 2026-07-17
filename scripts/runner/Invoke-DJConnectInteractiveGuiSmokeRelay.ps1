[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ConfigPath
)

$ErrorActionPreference = 'Stop'

function Write-RelayResult([hashtable] $Result, [string] $ResultsDirectory) {
    $temporaryPath = Join-Path $ResultsDirectory ("$($Result.request_id).tmp")
    $resultPath = Join-Path $ResultsDirectory ("$($Result.request_id).json")
    $Result | ConvertTo-Json -Depth 5 -Compress | Set-Content -Path $temporaryPath -NoNewline -Encoding utf8
    Move-Item -LiteralPath $temporaryPath -Destination $resultPath -Force
}

if (-not (Test-Path -LiteralPath $ConfigPath)) {
    throw "Interactive GUI smoke relay configuration is absent: $ConfigPath"
}

$config = Get-Content -Raw -LiteralPath $ConfigPath | ConvertFrom-Json
if ($config.schema_version -ne 1 -or [string]::IsNullOrWhiteSpace($config.install_root) -or [string]::IsNullOrWhiteSpace($config.requests_directory) -or [string]::IsNullOrWhiteSpace($config.results_directory)) {
    throw 'Interactive GUI smoke relay configuration is invalid.'
}

$requestsDirectory = [IO.Path]::GetFullPath([string]$config.requests_directory)
$resultsDirectory = [IO.Path]::GetFullPath([string]$config.results_directory)
$installRoot = [IO.Path]::GetFullPath([string]$config.install_root)
if (-not (Test-Path -LiteralPath $requestsDirectory) -or -not (Test-Path -LiteralPath $resultsDirectory)) {
    throw 'Interactive GUI smoke relay directories are unavailable.'
}

$request = Get-ChildItem -LiteralPath $requestsDirectory -Filter '*.json' -File |
    Sort-Object Name |
    Where-Object { -not (Test-Path -LiteralPath (Join-Path $resultsDirectory $_.Name)) } |
    Select-Object -First 1
if ($null -eq $request) {
    exit 0
}

$requestId = [IO.Path]::GetFileNameWithoutExtension($request.Name)
if ($requestId -notmatch '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') {
    exit 0
}

$startedAt = (Get-Date).ToUniversalTime().ToString('o')
$result = [ordered]@{
    schema_version = 1
    request_id = $requestId
    expected_version = $null
    observed_version = $null
    process_session_id = $null
    relay_session_kind = 'interactive_user_task'
    startup_marker_result = 'NOT_STARTED'
    process_exited = $null
    process_exit_code = $null
    final_result = 'FAIL'
    failure_classification = 'INTERACTIVE_RELAY_EXECUTION_ERROR'
    timestamps = @{ started_at = $startedAt; observed_at = $null }
}

try {
    $payload = Get-Content -Raw -LiteralPath $request.FullName | ConvertFrom-Json
    if ($payload.schema_version -ne 1 -or $payload.request_id -ne $requestId -or [string]$payload.expected_version -notmatch '^[0-9]+\.[0-9]+\.[0-9]+$') {
        throw 'Interactive GUI smoke request is invalid.'
    }
    $result.expected_version = [string]$payload.expected_version
    $executable = Join-Path $installRoot 'current\DJConnect.exe'
    if (-not (Test-Path -LiteralPath $executable)) {
        throw 'Installed DJConnect executable is absent.'
    }
    $observedVersion = (Get-Item -LiteralPath $executable).VersionInfo.ProductVersion
    $result.observed_version = $observedVersion
    if ($observedVersion -notlike "$($result.expected_version)*") {
        throw "Installed version mismatch: $observedVersion"
    }

    $process = Start-Process -FilePath $executable -PassThru
    $result.process_session_id = $process.SessionId
    if ($process.SessionId -eq 0) {
        throw 'Interactive GUI relay launched DJConnect in session 0.'
    }
    Start-Sleep -Seconds 10
    $process.Refresh()
    $result.process_exited = $process.HasExited
    if ($process.HasExited) {
        $result.process_exit_code = $process.ExitCode
        $result.startup_marker_result = 'PROCESS_EXITED'
        $result.failure_classification = 'PROCESS_EXITED_DURING_INTERACTIVE_STARTUP'
    } else {
        $result.startup_marker_result = 'PROCESS_ALIVE'
        $result.final_result = 'PASS'
        $result.failure_classification = 'none'
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
} catch {
    $result.failure_classification = 'INTERACTIVE_RELAY_EXECUTION_ERROR'
    $result.error_category = $_.Exception.GetType().Name
} finally {
    $result.timestamps.observed_at = (Get-Date).ToUniversalTime().ToString('o')
    Write-RelayResult -Result $result -ResultsDirectory $resultsDirectory
}
