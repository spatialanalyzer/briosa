# R17 portable server validation and protocol checkpoint

Recorded 2026-09-28 for the two exact-SA target products. The validated source
checkpoint is `89b59aa0ddd05ea868d94c4e4a4f89ee0fbcbccb` on the server redesign
branch. This is a development candidate, not a release or licensed-SA result.

| Check | SA 2026.1.0529.7 | SA 2024.1.0508.5 |
| --- | ---: | ---: |
| Locked restore and Release solution build | Pass; 0 warnings/errors | Pass; 0 warnings/errors |
| Release portable tests, excluding benchmarks | 998 passed; 0 failed/skipped | 1,003 passed; 0 failed/skipped |
| Debug gRPC reflection tests | 3 passed | 3 passed |
| Buf format, lint, descriptor, and reviewed breaking comparison | Pass | Pass |
| MP argument-name migration policy, including six rejection cases | Pass | Pass |
| Exact interop artifact/provenance/API verification | Pass | Pass |
| SDK-disabled local host and worker lifecycle smoke | Pass | Pass |
| Maintained fake-worker runtime/full-path benchmark | Pass | Pass |
| Maintained logging-pipeline benchmark | Pass | Pass |

The root target-isolation and CI-workflow checks passed. The isolation verifier
now evaluates the existing generated build-identity source path before checking
target ownership. The name-policy test fixture restores three separately
reviewed robot ID corrections to its older pinned-baseline shape so that it
tests only the argument-name exception. Neither correction changes production
protobuf, runtime code, or product behavior.

The benchmark scripts wrote machine-local JSON under each target's
`artifacts/ci-metrics/`. The named-pipe fake-worker dispatch p95 was 0.1943 ms
for 2026 and 0.1861 ms for 2024 (512 measured requests after 64 warmups). Both
runs admitted and terminated all 576 requests and drained queue, admission,
and active-execution state. The generated-client loopback HTTP/2 scalar-get p95
was 0.4045/0.7667 ms for 2026 and 0.4341/0.8019 ms for 2024 with logging
off/on, respectively. The full-path harness also covered nested and list
payloads, concurrency, overload, and heartbeat; its ten rows per logging mode
passed the evidence contract.

In the logging harness, disabled/normal-file p95 was 0.1537/0.2988 ms for
2026 and 0.1654/0.2788 ms for 2024 (512 measured calls per mode). Normal mode
reported no drops or sink failures. The blocked and throwing sink scenarios
completed their expected containment checks. The only committed like-boundary
baseline is the single 2026-09-15 logging run: disabled/normal p95 was
0.2115/0.2593 ms. The current pair moves in opposite directions. These short
local runs include scheduling, JIT, and filesystem effects; they establish no
reliable speedup or regression, sustained disk throughput, or SA command
latency. No comparable earlier full-path HTTP/2 or 2024 logging baseline is
committed.

Two deterministic protocol ZIPs were built twice and passed descriptor,
manifest, ordering, and checksum checks. They carry public package `briosa`,
compatibility major 2/revision 0, version `0.9.0-dev.1`, and the source
checkpoint above. R18–R21 must consume these exact local artifacts and hashes:

| Target | Protocol ZIP SHA-256 | Descriptor SHA-256 |
| --- | --- | --- |
| 2026.1.0529.7 | `177ed8b761d9a438809a344463ef8363adff4ae306f3d7e685e6b0d6f362f958` | `57ee91c9d1f5e4df66a06b6c9adaa270fb08989c70aabfa9c95f33ebf81513d1` |
| 2024.1.0508.5 | `026d60d0576d1529d1dd86c5a833e373145445c700bf1837f7e0d5441a5dfe2e` | `6d079cc7b1ca47bdadd8450b1aa7f4b16a178141fddc953f4600da1830a19c8b` |

Each ZIP, `.sha256`, and external provenance file is in its target's
`artifacts/r17-protocol/` directory. The build is reproducible from the source
checkpoint if these ignored local outputs are lost. This R17 run did not launch
or connect to SpatialAnalyzer. Live exact-target coverage remains a separate
R25 task, and the 18 pre-existing modified compatibility reports remain for
R24 reconciliation.
