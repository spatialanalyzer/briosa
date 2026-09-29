# R25 licensed validation — 2026-09-28

This is the licensed-machine validation record for the frozen `0.9.0-dev.1`
server candidates built from source `89b59aa0ddd05ea868d94c4e4a4f89ee0fbcbccb`.
The package ZIP SHA-256 values are
`112f41ab4d5c0debdbba8e2b750aa197530afa8b2bf6d240a380b710b2ac86d1`
for SA 2026.1.0529.7 and
`4ea9fc46f0b695d641baa434320c92ae734612985dabfa55144b47bd4a3be68a`
for SA 2024.1.0508.5. Returned SA values, machine paths, and license data
were not retained.

| Target | Exact local application and SDK file versions | Executed result |
| --- | --- | --- |
| 2026.1.0529.7 | Both `2026.1.0529.7` | `Test-LocalLicensedLifecycle.ps1` owned-application and SDK-loss-recovery scenarios passed. The frozen packaged server passed `Test-LicensedSpatialAnalyzer.ps1`: external connect reached MP readiness, the standard generated-client read-only smoke succeeded, and public stop-SDK completed. The test-owned SA instance and Briosa processes exited. |
| 2024.1.0508.5 | Both installed files `2024.1.0508.5` | After the maintainer changed SDK registration to 2024.1, the frozen packaged server passed `Test-LicensedSpatialAnalyzer.ps1`: external connect reached MP readiness, the standard generated-client read-only smoke succeeded, and public stop-SDK completed. The test-owned SA instance and Briosa processes exited. The [exact 2024 SDK/SA string-setter probe](../../targets/2024.1.0508.5/docs/testing/evidence/string-setters-2026-09-28.md) had already passed directly for the two affected MP steps. |

The [2026 string-setter record](../../targets/2026.1.0529.7/docs/testing/evidence/string-setters-2026-09-28.md)
also includes live execution of both affected RPCs through a source-built
server. The R25 packaged smoke covers the maintained read-only generated-client
scenario for both targets; it does not rerun those two string-setter RPCs from
either frozen ZIP.

The licensed scripts used separate activated-SDK and connected-SA operator
attestations (`r25-local-file-version-2026-09-28` for the 2026 run and
`r25-local-file-version-2024-2026-09-28` for the 2024 run) and required the
server's bounded execution-channel proof before MP readiness. File-version
inspection identified the installed executables; the script assertions
established readiness and result success. These checks do not establish
sustained latency or throughput.

No reporting-frame object/relationship, physical instrument, robot, crib-sheet,
or projector fixture was supplied for this run. Their opt-in licensed scenarios
remain unexecuted. A protected licensed CI runner and fixture-backed coverage
remain release follow-ups. The portable fake-worker and package matrix evidence
is recorded separately in the R17–R24 delivery records.

The packaged smoke initially completed its assertions but failed cleanup
because interactive startup auto-launched the Control Center, leaving its DLL
in use. Both target licensed-smoke scripts now pass
`--Briosa:Desktop:Mode=Disabled`, as other non-desktop package checks do. The
2026 packaged scenario was rerun and exited successfully with clean teardown.
