# ADR 0013: Protected licensed SpatialAnalyzer runner

## Status

Accepted for the v0.1 vertical slice on 2026-07-22. Provisioning the organization runner group, repository environment, and dedicated licensed machine remains an explicit administrator action.

## Context

The real-SA smoke test executes repository-produced code on a Windows machine that contains a licensed desktop application. Briosa is public, and GitHub warns that self-hosted runners do not provide ephemeral isolation and can be persistently compromised by untrusted workflow code. Environment approval protects environment secrets, but it does not isolate the runner itself.

The repository currently has no self-hosted runners or deployment environments. A repository-level runner would be available to every eligible workflow in this public repository, including a workflow proposed by an untrusted pull request. The licensed machine therefore needs a narrower boundary than labels alone.

The test payload already exercises one successful, read-only `GetWorkingDirectory` call and redacts the returned value. Portable fake-worker tests remain responsible for failure, hang, crash, and replacement injection.

## Decision

The initial licensed integration environment uses these controls together:

1. Use a dedicated Windows x64 machine or isolated VM with no personal files, developer credentials, general-purpose secrets, or access to unrelated internal systems. Do not enroll a maintainer's everyday workstation.
2. Register the runner at the `spatialanalyzer` organization level in a `briosa-licensed-sa` runner group. Restrict the group to the `spatialanalyzer/briosa` repository and directly to `spatialanalyzer/briosa/.github/workflows/licensed-sa.yml@main`.
3. Give the runner the exact labels `briosa-licensed` and `sa-2026-1-0529-7` in addition to the standard `self-hosted`, `windows`, and `x64` labels. Labels route jobs; the runner-group workflow restriction is the authorization boundary.
4. Protect the job with the `licensed-sa-2026-1-0529-7` GitHub environment. Permit only `main`, require an explicit trusted-maintainer approval, disallow administrator bypass, and store no secrets in the environment. Enable prevent-self-review as soon as a second trusted maintainer exists; with the current single-member organization, enabling it would make the workflow impossible to approve.
5. Trigger the workflow only with `workflow_dispatch`. Both jobs also reject any repository, ref, or event other than `spatialanalyzer/briosa`, `refs/heads/main`, and `workflow_dispatch`. Pull-request, `pull_request_target`, push, workflow-run, repository-dispatch, and caller-supplied ref triggers are absent.
6. Build the package and self-contained generated client on a GitHub-hosted Windows runner. Pin every action in this workflow to a full commit SHA, disable persisted checkout credentials, upload one short-retention payload, and pass its SHA-256 to the protected job.
7. The licensed runner does not check out the repository. It downloads the payload created by the hosted job, verifies the expected hash, and runs the included scripts only after environment approval.
8. Serialize the exact-target workflow without cancelling an in-flight run. Preflight and postflight require exactly one matching SA process and no Briosa or `SpatialAnalyzerSDK` processes. The workflow deletes its temporary payload but never automatically terminates or restarts SpatialAnalyzer.
9. Run the Actions runner interactively in the same dedicated Windows user session as SpatialAnalyzer unless Hexagon confirms a supported unattended/service configuration. Keep the runner offline when licensed validation is not being performed.

GitHub documents that environment protection rules are evaluated before a job is sent to a runner and that organization runner groups can restrict access to selected repositories and selected workflows. Those controls are complementary; neither one replaces the other.

## Scheduling and release validation

The v0.1 workflow is manual. A maintainer starts a clean SA instance, brings the runner online, dispatches the workflow from `main`, verifies the source commit, and approves the protected environment.

Scheduled or release-triggered execution is not enabled until unattended SA startup, license use, first-instance port ownership, and runner quarantine/recovery are proven on the dedicated machine. Any later automation must retain the same runner group, exact workflow restriction, protected environment, trusted `main` source, serialization, and pre/postflight checks. It must never make the licensed runner a pull-request check.

## Recovery

A failed postflight is a runner quarantine signal. Stop the Actions runner before investigating. Close residual Briosa and SDK processes, close every SA instance, start exactly one clean matching SA instance so it can reacquire SDK port ownership, rerun preflight, and only then bring the runner online. Reboot the dedicated machine if process ownership or cleanup remains uncertain.

The workflow reports only process counts and stable issue codes. It does not report process IDs, executable paths, SDK ports, license material, arguments, or returned SpatialAnalyzer values.

## Dependencies and unresolved vendor assumptions

- Hexagon licensing must permit the chosen dedicated hardware or VM, the runner account, and manual or unattended automation. Briosa does not supply or manage that license.
- The project still needs Hexagon guidance on supported integration-test use, VM/remote-session restrictions, activation recovery, and supported legacy-release coverage.
- SA 2026.1.0529.7 must be installed and licensed separately. The workflow does not install, start, license, update, or terminate SpatialAnalyzer.
- The approved SDK surface has no reviewed version query. The exact executable location and package identity remain controlled assumptions, not runtime version discovery.

## Consequences

- Untrusted pull-request code cannot route a job to the licensed runner merely by copying its labels.
- The licensed machine receives a reviewed `main` payload and no repository checkout credentials.
- Ordinary CI and release packaging continue to require no SpatialAnalyzer installation or license.
- A repository administrator and organization owner must provision the environment and restricted runner group before the workflow can run.
- The current single-maintainer organization cannot provide two-person approval. Runner-group selected-workflow access, a manual environment gate, and source-commit review are the minimum controls until a second trusted reviewer is appointed.
- `main` was not branch-protected when this decision was recorded. Branch protection or an equivalent ruleset requiring pull requests, CI, and review should be enabled before the licensed runner is brought online.
- A dedicated licensed Windows environment is an operational cost of the stronger boundary.


## References

- [GitHub secure use reference](https://docs.github.com/en/actions/reference/security/secure-use)
- [Managing access to self-hosted runners using groups](https://docs.github.com/en/actions/how-tos/manage-runners/self-hosted-runners/manage-access)
- [Deployments and environments](https://docs.github.com/en/actions/reference/workflows-and-actions/deployments-and-environments)
- [Using self-hosted runners in a workflow](https://docs.github.com/en/actions/how-tos/manage-runners/self-hosted-runners/use-in-a-workflow)
