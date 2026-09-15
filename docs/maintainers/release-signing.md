# Azure release signing

Setup record: 15 September 2026. This runbook covers the `briosa` server products
and the separate `briosa-installer` application. It does not select product release
versions or authorize a new public release.

## Two independent kinds of signing

| Purpose | Service and identity | Verification |
| --- | --- | --- |
| Windows executable and script publisher identity | Azure Artifact Signing, Public Trust profile `briosa-public` in `briosasigning`, East US | Authenticode chain, publisher **David Lucas**, and RFC 3161 timestamp |
| Catalog and package acquisition through public, enterprise, or offline sources | RSA 4096 key `briosa-catalog` in `briosa-release-kv` | RSA-PSS/SHA-256 signature against the pinned public key, exact catalog digest, and expiry |

Briosa is free and open source. Its Windows publisher is the maintainer's validated
personal identity; this does not represent an incorporated Briosa organization or
endorsement by Hexagon. Windows can display **David Lucas** as the publisher.
Signing establishes publisher identity and integrity; it does not guarantee
SmartScreen reputation or that every enterprise permits execution.

Artifact Signing holds its private keys and manages its short-lived certificates.
Always timestamp signatures using Microsoft's RFC 3161 endpoint. Do not pin a
particular short-lived leaf certificate thumbprint. The independent catalog key
is stable and must not rotate just because an Authenticode certificate changes.
[Artifact Signing overview](https://learn.microsoft.com/en-us/azure/artifact-signing/overview)

## Configured resources and trust root

- Resource group: `briosa-release`.
- Signing endpoint: `https://eus.codesigning.azure.net/`.
- Signing account: `briosasigning`; certificate profile: `briosa-public`.
- Profile was verified **Active**, type **PublicTrust**.
- Key Vault: `briosa-release-kv`, Azure RBAC enabled.
- Key: `briosa-catalog`, RSA 4096, enabled, no automatic key expiry configured.
- Vault soft deletion and purge protection are enabled, with 90-day retention.
- The exact key version and expected Windows publisher are recorded in
  [`eng/signing/azure.json`](../../eng/signing/azure.json).
- The public RSA key is [`catalog-public.pem`](../../eng/signing/catalog-public.pem),
  encoded as SubjectPublicKeyInfo PEM (`BEGIN PUBLIC KEY`).

SHA-256 of the SPKI DER bytes:

```text
2081345a3be37cdfa00f8b8e111fdd57b45397836d998f7515875e72187f593d
```

[`public-source.json`](../../eng/signing/public-source.json) supplies the default
catalog `https://briosa.dev/downloads/catalog.json` and this public key to installer
release packaging. Installer updates share this source by default. Engineers can
change the source to their enterprise mirror; an unchanged mirror keeps the same
publisher key. No credentials are bundled in this file.

Public keys, client IDs, subscription IDs, tenant IDs, and resource names are
identifiers, not login secrets. Keep passwords, client secrets, GitHub tokens,
Cloudflare/R2 credentials, billing data, AU10TIX/Authenticator identity records,
and recovery codes out of the repositories and workflow output.

## Recreate or inspect the Azure setup

Use official Azure CLI and GitHub CLI installations. Sign in to the directory
that owns the subscription, then confirm `az account show` selects it. This
installation used Azure CLI 2.90.0. Do not copy token caches between accounts.

1. Register `Microsoft.CodeSigning` in the subscription and create the signing
   account in East US under `briosa-release`.
2. Complete **Individual / Public** identity validation using the maintainer's
   legal identity, current billing address, AU10TIX, and Microsoft Authenticator.
   Create the Public Trust certificate profile `briosa-public` from that identity.
3. Create a Standard Key Vault with Azure RBAC, soft deletion, and purge protection.
   Generate the RSA 4096 catalog key inside it. Do not generate or upload a test key
   as the production publisher key.
4. Create Entra app registrations `briosa-github-server` and
   `briosa-github-installer` in the same directory. Portal registration also creates
   the associated service principal. CLI-created app registrations may require
   `az ad sp create --id <application-client-id>` if no service principal exists.
5. Add the GitHub federated credentials described below. Do not create client
   secrets, certificates for app login, or long-lived Azure passwords in GitHub.
6. Assign the scoped roles below and configure the GitHub environments.
7. Export only the catalog public key, verify its fingerprint independently, and
   update the reviewed public trust files when establishing a new trust root.
8. Run both protected signing probes and a dispatch-only package build before
   using the setup for a product release.

See Microsoft's [setup quickstart](https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart)
and [role-assignment tutorial](https://learn.microsoft.com/en-us/azure/artifact-signing/tutorial-assign-roles).

### Identity-validation troubleshooting

The initial individual validation failed with **Address matching failed** because
the stored billing ZIP+4 included a hyphen while the Verified ID used nine digits.
The maintainer confirmed that correcting the billing representation and restarting
validation resolved the problem. Use the Azure CLI billing-account update process
documented in the [Artifact Signing FAQ](https://learn.microsoft.com/en-us/azure/artifact-signing/faq)
when the portal prevents a formatting-only correction.

Back up the original billing record privately; preserve all populated fields.
Check the saved billing record and a fresh validation form. Existing validation
requests retain their original address. Microsoft's retry procedure removes the
affected request and associated Verified ID and starts a fresh validation. Never
publish the billing backup, ID scan, credential QR link, or personal address.

### Service principals and scoped access

In Azure IAM, select **User, group, or service principal**, then search for the app
name. These app registrations are application service principals; they do not
appear under **Managed identity**. A generic ABAC banner alone does not establish
why an identity is absent. Confirm the correct picker and directory first.

| Principal | Role | Scope |
| --- | --- | --- |
| `briosa-github-server` | Artifact Signing Certificate Profile Signer | `briosasigning/certificateProfiles/briosa-public` |
| `briosa-github-installer` | Artifact Signing Certificate Profile Signer | Same profile |
| `briosa-github-server` | Key Vault Crypto User | `briosa-release-kv/keys/briosa-catalog` |

The installer identity has no catalog-key grant. The initial account/vault-wide
grants were replaced with these resource-specific grants. No app needs Owner,
Contributor, User Access Administrator, or Identity Verifier for release signing.

Example inspection and assignment using PowerShell, after confirming the tenant:

```powershell
$subscription = az account show --query id -o tsv
$account = "/subscriptions/$subscription/resourceGroups/briosa-release/providers/Microsoft.CodeSigning/codeSigningAccounts/briosasigning"
$vault = "/subscriptions/$subscription/resourceGroups/briosa-release/providers/Microsoft.KeyVault/vaults/briosa-release-kv"
$server = az ad sp list --display-name briosa-github-server --query "[?displayName=='briosa-github-server'].id" -o tsv
$installer = az ad sp list --display-name briosa-github-installer --query "[?displayName=='briosa-github-installer'].id" -o tsv
# Check each lookup is one exact identity before assigning anything.
az role assignment create --assignee-object-id $server --assignee-principal-type ServicePrincipal --role 'Artifact Signing Certificate Profile Signer' --scope "$account/certificateProfiles/briosa-public"
az role assignment create --assignee-object-id $installer --assignee-principal-type ServicePrincipal --role 'Artifact Signing Certificate Profile Signer' --scope "$account/certificateProfiles/briosa-public"
az role assignment create --assignee-object-id $server --assignee-principal-type ServicePrincipal --role 'Key Vault Crypto User' --scope "$vault/keys/briosa-catalog"
```

## GitHub OIDC and environments

Both repositories use an environment named exactly **`release-signing`**. Its
deployment branch policies permit branch `main` and tags `v*`. Signing jobs declare
that environment and have `id-token: write`; ordinary PR CI has no Azure credentials
or OIDC permission. Builds finish before the package signing job authenticates.

The installer initially had an empty environment called `briosa-signing`; it did
not match the Azure trust. The correctly named environment was created with the
same `main`/`v*` restrictions. The unused original is not referenced by workflows.

Each app registration has a federated credential with:

| Field | Value |
| --- | --- |
| Name | `github-release-signing` |
| Issuer | `https://token.actions.githubusercontent.com` |
| Audience | `api://AzureADTokenExchange` |
| Server subject | `repo:spatialanalyzer@307099018/briosa@1306759642:environment:release-signing` |
| Installer subject | `repo:spatialanalyzer@307099018/briosa-installer@1358817864:environment:release-signing` |

These repositories use GitHub's immutable repository/owner IDs in the subject.
Do not replace these with the older name-only `repo:owner/repo:environment:...`
examples. Verify the actual format before recreating trust:

```powershell
gh api repos/spatialanalyzer/briosa/actions/oidc/customization/sub
gh api repos/spatialanalyzer/briosa-installer/actions/oidc/customization/sub
az ad app federated-credential list --id <application-client-id>
```

The Azure audience, issuer, and complete subject must match exactly, including the
environment name. [GitHub OIDC reference](https://docs.github.com/en/actions/reference/security/oidc)

Set these as **environment variables** in each repository's `release-signing`
environment. Use the app's **Application (client) ID**, not its object ID:

| Variable | Server | Installer |
| --- | --- | --- |
| `AZURE_CLIENT_ID` | Client ID of `briosa-github-server` | Client ID of `briosa-github-installer` |
| `AZURE_TENANT_ID` | Owning Azure directory | Same directory |
| `AZURE_SUBSCRIPTION_ID` | Signing subscription | Same subscription |
| `AZURE_SIGNING_ENDPOINT` | `https://eus.codesigning.azure.net/` | Same |
| `AZURE_SIGNING_ACCOUNT` | `briosasigning` | Same |
| `AZURE_SIGNING_CERTIFICATE_PROFILE` | `briosa-public` | Same |
| `AZURE_CATALOG_KEY_ID` | Exact versioned key URI in `eng/signing/azure.json` | Not needed |

Example: `gh variable set AZURE_CLIENT_ID --repo spatialanalyzer/briosa --env release-signing --body <client-id>`.
Workflow expressions use `vars`, not `secrets`. `azure/login` obtains short-lived
credentials from the GitHub OIDC token. The package-signing action reads the
reviewed endpoint, account, profile, and expected publisher from `azure.json`.
Microsoft's login and signing actions are pinned to reviewed commit SHAs.

The configured environments use branch/tag restrictions and permit administrator
bypass; they currently have no additional required-reviewer gate. Maintainers with
permission to change trusted refs/workflows can authorize signing. Revisit reviewer
requirements when project stewardship expands; never loosen the ref restriction
just to test a PR. [GitHub environment protection](https://docs.github.com/en/actions/reference/workflows-and-actions/deployments-and-environments)

## Build and sign products

Both repositories' `release.yml` workflows support manual dispatch with an explicit
semantic version. A dispatch produces workflow artifacts without a GitHub Release;
pushing an approved `v*` tag publishes the final signed product to GitHub Releases.
Do not use a product release tag just to test authentication.

1. Build, test, and package without Azure access. Server package reproducibility
   checks compare unsigned builds; a timestamped signature is intentionally time-dependent.
2. Transfer the tested ZIP and its checksum/provenance to the protected signing job.
3. `Prepare-SignedPackage.ps1` verifies archive identity, containment, internal and
   external hashes, and matching embedded/external provenance, then selects the
   Briosa EXE/DLL files and the installer's shortcut PowerShell script.
4. The official Azure signing action uses SHA-256 and RFC 3161 timestamps.
   Third-party runtime DLL signatures are retained.
5. `Complete-SignedPackage.ps1` requires valid, timestamped signatures from the
   expected publisher, then rebuilds `files.sha256`, the ZIP, its outer SHA-256,
   and adjacent provenance. It refuses to overwrite an existing final artifact.
6. Re-extract the final archive and verify its hashes and signatures again before
   uploading final release assets. Installer checks also exercise the signed CLI
   and launcher without displaying a window.

Only the server/installer Windows product ZIPs go through this signing path.
Protocol and client-conformance developer artifacts retain their existing format.
Unsigned intermediate workflow artifacts are labeled `unsigned-*`; do not publish
them as the Windows product. Installer release workflows pin the shared signing
tools and default public key to a specific `briosa` commit.

## Sign a reviewed catalog

Generate the complete catalog from approved, **already signed and finalized** ZIPs.
Retain earlier versions required for installation/repair. See the
[hosting runbook](hosting.md) for layout and publication order.

```powershell
./eng/New-ReleaseCatalog.ps1 -ArtifactDirectory ./artifacts/public -OutputPath ./artifacts/public/catalog.json
$config = Get-Content ./eng/signing/azure.json -Raw | ConvertFrom-Json
./eng/Sign-ReleaseCatalog.ps1 -CatalogPath ./artifacts/public/catalog.json -AzureKeyId $config.catalogKeyId -PublicKeyPath ./eng/signing/catalog-public.pem -ValidDays 30
./eng/Test-CatalogSignature.ps1 -CatalogPath ./artifacts/public/catalog.json -PublicKeyPath ./eng/signing/catalog-public.pem
```

This requires an already authenticated, authorized Azure CLI. Key Vault receives a
SHA-256 digest and returns a PS256 signature; no private PEM or PFX is materialized.
The signer requires an explicit key version and verifies the result against the
pinned public key before atomically replacing the signature envelope. Failed
signing or an unexpected key preserves the prior envelope.

For a catalog committed and reviewed on an allowed repository ref, dispatch
`sign-catalog.yml` with its repository-relative `catalog_path`. It produces a
`signed-release-catalog` workflow artifact. It does not upload files to R2.
The original `-PrivateKeyPath` option remains only for disposable local tests and
explicitly managed nonproduction signing; production workflows use Key Vault.

Current signatures last 30 days. Refresh before expiry even when package entries
do not change; a weekly cadence provides margin. The R2 publisher and automatic
refresh/expiry monitoring are separate hosting work and are not installed by these
signing workflows. Until those exist, the publishing maintainer must renew the
hosted signature manually. Never leave a first catalog live without arranging renewal.

## Validate and troubleshoot

```powershell
./eng/Test-ReleaseCatalog.ps1
./eng/Test-ReleaseSigning.ps1
gh workflow run signing-smoke.yml --repo spatialanalyzer/briosa --ref main
gh workflow run signing-smoke.yml --repo spatialanalyzer/briosa-installer --ref main
```

Local tests use disposable RSA keys and a fake `az` command. They cover digest
handling, wrong keys, unavailable signing, unchanged prior signatures, expiry,
tampering, archive traversal, internal checksums, third-party file selection, and
rejection of unsigned final releases. The live server probe tests both signing
services; the installer probe independently proves its own OIDC and Authenticode
permissions. Probe artifacts are inert, retained for seven days, and never added to
the public feed. Also run dispatch-only product workflows to validate real PE files.

- OIDC login failure: compare exact subject, repository IDs, tenant, audience,
  environment spelling, and the client ID variable. A service-principal object ID
  is not an application client ID.
- Azure authorization failure: inspect the role on the exact certificate profile
  or catalog key and allow time for propagation. Contributor does not imply signing
  permission. Do not broaden to subscription Owner to work around a missing grant.
- Windows verification failure: inspect publisher, timestamp, chain status, and
  first-party file selection. Preserve third-party signatures; never bypass TLS or
  trust validation to make a release pass.
- Catalog failure: compare exact bytes, public fingerprint, versioned key URI,
  system clock, and expiry. A format-valid catalog is not evidence of a valid signature.
- Enterprise acquisition failure: verify the mirror carries the current catalog,
  its matching signature, and all referenced immutable artifacts. The installer
  must not fall back to public hosting.

## Key maintenance and recovery

Keep recovery ownership and private contact information outside Git. Retain the
public key and fingerprint independently of the download bucket. Periodically
review Azure identity-validation expiry, certificate-profile status, app grants,
GitHub federated credentials, and workflow pins.

Use Azure Key Vault's encrypted backup operation to an access-controlled backup
location if required by the maintainers' recovery procedure. A backup blob is
sensitive and belongs outside Git, build artifacts, and R2. Azure restricts restore
to the same subscription and Azure geography; record and test those constraints.
Soft deletion/purge protection provide 90 days of recovery but do not replace a
planned account/subscription recovery procedure.
[Key Vault backup and restore](https://learn.microsoft.com/en-us/azure/key-vault/general/backup)

Catalog-key rotation is an explicit trust migration: create a new key version,
record its public key/fingerprint, update installer defaults and managed enterprise
trust, and coordinate the catalog cutover. The current client pins one publisher
key per source; it does not automatically trust a replacement. Do not switch the
version variable alone or schedule automatic key rotation without that migration.

If compromise is suspected, pause signing and publication, remove the affected
app's access or federated trust, investigate affected releases, and coordinate
replacement trust with consumers. Catalog signature expiry and client history
checks are not a global revocation service for already installed software.
