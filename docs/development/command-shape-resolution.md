# SA 2026.1.0529.7 command-shape resolution

The disposition ledger stores the reviewed executable shape of every approved candidate in `command_shape`. This is the maintained boundary between extracted evidence and future catalog scaffolding. It prevents a generator from reinterpreting documentation text, SDK samples, primitive types, or prior-release wrapper choices while promoting a command.

## Resolution policy

For a reviewed candidate, the single exact-target View SDK Code occurrence determines the executable MP step, SDK argument name, observed call order, setter/getter binding, and direction. A setter and getter for the same argument resolve to `input_output`; a setter alone resolves to `input`; and a getter alone resolves to `output`. The inventory retains conflicting documentation evidence unchanged.

Semantic family and SDK method are also separate decisions. When one exact SDK method serves multiple domains, reviewers select the family from the command argument's exact-release evidence and record that assignment explicitly; the generator must not infer the public type from the method name. Collection object versus collection item assignments follow [ADR 0016](../architecture/0016-command-argument-semantic-families.md).

Input presence, SDK omission, and Briosa convenience defaults are separate decisions:

- an input explicitly described as optional by the command text or installed documentation is optional and omits its SDK setter when absent;
- every other MP input is required unless Briosa supplies an independently reviewed convenience default;
- when a caller omits an input with a reviewed convenience default, Briosa calls the SDK setter with that value rather than omitting the setter;
- a matching ObjectiveSA prior-release default and SA 2026 generated VB value are accepted as reviewed evidence only when the exact MP step, argument semantics, and SDK setter agree;
- the SA 2026 evidence wins when it conflicts with ObjectiveSA because ObjectiveSA targets earlier SpatialAnalyzer releases;
- a conflict, or a plausible SA 2026 value without matching ObjectiveSA coverage, remains an inactive `needs_review` candidate. The input continues to reject omission until a maintainer explicitly approves the value;
- paths, credentials, license data, unresolved variables, and empty identity placeholders are never guessed automatically.

Run the local evidence review with:

```powershell
./eng/Review-CommandDefaults.ps1 `
  -ObjectiveSARoot C:\git\ObjectiveSA `
  -InventoryPath inventory\sa\2026.1.0529.7\inventory.json `
  -DispositionDirectory disposition\sa\2026.1.0529.7 `
  -Apply
```

The current ObjectiveSA layout colocates interfaces and implementations below `ObjectiveSA/Methods`; the importer also accepts the legacy split layout. The optional `-SdkCodeRoot` may be supplied when recalculating from raw View SDK Code. When it is omitted, the importer reuses the exact-target candidates already committed to the disposition ledger, making the issue #82 queue reproducible without retaining vendor source.

ObjectiveSA and raw View SDK Code remain local evidence and are not copied into the repository. The committed ledger retains only curated values, source classes, and review state. Run `./eng/Sync-ValueFamilyEvidence.ps1` after disposition synchronization to regenerate `generated/values/sa/2026.1.0529.7/default-review-queue.json`; issue [#82](https://github.com/spatialanalyzer/briosa/issues/82) owns its command-by-command maintainer review. See the [exact-target value-family evidence guide](value-family-evidence.md) for pinned provenance, fingerprints, counts, and drift checks.

## Direction-finding audit

The extractor reports a direction disagreement whenever the documentation table section and observed SDK call phase differ. That finding is intentionally literal; it does not by itself mean the command semantics are ambiguous.

The SA 2026 inventory contains 16 commands with 30 affected argument rows. All were rechecked against the installed command prose and View SDK Code:

| Outcome | Commands | Review conclusion |
| --- | ---: | --- |
| Approved candidate | 7 | Documentation placed semantically returned values under `Input Arguments`; the prose describes returned data and the generated getters agree. The executable output shape is unambiguous. |
| Intentional exclusion | 8 | The same section-label problem does not change the existing client-owned exclusion decision. |
| SDK unavailable | 1 | The direction label is not the limiting issue; another argument lacks a usable SDK binding. |

The seven approved commands are `Compute Group to Group Orientation (Rx, Ry, Rz)`, `Get Surface Physical Stats`, `Get Pipe Relationship Properties`, `Get QDAS Catalog Entries`, `Get Instrument Interface Response Timeout`, `Get Relationship Associated Data`, and `Get Scale Bar Stats`. They are not documentation/SDK semantic clashes. `Compute Group to Group Orientation (Rx, Ry, Rz)` is read-only and belongs in Wave 1.

The other nine reviewed commands are `Close JSON File`, `Open JSON File`, `Run Another Program`, `Make a Report Items Ref List`, `Double Comparison (result)`, `Integer Comparison (result)`, `Increment Point Name`, `Remove Specified Characters From String`, and `Construct TCP Fixture`. Their existing product-scope or SDK-availability dispositions remain correct.

## Fail-closed outcomes

If an argument has no observed usable setter or getter, the command is `sdk_unavailable`; Briosa does not guess a generic binding. Commands with incomplete exact-target semantics remain `blocked` and carry one or more command-scoped discrepancies. Each discrepancy records the affected inventory argument indexes, a `briosa` or `hexagon` owner, and exactly one GitHub dependency.

The reconciliation leaves two focused dependency groups:

- issue #79 owns commands whose documentation/SDK evidence is incomplete or whose generated exact binding is absent from the committed interop API;
- issue #80 reviewed 11 save, import/export, merge, and PDF operations. One is intentionally excluded and four now have constrained Wave 3 candidate contracts; six remain blocked on exact-target collision, overwrite, partial-failure, or interactive behavior. The machine-readable constraints and evidence limitations are documented in [File-operation contracts](file-operation-contracts.md).

The generated disposition report names every blocked command and discrepancy. Changing the inventory fingerprint moves the command back to re-review and discards its resolved shape.

## Review and verification

Reviewers edit command decisions and shapes in the category shards, then synchronize and verify:

```powershell
dotnet run --project tools/Briosa.Generator -c Release -- `
  disposition-sync `
  inventory/sa/2026.1.0529.7/inventory.json `
  disposition/sa/2026.1.0529.7

./eng/Verify-Disposition.ps1
```

Only a reviewed `approved_candidate` with a complete `resolved` command shape can pass validation. Excluded and SDK-unavailable commands publish no executable shape. Blocked commands publish discrepancies, not partially trusted arguments. Inactive default candidates never change request omission behavior and cannot be promoted as catalog defaults without a later reviewed ledger change.
