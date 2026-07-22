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
