# Briosa CLI packaging and descriptor spike

Scope is SpatialAnalyzer `2026.1.0529.7`, Windows x64, with the fake worker. The branch preserves the proof for review. [Spike](https://github.com/spatialanalyzer/briosa/issues/441) · [Epic](https://github.com/spatialanalyzer/briosa/issues/440) · [Architecture record](https://github.com/orgs/spatialanalyzer/discussions/9)

## 1. Build and restore

On SDK `10.0.404`, both the full SDK and Core-only solution rebuilt in Release with **0 warnings, 0 errors**. The solution inherited `TreatWarningsAsErrors=true`, `AnalysisLevel=10.0-all`, locked restore and `NuGetAuditMode=all`. NuGet logged cache hits for the vulnerability index, base dataset and update snapshot through 2026-11-27 11:35 UTC. The restore source was a worktree-local empty directory and advisory data came from the pre-existing HTTP cache. There were no NuGet audit or analyzer diagnostics in either build. This is an audit against that snapshot.

Commands, from the 2026 target, once with `$core = 'false'` and once with `$core = 'true'`:

```powershell
dotnet restore Briosa.slnx --locked-mode --force --configfile $config "-p:UseMcpCore=$core" -v normal
dotnet restore src/Briosa.Cli/Briosa.Cli.csproj --locked-mode -r win-x64 --force --configfile $config "-p:UseMcpCore=$core" -v normal
dotnet build Briosa.slnx -c Release --no-restore '-t:Rebuild' '-m:3' "-p:UseMcpCore=$core"
```

| Graph scope | Packages and resolved versions |
| --- | --- |
| First-party protocol and gRPC libraries already used by the server | `Briosa.Protocol` project reference, `Google.Protobuf 3.36.2`, `Grpc.Core.Api 2.85.0`, `Grpc.Net.Client 2.85.0`, `Grpc.Net.Common 2.85.0` |
| New with Core on its own | `ModelContextProtocol.Core 2.4.0`, `Microsoft.Extensions.AI.Abstractions 10.11.0`, `Microsoft.Extensions.DependencyInjection.Abstractions 10.0.14`, `Microsoft.Extensions.Logging.Abstractions 10.0.14` |
| Added by full SDK over Core | `ModelContextProtocol 2.4.0` and the following `Microsoft.Extensions.* 10.0.14` packages: `Caching.Abstractions`, `Configuration.Abstractions`, `Diagnostics.Abstractions`, `FileProviders.Abstractions`, `Hosting.Abstractions`, `Options`, `Primitives` |

The proof uses `StdioServerTransport` and `McpServer.Create` from Core. The full package adds the hosting integrations and corresponding dependency graph.

The first rerun reached the build with the audit restores green, then failed with `MSB1008: Only one project can be specified. Switch: Rebuild`. Quoting PowerShell's colon switches passed them as single arguments. Both complete solution builds then passed.
