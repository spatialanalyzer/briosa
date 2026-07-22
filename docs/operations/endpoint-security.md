# Public endpoint security

Briosa v0.1 supports one local deployment posture: a trusted client and Briosa run on the same single-user Windows machine, and the gRPC host listens on an IP loopback address.

The packaged default is cleartext HTTP/2 at `127.0.0.1:50051`. Cleartext is limited to loopback; it is not suitable for LAN or remote traffic. Briosa v0.1 has no client authentication or per-caller authorization, so any local process that can connect to the port can call the exposed API.

## Supported configuration

The only public endpoint keys are:

```json
{
  "Briosa": {
    "Endpoint": {
      "Address": "127.0.0.1",
      "Port": 50051
    }
  }
}
```

Use a different loopback port with an environment variable:

```powershell
$env:Briosa__Endpoint__Port = '43117'
./Briosa.Server.exe
```

Or with a command-line setting:

```powershell
./Briosa.Server.exe --Briosa:Endpoint:Port=43117
```

IPv6 loopback is supported with the IP literal `::1`. Hostnames are deliberately rejected.

## Rejected configuration

The server fails startup when:

- `Briosa:Endpoint:Address` is a wildcard, any-address, LAN, public, hostname, or malformed value;
- the port is outside 1 through 65535;
- `--urls`, `ASPNETCORE_URLS`, `HTTP_PORTS`, or `HTTPS_PORTS` is present; or
- `Kestrel:Endpoints` defines another listener.

Do not create a Windows port proxy, reverse proxy, SSH tunnel, VPN publication, container port mapping, or firewall/NAT rule that makes the loopback service remotely reachable. Those mechanisms bypass the intended reachability boundary without adding Briosa caller identity or authorization.

## Local operating guidance

- Run Briosa and SA as a dedicated, non-administrator Windows user where practical.
- Do not share the host with mutually untrusted users or processes.
- Keep Windows firewall inbound rules closed for the Briosa port.
- Do not log gRPC request or response bodies, MP arguments, or returned values.
- Treat returned paths, geometry, identifiers, measurements, and proprietary results as protected data in client applications.
- Use health and discovery responses only for their documented value-free operational purpose.

## Remote access

Remote public access is not enabled by changing an address. It needs a reviewed design and implementation for TLS, certificate lifecycle, authentication, operation-level authorization, network policy, quotas, rate and queue controls, audit events, redaction, retention, and returned-data handling.

The worker setting `Briosa:SpatialAnalyzer:Host` is separate. It selects the outbound SDK target and never widens the public endpoint. Remote SDK transport, ports, firewall, and licensing are under investigation in issue #23 and should not be treated as supported merely because `ConnectEx` can accept a hostname.

See the [v0.1 threat model](../security/threat-model.md) and [loopback endpoint decision](../architecture/0014-loopback-only-public-endpoint.md) for the complete boundary and unresolved policies.
