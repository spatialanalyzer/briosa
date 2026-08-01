# Security and observability

- Status: Current for local single-tenant use; remote deployment remains unresolved
- Last reviewed: 2026-08-01

## Supported deployment boundary

Briosa currently exposes an unauthenticated cleartext HTTP/2 gRPC endpoint to
trusted local processes only. `Briosa:Endpoint:Address` must be an IPv4 or IPv6
loopback IP literal, and the port must be from 1 through 65535. The default is
`127.0.0.1:50051`.

Kestrel is configured in code for HTTP/2. Generic ASP.NET Core URL, port, and
Kestrel endpoint settings are rejected so a routine override cannot silently widen
the listener. Hostnames, wildcards, any-address, LAN, public, reverse-proxy,
tunnel, and other remotely reachable topologies are unsupported.

Loopback is not authentication. It does not isolate processes running as the same
user or on the same machine. Remote support requires a separate accepted design
for TLS, authenticated caller identity, authorization, abuse controls, topology,
and audit policy; there is no insecure remote opt-in.

## Runtime operation policy

The compiled handwritten source is the immutable upper bound of operations a
target can express. Runtime policy may reduce that set but cannot enable an RPC
absent from the protobuf contract, implementation, and `SpatialAnalyzerApi`
registry.

`Briosa:Security:Operations:Allow` and `Deny` use exact operation IDs. A missing
allowlist enables nothing, deny wins over allow, and empty, duplicate, scalar, or
unknown IDs fail startup. Policy is fixed for the process lifetime. Discovery
reports only the admitted subset.

Each operation descriptor explicitly records:

- operation ID and exact MP step;
- read/mutation effect;
- execution scope;
- replay safety; and
- reviewed risk flags.

Unknown or unsupported metadata fails closed. Policy denial occurs before worker
enqueue or SDK execution and returns a typed value-free `PermissionDenied` outcome
with `NotStarted` disposition.

Command policy is deployment policy, not caller authorization. The initial server
is single-tenant and suitable only for mutually trusting local callers.

## Audit and diagnostic data

One correlation identifier follows a request through host audit events, the policy
decision, worker-control messages, and the worker outcome. Stable events may record
actor category, endpoint, operation ID, effect, risk flags, execution scope,
worker generation, queue/SDK timing, MP outcome, output-retrieval outcome, raw MP
result code, gRPC status, and curated diagnostic code.

Audit APIs do not accept raw request arguments or returned values. Default logs,
debug logs, traces, status messages, discovery, and error metadata exclude:

- geometry, measurements, identifiers, and proprietary operation data;
- paths, credentials, license material, and evidence references;
- target hostnames, peer details, process identifiers, and executable paths; and
- raw SDK exceptions or arbitrary vendor text.

Operators own log sink access, collection, and retention. Retention should be no
longer than operational or compliance requirements demand.

## Protected licensed validation

Real-SA validation executes repository-produced code on a Windows machine with a
separately licensed desktop application. It must never expose that machine to
untrusted pull-request code.

The protected design uses these controls together:

- a dedicated Windows x64 machine or isolated VM with no personal data or
  unrelated credentials;
- a restricted organization runner group limited to this repository and the exact
  trusted licensed workflow on `main`;
- an exact-target runner label and protected GitHub environment with explicit
  maintainer approval;
- manual dispatch from reviewed `main`, never pull-request or caller-supplied code;
- package and smoke-client construction on a GitHub-hosted runner;
- a short-lived hashed payload downloaded by the licensed runner without checking
  out the repository;
- serialization without canceling an in-flight licensed run; and
- preflight/postflight enforcement of one exact SA instance and no residual
  Briosa or standalone SDK clients.

The workflow never installs, starts, updates, licenses, or terminates
SpatialAnalyzer. It leaves the pre-existing SA process untouched and deletes its
temporary Briosa payload. A failed postflight quarantines the runner until an
operator restores a clean SA/SDK state.

Provisioning the dedicated runner, environment, licensing arrangement, and vendor
guidance remains an explicit administrative decision. Local licensed development
evidence is useful but is not protected-runner or release evidence.

See the target [endpoint security](../../targets/2026.1.0529.7/docs/operations/endpoint-security.md),
[licensed runner](../../targets/2026.1.0529.7/docs/operations/licensed-sa-runner.md),
and [threat model](../../targets/2026.1.0529.7/docs/security/threat-model.md) for
operating details.
