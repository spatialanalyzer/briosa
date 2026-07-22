# Licensed SpatialAnalyzer runner operations

The protected workflow validates the packaged vertical slice against one separately licensed SpatialAnalyzer 2026.1.0529.7 x64 instance. It is not an ordinary pull-request check and must not run on a personal development workstation.

Read [ADR 0013](../architecture/0013-protected-licensed-runner.md) before provisioning or changing the runner. The trust boundary depends on the runner group, selected-workflow policy, environment, workflow triggers, and dedicated machine being configured together.

## Required GitHub configuration

An organization owner must create an organization runner group with these exact settings:

| Setting | Required value |
| --- | --- |
| Runner group | `briosa-licensed-sa` |
| Repository access | Selected repositories: `spatialanalyzer/briosa` only |
| Workflow access | Selected workflows: `spatialanalyzer/briosa/.github/workflows/licensed-sa.yml@main` only |

A repository administrator must create an environment with these settings:

| Setting | Required value |
| --- | --- |
| Environment | `licensed-sa-2026-1-0529-7` |
| Deployment branches | Selected branch: `main` only |
| Required reviewers | The trusted maintainer; add a second reviewer when available |
| Prevent self-review | Enable when a second trusted maintainer exists |
| Administrator bypass | Disabled |
| Environment secrets | None |

The workflow intentionally remains queued if the group has no matching online runner. Do not work around that state by adding a repository-level runner or broadening workflow access.

Before bringing the runner online, protect `main` with a branch ruleset that requires pull requests, CI, and review and rejects force pushes and deletion. At the time this guide was written, `main` was not protected and the organization had only one visible member. The environment must therefore require an explicit manual approval, but it cannot enforce two-person review until another trusted maintainer is appointed.

## Dedicated machine

Use a dedicated Windows x64 machine or isolated VM containing no personal data, source-control credentials, signing keys, cloud credentials, or access to unrelated networks. Confirm with the Hexagon licensing focal that the selected hardware, virtualization, remote-session behavior, and automation pattern are permitted before activation.

Install SpatialAnalyzer 2026.1.0529.7 x64 and activate its separate license. Register the GitHub Actions runner at the organization level in `briosa-licensed-sa` with these labels:

```text
self-hosted,windows,x64,briosa-licensed,sa-2026-1-0529-7
```

Run the Actions runner interactively under the same dedicated Windows account and desktop session as SpatialAnalyzer. Do not configure it as a Windows service until an equivalent, supported SDK connection has been demonstrated. Keep the runner application offline except during licensed validation.

The runner requires outbound access to GitHub Actions and artifact endpoints. Briosa and the SDK use only the local SA instance during this test. Do not add repository or environment secrets; the generated `GITHUB_TOKEN` is read-only.

## Manual validation

1. Stop the Actions runner if it is online.
2. Close every SpatialAnalyzer instance, Briosa server or worker, and `SpatialAnalyzerSDK` process.
3. Start exactly one SpatialAnalyzer 2026.1.0529.7 x64 instance and wait until it is ready. This clean sequence lets the first eligible instance acquire the observed SDK ports.
4. Start the Actions runner interactively in the same dedicated session.
5. In GitHub Actions, select **Licensed SpatialAnalyzer validation**, choose `main`, and dispatch the workflow.
6. Review the resolved source commit and hosted `prepare` job before approving the `licensed-sa-2026-1-0529-7` environment.
7. Confirm that preflight, the generated-client smoke test, postflight, and temporary-payload cleanup all pass.
8. Stop the Actions runner when validation is complete.

The hosted job builds the exact-target package and a self-contained generated client, packages only the required test scripts and binaries, and records the trusted source identity. The protected job verifies the payload SHA-256 before executing it and never checks out repository source.

## State checks and diagnostics

The runner state script can be invoked from a trusted local checkout:

```powershell
./eng/Test-LicensedRunnerState.ps1 -Phase Preflight
./eng/Test-LicensedRunnerState.ps1 -Phase Postflight
```

Both phases require:

- 64-bit Windows;
- exactly one process from the expected SA 2026.1.0529.7 x64 installation;
- no `Briosa.Server` or `Briosa.Worker` process; and
- no `SpatialAnalyzerSDK` process.

Output contains counts, a success flag, and stable issue codes only. `EXACT_SA_INSTANCE_COUNT_INVALID`, `RESIDUAL_BRIOSA_PROCESS`, or `RESIDUAL_SDK_PROCESS` means the machine must not accept another licensed run.

## Recovery and quarantine

If preflight or postflight fails:

1. Stop the Actions runner first so it cannot accept another job.
2. Preserve the GitHub run URL and safe issue codes; do not copy local paths, license details, server logs, or returned values into a public issue.
3. Close residual Briosa and SDK processes.
4. Close every SpatialAnalyzer instance. Closing only the first instance does not transfer observed SDK port ownership to an already-open instance.
5. Start exactly one clean matching SA instance and rerun preflight locally.
6. Reboot the dedicated machine if any process, SDK connection, or port ownership remains uncertain.
7. Bring the runner online only after preflight passes.

The workflow deliberately does not kill or restart SpatialAnalyzer. Automatic recovery could destroy unsaved work or conceal an unhealthy licensed environment.

## Scheduling

No schedule is enabled for v0.1. Before adding one, demonstrate supported unattended SA startup and license use, reliable session ownership, automatic quarantine, and operator notification on the dedicated machine. A schedule must continue to use trusted `main`, the same protected environment and selected-workflow runner group, and serialized exact-target execution.
