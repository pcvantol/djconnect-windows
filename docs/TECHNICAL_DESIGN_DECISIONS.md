# Technical Design Decisions

## Scope

This repository contains the MIT-licensed DJConnect desktop app scaffold for
Windows and macOS. The app targets the DJConnect `3.2.x` Home Assistant
protocol line and remains a thin client for Home Assistant-owned playback,
Ask DJ and Music DNA state.

Current app release: `3.2.6`.

## Project Shape

- `src/DJConnect.Windows/DJConnect.Windows.csproj`: .NET MAUI single-project
  app.
- `MainPage.xaml`: native desktop UI with a sidebar flow matching the macOS
  client's NavigationSplitView structure.
- `Platforms/MacCatalyst/Program.cs` and `AppDelegate.cs`: Mac Catalyst MAUI
  entrypoint.
- `ViewModels/MainViewModel.cs`: runtime state and user actions.
- `Services/DJConnectApiClient.cs`: typed HTTP client for Home Assistant.
- `Services/HomeAssistantTransportManager.cs`: local/remote HA URL selection
  after local pairing.
- `Services/ApiErrorLocalizer.cs`: centralized user-facing mapping for
  backend/API error codes.
- `Resources/Strings*.resx`: standard .NET resources for English, Dutch,
  German, French and Spanish.
- `Services/DiagnosticRedactor.cs`: shared redaction for logs, feedback,
  crash reports, diagnostics and clipboard exports.
- `Services/CredentialStore.cs`: Windows Credential Manager and macOS Keychain.
- `Models/ApiModels.cs`: Home Assistant request/response records.
- `Contracts/DJConnectContract.cs`: protocol constants and legal notice.
- `clear_old_releases.sh`: dry-run-first GitHub release/tag/workflow cleanup
  helper.
- `tests/DJConnect.Tests`: package-free console test harness for protocol/core
  behavior that can run without MAUI workloads.
- `.github/workflows/ci.yml`: GitHub Actions CI for protocol tests, Windows
  runner tests/format checks, workflow YAML hygiene, secret-term scanning,
  platform MAUI builds and unsigned Windows artifact sanity checks without
  signing secrets.
- `.github/workflows/codeql.yml`: CodeQL security analysis for C#.
- `.github/workflows/semgrep.yml`: advisory Semgrep scan through the shared
  DJConnect Semgrep workflow.

## UI Pattern

The desktop client uses a MAUI `ContentPage` with a persistent sidebar and
detail panels. This mirrors the macOS client flow: Now Playing first, Ask DJ as
the second primary workflow, Queue as a visible navigation target, and
Settings/About as separate desktop sections. Home Assistant calls stay in
services/view models; XAML code-behind only switches visible panels and sidebar
selection state.

Now Playing, Ask DJ, Queue and Playlists stay thin. Playback state, queue
state, playlist state, message history and returned actions are backend-owned
by Home Assistant DJConnect. The Windows client normalizes render models,
applies feature gating and sends generic commands only; it does not hardcode
Spotify-specific queue, playlist or intent families.

First launch is gated by the interactive welcome wizard. `DJConnectWelcomeSeen`
is stored locally once the user skips or finishes it. Dismissing onboarding
opens local Home Assistant pairing. Windows no longer starts a client-hosted
local API or `_djconnect._tcp` advertisement; the user enters the local Home
Assistant URL and the pairing code shown by the HA integration. The client does
not prefill, generate or copy pairing codes and does not show a Windows client
address or inbound waiting state.

Settings, Privacy, Logs, Feedback and Crash report are utility flows rather
than backend upload surfaces. They show privacy-safe context, use explicit user
actions and run all generated diagnostics through `DiagnosticRedactor` before
preview, copy, storage or issue URL creation.

Wakeword UI state is intentionally separate from wakeword engine availability.
The settings and prompt state can persist dismissals/preferences, but
`WakewordFeatureAvailable` remains false until the app has a real foreground
listener that can satisfy privacy, lifecycle and platform-permission
requirements.

Demo Mode is session-only and forced off during startup. It is a local product
tour/runtime substitute, not a persisted connection mode: no Home Assistant
calls and no token writes occur while demo data is active.

Monkey-test mode is an explicit CI/UI-stress extension of Demo Mode. It is
enabled through `DJCONNECT_DEMO_MONKEY_TEST=1` or legacy monkey/UI-test
environment variables. When enabled, the ViewModel starts directly in Demo Mode
and routes settings writes through a no-op save helper. Destructive or external
actions such as pairing reset, token writes, clipboard copy, browser launch,
permission settings, log/history clear and demo exit are suppressed.

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
the contract, model and privacy helper source files directly and cover:

- client identity and `windows` device-id convention;
- pairing payload compatibility fields;
- Ask DJ message serialization and backend exchange ordering;
- Ask DJ track insight v2 and v1 fallback presentation without prose
  parsing or stale media/action reuse;
- history revision, trim metadata and recent item deserialization;
- Ask DJ mood/app metadata, response `links[]` alongside `sources[]`, safe
  backend error objects, stale backend actions and unsupported backend
  capabilities;
- confirmation playback action deserialization;
- generic command payloads without removed Spotify override fields;
- shared redaction for tokens, Authorization headers, pairing codes, bootstrap
  proofs, HA tokens, push tokens, secrets and private URLs;
- protocol compatibility checks;
- localization completeness, supported locale normalization and API-error
  guidance without localizing protocol values;
- queue and playlist normalization limits/dedupe;
- onboarding, What's New, crash, wakeword, demo, monkey-test env detection,
  diagnostic preference,
  permission flag and absence of local API/mDNS runtime code paths.
