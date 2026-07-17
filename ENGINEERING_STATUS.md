# Windows Engineering Status

**State:** `MERGED_RECONCILED` — Windows smoke service diagnostics.

PR [#27](https://github.com/pcvantol/djconnect-windows/pull/27) merged at `29e998874db2bdb813fe3a4d614367c6da2f7fd4`. The diagnostic evidence contract is completed, merged and reconciled. The later authorized 3.3.0 deployment run `29583151393` completed successfully with the approved candidate and artifact. Smoke run `29583233193` passed manifest binding, deployment-evidence and installed-version checks, then observed `DJConnect.exe` exit within the ten-second window in session 0, with no matching Application or .NET crash event. This is objective evidence that the current service-runner launch is non-interactive; it neither proves an application defect nor qualifies the GUI target.

**Blockers/limitations:** Windows release qualification is blocked only on a least-privilege interactive-session GUI smoke relay. The runner service exposes machine-visible `pwsh`; the preflight passes. The daily maintenance task is an administrator-context task because WinGet is unavailable to `SYSTEM`; its successful log is evidence that the interactive tooling path is current.

**Deferred work:** implement and qualify a least-privilege interactive-session relay for the already deployed Windows artifact. Do not weaken the deployment service identity or redeploy the unchanged artifact solely to address this smoke-topology gap.

**Recommended next prompt:** implement the minimal least-privilege interactive Windows GUI smoke relay, then rerun the already authorized smoke against deployment run `29583151393`.
