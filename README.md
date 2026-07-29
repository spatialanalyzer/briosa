# Briosa

Briosa is an open-source gRPC bridge around the Hexagon SpatialAnalyzer SDK. SpatialAnalyzer must be installed separately before Briosa can perform useful work.

## Current target

The initial vertical slice targets .NET 10 on Windows x64 and SpatialAnalyzer 2026.1.0529.7. Its first public operation is the exact-target `GetWorkingDirectory` RPC.

## Build

Install the SDK selected by `global.json`, then run:

```powershell
dotnet restore Briosa.slnx --locked-mode
dotnet build Briosa.slnx -c Release --no-restore
dotnet test Briosa.slnx -c Release --no-build --no-restore
```

The committed managed interop metadata allows these commands to run on an ordinary Windows x64 machine without installing or starting SpatialAnalyzer.

## Windows package

Release archives are self-contained, non-trimmed Windows x64 directory publishes for one exact SpatialAnalyzer target:

```text
briosa-<briosa-version>-sa-2026.1.0529.7-win-x64.zip
```

Run `./eng/New-WindowsPackage.ps1 -Version 0.1.0` to build an archive, checksum, and provenance manifest locally. Run `./eng/Test-WindowsPackage.ps1 -Version 0.1.0-test` to build twice and verify reproducibility, package checksums, offline diagnostics, and host startup without SpatialAnalyzer.

See the [Windows package operator guide](docs/operations/windows-package.md), [protocol artifact and conformance guide](docs/operations/protocol-artifacts.md), and [package identity decision](docs/architecture/0011-windows-package-identity.md) for prerequisites, verification, defaults, artifact contents, and release behavior.

## Public endpoint security

Briosa v0.1 listens on cleartext HTTP/2 at `127.0.0.1:50051` by default and accepts only IPv4 or IPv6 loopback addresses. LAN, Internet, reverse-proxy, tunnel, shared-host, and other remotely reachable deployments are unsupported: v0.1 has no client authentication, per-operation authorization, or TLS configuration. Generic ASP.NET Core URL and Kestrel endpoint overrides are rejected so they cannot silently widen the listener.

See the [public endpoint operator guide](docs/operations/endpoint-security.md), [v0.1 threat model](docs/security/threat-model.md), and [loopback endpoint decision](docs/architecture/0014-loopback-only-public-endpoint.md) before deploying the server. `Briosa:SpatialAnalyzer:Host` controls the separate outbound SDK target and never changes the public listener.

## Public protocol
## Command policy and auditing

The generated exact-target catalog is the maximum command surface. Runtime exact-ID allow and deny lists reduce that surface, with missing allowlists denying all and deny taking precedence. Policy rejection happens before worker or SDK execution, and capability discovery shows only currently allowed operations.

Structured events correlate the host, policy decision, worker generation, MP outcome, and output-retrieval outcome without accepting arguments or returned values. Enabling verbose logging does not enable value logging.

See the [command policy and auditing guide](docs/operations/command-policy-and-auditing.md), [audit architecture decision](docs/architecture/0015-command-policy-and-audit-events.md), and [v0.1 threat model](docs/security/threat-model.md).


Briosa separates the stable `briosa.core.v1alpha1` package from MP contracts generated for one exact SpatialAnalyzer release, beginning with `briosa.sa.v2026_1_0529_7.v1alpha1`. Target packages are independent, version-faithful APIs; matching command shapes never imply matching semantics across SA releases.

Install Buf 1.72.0 and run `./eng/Verify-Protocol.ps1` to check formatting, lint rules, and schema compilation. Once Briosa has a public release baseline, pass its explicit Git ref with `-AgainstRef`; the evolving unreleased `main` branch is intentionally not treated as a compatibility baseline. The .NET build compiles the reviewed protobuf sources directly.

See [the exact-SA-target protocol decision](docs/architecture/0005-exact-sa-target-protocols.md) for package layout, version coordinates, compatibility, presence, target isolation, and review rules.

Successful MP responses pair exact-target typed values with explicit core execution and output-retrieval details. Failed calls use canonical gRPC statuses and carry a value-free typed error in `briosa-operation-error-bin`. See [the MP outcome and error decision](docs/architecture/0008-mp-outcomes-and-grpc-errors.md) and its [uncertain-completion amendment](docs/architecture/0018-uncertain-completion-and-replay.md). Worker recovery and command replay safety are separate; an ambiguously completed call must not be automatically replayed merely because a replacement worker becomes available.

## Health and discovery

The public host exposes standard gRPC health checks named `briosa.liveness` and `briosa.readiness`. Liveness is independent of SpatialAnalyzer. Readiness requires a control-ready worker, a connected SDK snapshot, and a successful bounded execution-channel probe for the current worker generation. `ConnectEx` success alone remains attached-but-unverified and cannot admit MP work. The Get Working Directory probe discards its returned path inside the worker and exposes only safe verification state. The stable core `DiscoveryService` reports attachment and execution readiness separately, along with safe build coordinates and only operations present in the reviewed exact-target catalog and enabled by runtime policy. See [ADR 0017](docs/architecture/0017-execution-channel-readiness.md).

See the [health and discovery operator guide](docs/operations/health-and-discovery.md) and [architecture decision](docs/architecture/0010-health-version-and-capability-discovery.md) for service names, response semantics, connected-version verification, and the information boundary.

## Supported command catalog

The `catalog` directory is the reviewed, machine-readable allowlist of MP operations Briosa exposes for each exact SpatialAnalyzer target. It is deliberately separate from the complete installed SA inventory: catalog absence means an operation is not exposed by Briosa, not that SA lacks it.

The `inventory` directory contains deterministic derived facts from locally installed MP documentation and **View SDK Code** exports. It preserves missing and conflicting metadata for review without committing vendor source material or making an operation public. See [the extraction guide](docs/development/mp-command-inventory.md) for inputs, provenance, regeneration, and the intellectual-property boundary.

The `disposition` directory accounts for every exact-target inventory key without making all of them public. Category-sharded decisions record approved candidates, intentional exclusions, SDK-unavailable operations, and named blockers. Evidence fingerprints force command-scoped re-review when extracted facts change. See [the disposition review guide](docs/development/command-dispositions.md) for decision fields, review states, delivery waves, and promotion rules.

Run `./eng/Verify-Catalog.ps1` to validate JSON structure, target and path identity, deterministic protocol names, argument direction, reviewed input omission/default behavior, evidence references, risk metadata, and private SDK setter/getter availability. Validation requires neither SpatialAnalyzer nor the local vendor evidence corpus.

Run `./eng/Verify-Disposition.ps1` to validate complete inventory coverage, evidence identity, review-state semantics, deterministic category shards, and the generated disposition report. New and changed commands fail closed until reviewed.

Run `./eng/Verify-BindingRegistry.ps1` to reconcile every inventory-observed SDK setter/getter with the committed exact-target interop API, reviewed semantic value family, public/private type targets, and protocol/worker/adapter/fake/generator coverage. The worker test suite then table-drives every usable method/family row through the private control protocol and exact adapter seam, including negative paths. Inventory-only methods remain explicitly blocked. See the [SA 2026.1.0529.7 completeness reference](docs/reference/sa/2026.1.0529.7/binding-family-completeness.md).

Run `./eng/Verify-ValueFamilyEvidence.ps1` to validate the exact-target enum literals, structured fields, source fingerprints, and all multi-domain SDK-method assignments, and to reject stale or nondeterministic generated evidence artifacts. See the [value-family evidence guide](docs/development/value-family-evidence.md) before changing these mappings.

Run `./eng/Verify-CatalogScaffolds.ps1` to generate and schema-check the incomplete review queue twice. Scaffolds account for every reviewed approved candidate not already in the supported catalog, consume exact per-occurrence value-family assignments, retain explicit public-policy blockers, and never write into `catalog`. See the [catalog review-scaffold guide](docs/development/catalog-review-scaffolds.md) for incremental conflict handling and the manual promotion checklist.

For SA `2026.1.0529.7`, see the [intentional-exclusion policy](docs/reference/sa/2026.1.0529.7/intentional-exclusions.md) and the generated [command-level disposition report](disposition/sa/2026.1.0529.7/report.md).

Run `dotnet run --project tools/Briosa.Generator -c Release -- catalog-generate catalog .` to regenerate exact-target protobuf, server bindings, reference documentation, and coverage manifests. Never edit those artifacts by hand. `./eng/Verify-CatalogArtifacts.ps1` performs a clean generation and fails on content or file-list drift.

Release-aligned client generation uses the deterministic protocol artifact described in [the protocol artifact and conformance guide](docs/operations/protocol-artifacts.md). It packages canonical protobuf sources, a descriptor set, exact catalog identity, and shared value-safe fixtures for all thin-client repositories. Produce it with `./eng/New-ProtocolArtifact.ps1` and verify its byte identity, manifests, checksums, descriptor, and fixtures with `./eng/Test-ProtocolArtifact.ps1`; neither command requires SpatialAnalyzer or an SA license.

See [the Get Working Directory vertical-slice decision](docs/architecture/0007-get-working-directory-vertical-slice.md) for the generated and hand-written boundaries, exact SDK sequence, and failure behavior.

See [the supported-command catalog decision](docs/architecture/0006-versioned-command-catalog.md) for the inventory boundary, schema, naming, review, and release rules. See [the catalog-derived artifact decision](docs/architecture/0009-catalog-derived-operation-artifacts.md) for generated adapter responsibilities, exact binding enforcement, documentation, drift checks, and completeness markers.

## SpatialAnalyzer interop

Only the worker boundary references the generated COM metadata. Original SpatialAnalyzer binaries and type libraries are not part of this repository.

See [the interop generation guide](docs/development/interop-generation.md) and [the COM boundary architecture decision](docs/architecture/0001-spatialanalyzer-com-boundary.md) for generation, provenance, redistribution, architecture, and STA rules.

## Portable SDK tests

The [fake SDK and contract-test harness](docs/testing/fake-sdk-harness.md) verifies Briosa's lifecycle, serialization, result handling, and recovery seams without installing or licensing SpatialAnalyzer. The scripted fake tests Briosa contracts and is not a SpatialAnalyzer emulator.

The [generated-client smoke guide](docs/testing/generated-client-smoke.md) covers portable packaged-host scenarios and the explicit licensed-SA vertical-slice test. Both use a separate generated client process and redact the returned working-directory value.

The [licensed runner operations guide](docs/operations/licensed-sa-runner.md) defines the dedicated-machine, organization runner-group, protected-environment, trusted-payload, and recovery requirements for real-SA validation. Never attach a repository-level self-hosted runner or a personal workstation to this public repository.

## Worker process lifecycle

The gRPC host supervises SpatialAnalyzer automation in a disposable child worker over a private named pipe. It reports explicit lifecycle snapshots, replaces hung or crashed workers within a bounded restart window, and escalates failed graceful shutdown to process-tree termination.

The host expects `Briosa.Worker.exe` beside the server by default. Development or packaged layouts can set `Briosa__Worker__ExecutablePath` to an explicit worker path. A missing worker degrades SDK readiness without terminating the public host.

Each worker reports SDK attachment as `Disconnected`, `Connecting`, `Connected`, `Faulted`, or `Stopping` and reports execution verification as `Unverified`, `Verifying`, `ExecutionReady`, `CompetingClientSuspected`, or `OperatorRecoveryRequired`. These states remain independent from process readiness. The SpatialAnalyzer target defaults to `localhost`; set `Briosa__SpatialAnalyzer__Host` to an explicit hostname or IP address. Production currently makes one `ConnectEx` attempt because no status code has a reviewed transient classification. MP work is rejected with a stable unavailable outcome until execution is verified.

A verification timeout, cancellation, worker exit, or lost control response terminates the affected worker and quarantines the target without entering the automatic restart loop. After establishing a clean SpatialAnalyzer/SDK state, restart Briosa to perform the explicit recovery cycle. The supervisor also exposes the same explicit recovery transition internally for controlled hosting and portable tests.

The queue serializes one complete MP sequence at a time; it does not isolate application-global state across multiple RPCs from different callers. The initial deployment contract is single-tenant per worker/SpatialAnalyzer target. Commands requiring exclusive multi-call ownership remain blocked pending a later lease design; see [ADR 0019](docs/architecture/0019-global-state-workflow-isolation.md).

See [the worker process lifecycle decision](docs/architecture/0002-worker-process-lifecycle.md), [the SDK connection lifecycle decision](docs/architecture/0003-sdk-connection-lifecycle.md), [the execution-readiness amendment](docs/architecture/0017-execution-channel-readiness.md), and [the MP execution pipeline decision](docs/architecture/0004-mp-execution-pipeline.md) for protocol, connection ownership, serialization, deadlines, recovery, security, and STA details.

## License

Briosa is licensed under Apache-2.0. SpatialAnalyzer, the SA SDK, their brands, and their proprietary implementation remain Hexagon intellectual property. This project does not imply Hexagon affiliation, endorsement, or support.
