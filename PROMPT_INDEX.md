# DJConnect Repository Prompt Index

Status: repository-local prompt navigation

Repository: `pcvantol/djconnect-windows`

This file tracks only this repository's local phase state. It must not copy the canonical platform roadmap from `pcvantol/djconnect/PROMPT_INDEX.md`.

## Current Repository Phase

No implementation prompt is active.

## Status

MERGED_RECONCILED

## Depends On

- Canonical Platform Foundation in `pcvantol/djconnect`
- `pcvantol/djconnect/REPOSITORY_OWNERSHIP.md`
- `pcvantol/djconnect/docs/meta/PHASE_COMPLETION_PROTOCOL.md`

## Completed Prompt

Windows smoke service diagnostics is completed, merged and reconciled by PR
[#27](https://github.com/pcvantol/djconnect-windows/pull/27). Its retained
evidence confirms that the service runner launches GUI software in session 0;
that cannot qualify interactive application startup.

## Completion Report

`docs/history/prompts/2026-07-17-windows-smoke-service-diagnostics.md`

## Next Repository Phase

Implement and qualify a least-privilege interactive-session relay for Windows
GUI smoke. Then rerun the already authorized smoke against deployment run
`29583151393`; do not redeploy the unchanged artifact.

## Boundary

Do not add Pi phases, ESP phases, Apple phases, platform verification phases or unrelated repository roadmaps here unless this repository owns that work.
