# SpatialAnalyzer interop metadata

This directory contains managed COM metadata generated from a publicly available SpatialAnalyzer SDK type library with `TlbImp.exe`. Project approval permits redistribution of these generated definitions.

The approval does **not** cover original Hexagon or New River Kinematics executables, DLLs, type libraries, installers, documentation, or implementation code. Do not add those files to this repository.

Each versioned directory contains the generated managed assembly, a canonical text representation of its API, and a provenance manifest. Never edit those artifacts by hand. Regenerate them with `eng/Generate-SpatialAnalyzerInterop.ps1` from Visual Studio Developer PowerShell.
