Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# The one way this target builds its protocol descriptor set. The protocol
# artifact and the Windows package both call New-ProtocolDescriptorSet, so both
# ship the same descriptor/briosa.protoset bytes for the same source revision
# and Buf version.

function Resolve-BufCommand {
    param([Parameter(Mandatory)][string]$BufPath)

    $command = Get-Command -Name $BufPath -CommandType Application -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($null -eq $command) {
        throw ("Buf was not found as '$BufPath'. Building descriptor/briosa.protoset, which the " +
            'protocol artifact and the Windows package ship, requires Buf. Install Buf or pass -BufPath.')
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
    Push-Location $SourceRoot
    try {
        & $BufCommand build `
            --as-file-descriptor-set `
            --output $resolvedOutput
        if ($LASTEXITCODE -ne 0) {
            throw "Buf descriptor generation failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}

Export-ModuleMember -Function Resolve-BufCommand, Copy-NormalizedTextFile, Copy-NormalizedTextTree, New-ProtocolDescriptorSet
