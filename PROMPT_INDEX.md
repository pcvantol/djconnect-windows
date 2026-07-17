# DJConnect Repository Prompt Index

Status: repository-local prompt navigation

Repository: `pcvantol/djconnect-windows`

This file tracks only this repository's local phase state. It must not copy the canonical platform roadmap from `pcvantol/djconnect/PROMPT_INDEX.md`.

## Current Repository Phase

Windows interactive GUI smoke relay.

## Status

REVIEWABLE_FROZEN

## Depends On

- Canonical Platform Foundation in `pcvantol/djconnect`
- `pcvantol/djconnect/REPOSITORY_OWNERSHIP.md`
- `pcvantol/djconnect/docs/meta/PHASE_COMPLETION_PROTOCOL.md`

## Previous Prompt

Windows smoke service diagnostics is completed, merged and reconciled by PR
[#27](https://github.com/pcvantol/djconnect-windows/pull/27). Its retained
evidence confirms that the service runner launches GUI software in session 0;
that cannot qualify interactive application startup.

## Completion Report

`docs/history/prompts/2026-07-17-windows-smoke-service-diagnostics.md`

## Current Prompt

Implement a least-privilege interactive-session relay for Windows GUI smoke.
The hardened service runner may validate and submit a correlation-bound request
only; it must not start the GUI. The interactive task must run as a limited
named user with no stored password and scoped request/result ACLs.

## Next Repository Phase

After this increment merges and is installed, rerun the already authorized
smoke against deployment run `29583151393`; do not redeploy the unchanged
artifact.

## Boundary

Do not add Pi phases, ESP phases, Apple phases, platform verification phases or unrelated repository roadmaps here unless this repository owns that work.
