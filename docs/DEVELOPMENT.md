# Development

## Requirements

- .NET SDK matching `global.json`.
- .NET MAUI workloads for Windows and/or Mac Catalyst.
- Windows 10 19041 or newer for Windows builds.
- macOS with Xcode command line tools for Mac Catalyst builds.

Install workloads:

```sh
dotnet workload restore
```

## Build

Windows:

```sh
dotnet build -f net10.0-windows10.0.19041.0
```

macOS:

```sh
dotnet build -f net10.0-maccatalyst
```

All target frameworks:

```sh
dotnet build DJConnect.Windows.sln
```

## Local Pairing

1. Start the app.
2. Enter the Home Assistant base URL, for example
   `http://homeassistant.local:8123`.
3. Enter a Home Assistant pairing code and choose `Koppelen`, or paste an
   existing DJConnect bearer token during development.
4. Keep the app open while validating status, Ask DJ and command flows.

The app sends the same pairing code as `pairing_token`, `pair_code` and
`pairing_code` for compatibility with current Home Assistant integration
builds.

## Logging And Secrets

Do not add logs that include bearer tokens, Authorization headers, pairing
codes, Home Assistant long-lived tokens, Spotify credentials, OAuth refresh
tokens or raw secret-bearing response bodies.

Visible UI errors should stay short. Technical details belong in redacted
diagnostics once a diagnostics surface is added.

## Checks

Automatic protocol/core tests do not require MAUI workloads:

```sh
./run_tests.sh
```

Full scaffold checks:

```sh
./run_tests.sh
dotnet format DJConnect.Windows.sln --verify-no-changes --no-restore
dotnet build DJConnect.Windows.sln --no-restore
```

On a machine without MAUI workloads, build stops with `NETSDK1147` and instructs
you to run `dotnet workload restore`.

## Continuous Integration

GitHub Actions workflow:

```text
.github/workflows/ci.yml
```

Jobs:

- `protocol-tests`: runs `./run_tests.sh` and formatting for the test project
  on Ubuntu without MAUI workloads.
- `maui-macos-build`: restores MAUI workloads and builds `net10.0-maccatalyst`
  on macOS.
- `maui-windows-build`: restores MAUI workloads and builds
  `net10.0-windows10.0.19041.0` on Windows.
