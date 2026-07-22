# Briosa v0.1 threat model

## Scope and security posture

This threat model covers the v0.1 public gRPC host, disposable SDK worker, SpatialAnalyzer SDK connection, generated `GetWorkingDirectory` operation, packaging, and operator configuration for exact target SA 2026.1.0529.7.

The supported v0.1 deployment is one trusted user on one Windows machine. Briosa accepts cleartext HTTP/2 only on an IP loopback address. It provides no client authentication or per-caller authorization. LAN, Internet, reverse-proxy, SSH-tunnel, port-forward, shared-host, container-host-bridge, and other remotely reachable deployments are unsupported.

Loopback is a reachability boundary, not an identity boundary. Any local process able to connect to the selected port can call the public API. Operators must not run Briosa in a session or machine where other local users or untrusted processes need to be treated as hostile.

## Security objectives

- Prevent network callers outside the local machine from reaching an unauthenticated MP endpoint.
- Prevent endpoint configuration drift from silently widening the listener.
- Preserve the integrity of exact-target request shapes and the serialized SDK execution sequence.
- Treat command inputs and returned values as protected data.
- Keep geometry, paths, object identifiers, measurements, credentials, license information, and proprietary data out of default logs and diagnostics.
- Bound SDK hangs and crashes without claiming that client cancellation cancels synchronous COM work.
- Make unsupported deployment assumptions explicit instead of silently accepting them.

## Protected assets

| Asset | Security concern |
| --- | --- |
| SpatialAnalyzer session and license | Unauthorized use, state change, disruption, or license exhaustion |
| MP execution authority | Invocation of unsafe, destructive, file, network, or administrative operations |
| Returned results | Exfiltration of paths, geometry, identifiers, measurements, statistics, tolerances, and proprietary result data |
| Request inputs | Disclosure or tampering involving geometry, paths, credentials, object names, or production parameters |
| Local files and network resources reachable by SA | Read, write, overwrite, delete, import, export, or network-side effects initiated through MPs |
| Process availability | Request flooding, worker hangs, SDK port ownership loss, SA instability, or repeated restarts |
| Compatibility identity | Executing a request against the wrong exact SA release or command shape |
| Logs, diagnostics, and crash artifacts | Secondary disclosure of protected inputs, outputs, host details, or license information |
| Build and release artifacts | Substitution, configuration drift, or inclusion of unauthorized vendor binaries |

## Actors

| Actor | Trust level and capability |
| --- | --- |
| Authorized local operator | Controls installation, configuration, SA startup, package selection, and process recovery |
| Authorized local client | Uses a generated client and is permitted to invoke the curated API |
| Untrusted local process or user | Can attempt to connect to loopback, flood requests, inspect same-user data, or alter user-writable configuration |
| LAN or Internet attacker | Can scan or connect only if an operator, proxy, tunnel, firewall rule, or binding error exposes the endpoint |
| Malicious or defective client | Can send malformed, oversized, concurrent, repeated, cancelled, or deadline-constrained requests |
| Briosa host and worker | Trusted project code with different process and failure boundaries |
| SpatialAnalyzer and its SDK | Separately installed proprietary dependency with license, process, and transport behavior outside Briosa's implementation control |
| Maintainer or build system | Can change source, generated contracts, dependencies, workflows, and release artifacts |

## Trust boundaries and data flow

1. **Client to public host:** cleartext HTTP/2 over an IPv4 or IPv6 loopback socket. Requests and successful returned values cross this boundary. There is no authentication.
2. **Public host to worker:** private local named-pipe control channel. The host supervises one disposable worker and sends Briosa-owned command contracts rather than public protobuf or COM types.
3. **Worker to SDK/SA:** one worker-owned COM/OLE Automation client on a dedicated STA. The complete MP sequence and result retrieval are serialized.
4. **SA to machine and external resources:** SpatialAnalyzer may access local files, network paths, devices, license services, and other resources according to the selected MP and the operator account.
5. **Build and operator boundary:** trusted source and configuration become a package executed under the operator's Windows identity. Package checksums and provenance identify the artifact but do not authenticate a runtime caller.
6. **Diagnostics boundary:** logs, gRPC statuses, health, discovery, and crash artifacts can leave the process. They must remain value-free by default.

The successful result path reverses the first four boundaries: SA produces MP results, the SDK getter returns them to the worker, the host maps them to the exact-target protobuf result, and the generated client receives them. Output data requires the same confidentiality treatment as input data.

## Deployment scenarios

| Scenario | v0.1 status | Rationale |
| --- | --- | --- |
| Dedicated or single-user Windows machine, loopback client | Supported | Matches the enforced listener and local trust assumption |
| Multiple trusted applications under the same operator account | Conditionally supported | Every local process can call the endpoint; clients still share one serialized SDK connection |
| Shared workstation with mutually untrusted local users or processes | Unsupported | Loopback provides no caller identity or authorization |
| Direct LAN binding, wildcard binding, or public IP binding | Rejected at startup | No TLS, authentication, authorization, or network abuse controls exist |
| Reverse proxy, port proxy, SSH tunnel, VPN publication, or container port forwarding | Unsupported | These mechanisms can turn loopback cleartext into a remote unauthenticated endpoint |
| Public host local but SDK target set to a remote SA host | Unresolved | Separate outbound SDK transport, firewall, licensing, and integrity questions are tracked by issue #23 |

## Threat register

| ID | Threat or abuse case | Impact | v0.1 mitigation | Residual risk or follow-up |
| --- | --- | --- | --- | --- |
| T01 | Wildcard or non-loopback listener exposes MP execution | Remote command execution and output theft | Programmatic Kestrel listener accepts only `IPAddress.IsLoopback`; generic URL and endpoint settings are rejected | A tunnel or proxy created outside Briosa can still expose loopback |
| T02 | Remote caller impersonates an authorized client | Unauthorized MP use | Remote reachability is unsupported and rejected | No local caller identity; remote support requires authentication |
| T03 | Network request or result is observed or modified | Input tampering and proprietary result disclosure | Traffic is confined to loopback | Cleartext remains visible to sufficiently privileged local software; remote support requires TLS |
| T04 | Local untrusted process calls discovery or MP methods | Capability discovery, path/result exfiltration, command abuse | Documented single-user/trusted-process deployment; generated catalog plus fail-closed runtime policy restrict exposed operations | Runtime policy is not caller authorization; authenticated identity is required for hostile local multi-user scenarios |
| T05 | A successful result leaks through logs or diagnostics | Disclosure of paths, geometry, identifiers, or measurements | Audit logger APIs accept metadata only; redaction tests include sensitive results with verbose logging enabled | Client, collector, and third-party middleware logging remain operator responsibilities |
| T06 | Malicious client floods or overlaps requests | SA starvation, queue pressure, timeouts, or denial of service | One serialized worker execution path, deadlines, watchdog replacement, process isolation | Admission limits, quotas, identities, and per-actor rate policy are unresolved |
| T07 | SDK call hangs or worker crashes | Availability loss and stale SDK ownership | Disposable worker, bounded watchdog, host survival, explicit readiness | Terminating a worker may leave SDK state requiring operator recovery |
| T08 | Dangerous MP is exposed or arguments are reinterpreted across SA releases | File/network side effects, production changes, or semantic misuse | Exact-release bindings, reviewed effect/risk/data classifications, generated catalog ceiling, runtime allow/deny policy, and pre-worker binding checks | Review quality and operator policy remain trusted; deny-by-command is not argument-level authorization |
| T09 | Returned result is treated as less sensitive than input | Proprietary-data exfiltration | Threat model and diagnostics rules classify both directions as protected | Client applications remain responsible for their own storage and logging |
| T10 | Operator uses the wrong SA release | Semantically incorrect execution | Exact package/catalog coordinates, exact-target service namespace, fail-closed unsupported service | Connected SA version remains unverified until a reviewed runtime probe exists |
| T11 | Generic ASP.NET configuration silently overrides the listener | Accidental network exposure | `--urls`, `ASPNETCORE_URLS`, HTTP/HTTPS port keys, and `Kestrel:Endpoints` are rejected; protocols are set in code | Hosting behind IIS or another process is unsupported and must not override the boundary |
| T12 | Remote SDK connection crosses an unknown transport boundary | SDK tampering, eavesdropping, firewall exposure, or licensing violation | Local SDK target remains the default | Issue #23 must resolve ports, transport security, firewall, and licensing before support claims |
| T13 | Build or package is substituted | Execution of malicious server/worker code | Reproducible package, SHA-256 files, source revision, provenance, protected release workflow | Operators must acquire artifacts through a trusted channel and verify checksums |
| T14 | Licensed CI runner executes untrusted code | License and machine compromise | Selected-workflow organization runner group, protected environment, trusted-main payload, no checkout on licensed runner | Hardware and license provisioning remain open in issue #20 |

## Enforced endpoint configuration

The only endpoint settings are:

```text
Briosa:Endpoint:Address
Briosa:Endpoint:Port
```

The address must be an IPv4 or IPv6 loopback IP literal. Hostnames are rejected to avoid DNS and wildcard interpretation. The port must be an integer from 1 through 65535. The packaged defaults are `127.0.0.1` and `50051`.

The server rejects `urls`, `ASPNETCORE_URLS`, `HTTP_PORTS`, `HTTPS_PORTS`, and configured `Kestrel:Endpoints`. Kestrel is bound in code to one HTTP/2 endpoint. There is deliberately no `AllowInsecureRemoteAccess` escape hatch.

## Requirements before remote access can be designed

Remote access requires a new reviewed decision and implementation covering at least:

- TLS with certificate issuance, trust, rotation, revocation, and minimum protocol policy;
- authenticated caller identity suitable for services and human operators;
- operation-level authorization and command-risk policy;
- deny-by-default exposure, network segmentation, and firewall rules;
- request size, concurrency, queue, deadline, and rate controls;
- replay and credential-theft considerations;
- structured audit events, correlation, redaction, and retention;
- reverse-proxy identity and forwarded-header trust if a proxy is supported;
- returned-data confidentiality and client-side handling expectations;
- security tests for unauthorized, malformed, oversized, replayed, cancelled, and abusive requests; and
- an updated threat model for the chosen LAN or remote topology.

Changing only the listener address, adding a firewall exception, or placing an unauthenticated proxy in front of Briosa is not remote-access enablement.

## Assumptions and unresolved policy

- The Windows account running Briosa and SA is trusted and should not be an administrator unless SA independently requires it.
- Other local processes in that account are trusted for v0.1; loopback does not distinguish them.
- Windows firewall and network policy do not publish or proxy the loopback endpoint.
- The host and worker named-pipe access controls require continued review as service-account and multi-user deployment evolves.
- Remote SDK transport and licensing remain issue #23.
- Runtime command policy and value-free audit events are implemented. Caller identity, per-actor authorization, centralized retention policy, and admission control remain future remote-access requirements.
- Exact supported SA releases and connected-version verification remain separately governed decisions.

## Verification

- Unit tests cover default IPv4 loopback, explicit IPv6 loopback, port validation, non-loopback rejection, hostname rejection, generic URL keys, and Kestrel endpoint rejection.
- Package tests prove the default listener starts on loopback and a packaged server given `0.0.0.0` exits instead of listening.
- Portable generated-client scenarios cross the real packaged loopback HTTP/2 endpoint.
- Policy and audit tests cover fail-closed configuration, deny precedence, pre-worker rejection, discovery filtering, typed errors, correlation, outcome metadata, and sensitive-value redaction at verbose log levels.
- Ordinary CI requires no SA installation or license.
