# Windows Internal Deployment Service-Shell Remediation

- **Prompt ID:** `WINDOWS-RELEASE-3.3-SERVICE-SHELL-REMEDIATION`
- **Title:** Make the Windows internal deployment consumer service-compatible
- **Generation:** 2
- **Engineering program:** Platform Release Engineering
- **Branch:** `codex/fix-windows-service-shell`
- **Commit SHA:** `38570b2c9827e1c831d9518bed7df536cdb49ae0`
- **Pull request:** reviewable pull request created from this branch
- **Decision:** `WINDOWS_INTERNAL_DEPLOYMENT_SERVICE_SHELL_REMEDIATION_REVIEWABLE`

## Validation summary

The first authorized deployment attempt, run `29482534415`, failed in its initial immutable-request validation step with `pwsh: command not found`. It therefore did not reach canonical readiness preflight, manifest validation, artifact download, installation or evidence publication. The runner executes as `NT AUTHORITY\NETWORK SERVICE`; the interactive user's MSIX-installed PowerShell is not available in that account's execution environment.

The deployment and smoke workflows now use the built-in Windows `powershell` shell. Both explicitly verify that Bash is available to the service account before invoking the canonical shared readiness-preflight action. The smoke workflow also retains all immutable request values by keeping `GITHUB_TOKEN` in its existing environment map.

## Created artifacts

- This immutable Prompt History record.

## Updated artifacts

- Windows deployment workflow.
- Windows post-deployment smoke workflow.
- Windows internal deployment documentation.
- Rolling engineering and repository records.

## Known limitations

This remediation has not yet been executed on the service runner. A rerun may stop with the explicit Bash prerequisite error if Bash is not installed or not available through the machine-level `PATH` for `NETWORK SERVICE`.

## Deferred work

Rerun the already authorized exact Internal Release 3.3.0 Windows deployment. Dispatch its separate post-deployment smoke only after deployment success.

## Recommended next prompt

Windows Internal Release 3.3.0 manifest-bound deployment and post-deployment smoke qualification.
