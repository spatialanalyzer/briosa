# GitHub security and CI baseline

The public `spatialanalyzer/briosa` repository uses GitHub's included security
features and existing build tools. No third-party scanning subscription, coverage
service, paid GitHub security upgrade, or metered AI feature is required.

## Repository settings

Enable dependency alerts, Dependabot security updates, secret scanning and secret
push protection. Enable automatic dependency submission on standard GitHub-hosted
runners so the dependency graph includes centrally managed NuGet dependencies.
The `nuget` entries in `dependabot.yml` trigger GitHub's managed .NET submission.
`EnableWindowsTargeting` permits its Linux runner to restore Windows reference
packs; supported product execution and ordinary CI remain Windows x64.
The root `DependencySubmission.sln` is a restore-only compatibility entry point
for GitHub's scanner, which currently ignores `.slnx` and stops after 20 files.
It lists both products without adding any cross-target project references.
The repository policy check verifies its membership against the target `.slnx`
files. When adding a project, also use `dotnet sln DependencySubmission.sln add
--solution-folder <exact-sa-release> <project-path>`. Continue building and testing
each product from its own target directory.
NuGet Audit also checks resolved direct and transitive dependencies
at restore time, with warnings treated as errors. Repository settings live in
GitHub rather than this checkout.

`global.json` pins SDK 10.0.401, including runtime 10.0.12. This replaces the
10.0.10 runtime that the initial graph identified as affected by Microsoft's
[.NET runtime advisory](https://github.com/advisories/GHSA-c494-m2fq-59mx) and
[Windows Desktop advisory](https://github.com/advisories/GHSA-jqhp-238x-qhgf).
Install the pinned SDK when updating a development or licensed-validation host.
Previously published self-contained packages need rebuilding and republishing
before they contain the patched runtime; changing this repository does not patch
already installed distributions.

The repository-specific ruleset requires the validated ordinary CI jobs,
dependency review and all four CodeQL jobs. Code scanning additionally blocks new
high/critical security findings. It supplements the inherited organization rule;
it does not modify protection for other repositories.

## Pull requests and default branch

- `ci.yml` builds and tests each exact-SA product independently. On pull requests,
  Buf compares that target's protocol against the PR base commit. Main-branch
  runs still format, lint and build the protocol.
- The existing Coverlet collector emits Cobertura XML alongside TRX results.
  The job summary unions measured source lines across test assemblies per target,
  and the `test-results-sa-<release>` artifact retains reports for seven days.
  The summary fails if Desktop, Server, Worker or Worker.Control has no measured
  lines, catching silent collector failures without imposing a percentage target.
  Generated protobuf code and test helpers are excluded. Coverage measures code
  loaded in test processes: it does not measure child-worker execution, desktop
  smoke processes or licensed SA integration. Unreported assemblies are not
  assumed to be covered. There is no arbitrary percentage gate.
- `codeql.yml` manually builds each target's C# solution and separately analyzes
  Python and Actions. Analysis runs on PRs, main pushes and a weekly schedule.
  Distinct C# categories preserve results for both independently shipped targets.
- `dependency-review.yml` rejects dependency changes with known high/critical
  vulnerabilities. It requires no repository checkout or PR comment permission.
  A license allowlist is not imposed; adopting one requires a project decision.
- Dependabot checks weekly for Actions, each target's NuGet packages, and the
  Python publishing requirements. Routine minor/patch updates are grouped;
  NuGet security fixes have separate groups. Maintainers review and merge updates.
- Third-party Actions are pinned to immutable commit SHAs, enforced by the existing
  workflow policy check. Dependabot maintains these pins.

PR checks run only on standard GitHub-hosted runners. CodeQL's scoped
`security-events: write` permission uploads analysis; ordinary CI and dependency
review retain read-only contents access. Do not use `pull_request_target` to build
untrusted contributions or expose credentials to fork code. Existing manually
dispatched licensed-SA workflows and protected environments remain separate.

## Local checks

Run from either `targets/<exact-sa-release>` directory:

```powershell
dotnet restore Briosa.slnx --locked-mode
dotnet build Briosa.slnx -c Release --no-restore
dotnet test Briosa.slnx -c Release --no-build --no-restore --collect:"XPlat Code Coverage" --settings ../../eng/coverage.runsettings --results-directory artifacts/test-results --logger trx
../../eng/Write-CoverageSummary.ps1 -ResultsDirectory artifacts/test-results
./eng/Verify-Protocol.ps1 -AgainstRef origin/main
```

Treat security findings as issues to investigate, not proof of exploitability or
proof of safety. Static scans do not establish SDK semantics, COM ownership,
execution recovery correctness or licensed validation status. Those remain covered
by the existing focused tests, exact-target evidence and maintainer review.
