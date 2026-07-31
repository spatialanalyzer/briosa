# ADR 0022: Exact-target runtime identity and operator attestation

- Status: Accepted for the portable v0.2 foundation; authoritative runtime probes and protected validation remain pending
- Date: 2026-07-29
- Issue: [#70](https://github.com/spatialanalyzer/briosa/issues/70)
- Amends: [ADR 0010](0010-health-version-and-capability-discovery.md), [ADR 0017](0017-execution-channel-readiness.md)

## Context

Briosa is built for an exact SpatialAnalyzer release, but that configured target is only a build and deployment coordinate. It does not identify the COM server that Windows activated or the SpatialAnalyzer application to which `ConnectEx` attached. Existing ObjectiveSA experience indicates that one machine-wide SDK registration can select the last installed SDK even when several releases are installed. This is useful project evidence, not a vendor-guaranteed activation contract.

The approved SDK surface currently has no reviewed query for either runtime identity. Copying `2026.1.0529.7` from package metadata into an observed field would create a false compatibility claim. Conversely, silently treating missing evidence as compatible would allow an exact-target server to execute against an unknown release.

## Decision

Briosa preserves three claims independently:

1. the configured exact target from build metadata;
2. the activated SDK engine/type-library identity; and
3. the connected SpatialAnalyzer application identity.

Each runtime claim contains an optional version, an evidence source, and an exact-target match state. Evidence source is `Unavailable`, `RuntimeVerification`, or `OperatorAttestation`; match state is `Unavailable`, `ExactMatch`, or `Mismatch`. Configured target text is never used as a runtime observation.

The worker control protocol carries only raw runtime observations. A runtime-verified claim must contain a non-empty observed version; an unavailable claim must not contain a version. Malformed combinations are rejected before the worker becomes control-ready. The server compares observed text to the exact target using ordinal equality.

Until Hexagon identifies an authoritative query, operators may configure a fail-honest attestation for either claim independently. Each attestation requires both a version and a change-controlled evidence reference:

```text
Briosa:SpatialAnalyzer:Identity:ActivatedSdk:OperatorAttestation:Version
Briosa:SpatialAnalyzer:Identity:ActivatedSdk:OperatorAttestation:Reference
Briosa:SpatialAnalyzer:Identity:ConnectedSpatialAnalyzer:OperatorAttestation:Version
Briosa:SpatialAnalyzer:Identity:ConnectedSpatialAnalyzer:OperatorAttestation:Reference
```

An attestation is eligible only when runtime evidence for that same claim is unavailable. Runtime evidence always wins, including a mismatch; an attestation can never mask it. A partial version/reference pair fails server configuration. The two claims may legitimately use different sources, but both effective match states must be `ExactMatch` before Briosa admits MP work.

Evidence references are internal operator records. They are validated for presence but are not sent to the worker, returned by discovery, placed in default logs, or included in audit events. Discovery exposes only the effective version, source, and match state for each claim. The legacy connected-version fields remain populated consistently for compatible clients, including distinct operator-attested states.

Execution-channel readiness remains a separate dimension, but the identity gate is evaluated first. Briosa does not issue even the read-only ownership probe while either effective identity is unavailable or mismatched. In that state the public host stays live, discovery remains available, execution readiness stays unverified, `briosa.readiness` is unhealthy, and command admission returns a value-free not-started unavailable outcome. Identity evidence must also remain stable across the worker startup exchange and ordinary responses; inconsistent process evidence fails closed.

## Operator attestation procedure

An operator must establish and retain independent evidence for each unavailable claim before supplying configuration. The reference should identify a controlled installation inventory, registration inspection, vendor-supported diagnostic, or protected-run record; it must not contain a path, credential, license value, or other sensitive material. The operator records the exact observed/attested release rather than copying the Briosa package target by default.

The protected licensed workflow requires separate version and reference dispatch inputs for both claims and passes them only after its environment reviewer approves the run. The workflow does not fill those versions from the package target. Portable fake-worker tests use an explicitly labeled `portable-fake-worker` reference and are not release evidence.

## Validation and unresolved evidence

Portable tests cover mixed runtime/attested claims, independent missing claims, malformed evidence, partial configuration, exact matches, mismatches, runtime precedence, process transport, discovery, health, and command admission without SpatialAnalyzer.

Issue #70 remains open until the project obtains Hexagon guidance on authoritative SDK-engine/type-library and connected-application identification and, where environment and licensing policy permit, records protected validation for a matching installation and a deliberately detectable mismatch. Until then, the implementation is a reversible fail-closed foundation and operator attestation remains explicitly distinguishable from runtime verification.

## Consequences

- A default installation with no runtime identity mechanism and no explicit attestations is live but not ready for MP work.
- Operators can attest either missing claim without weakening a runtime-verified claim.
- Discovery communicates compatibility evidence without exposing evidence references or inventing observations.
- A future authoritative probe can populate the existing worker evidence contract and automatically take precedence over deployment attestation.
