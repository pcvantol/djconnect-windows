# Windows Engineering Status

**State:** `REVIEWABLE_FROZEN` — Windows smoke service diagnostics.

The Version 2.2 governance adoption and manifest-bound consumer were merged as PRs #17, #18 and #26. The genuine ARM64 runner, Environment configuration and target-scoped manifest authorization are now in place. The authorized 3.3.0 deployment run `29558445560` completed successfully with the approved candidate and artifact. Its first smoke run `29558500687` passed manifest binding, deployment-evidence and installed-version checks, then observed `DJConnect.exe` exit within the ten-second window. The runner is a `NETWORK SERVICE` Windows service, so this does not by itself prove a product crash. This increment retains redacted exit, session and crash-event evidence on both success and failure so the next authorized smoke determines the actual condition.

**Blockers/limitations:** this reviewable diagnostic update must merge before a diagnostic smoke rerun. A service-runner GUI launch can be session-bound; the captured process exit code and crash metadata determine whether an interactive-session relay is required. The runner service exposes machine-visible `pwsh`; the preflight passes. The daily maintenance task is an administrator-context task because WinGet is unavailable to `SYSTEM`; its successful log is evidence that the interactive tooling path is current.

**Deferred work:** after this PR merges, rerun only the already authorized manifest-bound Windows smoke operation against successful deployment run `29558445560`. Do not redeploy the artifact unless the new evidence requires it.

**Recommended next prompt:** rerun the already authorized Windows smoke against deployment run `29558445560`, inspect its retained evidence and either qualify the target or implement the minimal interactive-session relay required by evidence.
