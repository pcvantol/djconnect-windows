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

Windows smoke service diagnostics, merged and reconciled.

## Status

The Windows consumers pin the immutable central native-preflight action
revision and no longer require Bash/WSL. PR #27 is merged at
`29e998874db2bdb813fe3a4d614367c6da2f7fd4`. The authorized 3.3.0 deployment
completed successfully in run `29583151393`; smoke run `29583233193` passed
its immutable identity, deployment-evidence and installed-version checks, then
observed the GUI process exit under the non-interactive service runner.

## Blocking Dependencies

- A least-privilege interactive-session relay is required before the Windows
  GUI smoke can qualify the deployed target.

## Current Prompt

Implement least-privilege interactive Windows GUI smoke relay

## Completion Report

`docs/WINDOWS_INTERNAL_DEPLOYMENT.md` and immutable Prompt History.

## Last Qualification

The ARM64 runner qualified successfully in service context. Earlier retries
failed closed before installation because of service-visible PowerShell,
Execution Policy and WSL/Bash preflight defects. Those defects are resolved.
Authorized deployment run `29583151393` installed the exact approved artifact.
Smoke run `29583233193` confirmed the binding and installed `3.3.0` version,
then recorded exit code `-1073741189`, session `0` and no matching Application
or .NET crash event. The GUI target remains unqualified.

## Validated Base SHA

`b44c0e5c2f5b4c3cfd4fd1d1d2fcb27e9248bca7`

This records the synchronized main SHA inspected at the start of the Windows
consumer remediation increment.

## Repository-Local Next Action

Implement the isolated interactive-session smoke relay. Then rerun the already
authorized smoke against the existing successful deployment evidence; do not
redeploy first.

## Notes

Current release target under preparation: Internal Release 3.3.0 Windows ARM64.
