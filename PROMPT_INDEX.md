# DJConnect Repository Prompt Index

Status: repository-local prompt navigation

Repository: `pcvantol/djconnect-windows`

This file tracks only this repository's local phase state. It must not copy the canonical platform roadmap from `pcvantol/djconnect/PROMPT_INDEX.md`.

## Current Repository Phase

Windows runner PowerShell 7 maintenance automation.

## Status

IMPLEMENTATION_IN_PROGRESS

## Depends On

- Canonical Platform Foundation in `pcvantol/djconnect`
- `pcvantol/djconnect/REPOSITORY_OWNERSHIP.md`
- `pcvantol/djconnect/docs/meta/PHASE_COMPLETION_PROTOCOL.md`

## Current Prompt

Install a verified, daily SYSTEM maintenance task that keeps machine-level
PowerShell 7 current for the Windows runner. Do not alter the manifest,
artifact or deployment authorization.

## Completion Report

`docs/history/prompts/2026-07-16-windows-runner-powershell-7-maintenance.md`

## Next Repository Phase

After this increment and the central native-preflight remediation merge, adopt
the immutable action SHA in the Windows consumer, remove the obsolete Bash
prerequisite, then rerun the already authorized deployment and dispatch smoke
only if that deployment succeeds.

## Boundary

Do not add Pi phases, ESP phases, Apple phases, platform verification phases or unrelated repository roadmaps here unless this repository owns that work.
