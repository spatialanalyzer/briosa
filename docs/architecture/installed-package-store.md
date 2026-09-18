# Installed package store contract

- Date: 2026-09-12
- Status: Installer layout implemented; first-party client discovery specified in #191.
- Related: [release catalog](release-catalog.md), [exact-target products](exact-target-product-model.md).

The shared layout exposes complete, independently versioned products. It contains
no record of consuming clients, engineering projects, leases, or application impact.
Application teams own their server selection constraints. Contract-aware clients
use [installation selection and compatibility](installation-selection-and-compatibility.md)
and preserve runtime SDK/SA identity gates. Published older clients keep exact pins.

## Roots and immutable products

Canonical Windows roots are `%LOCALAPPDATA%\Briosa\Packages` for the current user
and `%PROGRAMDATA%\Briosa\Packages` for all users. An explicit custom store is
caller-managed. Canonical machine stores require protected administrative ownership;
ordinary accounts have read/execute access, and mutations require authorized elevation.

```text
<store>/
  store.lock
  catalog-history.json
  active-installer.json                 # optional
  products/<catalog-id>/
    receipt.json
    payload/<complete unpacked product>
  transactions/<transaction-guid>/      # temporary, never a discoverable product
    journal.json
    new/
    old/
```

IDs use safe single Windows path segments. Enumerate committed receipts, not
transaction directories. Payloads are immutable product files, not a place for
settings, jobs, credentials, or logs. Discovery conveys installation evidence only;
runtime checks must still establish readiness.

Receipt schema 1 records `package` (the exact catalog package record),
`catalogSha256`, `publisher` (fingerprint, issuedAt, expiresAt; algorithm fixed by schema),
`installedAt` (UTC timestamp), and `files` (relative path to lowercase SHA-256).
The serialization also carries schemaVersion. Verification requires an exact file
set and matching digests; reparse points are rejected. Receipts/history are locally
owned state backed by filesystem permissions, not independently signed attestation
against a malicious owner of that same store.

One store lock serializes changes. Acquisition verifies the reviewed catalog,
signature, freshness, ZIP/provenance size/digest, archive layout, and embedded manifest.
Commit renames a complete staged product directory into place. Repair requires the
original publisher and exact artifact, preserving the old directory until replacement
commits. Recovery restores the old directory after an interrupted pre-commit repair
or clears abandoned staging. Removal affects one exact product and never terminates
application processes. An active installer or in-use files block removal.

## Product payloads

Server ZIPs contain exactly one root:
`briosa-<version>-sa-<exact-sa-release>-win-x64`.
Schema-2 legacy and schema-3 contract-aware manifests must match exact identity, `protocolPackage: "briosa"`,
and `spatialAnalyzerBundled: false`. Required entry points are
`Briosa.Server.exe` and `Briosa.Worker.exe`; external provenance matches the embedded
manifest byte-for-byte. No vendor binaries are added by the installer.

Installer ZIPs contain exactly one root:
`briosa-installer-<version>-win-x64`. Required entry points are
`Briosa.Installer.exe`, `Briosa.Installer.Cli.exe`, and `Briosa.Launcher.exe`.
Its manifest has schemaVersion 1, component installer, artifactName, briosaVersion,
runtimeIdentifier win-x64, and currently selfContained true. Adjacent provenance,
when declared, matches the embedded manifest. The published distribution includes
runtime licenses and contains no SA target. Versioning is independent of servers.

The installer runs no package scripts/executables during acquisition or extraction.
Selecting a verified installer for launch is a separate action.

## Client server discovery

The rules below describe published exact-pinned clients through 0.1.1. New
contract-aware clients follow [the accepted selection contract](installation-selection-and-compatibility.md),
which supersedes the exact-build lookup and ambient environment-variable precedence.
Schema-3 manifests add a `compatibility` object; old manifests are eligible only
through the specified legacy exception. Registry indexing leaves receipts and
filesystem transactions authoritative for committed installation state.

First-party clients select the first eligible executable in this order:

1. `BRIOSA_SERVER_PATH`, pointing to `Briosa.Server.exe`.
2. The client's existing local `briosa-server/Briosa.Server.exe` location
   (the application base directory for .NET; the client module directory for Node.js
   and Python).
3. The canonical current-user store.
4. The canonical all-users store.
5. The legacy current-user path
   `%LOCALAPPDATA%/Briosa/servers/<briosa-version>/sa-<exact-sa-release>/Briosa.Server.exe`.

Managed candidates are exactly
`<store>/products/briosa-<briosa-version>-sa-<exact-sa-release>-win-x64/payload/Briosa.Server.exe`.
Use the server version, exact SA target, and source revision pinned by the client
protocol artifact, independently of the client package version. Do not enumerate
versions, select `latest`, search transaction directories, or cross SA targets.
An unavailable canonical root contributes no candidate; never resolve it relative
to the working directory. .NET obtains roots from Windows special folders;
Node.js and Python use `LOCALAPPDATA` (falling back to the user's `AppData/Local`)
and `PROGRAMDATA` (no relative fallback).

A managed candidate is eligible only when:

- `receipt.json` is a JSON object with schemaVersion 1 and a package object whose
  id, component (`server`), version, runtimeIdentifier (`win-x64`), and
  spatialAnalyzerTarget exactly match the requested product;
- `payload/manifest.json` is a JSON object with schemaVersion 2 and matching
  artifactName, briosaVersion, spatialAnalyzerTarget, runtimeIdentifier,
  sourceRevision, protocolPackage (`briosa`), and spatialAnalyzerBundled (`false`);
- the payload contains files named `manifest.json`, `Briosa.Server.exe`, and
  `Briosa.Worker.exe`, each recorded in the receipt's files object with a lowercase
  64-character hexadecimal digest.

Missing, unreadable, malformed, or mismatched candidates are skipped. If none is
eligible, startup reports `server-distribution-not-found`. Explicit, client-local,
and legacy paths retain their existing executable-file lookup and subsequent
runtime compatibility checks; they do not require Installer receipts. Merely
setting an unavailable explicit path continues to permit the remaining fallbacks.

These are discovery checks for a committed installation, not package integrity or
publisher verification. They do not rehash the complete payload or authenticate
locally owned receipt metadata. Use Installer package verification/repair for
damaged installations. Catalog trust, freshness, acquisition policy, and protected
machine-store permissions remain Installer responsibilities. Runtime server
identity, activated SDK identity, connected SA identity, and execution readiness
remain separate checks; installation evidence establishes none of them.

Custom stores are not searched automatically. Set `BRIOSA_SERVER_PATH` to the
desired custom product's `payload/Briosa.Server.exe`. Clients do not read Installer
settings, active-installer selection, or catalog history to change server selection.

See the [client discovery validation record](client-server-discovery-validation.md)
for the released-package smoke checks and their limits.

## Installer selection and bootstrap

`active-installer.json` has `schemaVersion: 1` and an installed installer `id`.
Activation verifies files and atomically replaces that pointer. The launcher verifies
the selected package on each launch. Invalid selected state fails closed; only
absence of a selection permits the adjacent initial installer fallback.

A completed conventional installer setup may clear an older selected installer
under the same store lock so the newly installed adjacent version takes effect.
It preserves equal/newer selections and every downloaded product, rejects invalid
selection state, and respects policy. This is an explicit setup action, not a
launcher-time version floor: users can still deliberately select an older version
afterward. Uninstalling the application does not remove the package store or settings.

Settings remain outside payloads. Explicit configuration/store arguments survive
restart. Selecting an installer never upgrades/removes server packages. An older
installed installer can be deliberately selected, subject to configuration-schema
compatibility; older parsers must reject unsupported settings without rewriting them.

A standalone launcher can be refreshed atomically from the verified selected
installer, including its bundled runtime. Managed payload directories cannot be
bootstrap update destinations. A locked/protected bootstrap can fail replacement
after selection has committed; this is reported as requiring an authorized retry.
The original standalone distribution's launcher is mutable for this purpose; managed
store payloads remain immutable. Initial distribution trust and Windows code signing
remain distinct from subsequent catalog verification.
