# Development Environment

## Repository

```text
/Users/pcvantol/Documents/GitHub/djconnect-windows
```

## Tooling

- .NET SDK from `global.json`.
- .NET MAUI workloads via `dotnet workload restore`.
- Xcode command line tools for Mac Catalyst builds on macOS.
- Windows SDK/Desktop tooling for Windows builds on Windows.

## Useful Commands

```sh
dotnet workload list
dotnet workload restore
dotnet build -f net10.0-maccatalyst
dotnet format DJConnect.Windows.sln --verify-no-changes --no-restore
git status --short --branch
```

Network access may be needed for first-time workload restore.
