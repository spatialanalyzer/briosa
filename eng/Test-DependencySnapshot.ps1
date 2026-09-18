[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$fixture = Join-Path $PSScriptRoot "../artifacts/dependency-snapshot-tests/$([Guid]::NewGuid())"
New-Item -ItemType Directory -Path "$fixture/src/obj" -Force | Out-Null
'<Solution><Project Path="src/Example.csproj" /></Solution>' | Set-Content "$fixture/Briosa.slnx"
$assets = @{
    project = @{ frameworks = @{ net10 = @{
        dependencies = @{ Direct = @{ version = '[1.0.0, )' } }
        downloadDependencies = @(@{ name = 'Runtime'; version = '[10.0.12, 10.0.12]' })
    } } }
    targets = @{
        net10 = @{
            'Direct/1.0.0' = @{ type = 'package'; dependencies = @{ Transitive = '2.0.0' } }
            'Transitive/2.0.0' = @{ type = 'package' }
            'LocalProject/1.0.0' = @{ type = 'project' }
        }
        'net10/win-x64' = @{
            'Direct/1.0.0' = @{ type = 'package'; dependencies = @{ Transitive = '2.0.0'; Native = '3.0.0' } }
            'Transitive/2.0.0' = @{ type = 'package' }
            'Native/3.0.0' = @{ type = 'package' }
        }
    }
}
$assetsPath = "$fixture/src/obj/project.assets.json"
$assets | ConvertTo-Json -Depth 20 | Set-Content $assetsPath
$outputPath = "$fixture/snapshot.json"
$parameters = @{ TargetDirectory = $fixture; OutputPath = $outputPath; CommitSha = ('a' * 40); GitRef = 'refs/pull/1/head' }
& "$PSScriptRoot/Write-DependencySnapshot.ps1" @parameters
$snapshot = Get-Content $outputPath -Raw | ConvertFrom-Json -AsHashtable
$packages = @($snapshot.manifests.Values)[0].resolved
if ($packages.Count -ne 4 -or $packages['pkg:nuget/Direct@1.0.0'].relationship -ne 'direct' -or
    $packages['pkg:nuget/Transitive@2.0.0'].relationship -ne 'indirect' -or
    $packages['pkg:nuget/Direct@1.0.0'].dependencies.Count -ne 2 -or
    -not $packages.ContainsKey('pkg:nuget/Runtime@10.0.12')) {
    throw 'Snapshot lost resolved packages, dependency edges, relationships or runtime packs.'
}
$assets.project.frameworks.net10.downloadDependencies[0].version = '[10.0.0, 11.0.0)'
$assets | ConvertTo-Json -Depth 20 | Set-Content $assetsPath
$rejected = $false
try { & "$PSScriptRoot/Write-DependencySnapshot.ps1" @parameters }
catch { $rejected = $_.Exception.Message -like '*Unresolved download dependency*' }
if (-not $rejected) { throw 'Snapshot accepted an unresolved runtime version range.' }
Write-Host 'Dependency snapshot graph, runtime packs and unresolved-range rejection passed.'
