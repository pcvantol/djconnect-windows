# DJConnect Repository Status

Status: Active engineering repository

## Repository

`pcvantol/djconnect-windows`

## Role

Windows Intelligence Client UX.

## Ownership

Owns: Windows client implementation, Windows presentation/runtime behavior and localized rendering for Windows surfaces.

Does not own: backend intelligence, provider-specific playback logic, canonical Ask DJ history, canonical Profile resolution or foundation docs.

## Current Phase

Windows smoke service diagnostics, reviewable.

## Status

The Windows consumers pin the immutable central native-preflight action
revision and no longer require Bash/WSL. The authorized 3.3.0 deployment
completed successfully in run `29558445560`; smoke run `29558500687` failed
only after the installed version passed because the GUI process exited under
the non-interactive service runner.

## Blocking Dependencies

- The diagnostic smoke update must merge before the authorized smoke rerun.
- The next evidence must distinguish an application crash from expected
  non-interactive session termination before changing the runtime or runner
  topology.

## Current Prompt

Windows smoke service diagnostics

## Completion Report

`docs/WINDOWS_INTERNAL_DEPLOYMENT.md` and immutable Prompt History.

## Last Qualification

The ARM64 runner qualified successfully in service context. Earlier retries
failed closed before installation because of service-visible PowerShell,
Execution Policy and WSL/Bash preflight defects. Those defects are resolved.
Authorized deployment run `29558445560` installed the exact approved artifact.
Smoke run `29558500687` confirmed the binding and installed `3.3.0` version,
then reported only that the process exited during the bounded startup window.

## Validated Base SHA

`b44c0e5c2f5b4c3cfd4fd1d1d2fcb27e9248bca7`

This records the synchronized main SHA inspected at the start of the Windows
consumer remediation increment.

## Repository-Local Next Action

Review and merge this diagnostic update. Then rerun the already authorized
smoke against the successful deployment evidence; do not redeploy first.

## Notes

Current release target under preparation: Internal Release 3.3.0 Windows ARM64.
