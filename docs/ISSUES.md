# Issues

## P0

- Backend contract does not yet list `windows` as a supported client type.

## P1

- Validate GitHub Actions CI and public unsigned release publication after
  pushing the repo and `v3.1.1` tag. Local Xcode 26.4.1 builds pass with both
  `MD_APPLE_SDK_ROOT` and `DEVELOPER_DIR` pointed at
  `/Applications/Xcode_26.4.1.app`; one later local no-restore build hung in
  tooling/bundling and was stopped.
- Ask DJ action cards render but are not yet clickable command buttons.
- Tests cover initial API payload compatibility, but not command execution
  defaults or stale-pairing handling yet.

## P2

- UI is functional scaffold quality, not final product polish.
- Unsigned diagnostic release zips exist through CI. Signed Windows packaging,
  Windows signing and Mac Catalyst notarization do not exist yet.
- Diagnostics/log export is not implemented.
