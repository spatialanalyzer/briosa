# SA 2026.1.0529.7 command disposition report

This deterministic report summarizes Briosa-authored disposition metadata. It does not republish installed vendor documentation or generated SDK source.

## Inventory

- Path: `../../../inventory/sa/2026.1.0529.7/inventory.json`
- SHA-256: `8a5e16b0fda8ebda70219b2c795af0c1b57004b0b048a32392d5b3253c97e502`
- Commands: 1412
- Disposition shards: 30

## Dispositions

| Disposition | Count |
| --- | ---: |
| `approved_candidate` | 0 |
| `blocked` | 1412 |
| `intentional_exclusion` | 0 |
| `sdk_unavailable` | 0 |

## Review states

| Review state | Count |
| --- | ---: |
| `needs_re_review` | 0 |
| `reviewed` | 0 |
| `unreviewed` | 1412 |

## Categories

| Category | Entries | Approved | Excluded | SDK unavailable | Blocked | Unresolved | Unreviewed | Needs re-review |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| AccumulatorMathOperations | 8 | 0 | 0 | 0 | 8 | 8 | 8 | 0 |
| AnalysisOperations | 189 | 0 | 0 | 0 | 189 | 189 | 189 | 0 |
| CloudAndMeshOperations | 1 | 0 | 0 | 0 | 1 | 1 | 1 | 0 |
| CloudMeshOps | 28 | 0 | 0 | 0 | 28 | 28 | 28 | 0 |
| ConstructionOperations | 278 | 0 | 0 | 0 | 278 | 278 | 278 | 0 |
| DimensionOperations | 1 | 0 | 0 | 0 | 1 | 1 | 1 | 0 |
| Dimensions | 19 | 0 | 0 | 0 | 19 | 19 | 19 | 0 |
| EventOperations | 1 | 0 | 0 | 0 | 1 | 1 | 1 | 0 |
| Events | 5 | 0 | 0 | 0 | 5 | 5 | 5 | 0 |
| ExcelDirectConnect | 17 | 0 | 0 | 0 | 17 | 17 | 17 | 0 |
| FileOperations | 125 | 0 | 0 | 0 | 125 | 125 | 125 | 0 |
| GDT | 40 | 0 | 0 | 0 | 40 | 40 | 40 | 0 |
| GDTOperations | 3 | 0 | 0 | 0 | 3 | 3 | 3 | 0 |
| GoogleSheets | 18 | 0 | 0 | 0 | 18 | 18 | 18 | 0 |
| GoogleSheetsOperations | 1 | 0 | 0 | 0 | 1 | 1 | 1 | 0 |
| InstrumentOperations | 185 | 0 | 0 | 0 | 185 | 185 | 185 | 0 |
| MPSubroutines | 4 | 0 | 0 | 0 | 4 | 4 | 4 | 0 |
| MPTaskOverview | 11 | 0 | 0 | 0 | 11 | 11 | 11 | 0 |
| MSOfficeReportingOperations | 14 | 0 | 0 | 0 | 14 | 14 | 14 | 0 |
| ProcessFlowOperations | 25 | 0 | 0 | 0 | 25 | 25 | 25 | 0 |
| RelationshipOperations | 67 | 0 | 0 | 0 | 67 | 67 | 67 | 0 |
| ReportingOperations | 71 | 0 | 0 | 0 | 71 | 71 | 71 | 0 |
| RobotCalibrationApplianceNodeOperations | 25 | 0 | 0 | 0 | 25 | 25 | 25 | 0 |
| RobotOperations | 33 | 0 | 0 | 0 | 33 | 33 | 33 | 0 |
| ScalarMathOperations | 21 | 0 | 0 | 0 | 21 | 21 | 21 | 0 |
| ScaleBars | 3 | 0 | 0 | 0 | 3 | 3 | 3 | 0 |
| UtilityOperations | 105 | 0 | 0 | 0 | 105 | 105 | 105 | 0 |
| Variables | 41 | 0 | 0 | 0 | 41 | 41 | 41 | 0 |
| Vector Operations | 22 | 0 | 0 | 0 | 22 | 22 | 22 | 0 |
| ViewControl | 51 | 0 | 0 | 0 | 51 | 51 | 51 | 0 |

## Unresolved work by risk effect

| Value | Count |
| --- | ---: |
| `unknown` | 1412 |

## Unresolved work by risk flag

| Value | Count |
| --- | ---: |
| `unknown` | 1412 |

## Unresolved work by value family

| Value | Count |
| --- | ---: |
| `unknown` | 1412 |

## Reason codes

| Value | Count |
| --- | ---: |
| `awaiting_review` | 1412 |

## Blockers

| Value | Count |
| --- | ---: |
| `https://github.com/spatialanalyzer/briosa/issues/43` | 1412 |

## Delivery waves

None.

## Promotion policy

- Only `approved_candidate` entries with `reviewed` state can be promoted into the supported command catalog.
- `unreviewed` and `needs_re_review` entries fail closed and remain absent from runtime capabilities.
- `intentional_exclusion` and `sdk_unavailable` are final non-supported dispositions with Briosa-authored reasons.
- `blocked` identifies a named dependency and cannot silently become supported.
- A changed per-command inventory fingerprint requires re-review before promotion.
