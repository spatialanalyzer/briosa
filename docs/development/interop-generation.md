# Regenerating SpatialAnalyzer interop metadata

## Prerequisites

- Windows x64
- the .NET SDK selected by `global.json`
- Visual Studio 2026 with the .NET Framework 4.8 SDK tools
- a publicly available SpatialAnalyzer installation containing `SpatialAnalyzerSDK.exe`

Generation reads the embedded type library. It does not start SpatialAnalyzer, connect to an instance, or require a running licensed session.

## Generate

Open **Visual Studio 2026 Developer PowerShell**, change to the repository root, and run:

```powershell
.\eng\Generate-SpatialAnalyzerInterop.ps1 `
  -TypeLibraryPath 'C:\Program Files (x86)\New River Kinematics\SpatialAnalyzer 2026.1.0529.7\SpatialAnalyzerSDK.exe' `
  -SpatialAnalyzerVersion '2026.1.0529.7'
```

The script imports twice, compares canonical API manifests, and updates the versioned artifacts only when the managed API changes. It records no local installation path.

If `TlbImp.exe` is not discoverable, pass its absolute path with `-TlbImpPath`.

## Verify

Anyone with the .NET SDK can verify the committed artifacts without installing SpatialAnalyzer:

```powershell
.\eng\Verify-InteropArtifacts.ps1
```

Review changes to the binary together with its `PublicApi.txt` and `provenance.json`. Never update only one of the three files.
