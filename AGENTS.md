# Briosa agent guide

This file is the canonical repository-level guidance for automated coding agents and human contributors working in `spatialanalyzer/briosa`. Read it before changing code, build infrastructure, schemas, documentation, or repository settings.

## Project purpose

Briosa is an open-source gRPC bridge around the Hexagon SpatialAnalyzer SDK. It will expose SpatialAnalyzer MP functions through a clean, language-neutral protocol. Separate repositories provide thin, idiomatic clients such as `briosa-dotnet`, `briosa-js`, and `briosa-py`.

Briosa is not a replacement for SpatialAnalyzer. A user must separately install, run, and hold a valid license for SpatialAnalyzer before Briosa can perform useful work.

The repository is licensed under Apache-2.0. SpatialAnalyzer, the SA SDK, their brands, proprietary binaries, and proprietary implementation remain Hexagon intellectual property. Do not imply ownership, affiliation, endorsement, or support beyond what is expressly documented by the project governance repository.

## Repository relationships

- `spatialanalyzer/briosa`: gRPC server, SDK worker, protocol definitions, command catalog, generators, and server tests.
- `spatialanalyzer/briosa-*`: language-specific thin clients.
- `spatialanalyzer/community`: organization Discussions. Architecture exploration is recorded in [Discussion #1](https://github.com/orgs/spatialanalyzer/discussions/1).
- `spatialanalyzer/governance`: organization and project governance, policies, and unresolved governance questions.
- [Briosa Roadmap & Delivery](https://github.com/orgs/spatialanalyzer/projects/1): cross-repository planning source of truth.

Keep public protocol design in `briosa`; do not let a client repository become the accidental source of truth for shared API semantics.

## Established technical facts

- The SA SDK is an OLE Automation/DCOM server exposed by `SpatialAnalyzerSDK.exe`.
- The initial implementation target is .NET 10 on Windows x64. SDK automation must run on an STA thread.
- SpatialAnalyzer must already be running for `ConnectEx(host, statusCode)` to connect. Use `localhost` for the local application; a reachable remote hostname or IP may also connect.
- When several SpatialAnalyzer instances are open, only the first eligible instance owns the SDK communication ports. Closing it does not transfer ownership to an already-open instance; a newly opened instance must acquire the ports.
- SA 2026.1.0529.7 was observed listening on TCP 901, 902, and 903, with SDK traffic observed on 902. Treat these observations as evidence, not as a vendor-guaranteed protocol contract.
- Multiple SDK clients may report successful connections, but concurrent MP execution is unsafe. Experiments showed the first connected client owning execution while a second client could block indefinitely in `ExecuteStep`.
- A successful `ExecuteStep` return value does not prove that the MP command succeeded. Always inspect `GetMPStepResult` and preserve the MP-level outcome.
- SA 2026.1.0529.7 exposed 1,295 structured MP command documents in 24 categories during initial exploration. This is candidate metadata, not automatically the supported Briosa API.

See the [Discussion #1 findings](https://github.com/spatialanalyzer/community/discussions/1#discussioncomment-17706394) before changing connection, concurrency, timeout, or process-lifecycle assumptions.

## Architectural invariants

Unless an accepted design decision explicitly changes them, preserve these constraints:

1. The public gRPC host must not own COM state directly. A separately supervised worker process owns the SDK client and can be replaced after a hang or crash.
2. One worker owns exactly one active SDK connection.
3. One worker-owned STA serializes the entire MP sequence: `SetStep`, argument setters, `ExecuteStep`, and result retrieval. Never interleave sequences from concurrent gRPC calls.
4. Client cancellation and gRPC deadlines must not be confused with successful cancellation of an in-flight COM call. A watchdog may need to terminate and replace the worker.
5. Public protobuf contracts must describe SpatialAnalyzer concepts without exposing COM implementation types.
6. Supported MP operations come from a curated, versioned command catalog. Generation should be deterministic across protocol, adapters, documentation, and completeness tests.
7. Ordinary builds and tests must not require SpatialAnalyzer, a license, or proprietary SDK binaries. Put the SDK behind an internal abstraction and exercise lifecycle and failure behavior with a fake.
8. Real-SA integration tests require a separately licensed, protected Windows environment. Never expose such a runner or its secrets to untrusted pull-request code.
9. Bind public services to loopback by default until remote authentication, transport security, authorization, and command-risk policies are established.
10. Log operation identity, timing, connection state, and outcomes, but do not log geometry, paths, credentials, proprietary data, or raw arguments by default.

## Interop and intellectual-property boundary

- Build only against interfaces made available through a properly installed and licensed SA SDK/type library.
- Do not copy SpatialAnalyzer source, decompile proprietary implementation, or commit/publish Hexagon binaries.
- Keep generated interop provenance and the generation procedure explicit and reproducible.
- Before distributing generated interface assemblies or extracted vendor documentation, confirm that the planned artifact and redistribution terms are covered by documented project approval. When uncertain, stop and request maintainer/Hexagon focal guidance.
- Treat installed MP documentation as input evidence. Do not republish vendor text wholesale; curate facts needed to define Briosa behavior and generate original documentation.

## Work planning and Git workflow

- GitHub issues and the organization Project are the source of truth for planned work.
- Epics are planning containers, not branch boundaries.
- Start from a Task. Use a short-lived branch named `<issue-number>-<short-description>`, such as `7-solution-scaffold`.
- A pull request is the smallest coherent, buildable, reviewable change. A Task may require several PRs, and one PR may close tightly coupled Tasks, but avoid long-lived Epic branches.
- Link PRs with `Closes #<issue-number>` only when the PR satisfies the issue's acceptance criteria. Use `Refs #<issue-number>` for partial work.
- Keep `main` buildable. Prefer squash merges and delete merged branches.
- Do not silently invent policy for an unresolved topic. Record the question in an issue, Discussion, or architecture decision and mark provisional behavior clearly.
- Keep changes scoped to the active issue. Do not opportunistically implement later roadmap items merely because their eventual shape seems obvious.

## Design and implementation expectations

- Favor explicit state machines and typed outcomes over booleans, ambient state, or exception-only control flow.
- Separate transport status, worker/connection availability, and MP command results.
- Make process ownership, COM lifetime, queueing, timeouts, retries, and cleanup observable and testable.
- Prefer generated code only for repetitive catalog-derived surfaces. Keep policy, orchestration, security decisions, and exceptional behavior in reviewed hand-written code.
- Never hand-edit generated artifacts. Change the catalog, schema, template, or generator and regenerate.
- Include negative-path tests for disconnected SA, MP failure, deadline, cancellation, worker hang/crash, and unsupported SA versions.
- Document why a constraint exists, especially when it comes from observed SDK behavior rather than official vendor guarantees.

## Validation levels

Use the least privileged environment that proves the change:

1. Formatting, static analysis, and protocol/catalog validation.
2. Unit and contract tests against the fake SDK.
3. Process-level tests using fake delay, hang, crash, and malformed-result behaviors.
4. Generated-client/server smoke tests that do not require SA where possible.
5. Explicitly authorized tests against a licensed SpatialAnalyzer installation.

Before controlling a desktop SpatialAnalyzer process, connecting to another host, changing firewall settings, or running a licensed integration environment, obtain explicit permission for the current task. Avoid attaching multiple experimental SDK clients to the same SA instance. A blocked client can leave connections behind and may require a clean SA restart.

## Important unresolved decisions

Do not treat these as settled:

- The exact relationship between Briosa semantic versions, command-catalog versions, and version-locked SpatialAnalyzer releases.
- Which SpatialAnalyzer releases will be supported and for how long.
- The authoritative command metadata Hexagon can provide and what derived artifacts may be redistributed.
- Remote gRPC authentication, authorization, TLS, network topology, and command-risk policy.
- The protected runner and SpatialAnalyzer license strategy for real integration tests.
- Whether contributions will use a Developer Certificate of Origin or another contribution mechanism; DCO is currently the likely direction.
- Long-term ownership of infrastructure costs and formal corporate stewardship.

When work encounters one of these questions, implement only a reversible minimum if the active issue allows it, document the assumption, and escalate the decision instead of presenting it as established policy.

## Current initial target

The first milestone is `v0.1 - SDK Vertical Slice`, initially targeting SpatialAnalyzer 2026.1.0529.7. Its objective is a production-shaped .NET 10 foundation with one supervised, serialized SDK connection, one generated read-only operation (`Get Working Directory`), generated-client smoke coverage, and safe diagnostics.

The milestone is a proving ground, not permission to expose all MP functions immediately. Establish the runtime boundary, fake test harness, protocol conventions, command catalog, and security defaults before expanding breadth.
