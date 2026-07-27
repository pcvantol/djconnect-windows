# Windows Management Summary

**Decision:** `WINDOWS_INTERACTIVE_GUI_SMOKE_RELAY_REVIEWABLE`.

The deployed Windows 3.3.0 artifact is still bound to the approved manifest.
This increment makes GUI smoke executable without weakening the hardened runner:
the service validates and submits only a version-bound request, while a limited
interactive-token scheduled task starts the fixed local GUI relay. Scoped ACLs
prevent the service account from changing the task, script, configuration or
results. Operational qualification awaits PR merge, one local installer run
and a smoke-only rerun against deployment `29583151393`.

## Dependabot Maintenance Status — 2026-07-27

**Decision:** `GO_PLATFORM_DEPENDABOT_MAINTENANCE_COMPLETE`.

The platform-wide Dependabot maintenance round is complete. This repository
merged [#50](https://github.com/pcvantol/djconnect-windows/pull/50) (MAUI
Controls 10.0.80 to 10.0.90) and
[#51](https://github.com/pcvantol/djconnect-windows/pull/51) (ten immutable
GitHub Actions pins after exact-SHA Owner Authorization). A transient Windows
runner timeout was not a NuGet-content defect. Windows product behavior did
not change.

Current GitHub evidence: zero open Dependabot security alerts and zero open
Dependabot pull requests. The canonical platform record is maintained in
`pcvantol/djconnect`.
