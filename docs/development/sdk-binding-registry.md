# SDK binding and semantic value-family registry

The `bindings` tree is the exact-release bridge between extracted MP argument evidence, the committed SpatialAnalyzer interop API, and Briosa's public/private value model. It does not approve MP operations. Command dispositions and the supported-command catalog remain the only path to a public RPC.

For SA `2026.1.0529.7`, the registry reconciles 105 inventory-observed setters and 29 inventory-observed getters with 106 setters and 39 getters exposed by the committed interop assembly. The union contains 151 exact method names grouped into 111 semantic value families.

## Source and generated files

Each exact target contains:

- `review.json`, the reviewed mapping from exact SDK method cores to semantic value families and public/private type targets;
- `registry.json`, the generated union of inventory observations, dispositions, exact CLR interop signatures, review decisions, and protocol/worker/adapter/fake/generator coverage;
- `report.md`, the generated human-readable coverage and implementation matrix.

Edit only `review.json`. Never hand-edit `registry.json` or `report.md`.

The review deliberately keeps specialized SDK methods distinct even when their CLR representation is the same. For example, `SetAngularUnitsArg` maps to `angular_unit`, while `SetStringArg` maps to the primitive `string` family. Structured setters and getters share a family only when their exact semantic shape matches; method-name similarity alone is insufficient.

`public_type_target` and `worker_type_target` are implementation targets for issues that build the value families. They do not claim that the corresponding type already exists. Generated `implementation_status` distinguishes `implemented`, `planned`, `blocked`, and `not_required` families. Per-binding coverage reports protocol, worker, adapter, fake, and generator status independently so partial implementations cannot appear complete.

## Binding statuses

| Status | Meaning |
| --- | --- |
| `usable` | The inventory observation, exact interop signature, and semantic family agree. Individual commands still require disposition and catalog approval. |
| `excluded_only` | The method is currently observed only on intentionally excluded commands. No adapter is required solely for those commands. |
| `blocked_missing_interop` | View SDK Code named the method, but the exact-target interop API does not expose it. Briosa cannot call it through the approved interface. |
| `unobserved_interop` | The interop API exposes the method, but no extracted command argument uses it. It is retained for drift accounting, not treated as supported. |

Mixed-use methods remain `usable` when at least one non-excluded command observes them. Product-scope exclusion is command-specific and must not disable a shared binding needed by another command.

The first exact-target review found six `blocked_missing_interop` methods. They remain linked to issue #53 for command-specific resolution or focused Hexagon clarification. A generated sample call is not evidence that Briosa may substitute a generic SDK method.

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
```

Verification requires only committed repository artifacts and the documented .NET SDK. It does not activate SpatialAnalyzer, connect to an SDK server, require an SA license, or read the local documentation and View SDK Code corpus.

For a new exact SpatialAnalyzer release, create an independent target review. Do not copy a prior registry as compatibility evidence: method availability, signatures, enum values, and argument meanings may change between releases.
