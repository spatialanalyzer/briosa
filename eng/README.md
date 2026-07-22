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

Pass `-NoBuild` only after `Briosa.Generator` has already been built in the selected configuration.
