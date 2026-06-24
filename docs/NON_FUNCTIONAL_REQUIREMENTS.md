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
- mDNS TXT records must never contain bearer tokens, Wi-Fi identifiers or
  secrets. Pairing code data is present only while pairable.
- Diagnostics are never uploaded automatically.

## Pairing And Discovery

`shouldAdvertiseMdns` is true only when onboarding is complete, Demo Mode is
off, the pairing screen is visible, the client is not paired and the local
Client API is running.

Required lifecycle:

- Fresh install: onboarding visible, no mDNS.
- After onboarding while unpaired: pairing screen visible, pairable mDNS may run.
- Successful pairing: token stored in OS credential storage, mDNS stops.
- Pairing reset: token/runtime state cleared, identity and pairing code rotated,
  pairing screen shown, mDNS starts only after the screen is visible.
- Demo Mode: no mDNS, no Home Assistant calls, no token writes, session-only.

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
- The user-facing term is `Client adres`, not `Client API URL`.
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
- mDNS TXT secret hygiene and pairable-only lifecycle snapshots.

Before release, run:

```sh
./run_tests.sh
git diff --check
```

Platform builds are validated in CI. Local Mac Catalyst no-restore builds may
reach `DJConnect.dll` and then remain in MAUI bundling; stop that known local
hang and rely on CI for final platform packaging.
