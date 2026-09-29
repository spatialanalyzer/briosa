# Surface extraction and gradients

This product exposes typed RPCs to construct geometry from a nonempty surface list, polygonize point clouds, select surface faces to construct objects, and retrieve a projected point with its normal, U, and V gradient vectors.

`ConstructGeometryFromSurfaces` omits the reference frame and destination collection setters when their fields are absent; minimum and maximum diameter default to zero, and the base name defaults to `Geometry Object`. It retrieves a list of created geometry objects. Polygonization requires a point-cloud list, an explicit point-of-view or working-frame orientation, and a mesh name; grid resolution defaults to zero.

For this **2024.1.0508.5** target, `ConstructObjectsFromSurfaceFacesRuntimeSelect` requires seven explicit Boolean choices, in MP order: planes, cylinders, spheres, cones, lines, points, and circles. It does not use the 2026 object-type text and point-offset bindings. The gradient RPCs retrieve four vectors in MP order. The edge form requires a B-spline edge and offset direction; offset distance defaults to `0.01`.

Portable generated-client and fake-worker tests verify the bindings, optional omission, output retrieval, defaults, invalid inputs, and registration. Licensed SpatialAnalyzer execution remains unverified for this migration. An opt-in protected run should cover each geometry family, both mesh orientations, representative surface-face choice combinations, gradient retrieval with and without the optional vector-line flag, and edge offset behavior. Record the exact connected SA and activated SDK versions and MP outcomes without publishing proprietary geometry or paths.
