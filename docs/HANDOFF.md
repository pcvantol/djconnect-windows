# Handoff

## Current Status

DJConnect desktop `3.1.0` has been scaffolded as a .NET MAUI app targeting
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
- MIT license and Spotify legal notice.

## Known Blockers

- MAUI workloads are not installed on the current machine, so build stops with
  `NETSDK1147` until `dotnet workload restore` is run.
- The backend contract does not yet document `client_type: "windows"`.
- CI exists but has not yet been validated in GitHub Actions.
- Packaging/signing/notarization are not implemented.

## Next Best Steps

1. Install MAUI workloads and run Windows/macOS builds.
2. Add backend/doc support for `windows` client type in the Home Assistant repo.
3. Extend tests to command execution defaults and history clear behavior.
4. Validate live pairing and Ask DJ against Home Assistant.
5. Validate GitHub Actions runs, then add packaging and release automation.
