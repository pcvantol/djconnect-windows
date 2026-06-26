# Issues

## P0

- Backend contract does not yet list `windows` as a supported client type.

## P1

- Mac Catalyst artifacts still depend on an Xcode 26.4.x toolchain. The
  `v3.1.9` GitHub Actions CI and public unsigned Windows publication completed
  successfully. Validate the `v3.1.10` release after push. Mac Catalyst jobs
  continue to skip on hosted runners with
  incompatible Xcode versions. Local Xcode 26.4.1 builds pass with both
  `MD_APPLE_SDK_ROOT` and `DEVELOPER_DIR` pointed at
  `/Applications/Xcode_26.4.1.app`; one later local no-restore build hung in
  tooling/bundling and was stopped.
- Live Home Assistant validation is still needed for pairing, status, Queue,
  Playlists, Ask DJ actions, Feedback issue URL prefill and redacted diagnostics
  copy flows.
- Tests cover command payloads and privacy helpers, but stale-pairing handling
  still needs focused coverage.

## P2

- UI is functional scaffold quality, not final product polish.
- Unsigned diagnostic release zips exist through CI. Signed Windows packaging,
  Windows signing and Mac Catalyst notarization do not exist yet.
- Wakeword UI state exists, but the real wakeword listener is intentionally
  unavailable until a privacy-safe foreground engine is implemented.
