# Portable generated-surface conformance

The portable conformance gate proves that every operation in an exact-target supported catalog has an executable, vendor-independent contract. It runs on an ordinary Windows CI runner and never installs, launches, activates, or connects to SpatialAnalyzer.

## Generated manifest

`generated/conformance/sa/<target>/manifest.json` is generated from the supported command catalog, the exact-target SDK binding registry and review, and the value-family evidence catalog. Its schema is `conformance/schemas/v1/manifest.schema.json`. Every input file, including each listed operation document, is recorded by repository-relative path and SHA-256 fingerprint.

The manifest records:

- the exact supported operation set, request/result identities, MP step, execution scope, replay safety, policy metadata, and ordered SDK bindings;
- positive and negative request, output, readiness, policy, deadline, cancellation, crash, hang, malformed-response, disposition, replay, and workflow-isolation scenario identities for each operation;
- every usable SDK method/value-family row;
- every implemented public/private value family;
- every reviewed enum member and unknown-enum negative case;
- every structured type and its singular, optional, and repeated field shapes; and
- both the accepted and fail-closed case for every exact `method|inventory_key|sdk_order|family` assignment.

Case IDs are stable evidence-derived identities. Adding, removing, or changing catalog or binding evidence must regenerate the manifest. Duplicate identities, an unsupported method/family row, or a shared SDK method without its exact reviewed command assignment fails generation.

## Executable contract

Catalog generation emits one `CatalogOperationConformanceBinding` per supported operation. It exposes the generated request mapper, immutable worker command, output contracts, result mapper, capability descriptor, and the normal `CatalogOperationExecutor` path. Tests discover these bindings rather than maintaining a second hand-written operation list.

The server conformance suite creates every protobuf request through descriptors, verifies the exact command and output contract, runs successful typed result mapping, and table-drives all generated operation scenarios through deterministic fakes. Negative cases preserve the difference between not-started and started-outcome-unknown execution, worker recovery and replay guidance, readiness, policy denial, MP/result/getter failures, and malformed worker responses. Returned unknown collection object/item enum values fail closed as data loss.

For the initial Wave 2 point lifecycle, the manifest also proves exact `vector3`, point-name, point-name-list, and reviewed logical-default setters; required-field rejection; global-state-mutation metadata; and unknown replay safety. These operations have no output arguments, so an output-getter-failure case is not applicable; MP-result retrieval failure, malformed response, deadline, cancellation, crash, hang, policy, and readiness cases remain generated and executable.

The global manifest rows are not count-only documentation. Generator tests require exact set equality with the binding and value evidence. Existing worker completeness tests execute the corresponding method/family, private-value, enum-literal, structured-value, and command-assignment contracts against the production adapter seam and private control protocol. A new evidence row therefore fails until both generation and its executable fake path exist.

## Running the gate

After building `Briosa.Generator`, run:

```powershell
./eng/Verify-PortableConformance.ps1 -NoBuild
dotnet test tests/Briosa.Generator.Tests/Briosa.Generator.Tests.csproj -c Release --filter FullyQualifiedName~PortableConformanceGeneratorTests
dotnet test tests/Briosa.Server.Tests/Briosa.Server.Tests.csproj -c Release --filter FullyQualifiedName~PortableConformanceTests
```

The verifier schema-checks the committed manifest, verifies its exact catalog operation set, identities, counts, and evidence hashes, generates twice in clean temporary roots, and compares both generations with the committed bytes. `Verify-FullSurface.ps1` includes this surface and its semantic verifier in the ordinary-CI umbrella.

To regenerate intentionally:

```powershell
dotnet run --project tools/Briosa.Generator -c Release -- portable-conformance-generate . .
```

Generated artifacts must not be hand-edited.

## Boundary of the claim

This gate proves Briosa-owned protocol validation, mapping, dispatch, policy, and failure behavior against fakes. It is not a SpatialAnalyzer emulator and does not prove that an MP command has the documented effect in a licensed application. Operation-specific real-SA behavior, installation compatibility, and disposable-file or device effects remain protected-runner work requiring explicit authorization and reviewed fixtures.
