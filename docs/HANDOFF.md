# Handoff

## Current Status

DJConnect desktop `3.2.7` has been scaffolded as a .NET MAUI app targeting
Windows and macOS. It includes:

- local Home Assistant app-pairing through `/api/djconnect/pair`;
- local-to-remote Home Assistant transport fallback after local pairing;
- stable `windows` client identity constants;
- typed API client for pairing, status, Ask DJ history/message/clear and
  command actions;
- Ask DJ timeline, Track Insight, action and recent-played rendering;
- Ask DJ mood values, optional audio-response mode, `links[]`/`sources[]`,
  generated-text metadata sparks, stale backend action and unsupported backend
  capability handling aligned with the Home Assistant `3.2.x` contract;
- Music DNA profile/settings/clear client models with server-authoritative
  mood/energy decoding and `music_dna_key` context forwarding;
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
  `/api/djconnect/pair`, local/remote URL metadata and music backend summary
  fields.
- Real Home Assistant field testing is still needed for the synced backend
  contract paths: local app pairing, remote fallback, Music Assistant
  unsupported-capability fallbacks, stale backend action rejection, Ask DJ
  links/sources/generated-text metadata rendering, Track Insight mood refresh
  and Music DNA profile/settings/clear behavior.
- GitHub Actions CI and public unsigned Windows publication passed for
  `v3.1.9`. The `3.2.7` Home Assistant client contract and Ask DJ/Track Insight/Music DNA parity release still needs CI/release validation after push.
  Mac Catalyst release artifacts continue to skip on hosted runners
  without the required Xcode 26.4.x toolchain.
- Public unsigned release automation exists; signed Windows installers,
  Windows signing and Mac Catalyst notarization are not implemented.
- Wakeword UI state exists, but the real wakeword listener remains disabled
  behind `WakewordFeatureAvailable`.

## Next Best Steps

1. Validate local pairing, remote fallback and Ask DJ against Home Assistant
   DJConnect `3.2.x`.
2. Extend tests for stale-pairing handling and any future real wakeword engine.
3. Add signed Windows packaging and signed/notarized Mac Catalyst distribution.
