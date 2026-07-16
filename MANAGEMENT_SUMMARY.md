# Windows Management Summary

**Decision:** `WINDOWS_RUNNER_TOOLING_MAINTENANCE_AUTOMATION_IN_PROGRESS`.

The manifest-bound ARM64 consumer, genuine Windows-on-ARM service runner, Environment configuration and exact target authorization are in place. The shared native-preflight remediation changes the Windows preflight runtime to PowerShell 7; this increment adds a daily SYSTEM maintenance task that keeps its machine-scoped PowerShell 7, .NET 10 SDK and installed .NET platform workloads current through `winget`, with log-backed initial verification. Windows CI and artifact jobs consume the machine SDK instead of a temporary job-local SDK download. No artifact, target, manifest or deployment authorization changes are included.
