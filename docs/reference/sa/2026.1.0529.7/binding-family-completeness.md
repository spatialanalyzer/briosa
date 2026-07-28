# SA 2026.1.0529.7 binding-family completeness

This reference records the portable adapter-completeness baseline for the exact SpatialAnalyzer target. It describes Briosa's private protocol and SDK-boundary coverage; it does not approve additional public operations or claim that the covered bindings were exercised against a licensed SpatialAnalyzer installation.

## Covered surface

| Evidence or runtime surface | Complete count |
| --- | ---: |
| Implemented semantic value families / private value kinds | 79 |
| Usable exact SDK methods | 97 |
| Usable setters | 75 |
| Usable getters | 22 |
| Expanded method/family contract rows | 103 |
| Input method/family rows | 79 |
| Output method/family rows | 24 |
| Evidence enum types / exact SDK literals | 42 / 470 |
| Structured worker types / fields | 35 / 108 |
| Reviewed shared-method observations | 995 |

The method count is smaller than the contract-row count because six exact SDK method names serve multiple reviewed semantic domains. In particular, collection-object-named scalar and list calls carry both the 26-choice object domain and the broader 42-choice item domain. The command-specific family assignment, not the SDK method name, selects encoding and decoding.

## Portable guarantees

Ordinary CI derives the contract rows from the committed binding registry and value-family evidence catalog and verifies:

- identical complete method sets for protocol, worker, adapter, fake, and generator coverage;
- request and response serialization through the real worker-control JSON channel for every supported direction;
- exact SDK method dispatch and `SetStep` → setters → `ExecuteStep` → `GetMPStepResult` → getters ordering on the worker-owned STA;
- setter rejection before execution, MP failure without result-getter calls, and typed getter failure without default-like output leakage;
- `VariantWrapper` marshalling at every implemented `ref object` list or container boundary;
- every evidence-derived enum member and exact SDK literal, plus every structured worker field;
- every shared-method semantic-domain pair and fail-closed unknown collection object/item type literals.

The tests use a vendor-independent dispatch fake and never activate SpatialAnalyzer. Licensed protected-runner evidence remains a separate validation level.

## Change rule

Any change to usable binding status, semantic-family membership, exact enum literals, structured fields, or shared-method assignments must update the real protocol/worker/adapter path. Regenerate the binding and value-family artifacts from their reviewed sources, then run:

```powershell
./eng/Verify-BindingRegistry.ps1
./eng/Verify-ValueFamilyEvidence.ps1
dotnet test tests/Briosa.Worker.Tests/Briosa.Worker.Tests.csproj -c Release `
  --filter FullyQualifiedName~BindingFamilyAdapterCompletenessTests
```

Never hand-edit `bindings/sa/2026.1.0529.7/registry.json`, `bindings/sa/2026.1.0529.7/report.md`, or generated value-family documentation.
