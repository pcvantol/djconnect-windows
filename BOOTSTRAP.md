# DJConnect Windows Repository Bootstrap

`djconnect-windows` owns the Windows client renderer; it does not own backend
intelligence or central platform governance. Adopted governance is Version 2.2
from `pcvantol/djconnect/docs/governance/PLATFORM_ARCHITECT_SYSTEM_INSTRUCTIONS.md`.

Every prompt starts with `git switch main`, `git pull --ff-only`, clean-tree,
upstream and predecessor-PR verification. Then read `AGENTS.md`, this file,
`ENGINEERING_STATUS.md`, `REPOSITORY_STATUS.md`, `MANAGEMENT_SUMMARY.md`,
`ROADMAP_INDEX.md`, `PROMPT_INDEX.md` and relevant local docs. Reconcile
`MERGED_UNRECONCILED` before work; history is immutable.

Lifecycle states: `LOCAL_IN_PROGRESS`, `REVIEWABLE_FROZEN`,
`MERGED_UNRECONCILED`, `MERGED_RECONCILED`. Verify requested work exists before
implementing. Branch cleanup is fail-closed until merge, archived history,
remote deletion and clean tree are proven.
