# SA 2026.1.0529.7 default decisions

Issue [#82](https://github.com/spatialanalyzer/briosa/issues/82) reviewed all 314 inactive convenience-default candidates produced by the exact-target evidence workflow. None is activated as a Briosa default. Every affected public input remains required, omission is rejected, and the matching SDK setter receives only an explicit request value.

This decision does not claim that SpatialAnalyzer has no internal defaults. A View SDK Code value proves an exact setter/value sample, not the public meaning of omitting a Briosa field. ObjectiveSA is pinned secondary evidence from version `2024.1.5.1`; it cannot establish SA `2026.1.0529.7` omission semantics by itself.

## Reviewed evidence

- Accepted proposal SHA-256: `1cb9d4e52b9371cdfbb3610edcb08f619824462a7eff4eb7ad3e3d2ed2c48556`
- Source Briosa commit: `0d7ee3808dba1389e754d8955e3e7b46a853a233`
- Source default-review queue SHA-256: `95cf36522c05952feeb882ed6e91c8803df5d08892ddc94b977afcbeaa666b00`
- Value catalog SHA-256: `db4bcf2d5255b41e834247effd5ee8ff04ba16ed5aa19b518fdd99ae3dca7834`
- Binding registry SHA-256: `41f9beb505eaca647134c263c8d65af92ff13c0de5b4a637f794e87660891aaf`
- ObjectiveSA commit: `324c73b8e172868b4ccb4a0121e3bd1cbc520c5c`
- ObjectiveSA source manifest SHA-256: `d6107f1e10d2c957198c3cb082368033117e7e2ed2907eafb9eadc40607d295b`

The review covers 282 exact-target sample-only candidates, 16 exact-target versus ObjectiveSA conflicts, and 16 ObjectiveSA-only candidates. It includes 276 mutating and 38 read-only operation inputs.

## Decision batches

| Batch | Decision basis | Inputs |
| --- | --- | ---: |
| 1 | Automatic safety rejection: empty identity/list placeholders or an environment-specific endpoint | 13 |
| 2 | Non-null prior-release-only candidates without exact-target samples | 4 |
| 3 | Non-automatic exact-target versus prior-release conflicts | 15 |
| 4 | Exact-target samples whose ObjectiveSA mappings also require the parameter | 196 |
| 5 | Remaining exact-target samples without a required ObjectiveSA mapping | 86 |
| **Total** | **Retain required input with no Briosa default** | **314** |

The exact per-argument record is the disposition ledger and its generated report. Each `reviewed_no_default` resolution retains candidate values, evidence state, reason codes, and the #82 decision reference. The generated pending queue must contain zero entries. Raw vendor documentation, generated code, and proprietary binaries are not committed.
