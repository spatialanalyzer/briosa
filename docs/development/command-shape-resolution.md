# SA 2026.1.0529.7 command-shape resolution

The disposition ledger stores the reviewed executable shape of every approved candidate in `command_shape`. This is the maintained boundary between extracted evidence and future catalog scaffolding. It prevents a generator from reinterpreting documentation text, SDK samples, or primitive types while promoting a command.

## Resolution policy

For a reviewed candidate, the single exact-target View SDK Code occurrence determines the executable MP step, SDK argument name, observed call order, setter/getter binding, and direction. A setter and getter for the same argument resolve to `input_output`; a setter alone resolves to `input`; and a getter alone resolves to `output`. This intentionally resolves documentation text, ordinal, and direction disagreements in favor of the exact SDK call sequence without changing the extracted inventory evidence.

Input presence is a Briosa contract decision, not a claim about an undocumented SpatialAnalyzer default:

- an input explicitly marked optional by the installed documentation is optional and omits its SDK setter when absent;
- every other input is required by Briosa and rejects an absent request field;
- every resolved input currently has default status `none`;
- generated SDK sample literals are neither retained nor promoted as defaults.

A future reviewed catalog default must record status `reviewed`, a value, and `set_catalog_default`. The semantic validator rejects every inconsistent combination.

## Fail-closed outcomes

If an argument has no observed usable setter or getter, the command is `sdk_unavailable`; Briosa does not guess a generic binding. Commands with incomplete exact-target semantics remain `blocked` and carry one or more command-scoped discrepancies. Each discrepancy records the affected inventory argument indexes, a `briosa` or `hexagon` owner, and exactly one GitHub dependency.

The initial reconciliation leaves only two focused dependency groups:

- issue #79 owns commands whose documentation/SDK evidence is incomplete or whose generated exact binding is absent from the committed interop API;
- issue #80 owns 11 save, import/export, merge, and PDF operations whose path, overwrite, append, replacement, or interactive behavior still needs a safe contract.

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

Only a reviewed `approved_candidate` with a complete `resolved` command shape can pass validation. Excluded and SDK-unavailable commands publish no executable shape. Blocked commands publish discrepancies, not partially trusted arguments.
