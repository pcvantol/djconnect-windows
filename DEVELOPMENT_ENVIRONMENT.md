# Development Environment

## Repository

```text
/Users/pcvantol/Documents/GitHub/djconnect-windows
```

## Tooling

- .NET SDK from `global.json`.
- .NET MAUI workloads via `dotnet workload restore`.
- Xcode command line tools for Mac Catalyst builds on macOS.
- Xcode 26.4.x side-by-side with newer Xcode versions when the installed .NET
  Mac Catalyst workload requires Xcode 26.4.
- Windows SDK/Desktop tooling for Windows builds on Windows.

## Useful Commands

```sh
dotnet workload list
dotnet workload restore
dotnet build src/DJConnect.Windows/DJConnect.Windows.csproj -f net10.0-maccatalyst
dotnet build src/DJConnect.Windows/DJConnect.Windows.csproj -f net10.0-windows10.0.19041.0
dotnet format DJConnect.Windows.sln --verify-no-changes --no-restore
git status --short --branch
```

For local Mac Catalyst builds with side-by-side Xcode 26.4.1:

```sh
MD_APPLE_SDK_ROOT=/Applications/Xcode_26.4.1.app \
DEVELOPER_DIR=/Applications/Xcode_26.4.1.app/Contents/Developer \
dotnet build src/DJConnect.Windows/DJConnect.Windows.csproj -f net10.0-maccatalyst
```

Network access may be needed for first-time workload restore.
