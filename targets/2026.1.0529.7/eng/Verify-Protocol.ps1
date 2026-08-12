[CmdletBinding()]
param(
    [string]$BufPath = "buf",
    [string]$AgainstRef
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$bufCommand = Get-Command -Name $BufPath -CommandType Application -ErrorAction Stop

if ($null -eq (Get-Command -Name "diff" -CommandType Application -ErrorAction SilentlyContinue)) {
    $gitCommand = @(
        Get-Command -Name "git" -CommandType Application -ErrorAction Stop
    )[0]
    $gitInstallRoot = Split-Path -Parent (Split-Path -Parent $gitCommand.Source)
    $gitDiffDirectory = Join-Path $gitInstallRoot "usr\bin"
    $gitDiffPath = Join-Path $gitDiffDirectory "diff.exe"
    if (-not (Test-Path -LiteralPath $gitDiffPath)) {
        throw "Buf formatting requires diff, and Git for Windows did not provide it."
    }

    $env:PATH = "$gitDiffDirectory$([IO.Path]::PathSeparator)$env:PATH"
}

function Invoke-BufCommand {
    param([Parameter(Mandatory)][string[]]$CommandArguments)

    & $bufCommand.Source @CommandArguments
    if ($LASTEXITCODE -ne 0) {
        throw "buf $($CommandArguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

Push-Location $repositoryRoot
try {
    $schemaFiles = @(Get-ChildItem -Path "proto" -Filter "*.proto" -Recurse -File)
    $hashesBeforeFormatting = @{}
    foreach ($schemaFile in $schemaFiles) {
        $hashesBeforeFormatting[$schemaFile.FullName] =
            (Get-FileHash -LiteralPath $schemaFile.FullName -Algorithm SHA256).Hash
    }

    Invoke-BufCommand -CommandArguments @("format", "--write")
    $changedByFormatter = @(
        foreach ($schemaFile in $schemaFiles) {
            $hashAfterFormatting =
                (Get-FileHash -LiteralPath $schemaFile.FullName -Algorithm SHA256).Hash
            if ($hashAfterFormatting -ne $hashesBeforeFormatting[$schemaFile.FullName]) {
                $schemaFile.FullName
            }
        }
    )
    if ($changedByFormatter.Count -gt 0) {
        throw "Buf reformatted: $($changedByFormatter -join ', ')"
    }

    Invoke-BufCommand -CommandArguments @("lint")
    Invoke-BufCommand -CommandArguments @("build")

    if (-not [string]::IsNullOrWhiteSpace($AgainstRef)) {
        $safeRepositoryRoot = $repositoryRoot.Replace('\', '/')
        $worktreeRoot = (& git `
            -c "safe.directory=$safeRepositoryRoot" `
            -C $repositoryRoot `
            rev-parse --show-toplevel).Trim()
        if ($LASTEXITCODE -ne 0) {
            throw "Could not locate the Git worktree containing $repositoryRoot."
        }

        $safeWorktreeRoot = $worktreeRoot.Replace('\', '/')
        $gitDirectory = (& git `
            -c "safe.directory=$safeWorktreeRoot" `
            -C $worktreeRoot `
            rev-parse --absolute-git-dir).Trim().Replace('\', '/')
        if ($LASTEXITCODE -ne 0) {
            throw "Could not locate the Git directory for $worktreeRoot."
        }

        $targetSubdirectory = [IO.Path]::GetRelativePath(
            $worktreeRoot,
            $repositoryRoot).Replace('\', '/')
        $baselineProtoPath = "$targetSubdirectory/proto"
        $baselinePath = & git `
            -c "safe.directory=$safeWorktreeRoot" `
            -C $worktreeRoot `
            ls-tree -d --name-only $AgainstRef -- $baselineProtoPath
        if ($LASTEXITCODE -ne 0) {
            throw "Could not inspect protobuf baseline at $AgainstRef."
        }

        $hasBaseline = $baselinePath -contains $baselineProtoPath

        if ($hasBaseline) {
            Invoke-BufCommand -CommandArguments @(
                "breaking",
                "--against",
                "$gitDirectory#ref=$AgainstRef,subdir=$targetSubdirectory"
            )
        }
        else {
            Write-Host "No protobuf baseline exists at $AgainstRef; skipping the breaking comparison."
        }
    }
}
finally {
    Pop-Location
}
