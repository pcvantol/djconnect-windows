# DJConnect

DJConnect is a native desktop client for the Home Assistant `djconnect` custom
integration, targeting Windows and macOS from one .NET codebase. It follows the
macOS app's functional shape: first-start onboarding, pairing, Now Playing,
Ask DJ chat with server-side history, Queue, Playlists, Play Now/follow-up
actions, recent listening list rendering, playback controls, status, Settings,
Logs, Privacy, Feedback, Crash report, Demo Mode and compact legal/about
sections.

Home Assistant remains the trusted backend. The desktop app does not store
Spotify credentials, Spotify OAuth tokens, Music DNA or Ask DJ server history as
the source of truth. The only app-owned credential is the DJConnect bearer token
issued by Home Assistant, stored through Windows Credential Manager on Windows
and Keychain on macOS. Diagnostics, feedback and crash-report text are generated
locally, redacted before preview/copy/open-issue actions and never uploaded
automatically.

## Current Version

- Desktop app: `3.2.9`
- Home Assistant protocol line: `3.2.x`
- Current local `client_type`: `windows`

## Cross-Repo Source Of Truth

Cross-repo DJConnect contracts, repo ownership and shared release hygiene live
in `/Users/pcvantol/Documents/GitHub/djconnect/SYNC_PROMPTS.md` in the Home
Assistant integration repo. Do not copy that file into this repository.

Canonical sibling repos:

- Home Assistant integration: `pcvantol/djconnect`
- Central API backend: `pcvantol/djconnect-api`
- Apple app: `pcvantol/djconnect-app`
- Windows desktop app: `pcvantol/djconnect-windows`
- ESP firmware: `pcvantol/djconnect-esp32`
- Website/docs: `pcvantol/djconnect-website`
- Raspberry Pi client: `pcvantol/djconnect-pi`

## Stack

The app is scaffolded as a .NET MAUI desktop app targeting:

```text
net10.0-windows10.0.19041.0
net10.0-maccatalyst
```

.NET itself runs on Windows and macOS, but WPF does not. MAUI is the better fit
for this repo because it keeps one C# UI/codebase while building through the
native Windows and macOS desktop stacks. It also avoids an Electron/web runtime.

## Functional Reference

The macOS app in `/Users/pcvantol/Documents/GitHub/djconnect-app` was used as
the functional reference. The desktop implementation translates workflows and
API contracts rather than copying SwiftUI or Apple-specific code.

## Documentation

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md): trust boundary, targets and
  runtime flow.
- [docs/ARCHITECTURE_DECISIONS.md](docs/ARCHITECTURE_DECISIONS.md): stack and
  design decisions.
- [docs/API_CONTRACT.md](docs/API_CONTRACT.md): Home Assistant endpoint shapes.
- [docs/TRACK_INSIGHT_PLATFORM.md](docs/TRACK_INSIGHT_PLATFORM.md): Windows
  Track Insight product architecture, navigation, shared engine, demo mode and
  visualizer roadmap.
- [docs/NON_FUNCTIONAL_REQUIREMENTS.md](docs/NON_FUNCTIONAL_REQUIREMENTS.md):
  security, privacy, lifecycle, accessibility and performance acceptance
  requirements.
- [docs/TECHNICAL_DESIGN_DECISIONS.md](docs/TECHNICAL_DESIGN_DECISIONS.md):
  code-level patterns and dependency notes.
- [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md): local setup, build and pairing.
- [docs/LOCALIZATION.md](docs/LOCALIZATION.md): supported locales, resource
  validation and API-error localization rules.
- [docs/RELEASE.md](docs/RELEASE.md): release checklist and packaging open
  work, public unsigned release publication and What's New publication.
- [docs/HANDOFF.md](docs/HANDOFF.md), [docs/TODO.md](docs/TODO.md) and
  [docs/ISSUES.md](docs/ISSUES.md): current status and backlog.
- [PRIVACY.md](PRIVACY.md), [SECURITY.md](SECURITY.md),
  [CONTRIBUTING.md](CONTRIBUTING.md) and [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md):
  project policy docs.

Release cleanup helper:

```sh
./clear_old_releases.sh --keep 1 --keep-workflow-runs 2
```

This keeps both the new CI run and the new public unsigned release run
available for audit.

Normal CI and security scans run with read-only repository permissions. Public
unsigned release publication is a separate manual workflow that uses scoped
release secrets only in publish steps; see [docs/RELEASE.md](docs/RELEASE.md).

Public unsigned releases use platform-specific tags in
[pcvantol/djconnect-app-releases](https://github.com/pcvantol/djconnect-app-releases):

- `windows/vX.Y.Z`
- `maccatalyst/vX.Y.Z`

The Windows release contains separate unsigned diagnostic zips for:

- `x64`: regular Intel/AMD Windows PCs.
- `arm64`: Windows on ARM, including Parallels Windows VMs on Apple Silicon
  Macs.

The same release workflow publishes localized What's New JSON files to
`djconnect.dev` under
`/release-notes/{windows|maccatalyst}/{en|nl|de|fr|es}/vX.Y.Z.json`.
These artifacts are for diagnostics and internal validation until signed
Windows packaging and Mac Catalyst notarization are added.

Key screens and flows mirrored from macOS and extended for desktop:

- Pairing/configuration: local Home Assistant app-pairing with pairing code,
  stable client identity, local/remote HA URL storage and pairing reset.
- Ask DJ: `POST /api/djconnect/v1/ask_dj/message`, server-side history sync via
  `GET /api/djconnect/v1/ask_dj/history`, clear via
  `POST /api/djconnect/v1/ask_dj/history/clear`, with client mood values,
  `audio_response` preference, `links[]`/`sources[]` rendering and bounded
  history trim metadata.
- Local fast path: HTTP remains the safe default. A Home Assistant native
  `/api/websocket` fast path for latency-sensitive DJConnect commands, Ask DJ
  messages and Track Insight is available only behind explicit live-test opt-in,
  only for local Home Assistant URLs and only when the client has a valid Home
  Assistant websocket auth token plus DJConnect capabilities confirming
  websocket support. Remote sessions, pairing, status, history, voice, push,
  image proxy and TTS/audio URLs stay on HTTP.
- Playback actions: follow-up confirmations and Play Now actions go through
  `POST /api/djconnect/v1/command`.
- Recent played answers: compact list rendering from returned `items[]`.
- Track Insight: first-class Windows feature with direct Now Playing entry,
  Ask DJ deep-link/hydration, Music DNA Match rendering and a dedicated
  navigation/visualizer roadmap in
  [docs/TRACK_INSIGHT_PLATFORM.md](docs/TRACK_INSIGHT_PLATFORM.md).
- Now Playing/status: Home Assistant status and generic playback commands.
- Queue and Playlists: backend-owned collections normalized locally, capped at
  100 rendered items, deterministic dedupe and generic start commands.
- Backend playback: Spotify Direct and Music Assistant are both consumed
  through Home Assistant response/action shapes. Backend-specific actions carry
  `music_backend_revision`; stale actions and unsupported capabilities are
  shown as refresh/unavailable states instead of Spotify-only UI.
- Settings: connection status, pairing reset, output/playback preferences,
  Ask DJ, Demo Mode, permissions, diagnostics, app info, language selection for
  English, Dutch, German, French and Spanish, and legal/privacy links.
- Logs/diagnostics: bounded local logs, log-level filtering, search, copy and
  wipe actions after shared redaction.
- Feedback and Crash reports: user-controlled preview/copy/open-issue flows
  with opt-in redacted diagnostics and no automatic upload.
- Privacy/legal/about: local data explanation, app metadata, MIT license,
  Spotify notice, project/security links and deletion/reset actions.
- Demo Mode: session-only local demo runtime with sample Now Playing, Ask DJ,
  Queue and Playlists; no Home Assistant calls or token writes.
- Monkey-test mode: `DJCONNECT_DEMO_MONKEY_TEST=1` starts Demo Mode for CI/UI
  stress runs and suppresses persistence, pairing/token writes, clipboard,
  browser and destructive reset/clear actions.
- Wakeword prompt: state and settings are present, but the foreground wakeword
  listener is feature-gated off until a real engine exists.

## Client Identity

The current desktop identity constants live in
`src/DJConnect.Windows/Contracts/DJConnectContract.cs`:

```text
client_type: windows
device_id: djconnect-windows-XXXXXXXXXXXX
```

`windows` is canonical for the shared `3.2.x` app-client contract. Windows is
an inbound-only app client: it exposes no Home Assistant-callable
`/api/device/*`, does not advertise mDNS, pairs locally through
`POST /api/djconnect/v1/pair`, and may use `ha_remote_url` only after
successful local pairing.

## Development

Install MAUI workloads once per machine:

```sh
dotnet workload restore
```

Build on Windows:

```powershell
dotnet build -f net10.0-windows10.0.19041.0
```

Build on macOS:

```sh
dotnet build -f net10.0-maccatalyst
```

If the installed .NET Mac Catalyst pack requires Xcode 26.4 while the default
Xcode is newer, point .NET at the side-by-side Xcode app:

```sh
MD_APPLE_SDK_ROOT=/Applications/Xcode_26.4.1.app \
DEVELOPER_DIR=/Applications/Xcode_26.4.1.app/Contents/Developer \
dotnet build src/DJConnect.Windows/DJConnect.Windows.csproj -f net10.0-maccatalyst
```

Run automatic protocol/core tests:

```sh
./run_tests.sh
```

GitHub Actions runs these tests on every push and pull request, plus MAUI build
jobs for Mac Catalyst and Windows. The Windows CI baseline also runs
protocol/core tests and formatting on a Windows runner, publishes unsigned local
artifacts for shape/checksum validation only, validates workflow YAML, scans
for unexpected secret-like strings, and runs CodeQL for C# plus advisory
Semgrep through the shared DJConnect workflow. CI does not use signing keys,
API tokens, pairing tokens or app secrets.

The manual `.github/workflows/public-unsigned-release.yml` workflow builds
unsigned Windows and Mac Catalyst diagnostic artifacts and publishes
EN/NL/DE/FR/ES What's New files when the required repository secrets are
configured.

This scaffold has no app-level third-party NuGet dependencies beyond
Microsoft.Maui.Controls `10.0.80`. The MAUI workloads are SDK/platform
prerequisites and may download Microsoft workload packs when first installed.

## Security

- Do not log or commit bearer tokens, passwords, OAuth tokens or other secrets.
- Spotify OAuth, refresh tokens and backend-specific music credentials remain
  in Home Assistant.
- Ask Music DNA and history remain server-side in Home Assistant.
- Local app settings are non-secret JSON under the user's application data
  folder. The DJConnect bearer token is stored in Windows Credential Manager or
  macOS Keychain.
- Logs, feedback bodies, crash reports, diagnostics and clipboard exports must
  pass through `DiagnosticRedactor` before preview, copy, storage or issue URL
  creation.
- Diagnostics and crash reports are never uploaded automatically; opening a
  GitHub issue only pre-fills redacted text and leaves submission to the user.

## License And Legal

Copyright (c) 2026 Peter van Tol.

DJConnect is MIT-licensed. Third-party dependencies keep their own
licenses.

Spotify is a trademark of Spotify AB. DJConnect is not affiliated with,
endorsed by, or sponsored by Spotify AB.
