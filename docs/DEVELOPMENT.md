# Development

## Requirements

- .NET SDK matching `global.json`.
- .NET MAUI workloads for Windows and/or Mac Catalyst.
- Windows 10 19041 or newer for Windows builds.
- macOS with Xcode command line tools for Mac Catalyst builds. With the current
  .NET 10 Mac Catalyst workload, Xcode 26.4.x may be required even when a newer
  Xcode remains the system default for Apple app workflows.

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

When the installed .NET Mac Catalyst pack requires Xcode 26.4 but the system
default is newer, use the side-by-side Xcode app explicitly:

```sh
MD_APPLE_SDK_ROOT=/Applications/Xcode_26.4.1.app \
DEVELOPER_DIR=/Applications/Xcode_26.4.1.app/Contents/Developer \
dotnet build src/DJConnect.Windows/DJConnect.Windows.csproj -f net10.0-maccatalyst
```

All target frameworks:

```sh
dotnet build DJConnect.Windows.sln
```

## Local Pairing

1. Start the app.
2. Finish or skip onboarding. The app does not advertise mDNS or host a local
   Client API.
3. Open pairing in Home Assistant through the DJConnect integration.
4. Enter the local Home Assistant URL and the pairing code shown by Home
   Assistant in the Windows app. Successful pairing stores only the returned
   DJConnect device token plus HA local/remote URL metadata.
5. Keep the app open while validating status, Ask DJ, Queue, Playlists and
   command flows.

The app sends the same pairing code as `pairing_token`, `pair_code` and
`pairing_code` for compatibility with current Home Assistant integration
builds. Remote Home Assistant URLs are used only after successful local pairing.

## Logging And Secrets

Do not add logs that include bearer tokens, Authorization headers, pairing
codes, Home Assistant long-lived tokens, Spotify credentials, OAuth refresh
tokens or raw secret-bearing response bodies.

Visible UI errors should stay short. Technical details belong in redacted
diagnostics. Logs, feedback bodies, crash reports and clipboard exports must use
`DiagnosticRedactor` before they are displayed, copied, persisted or used in a
GitHub issue URL.

## Demo And Wakeword

Demo Mode is session-only. Startup forces it off, starting it loads local sample
runtime data, and stopping it clears demo state. Demo Mode must not make Home
Assistant calls or write tokens.

For CI or UI stress testing, set:

```sh
DJCONNECT_DEMO_MONKEY_TEST=1
```

This starts directly in Demo Mode and makes random interaction non-destructive:
settings are not persisted, pairing/token writes are rejected, clipboard and
browser actions are no-ops, and clear
or reset actions do not delete local state. Legacy env names
`DJCONNECT_MONKEY_TEST`, `DJCONNECT_UI_TEST`, `MONKEY_TEST` and `UITEST` are also
recognized.

Wakeword state/settings are present for the UX, but the feature remains disabled
until a real foreground listener exists. Push-to-talk and text Ask DJ should
continue to work without wakeword.

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
you to run `dotnet workload restore`. In sandboxed shells, `dotnet format` may
fail while creating a Roslyn build-host named pipe; rerun the same check outside
the sandbox before treating it as a formatting failure.

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

Manual release workflow:

```text
.github/workflows/public-unsigned-release.yml
```

It runs only through manual dispatch, builds unsigned Windows and Mac Catalyst
artifacts, and publishes platform releases to
`pcvantol/djconnect-app-releases` when `PUBLIC_RELEASES_TOKEN` is configured.
The public tags are `windows/vX.Y.Z` and `maccatalyst/vX.Y.Z`. Windows release
artifacts are produced for both `win-x64` and `win-arm64`; the ARM64 zip is the
native Windows-on-ARM build for Parallels Windows VMs on Apple Silicon Macs.
When
`WEBSITE_RELEASE_NOTES_TOKEN` is configured, the workflow also publishes English
and Dutch What's New JSON files to `djconnect.dev` under
`/release-notes/{windows|maccatalyst}/{en|nl}/vX.Y.Z.json`.
