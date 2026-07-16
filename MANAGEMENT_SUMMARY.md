# Windows Management Summary

**Decision:** `WINDOWS_RUNNER_POWERSHELL_7_MAINTENANCE_AUTOMATION_IN_PROGRESS`.

The manifest-bound ARM64 consumer, genuine Windows-on-ARM service runner, Environment configuration and exact target authorization are in place. The shared native-preflight remediation changes the Windows preflight runtime to PowerShell 7; this increment adds a daily SYSTEM maintenance task that keeps its machine-scoped PowerShell 7 package current through `winget`, with log-backed initial verification. No artifact, target, manifest or deployment authorization changes are included.
