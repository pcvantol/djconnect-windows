# Windows Management Summary

**Decision:** `WINDOWS_INTERACTIVE_GUI_SMOKE_RELAY_REVIEWABLE`.

The deployed Windows 3.3.0 artifact is still bound to the approved manifest.
This increment makes GUI smoke executable without weakening the hardened runner:
the service validates and submits only a version-bound request, while a limited
interactive-token scheduled task starts the fixed local GUI relay. Scoped ACLs
prevent the service account from changing the task, script, configuration or
results. Operational qualification awaits PR merge, one local installer run
and a smoke-only rerun against deployment `29583151393`.
