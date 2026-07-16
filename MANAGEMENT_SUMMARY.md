# Windows Management Summary

**Decision:** `WINDOWS_NATIVE_PREFLIGHT_CONSUMER_ADOPTION_REVIEWABLE`.

The manifest-bound ARM64 consumer, genuine Windows-on-ARM service runner, Environment configuration and exact target authorization are in place. The central shared preflight is now natively PowerShell 7 on Windows. This increment pins its immutable merged SHA in the deployment and smoke consumers and removes the obsolete consumer-level Bash/WSL prerequisite. Existing PowerShell workflow steps retain their scoped execution-policy bypass. No artifact, target, manifest or deployment authorization changes are included. The next action remains a separately dispatched deployment, then smoke only after deployment success.
