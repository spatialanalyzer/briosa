Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-ReleaseSigningFiles {
    param([Parameter(Mandatory)][string]$PackageRoot)
    $root = (Resolve-Path -LiteralPath $PackageRoot).Path
    $manifest = Get-Content -LiteralPath (Join-Path $root 'manifest.json') -Raw | ConvertFrom-Json
    $name = $manifest.artifactName
    if ($name -notmatch '^briosa-(?:installer-)?[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?(?:-sa-[0-9.]+)?-win-x64$' -or
        $name -cne (Split-Path -Leaf $root) -or $manifest.runtimeIdentifier -cne 'win-x64') {
        throw 'Unexpected Windows release package identity.'
    }
    $installer = $null -ne $manifest.PSObject.Properties['component'] -and $manifest.component -ceq 'installer'
    if ($installer) {
        $required = @('Briosa.Installer.exe', 'Briosa.Installer.Cli.exe', 'Briosa.Launcher.exe', 'Install-DesktopShortcut.ps1')
        if ($manifest.schemaVersion -ne 1 -or -not $name.StartsWith('briosa-installer-')) { throw 'Invalid installer manifest.' }
    } else {
        $required = @('Briosa.Server.exe', 'Briosa.Worker.exe')
        if ($manifest.schemaVersion -ne 2 -or $name -cne "briosa-$($manifest.briosaVersion)-sa-$($manifest.spatialAnalyzerTarget)-win-x64") { throw 'Invalid server manifest.' }
    }
    foreach ($requiredName in $required) {
        if (-not (Test-Path -LiteralPath (Join-Path $root $requiredName) -PathType Leaf)) { throw "Missing first-party release file: $requiredName" }
    }
    # Only Briosa assemblies/entry points and the shipped shortcut script receive our signature.
    # Preserve existing third-party runtime signatures.
    $files = @(Get-ChildItem -LiteralPath $root -File -Recurse | Where-Object {
        $_.Name -cmatch '^Briosa\..+\.(exe|dll)$' -or ($installer -and $_.Name -ceq 'Install-DesktopShortcut.ps1')
    } | Sort-Object FullName)
    if ($files.Count -eq 0) { throw 'No first-party files to sign.' }
    return $files
}

function Assert-ReleaseTree {
    param([Parameter(Mandatory)][string]$PackageRoot)
    $root = Get-Item -LiteralPath $PackageRoot
    if ($root.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Package root is a reparse point.' }
    foreach ($item in Get-ChildItem -LiteralPath $root.FullName -Recurse -Force) {
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Release trees cannot contain reparse points.' }
    }
}

function Assert-ReleaseChecksums {
    param([Parameter(Mandatory)][string]$PackageRoot)
    Assert-ReleaseTree $PackageRoot
    $root = (Resolve-Path -LiteralPath $PackageRoot).Path
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($line in Get-Content -LiteralPath (Join-Path $root 'files.sha256')) {
        if ($line -notmatch '^([a-fA-F0-9]{64})  (.+)$') { throw 'Malformed internal checksum.' }
        $expected = $Matches[1]
        $relative = $Matches[2]
        $path = [IO.Path]::GetFullPath((Join-Path $root $relative))
        if (-not $path.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
            $relative.Contains(':') -or -not $seen.Add($relative.Replace('\', '/')) -or $relative -ieq 'files.sha256') { throw 'Unsafe or duplicate internal checksum path.' }
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $expected) { throw "Internal checksum differs: $relative" }
    }
    $actual = @(Get-ChildItem -LiteralPath $root -File -Recurse | Where-Object Name -NE 'files.sha256')
    if ($actual.Count -ne $seen.Count) { throw 'Internal checksums do not cover the complete release tree.' }
    foreach ($file in $actual) {
        if (-not $seen.Contains([IO.Path]::GetRelativePath($root, $file.FullName).Replace('\', '/'))) { throw 'Unlisted file in release tree.' }
    }
}

function Assert-ReleaseSignatures {
    param([Parameter(Mandatory)][string]$PackageRoot, [Parameter(Mandatory)][string]$ExpectedPublisher)
    Assert-ReleaseTree $PackageRoot
    foreach ($file in Get-ReleaseSigningFiles $PackageRoot) {
        $signature = Get-AuthenticodeSignature -LiteralPath $file.FullName
        if ($signature.Status -ne 'Valid' -or $null -eq $signature.TimeStamperCertificate -or
            $signature.SignerCertificate.GetNameInfo([Security.Cryptography.X509Certificates.X509NameType]::SimpleName, $false) -cne $ExpectedPublisher) {
            throw "Missing, invalid, untimestamped, or unexpected-publisher signature: $($file.Name)"
        }
    }
}

Export-ModuleMember -Function Get-ReleaseSigningFiles, Assert-ReleaseTree, Assert-ReleaseChecksums, Assert-ReleaseSignatures
