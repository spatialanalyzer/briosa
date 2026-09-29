# Vector construction

The typed vector RPCs construct a group from an area profile check or relationship, compare two point groups into a vector group, and construct one vector in working coordinates by begin/delta or begin/direction/magnitude. Group and vector references remain in their distinct exact-target MP name domains. The area profile operation requires nonempty reference vectors and vector groups plus a result group name. Radius and tolerance default to zero.

The group comparison requires a vector group name and both point groups. Its three tolerances default to zero; it returns vector count, RMS deviation, maximum absolute deviation, and average deviation. Both working-coordinate forms require a vector group and begin and delta/direction vectors. The new vector name defaults to empty; the begin/delta negative-magnitude flag defaults to false, and signed magnitude defaults to zero.

These operations mutate application state and have unsafe replay status. Portable generated-client and fake-worker tests check MP binding order, defaults, input validation, comparison output retrieval, and capability registration. Licensed execution on SpatialAnalyzer 2026.1.0529.7 remains unverified. A protected run should exercise representative nonempty groups and both vector modes while recording exact SDK/SA identities and MP outcomes.
