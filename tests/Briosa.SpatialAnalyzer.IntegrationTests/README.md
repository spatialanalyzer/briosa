# Licensed SpatialAnalyzer file-operation tests

This opt-in integration runner exercises the exact issue #80 MP step through the
pinned ObjectiveSA implementation. It is intentionally excluded from
`Briosa.slnx`: ordinary builds and pull-request tests must not require
SpatialAnalyzer, its SDK registration, a desktop session, a license, or the
ObjectiveSA checkout.

Run a single scenario through `eng/Test-LicensedFileOperations.ps1`. The script
requires explicit confirmation, verifies the exact `2026.1.0529.7` SA and SDK
binaries, starts one fresh disposable SA generation, admits one SDK client,
enforces a watchdog, closes the disposable generation, and emits only structural
status. It does not retain paths, object names, geometry, file contents, process
identifiers, or proprietary artifacts.

Use 64-bit PowerShell 7 (`pwsh`) from the repository root with Visual
Studio/MSBuild installed. For a self-contained generated-fixture scenario:

```powershell
./eng/Test-LicensedFileOperations.ps1 `
  -Scenario save `
  -ObjectiveSARoot C:\git\objectivesa `
  -ConfirmLicensedSpatialAnalyzerTest
```

The script verifies a clean exact-commit ObjectiveSA checkout, rebuilds both
ObjectiveSA and the runner, accepts absolute fixture paths containing spaces,
and returns a nonzero exit
code for a failed call, watchdog expiry, malformed/mismatched structural result,
cleanup failure, or residual SA/SDK/Briosa process.

The machine-readable `file-operation-matrix.json` contains exactly one scenario
for every issue #80 command. Fixture-dependent scenarios accept a local JSON
descriptor with an input path, a SpatialAnalyzer job path, and/or object
references. The event-list, XML-merge, and Polyworks scenarios open a disposable
copy of the supplied job so their references exist in the fresh SA generation.
The XML-merge scenario also mutates only a disposable copy of its input and
verifies that the supplied source file remains unchanged. The point-set scenario
instead creates a point group and treats the exact MP failure plus absence of an
output file as its expected negative result; it does not claim successful
point-set export. Descriptors, jobs, and third-party files are local licensed
inputs and must never be committed.

A local descriptor uses the snake-case shape defined by
`fixture-descriptor.schema.json`. Supply only the fields needed by the scenario:

```json
{
  "job_path": "C:\\private-fixtures\\objects.xit64",
  "input_path": "C:\\private-fixtures\\measurements.xml",
  "object": {
    "collection_name": "",
    "name": "FixturePointGroup",
    "type": "Point_Group"
  },
  "items": [
    {
      "collection_name": "",
      "name": "FixtureEvent",
      "type": "Event"
    }
  ]
}
```

`object.type` must be an ObjectiveSA `ObjectType` literal such as `Point_Set`,
`Point_Group`, or `Cloud`; `items[].type` must be an `ItemType` literal such as
`Event`. The fresh SA generation never opens the original job directly.

ObjectiveSA parity authorizes an at-risk Briosa candidate only when the exact MP
step and complete setter/getter shape match the committed SA 2026.1 evidence.
Successful execution is recorded separately. An unavailable fixture is not an
intentional exclusion and is never represented as a passing test.
