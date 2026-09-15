# Local Control Center validation — 2026-09-15

Maintainer-authorized interactive test on Windows against a separately licensed,
already-running SpatialAnalyzer. No proprietary values or machine paths retained.

## Environment evidence

- Exactly one SpatialAnalyzer application was running. Its executable file and
  product versions both identified 2026.1.0529.7.
- That same process owned the observed TCP 902 listener. This is local attachment
  evidence, not a vendor protocol guarantee.
- Before correction, COM activation started an SDK executable whose file and
  product versions both identified 2024.1.0508.5. The unconfigured desktop flow
  attached but correctly failed the identity gate with `runtime-identity-not-ready`.
- The maintainer explicitly approved changing SDK registration. The matching
  installation's `/Regserver` command required administrator rights; exit code 0
  without elevation did not change the registered path. Elevated registration
  selected the installed 2026.1.0529.7 SDK.
- Connected-SA operator evidence reference: `control-center-local-2026-09-15`.
  It applies to this observed local environment, not to other machines or sessions.

## Validation status

The updated Windows package was exercised through its actual Control Center UI:

- Saving only the connected-SA version/reference succeeded; SDK fallback fields
  remained empty.
- Starting the server left the SDK stopped and observed the external SA instance.
- Starting the SDK reported 2026.1.0529.7 from runtime verification, with exact
  match. Connected-SA evidence was separately labeled operator attestation.
- Connect completed the bounded read-only readiness probe and displayed
  **Ready for commands**.
- The generated-client `ready` smoke scenario passed through this same server and
  worker: working directory, collection enumeration, active collection, active
  units, and working frame. Returned values were not retained in this report.
- Routine pre-readiness health reports appeared as `ReadinessNotReady` at
  Information level, rather than generic framework errors.
- Restart from the ready state completed gracefully, kept the external SA
  application open, and left the replacement SDK stopped. Cached activity is
  cleared when the selected server instance changes.

Portable validation passed 399 target tests (214 server, 160 worker, 25 protocol),
plus the WPF layout/accessibility smoke. After final desktop refinements, the 44
affected server tests and WPF smoke passed again. These cover mismatch precedence,
missing evidence, STA observation, worker environment isolation, and informational
readiness logging. Fault injection and destructive recovery were not performed
against the maintainer's live SA session.
