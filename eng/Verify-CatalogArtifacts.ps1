[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$catalogRoot = Join-Path $repositoryRoot "catalog"
$generatorProject = Join-Path $repositoryRoot "tools\Briosa.Generator\Briosa.Generator.csproj"
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-catalog-$([Guid]::NewGuid().ToString('N'))"

function Get-NormalizedRelativePath {
    param(
        [Parameter(Mandatory)][string]$BasePath,
        [Parameter(Mandatory)][string]$Path
    )

    return [IO.Path]::GetRelativePath($BasePath, $Path).Replace('\', '/')
}

if (-not $NoBuild) {
    & dotnet build $generatorProject -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Catalog generator build failed with exit code $LASTEXITCODE."
    }
}

try {
    & dotnet run `
        --project $generatorProject `
        -c $Configuration `
        --no-build `
        --no-restore `
        -- `
        catalog-generate `
        $catalogRoot `
        $temporaryRoot
    if ($LASTEXITCODE -ne 0) {
        throw "Catalog artifact generation failed with exit code $LASTEXITCODE."
    }

    $expectedFiles = @(
        Get-ChildItem -LiteralPath $temporaryRoot -File -Recurse |
            ForEach-Object { Get-NormalizedRelativePath $temporaryRoot $_.FullName } |
            Sort-Object
    )

    $actualFiles = @()
    $protoRoot = Join-Path $repositoryRoot "proto\briosa\sa"
    if (Test-Path -LiteralPath $protoRoot) {
        $actualFiles += @(
            Get-ChildItem -LiteralPath $protoRoot -Filter "operations.proto" -File -Recurse |
                ForEach-Object { Get-NormalizedRelativePath $repositoryRoot $_.FullName }
        )
    }

    $serverGeneratedRoot = Join-Path $repositoryRoot "src\Briosa.Server\Generated"
    if (Test-Path -LiteralPath $serverGeneratedRoot) {
        $actualFiles += @(
            Get-ChildItem -LiteralPath $serverGeneratedRoot -Filter "*.g.cs" -File -Recurse |
                ForEach-Object { Get-NormalizedRelativePath $repositoryRoot $_.FullName }
        )
    }

    $documentationRoot = Join-Path $repositoryRoot "docs\reference\generated\sa"
    if (Test-Path -LiteralPath $documentationRoot) {
        $actualFiles += @(
            Get-ChildItem -LiteralPath $documentationRoot -Filter "operations.md" -File -Recurse |
                ForEach-Object { Get-NormalizedRelativePath $repositoryRoot $_.FullName }
        )
    }

    $coverageRoot = Join-Path $repositoryRoot "generated\catalog\sa"
    if (Test-Path -LiteralPath $coverageRoot) {
        $actualFiles += @(
            Get-ChildItem -LiteralPath $coverageRoot -Filter "coverage.json" -File -Recurse |
                ForEach-Object { Get-NormalizedRelativePath $repositoryRoot $_.FullName }
        )
    }

    $actualFiles = @($actualFiles | Sort-Object)
    $pathDifferences = @(Compare-Object $expectedFiles $actualFiles)
    if ($pathDifferences.Count -ne 0) {
        $details = $pathDifferences | ForEach-Object { "$($_.SideIndicator) $($_.InputObject)" }
        throw "Generated catalog artifact paths are stale:`n$($details -join "`n")"
    }

    foreach ($relativePath in $expectedFiles) {
        $expectedPath = Join-Path $temporaryRoot $relativePath
        $actualPath = Join-Path $repositoryRoot $relativePath
        $expectedHash = [Convert]::ToHexString([IO.File]::ReadAllBytes($expectedPath))
        $actualHash = [Convert]::ToHexString([IO.File]::ReadAllBytes($actualPath))
        if ($expectedHash -ne $actualHash) {
            throw "Generated catalog artifact '$relativePath' is stale."
        }
    }

    Write-Host "Verified $($expectedFiles.Count) generated catalog artifact(s)."
}
finally {
    $resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
    if ($resolvedTemporaryRoot.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}
