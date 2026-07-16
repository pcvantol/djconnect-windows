# Windows Management Summary

**Decision:** `WINDOWS_INTERNAL_DEPLOYMENT_EXECUTION_POLICY_REMEDIATION_IN_PROGRESS`.

The manifest-bound ARM64 consumer, genuine Windows-on-ARM service runner, Environment configuration and exact target authorization are in place. PR #19 fixed the unavailable user-profile/MSIX `pwsh`; the rerun then stopped before any target mutation because the machine PowerShell Execution Policy blocked an ephemeral runner script. This remediation uses an explicit workflow-scoped bypass with built-in Windows PowerShell. No artifact, target or manifest changes are included.
