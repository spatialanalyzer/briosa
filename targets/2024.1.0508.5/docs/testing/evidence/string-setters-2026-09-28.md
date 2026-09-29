# SA 2024 string-setter observation — 2026-09-28

A one-off, STA-thread SDK probe ran against a fresh licensed
SpatialAnalyzer `2024.1.0508.5` instance. The exact 2024 SDK engine was
prestarted for this session and its file version was independently observed
as `2024.1.0508.5`; the machine-wide 2026 SDK registration was not changed.
`ConnectEx` returned true with status `0`. No other SA, SDK, or Briosa process
was present before the probe; the session was closed afterward. The probe did
not use a Briosa RPC, and no returned application data was retained.

| MP step | Relevant `SetStringArg` input | Observed result |
| --- | --- | --- |
| Make a Collection Item Name Reference List - WildCard Selection | `Item Type` = `Any` (with both wildcard criteria `*`) | All three string setters returned true; `ExecuteStep` returned true; `GetMPStepResult` returned true with MP code `2`. |
| Set GD&T Options | `Distance Between Mode` = `Centroid`; `Evaluation Method` = `None` | All nine setters, including both string setters, returned true; `ExecuteStep` returned true; `GetMPStepResult` returned true with MP code `2`. |

The exact-target documentation's specialized setter names are absent from
the committed 2024 interop surface. ObjectiveSA uses `SetStringArg` for the
same arguments. This runtime observation supports the string-setter
alternative for these two 2024 MP steps and tested values. It does not
validate every enum choice, unrelated commands, or a Briosa RPC path.

Portable generated-client tests with a fake worker passed for both public
RPC routes and their bindings. A separate attempt to start the 2024 Briosa
worker while the 2024 SDK was prestarted failed at SDK client activation,
before either MP RPC could run. The machine's vendor COM registration still
selected the 2026 engine. Thus this record validates the 2024 SDK/SA setter
sequence directly and the 2024 RPC mappings portably; it does not claim a
licensed end-to-end 2024 Briosa RPC run.

The typed Briosa mappings preserve the target's public enum domains and
convert them to reviewed SDK literals before calling `SetStringArg`. Do not
transfer the separate 2026 dispatch observation to this target.

## Packaged RPC follow-up

The `0.9.0-dev.2` server ZIP built from
`aa0c7ae1a2c5529033a2772ef9aad7b37321c58c` (SHA-256
`cfe116a5ca0aa3b8b9e496726eee15305cd162aa37f9abd5e74fcc9721b72688`)
passed both public string-setter RPCs against matching installed SA/SDK
`2024.1.0508.5` on 2026-09-28. The generated client requested `Any` with
`*` wildcard criteria and GD&T `Centroid`/`None`; both calls returned
`Succeeded`. The maintained read-only/lifecycle package smoke also passed in
the same session. See the [R39 record](../../../../docs/development/r39-licensed-validation.md)
for the package and environment scope.
