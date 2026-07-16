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

Windows runner PowerShell 7 maintenance automation.

## Status

PowerShell 7 maintenance automation in progress; the authorized operation remains pending a rerun.

## Blocking Dependencies

- The maintenance automation and the central native-preflight remediation must
  merge before consumer adoption and rerun.
- `winget` must be available to the SYSTEM scheduled task for machine-level
  PowerShell 7 maintenance.

## Current Prompt

Windows runner PowerShell 7 maintenance automation

## Completion Report

`docs/WINDOWS_INTERNAL_DEPLOYMENT.md` and immutable Prompt History.

## Last Qualification

The ARM64 runner qualified successfully in service context. Authorized deployment
run `29482534415` failed before preflight or installation because `pwsh` was not
available to `NETWORK SERVICE`; no artifact or target mutation occurred. After
PR #19, rerun `29483069749` again stopped before preflight because the machine
PowerShell Execution Policy blocked the generated GitHub Actions script. The
shared native-preflight remediation is pending; this increment adds automatic
machine-level PowerShell 7 maintenance for the runner service.

## Validated Base SHA

`b44c0e5c2f5b4c3cfd4fd1d1d2fcb27e9248bca7`

This records the synchronized main SHA inspected at the start of the Windows
consumer remediation increment.

## Repository-Local Next Action

Review and merge the maintenance automation. Then adopt the merged central
native-preflight SHA in the consumer and rerun the already authorized
manifest-bound deployment, followed by smoke only on success.

## Notes

Current release target under preparation: Internal Release 3.3.0 Windows ARM64.
