# Build And Release Hygiene

- Keep build outputs out of git: `bin/`, `obj/`, `publish/`, `.vs/` and
  packaging artifacts are ignored.
- Do not commit generated app packages, logs or local settings.
- Run `./run_tests.sh` before release work.
- Run formatter/YAML/whitespace checks before tagging:
  `dotnet format tests/DJConnect.Tests/DJConnect.Tests.csproj --verify-no-changes --no-restore`,
  workflow YAML parsing and `git diff --check`.
- Before every release, review dependency and tooling updates: `global.json`,
  .NET MAUI workload requirements, NuGet `PackageReference` versions,
  GitHub Actions versions, `release.sh` and cleanup helpers. Use
  `dotnet list ... package --outdated --include-transitive` and
  `dotnet workload list` as the baseline audit commands.
- Run `rg -n "token|password|secret|refresh" -g '!bin/**' -g '!obj/**'`
  before release and inspect matches for accidental values.
- Dependency changes must update `THIRD_PARTY_NOTICES.md` and
  `docs/THIRD_PARTY_NOTICES.md`, `docs/TECHNICAL_DESIGN_DECISIONS.md` and any
  affected setup/release docs in the same release commit.
- CI includes a dependency/tooling hygiene job that verifies package references
  are reflected in third-party/dependency docs and reports outdated NuGet
  packages without auto-updating them.
- Do not push from this repo unless explicitly requested by the maintainer.
- When the maintainer asks to release/push, push `main` and the release tag,
  validate GitHub Actions, manually start public unsigned publication only when
  requested, then run `./clear_old_releases.sh` first as a dry-run and then
  with `--execute` when the plan is correct.
- CI uses concurrency cancellation, so a newer run for the same branch cancels
  older in-progress attempts.
- Use `./clear_old_releases.sh` without `--execute` first when pruning old
  GitHub releases, tags or workflow runs.
- Normal CI/security workflows use read-only repository permissions.
- Confirm GitHub Actions workflow permissions are read/write before release
  cleanup; workflow-run deletion requires `actions: write`.
- Manual public unsigned release publication requires `PUBLIC_RELEASES_TOKEN`
  with write access to `pcvantol/djconnect-app-releases`.
- Static EN/NL/DE/FR/ES What's New publication requires `WEBSITE_RELEASE_NOTES_TOKEN`
  with write access to `pcvantol/djconnect-website`.
- Keep `docs/release-notes/{en|nl|de|fr|es}/vX.Y.Z.md` aligned with
  user-visible release changes.
