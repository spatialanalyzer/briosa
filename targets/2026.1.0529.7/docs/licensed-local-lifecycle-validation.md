# Licensed local lifecycle validation

`eng/Test-LocalLicensedLifecycle.ps1` validates the public lifecycle contract
against a properly installed and licensed SpatialAnalyzer 2026.1.0529.7 on a
maintainer-controlled 64-bit Windows machine. It is opt-in and is not part of
ordinary CI. Run one scenario at a time and do not run these scenarios in
parallel.

The runner has three modes:

- `OwnedApplication` starts the SDK while disconnected, launches a fresh
  server-owned SpatialAnalyzer application, connects, restarts and reconnects
  the SDK, then stops the SDK and normally closes only that application.
- `ExternalApplication` requires exactly one eligible SpatialAnalyzer process
  that the operator started before the test. It connects and stops Briosa's SDK
  while proving the external application remains running.
- `SdkLossRecovery` launches an owned application, identifies the one SDK
  engine created by the test, deliberately terminates only that process, then
  validates incident detection and explicit replacement without replay.

All modes first prove that starting the gRPC server alone creates neither a
worker nor an SDK process. The runner refuses ambiguous starting states, an
occupied loopback port, an unexpected executable, pre-existing Briosa workers,
or pre-existing SDK engines. It records only value-free lifecycle results.

Example:

```powershell
.\eng\Test-LocalLicensedLifecycle.ps1 `
  -Scenario OwnedApplication `
  -ConfirmLicensedSpatialAnalyzerTest `
  -SpatialAnalyzerExecutablePath "C:\Program Files (x86)\New River Kinematics\SpatialAnalyzer 2026.1.0529.7\x64\Spatial Analyzer64.exe" `
  -ActivatedSdkAttestedVersion "2026.1.0529.7" `
  -ActivatedSdkAttestationReference "maintainer-local-validation" `
  -ConnectedSpatialAnalyzerAttestedVersion "2026.1.0529.7" `
  -ConnectedSpatialAnalyzerAttestationReference "maintainer-local-validation"
```

For `ExternalApplication`, first start exactly one copy of the configured
SpatialAnalyzer executable and wait for it to finish opening. Do not leave any
standalone SDK clients or Briosa workers running.

If a scenario fails, inspect the machine before retrying. The runner stops the
server and workers it created, but it does not broadly terminate SDK or
SpatialAnalyzer processes when ownership is uncertain. Any residual process is
reported for manual recovery.
