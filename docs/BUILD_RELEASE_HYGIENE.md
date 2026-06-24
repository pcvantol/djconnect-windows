# Build And Release Hygiene

- Keep build outputs out of git: `bin/`, `obj/`, `publish/`, `.vs/` and
  packaging artifacts are ignored.
- Do not commit generated app packages, logs or local settings.
- Run `./run_tests.sh` before release work.
- Run formatter/YAML/whitespace checks before tagging:
  `dotnet format tests/DJConnect.Tests/DJConnect.Tests.csproj --verify-no-changes --no-restore`,
  workflow YAML parsing and `git diff --check`.
- Run `rg -n "token|password|secret|refresh" -g '!bin/**' -g '!obj/**'`
  before release and inspect matches for accidental values.
- Dependency changes must update `THIRD_PARTY_NOTICES.md` and
  `docs/TECHNICAL_DESIGN_DECISIONS.md`.
- Do not push from this repo unless explicitly requested by the maintainer.
- When the maintainer asks to release/push, push `main` and the release tag,
  validate GitHub Actions, then run `./clear_old_releases.sh` first as a
  dry-run and then with `--execute` when the plan is correct.
- Use `./clear_old_releases.sh` without `--execute` first when pruning old
  GitHub releases, tags or workflow runs.
- Confirm GitHub Actions workflow permissions are read/write before release
  cleanup; workflow-run deletion requires `actions: write`.
- Public unsigned release publication requires `PUBLIC_RELEASES_TOKEN` with
  write access to `pcvantol/djconnect-app-releases`.
- Static EN/NL What's New publication requires `WEBSITE_RELEASE_NOTES_TOKEN`
  with write access to `pcvantol/djconnect-website`.
- Keep `docs/release-notes/en/vX.Y.Z.md` and
  `docs/release-notes/nl/vX.Y.Z.md` aligned with user-visible release changes.
