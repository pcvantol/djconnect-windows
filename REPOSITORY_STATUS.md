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

Windows native-preflight consumer adoption, reviewable.

## Status

The Windows consumers now pin the immutable central native-preflight action
revision and no longer require Bash/WSL. The authorized operation remains
pending a post-merge rerun.

## Blocking Dependencies

- This consumer-adoption PR must merge before the authorized rerun.
- PowerShell 7 must be visible to the Windows runner service account. The
  maintenance task runs in an interactive administrator context; WinGet is not
  a `SYSTEM` dependency.

## Current Prompt

Windows native-preflight consumer adoption

## Completion Report

`docs/WINDOWS_INTERNAL_DEPLOYMENT.md` and immutable Prompt History.

## Last Qualification

The ARM64 runner qualified successfully in service context. Authorized deployment
run `29482534415` failed before preflight or installation because `pwsh` was not
available to `NETWORK SERVICE`; no artifact or target mutation occurred. After
PR #19, rerun `29483069749` stopped before preflight because the local Execution
Policy blocked the generated GitHub Actions script. A later run reached the
previous shared preflight, where Bash resolved to WSL. The consumer now adopts
the merged native PowerShell 7 preflight; deployment remains pending this PR's
merge and a fresh authorized run.

## Validated Base SHA

`b44c0e5c2f5b4c3cfd4fd1d1d2fcb27e9248bca7`

This records the synchronized main SHA inspected at the start of the Windows
consumer remediation increment.

## Repository-Local Next Action

Review and merge this consumer update. Then rerun the already authorized
manifest-bound deployment, followed by smoke only on success.

## Notes

Current release target under preparation: Internal Release 3.3.0 Windows ARM64.
