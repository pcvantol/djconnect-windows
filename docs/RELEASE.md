# Release

This repo is an early desktop scaffold. Public CI releases are unsigned
diagnostic artifacts until the app has Windows packaging/signing, Mac Catalyst
signing/notarization and live Home Assistant validation.

Current release: `3.3.0`.

## Pre-Release Checklist

- Confirm the working tree contains only intended changes.
- Review `/Users/pcvantol/Documents/GitHub/djconnect/SYNC_PROMPTS.md` and
  `/Users/pcvantol/Documents/GitHub/djconnect/PRODUCT_ROADMAP.md` before
  publishing. Keep those central files in `pcvantol/djconnect`; do not copy
  them into this repository.
- Update `README.md`, `CHANGELOG.md`, `docs/HANDOFF.md`,
  `docs/TODO.md`, `docs/ISSUES.md`, `docs/API_CONTRACT.md` and
  `docs/TECHNICAL_DESIGN_DECISIONS.md` for changed behavior.
- Treat documentation updates as release-blocking work. If CI, release
  automation, permissions, download paths or app startup What's New behavior
  changes, update the relevant docs in the same release commit.
- Confirm `THIRD_PARTY_NOTICES.md` is current for dependencies and platform
  APIs.
- Check for dependency and tooling updates before tagging: .NET SDK in
  `global.json`, MAUI workload requirements, NuGet `PackageReference` versions,
  GitHub Actions versions and release/build helper tools. Any accepted update
  must also update `THIRD_PARTY_NOTICES.md`,
  `docs/THIRD_PARTY_NOTICES.md`, `docs/TECHNICAL_DESIGN_DECISIONS.md` and the
  relevant setup/release docs in the same release commit.
- Confirm the Spotify trademark/non-affiliation notice remains visible in docs
  and About UI.
- If this release changes cross-repo contracts, endpoints, client types,
  Spotify OAuth behavior, discovery/pairing, firmware/app publication or
  shared release hygiene, update `pcvantol/djconnect/SYNC_PROMPTS.md` in the
  Home Assistant integration repo or open a follow-up there.
- Run `./run_tests.sh`.
- Run formatting and build checks on a machine with MAUI workloads installed.
- Validate pairing, status, Ask DJ message/history/clear and command actions
  against a real Home Assistant `djconnect` integration.
- Confirm no secrets, tokens, passwords or OAuth values are committed.
- Confirm the repository secret `PUBLIC_RELEASES_TOKEN` is present only when a
  maintainer intentionally starts the manual public unsigned release workflow.
- Add/update `docs/release-notes/{en|nl|de|fr|es}/vX.Y.Z.md` when the in-app
  What's New text should differ from the full changelog.
- Push `main` and the release tag only when explicitly requested by the
  maintainer.
- After pushing, validate GitHub Actions with `gh run list` and inspect failed
  jobs before considering the source release complete.
- Start the manual public unsigned release workflow only when publication is
  intended. After the new public release is published and validated, run the
  cleanup helper in dry-run mode and then execute it when the plan is correct.

## Standard Release Hygiene

Every release should follow this order unless the maintainer explicitly asks
for a different flow:

1. Update app version/build metadata, changelog, release notes and repo docs.
2. Run local checks:

   ```sh
   ./run_tests.sh
   dotnet list src/DJConnect.Windows/DJConnect.Windows.csproj package --outdated --include-transitive
   dotnet list tests/DJConnect.Tests/DJConnect.Tests.csproj package --outdated --include-transitive
   dotnet workload list
   dotnet format tests/DJConnect.Tests/DJConnect.Tests.csproj --verify-no-changes --no-restore
   ruby -e 'require "yaml"; Dir[".github/workflows/*.{yml,yaml}"].each { |f| YAML.load_file(f); puts "ok #{f}" }'
   git diff --check
   ```

3. Review GitHub Actions and helper tool versions (`actions/checkout`,
   `actions/setup-dotnet`, `release.sh`, `clear_old_releases.sh`) and update
   them when the release intentionally accepts a tooling refresh.
4. Commit the release changes and create or move the local annotated
   `vX.Y.Z` tag before it is pushed.
5. Before any release publication helper pushes a release commit, verify the
   current `HEAD` is based on the intended remote release branch, then push the
   release commit explicitly to that branch, for example `git push origin
   HEAD:main`. Do not rely on a potentially stale local `main` ref.
6. Push `main` and the release tag only after explicit maintainer approval.
7. Validate GitHub CI/security workflows:

   ```sh
   gh run list --repo pcvantol/djconnect-windows --limit 10
   gh release list --repo pcvantol/djconnect-windows --limit 5
   gh release list --repo pcvantol/djconnect-app-releases --limit 10
   ```

   Newer CI runs for the same branch should cancel older in-progress attempts
   through workflow concurrency.

8. If public unsigned artifacts should be published, manually start
   `.github/workflows/public-unsigned-release.yml` with the release version and
   validate the public release repository afterward. This workflow is not
   triggered by tag pushes and is the only workflow that should receive
   publication secrets.

9. Run cleanup:

   ```sh
   ./clear_old_releases.sh --keep 1 --keep-workflow-runs 2
   ./clear_old_releases.sh --keep 1 --keep-workflow-runs 2 --execute
   ```

10. Re-check `gh run list` and document any remaining failed/pending workflows.

## Public Unsigned Releases

Workflow:

```text
.github/workflows/public-unsigned-release.yml
```

The workflow is manual-only through `workflow_dispatch` with a semver version.
It builds unsigned Windows and Mac Catalyst artifacts, creates SHA-256 checksum
files and publishes them to:

```text
pcvantol/djconnect-app-releases
```

Public release tags in that repository:

- `windows/vX.Y.Z`: unsigned Windows publish output zips for `x64` and
  `arm64`.
- `maccatalyst/vX.Y.Z`: unsigned Mac Catalyst app bundle zip.

This mirrors the Apple client repo's public unsigned release model, where each
platform gets its own namespaced public release tag. These artifacts are for
diagnostics and internal validation; they are not store/MSIX installers,
signed Windows packages, signed Mac apps or notarized Mac apps.

Required secret in this repository:

```text
PUBLIC_RELEASES_TOKEN
```

The token must have access to `pcvantol/djconnect-app-releases` with:

- Repository contents: read/write, so releases and tags can be created,
  updated and deleted.
- Metadata: read.

Required secret for in-app What's New publication:

```text
WEBSITE_RELEASE_NOTES_TOKEN
```

The token must have write access to `pcvantol/djconnect-website`. The workflow
publishes static Markdown and JSON files to:

```text
wwwroot/release-notes/windows/{en|nl|de|fr|es}/vX.Y.Z.{md,json}
wwwroot/release-notes/maccatalyst/{en|nl|de|fr|es}/vX.Y.Z.{md,json}
wwwroot/release-notes/windows/vX.Y.Z.{md,json}
wwwroot/release-notes/maccatalyst/vX.Y.Z.{md,json}
```

The non-localized paths are English fallbacks for older clients.

The workflow deliberately exposes `PUBLIC_RELEASES_TOKEN` only in the final
publish job, after unsigned build artifacts have been produced by the
platform-specific jobs. `WEBSITE_RELEASE_NOTES_TOKEN` is only exposed in the
static release-note publication step. Signing keys, API tokens, pairing tokens
and app secrets are not used by CI; future signing must be added as a separate
manual release workflow with scoped secrets.

## In-App What's New Notes

Windows and Mac Catalyst clients should load platform- and language-specific
release notes from `djconnect.dev` after an app update:

```text
https://djconnect.dev/release-notes/windows/en/vX.Y.Z.json
https://djconnect.dev/release-notes/windows/nl/vX.Y.Z.json
https://djconnect.dev/release-notes/windows/de/vX.Y.Z.json
https://djconnect.dev/release-notes/windows/fr/vX.Y.Z.json
https://djconnect.dev/release-notes/windows/es/vX.Y.Z.json
https://djconnect.dev/release-notes/maccatalyst/en/vX.Y.Z.json
https://djconnect.dev/release-notes/maccatalyst/nl/vX.Y.Z.json
https://djconnect.dev/release-notes/maccatalyst/de/vX.Y.Z.json
https://djconnect.dev/release-notes/maccatalyst/fr/vX.Y.Z.json
https://djconnect.dev/release-notes/maccatalyst/es/vX.Y.Z.json
https://djconnect.dev/release-notes/windows/vX.Y.Z.json
https://djconnect.dev/release-notes/maccatalyst/vX.Y.Z.json
```

Source order for the static notes:

1. `docs/release-notes/{en|nl|de|fr|es}/vX.Y.Z.md`.
2. The matching `CHANGELOG.md` version section for English.
3. Localized release notes must be added for every supported locale in the
   same release change. Do not publish English fallback text for localized
   in-app What's New content.

## Packaging Open Work

- Windows MSIX or installer packaging.
- Windows signing.
- Windows ARM64 release artifacts are unsigned diagnostic zips intended for
  Windows on ARM, including Parallels Windows VMs on Apple Silicon Macs.
- Mac Catalyst app bundle signing and notarization.
- GitHub-hosted Mac Catalyst release artifacts require a runner with Xcode
  26.x. The workflow skips Mac Catalyst artifact publication when hosted macOS
  only provides an older Xcode.
- Version injection into status payloads.
- Public release artifacts are currently unsigned diagnostic zips.

## Release Cleanup

Old semantic-version GitHub releases, tags and workflow runs can be inspected
with:

```sh
./clear_old_releases.sh --keep 1 --keep-workflow-runs 2
```

When the dry-run output is correct, execute the cleanup:

```sh
./clear_old_releases.sh --keep 1 --keep-workflow-runs 2 --execute
```

The script only deletes remote releases/tags, local tags and workflow runs when
`--execute` is passed.

`--keep-workflow-runs 2` is the default release-hygiene choice for this repo so
the latest CI run and the latest public unsigned release run are both retained.

## GitHub Repository Permissions

The repository must be configured so release automation and cleanup helpers can
manage tags, releases and workflow runs without HTTP 403 failures.

Required repository Settings -> Actions -> General:

- Workflow permissions: `Read and write permissions`.
- `Allow GitHub Actions to create and approve pull requests`: enabled.

Required workflow token permissions for release/cleanup-capable workflows:

```yaml
permissions:
  contents: write
  actions: write
  pull-requests: write
```

Why:

- `contents: write` allows workflows to create/push tags and manage GitHub
  releases. Public release publication uses `PUBLIC_RELEASES_TOKEN` for the
  separate public releases repo, but the source workflow still declares write
  permissions explicitly to match the release automation posture.
- `actions: write` allows workflow-run cleanup, including deleting old runs.
- `pull-requests: write` is only needed for workflows that create, update or
  approve pull requests. It is enabled in this repo's CI permissions block to
  match the release/Codex automation posture.

Release/Codex identity requirements:

- Repository permission: `WRITE`, `MAINTAIN` or `ADMIN`; release maintainers
  should use `MAINTAIN` or `ADMIN`.
- Fine-grained token or GitHub App permissions:
  - Repository contents: read/write.
  - Actions: read/write.
  - Pull requests: read/write if PR automation is used.
  - Metadata: read.
  - Administration: read when checking repository settings.

Troubleshooting `HTTP 403: Could not delete the workflow run`:

1. Check repository Actions workflow permissions are read/write.
2. Check the workflow has `actions: write`.
3. Check the authenticated user/token has repo `WRITE` or higher and Actions
   read/write permission.
4. Verify with:

   ```sh
   gh auth status
   gh repo view pcvantol/djconnect-windows --json nameWithOwner,viewerPermission
   gh run list --repo pcvantol/djconnect-windows --limit 5
   gh release list --repo pcvantol/djconnect-windows --limit 5
   ```

5. Do not delete a real workflow run as a test without explicit confirmation
   for the exact run id.

Branch/ruleset notes:

- At the time of this setup, no remote `main` branch protection or repository
  rulesets were present yet because local release commits/tags had not been
  pushed.
- Once `main` exists remotely, protect it appropriately and ensure release
  automation can still push release tags or has a documented maintainer path.
