# DJConnect

DJConnect is a native desktop client for the Home Assistant `djconnect` custom
integration, targeting Windows and macOS from one .NET codebase. It follows the
macOS app's functional shape: pairing, Ask DJ chat with server-side history,
Play Now/follow-up actions, recent listening list rendering, playback controls,
status, settings and a compact legal/about section.

Home Assistant remains the trusted backend. The desktop app does not store
Spotify credentials, Spotify OAuth tokens, DJ Memory or Ask DJ server history as
the source of truth. The only app-owned credential is the DJConnect bearer token
issued by Home Assistant, stored through Windows Credential Manager on Windows
and Keychain on macOS.

## Current Version

- Desktop app: `3.1.1`
- Home Assistant protocol line: `3.1.x`
- Current local `client_type`: `windows`

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
- [docs/TECHNICAL_DESIGN_DECISIONS.md](docs/TECHNICAL_DESIGN_DECISIONS.md):
  code-level patterns and dependency notes.
- [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md): local setup, build and pairing.
- [docs/RELEASE.md](docs/RELEASE.md): release checklist and packaging open
  work, public unsigned release publication and What's New publication.
- [docs/HANDOFF.md](docs/HANDOFF.md), [docs/TODO.md](docs/TODO.md) and
  [docs/ISSUES.md](docs/ISSUES.md): current status and backlog.
- [PRIVACY.md](PRIVACY.md), [SECURITY.md](SECURITY.md),
  [CONTRIBUTING.md](CONTRIBUTING.md) and [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md):
  project policy docs.

Release cleanup helper:

```sh
./clear_old_releases.sh --keep 1 --keep-workflow-runs 1
```

Release and cleanup automation requires GitHub Actions read/write workflow
permissions plus workflow `contents: write` and `actions: write`; see
[docs/RELEASE.md](docs/RELEASE.md).

Public unsigned releases use platform-specific tags in
[pcvantol/djconnect-app-releases](https://github.com/pcvantol/djconnect-app-releases):

- `windows/vX.Y.Z`
- `maccatalyst/vX.Y.Z`

The same release workflow publishes English and Dutch What's New JSON files to
`djconnect.dev` under `/release-notes/{windows|maccatalyst}/{en|nl}/vX.Y.Z.json`.
These artifacts are for diagnostics and internal validation until signed
Windows packaging and Mac Catalyst notarization are added.

Key screens and flows mirrored from macOS:

- Pairing/configuration: Home Assistant URL, token/pairing code and stable
  client identity.
- Ask DJ: `POST /api/djconnect/ask_dj/message`, server-side history sync via
  `GET /api/djconnect/ask_dj/history`, clear via
  `POST /api/djconnect/ask_dj/history/clear`.
- Playback actions: follow-up confirmations and Play Now actions go through
  `POST /api/djconnect/command`.
- Recent played answers: compact list rendering from returned `items[]`.
- Now Playing/status: Home Assistant status and generic playback commands.
- Settings/about: local configuration, status, MIT license and Spotify notice.

## Client Identity

The current desktop identity constants live in
`src/DJConnect.Windows/Contracts/DJConnectContract.cs`:

```text
client_type: windows
device_id: djconnect-windows-XXXXXXXXXXXX
```

Open point: the current backend docs list `esp32`, `ios`, `macos`, `watchos` and
`raspberry_pi`, but not `windows`. The desktop client keeps this value central
so it can be updated easily if the Home Assistant integration adopts a different
canonical spelling. Minimal backend/doc updates should add `windows` as an app
client type and validate the `djconnect-windows-XXXXXXXXXXXX` prefix.

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
jobs for Mac Catalyst and Windows.

Release tags run `.github/workflows/public-unsigned-release.yml`, which builds
unsigned Windows and Mac Catalyst diagnostic artifacts and publishes EN/NL
What's New files when the required repository secrets are configured.

This scaffold has no app-level third-party NuGet dependencies. The MAUI
workloads are SDK/platform prerequisites and may download Microsoft workload
packs when first installed.

## Security

- Do not log or commit bearer tokens, passwords, OAuth tokens or other secrets.
- Spotify OAuth and refresh tokens remain in Home Assistant.
- Ask DJ Memory and history remain server-side in Home Assistant.
- Local app settings are non-secret JSON under the user's application data
  folder. The DJConnect bearer token is stored in Windows Credential Manager or
  macOS Keychain.

## License And Legal

Copyright (c) 2026 Peter van Tol.

DJConnect is MIT-licensed. Third-party dependencies keep their own
licenses.

Spotify is a trademark of Spotify AB. DJConnect is not affiliated with,
endorsed by, or sponsored by Spotify AB.
