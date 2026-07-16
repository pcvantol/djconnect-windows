$ErrorActionPreference = 'Stop'

$dotnetDirectory = Join-Path $env:ProgramFiles 'dotnet'
$dotnet = Join-Path $dotnetDirectory 'dotnet.exe'
if (-not (Test-Path $dotnet)) {
    throw "Machine .NET installation is absent at $dotnet. Run the runner tooling maintenance task."
}

$sdks = @(& $dotnet --list-sdks | Where-Object { $_ -match '^10\.0\.\d{3} ' })
if ($sdks.Count -eq 0) {
    throw 'Machine .NET 10 SDK is absent. Run the runner tooling maintenance task.'
}

$resolved = (& $dotnet --version).Trim()
if ($resolved -notmatch '^10\.0\.\d{3}$') {
    throw "global.json did not resolve a .NET 10 SDK: $resolved"
}

Add-Content -Path $env:GITHUB_PATH -Value $dotnetDirectory
Write-Host "Machine .NET SDK resolved: $resolved"
