# Surface extraction and gradients

This product exposes typed RPCs to construct geometry from a nonempty surface list, select surface faces to construct objects, and retrieve a projected point with its normal, U, and V gradient vectors.

`ConstructGeometryFromSurfaces` omits the reference frame and destination collection setters when their fields are absent; minimum and maximum diameter default to zero, and the base name defaults to `Geometry Object`. It retrieves a list of created geometry objects.

For this **2024.1.0508.5** target, `ConstructObjectsFromSurfaceFacesRuntimeSelect` requires seven explicit Boolean choices, in MP order: planes, cylinders, spheres, cones, lines, points, and circles. It does not use the 2026 object-type text and point-offset bindings. The gradient RPCs retrieve four vectors in MP order. The edge form requires a B-spline edge and offset direction; offset distance defaults to `0.01`.

Portable generated-client and fake-worker tests verify the bindings, optional omission, output retrieval, defaults, invalid inputs, and registration. Licensed SpatialAnalyzer execution remains unverified for this migration. An opt-in protected run should cover each geometry family, representative surface-face choice combinations, gradient retrieval with and without the optional vector-line flag, and edge offset behavior. Record the exact connected SA and activated SDK versions and MP outcomes without publishing proprietary geometry or paths.

`ConstructPolygonizedSurfaceFromPointClouds` was removed for contract major 3. In licensed session 2, run first and with no dialog open, its Mesh Orientation sent with `SetStringArg` raised "Encountered an improper argument." on this target, and the MP never completed until the server's watchdog ended the worker (probe 11 of the [session 2 record](../testing/evidence/probe-session-2-2024.1.0508.5-2026-10-25.md)). The SDK names a Mesh Orientation setter that the exact-target interop does not have. See [operation retirement](../../../../docs/development/operation-retirement.md).
