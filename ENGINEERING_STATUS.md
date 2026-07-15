# Windows Engineering Status

**State:** `LOCAL_IN_PROGRESS` — Windows Internal Deployment Consumer Remediation.

The Version 2.2 governance adoption was merged as PR #17. This increment replaces the static fail-closed Windows deployment and smoke placeholders with a manifest-bound internal ARM64 consumer. It does not dispatch a release.

**Blockers/limitations:** the only registered self-hosted Windows runner, `djconnect-windows11-parallels`, reports `X64`; a genuine ARM64 runner and the documented `windows-internal-deployment` environment variable are required before operational qualification. Windows also lacks an explicit current manifest target authorization.

**Deferred work:** target configuration, explicit authorization and the manifest-bound deployment plus separate smoke operation.

**Recommended next prompt:** after this PR merges, configure a genuine Windows ARM64 runner and the documented Environment variable, then obtain exact Windows target authorization before dispatch.
