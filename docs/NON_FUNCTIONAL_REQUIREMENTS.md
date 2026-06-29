# Non-Functional Requirements

This document is the acceptance contract for DJConnect Windows non-functional
requirements. It applies to onboarding, pairing, Now Playing, Ask DJ, Queue,
Playlists, Settings, Logs, Privacy, Legal, Feedback, Crash reports, Demo Mode,
Mini-games and update/error states.

## Security And Privacy

- Home Assistant is the trusted backend for pairing, token lifecycle, Spotify
  OAuth, playback commands and Ask DJ.
- The Windows client stores only the DJConnect device-token issued by Home
  Assistant after pairing.
- The app must never ask for Spotify credentials, Spotify OAuth tokens, Home
  Assistant long-lived access tokens, Sonos/backend credentials, OpenAI/API
  keys or Wi-Fi passwords.
- Removed override settings such as `spotify_source` and
  `liked_proxy_playlist_uri` must not appear in UI, payloads or settings.
- Logs, feedback bodies, crash reports, diagnostics and clipboard exports must
  use `DiagnosticRedactor` before user-visible/exported output.
- Protocol `3.2.x` Windows builds must not advertise a client-hosted local API
  or mDNS pairing service.
- Diagnostics are never uploaded automatically.

## Pairing And Discovery

Protocol `3.2.x` Windows runtime has no mDNS advertiser and no client-hosted
local API server.

Required lifecycle:

- Fresh install: onboarding visible, no local API or mDNS.
- After onboarding while unpaired: pairing screen visible for local HA URL plus
  pairing code entry.
- Successful pairing: token stored in OS credential storage, HA local/remote
  URL metadata stored as non-secret settings.
- Pairing reset: token/runtime state cleared, identity and pairing code rotated,
  pairing screen shown.
- Demo Mode: no Home Assistant calls, no token writes, session-only.
- Demo monkey-test mode: when `DJCONNECT_DEMO_MONKEY_TEST` or a supported legacy
  monkey/UI-test env var is truthy, the app starts directly in Demo Mode and
  must suppress persistence, credential writes, clipboard writes, external
  browser launches, permission settings, pairing
  reset, log/history clear and demo exit actions.

## Runtime And Errors

- Version mismatches block playback, queue, playlist start, output selection and
  Ask DJ while preserving token/pairing state.
- Settings, Logs, Privacy, Legal, Feedback and Crash report actions remain
  available during update-required states.
- User-facing errors are short and non-technical. Raw HTML/proxy/decode bodies
  and stack traces are diagnostics-only after redaction.
- Network failures must not wipe pairing/token state unless an explicit
  auth-stale/token-invalid contract is received.

## Lifecycle And Performance

- Runtime polling and backend calls happen only when useful: paired, compatible,
  foreground and backend-available.
- Background/inactive states should stop or reduce polling, stop wakeword/audio
  capture and stop active game loops.
- Logs are bounded: 120 visible rows, 500 persisted rows, redacted before
  persistence/export.
- Queue and playlist rendering supports 100 normalized items with deterministic
  dedupe.
- Slider/network commands should be debounced or coalesced where appropriate.
- Demo Mode and mini-games must not create unbounded timers or busy loops.
- Monkey-test mode must remain bounded and local-only so CI can random-click the
  UI without mutating user data or contacting Home Assistant.

## Accessibility And Input

- Main flows must work with mouse, touch and keyboard.
- Focus states must remain visible in dark styling.
- Icon-only or compact buttons need understandable labels or adjacent text.
- Sliders and form controls must be keyboard accessible.
- Dialogs should provide clear cancel paths.
- Mini-game loops stop when leaving the games screen.

## Localization

- User-facing strings should be available in Dutch and English where the
  surrounding ViewModel localization pattern is used.
- The pairing UI must not ask users to copy a Windows `Client adres`.
- Release notes use Dutch when locale starts with `nl`; otherwise English.

## Verification

The local test suite guards the highest-risk NFRs:

- generic command payloads exclude removed Spotify override fields;
- redaction removes Authorization headers, bearer/device tokens, pairing codes,
  bootstrap proofs, HA tokens, push tokens, cookies/secrets and private URLs;
- protocol minor compatibility rules;
- queue and playlist normalization limits/dedupe;
- fresh install/onboarding defaults;
- crash and wakeword defaults;
- Demo Mode defaults to session-off;
- monkey-test mode is explicit and environment driven;
- absence of Windows local API/mDNS runtime code paths.

Before release, run:

```sh
./run_tests.sh
git diff --check
```

Platform builds are validated in CI. Local Mac Catalyst no-restore builds may
reach `DJConnect.dll` and then remain in MAUI bundling; stop that known local
hang and rely on CI for final platform packaging.
