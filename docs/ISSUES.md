# Issues

## P1

- Live Home Assistant `3.2.x` validation is still needed for local pairing,
  remote fallback, Queue, Playlists, Ask DJ actions, Ask DJ links/sources,
  mood/audio-response behavior, stale backend action rejection, Music Assistant
  unsupported-capability fallback, Feedback issue URL prefill and redacted
  diagnostics copy flows.
- Mac Catalyst artifacts still depend on an Xcode 26.4.x toolchain. The
  `v3.1.9` GitHub Actions CI and public unsigned Windows publication completed
  successfully. Validate the `3.2.8` upgrade after push. Mac Catalyst jobs
  continue to skip on hosted runners with
  incompatible Xcode versions. Local Xcode 26.4.1 builds pass with both
  `MD_APPLE_SDK_ROOT` and `DEVELOPER_DIR` pointed at
  `/Applications/Xcode_26.4.1.app`; one later local no-restore build hung in
  tooling/bundling and was stopped.
- Tests cover command payloads, backend summary/error parsing, stale backend
  action, unsupported capability contracts, stale auth guidance, localization
  completeness and privacy helpers.

## P2

- UI is functional scaffold quality, not final product polish.
- Unsigned diagnostic release zips exist through CI. Signed Windows packaging,
  Windows signing and Mac Catalyst notarization do not exist yet.
- Wakeword UI state exists, but the real wakeword listener is intentionally
  unavailable until a privacy-safe foreground engine is implemented.
