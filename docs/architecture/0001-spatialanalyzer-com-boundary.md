# ADR 0001: SpatialAnalyzer COM boundary and interop provenance

- Status: Accepted for the v0.1 SDK vertical slice
- Date: 2026-07-20
- Issue: [#7](https://github.com/spatialanalyzer/briosa/issues/7)

## Context

SpatialAnalyzer exposes its SDK through the out-of-process `SpatialAnalyzerSDK.exe` OLE Automation/DCOM server. Briosa targets .NET 10 on Windows x64, but the public gRPC host must not own COM state. A separately supervised worker process will own one SDK connection and one dedicated STA execution thread.

The SpatialAnalyzer implementation and its original binaries remain Hexagon/New River Kinematics intellectual property. Project approval permits Briosa to redistribute the managed COM metadata generated from the publicly available SDK type library. That approval does not extend to the original executable, type-library container, installer, documentation, or other vendor implementation artifacts.

## Decision

The repository commits `Briosa.SpatialAnalyzer.Interop.dll` together with a provenance manifest and a canonical public API manifest. This lets an ordinary Windows runner compile the complete solution without installing or launching SpatialAnalyzer.

Only worker-side code may reference the interop assembly. `Briosa.Server`, `Briosa.Protocol`, language clients, and public protobuf contracts must not expose or depend on COM types.

The worker executable targets x64. The generated interop metadata is processor-agnostic: the installed SA 2026.1.0529.7 type library rejected `/machine:x64` with `TI2010`, while `/machine:Agnostic` produced metadata successfully for the verified .NET 10 x64 probe. This is an observed compatibility result, not a vendor guarantee about future releases.

Generation uses the x64 .NET Framework `TlbImp.exe` made available by Visual Studio Developer PowerShell. Briosa does not use MSBuild `COMReference`, because ordinary `dotnet build` must not consult the local COM registry or require full-framework MSBuild.

The importer is invoked with fixed input identity, namespace, assembly version, machine, and strict-reference arguments. `/primary` is not used because Briosa is not the type-library publisher.

## Determinism

Two clean imports of SA 2026.1.0529.7 produced different raw DLL hashes but identical canonical API hashes. The generated PE contains volatile data such as its module MVID. Briosa therefore defines interop determinism as identical managed assembly identity, types, interfaces, members, signatures, COM attributes, and referenced assemblies for identical inputs and importer configuration.

The canonical manifest deliberately excludes PE headers and the module MVID. Generation runs twice and fails if the canonical manifests differ. Once the committed assembly already represents that canonical API, regeneration preserves it instead of replacing it with a byte-different equivalent.

## Provenance and source control

Every committed interop assembly must be accompanied by:

- the SpatialAnalyzer product version and source file SHA-256;
- typelib name, GUID, version, LCID, system kind, and flags;
- importer filename, version, SHA-256, and complete normalized arguments;
- generated assembly identity and SHA-256; and
- canonical API filename and SHA-256.

Machine-specific source paths, usernames, license data, and generation timestamps are excluded. Generated artifacts are never hand-edited.

Repository verification rejects tracked `.exe`, `.tlb`, `.ocx`, and unapproved `.dll` files. Release packaging may include the approved managed interop assembly but must not include the original SDK executable or type-library container.

## Runtime consequences

Referencing the interop metadata does not start SpatialAnalyzer or connect to it. Later issues will implement worker supervision, COM activation, `ConnectEx`, STA serialization, watchdog termination, and MP result handling.

Client cancellation cannot be treated as cancellation of a blocked COM call. One worker-owned STA must serialize the complete `SetStep`, argument, `ExecuteStep`, and result sequence. Those runtime requirements remain mandatory even though the metadata assembly is processor-agnostic.

## Integration testing

Ordinary CI validates provenance, compiles the complete solution, and runs fake/vendor-independent tests without SpatialAnalyzer. A protected licensed Windows environment remains future work under [#20](https://github.com/spatialanalyzer/briosa/issues/20); it will regenerate metadata from an installed supported release and run real-SA integration tests without exposing the runner to untrusted pull-request code.
