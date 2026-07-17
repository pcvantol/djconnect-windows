# Windows Smoke Service Diagnostics

- **Prompt ID:** `WINDOWS-RELEASE-3.3-SMOKE-SERVICE-DIAGNOSTICS`
- **Title:** Retain evidence for Windows smoke process exits under a service runner
- **Generation:** 2
- **Engineering program:** Platform Release Engineering
- **Branch:** `codex/windows-smoke-service-diagnostics`
- **Commit SHA:** Reviewable branch tip; recorded by the Pull Request.
- **Pull request:** Reviewable pull request created from this branch.
- **Decision:** `WINDOWS_SMOKE_SERVICE_DIAGNOSTICS_REVIEWABLE`

## Validation summary

Authorized deployment run `29558445560` completed successfully for the exact
3.3.0 manifest binding. Smoke run `29558500687` passed manifest validation,
deployment-evidence validation and installed-version validation, then reported
only that `DJConnect.exe` exited during a ten-second startup window. The
runner executes as `NETWORK SERVICE`; the prior workflow stopped before it
could retain the exit code, session type or crash-event metadata.

This increment changes the smoke evidence contract only. The workflow now
writes schema-version 2 redacted evidence on success or failure, includes
process exit/session and matching crash-event metadata, and uploads that
evidence with `if: always()`. It does not change the deployed artifact,
runtime, manifest or deployment authorization.

## Created artifacts

- This immutable Prompt History record.

## Updated artifacts

- Windows post-deployment smoke evidence contract.
- Windows deployment operational documentation.
- Rolling engineering, repository, management, roadmap and prompt records.

## Known limitations

- A Windows GUI application launched from session 0 may exit because no
  interactive desktop is available. That remains a hypothesis until the next
  smoke evidence records exit and crash details.

## Deferred work

- After merge, rerun the already authorized smoke operation against deployment
  run `29558445560`; do not redeploy the unchanged approved artifact.
- If evidence confirms a session-bound exit without a matching crash, create a
  separate increment for a least-privilege interactive-session smoke relay.

## Recommended next prompt

Run the authorized Windows post-deployment smoke using deployment evidence
from run `29558445560`, then decide qualification or an interactive-session
relay based only on its retained evidence.
