# Container and unit values for SA 2026.1.0529.7

Issue [#56](https://github.com/spatialanalyzer/briosa/issues/56) implements the approved non-scalar value families beyond the identity/reference and vector/tolerance families. The public and worker contracts contain only language-neutral values. VariantWrapper, rectangular CLR arrays, COM objects, and vendor interop types remain inside the final worker adapter.

## Exact-target values

The SA 2026.1.0529.7 target defines:

- DoubleArray, which preserves order and permits an empty array;
- StringList used with the distinct edit_text worker kind so it cannot fall back to SetStringRefListArg;
- Transform, containing exactly 16 row-major values for the SDK's required double[4,4];
- WorldTransform, containing a 4x4 transform and an explicitly present scale factor;
- RgbColor, whose three protobuf uint32 channels are validated as bytes;
- FileReference, which preserves both the path and embedded_file flag, including empty/false values;
- Font, containing an explicitly present name, byte-sized point size, and RGB color; and
- typed angular, distance, and temperature unit enums.

The unit enums map to the exact strings accepted by this target. For example, degrees/minutes/seconds maps to Deg:Min:Sec, US survey feet maps to US Survey Feet, and Celsius maps to Celsius. UNSPECIFIED, unknown enum values, incomplete colors/fonts/file references, and transforms with any dimensionality other than 4x4 are rejected before SDK execution.

## COM boundary

Array-like setters create a VariantWrapper only immediately before the exact SDK call. Double arrays wrap double[]; edit text wraps object[] containing strings; transforms and world transforms wrap double[4,4]. Getter buffers use the same wrapper convention. Returned values are accepted only when their exact CLR shape is correct:

- the reported double-array length must be non-negative and match the returned double[];
- every edit-text element must be a string;
- transforms must be rectangular 4x4 double arrays; and
- file results preserve the getter's path and embedded-file Boolean separately.

A getter returning false, a malformed returned container, or a length/dimensionality mismatch produces an unretrieved output and never exposes a partial or default-shaped result. Setter false prevents ExecuteStep.

The wrapper convention and semantic interpretations were cross-checked against the manually reviewed ObjectiveSA wrapper for an earlier SA release. The callable names and CLR parameter signatures are taken from Briosa's committed SA 2026.1.0529.7 interop manifest. Portable fakes cover the exact calls, wrapper inputs, valid and default-like values, malformed return values, and getter failures. Real-SA cross-family conformance remains work for the protected licensed runner.

## Classification and logging

Classification belongs to each catalog argument because command context can make the same primitive shape more sensitive. Apply these starting rules during command curation:

| Value | Default catalog classification |
| --- | --- |
| file reference | path |
| transform or world transform | geometry |
| units and numeric arrays representing measured values | measurement |
| free-form edit text or opaque arrays | proprietary |
| RGB color and font presentation settings | non_sensitive, unless command context requires stricter treatment |

When evidence is ambiguous, choose the stricter applicable classification and mark the command for review. Do not infer that an array contains measurements merely from its CLR type.

Briosa's audit boundary is stricter than the table: it never logs raw arguments or results, including values classified as non_sensitive. Paths, geometry, measurements, and proprietary content must not appear in normal, debug, or trace events. Generated reference documentation may publish the classification label, not the runtime value.