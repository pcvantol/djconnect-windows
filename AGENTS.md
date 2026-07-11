# DJConnect Windows Agent Instructions

## DJConnect Platform Bootstrap

For a clean Codex/AI-agent session in this repository, start here:

`BOOTSTRAP_CODEX_SESSION.md`

That local bootstrap points back to the canonical platform foundation in
`pcvantol/djconnect` and then returns to the repository-specific rules in
this file. This repository extends the DJConnect Platform Foundation. It
does not redefine it.

## Role

This repo is the Windows first-party DJConnect intelligence client and renderer.

## Rules

- Windows is a platform renderer, not a separate product model.
- Durable intelligence belongs to the backend.
- Everything personal belongs to a DJConnect Profile.
- Device-local state is limited to Windows runtime/UI/capability concerns.
- Ask DJ, Track Insight, Discover, VibeCast and Music DNA use backend-owned contracts.
- Do not invent local recommendations, profile memory or insight facts that should come from the backend.
