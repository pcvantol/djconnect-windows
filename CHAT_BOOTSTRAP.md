# DJConnect Desktop Chat Bootstrap Prompt

Use this prompt to initialize a fresh AI/Codex chat for this repository.

```text
Werk in repo `/Users/pcvantol/Documents/GitHub/djconnect-windows`.

Lees eerst:
- README.md
- CHANGELOG.md
- docs/HANDOFF.md
- docs/ARCHITECTURE.md
- docs/ARCHITECTURE_DECISIONS.md
- docs/API_CONTRACT.md
- docs/TECHNICAL_DESIGN_DECISIONS.md
- docs/DEVELOPMENT.md
- docs/RELEASE.md
- docs/BUILD_RELEASE_HYGIENE.md
- docs/TODO.md
- docs/ISSUES.md
- DEVELOPMENT_ENVIRONMENT.md
- CONTRIBUTING.md
- SECURITY.md
- PRIVACY.md

Belangrijke huidige status:
- Repo: `pcvantol/djconnect-windows`.
- Remote: `git@github.com:pcvantol/djconnect-windows.git`.
- Huidige lokale release/tag: `v3.1.1`.
- App stack: .NET MAUI single-project desktop app.
- Targets:
  - `net10.0-windows10.0.19041.0`
  - `net10.0-maccatalyst`
- `global.json` pint de .NET SDK op `10.0.203`.
- CI staat in `.github/workflows/ci.yml` met:
  - protocol/core tests op Ubuntu;
  - MAUI Mac Catalyst build op macOS;
  - MAUI Windows build op Windows.
- Public unsigned release flow staat in
  `.github/workflows/public-unsigned-release.yml`:
  - triggert op `vX.Y.Z` tags of handmatige versie;
  - publiceert unsigned `windows/vX.Y.Z` en `maccatalyst/vX.Y.Z` releases naar
    `pcvantol/djconnect-app-releases` via `PUBLIC_RELEASES_TOKEN`;
  - publiceert EN/NL What's New JSON/Markdown naar `djconnect.dev` via
    `WEBSITE_RELEASE_NOTES_TOKEN`.
- Automatische tests staan in `tests/DJConnect.Tests` en draaien via
  `./run_tests.sh`.
- Lokale protocol/core checks waren groen: 6 tests passed.
- Lokale Mac Catalyst debug build is groen met Xcode 26.4.1 wanneer zowel
  `MD_APPLE_SDK_ROOT` als `DEVELOPER_DIR` naar
  `/Applications/Xcode_26.4.1.app` wijzen. Een latere lokale no-restore build
  bleef hangen in tooling/bundling en is afgebroken; laat GitHub Actions de
  platformbuilds opnieuw valideren na push.
- Release cleanup helper heet `clear_old_releases.sh`; dry-run is default.

Belangrijke regels:
- Dit is een .NET MAUI desktop app voor Windows en macOS.
- Huidige desktop app release: `3.1.1`.
- Home Assistant blijft eigenaar van pairing, Spotify OAuth/backend playback,
  Ask DJ history, DJ Memory, Assist/TTS en command execution.
- De app bewaart geen Spotify credentials, OAuth tokens, DJ Memory of Ask DJ
  server history als bron van waarheid.
- Het enige app-owned credential is de DJConnect bearer token in Windows
  Credential Manager of macOS Keychain.
- `client_type: "windows"` is centraal configureerbaar maar nog een backend
  contract open punt; documenteer benodigde backend/doc updates apart tenzij de
  gebruiker expliciet vraagt om cross-repo wijzigingen.
- Device ID conventie is momenteel `djconnect-windows-XXXXXXXXXXXX`.
- Pairing/status, Spotify OAuth/backend playback, Ask DJ history, memory,
  OTA/status en Assist/TTS blijven via Home Assistant integration lopen.
- Ask DJ gebruikt server-side endpoints:
  - `POST /api/djconnect/ask_dj/message`
  - `GET /api/djconnect/ask_dj/history`
  - `POST /api/djconnect/ask_dj/history/clear`
  - `POST /api/djconnect/command`
- History sync gebruikt `history_revision`, `clear_revision`, trim metadata en
  maximaal 1000 berichten per HA user.
- Spotify trademark/non-affiliation notice moet zichtbaar blijven waar relevant:
  `Spotify is a trademark of Spotify AB. DJConnect is not affiliated with,
  endorsed by, or sponsored by Spotify AB.`
- Repo is MIT-licensed. Copyright:
  `Copyright (c) 2026 Peter van Tol.`
- Geen secrets/tokens/wachtwoorden loggen of committen.
- Gebruik `rg` voor zoeken en `apply_patch` voor handmatige edits.
- Draai minimaal `./run_tests.sh` na protocol/model wijzigingen.
- Draai `dotnet format tests/DJConnect.Tests/DJConnect.Tests.csproj
  --verify-no-changes --no-restore` na testwijzigingen.
- Als `dotnet format` lokaal faalt op een Roslyn named-pipe permissie in de
  sandbox, draai dezelfde check buiten de sandbox; de laatste releasecheck was
  daarna groen.
- Push niet tenzij de gebruiker expliciet vraagt om pushen.
- Als de gebruiker expliciet vraagt om release/push: push `main` en de release
  tag, valideer GitHub Actions met `gh run list`, voer daarna
  `./clear_old_releases.sh --keep 1 --keep-workflow-runs 1` uit als dry-run en
  vervolgens met `--execute` als het plan klopt. Rapporteer CI/release status.
```
