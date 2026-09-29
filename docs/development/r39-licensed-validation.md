# R39 licensed package validation — 2026-09-28

The candidate is `0.9.0-dev.2`, built from server source
`aa0c7ae1a2c5529033a2772ef9aad7b37321c58c`. These observations are
separate from the historical [R25 candidate](r25-licensed-validation.md).
Returned SA values, job contents, license data, and machine paths were not
retained.

| Exact target | Server ZIP SHA-256 | Result |
| --- | --- | --- |
| 2024.1.0508.5 | `cfe116a5ca0aa3b8b9e496726eee15305cd162aa37f9abd5e74fcc9721b72688` | Passed: external connection and execution-channel readiness, maintained generated-client read-only smoke, both string-setter RPCs, and public stop-SDK. |
| 2026.1.0529.7 | `30ca84516e814acbba58556af7eedd96e73744c96b067c73a1b69f5516da3e84` | Passed against the maintainer-started exact SA/SDK: external connection and execution-channel readiness, maintained generated-client read-only smoke, both string-setter RPCs, and public stop-SDK. |

The 2024 run used one fresh SA process and one packaged Briosa server/worker.
Installed application and SDK file versions were inspected separately and
attested with the non-sensitive references
`r39-local-installed-sa-file-version-2026-09-28` and
`r39-local-installed-sdk-file-version-2026-09-28`. The server required its
bounded execution-channel proof before MP readiness. A temporary generated
client then called `MakeCollectionItemNameRefListWildcardSelection` with `Any`
and `*` criteria and `SetGdtOptions` with `Centroid` and `None`; both returned
the public `Succeeded` execution state. The maintained read-only smoke also
passed. The test-owned server, worker, SDK, and SA processes were stopped.

For 2026, the same package workflow was attempted after closing 2024 and
starting one exact 2026 SA instance. The installed application had ports
901–903, but the worker reported runtime-verified SDK identity mismatch and
the lifecycle RPC returned `FAILED_PRECONDITION`. A vendor `/Regserver`
invocation returned successfully without changing the protected machine-wide
COM mapping. A temporary per-user mapping affected registry queries but did
not affect SDK activation. It was removed, the 2026 SA instance was closed,
and the original 2024 COM mapping remained. After the maintainer updated the
machine-wide registration, the worker verified an exact activated SDK match.
However, two elevated and one ordinary hidden SA launches did not open SDK
ports 901–903. The packaged lifecycle attempt returned `UNAVAILABLE` after
`ConnectEx` status `-3`; no MP work started. All test-owned hidden processes
were closed. The maintainer then launched the exact 2026 SA normally. Against
that instance, the same packaged server passed external readiness and the
maintained read-only smoke. The one-off generated client called both setter
RPCs with the same reviewed `Any`/`*` and `Centroid`/`None` inputs; both
returned public `Succeeded`. Public stop-SDK completed, Briosa and SDK test
processes exited, and the maintainer-owned SA application remained open.

The one-off setter client is a local validation artifact under target
`artifacts/`; a temporary copy of the maintained test script inserted it
after the read-only smoke. Neither alters the candidate binaries or creates
a general licensed test suite. The reporting-frame, instrument, robot, crib-sheet, and projector
fixture-dependent scenarios remain unexecuted where fixtures are unavailable.
