Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Add-PublishedComponent {
    param($Components, [string]$Id, [string]$Version, [string]$Directory, [string]$Path)

    $key = "$Id/$Version"
    if (-not $Components.ContainsKey($key)) {
        $Components[$key] = @{ Directory = $Directory; Files = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase) }
    }
    [void]$Components[$key].Files.Add($Path)
}

function Get-PublishedComponents {
    param($Publishes, [string]$TargetDirectory)

    $components = @{}
    $targetPrefix = [IO.Path]::GetFullPath($TargetDirectory).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    foreach ($publish in $Publishes) {
        $receipt = $publish.Receipt
        $assets = Get-Content -LiteralPath $receipt.Properties.ProjectAssetsFile -Raw | ConvertFrom-Json -AsHashtable
        $packageFolders = @($assets.packageFolders.Keys | ForEach-Object {
            [IO.Path]::GetFullPath($_).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
        })
        $appHosts = @($receipt.Items.ResolvedAppHostPack | Where-Object Path -EQ $receipt.Properties.AppHostSourcePath)
        if ($appHosts.Count -ne 1 -or -not (Test-Path -LiteralPath $appHosts[0].Path -PathType Leaf)) {
            throw 'Could not identify the exact .NET apphost pack used by publish.'
        }
        $appHost = $appHosts[0]
        $appHostVersion = Split-Path $appHost.PackageDirectory -Leaf
        $appHostId = Split-Path (Split-Path $appHost.PackageDirectory -Parent) -Leaf
        if ($appHostId -ine 'Microsoft.NETCore.App.Host.win-x64' -or $appHostVersion -notmatch '^\d+\.\d+\.\d+$') {
            throw "Unrecognized apphost pack: $appHostId/$appHostVersion. Review its license and publish metadata."
        }
        $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        foreach ($item in $receipt.Items.ResolvedFileToPublish) {
            $relativePath = $item.RelativePath.Replace('\', '/')
            if ($relativePath.EndsWith('.pdb', [StringComparison]::OrdinalIgnoreCase)) { continue }
            if (-not (Test-Path -LiteralPath (Join-Path $publish.Output $relativePath) -PathType Leaf)) {
                throw "Expected publish asset is missing: $relativePath"
            }
            [void]$seen.Add($relativePath)
            $path = $publish.Prefix + $relativePath
            $source = [IO.Path]::GetFullPath($item.FullPath)
            $packageFolder = $packageFolders | Where-Object { $source.StartsWith($_, [StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
            if ($packageFolder) {
                $parts = $source.Substring($packageFolder.Length).Split([IO.Path]::DirectorySeparatorChar, 3)
                if ($parts.Count -ne 3) { throw "Could not identify the restored package for $path." }
                $id = $item['NuGetPackageId'] ?? $parts[0]
                $version = $item['NuGetPackageVersion'] ?? $parts[1]
                if ($id -ine $parts[0] -or $version -ine $parts[1]) { throw "Conflicting NuGet package identity for $path." }
                Add-PublishedComponent $components $id $version (Join-Path $packageFolder $parts[0] $parts[1]) $path
            }
            elseif ($item['IsKeyOutput'] -eq 'True' -and [IO.Path]::GetFileName($source) -ieq 'apphost.exe') {
                Add-PublishedComponent $components 'Microsoft.NETCore.App.Host.win-x64' $appHostVersion $appHost.PackageDirectory $path
            }
            elseif (-not $source.StartsWith($targetPrefix, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Unclassified publish asset '$path'. Review its origin and license before packaging."
            }
        }
        foreach ($file in Get-ChildItem -LiteralPath $publish.Output -File -Recurse | Where-Object Extension -NE '.pdb') {
            $relativePath = [IO.Path]::GetRelativePath($publish.Output, $file.FullName).Replace('\', '/')
            if ($receipt.Properties.StaticWebAssetEndpointsPublishManifestPath -and
                $relativePath -ceq "$($receipt.Properties.TargetName).staticwebassets.endpoints.json") { continue }
            if ($receipt.Properties.TargetName -ceq 'Briosa.Server' -and $relativePath -ceq 'web.config') { continue }
            if (-not $seen.Contains($relativePath)) { throw "Publish output '$relativePath' has no source record. Review its origin and license." }
        }
    }
    $components
}

function Read-PackageText {
    param($Entry)

    $reader = [IO.StreamReader]::new($Entry.Open(), [Text.UTF8Encoding]::new($false, $true), $true)
    try { $reader.ReadToEnd() } finally { $reader.Dispose() }
}

function Add-PackageText {
    param([string]$PackageRoot, [string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) { throw 'Cannot package an empty third-party notice or license.' }
    $text = $Text.Replace("`r`n", "`n").Replace("`r", "`n").TrimEnd("`n") + "`n"
    $bytes = [Text.Encoding]::UTF8.GetBytes($text)
    $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
    if ($hash -ieq (Get-FileHash -LiteralPath (Join-Path $PackageRoot 'LICENSE.txt')).Hash) { return 'LICENSE.txt' }
    $relativePath = "licenses/texts/$hash.txt"
    $path = Join-Path $PackageRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) { [IO.File]::WriteAllBytes($path, $bytes) }
    $relativePath
}

function Get-RestoredPackageNotices {
    param([string]$PackageRoot, [string]$Identity, [string]$Directory, [string[]]$Files)

    $id, $version = $Identity.Split('/', 2)
    $archivePath = Join-Path $Directory ("$id.$version.nupkg".ToLowerInvariant())
    if (-not (Test-Path -LiteralPath $archivePath -PathType Leaf)) { throw "No restored package archive for $Identity." }
    $archive = [IO.Compression.ZipFile]::OpenRead($archivePath)
    try {
        $nuspec = $archive.Entries | Where-Object FullName -IEQ "$id.nuspec" | Select-Object -First 1
        if (-not $nuspec) { throw "Missing nuspec for $Identity." }
        [xml]$xml = Read-PackageText $nuspec
        $metadata = $xml.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']")
        if ($metadata.id -ine $id -or $metadata.version -ine $version) { throw "Restored nuspec identity differs from $Identity." }
        $license = $metadata.SelectSingleNode("*[local-name()='license']")
        if (-not $license -or [string]::IsNullOrWhiteSpace($license.InnerText)) {
            throw "Missing license metadata for $Identity. Review the resolved package before packaging."
        }
        $declaration = $license.InnerText.Trim()
        $licenseEntries = @($archive.Entries | Where-Object Name -Match '^(licen[cs]e|copying)([._-][^.]+)*$')
        $noticeEntries = @($archive.Entries | Where-Object Name -Match '^(copyright|notices?|third[ _-]?party[ _-]?notices?)([._-][^.]+)*$')
        if ($id -ieq 'Microsoft.NETCore.App.Runtime.win-x64' -and (-not $licenseEntries -or -not $noticeEntries)) {
            throw "Missing .NET runtime license or third-party notices for $Identity."
        }
        $copyright = @($metadata.SelectNodes("*[local-name()='copyright']") | ForEach-Object { $_.InnerText.Trim() } | Where-Object { $_ })
        if (-not $copyright) {
            $copyright = @($Files | Where-Object { $_ -match '\.(dll|exe)$' } | ForEach-Object {
                [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $PackageRoot $_)).LegalCopyright
            } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique -CaseSensitive)
        }
        switch ($license.GetAttribute('type')) {
            'file' {
                $declaredFile = $archive.GetEntry($declaration.Replace('\', '/'))
                if (-not $declaredFile) { throw "Declared license '$declaration' is missing from $Identity." }
                $licenseEntries += $declaredFile
                $declaration = "file: $declaration"
            }
            'expression' {}
            default { throw "Unrecognized license metadata for $Identity. Review the resolved package before packaging." }
        }
        $licenseFiles = @($licenseEntries | ForEach-Object { Add-PackageText $PackageRoot (Read-PackageText $_) } | Sort-Object -Unique)
        if (-not $licenseFiles) {
            if ($declaration -ceq 'Apache-2.0') {
                $fallback = Join-Path $PackageRoot 'LICENSE.txt'
            }
            elseif ($declaration -cmatch '^[a-zA-Z0-9.-]+$') {
                $fallback = Join-Path $PSScriptRoot "licenses/$declaration.txt"
            }
            else { $fallback = $null }
            if (-not $fallback -or -not (Test-Path -LiteralPath $fallback -PathType Leaf)) {
                throw "No committed license text for '$declaration' required by $Identity. Review the resolved package before packaging."
            }
            $text = Get-Content -LiteralPath $fallback -Raw
            if ($text.Contains('<copyright notice>') -and -not $copyright) {
                throw "No copyright attribution available for $Identity. Review the resolved package before packaging."
            }
            $licenseFiles = @(Add-PackageText $PackageRoot ($text.Replace('<copyright notice>', ($copyright -join "`n"))))
        }
        $noticeFiles = @($noticeEntries | ForEach-Object { Add-PackageText $PackageRoot (Read-PackageText $_) } | Sort-Object -Unique)
        $row = [ordered]@{
            component = $Identity
            license = $declaration
            copyright = $copyright
            licenseFiles = $licenseFiles
            noticeFiles = $noticeFiles
            files = $Files
        }
        if ($id -ieq 'Microsoft.WindowsDesktop.App.Runtime.win-x64' -and -not $noticeFiles) {
            $repository = $metadata.SelectSingleNode("*[local-name()='repository']")
            if (-not $repository -or $repository.GetAttribute('url') -cne 'https://github.com/dotnet/dotnet' -or
                $repository.GetAttribute('commit') -cnotmatch '^[a-f0-9]{40}$') {
                throw "Missing exact source provenance for Windows Desktop runtime notices: $Identity."
            }
            $commit = $repository.GetAttribute('commit')
            $row.noticeFiles = @(foreach ($framework in 'wpf', 'winforms') {
                $source = Join-Path $PSScriptRoot "licenses/dotnet-windowsdesktop/$commit/$framework-THIRD-PARTY-NOTICES.txt"
                if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
                    throw "Missing reviewed $framework notices at Windows Desktop runtime source commit $commit ($Identity)."
                }
                Add-PackageText $PackageRoot (Get-Content -LiteralPath $source -Raw)
            })
            $row['textSource'] = "https://github.com/dotnet/dotnet/tree/$commit/src"
        }
        $row
    }
    finally { $archive.Dispose() }
}

function Write-WindowsPackageNotices {
    param([string]$PackageRoot, [string]$TargetDirectory, $Publishes)

    $root = Join-Path $PackageRoot 'licenses'
    [void][IO.Directory]::CreateDirectory((Join-Path $root 'texts'))
    $components = Get-PublishedComponents $Publishes $TargetDirectory
    $index = @(foreach ($key in ($components.Keys | Sort-Object -CaseSensitive -Culture 'en-US')) {
        $component = $components[$key]
        $files = @($component.Files | Sort-Object -CaseSensitive -Culture 'en-US')
        if ($key -like 'Microsoft.NETCore.App.Host.win-x64/*' -and -not (Get-ChildItem -LiteralPath $component.Directory -Filter '*.nupkg' -File)) {
            $version = $key.Split('/')[1]
            $runtimeKey = "Microsoft.NETCore.App.Runtime.win-x64/$version"
            if (-not $components.ContainsKey($runtimeKey)) { throw "No matching .NET runtime pack for SDK apphost $key." }
            $row = Get-RestoredPackageNotices $PackageRoot $runtimeKey $components[$runtimeKey].Directory $files
            $row.component = $key
            $row['textSource'] = $runtimeKey
        }
        else { $row = Get-RestoredPackageNotices $PackageRoot $key $component.Directory $files }
        $row
    })
    $brand = Join-Path $TargetDirectory 'src/Briosa.ControlCenter/Assets/Brand'
    $brandProvenance = Get-Content -LiteralPath (Join-Path $brand 'provenance.json') -Raw | ConvertFrom-Json
    $brandLicense = (Get-Content -LiteralPath (Join-Path $brand 'LICENSE') -Raw).Replace("`r`n", "`n")
    if ($brandLicense -cne (Get-Content -LiteralPath (Join-Path $PackageRoot 'LICENSE.txt') -Raw)) {
        throw 'The brand license differs from the root license. Review the pinned brand assets before packaging.'
    }
    foreach ($file in 'fonts/Inter-OFL.txt', 'fonts/Saira-OFL.txt', 'THIRD-PARTY-NOTICES.md') {
        $destination = Join-Path $root 'desktop-brand' $file
        [void][IO.Directory]::CreateDirectory((Split-Path -Parent $destination))
        Copy-Item -LiteralPath (Join-Path $brand $file) -Destination $destination
    }
    $index += [ordered]@{
        component = $brandProvenance.source
        license = 'Apache-2.0 for artwork; OFL-1.1 for Inter and Saira'
        licenseFiles = @('LICENSE.txt', 'licenses/desktop-brand/fonts/Inter-OFL.txt', 'licenses/desktop-brand/fonts/Saira-OFL.txt')
        noticeFiles = @('licenses/desktop-brand/THIRD-PARTY-NOTICES.md')
    }
    $document = [ordered]@{ schemaVersion = 1; components = $index }
    [IO.File]::WriteAllText((Join-Path $root 'index.json'), (($document | ConvertTo-Json -Depth 10).Replace("`r`n", "`n") + "`n"))
    [IO.File]::WriteAllText((Join-Path $root 'README.md'), @'
# Third-party licenses and notices

`index.json` lists the components and versions shipped in this archive, their
license declarations, available copyright attributions, license and notice
texts, and published files. Paths in the index are relative to the archive's
product directory. Identical texts are shared by all applicable components.
The root `LICENSE.txt` is also the Apache 2.0 text where referenced.
'@.Replace("`r`n", "`n") + "`n")
}

function Assert-WindowsPackageNotices {
    param([string]$PackageRoot)

    $index = Get-Content -LiteralPath (Join-Path $PackageRoot 'licenses/index.json') -Raw | ConvertFrom-Json -AsHashtable
    if ($index.schemaVersion -ne 1) { throw 'Unexpected Windows package notice index schema.' }
    $components = @{}
    $referenced = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($row in $index.components) {
        if (-not $row.component -or $components.ContainsKey($row.component) -or -not $row.license -or -not $row.licenseFiles.Count) {
            throw "Missing or duplicate license record for '$($row.component)'."
        }
        foreach ($path in @($row.licenseFiles) + @($row.noticeFiles)) {
            if ($path -cnotmatch '^(LICENSE\.txt|licenses/(?:texts/[a-f0-9]{64}\.txt|desktop-brand/(?:fonts/(?:Inter|Saira)-OFL\.txt|THIRD-PARTY-NOTICES\.md)))$' -or
                -not (Test-Path -LiteralPath (Join-Path $PackageRoot $path) -PathType Leaf)) {
                throw "Missing or invalid notice text '$path' for '$($row.component)'."
            }
            if ($path -cmatch '/([a-f0-9]{64})\.txt$' -and (Get-FileHash -LiteralPath (Join-Path $PackageRoot $path)).Hash -ine $Matches[1]) {
                throw "Notice content differs from its hash: $path"
            }
            [void]$referenced.Add($path)
        }
        $components[$row.component] = $row
    }
    foreach ($text in Get-ChildItem -LiteralPath (Join-Path $PackageRoot 'licenses') -File -Recurse | Where-Object Name -CNotIn @('index.json', 'README.md')) {
        $path = [IO.Path]::GetRelativePath($PackageRoot, $text.FullName).Replace('\', '/')
        if (-not $referenced.Contains($path)) { throw "Unindexed notice text: $path" }
    }
    foreach ($manifest in Get-ChildItem -LiteralPath $PackageRoot -Filter '*.deps.json' -File -Recurse) {
        $deps = Get-Content -LiteralPath $manifest.FullName -Raw | ConvertFrom-Json -AsHashtable
        $target = $deps.targets[$deps.runtimeTarget.name]
        foreach ($entry in $target.GetEnumerator()) {
            if ($deps.libraries[$entry.Key].type -notin @('package', 'runtimepack')) { continue }
            $assets = @('runtime', 'native', 'resources', 'runtimeTargets' | ForEach-Object {
                if ($entry.Value.Contains($_)) { $entry.Value[$_].Keys | Where-Object { $_ -notlike '*/_._' -and $_ -ne '_._' } }
            })
            if ($assets.Count -eq 0) { continue }
            $key = $entry.Key -replace '^runtimepack\.'
            if (-not $components.ContainsKey($key)) { throw "Shipped component '$key' has no notice index record." }
            if ($key -match '^Microsoft\.(NETCore|WindowsDesktop)\.App\.Runtime\.win-x64/' -and -not $components[$key].noticeFiles.Count) {
                throw "Shipped .NET runtime pack '$key' has no third-party notice text."
            }
        }
        $appHostPath = [IO.Path]::GetRelativePath($PackageRoot, ($manifest.FullName -replace '\.deps\.json$', '.exe')).Replace('\', '/')
        if (-not ($index.components | Where-Object { $_.component -like 'Microsoft.NETCore.App.Host.win-x64/*' -and $appHostPath -in $_['files'] })) {
            throw "Shipped apphost '$appHostPath' has no notice index record."
        }
    }
    $brand = Get-Content -LiteralPath (Join-Path $PackageRoot 'metadata/desktop-brand/provenance.json') -Raw | ConvertFrom-Json
    if (-not $components.ContainsKey($brand.source)) { throw "Desktop brand '$($brand.source)' has no notice index record." }
    Write-Host 'Verified third-party notice coverage for the packaged applications.'
}

Export-ModuleMember -Function Write-WindowsPackageNotices, Assert-WindowsPackageNotices
