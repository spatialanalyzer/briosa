# Briosa agent guide

This file is the canonical repository-level guidance for automated coding agents and human contributors working in `spatialanalyzer/briosa`. Read it before changing code, build infrastructure, schemas, documentation, or repository settings.

## Project purpose

Briosa is an open-source gRPC bridge around the Hexagon SpatialAnalyzer SDK. It will expose SpatialAnalyzer MP functions through a clean, language-neutral protocol. Separate repositories provide thin, idiomatic clients such as `briosa-dotnet`, `briosa-js`, and `briosa-py`.

Briosa is not a replacement for SpatialAnalyzer. A user must separately install, run, and hold a valid license for SpatialAnalyzer before Briosa can perform useful work.

The repository is licensed under Apache-2.0. SpatialAnalyzer, the SA SDK, their brands, proprietary binaries, and proprietary implementation remain Hexagon intellectual property. Do not imply ownership, affiliation, endorsement, or support beyond what is expressly documented by the project governance repository.

## Repository relationships

- `spatialanalyzer/briosa`: gRPC server, SDK worker, protocol definitions, handwritten MP operations, reference evidence, and server tests.
- `spatialanalyzer/briosa-*`: language-specific thin clients.
- `spatialanalyzer/community`: organization Discussions. Architecture exploration is recorded in [Discussion #1](https://github.com/orgs/spatialanalyzer/discussions/1).
- `spatialanalyzer/governance`: organization and project governance, policies, and unresolved governance questions.
- [Briosa Roadmap & Delivery](https://github.com/orgs/spatialanalyzer/projects/1): cross-repository planning source of truth.

Keep public protocol design in `briosa`; do not let a client repository become the accidental source of truth for shared API semantics.

## ObjectiveSA parity and at-risk commands

- ObjectiveSA is pinned prior-release secondary evidence, never a substitute for the exact-target SpatialAnalyzer inventory or interop surface.
- When an ObjectiveSA method has the same MP step and the complete input/output bindings agree with committed exact-target evidence, maintainers may use that match when reviewing a handwritten Briosa implementation without a separate command-by-command permission request.
- Missing local fixtures, third-party software, hardware, or licenses make an otherwise matched operation an **at-risk candidate**. They do not by themselves justify an intentional exclusion.
- Record at-risk validation gaps explicitly and add an opt-in licensed integration scenario. Do not represent an unavailable fixture or an unexecuted scenario as a passing test.
- When exact ObjectiveSA parity is absent and the desired disposition remains uncertain, ask the maintainer before excluding or promoting the command.
- Exact-target evidence still wins on any conflict. Do not use ObjectiveSA to add a binding, choice, default, or compatibility claim absent from the targeted SA release.

## Established technical facts

- The SA SDK is an OLE Automation/DCOM server exposed by `SpatialAnalyzerSDK.exe`.
- The initial implementation target is .NET 10 on Windows x64. SDK automation must run on an STA thread.
- SpatialAnalyzer must already be running for `ConnectEx(host, statusCode)` to connect. Use `localhost` for the local application; a reachable remote hostname or IP may also connect.
- When several SpatialAnalyzer instances are open, only the first eligible instance owns the SDK communication ports. Closing it does not transfer ownership to an already-open instance; a newly opened instance must acquire the ports.
- SA 2026.1.0529.7 was observed listening on TCP 901, 902, and 903, with SDK traffic observed on 902. Treat these observations as evidence, not as a vendor-guaranteed protocol contract.
- A machine may have several SpatialAnalyzer releases and matching Briosa distributions installed, but one running Briosa server is locked to one exact SA release and one active SDK/SA instance. A client selects and launches the matching server distribution; SA release identifiers never enter public protobuf package, service, message, or RPC names. The public protobuf package is `briosa` and the generated C# namespace is `Briosa`.
- Multiple SDK clients may report successful connections, but concurrent MP execution is unsafe. Experiments showed the first connected client owning execution while a second client could block indefinitely in `ExecuteStep`. `ConnectEx` success is attachment evidence, not proof of execution readiness; see ADR 0017.
- COM activation can resolve to the SDK engine currently registered on the machine independently from the connected SpatialAnalyzer application version. Preserve the configured target, activated SDK version, and connected SA version as separate claims and fail closed on a verified mismatch.
- Runtime identity evidence takes precedence over operator attestation for each claim. When runtime evidence is unavailable, the activated SDK and connected SA may be attested independently with an explicit version and non-sensitive evidence reference. Both effective claims must exactly match before Briosa issues the execution-channel probe or admits MP work; see ADR 0022.
- A successful `ExecuteStep` return value does not prove that the MP command succeeded. Call `GetMPStepResult` only after `ExecuteStep` returns true. Its Boolean reports whether the result was retrieved; MP result code `2` is the success state. Preserve retrieval state and the raw MP code separately.
- A timeout, cancellation, worker crash, or lost response after enqueue may leave command completion unknown. Worker replacement restores availability; it does not make replay safe. Never automatically retry an ambiguously completed command without reviewed exact-operation replay evidence; see ADR 0018.
- Serializing each MP sequence prevents COM interleaving but does not isolate application-global state across several RPCs. The initial service is single-tenant per worker/SA target, and exclusive multi-call workflows remain blocked until an explicit lease contract exists; see ADR 0019.
- For SDK methods taking a `ref object` list, marshal the CLR array through `System.Runtime.InteropServices.VariantWrapper`. A live SA 2026.1.0529.7 probe observed `DISP_E_TYPEMISMATCH` for a bare `object[]` on both `GetStringRefListArg` and `SetStringRefListArg`; the wrapped forms succeeded.
- SDK method names do not uniquely determine MP argument semantics. In SA 2026.1.0529.7, the collection-object-named scalar/list calls carry both the 26-choice object domain and the broader 42-choice item domain. Select the family per exact command argument and fail closed on unknown returned type literals; see ADR 0016.
- Live SA 2026.1.0529.7 validation of `Get Working Frame Properties` observed `GetCollectionObjectNameArg` returning non-empty collection and object names without an embedded object-type literal. That exact operation supplies its documented `Frame` type only when the literal is omitted; do not generalize this fallback to other collection-object outputs, and continue to fail closed on unknown embedded literals.
- The retained SA 2026.1.0529.7 reference inventory combines 1,302 structured command documents and 1,360 View SDK Code observations into 1,412 commands. It is non-authoritative implementation evidence: inventory membership, former dispositions, and historical catalog membership do not make a command part of the supported Briosa API.

See the [Discussion #1 findings](https://github.com/spatialanalyzer/community/discussions/1#discussioncomment-17706394) before changing connection, concurrency, timeout, or process-lifecycle assumptions.

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

- GitHub issues and the organization Project are the source of truth for planned work.
- Epics are planning containers, not branch boundaries.
- Start from a Task. Use a short-lived branch named `<issue-number>-<short-description>`, such as `7-solution-scaffold`.
- A pull request is the smallest coherent, buildable, reviewable change. A Task may require several PRs, and one PR may close tightly coupled Tasks, but avoid long-lived Epic branches.
- Batch related MP operations when that improves delivery speed, shared workflow validation, or review context. Keep every operation individually traceable to exact-target evidence and individually complete within the batch; do not impose a command-count limit.
- Link PRs with `Closes #<issue-number>` only when the PR satisfies the issue's acceptance criteria. Use `Refs #<issue-number>` for partial work.
- Keep `main` buildable. Prefer squash merges and delete merged branches.
- Do not silently invent policy for an unresolved topic. Record the question in an issue, Discussion, or architecture decision and mark provisional behavior clearly.
- Keep changes scoped to the active issue. Do not opportunistically implement later roadmap items merely because their eventual shape seems obvious.
- Run target builds and tests from that target's directory. When adding a target, update the explicit CI and release matrices and its protected licensed-validation path.

## Design and implementation expectations

- Favor explicit state machines and typed outcomes over booleans, ambient state, or exception-only control flow.
- Separate transport status, worker/connection availability, execution disposition, replay safety, and MP command results.
- Make process ownership, COM lifetime, queueing, timeouts, retries, and cleanup observable and testable.
- Preserve MP terminology mechanically wherever the target language permits it. Developers familiar with MPs should be able to recognize RPC and field names without learning a second Briosa-specific vocabulary.
- Hand-author operation protobuf, host mapping, worker request/result mapping, SDK sequence, capability registration, tests, and documentation. Keep each operation conceptually complete and reviewable whether delivered alone or in a coherent multi-command batch.
- Never hand-edit output from standard protobuf/gRPC tools. Handwritten `.proto` files and C# operation sources are ordinary reviewed source.
- Generative-AI tools may draft an operation from maintainer-provided MP and SDK evidence, but committed source, tests, observations, and engineering review are authoritative.
- Include negative-path tests for disconnected SA, MP failure, deadline, cancellation, worker hang/crash, and unsupported SA versions.
- Document why a constraint exists, especially when it comes from observed SDK behavior rather than official vendor guarantees.

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

The current baseline product is `targets/2026.1.0529.7`. It provides a production-shaped .NET 10 foundation with one supervised, serialized SDK connection, handwritten read-only MP operations, standard generated-client smoke coverage, and safe diagnostics. The handwritten protobuf contracts, `SpatialAnalyzerApi.Operations`, and runtime capability discovery—not a prose count or inventory—define the supported surface.

Add operations through coherent implementation issues and pull requests, batching related commands when useful. Reference inventory, bindings, values, or ObjectiveSA wrappers may accelerate review, but none is an implementation queue, public allowlist, or completeness requirement.
