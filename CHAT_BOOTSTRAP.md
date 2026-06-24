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
- Huidige lokale release/tag: `v3.1.8`.
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
- Laatste lokale package-free checks waren groen: 24 tests passed.
- Lokale Mac Catalyst debug build is groen met Xcode 26.4.1 wanneer zowel
  `MD_APPLE_SDK_ROOT` als `DEVELOPER_DIR` naar
  `/Applications/Xcode_26.4.1.app` wijzen. Een lokale no-restore compile komt
  tot `DJConnect.dll`, maar kan daarna blijven hangen in MAUI tooling/bundling;
  breek die lokale hang af en laat GitHub Actions de platformbuilds valideren
  na push.
- Release cleanup helper heet `clear_old_releases.sh`; dry-run is default.
- Werkboom bevat lokale Unreleased UI/runtime uitbreidingen:
  - permissions-uitleg voor microfoon, notificaties en lokale netwerk/firewall;
  - vernieuwde pairing-flow met Client adres, koppelcode, strikte mDNS gating
    en success-state;
  - Update Required, What's New, About, Legal en Mini-games schermen;
  - Settings, Playlists, Privacy, Logs/Diagnostiek, Feedback, Crash report,
    Wakeword-prompt state en session-only Demo Mode.
- Ask DJ ondersteunt het nieuwe server-side exchange contract:
  - `response.messages[]` is canoniek wanneer aanwezig;
  - messages kunnen `client_message_id`, `exchange_id`, `exchange_order` en
    `history_revision` hebben;
  - dedupe gebruikt primair `message.id`, daarna `client_message_id + role`;
  - `exchange_id + exchange_order` is alleen ordeningssignaal;
  - user-vragen blijven visueel boven assistant-antwoorden binnen dezelfde
    exchange, ook bij optimistic UI, HTTP response, push en history sync.
- Update Required blokkeert runtime controls bij HTTP 426, `version_mismatch`
  of HA major/minor buiten de compatibele app-reeks, maar reset geen
  pairing/token/mDNS state. Settings, logs, privacy, legal en feedback blijven
  bereikbaar.
- Pairing mDNS mag alleen adverteren wanneer `IsPairable` waar is en de lokale
  Client API draait; pairing success stopt mDNS en toont `Aan de slag!` voordat
  runtime UI wordt vrijgegeven.
- What's New gebruikt lokale `LastSeenAppVersion`: fresh install/onboarding zet
  de huidige versie als gezien, app-updates tonen release notes eenmalig,
  demo/monkey/UI-test mode slaat over. Release notes worden privacy-safe vanaf
  `djconnect.dev/release-notes/windows/...` geladen met korte timeout en
  fallbacktekst.
- Mini-games zijn volledig local-only: geen Home Assistant/API/playback/Ask
  DJ/token/mDNS gebruik. Highscores staan in Preferences onder
  `djconnect.windows.game.paddle.high`, `djconnect.windows.game.meteor.high`,
  `djconnect.windows.game.sky.high` en `djconnect.windows.game.maze.high`.
- About en Legal tonen alleen privacy-safe metadata; geen device id, private
  Home Assistant URL, token, pairingcode, credentials, raw errors of diagnostics.
- Feedback, Crash reports, Logs en Diagnostics gebruiken gedeelde redaction via
  `DiagnosticRedactor`. Preview/copy/open-issue gebeurt pas na redaction en er
  is geen automatische upload.
- Demo Mode is session-only en wordt bij startup uitgezet. Demo flows gebruiken
  geen Home Assistant calls, mDNS of token writes.
- Non-destructive monkeytest mode voor CI: `DJCONNECT_DEMO_MONKEY_TEST=1`
  start direct in Demo Mode en onderdrukt settings persistence, pairing/token
  writes, mDNS/local Client API, clipboard, browser, permission settings en
  destructieve reset/clear acties.
- Wakeword state/settings bestaan, maar `WakewordFeatureAvailable` is bewust
  `false` zolang er geen echte foreground wakeword engine is.

Belangrijke regels:
- Dit is een .NET MAUI desktop app voor Windows en macOS.
- Huidige desktop app release: `3.1.8`.
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
- Ask DJ rendering moet compatibel blijven met oude servers zonder
  `messages[]`; gebruik dan legacy `user_message`/`assistant_message` fallback
  en behoud server/created-at order waar geen exchange metadata bestaat.
- History sync gebruikt `history_revision`, `clear_revision`, trim metadata en
  maximaal 1000 berichten per HA user.
- Permissions-uitleg flags:
  `DJConnectPermissionExplanation.microphone.seen`,
  `DJConnectPermissionExplanation.notifications.seen` en
  `DJConnectPermissionExplanation.localNetwork.seen`.
- Spotify trademark/non-affiliation notice moet zichtbaar blijven waar relevant:
  `Spotify is a trademark of Spotify AB. DJConnect is not affiliated with,
  endorsed by, or sponsored by Spotify AB.`
- Repo is MIT-licensed. Copyright:
  `Copyright (c) 2026 Peter van Tol.`
- Geen secrets/tokens/wachtwoorden loggen of committen.
- Gebruik `rg` voor zoeken en `apply_patch` voor handmatige edits.
- Draai minimaal `./run_tests.sh` na protocol/model wijzigingen.
- Draai na UI/ViewModel wijzigingen ook `git diff --check`; voor platform
  compilechecks kan de Mac Catalyst no-restore build met de Xcode 26.4.1 env
  gebruikt worden, rekening houdend met de bekende bundling-hang na compile.
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
