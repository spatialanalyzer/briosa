# Variables runtime observations

Local licensed validation on 2026-09-25 exercised the redesigned public gRPC
host and private-protocol-23 worker against SpatialAnalyzer 2026.1.0529.7.
The SDK executable hash matched the committed interop provenance, and the
activated SDK and launched application's file versions matched the target.
Each run used a fresh test-owned application and unique temporary variable names.

The final generated-client run completed 22 successful MP calls:

- Set and Get Boolean, Integer, Double, and String Variable.
- Set and Get String Ref List, Vector, and Transform Variable.
- Set Font Variable with its default font settings.
- Set, Add Double to, Get, Get Min/Max, and Clear Named Double List Variable.
- Delete Variable and Delete Variables -- Wildcard Match, restricted to the
  unique test prefix.

One additional Get Named Double List Variable call deliberately checked the
failure after Clear described below. The worker remained ready afterward;
subsequent deletion calls succeeded. The SDK and application closed normally,
and no test-owned process remained. Reference-based object, point, relationship,
report-item, and vector-name variable operations were covered by portable
generated-client tests, not this live scenario. No 2024 runtime claim follows
from these observations.

## Exact wildcard step spelling

The installed documentation title uses `Delete Variables - Wildcard Match`,
but retained View SDK Code evidence uses `Delete Variables -- Wildcard Match`.
The evidence is the `documentation:Variables/DeleteVariablesWildcard.htm` entry
in [the exact-target inventory](../../inventory/sa/2026.1.0529.7/inventory.json),
referencing occurrence 41 of `Variables.txt`.

The previous single-hyphen binding returned MP code `-1` in the live run. Using
the double-hyphen SDK spelling returned MP code `2`. The handwritten operation
and generated-client regression now use that SDK spelling. Its RPC name and
request/result schema are unchanged.

## Getter failure after clearing a double list

A separate direct SDK probe distinguished SDK behavior from Briosa decoding:

| State | ExecuteStep | Retrieved MP code | GetDoubleArrayArg | Reported size | Returned container |
| --- | --- | --- | --- | --- | --- |
| Populated list | true | 2 | true | 2 | double array, length 2 |
| After Clear | true | 2 | false | 0 | object array, length 0 |

Briosa must not turn the failed getter into a successful empty value. The public
call correctly returns `DataLoss`, typed `OutputRetrievalFailure`, and
`Completed` execution evidence. This does not retire the worker or replay the MP.
The private value model can represent successfully retrieved empty arrays, as
portable tests demonstrate; this particular SDK response does not provide that
retrieval evidence. Do not generalize the observation to other list families or
SA releases without exact-target validation.
