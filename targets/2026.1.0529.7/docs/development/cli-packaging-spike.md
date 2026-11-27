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

## 2. Publish layout

The earlier session's plain full SDK publish failed at `Microsoft.Extensions.Caching.Abstractions.dll` with `Publish outputs disagree for shared file 'Microsoft.Extensions.Caching.Abstractions.dll'.` The preflight found all ten disagreements. The current isolated run reproduced the same overlap report.

| Relative file | Package root product version | Plain CLI product version |
| --- | --- | --- |
| `Microsoft.Extensions.Caching.Abstractions.dll` | 10.0.15 | 10.0.14 |
| `Microsoft.Extensions.Configuration.Abstractions.dll` | 10.0.15 | 10.0.14 |
| `Microsoft.Extensions.DependencyInjection.Abstractions.dll` | 10.0.15 | 10.0.14 |
| `Microsoft.Extensions.Diagnostics.Abstractions.dll` | 10.0.15 | 10.0.14 |
| `Microsoft.Extensions.FileProviders.Abstractions.dll` | 10.0.15 | 10.0.14 |
| `Microsoft.Extensions.Hosting.Abstractions.dll` | 10.0.15 | 10.0.14 |
| `Microsoft.Extensions.Logging.Abstractions.dll` | 10.0.15 | 10.0.14 |
| `Microsoft.Extensions.Options.dll` | 10.0.15 | 10.0.14 |
| `Microsoft.Extensions.Primitives.dll` | 10.0.15 | 10.0.14 |
| `System.Diagnostics.DiagnosticSource.dll` | 11.0.0 | 10.0.15 |

Product-version source suffixes are `8e0f8ed2312954727f8919d7d1c2f78b483b4ee7` for 10.0.15, `0427b9abbead3fa64c6655eb4290c537397131fa` for 10.0.14, and `297acf930fb0f5c07fd4e51d78e521b4ad2e5d1f` for 11.0.0.

The shared-root trial uses a CLI-only `FrameworkReference` to `Microsoft.AspNetCore.App` and a direct CLI reference to the existing central `System.Diagnostics.DiagnosticSource 11.0.0` pin. The framework reference causes publish to resolve the nine extension assemblies from the same 10.0.15 ASP.NET runtime pack as the server. That also preserves identical owners in the notice receipts. Both SDK choices merged. A SHA-256 comparison of every common ZIP entry against the earlier baseline found changes in `files.sha256` and `licenses/index.json` only. Server, worker, Control Center app, Control Center launcher, and their dependency files were byte-identical. The worker and launcher already require DiagnosticSource 11.0.0. The app under `desktop/` has its own runtime and remains isolated.

The inherited 11.0.0 DependencyInjection/Logging pin experiment would require two explicit server references, a separate Server lock file, and server binaries and dependency metadata validated with 11.0.0 assemblies in place of the 10.0.15 runtime-pack assemblies. It addresses two of the nine full SDK extension conflicts. The remaining seven would need their own byte-identical resolutions. The worker and root desktop launcher publish neither of the two abstraction DLLs, and the desktop app uses a separate runtime. There is no recorded successful result from the 11.0.0 trial. Its server project changes are retained in the initial checkpoint. A package-only pin to nine NuGet packages at 10.0.15 could not be tested from the cache and would also have to reconcile package ownership with the ASP.NET runtime pack in the publish receipts.

The root CLI is already the small-host option. Its 162,304-byte native apphost loads its managed DLL directly using the adjacent runtime. Its generated configuration declares included `Microsoft.NETCore.App` and `Microsoft.AspNetCore.App` 10.0.15. The Control Center launcher likewise reuses root runtime files, then starts a separate executable from `desktop/`. A separate CLI launcher would introduce another process and stdio lifetime handling for an isolated payload.

Commands: `./eng/New-WindowsPackage.ps1 -Version 0.0.0-spike441 -SpikeCliLayout Root` with ordinary full restore reproduced in the inherited raw run. The shared results used `CliSharedRuntime=true`, `CliPinDiagnosticSource=true` and `UseMcpCore=true` or `false`. The isolated comparison used `-SpikeCliLayout Isolated` and default CLI restore. The ZIP contents were checked with Python `zipfile` and per-entry `hashlib.file_digest(..., "sha256")`.
