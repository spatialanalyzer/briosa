# SA 2026.1.0529.7 binding reference

> Reference snapshot only. This document does not define public support or require complete protocol coverage.

The retained binding registry records 151 SDK setter/getter methods reconciled between View SDK Code observations and the committed exact-target interop surface. It remains useful for locating exact call names and reviewed marshaling families when implementing a handwritten operation.

Current support is narrower: only the calls required by implemented operations are product claims. `GetWorkingDirectory` uses `SetStep`, `ExecuteStep`, `GetMPStepResult`, and `GetStringArg("Directory", ...)`; `GetIThCollectionName` additionally uses `SetIntegerArg("Collection Index", ...)` and `GetCollectionNameArg("Resultant Name", ...)`.

Reusable worker codecs and adapters remain directly tested for their own contracts, including scalar values, identity/reference lists, specialized structured values, `VariantWrapper` list marshaling, malformed returns, and getter failures. Those internal tests do not make an MP command public.

Historical registry coverage fields and generated reports belong to the retired catalog pipeline. See [the SDK binding reference guide](../../../development/sdk-binding-registry.md) and [ADR 0024](../../../../../../docs/architecture/0024-handwritten-mp-operation-vertical-slices.md).
