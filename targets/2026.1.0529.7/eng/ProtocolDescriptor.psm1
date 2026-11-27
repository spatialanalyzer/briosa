Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# The one way this target builds its protocol descriptor set. The protocol
# artifact and the Windows package both call New-ProtocolDescriptorSet, so both
# ship the same descriptor/briosa.protoset bytes for the same source revision
# and Buf version.

function Resolve-BufCommand {
    param([Parameter(Mandatory)][string]$BufPath)

    $notFound = "Buf was not found as '$BufPath'. Building descriptor/briosa.protoset, which the " +
        'protocol artifact and the Windows package ship, requires Buf. Install Buf or pass -BufPath.'

    # A value with a directory part names one file. Get-Command would read it as
    # a wildcard pattern, where a directory named tool[1] matches tool1 instead.
    if ($BufPath.IndexOfAny([char[]]@([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)) -ge 0) {
        $file = [IO.Path]::GetFullPath($BufPath, (Get-Location).ProviderPath)
        if (-not [IO.File]::Exists($file)) {
            throw $notFound
        }

        return $file
    }

    $command = Get-Command -Name ([WildcardPattern]::Escape($BufPath)) -CommandType Application -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($null -eq $command) {
        throw $notFound
    }

    return $command.Source
}

function Copy-NormalizedTextFile {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination
    )

    [IO.Directory]::CreateDirectory((Split-Path -Parent $Destination)) | Out-Null
    [IO.File]::WriteAllText(
        $Destination,
        [IO.File]::ReadAllText($Source).Replace("`r`n", "`n"),
        [Text.UTF8Encoding]::new($false))
}

function Copy-NormalizedTextTree {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination
    )

    foreach ($file in Get-ChildItem -LiteralPath $Source -File -Recurse |
        Sort-Object FullName) {
        $relativePath = [IO.Path]::GetRelativePath($Source, $file.FullName)
        Copy-NormalizedTextFile `
            -Source $file.FullName `
            -Destination (Join-Path $Destination $relativePath)
    }
}

function New-ProtocolDescriptorSet {
    param(
        # The target directory that holds buf.yaml and the proto tree.
        [Parameter(Mandatory)][string]$TargetRoot,
        # Receives LF-normalized copies of buf.yaml and the proto tree. The
        # protocol artifact passes its bundle root, so it ships these copies.
        [Parameter(Mandatory)][string]$SourceRoot,
        [Parameter(Mandatory)][string]$OutputPath,
        # A resolved Buf executable, from Resolve-BufCommand.
        [Parameter(Mandatory)][string]$BufCommand
    )

    Copy-NormalizedTextFile `
        -Source (Join-Path $TargetRoot 'buf.yaml') `
        -Destination (Join-Path $SourceRoot 'buf.yaml')
    Copy-NormalizedTextTree `
        -Source (Join-Path $TargetRoot 'proto') `
        -Destination (Join-Path $SourceRoot 'proto')

    $resolvedOutput = [IO.Path]::GetFullPath($OutputPath)
    [IO.Directory]::CreateDirectory((Split-Path -Parent $resolvedOutput)) | Out-Null
    # Generate the descriptor from the normalized copies, never from the working
    # tree. Building from the working tree can produce a different descriptor on
    # Windows when Git checks text files out with CRLF line endings but the
    # artifact normalizes them to LF.
    # Buf reads the text after '#' in an output path as options, so a build
    # directory whose path contains '#' cannot be passed as an absolute path.
    # Name the output relative to the build directory instead.
    $relativeOutput = [IO.Path]::GetRelativePath([IO.Path]::GetFullPath($SourceRoot), $resolvedOutput)
    if ($relativeOutput.Contains('#')) {
        throw "The descriptor output path cannot be named relative to its build directory without '#'."
    }
    if ([IO.File]::Exists($resolvedOutput)) {
        [IO.File]::Delete($resolvedOutput)
    }
    Push-Location -LiteralPath $SourceRoot
    try {
        & $BufCommand build `
            --as-file-descriptor-set `
            --output $relativeOutput
        if ($LASTEXITCODE -ne 0) {
            throw "Buf descriptor generation failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
    # A build directory that Buf cannot start in, for example one whose path is
    # too long, can leave Buf running somewhere else and exiting with success.
    # Only a descriptor at the requested path counts.
    if (-not [IO.File]::Exists($resolvedOutput)) {
        throw "Buf reported success but did not write '$resolvedOutput'. Check that the build directory path is usable, for example not too long."
    }
}

Export-ModuleMember -Function Resolve-BufCommand, Copy-NormalizedTextFile, Copy-NormalizedTextTree, New-ProtocolDescriptorSet
