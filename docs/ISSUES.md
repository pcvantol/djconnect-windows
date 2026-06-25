# Issues

## P0

- Backend contract does not yet list `windows` as a supported client type.

## P1

- Validate GitHub Actions CI and public unsigned release publication after
  pushing the repo and `v3.1.9` tag. GitHub-hosted macOS runners may have Xcode
  16.x while the .NET 10 Mac Catalyst workload currently requires Xcode 26.x;
  the workflow skips Mac Catalyst artifacts on incompatible hosted runners.
  Local Xcode 26.4.1 builds pass with both `MD_APPLE_SDK_ROOT` and
  `DEVELOPER_DIR` pointed at `/Applications/Xcode_26.4.1.app`; one later local
  no-restore build hung in tooling/bundling and was stopped.
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
