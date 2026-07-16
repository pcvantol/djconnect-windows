# Windows Internal Deployment Execution-Policy Remediation

- **Prompt ID:** `WINDOWS-RELEASE-3.3-EXECUTION-POLICY-REMEDIATION`
- **Title:** Allow Windows service-run ephemeral workflow scripts
- **Generation:** 2
- **Engineering program:** Platform Release Engineering
- **Branch:** `codex/fix-windows-service-execution-policy`
- **Commit SHA:** `4d2043df1fc93e2a26776a7cfdd1d4f464c6acae`
- **Pull request:** reviewable pull request created from this branch
- **Decision:** `WINDOWS_INTERNAL_DEPLOYMENT_EXECUTION_POLICY_REMEDIATION_REVIEWABLE`

## Validation summary

Authorized rerun `29483069749` was assigned to the online ARM64 service runner and failed in `Validate immutable deployment request`. The log confirms that built-in Windows PowerShell was found, but it refused to load the generated Actions script because scripts are disabled by the local Execution Policy. It did not run readiness preflight, validate the manifest, download an artifact, install the application or publish evidence.

All Windows deployment and smoke PowerShell steps now invoke the built-in shell with `-ExecutionPolicy Bypass -File {0}`. This is scoped to the generated GitHub Actions workflow script and does not mutate the machine policy.

## Created artifacts

- This immutable Prompt History record.

## Updated artifacts

- Windows deployment workflow.
- Windows post-deployment smoke workflow.
- Windows deployment documentation.
- Rolling engineering and repository records.

## Known limitations

This remediation has not yet been exercised on the service runner. The next execution may stop at the explicit Bash prerequisite if Bash is not visible through the machine-level `PATH` to `NETWORK SERVICE`.

## Deferred work

Rerun the already authorized exact Internal Release 3.3.0 Windows deployment. Dispatch its separate post-deployment smoke only after deployment success.

## Recommended next prompt

Windows Internal Release 3.3.0 manifest-bound deployment and post-deployment smoke qualification.
