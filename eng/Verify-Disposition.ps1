[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$dispositionRoot = Join-Path $repositoryRoot "disposition"
$manifestSchema = Join-Path $dispositionRoot "schemas\v1\manifest.schema.json"
$shardSchema = Join-Path $dispositionRoot "schemas\v1\shard.schema.json"
$generatorProject = Join-Path $repositoryRoot "tools\Briosa.Generator\Briosa.Generator.csproj"
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$temporaryRoot = Join-Path $temporaryBase "briosa-disposition-$([Guid]::NewGuid().ToString('N'))"

function Test-DispositionJson {
    param(
        [Parameter(Mandatory)][string]$DocumentPath,
        [Parameter(Mandatory)][string]$SchemaPath
    )

    $json = Get-Content -LiteralPath $DocumentPath -Raw
    $valid = Test-Json -Json $json -SchemaFile $SchemaPath -ErrorAction Stop
    if (-not $valid) {
        throw "JSON Schema validation failed for '$DocumentPath'."
    }
}

function Get-NormalizedRelativePath {
    param(
        [Parameter(Mandatory)][string]$BasePath,
        [Parameter(Mandatory)][string]$Path
    )

    return [IO.Path]::GetRelativePath($BasePath, $Path).Replace('\', '/')
}

$manifests = @(
    Get-ChildItem -Path (Join-Path $dispositionRoot "sa") -Filter "manifest.json" -Recurse -File
)
if ($manifests.Count -eq 0) {
    throw "No exact-target disposition manifests were found."
}

if (-not $NoBuild) {
    & dotnet build $generatorProject -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Disposition validator build failed with exit code $LASTEXITCODE."
    }
}

try {
    foreach ($manifestPath in $manifests.FullName) {
        Test-DispositionJson -DocumentPath $manifestPath -SchemaPath $manifestSchema

        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        $targetDirectory = Split-Path -Parent $manifestPath
        $inventoryPath = [IO.Path]::GetFullPath((Join-Path $targetDirectory $manifest.inventory.path))
        foreach ($shard in $manifest.shards) {
            $shardPath = Join-Path $targetDirectory $shard.path
            Test-DispositionJson -DocumentPath $shardPath -SchemaPath $shardSchema
        }

        & dotnet run `
            --project $generatorProject `
            -c $Configuration `
            --no-build `
            --no-restore `
            -- `
            disposition-validate `
            $inventoryPath `
            $targetDirectory
        if ($LASTEXITCODE -ne 0) {
            throw "Disposition semantic validation failed with exit code $LASTEXITCODE."
        }
    }

    $temporaryDisposition = Join-Path $temporaryRoot "disposition"
    $temporaryInventory = Join-Path $temporaryRoot "inventory"
    New-Item -ItemType Directory -Path $temporaryDisposition -Force | Out-Null
    New-Item -ItemType Directory -Path $temporaryInventory -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $dispositionRoot "sa") -Destination $temporaryDisposition -Recurse
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "inventory\sa") -Destination $temporaryInventory -Recurse

    foreach ($manifestPath in $manifests.FullName) {
        $targetDirectory = Split-Path -Parent $manifestPath
        $targetRelativePath = Get-NormalizedRelativePath $repositoryRoot $targetDirectory
        $temporaryTarget = Join-Path $temporaryRoot $targetRelativePath
        $temporaryManifest = Get-Content `
            -LiteralPath (Join-Path $temporaryTarget "manifest.json") `
            -Raw | ConvertFrom-Json
        $temporaryInventoryPath = [IO.Path]::GetFullPath(
            (Join-Path $temporaryTarget $temporaryManifest.inventory.path))

        & dotnet run `
            --project $generatorProject `
            -c $Configuration `
            --no-build `
            --no-restore `
            -- `
            disposition-sync `
            $temporaryInventoryPath `
            $temporaryTarget
        if ($LASTEXITCODE -ne 0) {
            throw "Disposition synchronization failed with exit code $LASTEXITCODE."
        }

        $expectedFiles = @(
            Get-ChildItem -LiteralPath $temporaryTarget -File -Recurse |
                ForEach-Object { Get-NormalizedRelativePath $temporaryTarget $_.FullName } |
                Sort-Object
        )
        $actualFiles = @(
            Get-ChildItem -LiteralPath $targetDirectory -File -Recurse |
                ForEach-Object { Get-NormalizedRelativePath $targetDirectory $_.FullName } |
                Sort-Object
        )
        $pathDifferences = @(Compare-Object $expectedFiles $actualFiles)
        if ($pathDifferences.Count -ne 0) {
            $details = $pathDifferences | ForEach-Object { "$($_.SideIndicator) $($_.InputObject)" }
            throw "Disposition artifact paths are stale:`n$($details -join "`n")"
        }

        foreach ($relativePath in $expectedFiles) {
            $expectedPath = Join-Path $temporaryTarget $relativePath
            $actualPath = Join-Path $targetDirectory $relativePath
            $expectedHash = [Convert]::ToHexString([IO.File]::ReadAllBytes($expectedPath))
            $actualHash = [Convert]::ToHexString([IO.File]::ReadAllBytes($actualPath))
            if ($expectedHash -ne $actualHash) {
                throw "Disposition artifact '$targetRelativePath/$relativePath' is stale."
            }
        }
    }

    Write-Host "Verified $($manifests.Count) exact-target disposition ledger(s)."
}
finally {
    $resolvedTemporaryRoot = [IO.Path]::GetFullPath($temporaryRoot)
    if ($resolvedTemporaryRoot.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}
