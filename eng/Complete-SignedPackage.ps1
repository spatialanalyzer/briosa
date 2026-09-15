[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PackageRoot,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [Parameter(Mandatory)][string]$ExpectedPublisher
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'ReleasePackageSigning.psm1') -Force
$root = (Resolve-Path -LiteralPath $PackageRoot).Path
$output = [IO.Path]::GetFullPath($OutputDirectory)
$name = Split-Path -Leaf $root
Assert-ReleaseSignatures $root $ExpectedPublisher
$null = [IO.Directory]::CreateDirectory($output)
foreach ($leaf in @("$name.zip", "$name.zip.sha256", "$name.provenance.json")) {
    if (Test-Path -LiteralPath (Join-Path $output $leaf)) { throw 'Signed release outputs are immutable; choose a new output directory.' }
}
$checksumFile = Join-Path $root 'files.sha256'
$checksums = Get-ChildItem -LiteralPath $root -File -Recurse | Where-Object FullName -NE $checksumFile | ForEach-Object {
    "$((Get-FileHash -LiteralPath $_.FullName).Hash.ToLowerInvariant())  $([IO.Path]::GetRelativePath($root, $_.FullName).Replace('\', '/'))"
} | Sort-Object
[IO.File]::WriteAllText($checksumFile, ($checksums -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
Assert-ReleaseChecksums $root
$zip = Join-Path $output "$name.zip"
$archive = [IO.Compression.ZipFile]::Open($zip, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem -LiteralPath $root -File -Recurse | Sort-Object FullName) {
        $entry = [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, "$name/$([IO.Path]::GetRelativePath($root, $file.FullName).Replace('\', '/'))", [IO.Compression.CompressionLevel]::Optimal)
        $entry.LastWriteTime = [DateTimeOffset]::new(1980, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
    }
} finally { $archive.Dispose() }
$hash = (Get-FileHash -LiteralPath $zip).Hash.ToLowerInvariant()
[IO.File]::WriteAllText("$zip.sha256", "$hash  $name.zip`n", [Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $root 'manifest.json') -Destination (Join-Path $output "$name.provenance.json")
Write-Host "Verified publisher and timestamp; rebuilt checksums and archive: $zip"
