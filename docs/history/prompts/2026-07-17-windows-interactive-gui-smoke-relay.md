# Windows Interactive GUI Smoke Relay

- **Prompt ID:** `WINDOWS-RELEASE-3.3-INTERACTIVE-GUI-SMOKE-RELAY`
- **Title:** Qualify Windows GUI smoke through a least-privilege interactive relay
- **Generation:** 2
- **Engineering program:** Platform Release Engineering
- **Branch:** `codex/windows-interactive-gui-smoke-relay`
- **Commit SHA:** Recorded by the reviewable Pull Request.
- **Pull request:** Reviewable pull request created from this branch.
- **Decision:** `WINDOWS_INTERACTIVE_GUI_SMOKE_RELAY_REVIEWABLE`

## Validation summary

The existing smoke evidence proved that the hardened runner service launches
in session 0. This increment leaves that identity in place and replaces only
the GUI launch path. The service workflow validates the immutable request,
approved manifest, deployment evidence and installed version, then submits a
random correlation-bound request to a ProgramData relay. A limited
interactive-token scheduled task reads that request, starts only the configured
current `DJConnect.exe`, rejects session 0 and returns bounded redacted result
evidence. The service account has request-write/result-read permissions only.

Static YAML validation passed. `./run_tests.sh` passed with 126 tests and
localization validation. PowerShell parser validation remains deferred because
PowerShell is not installed on the macOS development host; the target Windows
installation is the required platform-native validation.

## Created artifacts

- Interactive relay task installer and fixed relay script.
- Immutable Prompt History record.

## Updated artifacts

- Windows post-deployment smoke workflow and contract.
- Windows deployment documentation and rolling records.
- Static relay contract regression test.

## Known limitations

- The configured interactive user must be signed in for the task to run.
- This increment does not dispatch smoke, redeploy the artifact or change the
  approved manifest and target authorization.

## Deferred work

- Install the relay on the Windows VM and execute the authorized smoke-only
  rerun against deployment `29583151393`.

## Recommended next prompt

Platform Release Engineering: install the merged Windows interactive GUI smoke
relay and execute the already authorized smoke-only qualification run.
