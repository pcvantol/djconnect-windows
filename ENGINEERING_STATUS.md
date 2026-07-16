# Windows Engineering Status

**State:** `REVIEWABLE` — Windows internal deployment service-shell remediation.

The Version 2.2 governance adoption and manifest-bound consumer were merged as PRs #17 and #18. The genuine ARM64 runner, Environment configuration and target-scoped manifest authorization are now in place. Authorized deployment run `29482534415` stopped safely before preflight or installation because the `NETWORK SERVICE` runner could not resolve user-profile/MSIX `pwsh`. This increment replaces that dependency with built-in Windows PowerShell and explicitly verifies Bash in the service context.

**Blockers/limitations:** the remediation PR must merge. Afterward, Bash must be present in the machine-level `PATH` visible to `NETWORK SERVICE`; the workflow will report a specific prerequisite failure if it is not.

**Deferred work:** rerun the already authorized manifest-bound deployment and, only after it succeeds, its separate smoke operation.

**Recommended next prompt:** merge this remediation, rerun the exact authorized Windows deployment, then dispatch the separate post-deployment smoke only on deployment success.
