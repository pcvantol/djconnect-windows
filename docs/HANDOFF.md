# Handoff

## Current Status

DJConnect desktop `3.1.1` has been scaffolded as a .NET MAUI app targeting
Windows and macOS. It includes:

- Home Assistant URL/token/pairing UI;
- stable `windows` client identity constants;
- typed API client for pairing, status, Ask DJ history/message/clear and
  command actions;
- Ask DJ timeline, action and recent-played rendering;
- basic playback command buttons;
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
- GitHub Actions workflow definitions exist but still need a remote run after
  the repo is pushed.
- Public unsigned release automation exists; signed Windows installers,
  Windows signing and Mac Catalyst notarization are not implemented.

## Next Best Steps

1. Push `main` and tags when the maintainer asks, then validate GitHub Actions
   CI and public unsigned release publication.
2. Add backend/doc support for `windows` client type in the Home Assistant repo.
3. Extend tests to command execution defaults and stale-pairing handling.
4. Validate live pairing and Ask DJ against Home Assistant.
5. Add signed Windows packaging and signed/notarized Mac Catalyst distribution.
