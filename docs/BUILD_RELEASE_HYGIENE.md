# Build And Release Hygiene

- Keep build outputs out of git: `bin/`, `obj/`, `publish/`, `.vs/` and
  packaging artifacts are ignored.
- Do not commit generated app packages, logs or local settings.
- Run `./run_tests.sh` before release work.
- Run `rg -n "token|password|secret|refresh" -g '!bin/**' -g '!obj/**'`
  before release and inspect matches for accidental values.
- Dependency changes must update `THIRD_PARTY_NOTICES.md` and
  `docs/TECHNICAL_DESIGN_DECISIONS.md`.
- Do not push from this repo unless explicitly requested by the maintainer.
- Use `./clear_old_releases.sh` without `--execute` first when pruning old
  GitHub releases, tags or workflow runs.
