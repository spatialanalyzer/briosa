# Frame construction commands

The [construction protobuf contract](../../proto/briosa/construction_operations.proto) defines these typed `ConstructionOperations` RPCs for creating frames:

| Inputs | RPCs |
| --- | --- |
| Explicit transform or guided creation | `ConstructFrame`, `ConstructFrameFromTransformInWorld`, `ConstructFrameWithWizard` |
| Existing frames, objects, instruments, and robot links | `ConstructFrameAverageOfOtherObjectFrames`, `ConstructFrameCopyAndMakeLeftHanded`, `ConstructFrameOnObject`, `ConstructFrameOnInstrumentBase`, `ConstructFrameAtRobotLink` |
| Points, axes, and planes | `ConstructFrameAtPointWithWorkingZAndClockedAxis`, `ConstructFramePickOriginAndPointOnXAxisClockZAlongWorkingZ`, `ConstructFrameThreePoints`, `ConstructFrameThreePlanes`, `ConstructFrameKnownOriginObjectDirectionObjectDirection`, `ConstructFrameFromPointMeasurementProbingFrames` |
| Mirror cube measurement | `ConstructMirrorCubeFrame` |

References and transform values required by an MP step must be supplied. A transform contains exactly 16 values. Optional frame-name arguments are omitted from the SDK command when absent. The three-point construction method and axis choices are limited to the exact values in the protobuf contract. The wizard defaults `wait_for_completion` to true; mirror-cube construction defaults to using marked mirror shots and a 90-degree nominal cube-face angle. `ConstructMirrorCubeFrame` returns total angular error when the MP command completes successfully.

These commands mutate SpatialAnalyzer state and require an MP-ready, single-tenant server. Portable fake-worker and generated-client tests cover the typed bindings and result mapping. This migration has **not** executed these commands against a licensed SpatialAnalyzer 2024.1.0508.5 instance. Deadline or cancellation after enqueue can leave completion unknown; inspect the execution disposition before considering replay.
