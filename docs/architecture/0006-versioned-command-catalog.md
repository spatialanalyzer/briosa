# ADR 0006: Exact-target supported-command catalogs

- Status: Accepted for the v0.1 vertical slice
- Date: 2026-07-21

## Context

SpatialAnalyzer's MP catalog is much larger than the API Briosa should expose. The SA 2026.1.0529.7 evidence set contains 1,360 generated `SetStep` occurrences across office integrations, MP control flow, file and network utilities, math helpers, metrology operations, and other categories. Some arguments have no corresponding SDK setter or getter and are emitted as `NOT_SUPPORTED`. Generated values such as `0`, `false`, and an empty string may be meaningful defaults or only sample values; each command requires review.

Raw MP names are also not unique operation identities. SA 2026.1.0529.7 contains two commands named `Save`: one saves the SA file without arguments, while another accepts an Excel workbook handle. A public API cannot infer identity or semantics from the `SetStep` string alone.

Briosa therefore needs a curated source of truth between installed evidence and generated public artifacts. It must preserve exact-release semantics, expose only useful supported operations, keep private SDK mechanics out of protobuf, and fail closed when review metadata is incomplete.

## Decision

Briosa distinguishes two datasets.

- The extracted SA inventory records candidate commands and evidence from installed documentation and generated SDK samples. Issue #15 owns extraction and review disposition.
- The supported-command catalog is the reviewed allowlist used by Briosa builds. Only catalog entries may produce public operations. Issue #16 owns generation from those entries.

Absence from the supported catalog means only that Briosa does not expose the command. It does not claim that the command is absent from SpatialAnalyzer.

## Exact-target layout and identity

Each target has an independent manifest and operation files:

```text
catalog/
  schemas/v1/
    catalog.schema.json
    operation.schema.json
  sa/
    2026.1.0529.7/
      catalog.json
      operations/
        file_operations.get_working_directory.json
```

The exact SA version appears once in the target manifest and must match its directory. The manifest also records the exact target protocol package and a positive, target-local catalog revision. Catalogs do not inherit, overlay, or declare compatibility ranges with another release.

The manifest lists every operation file in ordinal path order. Validation fails for an unlisted file, a missing file, duplicate source or operation identities, or target/package mismatches. This makes a target directory one complete reviewable snapshot of Briosa support for that release.

## Operation and protocol naming

Every operation has a stable Briosa identity with exactly two lower-snake-case segments:

```text
<service>.<operation>
```

For `file_operations.get_working_directory`, deterministic public names are:

| Artifact | Name |
| --- | --- |
| Protobuf service | `FileOperations` |
| RPC | `GetWorkingDirectory` |
| Request | `GetWorkingDirectoryRequest` |
| Result | `GetWorkingDirectoryResult` |

The catalog stores those public names so reviewers can see the API, and validation checks them against the deterministic transformation. The exact `mp_step` string is stored independently and is allowed to collide with another operation. Future client names derive from the same reviewed identity rather than from language-specific heuristics.

## Argument semantics

Arguments are one ordered list because MP documentation uses command-wide ordinals. Every entry explicitly records:

- a stable lower-snake-case argument identity;
- the exact MP argument name and ordinal;
- direction: input, output, input/output, or unknown;
- whether it is result-only;
- a public semantic type independent of COM;
- private SDK binding availability and exact setter/getter names;
- an original concise Briosa description.

Output-only arguments require a getter, prohibit a setter, and are result-only. Input-only arguments require a setter, prohibit a getter, and are not result-only. Input/output arguments require both bindings and are not result-only. The MP command outcome is not an argument; issue #13 owns its public representation and error mapping.

The schema can represent unknown or unavailable metadata so incomplete review is never silently coerced. Release validation rejects unknown direction, result-only status, semantic type, risk, input policy, or required SDK binding in the supported catalog.

## Input presence, omission, and defaults

Input presence and default values are separate decisions. Each input records:

- `presence`: required, optional, or unknown;
- `omission_behavior`: reject the request, omit the SDK setter, use a reviewed catalog default, or unknown;
- default status: reviewed, generated sample, none, or unknown;
- the value when the default status requires one.

A generated SDK sample value is evidence, not a reviewed default. Validation permits `set_catalog_default` only with a reviewed value. Required inputs reject omission. Optional inputs must explicitly say whether the server omits the setter or supplies a reviewed default.

`NOT_SUPPORTED` is recorded as unavailable SDK binding evidence. An operation with a required unsupported input is ineligible for the supported catalog. Any proposed exception for a genuinely optional unsupported argument requires explicit command review; there is no automatic fallback to a generic setter.

## Evidence and intellectual-property boundary

The manifest identifies evidence classes such as installed MP documentation, generated View SDK Code, and maintainer review. Operations contain concise references to the evidence used. Absolute workstation paths are not catalog identity.

Installed HTML, generated SDK samples, and prior proprietary wrapper source remain local evidence. They are not copied into the repository. The catalog contains curated facts, exact identifiers required for execution, and original Briosa descriptions. Issue #15 must preserve this boundary when extraction is implemented.

## Support and risk policy

The supported catalog is not a goal to expose every MP command. MP control-flow and subroutine constructs are replaced by the caller's programming language. Office, spreadsheet, serialization, and scalar/vector math helpers are normally better served by dedicated client libraries. Extracted inventory can mark these as intentionally ineligible without adding them to the public surface.

Every supported operation records whether it is read-only or mutating and any relevant risk flags, including filesystem, network, external-process, device-control, interactive, long-running, or sensitive-data behavior. Each argument records an explicit data classification. Unknown risk is denied until reviewed. The runtime exact-ID allow/deny policy consumes these facts without changing command semantics; see [ADR 0015](0015-command-policy-and-audit-events.md).

Stability and deprecation describe Briosa's public API lifecycle. Vendor availability or deprecation is separate evidence and must not be inferred from Briosa status.

## Initial operation

The SA 2026.1.0529.7 catalog begins with `file_operations.get_working_directory`:

- exact MP step `Get Working Directory`;
- no inputs;
- result ordinal 0 named `Directory`;
- output-only, result-only public string;
- private `GetStringArg` getter and no setter;
- read-only filesystem-metadata risk classification.

This is represented through the ordinary schema and validator with no command-specific code path.

## Validation

JSON Schema draft 7 validates document structure independently of code generation. The semantic validator then checks cross-file completeness, exact-target identity, deterministic names, evidence references, argument rules, reviewed defaults, and required SDK bindings. Ordinary CI runs both layers without SpatialAnalyzer, installed vendor documentation, a license, or proprietary SDK samples.

Schema changes and catalog changes are API-review inputs. The target-local catalog revision must be incremented when a reviewed snapshot changes. Generated artifacts remain downstream products and may not override catalog facts.

## Consequences

- Reviewers can distinguish SA evidence, unsupported inventory, and Briosa's actual public surface.
- Exact MP strings and private SDK bindings remain available to generators without leaking into protobuf.
- Same-name MP commands can become distinct Briosa operations, although ineligible variants need not be exposed.
- Optionality and defaults require deliberate review instead of trusting generated sample values.
- Adding a supported operation requires evidence, semantic review, risk classification, and complete binding metadata.
- Supporting another SA release adds a complete independent target directory rather than version thresholds or inherited deltas.
