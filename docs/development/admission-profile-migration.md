# Admission profile migration

Status: on `main` for the Wave 2b behavioral release; not yet released. Tracked
by [#293](https://github.com/spatialanalyzer/briosa/issues/293). The design was
decided on [#242](https://github.com/spatialanalyzer/briosa/issues/242).

The indexed `Briosa:Security:Operations:Allow` and `Deny` arrays are gone.
Runtime admission now comes from a named profile, optional per-flag settings,
and optional per-operation overrides. These are applied to each target's reviewed
classification table (`src/Briosa.Server/Security/OperationClassification.cs`).
The packaged default narrows from every registered operation to the `standard`
profile.

There is no compatibility shim (#244). A server that finds `Allow` or `Deny`, or
no `Profile`, refuses to start and names this note.

## What changes for operators

| Before | After |
| --- | --- |
| `Allow:0 … Allow:N` listing every registered operation (packaged) | `"Profile": "standard"` (packaged) |
| Missing `Allow` denied every operation | Missing `Profile` fails startup |
| `Deny:0=<service>.<operation>` | `Overrides:<service>:<operation>=deny` |
| Adding an ID to `Allow` | `Overrides:<service>:<operation>=allow`, or a wider profile |
| Narrowing by command only | `Flags:<risk_flag>=deny` narrows a whole risk family |
| Environment variable `Briosa__Security__Operations__Deny__0` | `Briosa__Security__Operations__Overrides__<service>__<operation>=deny` |

Each key is a single value, so the last configuration provider wins. Unlike the
indexed arrays, a user secret, environment variable, or command-line value can
narrow or widen any single decision.

Unknown profiles, flags, services, operations, or section names, and any value
other than `allow` or `deny`, fail startup. See the
[2026 policy guide](../../targets/2026.1.0529.7/docs/operations/command-policy-and-auditing.md)
or the [2024 policy guide](../../targets/2024.1.0508.5/docs/operations/command-policy-and-auditing.md)
for the profile definitions and precedence.

## What the default no longer admits

| Target | Registered | `read-only` | `standard` (default) | `device` | `full` | Exclusive (never admitted) | Interactive (opt-in only) |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 2026.1.0529.7 | 1,027 | 185 | 811 | 912 | 929 | 42 | 59 |
| 2024.1.0508.5 | 996 | 181 | 792 | 878 | 895 | 45 | 59 |

Compared with the previous default, `standard` stops admitting these:

- device sessions, device configuration, and motion (use `device`);
- external IO such as OPC DA, code execution (`run_subroutine`,
  `terminate_all_running_mps`), and file deletion (use `full`);
- operations that open or block on operator UI (opt in with
  `Flags:interactive_ui=allow` or a per-operation `allow` override); and
- multi-call device, trapping, watch, robot-interface, and appliance session
  lifecycles. These are `ExclusiveWorkflow` and are denied under every profile,
  flag, and override until a lease design is accepted (AGENTS.md invariant 13).

The operations that the client conformance scenarios, the smoke client, and
the briosa-examples tutorials use all stay admitted by `standard`. A test pins
this list.

The 13 `Make*RuntimeSelect` operations also move from replay `Safe` to `Unsafe`.
Re-prompting an operator is not a safe replay.

## Examples

Keep the previous behavior as closely as the new model allows (everything except
exclusive workflows):

```powershell
$env:Briosa__Security__Operations__Profile = "full"
$env:Briosa__Security__Operations__Flags__interactive_ui = "allow"
```

Read-only inspection with one extra mutation:

```jsonc
"Operations": {
  "Profile": "read-only",
  "Overrides": { "construction_operations": { "construct_collection": "allow" } }
}
```

Default document automation without file writes:

```powershell
--Briosa:Security:Operations:Profile=standard --Briosa:Security:Operations:Flags:filesystem_write=deny
```

## Clients and the contract version

`ListCapabilities` keeps reporting exactly the admitted operations, so a client
that checks discovery before calling an operation sees the narrower default.
A denied call still returns the typed, value-free `PermissionDenied` outcome
with a `NotStarted` disposition.

The behavioral contract major is still 2 on `main`. The bump to contract major 3,
the new client candidates and fixture sources, retiring the major-1 retained
clients, and the 0.4.0 expected-incompatible checks all land together in the
Wave 2b freeze change. That follows the frozen-candidate flow used for major 2
(#223). No 0.9.x release is cut from `main` while Wave 2b changes accumulate.
