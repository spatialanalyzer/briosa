# Extracted MP command inventory

Issue [#15](https://github.com/spatialanalyzer/briosa/issues/15) separates locally installed SpatialAnalyzer evidence from Briosa's curated supported-command catalog.

The extractor recursively reads two evidence trees for one exact SpatialAnalyzer release:

- the installed HTML under `Documentation/Content/Topics/Scripting/MPCommandListing`; and
- the VB-formatted output produced by **View SDK Code** in SpatialAnalyzer's MP Editor.

The VB exports may retain a `.txt` extension. They are evidence snippets rather than compilable source files, and the extractor accepts both `.txt` and `.vb`.

## Run the extractor

Pass all workstation-specific locations explicitly. They are never written to generated output.

```powershell
dotnet run --project tools/Briosa.Generator -- `
  mp-inventory-extract `
  2026.1.0529.7 `
  '<installed MPCommandListing directory>' `
  '<View SDK Code export directory>' `
  inventory/sa/2026.1.0529.7
```

The command writes:

- `inventory.json`, the machine-readable evidence inventory; and
- `report.md`, coverage counts, discrepancies, and unresolved metadata gaps.

Run the same command twice against unchanged source trees to obtain byte-identical files. Source fingerprints are SHA-256 hashes over sorted relative paths and per-file hashes. Timestamps and absolute paths are excluded.

## Evidence matching

Documentation pages are command candidates when they contain a command heading and at least one structured command section. Input and return tables provide documented ordinals, types, names, direction, and result-only status. Requiredness remains `unknown` unless the documentation explicitly marks an input optional.

Each SDK step is split at `SetStep`, with setters collected before `ExecuteStep` and getters collected afterward. `NOT_SUPPORTED` is preserved as unavailable binding evidence. Generated sample values are deliberately not treated as defaults.

The matcher uses exact command and argument names first. It may pair uniquely normalized names to account for typography, punctuation, spacing, or capitalization differences, but it retains both exact source strings and emits `mp_step_text_difference` or `argument_name_text_difference`. Ambiguous normalized matches are not guessed.

The overall MP outcome is represented separately from return arguments. A returned-status documentation section establishes only that the outcome is documented; it does not convert the status into an output argument.

## Intellectual-property boundary

Raw installed HTML, generated VB samples, and vendor prose remain local and are not committed. Generated inventory contains derived facts approved for redistribution: command/category identity, argument metadata, setter/getter names, relative evidence references, hashes, and discrepancy codes.

The inventory is evidence, not Briosa's public API. Only reviewed entries under `catalog/sa/<exact-version>` can generate supported protocol operations.

## Known authority gaps

- Installed documentation is useful but is not an authoritative machine-readable contract.
- Absence of “optional” does not prove that an input is required.
- Generated values do not prove meaningful defaults.
- A missing setter/getter or `NOT_SUPPORTED` marker is reported rather than replaced with an inferred generic SDK call.
- Documentation/SDK disagreements remain unresolved until maintainer review or authoritative Hexagon metadata resolves them.
- No compatibility or semantic equivalence is inferred across SpatialAnalyzer releases.

Portable tests use Briosa-authored synthetic HTML and VB fixtures. Ordinary builds and CI never require SpatialAnalyzer, a license, installed vendor documentation, or the local VB export tree.
