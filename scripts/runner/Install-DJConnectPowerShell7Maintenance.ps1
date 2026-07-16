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
$maintenanceScript = Join-Path $maintenanceDirectory 'Update-DJConnectPowerShell7.ps1'
$logFile = Join-Path $maintenanceDirectory 'powershell7-maintenance.log'
$taskName = 'Update-PowerShell7'
$taskPath = '\DJConnect\'

New-Item -ItemType Directory -Force -Path $maintenanceDirectory | Out-Null

$maintenanceContents = @'
$ErrorActionPreference = 'Stop'

$maintenanceDirectory = Join-Path $env:ProgramData 'DJConnect\runner-maintenance'
$logFile = Join-Path $maintenanceDirectory 'powershell7-maintenance.log'

function Write-MaintenanceLog([string] $message) {
    $timestamp = (Get-Date).ToUniversalTime().ToString('o')
    "$timestamp $message" | Add-Content -Path $logFile -Encoding utf8
}

try {
    $winget = Get-Command winget.exe -ErrorAction SilentlyContinue
    if ($null -eq $winget) {
        throw 'winget.exe is not available to the machine service account.'
    }

    $pwshPath = Join-Path $env:ProgramFiles 'PowerShell\7\pwsh.exe'
    $operation = if (Test-Path $pwshPath) { 'upgrade' } else { 'install' }
    Write-MaintenanceLog "Starting PowerShell 7 $operation through winget."

    & $winget.Source $operation --id Microsoft.PowerShell --exact --source winget --scope machine --silent --disable-interactivity --accept-package-agreements --accept-source-agreements
    if ($LASTEXITCODE -ne 0) {
        throw "winget $operation failed with exit code $LASTEXITCODE."
    }

    if (-not (Test-Path $pwshPath)) {
        throw "PowerShell 7 was not found at $pwshPath after winget $operation."
    }

    $version = (& $pwshPath -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString()').Trim()
    Write-MaintenanceLog "PowerShell 7 machine installation verified at version $version."
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

Register-ScheduledTask -TaskName $taskName -TaskPath $taskPath -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description 'DJConnect runner: keep machine-level PowerShell 7 current through winget.' -Force | Out-Null

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
