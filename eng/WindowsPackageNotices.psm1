Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$evidence = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'licenses/reviewed-windows-evidence.json') -Raw | ConvertFrom-Json -AsHashtable

function Set-PublishedOwner {
    param($Owners, [string]$Path, [string]$Owner)

    if ($Owners.ContainsKey($Path) -and $Owners[$Path] -ine $Owner) {
        throw "Conflicting owners for '$Path': '$($Owners[$Path])' and '$Owner'."
    }
    $Owners[$Path] = $Owner
}

function Add-PublishedComponent {
    param($Components, $Owners, [string]$Id, [string]$Version, [string]$Directory, [string]$Path, [string]$NativeName)

    $key = "$Id/$Version"
    Set-PublishedOwner $Owners $Path $key
    if (-not $Components.ContainsKey($key)) {
        $Components[$key] = @{ Directory = $Directory; Files = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase); Native = @{} }
    }
    elseif ($Components[$key].Directory -ine $Directory) { throw "Conflicting restore locations for $key." }
    [void]$Components[$key].Files.Add($Path)
    if ($NativeName) { $Components[$key].Native[$Path] = $NativeName }
}

function Test-ProductOutput {
    param($Item, $Receipt, [string]$Source, [string]$RelativePath, [string]$TargetDirectory)

    $properties = $Receipt.Properties
    $project = $properties['MSBuildProjectFullPath']
    $name = $properties.TargetName
    if ($project -and $RelativePath -ceq "$name.dll" -and
        @($Receipt.Items['IntermediateAssembly'] | Where-Object FullPath -IEQ $Source).Count -eq 1) { return $true }
    if (($RelativePath -ceq "$name.deps.json" -and $Source -ieq $properties['ProjectDepsFilePath']) -or
        ($RelativePath -ceq "$name.runtimeconfig.json" -and $Source -ieq $properties['ProjectRuntimeConfigFilePath'])) { return $true }
    if ($name -ceq 'Briosa.Server' -and $project -and $RelativePath -ceq 'appsettings.json' -and
        $Source -ieq (Join-Path (Split-Path -Parent $project) 'appsettings.json')) { return $true }
    if ($name -ceq 'Briosa.Worker' -and $RelativePath -ceq 'Briosa.SpatialAnalyzer.Interop.dll') {
        $interop = [IO.Path]::GetFullPath((Join-Path $TargetDirectory "interop/SpatialAnalyzer/$(Split-Path $TargetDirectory -Leaf)/$RelativePath"))
        if ($Source -ieq $interop -and $Item['ReferenceSourceTarget'] -eq 'ResolveAssemblyReference') { return $true }
    }
    $referenced = $Item['MSBuildSourceProjectFile']
    if ($Item['ReferenceSourceTarget'] -ne 'ProjectReference' -or -not $referenced -or
        -not [IO.File]::Exists($referenced) -or $Item['ResolvedFrom'] -ine $Source) { return $false }
    $projectName = [IO.Path]::GetFileNameWithoutExtension($referenced)
    if ($projectName -cnotmatch '^Briosa\.[a-zA-Z.]+$' -or $RelativePath -cne "$projectName.dll") { return $false }
    $projectRoot = Join-Path $TargetDirectory 'src' $projectName
    $bin = (Join-Path $projectRoot 'bin') + [IO.Path]::DirectorySeparatorChar
    $referenced -ieq (Join-Path $projectRoot "$projectName.csproj") -and
        $Source.StartsWith($bin, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Path]::GetFileName($Source) -ceq $RelativePath
}

function Get-PublishedComponents {
    param($Publishes, [string]$TargetDirectory, $Owners = @{})

    $components = @{}
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
            if (-not $seen.Add($relativePath)) { throw "Duplicate publish receipt path '$relativePath'." }
            $path = $publish.Prefix + $relativePath
            $source = [IO.Path]::GetFullPath($item.FullPath)
            $packageFolder = $packageFolders | Where-Object { $source.StartsWith($_, [StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1
            if ($packageFolder) {
                $parts = $source.Substring($packageFolder.Length).Split([IO.Path]::DirectorySeparatorChar, 3)
                if ($parts.Count -ne 3) { throw "Could not identify the restored package for $path." }
                $id = $item['NuGetPackageId'] ?? $parts[0]
                $version = $item['NuGetPackageVersion'] ?? $parts[1]
                if ($id -ine $parts[0] -or $version -ine $parts[1]) { throw "Conflicting NuGet package identity for $path." }
                $nativeName = if ($item['AssetType'] -eq 'native' -or $parts[2] -match '^runtimes[\\/]win-x64[\\/]native[\\/]') {
                    [IO.Path]::GetFileName($source)
                }
                Add-PublishedComponent $components $owners $id $version (Join-Path $packageFolder $parts[0] $parts[1]) $path $nativeName
            }
            elseif ($item['IsKeyOutput'] -eq 'True' -and $source -ieq $receipt.Properties['AppHostIntermediatePath'] -and
                $relativePath -ceq "$($receipt.Properties.TargetName).exe") {
                Add-PublishedComponent $components $owners 'Microsoft.NETCore.App.Host.win-x64' $appHostVersion $appHost.PackageDirectory $path
            }
            elseif (Test-ProductOutput $item $receipt $source $relativePath $TargetDirectory) {
                Set-PublishedOwner $owners $path 'Briosa'
            }
            else {
                throw "Unclassified publish asset '$path'. Review its origin and license before packaging."
            }
        }
        foreach ($file in Get-ChildItem -LiteralPath $publish.Output -File -Recurse | Where-Object Extension -NE '.pdb') {
            $relativePath = [IO.Path]::GetRelativePath($publish.Output, $file.FullName).Replace('\', '/')
            if ($seen.Contains($relativePath)) { continue }
            $properties = $receipt.Properties
            if (($properties.StaticWebAssetEndpointsPublishManifestPath -and
                    $relativePath -ceq "$($properties.TargetName).staticwebassets.endpoints.json") -or
                ($properties.TargetName -ceq 'Briosa.Server' -and $relativePath -ceq 'web.config' -and
                    -not [IO.File]::Exists((Join-Path (Split-Path -Parent $properties.MSBuildProjectFullPath) 'web.config')))) {
                Set-PublishedOwner $owners ($publish.Prefix + $relativePath) 'Briosa'
                continue
            }
            throw "Publish output '$relativePath' has no source record. Review its origin and license."
        }
    }
    $components
}

function Add-ReviewedText {
    param([string]$PackageRoot, [string]$Name)

    $review = $evidence.texts[$Name]
    $source = Join-Path $PSScriptRoot 'licenses' $review.file
    if ((Get-FileHash -LiteralPath $source).Hash -ine $review.sha256) {
        throw "Reviewed text '$Name' differs from its evidence digest."
    }
    Add-PackageText $PackageRoot ([IO.File]::ReadAllText($source, [Text.UTF8Encoding]::new($false, $true)))
}

function Read-PackageText {
    param($Entry)

    $reader = [IO.StreamReader]::new($Entry.Open(), [Text.UTF8Encoding]::new($false, $true), $true)
    try { $reader.ReadToEnd() } finally { $reader.Dispose() }
}

function ConvertTo-PackageText {
    param([string]$Text)
    if ([string]::IsNullOrWhiteSpace($Text)) { throw 'Cannot package an empty third-party notice or license.' }
    $Text.Replace("`r`n", "`n").Replace("`r", "`n").TrimEnd("`n") + "`n"
}

function Get-PackageTextHash {
    param([string]$Text)
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes((ConvertTo-PackageText $Text)))).ToLowerInvariant()
}

function Add-PackageText {
    param([string]$PackageRoot, [string]$Text)

    $text = ConvertTo-PackageText $Text
    $bytes = [Text.Encoding]::UTF8.GetBytes($text)
    $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
    if ($hash -ieq (Get-FileHash -LiteralPath (Join-Path $PackageRoot 'LICENSE.txt')).Hash) { return 'LICENSE.txt' }
    $relativePath = "licenses/texts/$hash.txt"
    $path = Join-Path $PackageRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) { [IO.File]::WriteAllBytes($path, $bytes) }
    $relativePath
}

function Assert-BrandResourceHash {
    param([string]$AssemblyPath, $Expected)

    $pe = [Reflection.PortableExecutable.PEReader]::new([IO.File]::OpenRead($AssemblyPath))
    try {
        $metadata = [Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
        $section = $pe.GetSectionData($pe.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress)
        foreach ($handle in $metadata.ManifestResources) {
            $resource = $metadata.GetManifestResource($handle)
            if (-not $resource.Implementation.IsNil -or $metadata.GetString($resource.Name) -cne 'Briosa.ControlCenter.App.g.resources') { continue }
            $reader = $section.GetReader([int]$resource.Offset, ($section.Length - [int]$resource.Offset))
            $resourceBytes = $reader.ReadBytes($reader.ReadInt32())
            $resources = [Resources.ResourceReader]::new([IO.MemoryStream]::new($resourceBytes))
            $sha256 = [Security.Cryptography.SHA256]::Create()
            try {
                foreach ($name in $Expected.Keys) {
                    [string]$type = ''
                    [byte[]]$bytes = @()
                    $resources.GetResourceData($name, [ref]$type, [ref]$bytes)
                    if ($type -cne 'ResourceTypeCode.Stream' -or $bytes.Length -lt 4 -or
                        [Convert]::ToHexString($sha256.ComputeHash($bytes, 4, $bytes.Length - 4)) -ine $Expected[$name]) {
                        throw "Embedded desktop brand resource differs from its reviewed digest: $name."
                    }
                }
            }
            finally { $sha256.Dispose(); $resources.Dispose() }
            return
        }
        throw 'Missing WPF brand resources in the published desktop application.'
    }
    finally { $pe.Dispose() }
}

function Get-RestoredPackageNotices {
    param([string]$PackageRoot, [string]$Identity, [string]$Directory, [string[]]$Files, $Native = @{}, [switch]$AppHost)

    $id, $version = $Identity.Split('/', 2)
    $runtimeReview = $evidence.runtimeNativeFiles[$id]
    if ($id -imatch '^Microsoft\.[^.]+\.App\.Runtime\.win-x64$' -and -not $runtimeReview) {
        throw "Unreviewed Windows runtime pack $Identity. Review the pack and its per-file terms before packaging."
    }
    if (($runtimeReview -or $id -ieq 'Microsoft.NETCore.App.Host.win-x64') -and $version -cne $evidence.runtimeVersion) {
        throw "Unreviewed Windows runtime pack $Identity. Review the version and per-file terms before packaging."
    }
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
        $repository = $metadata.SelectSingleNode("*[local-name()='repository']")
        $sourceUrl = if ($repository) { $repository.GetAttribute('url') }
        $commit = if ($repository) { $repository.GetAttribute('commit') }
        if ($id -ieq 'Microsoft.WindowsDesktop.App.Runtime.win-x64' -and
            ($sourceUrl -cne 'https://github.com/dotnet/dotnet' -or $commit -cne $evidence.desktopSourceCommit)) {
            throw "Unreviewed Windows Desktop runtime source '$sourceUrl' at '$commit' for $Identity."
        }
        $licenseEntries = @($archive.Entries | Where-Object Name -Match '^(licen[cs]e|copying)([._-][^.]+)*$')
        $noticeEntries = @($archive.Entries | Where-Object Name -Match '^(copyright|notices?|third[ _-]?party[ _-]?notices?)([._-][^.]+)*$')
        if ($id -ieq 'Microsoft.NETCore.App.Runtime.win-x64' -and (-not $licenseEntries -or -not $noticeEntries)) {
            throw "Missing .NET runtime license or third-party notices for $Identity."
        }
        $packageCopyright = @($metadata.SelectNodes("*[local-name()='copyright']") | ForEach-Object { $_.InnerText.Trim() } | Where-Object { $_ })
        $binaryCopyright = @()
        if (-not $packageCopyright) {
            $binaryCopyright = @($Files | Where-Object { $_ -match '\.(dll|exe)$' } | ForEach-Object {
                [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $PackageRoot $_)).LegalCopyright
            } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique -CaseSensitive)
        }
        $sources = @()
        switch ($license.GetAttribute('type')) {
            'file' {
                $declaredFile = $archive.GetEntry($declaration.Replace('\', '/'))
                if (-not $declaredFile) { throw "Declared license '$declaration' is missing from $Identity." }
                $primary = Add-PackageText $PackageRoot (Read-PackageText $declaredFile)
                $declaration = "file: $declaration"
            }
            'expression' {
                $review = $evidence.sourceLicenses[$Identity]
                if ($review) {
                    if ($declaration -cne $review.expression -or $sourceUrl -cne $review.repository -or $commit -cne $review.commit) {
                        throw "Reviewed license provenance differs from the resolved nuspec for $Identity."
                    }
                    $primary = Add-ReviewedText $PackageRoot $review.text
                    $sources += $evidence.texts[$review.text].url
                }
                elseif ($declaration -cin @('Apache-2.0', 'MIT')) {
                    $primary = $null
                    $hash = if ($declaration -ceq 'MIT') { $evidence.texts['.NET Foundation MIT'].sha256 } else { $evidence.projectApacheSha256 }
                    foreach ($entry in $licenseEntries | Where-Object { $_.FullName -notmatch '[/\\]' }) {
                        $text = Read-PackageText $entry
                        if ((Get-PackageTextHash $text) -ceq $hash) {
                            $primary = Add-PackageText $PackageRoot $text
                            break
                        }
                    }
                    if (-not $primary -and $declaration -ceq 'Apache-2.0' -and $sourceUrl) {
                        $review = $evidence.apacheSources[$sourceUrl]
                        if ($review -and $commit -ceq $review.commit) {
                            $primary = 'LICENSE.txt'
                            $sources += "$($sourceUrl -creplace '\.git$')/blob/$commit/LICENSE"
                        }
                    }
                }
                else { $primary = $null }
                if (-not $primary) {
                    throw "No committed license text for '$declaration' required by $Identity. Review the resolved package before packaging."
                }
            }
            default { throw "Unrecognized license metadata for $Identity. Review the resolved package before packaging." }
        }
        $licenseFiles = @($primary) + @($licenseEntries | ForEach-Object { Add-PackageText $PackageRoot (Read-PackageText $_) })
        $noticeFiles = @($noticeEntries | ForEach-Object { Add-PackageText $PackageRoot (Read-PackageText $_) })
        if ($id -ieq 'Microsoft.WindowsDesktop.App.Runtime.win-x64') {
            foreach ($framework in 'wpf', 'winforms') {
                $noticeFiles += Add-ReviewedText $PackageRoot $framework
                $sources += $evidence.texts[$framework].url
            }
        }
        if ($id -iin @('Grpc.Net.Client', 'Grpc.AspNetCore.Server')) {
            if ($sourceUrl -cne $evidence.grpcShared.repository -or $commit -cne $evidence.grpcShared.commit) {
                throw "Unreviewed shared-code license provenance for $Identity. Review NonCapturingTimer and other bundled sources."
            }
            $licenseFiles += Add-ReviewedText $PackageRoot '.NET Foundation MIT'
            $sources += $evidence.grpcShared.url
        }
        $perFile = [ordered]@{}
        if ($runtimeReview -and -not $AppHost) {
            foreach ($path in $Files) {
                $nativeName = $Native[$path]
                if (-not $nativeName -and $path -match '\.(dll|exe)$') {
                    try { [void][Reflection.AssemblyName]::GetAssemblyName((Join-Path $PackageRoot $path)) }
                    catch [BadImageFormatException] { $nativeName = [IO.Path]::GetFileName($path) }
                }
                if (-not $nativeName) { continue }
                $terms = @($runtimeReview.Keys | Where-Object { $nativeName -iin $runtimeReview[$_] })
                if ($terms.Count -ne 1) { throw "Unreviewed native runtime file '$path' ($nativeName) in $Identity." }
                if ($terms[0] -ceq 'MIT') { continue }
                $textPath = Add-ReviewedText $PackageRoot $terms[0]
                $licenseFiles += $textPath
                $perFile[$path] = [ordered]@{ license = $terms[0]; licenseFile = $textPath; source = $evidence.texts[$terms[0]].url }
            }
        }
        $row = [ordered]@{
            component = $Identity
            declaredLicense = $declaration
            declaredLicenseFile = $primary
            packageCopyright = $packageCopyright
            binaryCopyright = $binaryCopyright
            licenseFiles = @($licenseFiles | Sort-Object -Unique -CaseSensitive)
            noticeFiles = @($noticeFiles | Sort-Object -Unique -CaseSensitive)
            files = $Files
        }
        if (-not $packageCopyright) { $row['attribution'] = 'No copyright attribution in the NuGet declaration.' }
        if ($sources) { $row['reviewedSources'] = $sources }
        if ($perFile.Count) { $row['perFileLicenses'] = $perFile }
        $row
    }
    finally { $archive.Dispose() }
}

function Write-WindowsPackageNotices {
    param([string]$PackageRoot, [string]$TargetDirectory, $Publishes)

    $root = Join-Path $PackageRoot 'licenses'
    [void][IO.Directory]::CreateDirectory((Join-Path $root 'texts'))
    if ((Get-FileHash -LiteralPath (Join-Path $PackageRoot 'LICENSE.txt')).Hash -ine $evidence.projectApacheSha256) {
        throw 'Root Apache-2.0 text differs from its reviewed digest.'
    }
    $owners = @{}
    $components = Get-PublishedComponents $Publishes $TargetDirectory $owners
    $index = @(foreach ($key in ($components.Keys | Sort-Object -CaseSensitive -Culture 'en-US')) {
        $component = $components[$key]
        $files = @($component.Files | Sort-Object -CaseSensitive -Culture 'en-US')
        if ($key -like 'Microsoft.NETCore.App.Host.win-x64/*' -and -not (Get-ChildItem -LiteralPath $component.Directory -Filter '*.nupkg' -File)) {
            $version = $key.Split('/')[1]
            $runtimeKey = "Microsoft.NETCore.App.Runtime.win-x64/$version"
            if (-not $components.ContainsKey($runtimeKey)) { throw "No matching .NET runtime pack for SDK apphost $key." }
            $row = Get-RestoredPackageNotices $PackageRoot $runtimeKey $components[$runtimeKey].Directory $files -AppHost
            $row.component = $key
            $row['textSource'] = $runtimeKey
        }
        else { $row = Get-RestoredPackageNotices $PackageRoot $key $component.Directory $files $component.Native }
        $row
    })
    $assets = Join-Path $TargetDirectory 'src/Briosa.ControlCenter/Assets'
    $brand = Join-Path $assets 'Brand'
    $metadataDirectory = Join-Path $PackageRoot 'metadata/desktop-brand'
    foreach ($pair in @(@('Brand/provenance.json', 'provenance.json'), @('AppIcon/derivation.json', 'icon-derivation.json'))) {
        if ((Get-FileHash -LiteralPath (Join-Path $metadataDirectory $pair[1])).Hash -ine
            (Get-FileHash -LiteralPath (Join-Path $assets $pair[0])).Hash) {
            throw "Packaged brand metadata differs from its committed record: $($pair[1])."
        }
    }
    $brandProvenance = Get-Content -LiteralPath (Join-Path $brand 'provenance.json') -Raw | ConvertFrom-Json
    $icon = Get-Content -LiteralPath (Join-Path $assets 'AppIcon/derivation.json') -Raw | ConvertFrom-Json
    if ($icon.source -cne $evidence.icon.source -or $icon.commit -cne $evidence.icon.commit -or
        $evidence.icon.licenseSha256 -cne $evidence.projectApacheSha256 -or
        (Get-FileHash -LiteralPath (Join-Path $assets 'AppIcon/briosa.ico')).Hash -ine $icon.sha256) {
        throw 'The desktop icon differs from its reviewed derivation or license source.'
    }
    $embedded = @{ 'assets/appicon/briosa.ico' = $icon.sha256 }
    foreach ($asset in $brandProvenance.assets) {
        if ((Get-FileHash -LiteralPath (Join-Path $brand $asset.path)).Hash -ine $asset.sha256) {
            throw "Desktop brand asset '$($asset.path)' differs from its reviewed digest."
        }
        if ($asset.path -match '\.(svg|ttf)$') { $embedded["assets/brand/$($asset.path.ToLowerInvariant())"] = $asset.sha256 }
    }
    foreach ($publish in $Publishes | Where-Object { $_.Receipt.Properties.TargetName -cin @('Briosa.ControlCenter', 'Briosa.ControlCenter.App') }) {
        $properties = $publish.Receipt.Properties
        $iconPath = [IO.Path]::GetFullPath($properties.ApplicationIcon, (Split-Path -Parent $properties.MSBuildProjectFullPath))
        if ($iconPath -ine (Join-Path $assets 'AppIcon/briosa.ico')) {
            throw "Unexpected ApplicationIcon for '$($properties.TargetName)'. Review its derivation before packaging."
        }
    }
    Assert-BrandResourceHash (Join-Path $PackageRoot 'desktop/Briosa.ControlCenter.App.dll') $embedded
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
        sourceCommit = $brandProvenance.commit
        declaredLicense = 'Apache-2.0 for artwork; OFL-1.1 for Inter and Saira'
        licenseFiles = @('LICENSE.txt', 'licenses/desktop-brand/fonts/Inter-OFL.txt', 'licenses/desktop-brand/fonts/Saira-OFL.txt')
        noticeFiles = @('licenses/desktop-brand/THIRD-PARTY-NOTICES.md')
        embeddedIn = @('desktop/Briosa.ControlCenter.App.dll')
    }
    $index += [ordered]@{
        component = $icon.source
        sourceCommit = $icon.commit
        declaredLicense = 'Apache-2.0'
        licenseFiles = @('LICENSE.txt')
        noticeFiles = @()
        reviewedSources = @($evidence.icon.licenseUrl)
        derivation = 'metadata/desktop-brand/icon-derivation.json'
        embeddedIn = @('Briosa.ControlCenter.dll', 'Briosa.ControlCenter.exe', 'desktop/Briosa.ControlCenter.App.exe', 'desktop/Briosa.ControlCenter.App.dll')
    }
    $document = [ordered]@{ schemaVersion = 2; components = $index }
    $indexText = ($document | ConvertTo-Json -Depth 12).Replace("`r`n", "`n") + "`n"
    [IO.File]::WriteAllText((Join-Path $root 'index.json'), $indexText)
    [IO.File]::WriteAllText((Join-Path $root 'README.md'), @'
# Third-party licenses and notices

`index.json` lists the components and versions shipped in this archive, their
license declarations, available copyright attributions, license and notice
texts, and published files. Paths in the index are relative to the archive's
product directory. Identical texts are shared by all applicable components.
The root `LICENSE.txt` is also the Apache 2.0 text where referenced.
For Microsoft runtime packs, `declaredLicense` describes NuGet metadata only.
The entries under `perFileLicenses` give the terms for those specific paths
instead of the pack's MIT declaration.
'@.Replace("`r`n", "`n") + "`n")
    @{
        IndexText = $indexText
        PayloadPaths = @($owners.Keys)
        TextPaths = @($index | ForEach-Object { $_.licenseFiles + $_.noticeFiles } | Sort-Object -Unique)
    }
}

function Get-WindowsPackageNoticeExpectation {
    param([string]$PackageRoot, $Notice)

    $files = @{}
    $packageMetadata = @('CONTROL-CENTER.md', 'files.sha256', 'HEALTH-AND-DISCOVERY.md', 'LICENSE.txt', 'manifest.json',
        'README.md', 'licenses/README.md', 'metadata/interop-provenance.json',
        'metadata/desktop-brand/icon-derivation.json', 'metadata/desktop-brand/provenance.json')
    foreach ($path in $Notice.PayloadPaths + $Notice.TextPaths + $packageMetadata | Sort-Object -Unique) {
        $files[$path] = (Get-FileHash -LiteralPath (Join-Path $PackageRoot $path)).Hash
    }
    @{ Files = $files; IndexText = $Notice.IndexText }
}

function Assert-WindowsPackageNotices {
    param(
        [Parameter(Mandatory)][string]$PackageRoot,
        [Parameter(Mandatory)]$Expected,
        [switch]$Quiet
    )

    $indexPath = Join-Path $PackageRoot 'licenses/index.json'
    if (-not [IO.File]::Exists($indexPath) -or [IO.File]::ReadAllText($indexPath) -cne $Expected.IndexText) {
        throw 'Notice index differs from the owners, paths, and required texts resolved by the independent publish.'
    }
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($file in Get-ChildItem -LiteralPath $PackageRoot -File -Recurse) {
        $path = [IO.Path]::GetRelativePath($PackageRoot, $file.FullName).Replace('\', '/')
        if ($path -ceq 'licenses/index.json') { continue }
        if (-not $Expected.Files.ContainsKey($path) -or -not $seen.Add($path)) {
            throw "Packaged file inventory differs: unexpected '$path'."
        }
        $actualHash = (Get-FileHash -LiteralPath $file.FullName).Hash
        if ($path -cmatch '^licenses/texts/([0-9a-f]{64})\.txt$' -and $actualHash -ine $Matches[1]) {
            throw "Packaged rights text differs from its content-addressed name: '$path'."
        }
        if ($actualHash -cne $Expected.Files[$path]) {
            throw "Packaged file digest differs from its independently assembled source: '$path'."
        }
    }
    foreach ($path in $Expected.Files.Keys) {
        if (-not $seen.Contains($path)) { throw "Packaged file inventory differs: missing '$path'." }
    }
    if (-not $Quiet) { Write-Host 'Verified third-party owners, packaged files and rights texts against the independent publish.' }
}

Export-ModuleMember -Function Write-WindowsPackageNotices, Get-WindowsPackageNoticeExpectation, Assert-WindowsPackageNotices
