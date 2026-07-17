[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $InstallRoot,
    [string] $InteractiveUser = ([Security.Principal.WindowsIdentity]::GetCurrent().Name),
    [switch] $RunNow
)

$ErrorActionPreference = 'Stop'
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if ($identity.User.Value -eq 'S-1-5-18' -or -not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this installer from an elevated PowerShell session as the intended interactive Windows user.'
}
if ([string]::IsNullOrWhiteSpace($InteractiveUser) -or $InteractiveUser -match '^(NT AUTHORITY|NT SERVICE)\\') {
    throw 'InteractiveUser must be a named, signed-in Windows user; service identities are not permitted.'
}
if (-not [IO.Path]::IsPathRooted($InstallRoot)) {
    throw 'InstallRoot must be an absolute path.'
}

$runnerService = @(Get-CimInstance Win32_Service | Where-Object { $_.Name -like 'actions.runner.*' })
if ($runnerService.Count -ne 1 -or [string]::IsNullOrWhiteSpace($runnerService[0].StartName) -or $runnerService[0].StartName -notmatch '^NT SERVICE\\') {
    throw 'Expected exactly one hardened GitHub Actions virtual-service runner identity.'
}
$runnerIdentity = $runnerService[0].StartName
$relayRoot = Join-Path $env:ProgramData 'DJConnect\interactive-gui-smoke'
$requestsDirectory = Join-Path $relayRoot 'requests'
$resultsDirectory = Join-Path $relayRoot 'results'
$relayScript = Join-Path $relayRoot 'Invoke-DJConnectInteractiveGuiSmokeRelay.ps1'
$configPath = Join-Path $relayRoot 'relay-config.json'
$launcherPath = Join-Path $relayRoot 'RunInteractiveGuiSmoke.cmd'
$sourceScript = Join-Path $PSScriptRoot 'Invoke-DJConnectInteractiveGuiSmokeRelay.ps1'
if (-not (Test-Path -LiteralPath $sourceScript)) {
    throw "Relay source script is absent: $sourceScript"
}

function Invoke-RelayAdminRecovery([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return }

    # Earlier relay installations used inherit-only administrator ACEs on files.
    # An elevated installer must be able to repair those ACLs before it updates
    # its own managed files; the final ACLs below remain least-privilege.
    & takeown.exe /F $Path /R /D Y | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Failed to take ownership of managed relay path $Path" }
    & icacls.exe $Path /grant:r 'BUILTIN\Administrators:(OI)(CI)F' /T /C | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Failed to restore administrator access to managed relay path $Path" }
}

Invoke-RelayAdminRecovery $relayRoot
New-Item -ItemType Directory -Force -Path $relayRoot, $requestsDirectory, $resultsDirectory | Out-Null
Copy-Item -LiteralPath $sourceScript -Destination $relayScript -Force
[ordered]@{
    schema_version = 1
    install_root = [IO.Path]::GetFullPath($InstallRoot)
    requests_directory = $requestsDirectory
    results_directory = $resultsDirectory
} | ConvertTo-Json | Set-Content -LiteralPath $configPath -Encoding utf8
@"
@echo off
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Invoke-DJConnectInteractiveGuiSmokeRelay.ps1" -ConfigPath "%~dp0relay-config.json"
"@ | Set-Content -LiteralPath $launcherPath -Encoding ascii

function Set-RelayDirectoryAcl([string] $Path, [string[]] $Grants) {
    & icacls.exe $Path /inheritance:r /grant:r 'SYSTEM:(OI)(CI)F' 'BUILTIN\Administrators:(OI)(CI)F' @Grants | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Failed to apply least-privilege ACL to $Path" }
}
function Set-RelayFileAcl([string] $Path, [string[]] $Grants) {
    & icacls.exe $Path /inheritance:r /grant:r 'SYSTEM:F' 'BUILTIN\Administrators:F' @Grants | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Failed to apply least-privilege ACL to $Path" }
}
Set-RelayDirectoryAcl $relayRoot @("${InteractiveUser}:(OI)(CI)RX", "${runnerIdentity}:(OI)(CI)RX")
Set-RelayDirectoryAcl $requestsDirectory @("${InteractiveUser}:(OI)(CI)RX", "${runnerIdentity}:(OI)(CI)M")
Set-RelayDirectoryAcl $resultsDirectory @("${InteractiveUser}:(OI)(CI)M", "${runnerIdentity}:(OI)(CI)RX")
Set-RelayFileAcl $relayScript @("${InteractiveUser}:RX", "${runnerIdentity}:R")
Set-RelayFileAcl $configPath @("${InteractiveUser}:R", "${runnerIdentity}:R")
Set-RelayFileAcl $launcherPath @("${InteractiveUser}:RX", "${runnerIdentity}:R")

$taskName = 'InteractiveGuiSmoke'
$taskPath = '\DJConnect\'
$taskCommand = "$env:ComSpec /d /c `"$launcherPath`""
& schtasks.exe /Create /TN "$taskPath$taskName" /TR $taskCommand /SC MINUTE /MO 1 /RU $InteractiveUser /IT /RL LIMITED /F | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Failed to register the interactive GUI smoke scheduled task.' }

$heartbeatPath = Join-Path $resultsDirectory 'relay-heartbeat.json'
Remove-Item -LiteralPath $heartbeatPath -Force -ErrorAction SilentlyContinue
Start-ScheduledTask -TaskName $taskName -TaskPath $taskPath
$heartbeatDeadline = (Get-Date).AddSeconds(30)
while (-not (Test-Path -LiteralPath $heartbeatPath) -and (Get-Date) -lt $heartbeatDeadline) {
    Start-Sleep -Milliseconds 500
}
if (-not (Test-Path -LiteralPath $heartbeatPath)) {
    throw 'Interactive GUI smoke relay task did not produce a heartbeat within 30 seconds. Keep the configured smoke user signed in and inspect the Task Scheduler operational log.'
}
$heartbeat = Get-Content -Raw -LiteralPath $heartbeatPath | ConvertFrom-Json
if ($heartbeat.schema_version -ne 1 -or $heartbeat.relay_session_kind -ne 'interactive_user_task' -or [int]$heartbeat.process_session_id -eq 0) {
    throw 'Interactive GUI smoke relay heartbeat did not prove an interactive user session.'
}
if ($RunNow) {
    Write-Host 'Interactive GUI smoke relay heartbeat verified immediately after installation.'
}

Write-Host "Interactive GUI smoke relay installed for $InteractiveUser."
Write-Host "Task: $taskPath$taskName (limited, interactive-token, every minute)"
Write-Host "Service identity: $runnerIdentity (request write/result read only)"
