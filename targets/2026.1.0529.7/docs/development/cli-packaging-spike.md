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

## 3. Signing, size, startup, and determinism

`../../eng/Prepare-SignedPackage.ps1 -PackagePath $coreZip -OutputDirectory $stage` selected 16 files in the current Core package. The two new signing inputs were `Briosa.Cli.exe` and `Briosa.Cli.dll`. The recursive `^Briosa\..+\.(exe|dll)$` selector also works under `cli/`. It excluded both new third-party assemblies, `ModelContextProtocol.Core.dll` and `Microsoft.Extensions.AI.Abstractions.dll`. `../../eng/Test-ReleaseSigning.ps1` passed with added root and nested CLI fixtures that assert the exact selection, plus the existing no-CLI fixture. Keep package-specific CLI presence assertions in the 2026 smoke. Adding a CLI file to the shared signing module's unconditional server requirements would reject 2024 and earlier server packages.

Archive lengths and uncompressed totals are from `zipfile.ZipInfo.file_size` and file size on disk. The four matrix archives came from the earlier session and were remeasured here.

| Variant | ZIP bytes | Change from baseline | Uncompressed bytes | ZIP SHA-256 |
| --- | ---: | ---: | ---: | --- |
| Baseline | 134,004,446 | — | 316,595,173 | `f4590f351b42d0da9bed3630f6921d3fc15e36d2555dc6162fa00dbfec53819d` |
| Core shared root | 134,733,609 | +729,163 | 318,849,705 | `50d7d1708d4bc3ef5441cec77edd8bc6a0ed97db039669921b86c39c86110e95` |
| Full shared root | 134,768,496 | +764,050 | 318,942,017 | `49958326feda3ddf840f20e8ad72882a7ff1a1ef0216a1ebe95181712349d16c` |
| Full isolated | 174,665,354 | +40,660,908 | 410,076,209 | `4e1edb9bdabc7785a95bab05f314cf4981beda0464ee11dc896e3afc45169f1e` |

Current reruns of `./eng/Test-WindowsPackage.ps1 -Version 0.0.0-spike441 -OutputDirectory $output -SpikeCliLayout Root` with the Core/shared settings and `-SpikeCliLayout Isolated` with default full settings gave identical first and second hashes within each pair. The Core ZIP measured 134,733,897 compressed and 318,850,217 uncompressed bytes, with SHA-256 `2e2bf05df127ed4a7e9b2838a31133c415a7fac9b79d763a1aee74d4ee4f21c4` twice. Full isolated measured 174,665,554 and 410,076,721 bytes, with SHA-256 `4a60862f4b6e59502cdc64db5108bfd23303ff0dedc737b17f5fa2ccac0e465f` twice. Checksums, notices, packaged CLI launch, diagnostics and server smoke passed for both. The spike skipped the desktop ownership test because it uses a state directory outside this worktree.

Five pairs of wall-clock `Briosa.Cli.exe --version` invocations per layout, via `./eng/Test-CliSpike.ps1 -PackagePath $zip -ProtocolPath $protocol -RunDirectory $dir -Timing`. Each first invocation ran from a fresh directory copied with `robocopy /J`. Its warm sample immediately reused that directory. Windows 11 Pro build 26300, Ryzen 9 5900HS (8 cores / 16 threads), 39.4 GiB RAM, local NVMe SSD, PowerShell 7.7.0, AC power with the Turbo plan, and the shared Briosa verification lock held. No reboot or global cache purge was used. First-launch samples are a cold-start proxy.

| Variant | First launch, median (range), ms | Warm, median (range), ms |
| --- | ---: | ---: |
| Core shared root | 402.86 (380.21–416.98) | 82.08 (76.64–88.03) |
| Full shared root | 355.67 (346.92–361.70) | 72.80 (72.14–78.26) |
| Full isolated | 375.36 (361.63–391.38) | 72.76 (70.34–75.30) |

All three returned a stdio MCP `initialize` response and exited on EOF. A separate Core launch with `DOTNET_ROOT` unset reported packaged `hostfxr.dll` and `coreclr.dll` in `DOTNET_HOST_TRACE`.

## 4. Third-party licenses and notices

Inspected each restored `.nupkg` with Python `zipfile` and parsed the `.nuspec` license and repository elements. Compared each package payload's `licenses/index.json` to baseline, using the publish receipt ownership rather than counting build-only references or duplicate runtime files as new components.

| New shipped package | Layout | Declared license | Nuspec repository and commit | Evidence and notice |
| --- | --- | --- | --- | --- |
| `ModelContextProtocol.Core/2.4.0` | All CLI variants | Expression `Apache-2.0` | `https://github.com/modelcontextprotocol/csharp-sdk` at `4cbf42ef107763f0bc75c358af8cb2dfe47a769a` | Identity row plus `MCP C# SDK 2.4.0 license` and `MCP C# SDK 2.4.0 notices`. The nupkg has neither text. Ship the reviewed source license and third-party notices. |
| `ModelContextProtocol/2.4.0` | Full SDK variants | Expression `Apache-2.0` | Same MCP repository and commit | Separate identity row with the same two texts. The nupkg has neither text. |
| `Microsoft.Extensions.AI.Abstractions/10.11.0` | All CLI variants | Expression `MIT` | `https://github.com/dotnet/extensions` at `25f07afa51034e37b334210642d7dc5f191c142d` | Identity row plus `.NET extensions AI 10.11.0 MIT`. The nupkg has neither a license nor a notice file. Ship the reviewed MIT text. No separate notice found in the nupkg. |
| `Microsoft.Extensions.Caching.Abstractions/10.0.14`, `Microsoft.Extensions.Configuration.Abstractions/10.0.14`, `Microsoft.Extensions.DependencyInjection.Abstractions/10.0.14`, `Microsoft.Extensions.Diagnostics.Abstractions/10.0.14`, `Microsoft.Extensions.FileProviders.Abstractions/10.0.14`, `Microsoft.Extensions.Hosting.Abstractions/10.0.14`, `Microsoft.Extensions.Logging.Abstractions/10.0.14`, `Microsoft.Extensions.Options/10.0.14`, `Microsoft.Extensions.Primitives/10.0.14` | Full isolated | All expression `MIT` | All `https://github.com/dotnet/dotnet` at `0427b9abbead3fa64c6655eb4290c537397131fa` | One identity row per package pointing to the existing `.NET Foundation MIT` text. Each nupkg includes `THIRD-PARTY-NOTICES.TXT`. Stage that nupkg text and record the reference. |

The three new reviewed text digests are `7c6a686d2b34b86ad8c726e0ac4d76903b525f251d73e617f8db3879fa8f11fc` (SDK license), `2f7f668d285953d085a0050b6350358ef490300298f2d712fdcdd0438d53a751` (SDK notices), and `47f0ad8ba753eae5de6178291cd8f38bbb42d2368b177f8e11714ad17c701275` (AI MIT). The SDK notice uses normalized LF. All nine isolated extension packages referenced the same nupkg notice digest, `c6926930d0181fa515076603e1305461b1bce5275a8b35337d75d2eef3acad4a`.

The MCP source license describes an MIT-to-Apache-2.0 transition. It preserves MIT terms and applies CC-BY-4.0 to non-specification documentation. The Windows ZIP adds the MCP DLLs and rights texts. Any later redistribution of upstream documentation needs a separate review. The SDK notice includes MIT mcpdotnet and polyfill attributions and an Apache-2.0 URI Template Tests attribution. The only licenses declared by the added nupkgs are MIT and Apache-2.0.

The inherited isolated build first failed with `No committed license text for 'MIT' required by Microsoft.Extensions.AI.Abstractions/10.11.0. Review the resolved package before packaging.` The per-identity evidence resolves the missing primary texts. `WindowsPackageNotices.psm1` also needed to accept and hash-check optional reviewed notice supplements alongside the existing nupkg notice extraction. Otherwise the MCP source third-party notice has no package entry from which it can be attached. Provenance is checked against the nupkg expression, repository and commit before reading these texts.

Both variants of the 2026 package smoke passed the notice checks. The current Core run also rejected a fixture with the reviewed MCP notice reference removed. A cached `2024.1.0508.5` `./eng/New-WindowsPackage.ps1 -Version 0.0.0-spike441 -OutputDirectory $output -PassNoticeExpectation` followed by `Assert-WindowsPackageNotices` and `../../eng/Test-WindowsPackageNoticeGates.ps1` passed on the resulting 2024 tree. Its ZIP SHA-256 was `5bb1f76a2cb39a5be5535a17fa3463e7f35b67af8f22cdc2138cd8e422fe16b8`. The shared evidence is selected from resolved publish identities, so unused 2026 records do not add components to the 2024 package.

