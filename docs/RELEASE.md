# Release

This repo is an early desktop scaffold. Releases should stay unsigned/private
until the app has packaging, signing, notarization and live Home Assistant
validation.

Current release: `3.1.0`.

## Pre-Release Checklist

- Confirm the working tree contains only intended changes.
- Update `README.md`, `CHANGELOG.md`, `docs/HANDOFF.md`,
  `docs/TODO.md`, `docs/ISSUES.md`, `docs/API_CONTRACT.md` and
  `docs/TECHNICAL_DESIGN_DECISIONS.md` for changed behavior.
- Confirm `THIRD_PARTY_NOTICES.md` is current for dependencies and platform
  APIs.
- Confirm the Spotify trademark/non-affiliation notice remains visible in docs
  and About UI.
- Run `./run_tests.sh`.
- Run formatting and build checks on a machine with MAUI workloads installed.
- Validate pairing, status, Ask DJ message/history/clear and command actions
  against a real Home Assistant `djconnect` integration.
- Confirm no secrets, tokens, passwords or OAuth values are committed.

## Packaging Open Work

- Windows MSIX or installer packaging.
- macOS app bundle signing and notarization.
- Version injection into status payloads.
- CI build matrix for Windows and macOS.
  Initial GitHub Actions workflow exists in `.github/workflows/ci.yml`.

## Release Cleanup

Old semantic-version GitHub releases, tags and workflow runs can be inspected
with:

```sh
./clear_old_releases.sh --keep 1 --keep-workflow-runs 1
```

When the dry-run output is correct, execute the cleanup:

```sh
./clear_old_releases.sh --keep 1 --keep-workflow-runs 1 --execute
```

The script only deletes remote releases/tags, local tags and workflow runs when
`--execute` is passed.
