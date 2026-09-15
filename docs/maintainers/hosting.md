# Domain, documentation, and downloads hosting

Porkbun registration · Cloudflare DNS/proxy/routing · GitHub Pages docs · R2 downloads.

This is the public maintainer runbook for `briosa.dev`, promoted from the initial
setup guide and updated on 15 September 2026. Keep registrar billing, recovery
codes, API tokens, R2 upload keys, and account recovery contacts outside Git.
See [Azure signing](release-signing.md) for the independent signing infrastructure.

## Deployment record and remaining checks

The maintainer reports completing the DNS, GitHub Pages, Cloudflare routing, and
R2 setup. External HTTPS checks on 15 September 2026 confirmed:

| URL | Observed response |
| --- | --- |
| `https://briosa.dev/` | 200, served through Cloudflare |
| `https://www.briosa.dev/` | 301 to `https://briosa.dev/` |
| `https://briosa.dev/install` | 404; installation landing page remains to be published |
| `https://briosa.dev/downloads/catalog.json` | 404; no public catalog verified |
| `https://briosa.dev/downloads/hosting-check.txt` | 404; test object not verified |

The checked documentation configuration uses `url: 'https://briosa.dev'`,
`baseUrl: '/'`, and `trailingSlash: false`. A 404 for an absent R2 object does not
prove or disprove the routing rule. Upload the sentinel object below and compare
both origin and public responses to validate the downloads route. Dashboard-only
properties such as DNSSEC, Full (strict), cache rules, bucket settings, and renewal
ownership still need a maintainer check; they cannot be inferred from HTTP alone.

The following sections record the intended configuration and recreation procedure.
Do not treat every checkpoint as a claim that it has been completed.
## What you will have

| Address | Purpose | Hosted by |
| --- | --- | --- |
| `https://briosa.dev/` | Documentation home and documentation pages | GitHub Pages, through Cloudflare |
| `https://www.briosa.dev/` | Also reaches the documentation home; redirects to the canonical `https://briosa.dev/` | GitHub Pages, through Cloudflare |
| `https://briosa.dev/install` | Instructions for installing Briosa and using an enterprise mirror | GitHub Pages, through Cloudflare |
| `https://briosa.dev/downloads` and `/downloads/` | Root of the public file namespace; a small R2-hosted file index if you add the index described below | R2, through Cloudflare |
| `https://briosa.dev/downloads/catalog.json` | Shared catalog of server and installer packages | R2, routed through Cloudflare |
| `https://briosa.dev/downloads/catalog.json.signature.json` | Publisher signature for that catalog | R2 |
| `https://briosa.dev/downloads/packages/...` | Versioned ZIPs, checksums, and provenance | R2 |
| `https://objects.briosa.dev/...` | R2 custom domain used by Cloud Connector; exposes the same bucket root without `/downloads` | R2; also publicly accessible |

The installer and enterprise mirrors use the **briosa.dev** addresses. `objects.briosa.dev` is a routing dependency, not a separate source engineers must configure. Cloud Connector serves the file at the original address; our design does not send the client through a download redirect.

For example, the public URL `/downloads/catalog.json` maps to the R2 object key `catalog.json`. **There is no `downloads` folder inside the bucket.** The `/downloads` prefix exists only on the public website URL. The browser redirect from `www` to the canonical hostname is separate: configure the installer and mirrors with `https://briosa.dev/downloads/catalog.json`, without `www`.

R2 public buckets do not generate a directory listing at their root. To make the bare `/downloads` address useful in a browser, this guide includes an optional `index.html` object and an explicit root rewrite. Without that object, a root request can return 404 even while catalog and package downloads work correctly. [R2 public bucket behavior](https://developers.cloudflare.com/r2/buckets/public-buckets/)

Keep GitHub Releases for release notes and, if useful, duplicate assets. The installer reads the signed catalog; it does not scrape GitHub releases. Language clients continue to use their NuGet, npm, or PyPI distribution channels.

### Cost and ownership

Choose Cloudflare's **Free** website plan and **R2 Standard** storage. R2 includes 10 GB-month of storage, 1 million Class A operations, and 10 million Class B operations monthly. Additional Standard storage is $0.015/GB-month; internet egress is free. Operations above the allowance cost extra. Domain registration and renewal are separate. [R2 pricing](https://developers.cloudflare.com/r2/pricing/)

Cloud Connector is available on Free, but **is currently beta**. This runbook uses it without a Worker. If its availability or behavior changes, preserve the public URLs and replace the routing implementation; do not change installer URLs casually. [Cloud Connector](https://developers.cloudflare.com/rules/cloud-connector/)

Record who owns domain renewal, billing, release publication, and signing-key recovery. Use individual administrator accounts with MFA and store recovery information in the project's approved password manager. This establishes operational contacts; it does not decide the project's unresolved long-term corporate stewardship.

## 1. Finish connecting Porkbun to Cloudflare DNS

1. Domain purchase at Porkbun is complete. Keep registration and renewal there; confirm auto-renewal and recovery access.
2. Use your existing Cloudflare account for both the domain's DNS zone and the R2 bucket.
3. If you have not already added the zone, use Cloudflare **Domains → Onboard a domain**, enter `briosa.dev`, and select **Free**. Review imported records. In Porkbun **Domain Management → briosa.dev → Details → Nameservers**, replace the existing nameservers with the exact values Cloudflare assigns. If you have completed this, proceed to the active-zone check. [Cloudflare domain setup](https://developers.cloudflare.com/dns/zone-setups/full-setup/setup/), [Porkbun nameserver instructions](https://kb.porkbun.com/article/22-how-to-change-nameservers)
4. Wait for the zone to show **Active**. For a fresh domain, remove only unwanted registrar parking records. For an existing domain, preserve mail and verification records. An existing DNSSEC delegation needs a coordinated migration; do not leave an old registrar DS record pointing at the previous DNS provider.
5. After DNS is working, enable DNSSEC in Cloudflare and follow its instructions to install the DS record at the registrar, if that is not handled automatically. [DNSSEC setup](https://developers.cloudflare.com/dns/dnssec/)

You do not need a registrar website package, a paid SSL certificate, Azure storage, or Azure Front Door for this design.

**Checkpoint:** Cloudflare reports an active zone, and the registrar shows the intended nameservers and renewal settings.

## 2. Connect the documentation site to the domain

### Verify ownership in GitHub first

In the **spatialanalyzer organization settings → Pages**, add and verify `briosa.dev`. GitHub supplies a TXT record, normally beneath `_github-pages-challenge-spatialanalyzer.briosa.dev`. Copy its exact name and value into Cloudflare DNS, complete verification, and retain the TXT record. Verify at the organization level because the organization owns `briosa-docs`. [GitHub domain verification](https://docs.github.com/en/pages/configuring-a-custom-domain-for-your-github-pages-site/verifying-your-custom-domain-for-github-pages)

In **spatialanalyzer/briosa-docs → Settings → Pages**, retain **GitHub Actions** as the publishing source and set the custom domain to `briosa.dev` before pointing website DNS at GitHub.

Enter only `briosa.dev`: no scheme, repository name, or path. This setting tells GitHub that requests for the new domain belong to **this repository**, despite its current address being `https://spatialanalyzer.github.io/briosa-docs/`. Choose the apex domain as canonical. With the apex and `www` DNS records below, GitHub redirects `www.briosa.dev` to `briosa.dev`, so both homepage addresses work. [GitHub apex and www behavior](https://docs.github.com/en/pages/configuring-a-custom-domain-for-your-github-pages-site/managing-a-custom-domain-for-your-github-pages-site#configuring-an-apex-domain-and-the-www-subdomain-variant)

### Change the Docusaurus configuration

This is a repository change to make through the normal review workflow. In [docusaurus.config.ts](https://github.com/spatialanalyzer/briosa-docs/blob/main/docusaurus.config.ts), change:

```typescript
url: 'https://briosa.dev',
baseUrl: '/',
```

Retain the existing organization/project names and `trailingSlash: false`. Audit links containing the old `/briosa-docs/` prefix. Create `src/pages/install.mdx` to produce a page at `/install`, with the current installer ZIP link, its checksum, the catalog address, publisher fingerprint, and mirror instructions. Do not put this page under `docs/` unless you also explicitly change its route: the ordinary documentation prefix would otherwise produce a different URL. Keep the page's assets outside `/downloads/`, which belongs to R2. [Docusaurus standalone pages](https://docusaurus.io/docs/creating-pages)

A minimal page skeleton is:

```mdx
---
title: Install Briosa
---

# Install Briosa

Installation instructions and the approved, versioned installer link go here.

[Release catalog](https://briosa.dev/downloads/catalog.json)
```

Finish the page with the actual installation and mirror instructions before publishing it to users. Creating this guide does not itself create or deploy that page.

Run these commands in the documentation checkout with its required Node.js 24 runtime:

```powershell
Set-Location 'C:/src/briosa-docs'
npm ci
if ($LASTEXITCODE -ne 0) { throw 'Dependency installation failed.' }
npm run check
if ($LASTEXITCODE -ne 0) { throw 'Documentation checks failed.' }
```

Merge the reviewed changes. The existing [deploy-pages.yml](https://github.com/spatialanalyzer/briosa-docs/blob/main/.github/workflows/deploy-pages.yml) publishes the build from `main`. There is no need to replace it with a new deployment system. [Docusaurus deployment guidance](https://docusaurus.io/docs/deployment)

### Set initial DNS records

Create the following records in Cloudflare, initially **DNS only** (gray cloud), with automatic TTL:

| Type | Name | Value |
| --- | --- | --- |
| A | `@` | `185.199.108.153` |
| A | `@` | `185.199.109.153` |
| A | `@` | `185.199.110.153` |
| A | `@` | `185.199.111.153` |
| CNAME | `www` | `spatialanalyzer.github.io` |

Use all four apex A records. Remove only conflicting parking/web records for `@` or `www`; retain unrelated mail and verification records. Do not add an A/CNAME record named `/install` or `/downloads`, and do not point the apex at R2. The separate `objects` record will be created by R2 in section 3. These are GitHub's currently documented IPv4 addresses. [GitHub DNS and HTTPS configuration](https://docs.github.com/en/pages/getting-started-with-github-pages/securing-your-github-pages-site-with-https#verifying-the-dns-configuration)

Wait for GitHub's DNS check and certificate provisioning, then enable **Enforce HTTPS**. Confirm both homepage addresses, a deep documentation link, and `/install` work over HTTPS. [GitHub certificate provisioning](https://docs.github.com/en/pages/getting-started-with-github-pages/securing-your-github-pages-site-with-https)

Coordinate the configuration deployment and domain change as one cutover; changing `baseUrl` can temporarily break the old address before the custom domain is ready. The former GitHub project URL is not the origin path we will rewrite into: the docs are now built to live at `/`. GitHub Actions publishing does not require a source `CNAME` file. [GitHub custom-domain configuration](https://docs.github.com/en/pages/configuring-a-custom-domain-for-your-github-pages-site/managing-a-custom-domain-for-your-github-pages-site)

**Checkpoint:** the docs work directly through GitHub with a valid certificate, before adding Cloudflare's HTTP proxy.

## 3. Create R2 storage and its custom domain

1. In Cloudflare, open **R2 object storage** and activate R2 billing if prompted. A free usage allowance does not mean a hard spending cap.
2. Create a bucket named `briosa-downloads`, using **Standard** storage. Select the location appropriate for the project's release operations; record any jurisdiction choice.
3. Open the bucket's **Settings → Custom Domains → Add** and connect `objects.briosa.dev`. Let Cloudflare create its DNS record. Wait for the custom domain to become **Active**.
4. Leave the **Public Development URL** (`r2.dev`) disabled. The production access path is the custom domain. [R2 public buckets](https://developers.cloudflare.com/r2/buckets/public-buckets/)

The bucket must be public through a custom domain in the **same Cloudflare zone** to work with Cloud Connector. An `r2.dev` address cannot substitute for that custom domain. [Cloud Connector R2 requirements](https://developers.cloudflare.com/rules/cloud-connector/providers/)

Use this object layout. Object keys have no leading slash:

```text
briosa-downloads                          R2 bucket
├── hosting-check.txt
├── index.html                           Optional small file index
├── catalog.json
├── catalog.json.signature.json
└── packages/
    ├── server/
    │   └── <SA-target>/<Briosa-version>/
    │       ├── briosa-<version>-sa-<target>-win-x64.zip
    │       ├── briosa-<version>-sa-<target>-win-x64.zip.sha256
    │       └── briosa-<version>-sa-<target>-win-x64.provenance.json
    └── installer/
        └── <Installer-version>/
            ├── briosa-installer-<version>-win-x64.zip
            ├── briosa-installer-<version>-win-x64.zip.sha256
            └── briosa-installer-<version>-win-x64.provenance.json
```

Upload a small text file as `hosting-check.txt` at the bucket root using the dashboard. Open `https://objects.briosa.dev/hosting-check.txt` and confirm its contents.

For a useful browser response at `/downloads`, also upload the following as `index.html` at the bucket root, with Content-Type `text/html; charset=utf-8` and Cache-Control `no-store`. This is a small file index served by R2; the installation guide still lives on the docs site at `/install`. The explicit URLs work whether a visitor opens `/downloads` or `/downloads/`.

```html
<!doctype html>
<html lang="en">
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Briosa downloads</title>
<h1>Briosa downloads</h1>
<ul>
  <li><a href="https://briosa.dev/install">Installation instructions</a></li>
  <li><a href="https://briosa.dev/downloads/catalog.json">Release catalog</a></li>
  <li><a href="https://briosa.dev/downloads/catalog.json.signature.json">Catalog signature</a></li>
</ul>
</html>
```

Everything in this bucket is intended for public delivery. Keep signing secrets, credentials, unapproved builds, vendor binaries, and private release backups elsewhere. Do not configure automatic deletion of retained packages.

## 4. Enable the proxy, rewrite paths, and route downloads

In the zone's **SSL/TLS** settings, use **Full (strict)** after the GitHub origin certificate is working. This validates the origin certificate as well as encrypting the connection. [Full (strict) requirements](https://developers.cloudflare.com/ssl/origin-configuration/ssl-modes/full-strict/)

Wait until Cloudflare's edge certificate is active, then change the four apex A records and the `www` CNAME to **Proxied** (orange cloud). This is the final operating state: DNS-only access would bypass the path-routing rules. Retain the R2-managed record for `objects`; do not replace it with an ordinary CNAME to the R2 S3 API endpoint.

### A. Rewrite the public path to a bucket-root path

Go to **Rules → Overview → Create rule → URL Rewrite Rule**. These are internal rewrites: the requested public URL stays the same. Leave the query-string setting unchanged. Create the following two rules, whose conditions are disjoint.

**Rule A1 — Strip the downloads prefix from file requests.** Custom filter:

```text
(http.host eq "briosa.dev"
 and starts_with(raw.http.request.uri.path, "/downloads/")
 and raw.http.request.uri.path ne "/downloads/")
```

Set **Path → Rewrite to → Dynamic** to:

```text
substring(http.request.uri.path, 10)
```

`/downloads` is ten ASCII characters. Removing those characters maps `/downloads/catalog.json` to `/catalog.json` and `/downloads/packages/server/...` to `/packages/server/...`. The leading slash after the prefix remains. The rule does not match `/downloadsomething` or any documentation page. `substring()` avoids the paid-plan regular-expression requirement. [Rewrite fields and functions](https://developers.cloudflare.com/rules/transform/url-rewrite/reference/fields-functions/), [substring reference](https://developers.cloudflare.com/ruleset-engine/rules-language/functions/#substring), [Transform Rules availability](https://developers.cloudflare.com/rules/transform/)

**Rule A2 — Handle the exact file-namespace root.** Custom filter:

```text
(http.host eq "briosa.dev"
 and raw.http.request.uri.path in {"/downloads" "/downloads/"})
```

If you uploaded the file index, set **Path → Rewrite to → Static** to `/index.html`. If you deliberately want no browser index, set it to `/` instead and expect the R2 root response rather than a directory listing. Both choices keep these exact paths with R2, never with the docs homepage.

### B. Send the original downloads namespace to R2

Under **Rules → Cloud Connector**, create a rule:

| Setting | Value |
| --- | --- |
| Provider | Cloudflare R2 |
| Bucket | `briosa-downloads` |
| Custom domain | `objects.briosa.dev` |
| Name | `Briosa downloads` |
| Match | Custom filter expression below |

```text
(http.host eq "briosa.dev"
 and (raw.http.request.uri.path eq "/downloads"
      or starts_with(raw.http.request.uri.path, "/downloads/")))
```

Deploy the rule. Select the bucket/custom domain in the wizard rather than an external-storage URL. Match only the public apex hostname, so the origin hostname does not match its own routing rule. GitHub handles the `www` alias through its canonical-domain redirect; installer and mirror URLs must use the apex directly. [Cloud Connector dashboard steps](https://developers.cloudflare.com/rules/cloud-connector/create-dashboard/)

**Use `raw.http.request.uri.path` in the filter conditions exactly as shown.** Cloud Connector runs after URL rewrites. A condition using only the rewritten path would see `/catalog.json`, lose the `/downloads/` match, and incorrectly send the request to GitHub. Raw fields preserve the original path for that routing decision. The dynamic rewrite value uses `http.request.uri.path` during the earlier rewrite phase; the connector then forwards the rewritten path to R2. [Rule-order troubleshooting](https://developers.cloudflare.com/rules/reference/troubleshooting/), [original-path field](https://developers.cloudflare.com/ruleset-engine/rules-language/fields/reference/raw.http.request.uri.path/)

For an older partial setup, replace the previous Cloud Connector condition and cache rules with this revision's expressions. Replace the old “retain a downloads folder in R2” layout with the root layout above before enabling the prefix rewrite. Do not stack the previous and revised routing recipes together.

Enable **Always Use HTTPS** for browser requests after HTTPS works. Installer and Artifactory configuration must still contain the final `https://` URL; their success must not depend on that HTTP redirect. [Always Use HTTPS](https://developers.cloudflare.com/ssl/edge-certificates/additional-options/always-use-https/)

**Checkpoint:** the following results match the intended split. Use Cloudflare Trace if a route behaves differently; neither a working DNS lookup nor a successful R2-origin request proves that the public rewrite works. [Cloudflare Trace](https://developers.cloudflare.com/rules/trace-request/)

| Public request | Expected result | Object key, if R2 |
| --- | --- | --- |
| `https://briosa.dev/` | Documentation homepage | — |
| `https://www.briosa.dev/` | Redirect to the canonical documentation homepage | — |
| `https://briosa.dev/install` | Installation instructions from the docs site | — |
| `https://www.briosa.dev/install` | Redirect to `https://briosa.dev/install` | — |
| `https://briosa.dev/downloads` | R2 file index, if uploaded | `index.html` |
| `https://briosa.dev/downloads/` | Same R2 file index, if uploaded | `index.html` |
| `https://briosa.dev/downloads/hosting-check.txt` | Direct 200 response with the test file | `hosting-check.txt` |
| `https://briosa.dev/downloads/catalog.json` | Direct JSON response once published | `catalog.json` |
| `https://briosa.dev/downloads/does-not-exist.zip` | R2 404; no docs fallback or redirect | Missing object |

Expect `https://objects.briosa.dev/catalog.json` to expose the same bytes as the public catalog after publication. The installer never needs that origin URL. A request to `/catalog.json` on the **public apex** is outside the R2 namespace and should not accidentally expose the catalog there.

## 5. Set cache behavior for metadata and packages

Create two **Cache Rules**. Use original paths for public requests and root paths for direct R2-origin requests. These expressions intentionally distinguish the two URL layouts.

**Rule 1 — Keep catalog metadata and the optional file index fresh.** Select **Bypass cache** for:

```text
(http.host eq "briosa.dev"
 and raw.http.request.uri.path in {
   "/downloads/catalog.json"
   "/downloads/catalog.json.signature.json"
   "/downloads" "/downloads/" "/downloads/index.html"
 })
or
(http.host eq "objects.briosa.dev"
 and raw.http.request.uri.path in {
   "/catalog.json" "/catalog.json.signature.json" "/" "/index.html"
 })
```

**Rule 2 — Cache immutable packages.** Select **Eligible for cache**, respect the origin's Cache-Control header, and use this match:

```text
(http.host eq "briosa.dev"
 and starts_with(raw.http.request.uri.path, "/downloads/packages/"))
or
(http.host eq "objects.briosa.dev"
 and starts_with(raw.http.request.uri.path, "/packages/"))
```

Avoid a broader rule that overrides the metadata bypass. Cloud Connector itself does not establish your cache policy. [Cache Rules settings](https://developers.cloudflare.com/cache/how-to/cache-rules/settings/), [Cloud Connector behavior](https://developers.cloudflare.com/rules/cloud-connector/)

Set these HTTP metadata values when uploading objects:

| Objects | Content-Type | Cache-Control |
| --- | --- | --- |
| Catalog and signature | `application/json` | `no-store` |
| Optional file index | `text/html; charset=utf-8` | `no-store` |
| Versioned ZIPs | `application/zip` | `public, max-age=31536000, immutable` |
| Versioned provenance | `application/json` | `public, max-age=31536000, immutable` |
| Versioned checksum files | `text/plain` | `public, max-age=31536000, immutable` |

Our publication process must never reuse an immutable path for different bytes. Headers do not enforce this on the storage account; publisher tooling must enforce it.

Do not put Cloudflare Access login, CAPTCHA, browser challenges, or HTML response rewriting on the download paths. Test with the actual installer, not just a browser. If zone-wide bot settings challenge non-browser downloads, adjust the applicable feature; do not assume an exception is available for every feature on the Free plan. Same-origin download links and the Windows installer do not require a broad CORS policy.

## Release publication and renewal

Azure signing is configured separately; use the [signing runbook](release-signing.md)
and the [catalog contract](../architecture/release-catalog.md). The reviewed
[`public-source.json`](../../eng/signing/public-source.json) now contains the actual
catalog URL and public key. Its SHA-256 fingerprint is recorded in the signing
runbook. Never upload a private key or local test signing identity.

1. Obtain approved, finalized, Authenticode-signed server/installer ZIPs and their
   rebuilt checksums and provenance from the release workflows. Dispatch-only
   validation builds and `unsigned-*` artifacts are not approved public releases.
2. Assemble the complete retained artifact tree with the layout in section 3.
   The catalog producer scans recursively; it does not merge with the currently
   hosted catalog. Keep old package entries needed for reinstall and repair.
3. Generate the catalog, sign it with the version-pinned Azure key for 30 days,
   and verify exact bytes against the pinned public key.
4. Use an R2 upload credential with Object Read & Write access restricted to
   `briosa-downloads`, held in a protected publishing environment or secret manager.
   Record the account's actual S3 API endpoint privately with the credential.
   No AWS account is needed for R2's S3-compatible API. [R2 CLI setup](https://developers.cloudflare.com/r2/get-started/cli/)
5. Upload immutable payloads/provenance/checksums first. Never overwrite an existing
   versioned object with different bytes or use a destructive bucket-wide sync.
6. Publish the matching `catalog.json` and `catalog.json.signature.json` last,
   using the cache rules above. Preserve a private release rollback record.
   Two-object publication is not atomic; readers reject a mismatched pair and can
   refresh after synchronization. Do not weaken verification to conceal that window.
7. Fetch the public catalog/signature and every referenced payload to verify size,
   hashes, publisher, expiry, HTTPS, and absence of redirects. Repeat through the
   enterprise mirror. Update `/install` only after a real release is available.

The workflows in this change sign and create GitHub artifacts/releases; they do
not upload to R2. A single serialized publisher must eventually own updates from
both repositories so parallel releases cannot replace one another's catalog.
Automatic R2 publishing, weekly renewal, and expiry monitoring remain to be deployed.
Until then, arrange an explicit manual publishing/renewal owner before exposing a
catalog that expires. Hosting the signed files is a separate operation from signing them.

| Cadence | Maintenance |
| --- | --- |
| Every release | Verify final public bytes, signatures, mirror behavior, and install instructions |
| Weekly, including weeks without releases | Re-sign the retained catalog for 30 days and publish its matching signature |
| Daily | Check metadata validity and notify the release owner before fewer than seven days remain |
| Monthly | Review provider costs, package retention, account recovery access, and origin/edge certificate health |
| Before domain renewal | Check registrar renewal settings and payment method |
| After trust or credential changes | Revalidate publication, installation, installer updates, and enterprise access |

GitHub's origin certificate and Cloudflare's edge certificate are separate.
Check GitHub Pages certificate renewal as well as the public edge. If origin
renewal needs temporary DNS-only access, schedule that maintenance: downloads
routing requires the Cloudflare proxy. Never disable origin certificate validation
as a workaround. [GitHub custom-domain troubleshooting](https://docs.github.com/en/pages/configuring-a-custom-domain-for-your-github-pages-site/troubleshooting-custom-domains-and-github-pages)
## Enterprise mirrors Configure and test an enterprise Artifactory mirror

Engineers use the ordinary installer. Enterprise repository administrators configure the mirror once; they do not need to build a customized installer or distribute custom configuration as the primary workflow.

Create a **Generic remote repository** in Artifactory with these proposed settings:

| Setting | Value |
| --- | --- |
| Repository key | `briosa-remote` |
| Upstream URL | `https://briosa.dev/downloads/` |
| Upstream authentication | Anonymous |
| Repository layout | Generic; preserve paths without translation |
| List Remote Artifacts | Off; discovery uses the catalog |
| Store Artifacts Locally | On for caching, subject to the freshness check below |
| Metadata Retrieval Cache Period | Start at 60 seconds where applicable |
| Missed Retrieval Cache Period | Start at 60 seconds |

The engineer's catalog address becomes:

```text
https://YOUR-COMPANY.jfrog.io/artifactory/briosa-remote/catalog.json
```

Use the raw repository address, not the Artifactory web UI. The remote caches on demand; configuring it does not pre-download every package. [Artifactory remote repositories](https://docs.jfrog.com/artifactory/docs/remote-repositories)

### Prove mutable metadata freshness

Do not assume a setting called “Metadata Retrieval Cache Period” automatically classifies Briosa's custom JSON files as expiring metadata. JFrog distinguishes metadata from ordinary immutable artifacts. Test both `catalog.json` and `catalog.json.signature.json` across a publication and a signature-only renewal. Lowering a timer alone is not proof. [Artifactory cache behavior](https://docs.jfrog.com/artifactory/docs/remote-repositories#cache-settings-for-remote-repositories)

If the Generic remote retains stale JSON, have the administrator establish a tested metadata refresh procedure. A simple diagnostic alternative is disabling **Store Artifacts Locally**, which streams through Artifactory without its local artifact cache. For an enterprise that requires cached/offline operation, use an administrator-managed synchronization into a Generic local repository with the same file layout until reliable remote refresh is established. In either case, the engineer continues to use the standard installer and one internal catalog address.

In the installer, open **Settings → Package sources**, enter the internal URL, configure its approved authentication, and confirm the Briosa publisher fingerprint. Settings save automatically. Keep installer updates using the shared source unless the enterprise deliberately provides a separate installer catalog. An unchanged mirror retains Briosa's publisher public key; it does not need the private signing key. See [implemented enterprise settings](https://github.com/spatialanalyzer/briosa-installer/blob/main/docs/administration.md).

Validate both server installation and installer updates from a test workstation whose permitted network access is internal only. Check that every request stays on the configured source. Also test metadata renewal after the cache interval; stale or expired signatures must not be accepted. A first successful installation does not prove future updates will work.

For offline distribution, copy a complete catalog/signature/package tree before disconnecting. Cached files alone do not extend signature validity. Plan fresh signed metadata before expiry for future acquisition and repair; already installed packages remain separately verifiable offline.

