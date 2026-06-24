# Changelog

## 3.1.1 - 2026-06-24

- Fixed GitHub Actions MAUI restore commands to pass target frameworks through
  MSBuild properties instead of invalid `dotnet restore -f` usage.
- Updated CI token permissions and release docs for tag/release management and
  workflow-run cleanup.
- Added explicit `Microsoft.Maui.Controls` package reference required by modern
  MAUI projects.
- Raised Mac Catalyst minimum supported OS platform version to `15.0` and
  documented `MD_APPLE_SDK_ROOT` for side-by-side Xcode builds.
- Added Mac Catalyst MAUI platform entrypoint and switched app startup to
  `CreateWindow`.
- Confirmed local Mac Catalyst debug build succeeds with side-by-side Xcode
  26.4.1 when both `MD_APPLE_SDK_ROOT` and `DEVELOPER_DIR` are set.
- Added public unsigned release workflow for Windows and Mac Catalyst artifacts
  published to `pcvantol/djconnect-app-releases`.
- Added English and Dutch What's New release-note publication for Windows and
  Mac Catalyst through `djconnect.dev`.

## 3.1.0 - 2026-06-24

- Scaffolded DJConnect desktop app as a .NET MAUI project for Windows and
  macOS.
- Added Home Assistant pairing, status, Ask DJ and command API structure.
- Added platform credential storage for Windows Credential Manager and macOS
  Keychain.
- Added documentation set mirroring the Apple client repo structure.
- Added package-free automatic protocol/core tests runnable through
  `./run_tests.sh`.
- Added GitHub Actions CI for protocol tests and Windows/macOS MAUI builds.

## Unreleased

- Updated repository documentation to reflect the `3.1.1` release state,
  public unsigned release flow and EN/NL What's New publication.
- Documented standard release hygiene: docs, tests, GitHub CI validation and
  old release/tag/workflow cleanup.
