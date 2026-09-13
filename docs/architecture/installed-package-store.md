# Installed package store contract

- Date: 2026-09-12
- Status: Implemented by the installer; client discovery integration remains separate.
- Related: [release catalog](release-catalog.md), [exact-target products](exact-target-product-model.md).

The shared layout exposes complete, independently versioned products. It contains
no record of consuming clients, engineering projects, leases, or application impact.
Application teams own selecting the exact server distribution. This document does
not implement a new client resolver or relax runtime SDK/SA identity gates.

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
The existing schema-2 manifest must match exact identity, `protocolPackage: "briosa"`,
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

## Installer selection and bootstrap

`active-installer.json` has `schemaVersion: 1` and an installed installer `id`.
Activation verifies files and atomically replaces that pointer. The launcher verifies
the selected package on each launch. Invalid selected state fails closed; only
absence of a selection permits the adjacent initial installer fallback.

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
