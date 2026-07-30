# SA 2026.1.0529.7 administrative and data-flow command review

Issue [#51](https://github.com/spatialanalyzer/briosa/issues/51) reviews the final 371 previously unreviewed exact-target commands across Event, File, Process Flow, Relationship, Reporting, Utility, Vector, and View Control domains. Together with the earlier product-scope, geometry, and device reviews, all 1,412 inventory entries now have a reviewed disposition.

## Results

| Disposition | Commands |
| --- | ---: |
| Approved candidate | 218 |
| Blocked | 1 |
| Intentional exclusion | 83 |
| SDK unavailable | 69 |

Approved candidates are assigned to the risk-ordered delivery plan:

| Delivery wave | Commands |
| --- | ---: |
| Wave 1: low-risk read-only queries | 35 |
| Wave 2: in-memory SA state mutations | 97 |
| Wave 3: bounded filesystem and audit operations | 12 |
| Wave 4: interactive or potentially long-running operations | 74 |

Candidate status is not public support. This review does not promote a new operation into the supported catalog.

## Product and evidence rules

- SA-specific collection import/export, report generation, relationships, events, notes, folders, logs, and programmatic view state may remain candidates when the exact-target SDK shape is coherent.
- Generic filesystem manipulation, ODBC access, arbitrary program or PowerShell execution, raw ASCII serialization, DataShare integration, application-chrome control, notifications, and MP runtime control flow remain client-owned intentional exclusions.
- Briosa does not expose a public command that shuts down the separately installed SpatialAnalyzer application.
- Operator-driven relationship watch and trapping workflows are excluded. `Relationship Watch Window Template` is additionally excluded because its exact-target shape includes SA-hosted UDP integration.
- Issue #80 finalized 11 ambiguous save, merge, PDF, and SDK-only import/export variants after controlled SA 2026.1.0529.7 validation: four have performed, constrained Wave 3 candidate contracts and seven are intentional exclusions. None retains an issue #80 blocker. See [the file-operation contract review](file-operation-contracts.md); candidate status does not make an operation publicly supported.
- An absent exact-target SDK occurrence or explicit unavailable required input produces `sdk_unavailable`. Other direction, ordinal, getter, setter, or semantic conflicts remain command-scoped blockers linked only to issue #53.

## Filesystem and administrative policy gate

Every filesystem candidate is deny-by-default until its promoted operation defines and tests all applicable controls:

- canonicalize paths before authorization and constrain them to explicitly configured roots;
- define whether each path is read, write, metadata-only, or a SpatialAnalyzer-managed embedded reference;
- reject traversal, unexpected reparse points, disallowed network paths, and path-type mismatches;
- make overwrite behavior explicit and default it to false; never infer overwrite consent from an SDK sample value;
- establish extension, size, count, duration, and temporary-file limits appropriate to the format;
- specify partial-output and retry behavior, including cleanup after cancellation, worker termination, or SA failure;
- redact customer paths, report contents, geometry, credentials, and proprietary data from default logs and CI artifacts;
- use isolated disposable directories and nonproduction fixtures for protected real-SA tests.

Administrative candidates that alter language, units, automatic backup, logging, wildcard interpretation, working frames, or global view state also remain deny-by-default. Promotion requires an operation-specific authorization policy, before/after state capture where possible, deterministic restoration or disposable-collection cleanup, and concurrency tests proving the worker's serialized command boundary.

Network and arbitrary external-process operations have no approved candidates in this review. Reintroducing one requires a separate product-scope and security decision rather than merely adding a risk flag.

The committed category shards remain the machine-readable source of truth for every command-level decision and risk assessment.
