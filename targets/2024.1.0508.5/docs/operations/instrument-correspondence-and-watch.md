# Instrument correspondence, checks, and watch operations

SpatialAnalyzer 2024.1.0508.5 exposes these handwritten `InstrumentOperations` routes through Briosa:

| Purpose | RPCs |
| --- | --- |
| Correspondence | `AutoCorrespondClosestPoint`, `AutoCorrespondWithProximityTrigger` |
| Instrument checks and measurement | `Collimation`, `DriftCheck`, `EdgeScanMeasurement` |
| Watch windows | `WatchClosestPoint`, `WatchPointToEdge`, `WatchPointToObjects`, `WatchPointToPoint`, `WatchPointToPointWithViewZooming` |

The RPCs require typed instrument, point, object, and group references. Collimation requires supported tilt and baseline choices. Watch-to-edge and watch-to-objects require projection options. Briosa checks required inputs before enqueue and preserves the target MP labels, setter order, and defaults. `DriftCheck` returns maximum and RMS errors, whether an instrument was added, and the returned instrument ID. Watch window location and size default to zero; the view-zooming route defaults to update rather than close.

These commands may measure or alter application state, and all have unsafe replay semantics. If a response is lost after enqueue, check SpatialAnalyzer state before deciding whether to repeat the action.

Validation: target server builds, focused fake-worker mapping tests, and a generated-client/server smoke test pass without SpatialAnalyzer. The exact-target bindings match the committed inventory. Licensed SA behavior has not been exercised by these portable tests.
