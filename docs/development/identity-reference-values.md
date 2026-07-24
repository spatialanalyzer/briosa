# Identity and reference-list values for SA 2026.1.0529.7

Issue [#55](https://github.com/spatialanalyzer/briosa/issues/55) implements the exact-target identity and reference-list families used by approved SA 2026.1.0529.7 commands. Public protobuf messages expose named components; the delimiter-encoded `VARIANT` arrays required by SpatialAnalyzer remain private to the worker adapter.

## Public and worker model

Structurally different references remain different types even when their primitive components match. For example, `CollectionInstrumentId` and `CollectionMachineId` both contain a collection name and integer identifier, but they select different exact SDK methods and are not interchangeable. Specialized string identifiers such as chart, cloud, collection, frame, vector-group, and view names likewise retain distinct worker value kinds so the adapter cannot fall back to `SetStringArg`.

Composite public values cover:

- collection/instrument and collection/machine identifiers;
- collection/object, collection/group, and collection/vector-group names;
- point and vector names;
- ordered lists of the supported composite references and strings.

Every scalar component uses protobuf explicit presence. A collection-object value requires `object_type`: `SetCollectionObjectNameArg2` accepts it explicitly, while `GetCollectionObjectNameArg` returns it in the comma-delimited object payload alongside the object name. The adapter decodes that SDK representation before the value crosses the worker boundary. Empty lists are valid and list order is preserved.

## Exact SDK boundary

The ObjectiveSA wrapper audit established a consistent argument convention for the implemented SDK surface. A setter Boolean reports whether SA accepted the argument; false rejects the command before `ExecuteStep`. Optional request values are omitted by the generated binding, rather than sent as invented defaults. An argument-getter Boolean reports whether the output was retrieved; false never turns a default CLR value into a successful result. A successful getter may legitimately return an empty string, zero, false, or an empty list. `GetMPStepResult` follows the same retrieval convention, with its separate numeric code determining MP success as documented in ADR 0004.

The adapter calls the reviewed method named by the catalog and fails closed if a value kind and `sdk_binding` disagree. The implementation covers 25 additional exact methods, bringing the testable SDK seam to 39 methods across 24 implemented value families. Reference lists are formatted only at the COM boundary:

- collection/group: `collection::group`;
- collection/object: `collection::object::type`;
- point or vector: `collection::group::name`;
- collection/instrument: `collection::numeric-id`.

Every `ref object` list getter is initialized with an empty `VariantWrapper`, matching the COM Automation calling convention used by the reviewed ObjectiveSA wrapper; a bare CLR array is not passed as the initial out buffer. A returned list is accepted only when every element has the expected shape. A non-string element, wrong component count, or invalid numeric identifier makes the entire output unretrieved; Briosa never returns a partial list. Input components containing the private `::` separator are rejected before `ExecuteStep` because they would be ambiguous.

The delimiter forms are based on the manually reviewed ObjectiveSA implementation for an earlier SA release, while the callable method names and CLR signatures come from the committed SA 2026.1.0529.7 interop manifest. Portable tests cover formatting, parsing, worker serialization, generated mappings, exact dispatch, empty lists, and failure behavior. Real-SA conformance remains a protected-runner check once the licensed environment is available; this issue does not claim that such a run has occurred.

## Adding a command

Catalog arguments use the semantic family IDs recorded in `bindings/sa/2026.1.0529.7/registry.json`, not a convenient primitive substitute. Regenerate catalog artifacts after approval. Do not hand-code a delimiter string in a service, generated binding, public client, or fake SDK implementation.