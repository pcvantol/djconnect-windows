# Windows Internal Deployment Consumer

Status: `IMPLEMENTATION_READY_SERVICE_SHELL_REMEDIATION_REVIEWABLE`

The Windows ARM64 deployment consumer installs one manifest-bound portable artifact on the qualified self-hosted Windows-on-ARM runner. It is an internal deployment path only; it does not create an installer, publish a release, modify release tags or provide Store distribution.

## Required environment

The `windows-internal-deployment` GitHub Environment must expose the non-secret configuration variable `DJCONNECT_WINDOWS_INTERNAL_INSTALL_ROOT`. Its value must be an absolute, writable directory for the runner service account. The consumer installs the verified artifact below `current` and preserves any pre-existing installation as a run-scoped `previous-<run-id>` directory. No rollback is automatic.

The runner must be online with `self-hosted`, `Windows`, `ARM64` and `internal-release` labels. The native shared preflight requires a machine-level PowerShell 7 (`pwsh`) installation that is visible to `NETWORK SERVICE`; it must not depend on WSL or a user-profile/MSIX shell. Until the consumer adopts the merged native-preflight action SHA, its current pinned action still has a temporary Bash requirement. For a `NETWORK SERVICE` runner, place the runner under a service-readable path such as `C:\actions-runner-arm64`, not below a user profile, and ensure every parent directory is traversable by that account.

The registered `djconnect-windows11-parallels-arm64` runner is a genuine Windows-on-ARM target. Its first authorized deployment attempt stopped safely before preflight or installation because `pwsh` was unavailable to the service account. The next attempt reached the shared preflight and proved that its Bash dependency resolved to WSL. The pending native-preflight adoption removes this WSL dependency; the consumer's own ephemeral Windows PowerShell steps continue to use a workflow-scoped `-ExecutionPolicy Bypass`.

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

`.github/workflows/windows-post-deployment-smoke.yml` is a separate manual workflow. It validates the same manifest identity and successful deployment evidence, then checks the installed file version, performs one bounded 10-second safe launch/read-back and rejects a matching immediate crash event. Windows is an inbound-only client and exposes no application-local health or WebSocket endpoint, so those checks are explicitly `NOT_APPLICABLE` in the redacted smoke evidence.

## Authorization boundary

This implementation does not authorize or dispatch a deployment. A later operation still requires an approved manifest binding with the exact Windows candidate SHA, artifact ID and SHA-256, plus explicit target-scoped maintainer authorization. Smoke may be dispatched only after that deployment succeeds.
