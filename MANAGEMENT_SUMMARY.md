# Windows Management Summary

**Decision:** `WINDOWS_INTERNAL_DEPLOYMENT_SERVICE_SHELL_REMEDIATION_IN_PROGRESS`.

The manifest-bound ARM64 consumer, genuine Windows-on-ARM service runner, Environment configuration and exact target authorization are in place. The first authorized deployment stopped before any target mutation because its `NETWORK SERVICE` account could not resolve user-profile/MSIX `pwsh`. This remediation uses the Windows built-in PowerShell and verifies Bash in the same service context. No artifact, target or manifest changes are included.
