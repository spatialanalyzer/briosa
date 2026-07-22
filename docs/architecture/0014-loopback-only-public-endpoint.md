# ADR 0014: Loopback-only public endpoint

## Status

Accepted for v0.1 on 2026-07-22.

## Context

Briosa can automate a separately licensed desktop application and return paths, geometry, identifiers, measurement values, statistics, and other proprietary results. The v0.1 host has no client authentication, caller identity, operation-level authorization, or TLS configuration.

The package previously defaulted `Urls` to `http://127.0.0.1:50051`, but ASP.NET Core also accepts URL bindings from command-line arguments, environment variables, HTTP/HTTPS port keys, and Kestrel endpoint configuration. Several of those formats treat wildcards or unrecognized hostnames as all-interface bindings. A safe default is insufficient if a routine override can silently create a remote unauthenticated MP endpoint.

## Decision

- Briosa owns one authoritative public endpoint configured by `Briosa:Endpoint:Address` and `Briosa:Endpoint:Port`.
- The address must parse as an IP literal for which `IPAddress.IsLoopback` is true. IPv4 and IPv6 loopback are supported; hostnames, wildcard, any-address, LAN, and public addresses are rejected.
- The port must be an integer from 1 through 65535. Defaults remain `127.0.0.1:50051`.
- Kestrel is configured in code with `Listen(address, port)` and HTTP/2 only. Code-based endpoints take precedence over generic hosting URLs, but Briosa also rejects `urls`, `http_ports`, `https_ports`, and `Kestrel:Endpoints` so an operator cannot mistake an ignored unsafe setting for an enabled endpoint.
- The v0.1 loopback connection uses cleartext HTTP/2. This is acceptable only within the stated trusted-local-process model. Loopback is not authentication.
- There is no insecure remote opt-in. LAN, Internet, reverse-proxy, tunnel, shared-host, and other remotely reachable topologies remain unsupported until a later decision implements TLS, authentication, authorization, abuse controls, and auditability.
- `Briosa:SpatialAnalyzer:Host` controls the worker's outbound SDK target and does not change the public listener. Security and licensing for a remote SA target remain issue #23.

## Consequences

- Ordinary environment and command-line precedence can change the loopback port but cannot widen the listener.
- Existing test scripts use `Briosa:Endpoint:Port` instead of `--urls`.
- Deployments that relied on generic ASP.NET Core URL settings fail with an actionable startup error.
- A reverse proxy cannot be declared supported merely because it can reach loopback; it would expose an unauthenticated cleartext service.
- Remote access requires deliberate new protocol and operational work rather than a configuration toggle.

## Verification

The [v0.1 threat model](../security/threat-model.md) maps assets, actors, trust boundaries, deployment scenarios, abuse cases, mitigations, residual risks, and remote prerequisites. Unit tests cover configuration rejection, and package smoke tests prove a non-loopback endpoint fails startup.

## References

- [Configure endpoints for Kestrel](https://learn.microsoft.com/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0)
- [ASP.NET Core Web Host settings](https://learn.microsoft.com/aspnet/core/fundamentals/host/web-host?view=aspnetcore-10.0)
