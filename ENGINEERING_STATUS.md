# Windows Engineering Status

**State:** `IMPLEMENTATION_IN_PROGRESS` — Windows runner tooling maintenance automation.

The Version 2.2 governance adoption and manifest-bound consumer were merged as PRs #17 and #18. The genuine ARM64 runner, Environment configuration and target-scoped manifest authorization are now in place. Earlier retries stopped before target mutation because `NETWORK SERVICE` could not access user-profile `pwsh`, and then because the local Execution Policy blocked generated scripts. The shared-preflight remediation selects native PowerShell 7 on Windows; this increment installs a daily SYSTEM maintenance task that installs or upgrades machine-scoped PowerShell 7 and .NET 10 through `winget`, updates installed .NET workloads and verifies the resulting machine tooling. Windows build jobs use that machine SDK rather than a temporary per-job installation.

**Blockers/limitations:** the maintenance PR and the central native-preflight PR must merge. The Windows consumer must then pin the merged central action SHA and remove its obsolete Bash prerequisite before rerun. The first SYSTEM maintenance execution is an objective environment gate; if `winget` is unavailable to SYSTEM, it reports a log-backed blocker.

**Deferred work:** install and verify the maintenance task once on the runner, pin the native preflight action in the Windows consumer, then rerun the already authorized deployment and, only after it succeeds, its separate smoke operation.

**Recommended next prompt:** after the two remediation PRs merge, adopt the central immutable preflight SHA in this consumer and remove the local Bash prerequisite; then qualify the authorized deployment and smoke.
