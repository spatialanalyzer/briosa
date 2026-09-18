# Installation selection and behavioral compatibility

- Status: Accepted by the maintainer on 2026-09-18.
- Delivery: #209, #210, #211.

## Compatibility

Each exact-SA product owns compatibility.json. The initial contract is major 1,
revision 0. A client requires a major and minimum revision. A server is compatible
when the major matches and its revision meets the minimum. Breaking existing
lifecycle, presence, errors, ownership, outcomes, replay, or operation behavior
requires a new major. Additions preserve all earlier revisions in that major.
Enabled capabilities remain a separate runtime gate.

Exact SA target and executable RID are mandatory. Protocol package remains
briosa. Build artifact hashes stay pinned. Runtime version/source identity must
match the selected installation, not the client's generation artifact. Contract
declarations are promises gated by packaged conformance; future releases are not
represented as already tested.

Server and protocol manifests use schema 3 with a compatibility object containing
major and revision (uint32, major positive). Discovery exposes the same coordinates
in GetServerInfoResponse.compatibility, field 11, before any SDK activation.

Schema-2 server 0.6.1 from source
32a3b56ba4ae31ea5ec6ec3b2aa051eb61c866aa is the sole initial legacy exception,
scoped to the two existing exact targets and contract requirement 1.0. Other
missing metadata fails closed. Published exact-pinned clients retain their pins.

## Windows installation index

The installer maintains the 64-bit Registry view under
HKCU\\Software\\Briosa\\Installations and HKLM\\Software\\Briosa\\Installations.
Each installation has a child key named by its installation ID and a REG_SZ
Registration value containing JSON with these exact fields:

- schemaVersion: 1
- installationId: 64 lowercase hexadecimal characters
- productDirectory: absolute committed product directory
- packageId: receipt package ID
- serverVersion: semantic version
- spatialAnalyzerTarget: exact SA release
- runtimeIdentifier: win-x64

The ID is lowercase SHA-256 of UTF-8 normalized absolute productDirectory: replace
backslashes with slashes, trim trailing slashes, and lowercase ASCII A-Z. Scope
comes from the hive, not JSON. Validate the record against its committed receipt,
payload manifest and required files. Transactions and reparse points are ineligible.
The index is discovery evidence, not publisher authentication.

Commit files before registration. Recover/rescan reconciles incomplete registration
and removes only stale entries belonging to that store. Report Registry failure
without undoing a committed package. Canonical stores remain discoverable without
registration; custom stores use registration or explicit search roots. No global
active server or consuming-application inventory is introduced.

## Client selection

Construction is inert. Explicit discover/resolve calls only read local evidence.
Executable path and installation ID are mutually exclusive. Paths must be absolute;
an invalid explicit selection never falls back. Automatic resolution:

1. Collect registered installations, committed canonical stores, configured roots,
   and eligible client-local/legacy layouts, with bounded metadata/enumeration.
2. Validate/deduplicate; filter exact target, RID, contract, exclusions, prerelease
   policy, allowed scopes, exact version and inclusive minimum/exclusive maximum.
3. Choose highest eligible SemVer precedence. Prereleases require opt-in. Conflicting
   provenance for the same release is ambiguous. Identical copies prefer machine,
   user, portable scope, then normalized ordinal path. Ignore build metadata for
   SemVer precedence.
4. Revalidate before launch; compare live discovery against the selected manifest
   before SDK/SA actions. Do not fall back after runtime admission begins.
5. Freeze selection for the session and its recovery; resolve again on fresh start.

Elevated automatic discovery excludes user-writable candidates. Explicit paths are
deliberate choices but cannot bypass compatibility or exact-target gates. Registry
claims cannot relax scope policy. Detailed diagnostics expose paths only on explicit
request; routine logs remain low-sensitivity.

BRIOSA_SERVER_PATH requires explicit legacy opt-in. Direct selectors take precedence.
An opted-in environment override is an explicit selection and fails closed. Diagnostics
distinguish absent, incompatible, damaged, ambiguous and invalid explicitly selected
installations from a running-server identity mismatch.

## SA application and runtime

Resolve the exact SA application from an explicit executable or corroborated
installed-product/file evidence. Ambiguous copies require explicit selection.
The default layout is only a candidate requiring file-version evidence. Installed
application, activated SDK and connected SA remain independent claims; existing
identity/attestation and bounded execution-readiness gates are unchanged.
Discovery never activates COM or runs registration commands. Side-by-side installation
does not imply concurrent execution. No automatic SDK registration changes, competing
process shutdown, cross-client leases, or MP retries are introduced.

## Evidence and migration

Server-owned fixtures enforce language parity. The matrix separates declared ranges,
tested exact pairs, exclusions and licensed validation. Retain every released
contract-aware client in conformance until a support-window policy is accepted.
Embedded compatibility policy works offline; newly discovered exclusions require a
client or explicit application-policy update.

Installer readers support both manifest generations before modern servers ship.
Retain older immutable products for exact-pinned clients. Rollback is per application;
never relabel a published artifact or change a running session's selection.

