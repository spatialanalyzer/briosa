# SA 2026 string-setter observation — 2026-09-28

A one-off, STA-thread SDK probe ran against a fresh licensed
SpatialAnalyzer `2026.1.0529.7` instance and an activated SDK engine whose
file version was independently observed as `2026.1.0529.7`. `ConnectEx`
returned true with status `0`. No other SA, SDK, or Briosa process was present
before the probe; the session was closed afterward. The probe did not use a
Briosa RPC, and no returned application data was retained.

| MP step | Relevant `SetStringArg` input | Observed result |
| --- | --- | --- |
| Make a Collection Item Name Reference List - WildCard Selection | `Item Type` = `Any` (with both wildcard criteria `*`) | All three string setters returned true; `ExecuteStep` returned true; `GetMPStepResult` returned true with MP code `2`. |
| Set GD&T Options | `Distance Between Mode` = `Centroid`; `Evaluation Method` = `None` | All nine setters, including both string setters, returned true; `ExecuteStep` returned true; `GetMPStepResult` returned true with MP code `2`. |

The exact-target View SDK Code sample names `SetItemTypeArg` and two
`SetMPGDTOptions...Arg` setters. These methods are absent from the committed
interop surface; a separate read-only dispatch lookup against the installed
2026 engine returned `DISP_E_UNKNOWNNAME` for all three. ObjectiveSA uses
`SetStringArg` for the same arguments. The live result above establishes the
string-setter alternative for these two MP steps and tested values; it does
not validate every enum choice or unrelated uses of those argument names.

After the typed mappings were committed locally, a separate generated-client
check launched a fresh SA 2026 session through Briosa, established MP readiness,
and called both public RPCs. Each returned `MpExecutionState.Succeeded` and
MP code `2`; the wildcard RPC also returned a structurally decoded list.
The client stopped the SDK and closed its owned SA session. Portable
fake-worker generated-client tests cover both routes and their bindings.

The typed Briosa mappings preserve the public enum domains and convert them
to reviewed SDK literals before calling `SetStringArg`.

## Packaged RPC follow-up

The `0.9.0-dev.2` server ZIP built from
`aa0c7ae1a2c5529033a2772ef9aad7b37321c58c` (SHA-256
`30ca84516e814acbba58556af7eedd96e73744c96b067c73a1b69f5516da3e84`)
was attempted against installed SA `2026.1.0529.7` on 2026-09-28. Its
runtime-verified activated SDK identity was still `2024.1.0508.5` because
the protected machine-wide COM registration had not changed. The server
correctly rejected readiness before either RPC ran. After the machine-wide
registration was corrected, the worker verified the exact 2026 SDK, but
hidden SA launches did not open SDK ports and `ConnectEx` returned status
`-3`. The maintainer then launched SA normally; the same packaged server
reached MP readiness, passed the maintained read-only smoke, and both public
string-setter RPCs returned `Succeeded` for `Any`/`*` and `Centroid`/`None`.
Public stop-SDK completed while the maintainer-owned SA stayed open. The
earlier direct SDK and source-built RPC observations above remain distinct.
See the
[R39 record](../../../../docs/development/r39-licensed-validation.md).
