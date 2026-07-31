# Repository engineering checks

This directory contains repository-level policy checks. Product build, test, packaging, interop, and licensed-validation scripts live inside each `targets/<exact-sa-release>/eng` directory.

- `Verify-CiWorkflow.ps1` protects ordinary CI triggers and permissions.
- `Verify-LicensedRunnerWorkflow.ps1` protects the licensed-runner trust boundary.
- `Verify-TargetIsolation.ps1` verifies target ownership, stable public protobuf identities, target-local project/source references, and CI/release enumeration.
