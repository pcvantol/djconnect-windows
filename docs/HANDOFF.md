# Handoff

## Current Status

DJConnect desktop `3.3.0` is a .NET MAUI app targeting
Windows and macOS. It includes:

- local Home Assistant app-pairing through `/api/djconnect/v1/pair`;
- canonical `/api/djconnect/v1/...` HTTP routes for Home Assistant pairing,
  status, Ask DJ, Track Insight, Music DNA and Music Discovery;
- local-to-remote Home Assistant transport fallback after local pairing;
- stable `windows` client identity constants;
- typed API client for pairing, status, Ask DJ history/message/clear and
  command actions;
- Ask DJ timeline, Track Insight, action and recent-played rendering;
- Ask DJ mood values, optional audio-response mode, `links[]`/`sources[]`,
  generated-text metadata sparks, stale backend action and unsupported backend
  capability handling aligned with the Home Assistant `3.3.x` contract;
- DJ announcement output selection for device audio, Home Assistant speaker,
  both or text-only. Windows only sends `dj_announcement_output`; the Home
  Assistant speaker entity remains configured in Home Assistant DJConnect
  options, and Spotify Direct playback is not modified for announcements.
- Music DNA profile/settings/clear dashboard, opt-in/disable/clear flow,
  server-authoritative optional block rendering, websocket fast path and
  `music_dna_key` context forwarding;
- Ontdek / Music Discovery navigation, Music DNA consent gating, backend
  recommendation feed rendering, reason info actions, refresh and Play Now;
- Track Insight mood and Music DNA context forwarding, plus explicit test
  coverage that BPM and musical-key fields are not rendered;
- Now Playing, Queue and Playlists rendering with generic playback/start
  commands;
- Settings, Privacy, Logs/Diagnostics, Feedback, Crash report, Wakeword prompt
  state, Demo Mode, About, Legal, What's New and Mini-games screens;
- standard .NET resource localization for English, Dutch, German, French and
  Spanish, including centralized API-error guidance for pairing and stale auth;
- shared `DiagnosticRedactor` used before log storage/export, feedback preview,
  crash report preview, clipboard copy and GitHub issue URL construction;
- Windows Credential Manager and macOS Keychain token storage;
- automatic protocol/core tests through `./run_tests.sh`;
- GitHub Actions CI for protocol tests and Windows/macOS MAUI builds;
- public unsigned Windows release workflow validated for `v3.1.9`;
- EN/NL/DE/FR/ES What's New release-note publication to `djconnect.dev`;
- MIT license and Spotify legal notice.

## Known Blockers

- Local Mac Catalyst builds with side-by-side Xcode 26.4.x pass when both
  `MD_APPLE_SDK_ROOT=/Applications/Xcode_26.4.1.app` and
  `DEVELOPER_DIR=/Applications/Xcode_26.4.1.app/Contents/Developer` are set.
- Home Assistant DJConnect `3.2.x` support is required for
  `/api/djconnect/v1/pair`, local/remote URL metadata and music backend summary
  fields.
- Real Home Assistant field testing is still needed for the synced backend
  contract paths: local app pairing, remote fallback, Music Assistant
  unsupported-capability fallbacks, stale backend action rejection, Ask DJ
  links/sources/generated-text metadata rendering, DJ announcement output modes,
  Track Insight mood refresh Music DNA profile/settings/clear behavior and
  Ontdek recommendation
  feed/refresh/play behavior while Music DNA is enabled and disabled.
- GitHub Actions CI and public unsigned Windows publication passed for
  `v3.1.9`. The `3.2.10` canonical v1 Home Assistant API route release still
  needs CI/release validation after push.
  Mac Catalyst release artifacts continue to skip on hosted runners
  without the required Xcode 26.4.x toolchain.
- Public unsigned release automation exists; signed Windows installers,
  Windows signing and Mac Catalyst notarization are not implemented.
- Wakeword UI state exists, but the real wakeword listener remains disabled
  behind `WakewordFeatureAvailable`.

## Next Best Steps

1. Validate local pairing, remote fallback and Ask DJ against Home Assistant
   DJConnect `3.2.x`.
2. Validate Ontdek consent, refresh, reason info and Play Now against a live
   Home Assistant DJConnect backend with Music DNA enabled and disabled.
3. Extend tests for stale-pairing handling and any future real wakeword engine.
4. Add signed Windows packaging and signed/notarized Mac Catalyst distribution.
