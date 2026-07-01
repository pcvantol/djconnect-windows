# Localization

DJConnect Windows supports `en`, `nl`, `de`, `fr`, and `es`.

All user-facing UI text, alert text, empty states, notifications, and actionable API error guidance must be added to the standard resource files in `src/DJConnect.Windows/Resources` in the same change:

- `Strings.resx` for English
- `Strings.nl.resx`
- `Strings.de.resx`
- `Strings.fr.resx`
- `Strings.es.resx`

Do not localize protocol values, JSON keys, endpoints, tokens, command names, or `client_type`. The Windows client type must remain exactly `windows`.

Use placeholders instead of concatenating localized sentence fragments. Example: add a resource such as `Format_UpdateRequiredDetail` with `{0}`, then call `AppStrings.Format(...)`.

Backend/API errors that are shown to users must go through `ApiErrorLocalizer`. Do not display raw backend codes such as `client_type_mismatch`, `invalid_pair_code`, `invalid_client_type`, `not_configured`, `unauthorized`, or stale auth errors in alerts or notices.

Before submitting a change that adds or renames localization keys, run:

```sh
python3 tools/validate-localization.py
```

The script fails if any supported locale is missing a key or has a key not present in English.
