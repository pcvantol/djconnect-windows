# Technical Design Decisions

## Scope

This repository contains the MIT-licensed DJConnect desktop app scaffold for
Windows and macOS. The app targets the DJConnect `3.1.x` Home Assistant
protocol line and remains a thin client for Home Assistant-owned playback,
Ask DJ and memory state.

Current app release: `3.1.1`.

## Project Shape

- `src/DJConnect.Windows/DJConnect.Windows.csproj`: .NET MAUI single-project
  app.
- `MainPage.xaml`: native tabbed UI for pairing, Ask DJ, Now Playing and About.
- `Platforms/MacCatalyst/Program.cs` and `AppDelegate.cs`: Mac Catalyst MAUI
  entrypoint.
- `ViewModels/MainViewModel.cs`: runtime state and user actions.
- `Services/DJConnectApiClient.cs`: typed HTTP client for Home Assistant.
- `Services/CredentialStore.cs`: Windows Credential Manager and macOS Keychain.
- `Models/ApiModels.cs`: Home Assistant request/response records.
- `Contracts/DJConnectContract.cs`: protocol constants and legal notice.
- `clear_old_releases.sh`: dry-run-first GitHub release/tag/workflow cleanup
  helper.
- `tests/DJConnect.Tests`: package-free console test harness for protocol/core
  behavior that can run without MAUI workloads.
- `.github/workflows/ci.yml`: GitHub Actions CI for protocol tests and
  platform MAUI builds.

## UI Pattern

The first scaffold uses a single MAUI `TabbedPage`. This keeps the workflow
visible while the API layer stabilizes. As features grow, split larger tabs into
dedicated views and keep Home Assistant calls in services/view models, not in
XAML code-behind.

## Serialization

Use `System.Text.Json` and explicit `JsonPropertyName` attributes for
snake_case backend fields. Avoid dynamic JSON unless the backend contract is
intentionally open-ended.

## Credential Storage

Windows uses Credential Manager through `advapi32` P/Invoke. macOS uses the
platform `/usr/bin/security` tool to store a generic password in Keychain.
Settings JSON must remain non-secret.

## Dependencies

Current app-level dependencies are limited to the .NET SDK, .NET MAUI,
`Microsoft.Maui.Controls` and platform APIs. The Mac Catalyst target uses
minimum supported OS platform version `15.0`, matching the .NET 10
MacCatalyst workload requirement. There are no third-party NuGet packages in
the scaffold.

## Test Strategy

The initial automatic tests are a plain .NET console project instead of xUnit,
NUnit or MSTest. This avoids adding NuGet dependencies and lets protocol/core
checks run on machines where MAUI workloads are not installed. The tests link
the contract and model source files directly and cover:

- client identity and `windows` device-id convention;
- pairing payload compatibility fields;
- Ask DJ message serialization;
- history revision, trim metadata and recent item deserialization;
- confirmation playback action deserialization.
