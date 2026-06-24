# Handoff

## Current Status

DJConnect desktop `3.1.8` has been scaffolded as a .NET MAUI app targeting
Windows and macOS. It includes:

- onboarding-gated local pairing API and pairable `_djconnect._tcp` mDNS;
- stable `windows` client identity constants;
- typed API client for pairing, status, Ask DJ history/message/clear and
  command actions;
- Ask DJ timeline, action and recent-played rendering;
- Now Playing, Queue and Playlists rendering with generic playback/start
  commands;
- Settings, Privacy, Logs/Diagnostics, Feedback, Crash report, Wakeword prompt
  state, Demo Mode, About, Legal, What's New and Mini-games screens;
- shared `DiagnosticRedactor` used before log storage/export, feedback preview,
  crash report preview, clipboard copy and GitHub issue URL construction;
- Windows Credential Manager and macOS Keychain token storage;
- automatic protocol/core tests through `./run_tests.sh`;
- GitHub Actions CI for protocol tests and Windows/macOS MAUI builds;
- public unsigned release workflow for Windows and Mac Catalyst artifacts;
- EN/NL What's New release-note publication to `djconnect.dev`;
- MIT license and Spotify legal notice.

## Known Blockers

- Local Mac Catalyst builds with side-by-side Xcode 26.4.x pass when both
  `MD_APPLE_SDK_ROOT=/Applications/Xcode_26.4.1.app` and
  `DEVELOPER_DIR=/Applications/Xcode_26.4.1.app/Contents/Developer` are set.
- The backend contract does not yet document `client_type: "windows"`.
- GitHub Actions workflow definitions exist but still need validation on the
  remote repository after the maintainer pushes.
- Public unsigned release automation exists; signed Windows installers,
  Windows signing and Mac Catalyst notarization are not implemented.
- Wakeword UI state exists, but the real wakeword listener remains disabled
  behind `WakewordFeatureAvailable`.

## Next Best Steps

1. Push `main` and tags when the maintainer asks, then validate GitHub Actions
   CI and public unsigned release publication.
2. Add backend/doc support for `windows` client type in the Home Assistant repo.
3. Extend tests for stale-pairing handling and any future real wakeword engine.
4. Validate live pairing and Ask DJ against Home Assistant.
5. Add signed Windows packaging and signed/notarized Mac Catalyst distribution.
