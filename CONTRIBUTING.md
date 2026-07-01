# Contributing

## Localization

DJConnect Windows supports English, Dutch, German, French and Spanish. Any change that adds user-facing text must add translations for all supported locales in `src/DJConnect.Windows/Resources` and pass:

```sh
python3 tools/validate-localization.py
```

Keep protocol values, JSON keys, endpoints, tokens and `client_type=windows` unchanged. Route user-visible backend/API errors through `ApiErrorLocalizer` instead of showing raw backend codes.

DJConnect is maintained with careful, reviewable changes. Keep contributions
small enough to inspect and update docs when behavior changes.

## Guidelines

- Preserve the Home Assistant trust boundary.
- Do not introduce storage for Spotify credentials, OAuth tokens, DJ Memory or
  Ask DJ history as source of truth.
- Keep `client_type` and device-id conventions centralized.
- Add or update tests for protocol behavior in `tests/DJConnect.Tests`.
- Update `THIRD_PARTY_NOTICES.md` when dependencies change.
- Update `docs/release-notes/en/vX.Y.Z.md` and
  `docs/release-notes/nl/vX.Y.Z.md` for user-visible release changes.
- Never commit secrets, tokens, passwords, local settings or build artifacts.

## Checks

Run, where available:

```sh
./run_tests.sh
dotnet format DJConnect.Windows.sln --verify-no-changes --no-restore
dotnet build DJConnect.Windows.sln
```

Do not push branches, tags or releases unless the maintainer explicitly asks.
