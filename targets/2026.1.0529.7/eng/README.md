# Engineering scripts

Licensed local lifecycle validation is documented in
[`../docs/licensed-local-lifecycle-validation.md`](../docs/licensed-local-lifecycle-validation.md).

The scripts in this directory validate handwritten Briosa source, standard protobuf artifacts, Windows packaging, the supervised worker runtime, and the opt-in licensed read-only operation paths. None reconstructs or verifies a custom command catalog.

## Ordinary validation

```powershell
dotnet restore Briosa.slnx --locked-mode
dotnet build Briosa.slnx -c Release --no-restore
dotnet test Briosa.slnx -c Release --no-build --no-restore
./eng/Verify-Protocol.ps1
./eng/Verify-InteropArtifacts.ps1 -NoBuild
```

- `Verify-Protocol.ps1` formats, lints, and builds the handwritten protobuf contracts with Buf. `-AgainstRef` optionally runs the file-level breaking check.
- `Verify-InteropArtifacts.ps1` verifies the approved interop assembly, canonical API manifest, provenance, and binary allowlist.
- `Test-LocalSpatialAnalyzerHost.ps1` verifies Debug source-host composition without starting SpatialAnalyzer.
- `Test-RuntimePerformance.ps1` runs vendor-independent fake-worker measurements and structural bounded-state checks.

## Packaging and smoke tests

```powershell
./eng/Test-WindowsPackage.ps1 -Version 0.1.0-local
./eng/Test-ProtocolArtifact.ps1 -Version 0.1.0-local
./eng/Test-ClientConformancePackage.ps1 -Version 0.1.0-local
./eng/Test-ClientScenarios.ps1 `
  -PackagePath artifacts/package-smoke/briosa-0.1.0-local-sa-2026.1.0529.7-win-x64.zip
```

- `New-WindowsPackage.ps1` creates the self-contained Briosa Windows package. It includes build and interop provenance but does not duplicate the runtime operation registry or include a catalog.
- `Test-WindowsPackage.ps1` builds twice, checks byte reproducibility, manifests, checksums, diagnostics, loopback startup, and rejection of unsafe endpoint binding.
- `New-ProtocolArtifact.ps1` packages the handwritten `.proto` sources and Buf descriptor set for normal ecosystem client generation.
- `Test-ProtocolArtifact.ps1` verifies reproducibility, manifests, checksums, and descriptor equivalence.
- `New-ClientConformancePackage.ps1` composes the real self-contained server, a deterministic fake worker, the handwritten scenario contract, and the language-neutral runner into a target-qualified first-party client test artifact.
- `Test-ClientConformancePackage.ps1` builds that artifact twice, verifies its manifest, schema, checksums, and byte reproducibility, and exercises the runner with a minimal fixture. The artifact requires no SpatialAnalyzer installation, SDK binary, or license.
- `Test-ClientScenarios.ps1` starts the packaged server inert, uses the public lifecycle RPCs with a fake application and worker, and then runs standard generated gRPC scenarios. It covers success, unavailability, policy rejection, MP failure, getter failure, deadlines, cancellation, explicit watchdog recovery, and unsupported services.

## Interop provenance

`Generate-SpatialAnalyzerInterop.ps1` is an explicit maintainer tool for a properly installed and licensed SDK/type library. It uses `TlbImp.exe` and `Briosa.InteropInspector`; it is unrelated to operation generation.

Do not run it merely to satisfy an ordinary build. Confirm redistribution approval before changing or publishing interop artifacts.

## Licensed SpatialAnalyzer

`Test-LicensedSpatialAnalyzer.ps1` is the explicit local/package runner for the reviewed read-only generated-client workflow. It requires:

- Windows x64;
- a separately installed and licensed SpatialAnalyzer `2026.1.0529.7`;
- no competing SpatialAnalyzer SDK clients;
- independent activated-SDK and connected-SA identity evidence; and
- `-ConfirmLicensedSpatialAnalyzerTest`.

It starts or uses only the explicitly authorized exact-target environment, exercises the public gRPC API, emits structural status only, and never prints returned SpatialAnalyzer values.

`Test-LocalLicensedLifecycle.ps1` is the serial opt-in lifecycle suite described
in [`../docs/licensed-local-lifecycle-validation.md`](../docs/licensed-local-lifecycle-validation.md).

`Test-LicensedRunnerState.ps1` and `Verify-LicensedRunnerWorkflow.ps1` protect the separately dispatched self-hosted runner workflow.

## Removed pipeline

Issue #132 retired the custom catalog generator, disposition-completeness checks, generated operation documentation, generated conformance manifests, release audit, and stale-artifact gates. Git history preserves those tools and outputs. The retained `inventory`, `bindings`, and `values` trees are reference snapshots, not inputs to these scripts.
