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

Windows internal deployment execution-policy remediation.

## Status

Execution-policy remediation in progress; the authorized operation remains pending a rerun.

## Blocking Dependencies

- The execution-policy remediation must merge before rerun.
- Bash must be available through the machine-level `PATH` of the `NETWORK SERVICE`
  runner account for the canonical readiness preflight.

## Current Prompt

Windows internal deployment execution-policy remediation

## Completion Report

`docs/WINDOWS_INTERNAL_DEPLOYMENT.md` and immutable Prompt History.

## Last Qualification

The ARM64 runner qualified successfully in service context. Authorized deployment
run `29482534415` failed before preflight or installation because `pwsh` was not
available to `NETWORK SERVICE`; no artifact or target mutation occurred. After
PR #19, rerun `29483069749` again stopped before preflight because the machine
PowerShell Execution Policy blocked the generated GitHub Actions script.

## Validated Base SHA

`b44c0e5c2f5b4c3cfd4fd1d1d2fcb27e9248bca7`

This records the synchronized main SHA inspected at the start of the Windows
consumer remediation increment.

## Repository-Local Next Action

Review and merge the execution-policy remediation PR. Then rerun the already authorized
manifest-bound deployment and, on success, its separate smoke workflow.

## Notes

Current release target under preparation: Internal Release 3.3.0 Windows ARM64.
