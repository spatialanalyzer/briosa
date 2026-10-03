# Exact-target MP and SDK evidence

This guide indexes the retained SpatialAnalyzer `2026.1.0529.7` reference
snapshots. They help review an operation; they do not select one, define
public defaults, register a capability, or prove live behavior. The
[operation and protocol model](../../../../docs/architecture/operation-and-protocol-model.md)
defines the supported vertical slice.

| Evidence | Retained data | Use |
| --- | --- | --- |
| [MP inventory](../../inventory/sa/2026.1.0529.7/inventory.json) | Curated installed-documentation and View SDK Code observations, argument-level findings, fingerprints | Locate exact MP steps and input/output evidence; resolve discrepancies against the installed target. |
| [Binding registry](../../bindings/sa/2026.1.0529.7/registry.json) and [review](../../bindings/sa/2026.1.0529.7/review.json) | Observed SDK methods reconciled with committed interop and semantic families | Find exact call names and marshaling families; verify each operation against current interop and SDK tests. |
| [Value catalog](../../values/sa/2026.1.0529.7/catalog.json) | Exact-target choices/samples and pinned ObjectiveSA comparison | Review a value family; test each public choice and omission behavior independently. |
| [Interop provenance](../../interop/SpatialAnalyzer/2026.1.0529.7/Briosa.SpatialAnalyzer.Interop.provenance.json) and [public API](../../interop/SpatialAnalyzer/2026.1.0529.7/Briosa.SpatialAnalyzer.Interop.PublicApi.txt) | Approved exact-target type-library surface | Check that a proposed call is actually exposed; keep vendor binaries and source out of this repository. |

The inventory reconciles 1,302 structured documentation records and 1,360
View SDK Code observations into 1,412 command identities. Its committed JSON
retains source fingerprints, coverage, and per-command findings. The installed
HTML and generated SDK samples are not committed. A `NOT_SUPPORTED` sample
does not authorize a generic or undocumented substitute. A setter/getter
direction mismatch remains unresolved evidence, not a corrected binding.

`ExactTargetEvidenceCrossCheckTests` compares every implemented operation's
step, argument labels, setter/getter method, and direction with the inventory's
`sdk_evidence[].mp_step` and `sdk_binding` names. Each accepted difference is
a reviewed entry with rationale and provenance in
`tests/Briosa.Server.Tests/EvidenceDeviations.json`. The check covers only the
implemented subset; it is not a completeness gate.

The binding snapshot contains 151 method entries. Its historical `usable`,
`protocol`, `worker`, `adapter`, `fake`, and `generator` fields came
from a retired catalog pipeline. They are not current implementation or
coverage claims. Current calls live in `ISpatialAnalyzerSdkCalls`, the
adapter/codecs, worker tests, and each handwritten operation's exact setter
and getter sequence. In particular, an inventory-only binding that is absent
from committed interop cannot be called merely because a sample names it.

Evidence precedence is installed exact-target documentation, View SDK Code,
and interop; controlled exact-target runtime observations; then pinned
prior-release ObjectiveSA. The comparison used ObjectiveSA version
`2024.1.5.1`, commit
`324c73b8e172868b4ccb4a0121e3bd1cbc520c5c`. It can corroborate a
complete MP-step and binding match, but cannot add a target choice, default,
or compatibility claim. Raw vendor material, ObjectiveSA source, paths,
credentials, and licensed data are not republished.

The pinned ObjectiveSA source-manifest SHA-256 in the default review is
`d6107f1e10d2c957198c3cb082368033117e7e2ed2907eafb9eadc40607d295b`.

## Defaults and unresolved semantics

[Issue #82](https://github.com/spatialanalyzer/briosa/issues/82) reviewed
314 inactive convenience-default candidates. None became a Briosa default:
each affected public input remained required, omission was rejected, and the
SDK setter received only an explicit request value. This does not claim that
SpatialAnalyzer itself has no defaults. Of those candidates, 282 had only
exact-target samples, 16 conflicted with ObjectiveSA, and 16 came only from
ObjectiveSA; 276 affected mutating inputs and 38 read-only inputs. Each new
handwritten operation must still review its own presence and default behavior.

The reviewed proposal SHA-256 is
`1cb9d4e52b9371cdfbb3610edcb08f619824462a7eff4eb7ad3e3d2ed2c48556`
at Briosa commit `0d7ee3808dba1389e754d8955e3e7b46a853a233`.
The source queue SHA-256 is
`95cf36522c05952feeb882ed6e91c8803df5d08892ddc94b977afcbeaa666b00`;
the value-catalog and binding-registry review hashes are
`db4bcf2d5255b41e834247effd5ee8ff04ba16ed5aa19b518fdd99ae3dca7834`
and
`41f9beb505eaca647134c263c8d65af92ff13c0de5b4a637f794e87660891aaf`.
These identify the historical review; they are not current file checksums.

Exact-target observations that constrain special cases remain in the
[Variables validation record](variables-runtime-validation.md) and
[licensed lifecycle record](../licensed-local-lifecycle-validation.md).
The [string-setter observation](../testing/evidence/string-setters-2026-09-28.md)
records live `SetStringArg` success for the wildcard item-type and GD&T
options arguments where the sample's specialized setters are unavailable.
For example, `Get Working Frame Properties` alone may supply its documented
`Frame` type when its getter omits an embedded type; unknown literals still
fail closed. For SDK `ref object` lists, use `VariantWrapper`: a live
2026 probe rejected bare `object[]` for both string-reference list calls.
Collection-object-named SDK calls can carry either the 26-choice object domain
or the 42-choice item domain; choose per exact argument.

SA 2026.1.0529.7 was observed listening on TCP 901, 902, and 903, with SDK
traffic observed on 902. These are observations, not a vendor-guaranteed
protocol contract. After the first eligible SA instance closes, an already
open second instance did not inherit its SDK ports; a newly opened instance
was required. See the [runtime boundary](../../../../docs/architecture/runtime-boundary-and-lifecycle.md).

The inventory, binding, and value importers and their freshness gates were
retired in [#132](https://github.com/spatialanalyzer/briosa/issues/132).
Git history retains the producing code. Refreshing evidence is a separate
reviewed task and must not block ordinary builds.
