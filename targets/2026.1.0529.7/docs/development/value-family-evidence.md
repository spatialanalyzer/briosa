# Semantic value evidence

`values/sa/2026.1.0529.7/catalog.json` retains curated semantic evidence for SDK value families, exact-target choices and samples, and a pinned prior-release ObjectiveSA review.

The evidence precedence is:

1. exact SA `2026.1.0529.7` installed documentation, View SDK Code, and committed interop API;
2. controlled exact-target observations; and
3. ObjectiveSA as prior-release secondary evidence.

The pinned ObjectiveSA baseline is repository `spatialanalyzer/ObjectiveSA`, version `2024.1.5.1`, commit `324c73b8e172868b4ccb4a0121e3bd1cbc520c5c`, with the source fingerprint recorded in the snapshot. ObjectiveSA may corroborate wrapper structure and an exact name-and-binding match. It cannot add a choice, default, input, output, or compatibility claim absent from the exact target.

This snapshot is not a public protocol catalog. A value or default candidate does not become API behavior until the handwritten operation pull request reviews it, encodes it in strongly typed source, tests omission and invalid cases, and states exact-target validation status.

Raw vendor documentation, View SDK Code, `Instrument.lst`, ObjectiveSA source, paths, credentials, and licensed data are not copied into Briosa.

Issue #132 retired value-family generation, queues, and freshness gates from ordinary builds. Git history preserves the earlier importer. A future evidence-refresh issue may replace the snapshot without changing the supported API.
