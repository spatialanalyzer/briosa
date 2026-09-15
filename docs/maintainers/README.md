# Maintainer infrastructure runbooks

These version-controlled documents describe Briosa's release infrastructure and
how maintainers operate it. They are public, reviewed with code, and contain no
credentials, billing records, identity documents, private keys, or recovery codes.
A public GitHub repository's wiki is also public; limiting wiki editing does not
limit who can read it. Keep sensitive operational records in the maintainers'
access-controlled secret manager, not in a wiki or Git history.

- [Release signing with Azure and GitHub](release-signing.md): identities,
  permissions, public trust roots, release workflows, validation, and recovery.
- [Domain, documentation, and downloads hosting](hosting.md): Porkbun,
  Cloudflare DNS/routing, GitHub Pages, and R2.
- [Shared release catalog contract](../architecture/release-catalog.md): exact
  metadata bytes, signatures, expiry, and enterprise mirror behavior.

Update these runbooks in the same PR as infrastructure or workflow changes.
Record observations separately from intended configuration. A working homepage,
successful signing probe, or green build does not establish that a product has
been approved and published to the public catalog.
