# Engineering scripts

Run the scripts in this directory from the repository root. Interop generation requires Visual Studio Developer PowerShell; verification requires only the documented .NET SDK.

## Protocol verification

`Verify-Protocol.ps1` requires Buf 1.72.0. It verifies canonical formatting, lint rules, schema compilation, and FILE-level compatibility against `origin/main` when that ref contains a protobuf baseline:

```powershell
./eng/Verify-Protocol.ps1
```

## Command catalog verification

`Verify-Catalog.ps1` applies the versioned JSON Schemas and semantic release rules to every exact-target supported-command catalog. It rejects unlisted files, target or naming drift, unresolved metadata, unsafe default inference, and missing SDK bindings:

```powershell
./eng/Verify-Catalog.ps1
```

Regenerate the catalog-derived protobuf contracts and worker bindings with:

```powershell
dotnet run --project tools/Briosa.Generator -c Release -- catalog-generate catalog .
```

Generated artifacts are committed but must not be hand-edited. Verify a clean generation and reject stale or extra generated files with:

```powershell
./eng/Verify-CatalogArtifacts.ps1
```

Pass `-NoBuild` only after `Briosa.Generator` has already been built in the selected configuration.

## Generated-client smoke tests

Run portable packaged-host success and failure scenarios without SpatialAnalyzer:

```powershell
./eng/Test-GeneratedClientScenarios.ps1 -PackagePath <path-to-briosa-zip>
```

`Test-LicensedSpatialAnalyzer.ps1` is an explicit opt-in check for one already-running, separately licensed SA 2026.1.0529.7 instance.

`Test-LicensedRunnerState.ps1` verifies safe preflight or postflight process state without logging process IDs, paths, license data, or returned values:

```powershell
./eng/Test-LicensedRunnerState.ps1 -Phase Preflight
```

`Verify-LicensedRunnerWorkflow.ps1` is an ordinary-CI policy check that rejects untrusted triggers, mutable action references, a licensed-runner checkout, or drift from the exact runner group and environment:

```powershell
./eng/Verify-LicensedRunnerWorkflow.ps1
```

See [the generated-client smoke guide](../docs/testing/generated-client-smoke.md) for package creation, prerequisites, safety boundaries, and scenario coverage.
See [the licensed runner operations guide](../docs/operations/licensed-sa-runner.md) before provisioning or operating the protected machine.
