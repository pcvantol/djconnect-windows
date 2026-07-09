# DJConnect Windows Agent Instructions

## DJConnect Platform Bootstrap

For a clean Codex/AI-agent session, first follow the canonical platform bootstrap:

`pcvantol/djconnect/BOOTSTRAP_CODEX_SESSION.md`

Then continue with the repository-specific instructions in this file.

This repository extends the DJConnect Platform Foundation. It does not redefine it.

This must be additive only. Existing repo-specific AGENTS guidance remains authoritative for implementation details.


This repository follows the canonical DJConnect design foundation in `pcvantol/djconnect`.

Read first:

- `pcvantol/djconnect/DJCONNECT_CONSTITUTION.md`
- `pcvantol/djconnect/PRODUCT_VISION.md`
- `pcvantol/djconnect/DESIGN_PRINCIPLES.md`
- `pcvantol/djconnect/ARCHITECTURE_PRINCIPLES.md`
- `pcvantol/djconnect/SYNC_PROMPTS.md`
- `pcvantol/djconnect/PRODUCT_ROADMAP.md`
- `pcvantol/djconnect/INNOVATION_LAB.md`

## Role

This repo is the Windows first-party DJConnect intelligence client and renderer.

## Rules

- Windows is a platform renderer, not a separate product model.
- Durable intelligence belongs to the backend.
- Everything personal belongs to a DJConnect Profile.
- Device-local state is limited to Windows runtime/UI/capability concerns.
- Ask DJ, Track Insight, Discover, VibeCast and Music DNA use backend-owned contracts.
- Do not invent local recommendations, profile memory or insight facts that should come from the backend.
