# Windows Internal Deployment Consumer

Status: `WINDOWS_INTERACTIVE_GUI_SMOKE_RELAY_IMPLEMENTATION_IN_PROGRESS`

The Windows ARM64 deployment consumer installs one manifest-bound portable artifact on the qualified self-hosted Windows-on-ARM runner. It is an internal deployment path only; it does not create an installer, publish a release, modify release tags or provide Store distribution.

## Required environment

The `windows-internal-deployment` GitHub Environment must expose the non-secret configuration variable `DJCONNECT_WINDOWS_INTERNAL_INSTALL_ROOT`. Its value must be an absolute, writable directory for the runner service account. The consumer installs the verified artifact below `current` and preserves any pre-existing installation as a run-scoped `previous-<run-id>` directory. No rollback is automatic.

The runner must be online with `self-hosted`, `Windows`, `ARM64` and `internal-release` labels. The pinned canonical readiness preflight runs with PowerShell 7 (`pwsh`) on Windows; it has no Bash or WSL dependency. PowerShell 7 must therefore be machine-visible to `NETWORK SERVICE`, not available only through a user-profile/MSIX path. For a `NETWORK SERVICE` runner, place the runner under a service-readable path such as `C:\actions-runner-arm64`, not below a user profile, and ensure every parent directory is traversable by that account.

The registered `djconnect-windows11-parallels-arm64` runner is a genuine Windows-on-ARM target. Its first authorized deployment attempt stopped safely before preflight or installation because `pwsh` was unavailable to the service account. The next attempt reached the previous shared preflight and proved that its Bash dependency resolved to WSL. The consumer now pins the merged native preflight, removing that WSL dependency; its own ephemeral Windows PowerShell steps continue to use a workflow-scoped `-ExecutionPolicy Bypass`. Authorized deployment run `29583151393` completed successfully. Smoke run `29583233193` retained decisive evidence: the installed `3.3.0` executable exited in session 0 with exit code `-1073741189` and no matching Application/.NET crash event. The current service-runner smoke is therefore not a GUI qualification path; a separate least-privilege interactive-session relay is required.

## Windows runner tooling maintenance

The canonical shared readiness preflight uses native PowerShell 7 on Windows;
it does not use WSL. Keep PowerShell 7, the .NET 10 SDK and its installed
platform workloads machine-wide and current by installing the repository
maintenance task once from an elevated Windows PowerShell session after this
script is available on the runner:

```powershell
Set-Location <djconnect-windows-clone>
.\scripts\runner\Install-DJConnectPowerShell7Maintenance.ps1 -RunNow
```

The task runs daily at 10:00 local time as the administrator who installed it,
using an interactive scheduled-task token with the highest privilege level and
without storing that user's password. This is required because WinGet/App
Installer is registered in a signed-in user's Windows context and must not be
run under `SYSTEM`. The administrator must therefore be logged in at the
scheduled time; use `-RunNow` after login for an immediate run.

The task detects PowerShell 7 through `pwsh.exe`, so a valid Microsoft Store /
App Installer installation is upgraded in its existing user context without a
forced machine scope. It installs or upgrades `Microsoft.DotNet.SDK.10`
machine-wide, runs `dotnet workload update --no-cache`, and records only
version, workload and error metadata in
`C:\ProgramData\DJConnect\runner-maintenance\runner-tooling-maintenance.log`.
WinGet exit code `0x8A15002B` (`no applicable upgrade`) is recorded as an
already-current success; package presence and usable versions are still
verified after that result. Any other non-zero WinGet exit code fails the task.
The initial `-RunNow` invocation must succeed before a Windows consumer that
uses the native preflight can be dispatched. Windows CI and unsigned release
builds then use this machine SDK directly instead of a per-job temporary
`DOTNET_INSTALL_DIR`; they fail closed if a compatible .NET 10 SDK is absent.
If WinGet is unavailable, sign in once with the maintenance administrator and
complete App Installer registration; do not fall back to WSL or a
user-profile-only `pwsh`.

## Deployment contract

`.github/workflows/windows-internal-deployment.yml` accepts only a complete immutable release identity. It runs the canonical deployment-readiness preflight, reads the canonical central manifest, verifies the exact Windows source candidate and release-asset binding, downloads the bound artifact, verifies its SHA-256 and installs only the expected portable application contents. A successful mutation publishes redacted `windows-internal-deployment-evidence` with `DEPLOYED_PENDING_SMOKE`.

## Smoke contract

`.github/workflows/windows-post-deployment-smoke.yml` is a separate manual workflow. It validates the same manifest identity and successful deployment evidence, then checks the installed file version. It writes a correlation-bound, version-only request into the relay request directory and waits at most 180 seconds for redacted result evidence. It never starts `DJConnect.exe` itself.

The installed relay is a Windows Scheduled Task running every minute with an interactive-token, limited user principal. It runs only when that configured user is signed in. The fixed relay script is copied below `ProgramData`; its configuration and executable path are not controlled by the GitHub runner. The service virtual account receives only Modify access to `requests` and read-only access to `results`; the interactive user receives the inverse result-write permission. A successful relay result requires a non-zero process session and an alive application after ten seconds. The relay always terminates that bounded process. Windows is an inbound-only client and exposes no application-local health or WebSocket endpoint, so those checks remain `NOT_APPLICABLE` in redacted smoke evidence.

Install or update the relay once from an elevated PowerShell session while the intended smoke user is signed in:

```powershell
Set-Location C:\DJConnect\source\djconnect-windows
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\runner\Install-DJConnectInteractiveGuiSmokeRelay.ps1 -InstallRoot C:\DJConnect\internal-release
```

The installer rejects service identities, requires the existing hardened runner virtual account, applies scoped ACLs and registers `\DJConnect\InteractiveGuiSmoke` with `/IT` and limited run level. It stores no password, token or GitHub credential. Keep the smoke user signed in during the GitHub smoke run; otherwise the workflow fails closed as `INTERACTIVE_RELAY_UNAVAILABLE`.

## Authorization boundary

This implementation does not authorize or dispatch a deployment. A later operation still requires an approved manifest binding with the exact Windows candidate SHA, artifact ID and SHA-256, plus explicit target-scoped maintainer authorization. Smoke may be dispatched only after that deployment succeeds.
