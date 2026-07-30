# SDK binding and semantic value-family registry

The `bindings` tree is the exact-release bridge between extracted MP argument evidence, the committed SpatialAnalyzer interop API, and Briosa's public/private value model. It does not approve MP operations. Command dispositions and the supported-command catalog remain the only path to a public RPC.

For SA `2026.1.0529.7`, the registry reconciles 105 inventory-observed setters and 29 inventory-observed getters with 106 setters and 39 getters exposed by the committed interop assembly. The union contains 151 exact method names grouped into 115 semantic value families.

## Source and generated files

Each exact target contains:

- `review.json`, the reviewed mapping from exact SDK method cores to semantic value families and public/private type targets;
- `registry.json`, the generated union of inventory observations, dispositions, exact CLR interop signatures, review decisions, and protocol/worker/adapter/fake/generator coverage;
- `report.md`, the generated human-readable coverage and implementation matrix.

Edit only `review.json`. Never hand-edit `registry.json` or `report.md`.

A specialized SDK method can also serve more than one semantic domain. `binding_family_overrides` declares the complete allowed family set, and `argument_family_assignments` assigns every exact inventory command/SDK-order observation to one of those families. Documentation ordinals remain traceability metadata and may be empty or contain several values. Registry synchronization fails if an observation is unassigned, an assignment is stale, its documented ordinals drift, or an override omits the method's default family. Generated bindings therefore expose `semantic_value_families` as an array rather than collapsing the evidence to one label.

This rule applies to structured identifiers as well as enums. The four collection-object-named scalar/list methods can carry either `CollectionObjectName` or the broader `CollectionItemName`. ADR 0016 defines the runtime model, and the [exact-target value-family evidence catalog](value-family-evidence.md) records all reviewed assignments. Catalog generation must consume those assignments and must not infer object-versus-item semantics from the shared method name.

The review deliberately keeps specialized SDK methods distinct even when their CLR representation is the same. For example, `SetAngularUnitsArg` maps to `angular_unit`, while `SetStringArg` maps to the primitive `string` family. Structured setters and getters share a family only when their exact semantic shape matches; method-name similarity alone is insufficient.

`public_type_target` and `worker_type_target` are implementation targets for issues that build the value families. They do not claim that the corresponding type already exists. Generated `implementation_status` distinguishes `implemented`, `planned`, `blocked`, and `not_required` families. Per-binding coverage reports protocol, worker, adapter, fake, and generator status independently so partial implementations cannot appear complete.

## Mechanical adapter-completeness gate

The SA `2026.1.0529.7` implemented surface contains 97 usable exact methods: 75 setters and 22 getters. Expanding the six shared methods by their reviewed semantic domains produces 103 method/family contract rows: 79 input rows and 24 output rows. `BindingFamilyAdapterCompletenessTests` derives those rows directly from `registry.json` and `catalog.json`; there is no sample-only allowlist.

The gate requires the protocol, worker, adapter, fake, and generator coverage sets in `review.json` to equal the complete usable-method set. Each input row is serialized through `WorkerControlChannel`, converted by the real worker-control mapper, dispatched through the production adapter, and repeated with SDK setter rejection. Each output row covers successful retrieval, MP-failure suppression, getter failure, and response serialization. All adapter calls run through `SerializedSdkExecutor`, and the test asserts exact method identity, full MP call order, STA affinity, and `VariantWrapper` use for every `ref object` boundary.

The same test consumes all 42 evidence enum types and 470 exact SDK literals, all 35 structured worker types and 108 fields, and all 995 shared-method assignments. Collection object and collection item scalar/list domains are executed separately; unknown returned type literals fail closed. Adding an implemented family, usable binding, evidence member, structured field, or new shared-method domain without a complete worker-control and adapter path therefore fails ordinary CI.

This gate uses a vendor-independent dispatch fake. It does not activate SpatialAnalyzer and does not claim to emulate MP behavior. The exact current counts and covered failure modes are summarized in the [release-specific completeness reference](../reference/sa/2026.1.0529.7/binding-family-completeness.md).

## Binding statuses

| Status | Meaning |
| --- | --- |
| `usable` | The inventory observation, exact interop signature, and semantic family agree. Individual commands still require disposition and catalog approval. |
| `excluded_only` | The method is currently observed only on intentionally excluded commands. No adapter is required solely for those commands. |
| `blocked_missing_interop` | View SDK Code named the method, but the exact-target interop API does not expose it. Briosa cannot call it through the approved interface. |
| `blocked_semantics` | The exact interop method exists, but release-specific argument semantics are unresolved, so Briosa cannot define a safe public/worker mapping. |
| `unobserved_interop` | The interop API exposes the method, but no extracted command argument uses it. It is retained for drift accounting, not treated as supported. |

Mixed-use methods remain `usable` when at least one non-excluded command observes them. Product-scope exclusion is command-specific and must not disable a shared binding needed by another command.

The first exact-target review found six `blocked_missing_interop` methods. Issue #79 finalized their affected command rows without inventing replacements: four commands are `sdk_unavailable`, and `Create Cloud Thinning Settings` is intentionally excluded because clients already construct `cloud_thinning_options` for consumer commands that call `SetCloudThinningOptionsArg`. The registry retains all six method-level `blocked_missing_interop` facts because the generated names are still absent from the exact interop API; a final unsupported command disposition is not evidence that a method became callable. Issue #57 separately records the B-spline getter/setter, projection-options setter, and point-delta-report-options setter as `blocked_semantics` because the exact release does not provide verified encodings or complete choice lists. A generated sample call is not evidence that Briosa may substitute a generic SDK method.

## Workflow

After an inventory, disposition, interop, semantic-family, or adapter-coverage change, regenerate the target:

```powershell
dotnet run --project tools/Briosa.Generator -c Release -- `
  binding-registry-sync `
  inventory/sa/2026.1.0529.7/inventory.json `
  disposition/sa/2026.1.0529.7 `
  interop/SpatialAnalyzer/2026.1.0529.7 `
  bindings/sa/2026.1.0529.7
```

Then verify it exactly as ordinary CI does:

```powershell
./eng/Verify-BindingRegistry.ps1
./eng/Verify-ValueFamilyEvidence.ps1
dotnet test tests/Briosa.Worker.Tests/Briosa.Worker.Tests.csproj -c Release `
  --filter FullyQualifiedName~BindingFamilyAdapterCompletenessTests
```

Verification requires only committed repository artifacts and the documented .NET SDK. It does not activate SpatialAnalyzer, connect to an SDK server, require an SA license, or read the local documentation and View SDK Code corpus.

For a new exact SpatialAnalyzer release, create an independent target review. Do not copy a prior registry as compatibility evidence: method availability, signatures, enum values, and argument meanings may change between releases.
