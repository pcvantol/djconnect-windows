# Windows Runner Tooling Maintenance Automation

- **Prompt ID:** `WINDOWS-RUNNER-TOOLING-MAINTENANCE-001`
- **Title:** Keep the Windows deployment runner's PowerShell 7, .NET 10 and platform workloads current
- **Generation:** 2
- **Engineering program:** Platform Release Engineering
- **Branch:** `codex/automate-windows-powershell-maintenance`
- **Commit SHA:** `1c1e56b33cf55cda5661b0201661c2e76502651c`
- **Pull request:** reviewable pull request created from this branch
- **Decision:** `WINDOWS_RUNNER_TOOLING_MAINTENANCE_REVIEWABLE`

## Validation summary

Windows deployment run `29483901468` proved that the previous shared Bash
preflight resolved to WSL, an unintended non-native dependency. The central
native-preflight remediation selects PowerShell 7 on Windows. This increment
adds a one-time elevated installer that creates a daily SYSTEM scheduled task.
The task uses `winget` to install or upgrade the machine-scoped
`Microsoft.PowerShell` and `Microsoft.DotNet.SDK.10` packages, updates the
installed .NET workloads, verifies the installed `pwsh.exe` and .NET 10 SDK
versions, and writes redacted operational metadata to a fixed ProgramData log.
Its optional `-RunNow` mode waits for the task and fails on a non-zero result.
Windows CI and artifact jobs now verify and use this machine SDK instead of
downloading it into `runner.temp`.

## Created artifacts

- `scripts/runner/Install-DJConnectPowerShell7Maintenance.ps1`
- `scripts/runner/Test-DJConnectDotNet10.ps1`
- This immutable Prompt History record.

## Updated artifacts

- Windows internal deployment documentation.
- Rolling engineering, repository, management and prompt records.

## Known limitations

- An administrator must run the installer once on the Windows VM after merge.
- The SYSTEM task requires `winget`; a missing SYSTEM-visible `winget` is
  logged as an objective environment blocker.
- The Windows deployment consumer still requires a separate immutable pin
  update after the central native-preflight remediation merges.

## Deferred work

- Run and verify the scheduled maintenance task on the Windows runner.
- Pin the Windows consumer to the merged central native preflight action and
  remove the obsolete Bash prerequisite.
- Rerun the already authorized deployment and dispatch smoke only on success.

## Recommended next prompt

Windows native-preflight consumer adoption and authorized deployment/smoke
qualification.
