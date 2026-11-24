param(
    [Parameter(Mandatory)][string]$PackageRoot,
    [Parameter(Mandatory)][string]$TargetDirectory,
    [Parameter(Mandatory)]$Expected
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$module = Get-Module WindowsPackageNotices
$failures = [Collections.Generic.List[string]]::new()

function Assert-Rejected($Name, $Pattern, [scriptblock]$Action) {
    try { & $Action | Out-Null }
    catch {
        if ($_.Exception.Message -notlike $Pattern) { throw }
        Write-Host "Notice regression rejected: $Name"
        return
    }
    $failures.Add($Name)
    Write-Host "Notice regression accepted unexpectedly: $Name"
}

function New-FixturePackage($Identity, $Declaration, $Files, $Repository = '', $Suffix = '') {
    $id, $version = $Identity.Split('/')
    $path = Join-Path $fixture "$($Identity.Replace('/', '-'))$Suffix"
    [void][IO.Directory]::CreateDirectory($path)
    $archive = [IO.Compression.ZipFile]::Open((Join-Path $path "$id.$version.nupkg".ToLowerInvariant()), 'Create')
    try {
        $entries = @{ "$id.nuspec" = "<package><metadata><id>$id</id><version>$version</version>$Declaration$Repository</metadata></package>" }
        foreach ($file in $Files.Keys) { $entries[$file] = $Files[$file] }
        foreach ($entry in $entries.GetEnumerator()) {
            $writer = [IO.StreamWriter]::new($archive.CreateEntry($entry.Key).Open())
            try { $writer.Write($entry.Value) } finally { $writer.Dispose() }
        }
    }
    finally { $archive.Dispose() }
    $path
}

function Test-IndexMutation($Name, [scriptblock]$Mutate) {
    $index = [Text.Encoding]::UTF8.GetString($originalIndex) | ConvertFrom-Json -AsHashtable
    try {
        & $Mutate $index
        [IO.File]::WriteAllText($indexPath, (($index | ConvertTo-Json -Depth 20).Replace("`r`n", "`n") + "`n"))
        Assert-Rejected $Name 'Notice index differs*' {
            Assert-WindowsPackageNotices $PackageRoot $Expected
        }
    }
    finally { [IO.File]::WriteAllBytes($indexPath, $originalIndex) }
}

$fixtureBase = Join-Path $TargetDirectory 'artifacts'
$fixture = Join-Path $fixtureBase "notice-gates-$([guid]::NewGuid().ToString('N'))"
[void][IO.Directory]::CreateDirectory((Join-Path $fixture 'publish'))
[void][IO.Directory]::CreateDirectory((Join-Path $fixture 'licenses/texts'))
try {
    Copy-Item -LiteralPath (Join-Path $PackageRoot 'LICENSE.txt') -Destination $fixture
    $native = 'review-vendored-native.dll'
    Copy-Item -LiteralPath (Join-Path $PackageRoot 'hostfxr.dll') -Destination (Join-Path $fixture $native)
    Copy-Item -LiteralPath (Join-Path $fixture $native) -Destination (Join-Path $fixture 'publish')
    $assets = Join-Path $fixture 'project.assets.json'
    [IO.File]::WriteAllText($assets, '{"packageFolders":{}}')
    $appHost = Join-Path $fixture 'microsoft.netcore.app.host.win-x64/10.0.15'
    $publish = @{
        Prefix = ''; Output = (Join-Path $fixture 'publish')
        Receipt = @{
            Properties = @{ ProjectAssetsFile = $assets; AppHostSourcePath = (Join-Path $fixture $native); TargetName = 'Briosa.ControlCenter'; StaticWebAssetEndpointsPublishManifestPath = '' }
            Items = @{
                ResolvedAppHostPack = @(@{ Path = (Join-Path $fixture $native); PackageDirectory = $appHost })
                ResolvedFileToPublish = @(@{ RelativePath = $native; FullPath = (Join-Path $fixture $native); CopyToPublishDirectory = 'Always' })
            }
        }
    }
    Assert-Rejected 'vendored native publish Content' 'Unclassified publish asset*' {
        & $module { Get-PublishedComponents @($args[0]) $args[1] } $publish $TargetDirectory
    }

    $identity = 'NoticeGate.Sample/1.0.0'
    $directory = New-FixturePackage $identity '<license type="expression">BSD-4-Clause</license>' @{
        'third_party/dependency/LICENSE.txt' = 'Unrelated terms for a different dependency.'
    }
    Assert-Rejected 'unsupported declaration with unrelated nested license' 'No committed license text*' {
        & $module { Get-RestoredPackageNotices $args[0] $args[1] $args[2] @() } $fixture $identity $directory
    }
    $identity = 'NoticeGate.File/1.0.0'
    $directory = New-FixturePackage $identity '<license type="file">terms/main.txt</license>' @{ 'terms/main.txt' = 'Declared license text.' }
    $control = & $module { Get-RestoredPackageNotices $args[0] $args[1] $args[2] @() } $fixture $identity $directory
    if ($control.licenseFiles.Count -ne 1) { throw 'The declared-file license control failed.' }

    $evidence = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'licenses/reviewed-windows-evidence.json') -Raw | ConvertFrom-Json
    $identity = "Microsoft.WindowsDesktop.App.Runtime.win-x64/$($evidence.runtimeVersion)"
    $desktopFiles = @{
        'LICENSE.TXT' = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'licenses/dotnet-foundation-MIT.txt') -Raw
        'NOTICE.txt' = 'Unrelated notice, not the WPF or WinForms notice.'
    }
    $directory = New-FixturePackage $identity '<license type="expression">MIT</license>' $desktopFiles '<repository url="https://github.com/dotnet/dotnet" commit="0000000000000000000000000000000000000000"/>'
    Assert-Rejected 'unreviewed desktop commit with unrelated notice' 'Unreviewed Windows Desktop runtime source*' {
        & $module { Get-RestoredPackageNotices $args[0] $args[1] $args[2] @() } $fixture $identity $directory
    }
    $reviewedRepository = '<repository url="https://github.com/dotnet/dotnet" commit="' + $evidence.desktopSourceCommit + '"/>'
    $directory = New-FixturePackage $identity '<license type="expression">MIT</license>' $desktopFiles $reviewedRepository '-reviewed'
    $control = & $module { Get-RestoredPackageNotices $args[0] $args[1] $args[2] @() } $fixture $identity $directory
    foreach ($framework in 'wpf', 'winforms') {
        if ("licenses/texts/$($evidence.texts.$framework.sha256).txt" -cnotin $control.noticeFiles) {
            throw "Reviewed desktop supplement missing despite an in-pack notice: $framework."
        }
    }
    Copy-Item -LiteralPath (Join-Path $fixture $native) -Destination (Join-Path $fixture 'review.dll')
    Assert-Rejected 'unexpected native runtime file' 'Unreviewed native runtime file*' {
        & $module {
            Get-RestoredPackageNotices $args[0] $args[1] $args[2] @('review.dll') @{ 'review.dll' = 'review.dll' }
        } $fixture $identity $directory
    }
    Assert-Rejected 'unreviewed runtime version' 'Unreviewed Windows runtime pack*' {
        & $module { Get-RestoredPackageNotices $args[0] 'Microsoft.WindowsDesktop.App.Runtime.win-x64/999.0.0' $args[1] @() } $fixture $directory
    }

    $indexPath = Join-Path $PackageRoot 'licenses/index.json'
    $originalIndex = [IO.File]::ReadAllBytes($indexPath)
    Test-IndexMutation 'invented component' {
        param($Index)
        $Index.components += @{ component = 'Invented/1.0.0'; declaredLicense = 'Apache-2.0'; licenseFiles = @('LICENSE.txt'); noticeFiles = @(); files = @() }
    }
    Test-IndexMutation 'wrong license assigned to gRPC' {
        param($Index)
        ($Index.components | Where-Object component -Like 'Grpc.Core.Api/*').licenseFiles = ($Index.components | Where-Object component -Like 'Google.Protobuf/*').licenseFiles
    }
    Test-IndexMutation 'missing OpenTelemetry notice reference' {
        param($Index)
        ($Index.components | Where-Object component -Like 'OpenTelemetry.Api/*').noticeFiles = @()
    }
    Test-IndexMutation 'missing runtime pack paths' {
        param($Index)
        ($Index.components | Where-Object component -Like 'Microsoft.AspNetCore.App.Runtime.win-x64/*').files = @()
    }
    $assembly = Join-Path $PackageRoot 'Grpc.Core.Api.dll'
    $renamed = "$assembly.renamed"
    [IO.File]::Move($assembly, $renamed)
    try {
        Assert-Rejected 'renamed shipped assembly' 'Packaged file inventory differs*' {
            Assert-WindowsPackageNotices $PackageRoot $Expected
        }
    }
    finally { [IO.File]::Move($renamed, $assembly) }
    $brandFiles = @('licenses/desktop-brand/fonts/Inter-OFL.txt', 'licenses/desktop-brand/fonts/Saira-OFL.txt', 'licenses/desktop-brand/THIRD-PARTY-NOTICES.md')
    try {
        foreach ($file in $brandFiles) { [IO.File]::Move((Join-Path $PackageRoot $file), (Join-Path $fixture (Split-Path $file -Leaf))) }
        Test-IndexMutation 'missing brand texts and references' {
            param($Index)
            $row = $Index.components | Where-Object component -Like '*brand@v2.1.0'
            $row.licenseFiles = @('LICENSE.txt')
            $row.noticeFiles = @()
        }
    }
    finally {
        foreach ($file in $brandFiles) {
            $saved = Join-Path $fixture (Split-Path $file -Leaf)
            if ([IO.File]::Exists($saved)) { [IO.File]::Move($saved, (Join-Path $PackageRoot $file)) }
        }
    }
    foreach ($file in @('LICENSE.txt', $brandFiles[0], 'metadata/desktop-brand/icon-derivation.json')) {
        $path = Join-Path $PackageRoot $file
        $saved = [IO.File]::ReadAllBytes($path)
        try {
            [IO.File]::AppendAllText($path, 'Modified after package generation.')
            Assert-Rejected "corrupted $file" 'Packaged file digest differs*' {
                Assert-WindowsPackageNotices $PackageRoot $Expected
            }
        }
        finally { [IO.File]::WriteAllBytes($path, $saved) }
    }
    $text = Get-ChildItem -LiteralPath (Join-Path $PackageRoot 'licenses/texts') -File | Select-Object -First 1
    $relativePath = "licenses/texts/$($text.Name)"
    $originalBytes = [IO.File]::ReadAllBytes($text.FullName)
    $sameStagedBytes = @{ IndexText = $Expected.IndexText; Files = $Expected.Files.Clone() }
    try {
        [IO.File]::AppendAllText($text.FullName, 'Different text at a reserved digest path.')
        $sameStagedBytes.Files[$relativePath] = (Get-FileHash -LiteralPath $text.FullName).Hash
        Assert-Rejected 'wrong rights text staged identically in both builds' 'Packaged rights text differs from its content-addressed name*' {
            Assert-WindowsPackageNotices $PackageRoot $sameStagedBytes
        }
    }
    finally { [IO.File]::WriteAllBytes($text.FullName, $originalBytes) }
    if ($failures.Count) { throw "Notice regressions not rejected: $($failures -join '; ')" }
}
finally {
    if ([IO.Path]::GetFullPath($fixture).StartsWith(([IO.Path]::GetFullPath($fixtureBase) + [IO.Path]::DirectorySeparatorChar), [StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $fixture -Recurse -Force
    }
}
