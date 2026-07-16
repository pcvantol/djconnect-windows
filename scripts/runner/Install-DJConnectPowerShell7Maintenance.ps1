[CmdletBinding()]
param(
    [switch] $RunNow
)

$ErrorActionPreference = 'Stop'

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this installer from an elevated Windows PowerShell session.'
}

$maintenanceDirectory = Join-Path $env:ProgramData 'DJConnect\runner-maintenance'
$maintenanceScript = Join-Path $maintenanceDirectory 'Update-DJConnectRunnerTooling.ps1'
$logFile = Join-Path $maintenanceDirectory 'runner-tooling-maintenance.log'
$taskName = 'Update-RunnerTooling'
$taskPath = '\DJConnect\'

New-Item -ItemType Directory -Force -Path $maintenanceDirectory | Out-Null

$maintenanceContents = @'
$ErrorActionPreference = 'Stop'

$maintenanceDirectory = Join-Path $env:ProgramData 'DJConnect\runner-maintenance'
$logFile = Join-Path $maintenanceDirectory 'runner-tooling-maintenance.log'

function Write-MaintenanceLog([string] $message) {
    $timestamp = (Get-Date).ToUniversalTime().ToString('o')
    "$timestamp $message" | Add-Content -Path $logFile -Encoding utf8
}

try {
    $winget = Get-Command winget.exe -ErrorAction SilentlyContinue
    if ($null -eq $winget) {
        throw 'winget.exe is not available to the machine service account.'
    }

    function Install-OrUpgrade([string] $packageId, [bool] $isInstalled, [string] $description) {
        $operation = if ($isInstalled) { 'upgrade' } else { 'install' }
        Write-MaintenanceLog "Starting $description $operation through winget."
        & $winget.Source $operation --id $packageId --exact --source winget --scope machine --silent --disable-interactivity --accept-package-agreements --accept-source-agreements
        if ($LASTEXITCODE -ne 0) {
            throw "winget $operation for $packageId failed with exit code $LASTEXITCODE."
        }
    }

    $pwshPath = Join-Path $env:ProgramFiles 'PowerShell\7\pwsh.exe'
    Install-OrUpgrade 'Microsoft.PowerShell' (Test-Path $pwshPath) 'PowerShell 7'
    if (-not (Test-Path $pwshPath)) {
        throw "PowerShell 7 was not found at $pwshPath after maintenance."
    }
    $version = (& $pwshPath -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString()').Trim()
    Write-MaintenanceLog "PowerShell 7 machine installation verified at version $version."

    $dotnetPath = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
    $dotnet10Sdks = if (Test-Path $dotnetPath) {
        @(& $dotnetPath --list-sdks | Where-Object { $_ -match '^10\.0\.\d{3} ' })
    } else {
        @()
    }
    $dotnet10Installed = $dotnet10Sdks.Count -gt 0
    Install-OrUpgrade 'Microsoft.DotNet.SDK.10' $dotnet10Installed '.NET 10 SDK'
    if (-not (Test-Path $dotnetPath)) {
        throw ".NET SDK was not found at $dotnetPath after maintenance."
    }
    $dotnet10Sdks = @(& $dotnetPath --list-sdks | Where-Object { $_ -match '^10\.0\.\d{3} ' })
    if ($dotnet10Sdks.Count -eq 0) {
        throw '.NET 10 SDK is absent after maintenance.'
    }
    Write-MaintenanceLog "Machine .NET 10 SDKs verified: $($dotnet10Sdks -join '; ')."
} catch {
    Write-MaintenanceLog "FAILED: $($_.Exception.Message)"
    throw
}
'@

$maintenanceContents | Set-Content -Path $maintenanceScript -Encoding utf8

$action = New-ScheduledTaskAction -Execute "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" -Argument "-NoLogo -NoProfile -ExecutionPolicy Bypass -File `"$maintenanceScript`""
$trigger = New-ScheduledTaskTrigger -Daily -At 03:30
$principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Minutes 20) -MultipleInstances IgnoreNew

Register-ScheduledTask -TaskName $taskName -TaskPath $taskPath -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description 'DJConnect runner: keep machine-level PowerShell 7 and .NET 10 SDK current through winget.' -Force | Out-Null

if ($RunNow) {
    $startedAt = Get-Date
    Start-ScheduledTask -TaskName $taskName -TaskPath $taskPath
    $deadline = (Get-Date).AddMinutes(20)
    do {
        Start-Sleep -Seconds 2
        $task = Get-ScheduledTask -TaskName $taskName -TaskPath $taskPath
        $taskInfo = Get-ScheduledTaskInfo -TaskName $taskName -TaskPath $taskPath
    } while (($task.State -eq 'Running' -or $taskInfo.LastRunTime -lt $startedAt) -and (Get-Date) -lt $deadline)

    if ($task.State -eq 'Running' -or $taskInfo.LastRunTime -lt $startedAt) {
        throw 'PowerShell 7 maintenance task did not finish within 20 minutes.'
    }

    if ($taskInfo.LastTaskResult -ne 0) {
        throw "PowerShell 7 maintenance task failed with result $($taskInfo.LastTaskResult). Review $logFile."
    }
}

Get-ScheduledTask -TaskName $taskName -TaskPath $taskPath |
    Select-Object TaskName, TaskPath, State
Write-Host "Maintenance log: $logFile"
