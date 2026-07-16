# Windows Engineering Status

**State:** `IMPLEMENTATION_IN_PROGRESS` — Windows internal deployment execution-policy remediation.

The Version 2.2 governance adoption and manifest-bound consumer were merged as PRs #17 and #18. The genuine ARM64 runner, Environment configuration and target-scoped manifest authorization are now in place. Authorized deployment run `29482534415` stopped safely before preflight or installation because the `NETWORK SERVICE` runner could not resolve user-profile/MSIX `pwsh`; PR #19 replaced that dependency. Rerun `29483069749` then stopped at its first immutable-request script because the machine PowerShell Execution Policy blocked generated runner scripts. This increment invokes built-in PowerShell with a workflow-scoped execution-policy bypass.

**Blockers/limitations:** the remediation PR must merge. Afterward, Bash must be present in the machine-level `PATH` visible to `NETWORK SERVICE`; the workflow will report a specific prerequisite failure if it is not.

**Deferred work:** rerun the already authorized manifest-bound deployment and, only after it succeeds, its separate smoke operation.

**Recommended next prompt:** merge this remediation, rerun the exact authorized Windows deployment, then dispatch the separate post-deployment smoke only on deployment success.
