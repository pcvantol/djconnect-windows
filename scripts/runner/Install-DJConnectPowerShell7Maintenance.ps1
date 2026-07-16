[CmdletBinding()]
param(
    [switch] $RunNow
)

$ErrorActionPreference = 'Stop'

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$windowsPrincipal = [Security.Principal.WindowsPrincipal]::new($identity)
if ($identity.User.Value -eq 'S-1-5-18') {
    throw 'Run this installer as the logged-in Windows administrator, not as SYSTEM.'
}
if (-not $windowsPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this installer from an elevated Windows PowerShell session.'
}
$maintenanceUser = $identity.Name

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
    if ([Security.Principal.WindowsIdentity]::GetCurrent().User.Value -eq 'S-1-5-18') {
        throw 'DJConnect runner tooling maintenance must not run as SYSTEM.'
    }

    $windowsApps = Join-Path $env:LOCALAPPDATA 'Microsoft\WindowsApps'
    if ((Test-Path $windowsApps) -and (($env:Path -split ';') -notcontains $windowsApps)) {
        $env:Path = "$windowsApps;$env:Path"
    }
    $winget = Get-Command winget.exe -ErrorAction SilentlyContinue
    if ($null -eq $winget) {
        throw 'winget.exe is unavailable for the interactive maintenance user. Sign in to Windows once, complete App Installer registration, then rerun the installer.'
    }

    Write-MaintenanceLog "Running as interactive administrator $([Security.Principal.WindowsIdentity]::GetCurrent().Name)."

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

    # The native Windows build depends on the installed .NET workload set as
    # well as the SDK. Keeping it current here avoids a workflow having to
    # download or repair platform workloads for every run.
    Write-MaintenanceLog 'Updating installed .NET workloads for the machine SDK.'
    & $dotnetPath workload update --no-cache
    if ($LASTEXITCODE -ne 0) {
        throw ".NET workload update failed with exit code $LASTEXITCODE."
    }
    $workloads = @(& $dotnetPath workload list | Where-Object { $_ -and $_ -notmatch '^-+$' -and $_ -notmatch '^Workload version:' -and $_ -notmatch '^Installed Workload' })
    Write-MaintenanceLog "Installed .NET workloads after maintenance: $($workloads -join '; ')."
} catch {
    Write-MaintenanceLog "FAILED: $($_.Exception.Message)"
    throw
}
'@

$maintenanceContents | Set-Content -Path $maintenanceScript -Encoding utf8

$action = New-ScheduledTaskAction -Execute "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" -Argument "-NoLogo -NoProfile -ExecutionPolicy Bypass -File `"$maintenanceScript`""
$trigger = New-ScheduledTaskTrigger -Daily -At 10:00
$taskPrincipal = New-ScheduledTaskPrincipal -UserId $maintenanceUser -LogonType Interactive -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Minutes 45) -MultipleInstances IgnoreNew

Register-ScheduledTask -TaskName $taskName -TaskPath $taskPath -Action $action -Trigger $trigger -Principal $taskPrincipal -Settings $settings -Description 'DJConnect runner: keep PowerShell 7, .NET 10 SDK and installed .NET workloads current through winget as the logged-in administrator.' -Force | Out-Null

if ($RunNow) {
    $startedAt = Get-Date
    Start-ScheduledTask -TaskName $taskName -TaskPath $taskPath
    $deadline = (Get-Date).AddMinutes(45)
    do {
        Start-Sleep -Seconds 2
        $task = Get-ScheduledTask -TaskName $taskName -TaskPath $taskPath
        $taskInfo = Get-ScheduledTaskInfo -TaskName $taskName -TaskPath $taskPath
    } while (($task.State -eq 'Running' -or $taskInfo.LastRunTime -lt $startedAt) -and (Get-Date) -lt $deadline)

    if ($task.State -eq 'Running' -or $taskInfo.LastRunTime -lt $startedAt) {
        throw 'Runner tooling maintenance task did not finish within 45 minutes.'
    }

    if ($taskInfo.LastTaskResult -ne 0) {
        throw "PowerShell 7 maintenance task failed with result $($taskInfo.LastTaskResult). Review $logFile."
    }
}

Get-ScheduledTask -TaskName $taskName -TaskPath $taskPath |
    Select-Object TaskName, TaskPath, State
Write-Host "Maintenance account: $maintenanceUser (interactive administrator; must be logged in at run time)."
Write-Host "Maintenance log: $logFile"
