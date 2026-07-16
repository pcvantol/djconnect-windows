# DJConnect Repository Prompt Index

Status: repository-local prompt navigation

Repository: `pcvantol/djconnect-windows`

This file tracks only this repository's local phase state. It must not copy the canonical platform roadmap from `pcvantol/djconnect/PROMPT_INDEX.md`.

## Current Repository Phase

Windows internal deployment execution-policy remediation.

## Status

IMPLEMENTATION_IN_PROGRESS

## Depends On

- Canonical Platform Foundation in `pcvantol/djconnect`
- `pcvantol/djconnect/REPOSITORY_OWNERSHIP.md`
- `pcvantol/djconnect/docs/meta/PHASE_COMPLETION_PROTOCOL.md`

## Current Prompt

Make the existing manifest-bound Windows deployment and smoke workflows executable by the `NETWORK SERVICE` runner account after rerun `29483069749` stopped before preflight because the machine PowerShell Execution Policy blocked generated runner scripts. Do not alter the manifest, artifact or deployment authorization.

## Completion Report

`docs/history/prompts/2026-07-16-windows-internal-deployment-execution-policy-remediation.md`

## Next Repository Phase

After this increment is merged, rerun the already authorized manifest-bound deployment and dispatch smoke only if that deployment succeeds.

## Boundary

Do not add Pi phases, ESP phases, Apple phases, platform verification phases or unrelated repository roadmaps here unless this repository owns that work.
