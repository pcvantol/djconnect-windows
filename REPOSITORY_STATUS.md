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

Windows Internal Deployment Consumer Remediation.

## Status

Static implementation in progress; operational deployment remains blocked.

## Blocking Dependencies

- A genuine self-hosted Windows ARM64 runner is not currently registered.
- `windows-internal-deployment` needs the documented absolute installation-root
  configuration variable.
- The current operational manifest has no separately authorized Windows target
  operation.

## Current Prompt

Windows internal deployment consumer remediation

## Completion Report

`docs/WINDOWS_INTERNAL_DEPLOYMENT.md` and immutable Prompt History.

## Last Qualification

The prior static entrypoints were validated as fail-closed only. Operational
deployment and post-deployment smoke have not been executed.

## Validated Base SHA

`b44c0e5c2f5b4c3cfd4fd1d1d2fcb27e9248bca7`

This records the synchronized main SHA inspected at the start of the Windows
consumer remediation increment.

## Repository-Local Next Action

Review and merge the remediation PR. Then configure the ARM64 execution target
and obtain a separately authorized manifest-bound deployment operation.

## Notes

Current release target under preparation: Internal Release 3.3.0 Windows ARM64.
