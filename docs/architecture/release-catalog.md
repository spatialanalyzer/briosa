# Mirrorable release catalog and publisher signatures

- Date: 2026-09-12
- Status: Implemented by the Briosa Installer local review build.
- Scope: Shared metadata/signature contract; production hosting and key custody remain release work.
- Related: [exact-target products](exact-target-product-model.md),
  [distribution](validation-and-distribution.md), and
  [Discussion #8](https://github.com/orgs/spatialanalyzer/discussions/8).

## Purpose and ownership

Expose immutable artifacts through a small JSON catalog that can be mirrored
unchanged to Artifactory or an offline directory. The installer selects its source;
it does not scrape GitHub, infer support from installed SA versions, or embed public
artifact URLs in mirrored metadata. The shared schema lives in
[schemas/releases/v1](../../schemas/releases/v1/catalog.schema.json).

Existing servers retain their target-qualified ZIP, checksum, and schema-2 provenance.
This adds discovery and publisher verification without changing protobuf names,
runtime identity gates, SDK compatibility, or client runtime selection. Installer
UI/administration instructions belong in `briosa-installer`.

## Metadata

The root contains `schemaVersion: 1` and `packages`. Each package has an opaque
`id`, `component` (server or installer), semantic `version`, `runtimeIdentifier`,
and `artifact`. Servers require an exact `spatialAnalyzerTarget` and `provenance`.
Installers must not carry an SA target; their versions remain independent.

Artifact/provenance references contain relative `path`, integer byte `size`, and
lowercase `sha256`. Installer provenance is optional in the catalog schema; current
packaging emits it. See [the installed-store contract](installed-package-store.md)
for installer ZIP contents and package discovery.

Membership declares availability, not SDK compatibility or MP readiness. Empty
catalogs/components are valid. Do not automatically select the greatest version,
fall back to a different source, or prune older products.

Reject unknown/duplicate JSON properties, unsupported schemas, malformed fields,
duplicate IDs, and duplicate component/version/target/RID coordinates. IDs compare
case-insensitively for Windows; version and SA release strings compare exactly.
A reused artifact path must declare identical size/digest. Integer fields require
integer tokens without fraction/exponent notation. JSON Schema does not express
all these reader checks.

Catalogs are bounded to 1 MiB and 1,000 entries. Paths have nonempty segments at most
128 characters, no trailing dot or Windows device basename (CON, PRN, AUX, NUL,
COM1–COM9, LPT1–LPT9), and only the schema's ASCII characters. Absolute URLs,
drive/share roots, backslashes, percent encoding, query strings, fragments, and
dot traversal are rejected. Resolve references beneath the catalog's directory.

All source requests reject redirects, cookies, and public fallback. Authentication
is explicitly configured per source in the installer; secrets never enter catalog
metadata. An updater override owns its metadata and every payload request.
Payload access repeats path checks and rejects filesystem reparse points.

## Publisher verification

Hash the exact received catalog bytes. Artifact hashes remain declarations until
verified acquisition. HTTPS alone does not establish an approved publisher.
Without an approved public key, browsing is permitted, verification is reported
as not performed, and installation is disabled.

The detached signature is `<catalog-filename>.signature.json`. Its fields are
`schemaVersion: 1`, `algorithm: "RSA-PSS-SHA256"`, lowercase `catalogSha256`,
integer Unix-second `issuedAt`/`expiresAt`, and Base64 `signature`. See the
[signature schema](../../schemas/releases/v1/catalog-signature.schema.json).
An approved distribution, fingerprint-confirmed import, or enterprise configuration
provides the SPKI PEM RSA public key separately. Keys are 3072–8192 bits. Fingerprints
are lowercase SHA-256 of SPKI DER bytes.

Sign with RSA-PSS/SHA-256 over these exact UTF-8 bytes, with LF endings and final LF:

```text
Briosa release catalog signature v1
<lowercase SHA-256 of exact catalog bytes>
<issuedAt as invariant decimal integer>
<expiresAt as invariant decimal integer>
```

Bound the signature envelope to 16 KiB. Reject duplicate/unknown fields, differing
digest/algorithm, invalid signatures, issue times over five minutes ahead of the
local clock, expired catalogs, and validity intervals exceeding 90 days.
Each package store records the greatest accepted issuedAt per exact source/publisher
and rejects older catalogs for acquisition. Repair needs the original publisher
and artifact in a current signed catalog. Deliberately selecting an older package
listed in a current catalog remains valid.

Local clock/history controls are not a global revocation service or transparency
log. Installed packages can be verified and selected offline; current administrator
publisher restrictions still apply to managed installer selection/launch. Clearing
a source key prevents future acquisition, not arbitrary execution of existing files.
Production key custody, rotation, hosting, and initial executable code signing
require release provisioning. No test identity becomes a production trust root.

## Produce, sign, and mirror

```powershell
./eng/New-ReleaseCatalog.ps1 -ArtifactDirectory ./artifacts/release -OutputPath ./artifacts/release/catalog.json
./eng/Sign-ReleaseCatalog.ps1 -CatalogPath ./artifacts/release/catalog.json -PrivateKeyPath <protected-key-file> -ValidDays 7
```

The producer accepts matching server and installer provenance/ZIPs, verifies adjacent
checksums, and calculates sizes/digests. Protocol and client-conformance assets are
excluded. Put the catalog beside/above artifacts so all references are children.
The signer prints only a public fingerprint; protect and never commit its private key.
Neither script fetches assets, inspects SA, or publishes a GitHub release.

Mirror exact catalog/signature/artifact bytes together, refreshing before expiry.
Publish payloads before metadata/signature. A partially updated pair fails closed;
retry after mirror synchronization. Changing the catalog requires a new signature.

`./eng/Test-ReleaseCatalog.ps1` uses invented fixtures to validate production,
determinism, relocation, signature verification, and failure preservation.
Optionally pass `-ConsumerCliAssembly <built-Briosa.Installer.Cli.dll>` to verify
both unsigned and signed interpretation by the installer. Full packaged acquisition
is exercised by `briosa-installer/eng/Test-InstallerPackage.ps1`.
