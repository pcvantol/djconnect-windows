# Changelog

## Unreleased

## 3.2.8 - 2026-07-04

- Updated Microsoft.Maui.Controls from `10.0.20` to `10.0.80` and refreshed
  dependency/tooling release hygiene docs.
- Added the Windows Music DNA dashboard with opt-in, disable and clear flows
  backed by Home Assistant `music_dna` profile/settings/clear endpoints.
- Added Music DNA websocket fast-path support with canonical HTTP fallback and
  expanded parser/viewmodel tests for optional dashboard blocks.
- Added the first-class Ontdek / Music Discovery page with Music DNA consent
  gating, backend-rendered recommendations, reason info, refresh and Play Now.
- Expanded Music Discovery tests and docs for disabled feeds, consent wiring,
  identity query parameters and websocket feed/refresh fallbacks.

## 3.2.7 - 2026-07-04

- Synced Ask DJ generated-text spark rendering with backend metadata so
  fallback, system, error and Track Insight fallback text stay undecorated.
- Forwarded Music DNA context through Ask DJ and Track Insight, including
  `music_dna_key`, mood, language and locale across HTTP and WebSocket paths.
- Expanded Music DNA decoding for backend-owned mood and energy profile shapes,
  recent-track signal formatting and server-provided based-on/profile fields.
- Updated Track Insight tests and docs to keep BPM and musical-key fields out
  of the Windows UI while preserving supported backend analysis sections.

## 3.2.6 - 2026-07-04

- Synced the Windows app version metadata sent to Home Assistant with the
  packaged desktop version.
- Propagated the selected app language and locale through DJConnect command,
  Ask DJ, voice and WebSocket payloads so backend guidance can stay localized.
- Updated the client contract for Home Assistant `3.2.15` response shapes.
- Fixed Queue refresh to request `command: "queue"` with `limit: 100`, read
  both flat and nested queue payloads, preserve queue context and render real
  returned rows with title, artist, album and artwork metadata.

## 3.2.5 - 2026-07-01

- Completed app-wide localization for the Windows client across English,
  Dutch, German, French and Spanish, including XAML, code-behind alerts,
  onboarding, settings, mini-games, empty states and user-facing API guidance.
- Added Windows `djconnect://` protocol activation wiring so QR/deeplink
  payloads enter the existing pairing flow on cold start or while the app is
  already running.
- Tightened localization tooling with resource formatting, placeholder parity,
  duplicate key checks and developer documentation for future translated UI
  changes.
- Polished ordinary Windows UI copy so technical terms stay in About, feedback
  and diagnostics rather than primary settings and alerts.

## 3.2.4 - 2026-07-01

- Implemented the Home Assistant pairing screen and pairing flow for the
  Windows client, aligned with the Apple app contract: local HA URL plus
  six-digit pair code, QR/deeplink validation, exact `/api/djconnect/pair`
  endpoint use, `X-DJConnect-Client-Type: windows`, no bearer token on pairing,
  and paired state only after authenticated status verification.
- Centralized pairing error presentation so raw backend codes are never shown
  to users, while protocol values, JSON keys and `client_type=windows` remain
  unchanged.

## 3.2.3 - 2026-07-01

- Added complete Windows client localization infrastructure for English,
  Dutch, German, French and Spanish, including resource completeness
  validation, language selection, localized release-note lookup and centralized
  actionable API-error guidance for pairing, client type mismatch,
  unauthorized/stale auth and backend action errors.
- Updated the local SDK pin to .NET SDK `10.0.301` for the release/test
  toolchain.

## 3.2.2 - 2026-07-01

- Added a CI/security baseline for the Windows client: Windows-runner restore,
  build, tests, formatting, workflow hygiene, secret-term scanning and unsigned
  artifact sanity checks; CodeQL for C#; advisory Semgrep through the shared
  DJConnect workflow; and manual-only public unsigned release publication.

## 3.2.1 - 2026-06-27

- Synced the Windows client contract with the Home Assistant `3.2.x` backend:
  status now sends app/protocol version metadata, Ask DJ sends canonical mood
  values, `links[]` render with sources, safe backend error objects parse, and
  tests cover stale backend actions plus unsupported backend capabilities.

## 3.2.0 - 2026-06-26

- Upgraded the desktop client contract to DJConnect protocol `3.2.x`.
- Replaced the Windows-hosted local Client API/mDNS pairing flow with local
  Home Assistant pairing through `POST /api/djconnect/pair`; remote Home
  Assistant transport is only used after successful local pairing.
- Added local-to-remote Home Assistant transport selection, connection mode
  diagnostics and backend summary parsing for Spotify Direct / Music Assistant
  responses.
- Forward backend-owned playback actions without Spotify-only assumptions,
  including Music Assistant action values and optional backend revision data.

## 3.1.10 - 2026-06-26

- Added Ask DJ technical track analysis contract v2 support. The client now
  detects technical analysis responses, renders server-provided sections,
  timeline entries, DJ tips and limitations in a compact read-only card, and
  preserves v1 measured/inferred fallback rendering.
- Kept technical analysis responses informational by only showing media and
  playback actions when the backend explicitly returns `images[]` or
  `playback_actions[]`, without deriving BPM, structure labels, timestamps or
  tips from prose.

## 3.1.9 - 2026-06-25

- Tightened the Ask DJ client contract so text and future voice/PTT requests
  stay backend-owned, response-owned images/sources render per bubble, commands
  include client identity/message ids and stale pairing clears the local
  Ask DJ cache before re-pairing.
- Added non-destructive Demo Mode monkey-test support for CI/UI stress runs.
  `DJCONNECT_DEMO_MONKEY_TEST=1` starts the app directly in Demo Mode, suppresses
  persistence, pairing/token writes, mDNS/local Client API startup, clipboard
  writes, external browser launches, permission settings and destructive clear
  or reset actions.

## 3.1.8 - 2026-06-24

- Re-ran the 3.1.x desktop feature release with Windows unsigned artifacts
  restoring each runtime immediately before its matching no-restore publish.
- Includes the 3.1.2 desktop UX, privacy, diagnostics, Demo Mode, Wakeword-gated
  UI and non-functional requirement coverage.

## 3.1.7 - 2026-06-24

- Re-ran the 3.1.x desktop feature release with the Windows runtime identifier
  restore list escaped for MSBuild on GitHub Actions.
- Includes the 3.1.2 desktop UX, privacy, diagnostics, Demo Mode, Wakeword-gated
  UI and non-functional requirement coverage.

## 3.1.6 - 2026-06-24

- Re-ran the 3.1.x desktop feature release with CI and release Mac Catalyst
  gating fixed to Xcode 26.4.x.
- Restored Windows unsigned artifacts with a single explicit
  `win-x64;win-arm64` restore before no-restore publish.
- Includes the 3.1.2 desktop UX, privacy, diagnostics, Demo Mode, Wakeword-gated
  UI and non-functional requirement coverage.

## 3.1.5 - 2026-06-24

- Re-ran the 3.1.x desktop feature release with Windows unsigned publish
  restoring per runtime during publish instead of relying on stale no-restore
  assets.
- Tightened Mac Catalyst release artifact gating to Xcode 26.4.x, matching the
  installed .NET 10 Mac Catalyst workload requirement.
- Includes the 3.1.2 desktop UX, privacy, diagnostics, Demo Mode, Wakeword-gated
  UI and non-functional requirement coverage.

## 3.1.4 - 2026-06-24

- Re-ran the 3.1.x desktop feature release with explicit Windows runtime
  identifiers in the app project for `win-x64` and `win-arm64` unsigned release
  artifacts.
- Includes the 3.1.2 desktop UX, privacy, diagnostics, Demo Mode, Wakeword-gated
  UI and non-functional requirement coverage.

## 3.1.3 - 2026-06-24

- Re-ran the 3.1.2 desktop feature release with explicit Windows runtime
  restore/publish properties for `win-x64` and `win-arm64` unsigned release
  artifacts.
- Includes the 3.1.2 desktop UX, privacy, diagnostics, Demo Mode, Wakeword-gated
  UI and non-functional requirement coverage.

## 3.1.2 - 2026-06-24

- Added client-side local pairing API and `_djconnect._tcp` mDNS discovery flow
  matching the Apple client contract.
- Added onboarding gating so mDNS discovery starts only after onboarding is
  dismissed and the pairing screen is active.
- Added the one-time interactive DJConnect welcome wizard persisted through
  `DJConnectWelcomeSeen`, with Home Assistant setup guidance and feature steps.
- Implemented the data-driven Speelt Nu / Now Playing page with playback
  status, artwork fallback, generic playback commands, output selection,
  volume debounce and compatibility gating.
- Implemented the Ask DJ chat page with backend-synchronized history merge,
  pending user bubbles, system-message rendering, optional audio replay,
  returned playback/confirmation actions and privacy-safe error notices.
- Implemented the Wachtrij / Queue page with backend queue normalization,
  deterministic dedupe, a 100-item render limit, empty/loading/notices and
  generic queue item start commands.
- Added pairing reset behavior that clears the stored DJConnect device token,
  rotates install identity and pairing code, then re-enables pairable mDNS.
- Replaced the mobile bottom-tab scaffold with a desktop sidebar flow that
  follows the macOS client's NavigationSplitView structure.
- Added DJConnect app icon resources for Windows and Mac Catalyst builds.
- Fixed default Mac Catalyst contrast by forcing the light app theme and
  explicit desktop content colors.
- Added Windows ARM64 unsigned diagnostic release artifacts for Windows on ARM,
  including Parallels Windows VMs on Apple Silicon Macs.
- Added CI concurrency cancellation so newer runs for the same branch cancel
  older in-progress CI attempts.
- Updated repository documentation to reflect the `3.1.2` release state,
  public unsigned release flow and EN/NL What's New publication.
- Documented standard release hygiene: docs, tests, GitHub CI validation and
  old release/tag/workflow cleanup.
- Fixed CI/release workflow handling for GitHub-hosted macOS runners without
  Xcode 26.x and restored Windows release publish with an explicit `win-x64`
  runtime restore.
- Disabled ReadyToRun for unsigned Windows diagnostic publish artifacts so
  GitHub release builds do not require an unavailable optimization runtime pack.
- Added contextual app-permission explanations for Ask DJ microphone use,
  notification opt-in and local network/firewall pairing before relevant
  Windows prompts or settings actions.
- Added an Update Required screen that blocks runtime controls on DJConnect
  protocol mismatches while keeping settings, logs, privacy, legal and feedback
  available.
- Reworked the About screen with privacy-safe app metadata, setup/help links,
  project notes, credits and navigation to privacy, legal, feedback, logs and
  release notes.
- Added a local-only Mini-games screen with Paddle Rally, Meteor Run, Sky Dash
  and Maze Chase, local highscores and lifecycle cleanup when leaving the page.
- Added a one-time What's New screen after app updates with localized release
  note loading from djconnect.dev and a safe fallback when notes are unavailable.
- Reworked the pairing screen with Client adres and koppelcode copy actions,
  local-network setup guidance, stricter pairable mDNS gating and a success
  state before entering the runtime UI.
- Reworked the Legal screen with MIT license details, copy actions, privacy and
  trademark summaries, Home Assistant backend notes, third-party notices and
  project/security links.
- Implemented the Feedback screen with Bug/Idee/Vraag/App Store feedback
  types, privacy-safe context, opt-in redacted logs, editable preview, clipboard
  copy and optional GitHub issue prefill without automatic upload.
- Implemented the Settings screen with connection, output/playback, Ask DJ,
  Demo Mode, permissions, diagnostics and app-info sections, including pairing
  reset confirmation and privacy-safe client-address copy.
- Implemented the Afspeellijsten / Playlists page with backend shape
  normalization, deterministic dedupe, search/filtering, 100-item cap, selected
  output support and generic `playlist_start` commands.
- Implemented the Privacy screen with local stored/not-stored data summaries,
  Home Assistant ownership notes, diagnostics controls, permission explanations
  and deletion/reset actions.
- Implemented privacy-safe Crash report prompting after unclean shutdowns, with
  debugger/UI-test suppression, redacted preview/copy/open-issue actions and no
  automatic upload.
- Implemented Logs / Diagnostiek with structured redacted log entries, bounded
  persistence, level filtering, search navigation, copy and wipe actions.
- Added Wakeword / Stemactivatie prompt state and disabled settings controls
  behind a feature gate until a real foreground wakeword engine exists.
- Implemented session-only Demo Mode as a separate local UX flow with sample
  queue/playlists, local Ask DJ responses and no Home Assistant calls, mDNS or
  token writes.
- Added shared `DiagnosticRedactor` coverage and non-functional requirement
  tests for redaction, compatibility, playlist/queue normalization, crash flags,
  wakeword defaults, demo session behavior, diagnostics preferences, permission
  flags and mDNS lifecycle.

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
