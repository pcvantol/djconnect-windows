# Windows Engineering Status

**State:** `REVIEWABLE_FROZEN` — Windows native-preflight consumer adoption.

The Version 2.2 governance adoption and manifest-bound consumer were merged as PRs #17 and #18. The genuine ARM64 runner, Environment configuration and target-scoped manifest authorization are now in place. Earlier retries stopped before target mutation because `NETWORK SERVICE` could not access PowerShell 7, and then because the prior shared Bash preflight resolved to WSL. The merged central native-preflight action selects PowerShell 7 on Windows. This increment pins that immutable central revision in both Windows deployment consumers and removes their obsolete Bash prerequisite. The separately merged maintenance task keeps PowerShell 7, .NET 10 and installed workloads current through an interactive administrator task; it is not a deployment authorization.

**Blockers/limitations:** this reviewable consumer update must merge. The runner service must expose machine-visible `pwsh`; the preflight fails closed if it does not. The daily maintenance task is an administrator-context task because WinGet is unavailable to `SYSTEM`; its successful log is evidence that the interactive tooling path is current.

**Deferred work:** after this PR merges, rerun the already authorized manifest-bound Windows deployment and, only after it succeeds, dispatch its separate smoke operation.

**Recommended next prompt:** after this PR merges, qualify the already authorized Windows deployment and post-deployment smoke. Do not dispatch smoke before a successful deployment run.
