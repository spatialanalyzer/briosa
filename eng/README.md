# Repository engineering checks

This directory contains repository-level policy checks. Product build, test, packaging, interop, and licensed-validation scripts live inside each `targets/<exact-sa-release>/eng` directory.

- `Verify-CiWorkflow.ps1` protects ordinary CI triggers and permissions.
- `Verify-LicensedRunnerWorkflow.ps1` protects the licensed-runner trust boundary.
- `Verify-TargetIsolation.ps1` verifies target ownership, stable public protobuf identities, target-local project/source references, and CI/release enumeration.
- `Test-CrossTargetPropagation.ps1` compares committed blobs between `-BaseRef` (default: merge base with `origin/main`) and `-HeadRef` and reports twin files that matched in both targets at the base, allowing only target-version differences, but changed on one side or no longer match. It is report-only unless `-Strict`; `-AllowPath` accepts target-relative wildcard patterns for reviewed divergences.
- `Test-CrossTargetPropagationPolicy.ps1` exercises the propagation report against a temporary two-target Git repository.
- `Resolve-ReleaseVersion.ps1` resolves the release version, main-ancestry signing eligibility, and the protocol baseline for `release.yml`. The version must exceed every other stable `v*` tag in the repository; the baseline is the highest stable tag reachable from the released commit, excluding only the tag being released.
- `Test-ResolveReleaseVersionPolicy.ps1` exercises release version resolution against a temporary Git repository.

The [GitHub security and CI baseline](security-baseline.md) describes security
scans, dependency updates, required checks and the existing-tool coverage reports.
