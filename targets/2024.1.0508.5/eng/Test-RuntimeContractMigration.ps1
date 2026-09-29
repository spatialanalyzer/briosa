[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Against,
    [string]$BufPath = "buf",
    [string]$CurrentDescriptor
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$targetRoot = Split-Path -Parent $PSScriptRoot
$ledger = Get-Content -LiteralPath (Join-Path $targetRoot "docs/development/runtime-contract-migration.json") -Raw | ConvertFrom-Json
$temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) "briosa-runtime-migration-$([Guid]::NewGuid().ToString('N'))"
[IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null

function Invoke-Buf {
    param([string[]]$Arguments)
    & $BufPath @Arguments
    if ($LASTEXITCODE -ne 0) { throw "buf $($Arguments -join ' ') failed." }
}

function Get-Messages {
    param($Messages, [string]$Prefix, [hashtable]$Index)
    foreach ($message in $Messages) {
        $name = "$Prefix$($message.name)"
        $Index[$name] = $message
        if ($message.Contains('nestedType')) { Get-Messages $message.nestedType "$name." $Index }
    }
}

Push-Location $targetRoot
try {
    $baselinePath = Join-Path $temporaryRoot "baseline.json"
    $currentPath = Join-Path $temporaryRoot "current.json"
    Invoke-Buf -Arguments @("build", $Against, "--exclude-source-info", "--as-file-descriptor-set", "-o", $baselinePath)
    $baselineHash = (Get-FileHash -LiteralPath $baselinePath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($baselineHash -ne $ledger.baseline_descriptor_sha256) {
        & (Join-Path $PSScriptRoot "Test-MpArgumentNameMigration.ps1") -Against $Against -BufPath $BufPath
        return
    }

    $contract = Get-Content -LiteralPath (Join-Path $targetRoot "compatibility.json") -Raw | ConvertFrom-Json
    if ($contract.major -ne 2) { throw "The reviewed runtime migration requires compatibility major 2." }
    $buildArguments = @("build")
    if ($CurrentDescriptor) { $buildArguments += $CurrentDescriptor }
    Invoke-Buf -Arguments ($buildArguments + @("--exclude-source-info", "--as-file-descriptor-set", "-o", $currentPath))
    $baseline = Get-Content -LiteralPath $baselinePath -Raw | ConvertFrom-Json -AsHashtable
    $current = Get-Content -LiteralPath $currentPath -Raw | ConvertFrom-Json -AsHashtable
    $oldMessages = @{}
    $newMessages = @{}
    foreach ($file in $baseline.file) {
        if ($file.Contains('messageType')) { Get-Messages $file.messageType "$($file.package)." $oldMessages }
    }
    foreach ($file in $current.file) {
        if ($file.Contains('messageType')) { Get-Messages $file.messageType "$($file.package)." $newMessages }
    }
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($entry in $ledger.fields) {
        $identity = "briosa.$($entry.message)"
        if (-not $seen.Add($identity)) { throw "Duplicate migration entry: $identity" }
        $oldMessage = $oldMessages[$identity]
        $newMessage = $newMessages[$identity]
        $oldFields = @($oldMessage.field | Where-Object number -EQ 1)
        $newFields = @($newMessage.field | Where-Object number -EQ $entry.new_field_number)
        if ($oldFields.Count -ne 1 -or $newFields.Count -ne 1) { throw "Missing migration field: $identity" }
        $oldField = $oldFields[0]
        $newField = $newFields[0]
        if ($oldField.name -cne 'machine_id' -or $newField.name -cne 'machine_id' -or
            $oldField.typeName -cne '.briosa.CollectionMachineId' -or
            $newField.typeName -cne '.briosa.CollectionInstrumentId') {
            throw "Unreviewed runtime field change: $identity"
        }
        if (@($newMessage.field | Where-Object number -EQ 1).Count -ne 0 -or
            -not $newMessage.Contains('reservedRange') -or
            @($newMessage.reservedRange | Where-Object { $_.start -eq 1 -and $_.end -eq 2 }).Count -ne 1) {
            throw "The retired field must remain reserved: $identity"
        }
        # Normalize only the reviewed type and number in an ephemeral descriptor.
        # Names, presence, defaults, RPCs, and all other fields retain FILE checks.
        $newField.number = 1
        $newField.typeName = $oldField.typeName
        $newMessage.reservedRange = @($newMessage.reservedRange | Where-Object { $_.start -ne 1 -or $_.end -ne 2 })
    }
    if ($seen.Count -ne 3) { throw "Expected exactly three reviewed robot ID corrections." }
    $normalizedPath = Join-Path $temporaryRoot "reviewed-runtime.json"
    [IO.File]::WriteAllText($normalizedPath, ($current | ConvertTo-Json -Depth 100 -Compress))
    Invoke-Buf -Arguments @("breaking", $normalizedPath, "--against", $baselinePath, "--config", (Join-Path $targetRoot "buf.yaml"))
    Write-Host "Verified three reviewed robot ID corrections; all remaining FILE checks passed."
}
finally {
    Pop-Location
    $resolvedTemporary = [IO.Path]::GetFullPath($temporaryRoot)
    $resolvedParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedTemporary.StartsWith($resolvedParent, [StringComparison]::OrdinalIgnoreCase)) { throw "Invalid temporary path." }
    Remove-Item -LiteralPath $resolvedTemporary -Recurse -Force
}
