# SA 2026.1.0529.7 binding reference

> Reference snapshot only. This document does not define public support or require complete protocol coverage.

The retained binding registry records 151 SDK setter/getter methods reconciled between View SDK Code observations and the committed exact-target interop surface. It remains useful for locating exact call names and reviewed marshaling families when implementing a handwritten operation.

Current support is narrower: only calls exercised by operations in the handwritten registry are product claims. Each operation source and its exact SDK-order tests record the applicable argument names, value families, and bindings without duplicating a manually maintained operation list in this reference snapshot.

Reusable worker codecs and adapters remain directly tested for their own contracts, including scalar values, identity/reference lists, specialized structured values, `VariantWrapper` list marshaling, malformed returns, and getter failures. Those internal tests do not make an MP command public.

Historical registry coverage fields and generated reports belong to the retired catalog pipeline. See [the SDK binding reference guide](../../../development/sdk-binding-registry.md) and the [operation and protocol model](../../../../../../docs/architecture/operation-and-protocol-model.md).
