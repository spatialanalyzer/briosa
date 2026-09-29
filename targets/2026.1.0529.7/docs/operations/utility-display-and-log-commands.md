# Utility display and log commands

The [utility protobuf contract](../../proto/briosa/utility_operations.proto) defines `GetScreenResolution`, `MoveInstrumentsDragGraphically`, `MoveObjectsDragGraphically`, `ScaleObjects`, `SetViewIdleUpdateFrequency`, `TrimLogFile`, and `WriteToLog`. Check `DiscoveryService/ListCapabilities` for the subset admitted by the running server's command policy.

`GetScreenResolution` defaults an omitted display to `-1` (the primary display) and returns six integer dimensions and positions. The drag commands require a nonempty instrument or object list and interact with SpatialAnalyzer graphics. `ScaleObjects` requires a nonempty object list; its omitted scale factor is `0`. `SetViewIdleUpdateFrequency` defaults the idle count to `0`, `TrimLogFile` defaults entries to keep to `10`, and `WriteToLog` defaults its entry to an empty string. `MoveInstrumentsDragGraphically` and `MoveObjectsDragGraphically` retain `fixture_validation_pending` risk flags.

Portable fake-worker and generated-client tests cover these typed mappings. The seven commands have **not** been validated against a licensed SpatialAnalyzer 2026.1.0529.7 instance in this migration. These operations may change application-wide state; use a single-tenant server and inspect execution disposition before deciding whether to repeat a call.
