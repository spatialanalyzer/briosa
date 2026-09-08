# Mirrorable release catalog — preview contract

- Date: 2026-09-08
- Status: Implementation candidate for read-only discovery and package previews.
- Scope: Shared release metadata owned by `briosa`; not authorization to install.
- Related: [exact-target products](exact-target-product-model.md),
  [distribution](validation-and-distribution.md), and
  [Community Discussion #8](https://github.com/orgs/spatialanalyzer/discussions/8).

## Purpose and ownership

Expose ordinary immutable release artifacts through a small JSON catalog that
can be copied unchanged to Artifactory or an offline directory. The installer
selects the catalog location; it must not scrape GitHub releases, infer supported
products from installed SA versions, or embed public artifact URLs in mirrored
metadata. The schema lives in [schemas/releases/v1](../../schemas/releases/v1/catalog.schema.json).

Existing server distributions retain their target-qualified ZIP, adjacent
checksum, and schema-2 provenance manifest. This catalog adds discovery metadata;
it does not change package contents, protobuf names, runtime identity checks,
SDK compatibility, or client runtime selection.

## Fields and interpretation

The root contains `schemaVersion: 1` and `packages`. Each package has an opaque
`id`, `component` (`server` or `installer`), semantic `version`,
`runtimeIdentifier`, and `artifact`. A server additionally requires the exact
`spatialAnalyzerTarget` and `provenance`; installer entries must not carry an SA
target. Installer and server versions remain independent.

An artifact or provenance reference contains a relative `path`, byte `size`,
and lowercase `sha256`. Server provenance points to the existing external
`.provenance.json`. An installer provenance reference is optional until its
packaging contract exists. File extensions on installer entries do not select
an executable installer technology or authorize launching a file.

These are catalog declarations, not observations of downloaded packages or local
SA installations. Do not infer SDK compatibility or MP readiness from membership.
An empty catalog, or no entries for a selected component, is valid. Do not
automatically select the greatest version, fall back to another component's
catalog, or prune an older product from a machine.

## Structural and location validation

Reject unknown/duplicate JSON properties, unsupported schemas, malformed fields,
duplicate IDs, and duplicate `(component, version, SA target, runtime identifier)`
coordinates. IDs compare case-insensitively to avoid Windows ambiguity. Versions
and exact SA release strings compare exactly, without normalization. An artifact
path reused anywhere in a catalog must declare the same size and digest.
Integer fields use integer JSON tokens, without fractional or exponent notation.
JSON Schema alone does not express this reader constraint or duplicate-property
and cross-entry consistency checks.

The preview caps catalogs at 1 MiB and 1,000 entries. In addition to the JSON
schema, every reference path must have nonempty segments no longer than 128
characters, no trailing dot, and no reserved Windows device basename (`CON`,
`PRN`, `AUX`, `NUL`, `COM1`–`COM9`, `LPT1`–`LPT9`). Allowed ASCII characters and
relative syntax exclude absolute URLs, drive/share roots, backslashes, percent
encoding, query strings, fragments, and `.`/`..` traversal segments.

Resolve references against the catalog's containing directory. This keeps
server and installer metadata and their payload references inside the selected
mirror layout. Catalog GET redirects are rejected in the initial reader; point
configuration at the final permitted catalog URL. No redirect or failed read
triggers public fallback. Each explicit refresh uses its selected source and
has no cross-source cache. Authentication-required responses are reported;
automatic HTTP credentials, cookies, and an authentication plugin are not
introduced by this preview.

Future payload acquisition must validate locations again and handle filesystem
junctions, redirects, and authorization at the point of access. Lexical path
validation in a preview is not proof of the eventual destination of a file read.

## Integrity and trust boundary

The reader hashes the received catalog bytes so a preview can identify its exact
metadata snapshot. Artifact hashes and sizes are *declared* until payloads are
downloaded and checked. Neither a self-declared hash nor successful HTTPS access
establishes approved publisher identity. The reader reports publisher verification
as not performed, and this increment has no install, extract, launch, or update
application action.

Signing keys, trusted publishers, approval policy, revocation/offline behavior,
catalog freshness/rollback rules, and payload/provenance verification must be
settled before actionable installation plans. This unsigned preview schema does
not silently decide a production trust policy.

## Producing a server catalog

From the repository root, after existing package verification has produced its
artifacts, run:

```powershell
./eng/New-ReleaseCatalog.ps1 -ArtifactDirectory ./artifacts/release -OutputPath ./artifacts/release/catalog.json
```

The producer reads only matching target-qualified server provenance files and
ZIPs, verifies the adjacent ZIP checksum, and calculates reference sizes/hashes.
Protocol and client-conformance assets are not server packages. Put the output
catalog beside or above its artifacts so all references remain relative children.
Moving that complete directory tree preserves the catalog bytes.

The script does not fetch assets, inspect SA, produce a signature, or modify a
GitHub release. Integrating it into public release publication and maintaining
the public catalog remain subsequent work. Test it with
`./eng/Test-ReleaseCatalog.ps1`, which uses invented ZIP/manifest fixtures only.
To exercise the matching installer consumer too, pass
`-ConsumerCliAssembly <path-to-Briosa.Installer.Cli.dll>` from a built installer
checkout. This reads a generated offline mirror catalog without acquiring its
payloads and checks the reader's unverified metadata status.
