[CmdletBinding()]
param([Parameter(Mandatory)][string]$PackagePath, [Parameter(Mandatory)][string]$OutputDirectory)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'ReleasePackageSigning.psm1') -Force
$zip = (Resolve-Path -LiteralPath $PackagePath).Path
$destination = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $destination) { throw 'Signing staging directory must be new.' }
$name = [IO.Path]::GetFileNameWithoutExtension($zip)
if ($name -notmatch '^briosa-[a-zA-Z0-9.-]+-win-x64$') { throw 'Unexpected archive filename.' }
$checksum = (Get-Content -LiteralPath "$zip.sha256" -Raw).Trim()
if ($checksum -ine "$((Get-FileHash -LiteralPath $zip).Hash)  $name.zip") { throw 'Input archive checksum differs.' }
$archive = [IO.Compression.ZipFile]::OpenRead($zip)
try {
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $archive.Entries) {
        $path = $entry.FullName
        if (-not $path.StartsWith("$name/", [StringComparison]::Ordinal) -or $path.Contains('\') -or $path.Contains(':') -or
            ($path.Split('/') | Where-Object { $_ -eq '..' -or $_ -eq '.' -or $_.EndsWith('.') -or $_.EndsWith(' ') }) -or
            -not $seen.Add($path)) { throw 'Unsafe, duplicate, or unexpected archive entry.' }
    }
} finally { $archive.Dispose() }
[IO.Compression.ZipFile]::ExtractToDirectory($zip, $destination)
$root = Join-Path $destination $name
Assert-ReleaseChecksums $root
$manifestBytes = [IO.File]::ReadAllBytes((Join-Path $root 'manifest.json'))
$provenanceBytes = [IO.File]::ReadAllBytes((Join-Path (Split-Path -Parent $zip) "$name.provenance.json"))
if ([Convert]::ToBase64String($manifestBytes) -cne [Convert]::ToBase64String($provenanceBytes)) { throw 'Embedded and external provenance differ.' }
$files = @(Get-ReleaseSigningFiles $root)
$catalog = Join-Path $destination 'authenticode-files.txt'
[IO.File]::WriteAllText($catalog, (($files | ForEach-Object { [IO.Path]::GetRelativePath($destination, $_.FullName) }) -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
if ($env:GITHUB_OUTPUT) {
    "package-root=$root" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
    "files-catalog=$catalog" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
}
Write-Host "Prepared $($files.Count) first-party files in $root"
