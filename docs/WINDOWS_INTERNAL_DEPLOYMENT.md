# Windows Internal Deployment Consumer

Status: `IMPLEMENTATION_READY_SERVICE_SHELL_REMEDIATION_REVIEWABLE`

The Windows ARM64 deployment consumer installs one manifest-bound portable artifact on the qualified self-hosted Windows-on-ARM runner. It is an internal deployment path only; it does not create an installer, publish a release, modify release tags or provide Store distribution.

## Required environment

The `windows-internal-deployment` GitHub Environment must expose the non-secret configuration variable `DJCONNECT_WINDOWS_INTERNAL_INSTALL_ROOT`. Its value must be an absolute, writable directory for the runner service account. The consumer installs the verified artifact below `current` and preserves any pre-existing installation as a run-scoped `previous-<run-id>` directory. No rollback is automatic.

The runner must be online with `self-hosted`, `Windows`, `ARM64` and `internal-release` labels, and provide Bash in the machine-level `PATH` for the canonical shared readiness-preflight action. The service also requires the built-in `powershell.exe`; workflows deliberately do not depend on a user-profile or MSIX-installed `pwsh` and invoke Windows PowerShell with `-ExecutionPolicy Bypass` for their ephemeral GitHub Actions scripts. For a `NETWORK SERVICE` runner, place the runner under a service-readable path such as `C:\actions-runner-arm64`, not below a user profile, and ensure every parent directory is traversable by that account.

The registered `djconnect-windows11-parallels-arm64` runner is a genuine Windows-on-ARM target. Its first authorized deployment attempt stopped safely before preflight or installation because `pwsh` was unavailable to the service account. The next attempt reached the built-in shell but stopped at its first script because the local PowerShell execution policy blocked generated scripts. The workflows use `-ExecutionPolicy Bypass` narrowly for their own ephemeral steps and fail with an explicit prerequisite error if Bash is not visible to that same service context.

## Deployment contract

`.github/workflows/windows-internal-deployment.yml` accepts only a complete immutable release identity. It runs the canonical deployment-readiness preflight, reads the canonical central manifest, verifies the exact Windows source candidate and release-asset binding, downloads the bound artifact, verifies its SHA-256 and installs only the expected portable application contents. A successful mutation publishes redacted `windows-internal-deployment-evidence` with `DEPLOYED_PENDING_SMOKE`.

## Smoke contract

`.github/workflows/windows-post-deployment-smoke.yml` is a separate manual workflow. It validates the same manifest identity and successful deployment evidence, then checks the installed file version, performs one bounded 10-second safe launch/read-back and rejects a matching immediate crash event. Windows is an inbound-only client and exposes no application-local health or WebSocket endpoint, so those checks are explicitly `NOT_APPLICABLE` in the redacted smoke evidence.

## Authorization boundary

This implementation does not authorize or dispatch a deployment. A later operation still requires an approved manifest binding with the exact Windows candidate SHA, artifact ID and SHA-256, plus explicit target-scoped maintainer authorization. Smoke may be dispatched only after that deployment succeeds.
