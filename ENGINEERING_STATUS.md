# Windows Engineering Status

**State:** `REVIEWABLE_FROZEN` — Windows interactive GUI smoke relay.

PR [#27](https://github.com/pcvantol/djconnect-windows/pull/27) merged at `29e998874db2bdb813fe3a4d614367c6da2f7fd4`. The diagnostic evidence contract is completed, merged and reconciled. The later authorized 3.3.0 deployment run `29583151393` completed successfully with the approved candidate and artifact. Smoke run `29583233193` passed manifest binding, deployment-evidence and installed-version checks, then observed `DJConnect.exe` exit within the ten-second window in session 0, with no matching Application or .NET crash event. This is objective evidence that the current service-runner launch is non-interactive; it neither proves an application defect nor qualifies the GUI target.

**Current increment:** The smoke workflow now validates and orchestrates from the hardened service runner but never launches `DJConnect.exe` there. It submits a correlation-bound version request to an installed, limited interactive-token scheduled task. The task uses a fixed ProgramData script/configuration, rejects session 0, reports only redacted lifecycle evidence and terminates the bounded GUI process. ACLs grant the service account only request-write/result-read rights.

**Blockers/limitations:** An administrator must install or update the relay on the Windows VM while the intended smoke user is signed in. This reviewable implementation does not execute an operational smoke or change the approved artifact, manifest binding, deployment authorization or hardened service identity.

**Deferred work:** after merge and relay installation, rerun smoke against existing deployment run `29583151393`. Do not redeploy the unchanged artifact.

**Recommended next prompt:** install and qualify the merged interactive relay, then rerun the already authorized smoke against deployment run `29583151393`.
