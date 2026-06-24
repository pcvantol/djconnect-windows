# Contributing

DJConnect is maintained with careful, reviewable changes. Keep contributions
small enough to inspect and update docs when behavior changes.

## Guidelines

- Preserve the Home Assistant trust boundary.
- Do not introduce storage for Spotify credentials, OAuth tokens, DJ Memory or
  Ask DJ history as source of truth.
- Keep `client_type` and device-id conventions centralized.
- Add or update tests for protocol behavior once the test project exists.
- Update `THIRD_PARTY_NOTICES.md` when dependencies change.
- Never commit secrets, tokens, passwords, local settings or build artifacts.

## Checks

Run, where available:

```sh
./run_tests.sh
dotnet format DJConnect.Windows.sln --verify-no-changes --no-restore
dotnet build DJConnect.Windows.sln
```
