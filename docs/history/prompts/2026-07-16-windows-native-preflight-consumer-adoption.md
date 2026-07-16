# Windows Native-Preflight Consumer Adoption

- **Prompt ID:** `WINDOWS-RELEASE-3.3-NATIVE-PREFLIGHT-CONSUMER-ADOPTION`
- **Title:** Adopt the merged native deployment-readiness preflight in the Windows consumer
- **Generation:** 2
- **Engineering program:** Platform Release Engineering
- **Branch:** `codex/windows-native-preflight-adoption`
- **Commit SHA:** `c04bb48e2ce645e6fa1498f1094f0fd2290212d3`
- **Pull request:** Reviewable pull request created from this branch
- **Decision:** `WINDOWS_NATIVE_PREFLIGHT_CONSUMER_ADOPTION_REVIEWABLE`

## Validation summary

The central native deployment-readiness action was merged on `main` at
`3d7d24a84b3aaacb8f2fb229e09c33da85e0545d`. Its Windows path invokes
PowerShell 7 (`pwsh`), while non-Windows runners use Bash. The Windows
deployment and smoke workflows now pin exactly that immutable revision and no
longer probe for Bash. Static YAML and action-reference validation verify both
consumers retain the canonical target, immutable identity inputs and distinct
deployment/smoke workflow boundaries.

No deployment, artifact download, target mutation or smoke dispatch occurs in
this increment.

## Created artifacts

- This immutable Prompt History record.

## Updated artifacts

- Windows deployment and smoke workflow action pins.
- Windows deployment documentation.
- Rolling engineering, repository, management, roadmap and prompt records.

## Known limitations

- The Windows runner service must expose `pwsh`; the canonical preflight fails
  closed when it is absent.
- The authorized Windows deployment has not been rerun by this increment.

## Deferred work

- After this PR merges, dispatch the already authorized Windows deployment
  with the exact manifest identity.
- Dispatch post-deployment smoke only after a successful deployment run.

## Recommended next prompt

Platform Release Engineering: qualify the already authorized Windows
manifest-bound deployment and, only after it succeeds, post-deployment smoke.
