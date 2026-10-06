# Briosa agent guide

This file is the canonical repository-level guidance for automated coding agents and human contributors working in `spatialanalyzer/briosa`. Read it before changing code, build infrastructure, schemas, documentation, or repository settings.

## Project purpose

Briosa is an open-source gRPC bridge around the Hexagon SpatialAnalyzer SDK. It exposes SpatialAnalyzer MP functions through a language-neutral protocol. Separate repositories provide thin, idiomatic clients such as `briosa-dotnet`, `briosa-js`, and `briosa-py`.

Briosa is not a replacement for SpatialAnalyzer. A user must separately install, run, and hold a valid license for SpatialAnalyzer before Briosa can perform useful work.

The repository is licensed under Apache-2.0. SpatialAnalyzer, the SA SDK, their brands, proprietary binaries, and proprietary implementation remain Hexagon intellectual property. Do not imply ownership, affiliation, endorsement, or support beyond what is expressly documented by the project governance repository.

## Repository relationships

- `spatialanalyzer/briosa`: gRPC server, SDK worker, protocol definitions, handwritten MP operations, reference evidence, and server tests.
- `spatialanalyzer/briosa-*`: language-specific thin clients.
- `spatialanalyzer/community`: organization Discussions. Architecture exploration is recorded in [Discussion #1](https://github.com/orgs/spatialanalyzer/discussions/1).
- `spatialanalyzer/governance`: organization and project governance, policies, and unresolved governance questions.
- [Briosa Roadmap & Delivery](https://github.com/orgs/spatialanalyzer/projects/1): cross-repository planning source of truth.

Keep public protocol design in `briosa`; do not let a client repository become the accidental source of truth for shared API semantics.
Keep language-neutral first-party client behavior in
[`docs/architecture/client-library-behavioral-contract.md`](docs/architecture/client-library-behavioral-contract.md).
Client repositories own idiomatic syntax and types, but they must link to that
contract rather than duplicating or silently redefining shared behavior.

## ObjectiveSA parity and at-risk commands

- ObjectiveSA is pinned prior-release secondary evidence, never a substitute for the exact-target SpatialAnalyzer inventory or interop surface.
- When an ObjectiveSA method has the same MP step and the complete input/output bindings agree with committed exact-target evidence, maintainers may use that match when reviewing a handwritten Briosa implementation without a separate command-by-command permission request.
- Missing local fixtures, third-party software, hardware, or licenses make an otherwise matched operation an **at-risk candidate**. They do not by themselves justify an intentional exclusion.
- Record at-risk validation gaps explicitly and add an opt-in licensed integration scenario. Do not represent an unavailable fixture or an unexecuted scenario as a passing test.
- When exact ObjectiveSA parity is absent and the desired disposition remains uncertain, ask the maintainer before excluding or promoting the command.
- Exact-target evidence still wins on any conflict. Do not use ObjectiveSA to add a binding, choice, default, or compatibility claim absent from the targeted SA release.

## Established technical facts

- The SA SDK is an OLE Automation/DCOM server exposed by `SpatialAnalyzerSDK.exe`. The target is .NET 10 on Windows x64; SDK automation runs on one worker-owned STA.
- SpatialAnalyzer must already be running for `ConnectEx` to attach. Only the first eligible SA instance owns SDK communication ports; connection success alone does not prove execution readiness. See the [runtime boundary](docs/architecture/runtime-boundary-and-lifecycle.md).
- One running Briosa server is locked to one exact SA release and SDK/SA instance. COM activation and the connected application can have different versions. Preserve configured target, activated SDK, and connected SA as distinct claims; reject verified mismatches. When runtime evidence is unavailable, each version may be independently attested with a non-sensitive reference. Require exact matching claims before the bounded execution-channel probe admits MP work. Public protobuf package and generated C# namespace remain `briosa` and `Briosa`.
- `ExecuteStep` returning true is not MP success. Only then call `GetMPStepResult`; its Boolean is retrieval state, while MP code `2` means command success. Preserve both. Timeout, cancellation, process loss, or a missing response after enqueue can leave completion unknown; replacement does not make replay safe. Never automatically retry ambiguous completion without reviewed exact-operation replay evidence. See [execution outcomes](docs/architecture/execution-outcomes-and-recovery.md).
- Queue serialization prevents COM interleaving but does not isolate application-global state across RPCs. The initial worker/SA target is single-tenant; exclusive multi-call workflows await an accepted lease contract.
- For SDK `ref object` lists, marshal CLR arrays with `VariantWrapper`; a live 2026 probe rejected bare `object[]` for both string-reference list calls. SDK method names alone do not determine value family: collection-object-named calls carry both 26-choice object and 42-choice item domains. Select per exact argument and fail closed on unknown literals.
- The exact `Get Working Frame Properties` operation may supply its documented `Frame` type when the getter omits an embedded type. Do not generalize this fallback. The [exact-target evidence guide](targets/2026.1.0529.7/docs/development/exact-target-evidence.md) holds these observations and the non-authoritative inventory boundary.

See [Discussion #1 findings](https://github.com/spatialanalyzer/community/discussions/1#discussioncomment-17706394) before changing connection, concurrency, timeout, or process-lifecycle assumptions.

## Architectural invariants

Unless an accepted design decision explicitly changes them, preserve these constraints:

1. The public gRPC host must not own COM state directly. A separately supervised worker process owns the SDK client and can be replaced after a hang or crash.
2. One worker owns exactly one active SDK connection.
3. One worker-owned STA serializes the entire MP sequence: `SetStep`, argument setters, `ExecuteStep`, and result retrieval. Never interleave sequences from concurrent gRPC calls.
4. Client cancellation and gRPC deadlines must not be confused with successful cancellation of an in-flight COM call. A watchdog may need to terminate and replace the worker.
5. Public protobuf contracts must describe SpatialAnalyzer concepts without exposing COM implementation types.
6. Each supported MP operation is a complete reviewed vertical slice: a mechanically MP-compatible strongly typed protobuf RPC, handwritten C# host/worker/SDK mapping, capability registration, portable tests, validation status, and user documentation. Coherent issues and pull requests may contain multiple such operations; there is no arbitrary one-command boundary or fixed batch maximum. Standard protobuf/gRPC generation is allowed; Briosa-specific operation generation and inventory-completeness gates are not.
7. Ordinary builds and tests must not require SpatialAnalyzer, a license, or proprietary SDK binaries. Put the SDK behind an internal abstraction and exercise lifecycle and failure behavior with a fake.
8. Real-SA integration tests require a separately licensed, protected Windows environment. Never expose such a runner or its secrets to untrusted pull-request code.
9. Bind public services to loopback by default until remote authentication, transport security, authorization, and command-risk policies are established.
10. Log operation identity, timing, connection state, and outcomes, but do not log geometry, paths, credentials, proprietary data, or raw arguments by default.
11. Do not report MP readiness from `ConnectEx` alone. Readiness requires exact-match activated-SDK and connected-SA evidence followed by a bounded execution-channel proof for the current worker generation. Never issue the probe while either identity claim is unavailable or mismatched.
12. Preserve whether execution definitely did not start, may have started with an unknown outcome, or completed. Recovery guidance and replay safety are independent decisions.
13. Treat the initial worker/SA target as single-tenant. Do not describe queue serialization as cross-client workflow isolation or expose an exclusive multi-call workflow without an accepted lease/session design.
14. Keep each supported SA release as a complete product under `targets/<exact-sa-release>/`. Target projects must not reference projects or runtime source from another target. Keep the public protobuf package stable as `briosa`; exact SA releases identify products, artifacts, packages, and runtime compatibility gates.

## Interop and intellectual-property boundary

- Build only against interfaces made available through a properly installed and licensed SA SDK/type library.
- Do not copy SpatialAnalyzer source, decompile proprietary implementation, or commit/publish Hexagon binaries.
- Keep generated interop provenance and the generation procedure explicit and reproducible.
- Before distributing generated interface assemblies or extracted vendor documentation, confirm that the planned artifact and redistribution terms are covered by documented project approval. When uncertain, stop and request maintainer/Hexagon focal guidance.
- Treat installed MP documentation as input evidence. Do not republish vendor text wholesale; curate only the facts needed to implement, test, and document Briosa behavior.

## Work planning and Git workflow

GitHub issues and the organization Project are the source of truth. Start from
a Task on a short-lived `<issue-number>-<short-description>` branch; keep each
PR coherent, buildable, and reviewable. A PR closes an issue only when its
acceptance criteria are met; otherwise use `Refs`. Keep `main` buildable.

Batch related MP operations when it improves review or validation, without a
fixed command limit. Keep each operation traceable to exact-target evidence and
complete within the batch. Do not silently settle unresolved policy or
opportunistically implement unrelated roadmap work. Run builds and tests from
each target directory; update explicit CI, release, and protected validation
matrices when adding a target.

## Design and implementation expectations

Favor explicit state machines and typed outcomes. Keep transport status,
worker availability, execution disposition, replay safety, and MP result
separate. Make process ownership, COM lifetime, queueing, timeouts, retries,
and cleanup observable and testable.

Preserve MP terminology in RPCs and fields. Hand-author protobuf, host/worker
mapping, SDK sequence, registration, tests, and documentation for every
supported operation. Standard protobuf/gRPC generation is allowed; do not
hand-edit its output or add Briosa-specific operation generation. Generative-AI
drafts require reviewed exact-target evidence and committed source/tests.

Include negative paths for disconnection, MP failure, deadline, cancellation,
worker hang/crash, and unsupported versions. Explain constraints rooted in
observed SDK behavior rather than presenting observations as vendor guarantees.

## Validation levels

Use the least privileged environment that proves the change:

1. Formatting, static analysis, and protobuf validation.
2. Unit and contract tests against the fake SDK.
3. Process-level tests using fake delay, hang, crash, and malformed-result behaviors.
4. Standard generated-client/server smoke tests that do not require SA where possible.
5. Explicitly authorized tests against a licensed SpatialAnalyzer installation.

Before controlling a desktop SpatialAnalyzer process, connecting to another host, changing firewall settings, or running a licensed integration environment, obtain explicit permission for the current task. Avoid attaching multiple experimental SDK clients to the same SA instance. A blocked client can leave connections behind and may require a clean SA restart.

## Important unresolved decisions

Do not treat these as settled:

- Which SpatialAnalyzer releases will be supported and for how long.
- The authoritative command metadata Hexagon can provide and what derived artifacts may be redistributed.
- Remote gRPC authentication, authorization, TLS, network topology, and command-risk policy.
- The protected runner and SpatialAnalyzer license strategy for real integration tests.
- Whether contributions will use a Developer Certificate of Origin or another contribution mechanism; DCO is currently the likely direction.
- Long-term ownership of infrastructure costs and formal corporate stewardship.

When work encounters one of these questions, implement only a reversible minimum if the active issue allows it, document the assumption, and escalate the decision instead of presenting it as established policy.

## Current initial target

The current baseline product is `targets/2026.1.0529.7`. It provides a production-shaped .NET 10 foundation with one supervised, serialized SDK connection, handwritten read-only and state-mutating MP operations, standard generated-client smoke coverage, and safe diagnostics. At Server 0.9.0 the 2026 target registers 1,027 handwritten operations and the 2024 target registers 996. Runtime admission comes from each target's reviewed classification table (`Security/OperationClassification.cs`) through a named profile, flags, and per-operation overrides; the packaged `standard` profile admits a reviewed subset, and no setting admits an `ExclusiveWorkflow` operation. See the [admission profile migration](docs/development/admission-profile-migration.md). The handwritten protobuf contracts, `SpatialAnalyzerApi.Operations`, and runtime capability discovery—not a prose count or inventory—define the supported surface.

`targets/2024.1.0508.5` is the independent legacy product established by the completed command-difference review. Six read-only operations and basic lifecycle behavior have local licensed 2024 validation; broader runtime coverage remains outstanding. Preserve its exact-target argument and choice differences, and do not transfer 2026 runtime observations into its validation record. See its [compatibility record](targets/2024.1.0508.5/docs/development/sa2024-compatibility.md).

Add operations through coherent implementation issues and pull requests, batching related commands when useful. Reference inventory, bindings, values, or ObjectiveSA wrappers may accelerate review, but none is an implementation queue, public allowlist, or completeness requirement.
