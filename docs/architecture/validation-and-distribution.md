# Validation and distribution

- Status: Current; protected infrastructure remains provisional
- Last reviewed: 2026-09-30

## Validation levels

Briosa uses the least privileged environment that can prove a claim:

1. formatting, static analysis, protobuf lint/build, and source-structure checks;
2. unit and contract tests against fake SDK abstractions;
3. process-level tests with fake delay, hang, crash, malformed response, and
   SDK/worker-loss and explicit replacement behavior;
4. packaged generated-client/server scenarios without SpatialAnalyzer; and
5. explicitly authorized success-path validation against a separately installed
   and licensed exact-target SpatialAnalyzer.

Ordinary restore, build, test, protocol, and package workflows require no
SpatialAnalyzer installation, license, running application, original SDK binary,
or local COM registration. The approved managed interop metadata is the only
vendor-derived build boundary.

## Portable validation

Unit and worker tests prove exact MP step and argument identity, setter/getter
order, typed value conversion, MP-result handling, output retrieval, connection
state, readiness, identity policy, serialization, replay classification, and
redaction.

Process-level tests use separate fake worker executables that speak the real
private control protocol. They exercise queueing, cancellation before and after
enqueue, heartbeat failure, watchdog expiry, worker crash, malformed responses,
admission closure, quarantine, incident retention, and explicit recovery without
emulating SpatialAnalyzer.

Runtime performance tests use only fake workers and invented values. They establish
regression evidence for the Briosa process/transport path, not SpatialAnalyzer
performance.

## Packaged generated-client scenarios

`Briosa.SmokeClient` references only the public generated protocol and
`Grpc.Net.Client`. It crosses the packaged loopback HTTP/2 boundary and verifies:

- discovery, target coordinates, and exact capability identity;
- successful strongly typed operations using invented fake-worker results;
- disconnected/unavailable behavior;
- policy denial and unsupported services;
- MP failure and output-getter failure;
- caller deadline and cancellation; and
- watchdog fault state followed by explicit replacement, reconnect, and
  successful recovery.

The client validates response structure, retrieval state, canonical gRPC status,
and typed value-free errors. It never logs returned SpatialAnalyzer values,
arguments, hostnames, process identifiers, licenses, or raw server diagnostics.

## Licensed SpatialAnalyzer validation

Licensed validation uses the same packaged public server and generated-client path
against an already-running exact-target SpatialAnalyzer. It requires fresh explicit
permission for the task, independently established activated-SDK and connected-SA
identity evidence, one clean eligible SA instance, and no competing SDK clients.

The standard licensed workflow is read-only and success-path only. Failure, hang,
crash, malformed-result, competing-client, and mutation injection remain portable
fake tests or separately reviewed experiments. The runner reports only structural
success and curated diagnostic codes; operation values remain unlogged.

The script owns and cleans up its Briosa server, worker, standalone SDK process,
temporary port, and extracted package. It does not close or restart the
pre-existing SpatialAnalyzer application.

Each operation records whether exact-target licensed validation passed, did not
run, or remains at risk. Missing licensed infrastructure is never represented as a
passing test.

## Protocol and package reproducibility

Every exact target independently verifies:

- Buf formatting, linting, compilation, and descriptor generation;
- deterministic protocol archive content and checksums;
- locked .NET dependencies and Release compilation;
- approved interop provenance and canonical public API, checked in CI and before
  release packaging by `Verify-InteropArtifacts.ps1 -NoBuild` against the
  committed provenance hashes, assembly identity, and regenerated API manifest;
- self-contained Windows x64 server/worker packaging;
- deterministic archive path set, byte content, order, timestamps, and hashes;
- manifest target, source, protocol, operation, and interop coordinates;
- offline package diagnostics; and
- source/project isolation from every other SA target.

Pull-request CI compares each target's protobuf contracts with the pull request's
base commit to catch unreviewed breaking changes; push CI on `main` runs format,
lint, and build checks without a breaking comparison. Release validation compares
each target with the previous stable release: the highest `v*` tag without a
prerelease label that is reachable from the released commit through any parent,
excluding only the tag being released. The
comparison is skipped only when no such tag exists or the target has no protocol
at that tag. The evolving unreleased `main` branch is not treated as a stable
compatibility baseline.

## CI and release orchestration

Repository workflows enumerate supported targets explicitly. Each target restores,
builds, tests, packages, and validates from its own directory. Adding a target
requires updating the CI, release, and protected licensed-validation matrices.
CI and release validation also run the Debug-only development reflection and
manual server startup tests.

The repository-policy CI job reports twin files that matched in both targets at
the event's base revision, apart from target version strings, but changed on only
one side or no longer match. The report is advisory: it adds workflow warnings and
a job summary but does not fail CI. See the [engineering checks](../../eng/README.md).

One repository semantic version may produce independently identified server and
protocol artifacts for several exact SA releases. The release workflow resolves
the version, requires it to be greater than every other stable `v*` tag in the
repository wherever that tag points, and
records whether the commit is on `main`. A tag on a commit outside `main` fails;
a manual dispatch outside `main` builds an unsigned dry run. A validation job
repeats the CI restore, interop, protocol, naming-policy, build, and test gates for
every target before packaging. Release assembly verifies every matrix result before
publication. Language-client packages are built and versioned in their own
repositories from the reviewed protocol artifact.

Target-specific commands and procedures are maintained in the
[local gRPC development guide](../../targets/2026.1.0529.7/docs/development/local-grpc-server.md),
[client smoke guide](../../targets/2026.1.0529.7/docs/testing/client-smoke.md), and
[licensed runner guide](../../targets/2026.1.0529.7/docs/operations/licensed-sa-runner.md).
