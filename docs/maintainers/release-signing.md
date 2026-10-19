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
4. Create Entra app registrations `briosa-github-server`, `briosa-github-installer`,
   and `briosa-github-r2` in the same directory. Portal registration also creates
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
| `briosa-github-r2` | Key Vault Crypto User | `briosa-release-kv/keys/briosa-catalog` |

The installer identity has no catalog-key grant. The R2 identity has no Artifact
Signing grant, so the unattended R2 publication environment can renew catalog
signatures but cannot sign executables. The initial account/vault-wide
grants were replaced with these resource-specific grants. No app needs Owner,
Contributor, User Access Administrator, or Identity Verifier for release signing.

Example inspection and assignment using PowerShell, after confirming the tenant:

```powershell
$subscription = az account show --query id -o tsv
$account = "/subscriptions/$subscription/resourceGroups/briosa-release/providers/Microsoft.CodeSigning/codeSigningAccounts/briosasigning"
$vault = "/subscriptions/$subscription/resourceGroups/briosa-release/providers/Microsoft.KeyVault/vaults/briosa-release-kv"
$server = az ad sp list --display-name briosa-github-server --query "[?displayName=='briosa-github-server'].id" -o tsv
$installer = az ad sp list --display-name briosa-github-installer --query "[?displayName=='briosa-github-installer'].id" -o tsv
$r2 = az ad sp list --display-name briosa-github-r2 --query "[?displayName=='briosa-github-r2'].id" -o tsv
# Check each lookup is one exact identity before assigning anything.
az role assignment create --assignee-object-id $server --assignee-principal-type ServicePrincipal --role 'Artifact Signing Certificate Profile Signer' --scope "$account/certificateProfiles/briosa-public"
az role assignment create --assignee-object-id $installer --assignee-principal-type ServicePrincipal --role 'Artifact Signing Certificate Profile Signer' --scope "$account/certificateProfiles/briosa-public"
az role assignment create --assignee-object-id $server --assignee-principal-type ServicePrincipal --role 'Key Vault Crypto User' --scope "$vault/keys/briosa-catalog"
az role assignment create --assignee-object-id $r2 --assignee-principal-type ServicePrincipal --role 'Key Vault Crypto User' --scope "$vault/keys/briosa-catalog"
```

## GitHub OIDC and environments

Both repositories use an environment named exactly **`release-signing`**. Its
deployment branch policy permits only branch `main`, plus the exact branch of an open
[hotfix release line](#hotfix-release-lines) in this repository. It admits no tags.
Signing jobs declare that environment and have `id-token: write`; ordinary PR CI
has no Azure credentials or OIDC permission. Builds finish before the package
signing job authenticates.

This repository also has **`r2-publication`**, used only by the publish job of
[`publish-r2.yml`](../../.github/workflows/publish-r2.yml). It admits only branch
`main` and has no required reviewer, so the weekly catalog renewal runs unattended.
It holds the R2 credentials and authenticates as `briosa-github-r2`, which can sign
catalogs but not executables. Renewal re-signs only the reviewed catalog on `main`,
and publication uploads only published-release assets that carry valid
**David Lucas** signatures and reproduce that catalog, so the environment cannot
introduce new trusted code.

The installer initially had an empty environment called `briosa-signing`; it did
not match the Azure trust. The correctly named environment replaced it. The unused
original is not referenced by workflows and can be deleted.

Each app registration has a federated credential with:

| Field | Value |
| --- | --- |
| Name | `github-release-signing` |
| Issuer | `https://token.actions.githubusercontent.com` |
| Audience | `api://AzureADTokenExchange` |
| Server subject | `repo:spatialanalyzer@307099018/briosa@1306759642:environment:release-signing` |
| Installer subject | `repo:spatialanalyzer@307099018/briosa-installer@1358817864:environment:release-signing` |

`briosa-github-r2` has one credential named `github-r2-publication` with the same
issuer and audience and subject
`repo:spatialanalyzer@307099018/briosa@1306759642:environment:r2-publication`.

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

The briosa `r2-publication` environment sets `AZURE_CLIENT_ID` to the client ID of
`briosa-github-r2`, the same `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, and
`AZURE_CATALOG_KEY_ID` as the server, and `R2_ACCOUNT_ID`. It needs no Artifact
Signing variables. Its R2 secrets are described in the
[hosting runbook](hosting.md#configure-publishing-access); `release-signing` holds no
R2 credentials.

Example: `gh variable set AZURE_CLIENT_ID --repo spatialanalyzer/briosa --env release-signing --body <client-id>`.
Workflow expressions use `vars`, not `secrets`. `azure/login` obtains short-lived
credentials from the GitHub OIDC token. The package-signing action reads the
reviewed endpoint, account, profile, and expected publisher from `azure.json`.
Microsoft's login and signing actions are pinned to reviewed commit SHAs.

GitHub runs a dispatched or pushed workflow from that ref's commit, so a check in a
workflow file binds only commits that keep it. The environment's branch policy is
the boundary: it admits only protected branches, which change only through reviewed
PRs, so a signing job always runs reviewed code. Never loosen the ref restriction
just to test a PR. `eng/Verify-ReleaseWorkflowPolicy.ps1` keeps the reviewed
workflows within this design (no tag-push triggers, environments only in listed
jobs, signing jobs limited to `main` or a release line); it is a review aid, not
the boundary.

`release-signing` requires the maintainer's approval for every deployment and does
not allow administrator bypass. While there is one maintainer, self-review is
allowed; add a second reviewer and prevent self-review when stewardship expands.
Approving a `release-signing` deployment and publishing a draft release are
maintainer actions; automation and agents never do either. [GitHub environment protection](https://docs.github.com/en/actions/reference/workflows-and-actions/deployments-and-environments)

## Build and sign products

Both repositories' `release.yml` workflows run only from a manual dispatch with an
explicit semantic version; no workflow runs on tag pushes. To release:

1. Merge the release changes to `main` through a reviewed PR.
2. Dispatch `release.yml` on `main` with the version and `publish: true`. With
   `publish: false` the run still signs the build but only retains workflow artifacts.
3. When the build and validation jobs pass, approve the `release-signing` deployment.
4. The `publish` job creates a **draft** GitHub Release with the signed assets,
   targeting the dispatched commit. Review it, then publish it; publishing creates
   the `v<version>` tag at that commit. If GitHub refuses to create the draft for a
   tag that does not exist yet, push the tag at the dispatched commit and rerun the
   failed job; an existing tag is accepted only when it points there.

A dispatch on any other branch is an unsigned dry run. Do not dispatch a product
version just to test authentication; use the signing probes below.

In this repository, the `version` job also decides signing eligibility and checks
version order before any product build:

- A dispatch on `main` is eligible, and its commit must be on `main`. The version
  must be greater than every stable `v*` tag in the repository except its own,
  including tags on the same or later commits, so a lower version cannot be
  released beside or behind an existing release.
- A dispatch on an open release line `release/<major>.<minor>` is eligible when the
  version is in that line and greater than every stable tag of the line, and the
  commit descends from a stable release of the line that is on `main`. Later
  releases on `main` do not block the line.
- Any other branch is an unsigned dry run, ordered like `main`. Tag refs and other
  refs fail, and so does a version whose tag already points to another commit. The
  `sign` job also requires an allowed ref and `eligible == 'true'`.
- The `validate` job compares protobuf contracts with the highest stable `v*` tag
  reachable from the released commit, excluding only the tag being released.
- `eng/Resolve-ReleaseVersion.ps1` implements these checks;
  `eng/Test-ResolveReleaseVersionPolicy.ps1` exercises them against a temporary
  Git repository.

These checks run from the dispatched branch's reviewed workflow. The environment's
branch policy keeps every other ref from signing; see the environment notes above.

### Hotfix release lines

`main` can carry unreleased breaking work. To ship a fix for an earlier release in
this repository, open a reviewed release line instead of tagging a commit outside
`main`:

1. Create `release/<major>.<minor>` at the line's latest stable release tag.
2. Protect the branch like `main` (PR required, squash only, linear history, no
   force push or deletion, creation by organization owners only, and the line's CI
   checks), then add its exact name to `release-signing`'s deployment branches.
   Never add a `release/*` pattern.
3. If the line's release workflow predates dispatch-only releases, backport it and
   `Resolve-ReleaseVersion.ps1` in the line's first PR, and make CI run on PRs into
   the line.
4. Land fixes through PRs, then dispatch `release.yml` on the line as above.
5. Remove the branch from `release-signing` when the line closes.

1. Validate every target (locked restore, committed interop artifacts, protocol
   compatibility, naming policy, Release build and tests, and Debug development
   tests), then build, test, and package without Azure access. Server package
   reproducibility checks compare unsigned builds; a timestamped signature is
   intentionally time-dependent.
2. The package job records the SHA-256 of each product ZIP as a per-target job
   output and uploads the ZIP and its checksum/provenance as a workflow artifact.
3. The protected signing job downloads that artifact and, before any signing step,
   requires the product ZIP to match the recorded SHA-256. A missing or different
   digest fails the job. The workflow artifact is not the only integrity source:
   `download-artifact` verifies the artifact's own digest, but a same-named artifact
   uploaded later with `overwrite` would carry a matching new digest.
4. `Prepare-SignedPackage.ps1` verifies archive identity, containment, internal and
   external hashes, and matching embedded/external provenance, then selects the
   Briosa EXE/DLL files and the installer's shortcut PowerShell script.
5. The official Azure signing action uses SHA-256 and RFC 3161 timestamps.
   Third-party runtime DLL signatures are retained.
6. `Complete-SignedPackage.ps1` requires valid, timestamped signatures from the
   expected publisher, then rebuilds `files.sha256`, the ZIP, its outer SHA-256,
   and adjacent provenance. It refuses to overwrite an existing final artifact.
7. Re-extract the final archive and verify its hashes and signatures again before
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

For a catalog committed and reviewed on `main`, dispatch `sign-catalog.yml` on
`main` with its repository-relative `catalog_path`; the file must validate against
the release catalog schema. It produces a `signed-release-catalog` workflow artifact. It does not upload files to R2.
The original `-PrivateKeyPath` option remains only for disposable local tests and
explicitly managed nonproduction signing; production workflows use Key Vault.

Current signatures last 30 days. The separate
[R2 publication workflow](../../.github/workflows/publish-r2.yml) renews the unchanged
reviewed catalog weekly in its own `r2-publication` environment and checks public
metadata daily. See the
[hosting runbook](hosting.md) for credentials, publication, failure notifications,
and recovery. New package entries require an explicit publication after review;
the renewal schedule does not publish pending catalog changes.

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
- Signing-tool installation failure: each signing job must install the SDK pinned
  in `global.json`, even when it only signs files built by another job. Microsoft's
  signing action installs a .NET tool and resolves the SDK from the checkout;
  installing the SDK in the build job does not prepare the separate signing runner.
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
