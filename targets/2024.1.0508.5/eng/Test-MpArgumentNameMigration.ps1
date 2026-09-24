[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Against,
    [string]$BufPath = "buf"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$targetRoot = Split-Path -Parent $PSScriptRoot
$ledger = Get-Content -LiteralPath (Join-Path $targetRoot "docs/development/mp-argument-name-migration.json") -Raw | ConvertFrom-Json
$temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) "briosa-name-migration-$([Guid]::NewGuid().ToString('N'))"
[IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null

function Invoke-Buf {
    param([string[]]$Arguments)
    & $BufPath @Arguments
    if ($LASTEXITCODE -ne 0) { throw "buf $($Arguments -join ' ') failed. See the diagnostics above." }
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
    Invoke-Buf -Arguments @("build", "--exclude-source-info", "--as-file-descriptor-set", "-o", $currentPath)
    $baselineHash = (Get-FileHash -LiteralPath $baselinePath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($baselineHash -ne $ledger.baseline_descriptor_sha256) {
        # An unrelated or already-migrated baseline receives the ordinary strict check.
        Invoke-Buf -Arguments @("breaking", "--against", $Against)
        return
    }

    Write-Host "Raw source/JSON compatibility diagnostics for the reviewed MP naming migration:"
    & $BufPath breaking --against $Against
    if ($LASTEXITCODE -ne 100) { throw "Expected the reviewed name changes against the pinned descriptor baseline." }

    $wireConfig = Join-Path $temporaryRoot "wire.yaml"
    [IO.File]::WriteAllText($wireConfig, "version: v2`nbreaking:`n  use:`n    - WIRE`n")
    Invoke-Buf -Arguments @("breaking", $currentPath, "--against", $baselinePath, "--config", $wireConfig)

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
        $identity = "briosa.$($entry.message)#$($entry.field_number)"
        if (-not $seen.Add($identity)) { throw "Duplicate migration field: $identity" }
        $oldMessage = $oldMessages["briosa.$($entry.message)"]
        $newMessage = $newMessages["briosa.$($entry.message)"]
        $oldFields = @($oldMessage.field | Where-Object number -EQ $entry.field_number)
        $newFields = @($newMessage.field | Where-Object number -EQ $entry.field_number)
        if ($oldFields.Count -ne 1 -or $newFields.Count -ne 1) { throw "Missing migration field: $identity" }
        $oldField = $oldFields[0]
        $newField = $newFields[0]
        if ($oldField.name -cne $entry.old_name -or $newField.name -cne $entry.new_name) {
            throw "Unapproved migration name at $identity."
        }
        $expectedJsonName = [regex]::Replace($entry.new_name, '_([a-z0-9])', { param($match) $match.Groups[1].Value.ToUpperInvariant() })
        if ($newField.jsonName -cne $expectedJsonName) { throw "Unapproved JSON name at $identity." }
        $newField.name = $oldField.name
        $newField.jsonName = $oldField.jsonName
        if ($oldField.Contains('proto3Optional') -and $oldField.proto3Optional) {
            if (-not $newField.Contains('proto3Optional') -or -not $newField.proto3Optional) { throw "Presence changed at $identity." }
            $oldOneof = $oldMessage.oneofDecl[$oldField.oneofIndex]
            $newOneof = $newMessage.oneofDecl[$newField.oneofIndex]
            $members = @($newMessage.field | Where-Object { $_.Contains('oneofIndex') -and $_.oneofIndex -eq $newField.oneofIndex })
            if ($members.Count -ne 1 -or $newOneof.name -cne "_$($entry.new_name)" -or $oldOneof.name -cne "_$($entry.old_name)") {
                throw "Unexpected optional oneof at $identity."
            }
            $newOneof.name = $oldOneof.name
        }
    }
    $normalizedPath = Join-Path $temporaryRoot "reviewed-names.json"
    [IO.File]::WriteAllText($normalizedPath, ($current | ConvertTo-Json -Depth 100 -Compress))
    # Restore only the explicitly reviewed names in an ephemeral descriptor, then
    # apply the repository's full FILE policy. Types, presence, RPCs, and every
    # unlisted field remain subject to the normal breaking rules.
    Invoke-Buf -Arguments @("breaking", $normalizedPath, "--against", $baselinePath, "--config", (Join-Path $targetRoot "buf.yaml"))
    Write-Host "Verified $($seen.Count) reviewed field renames; WIRE and remaining FILE checks passed."
}
finally {
    Pop-Location
    $resolvedTemporary = [IO.Path]::GetFullPath($temporaryRoot)
    $resolvedParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedTemporary.StartsWith($resolvedParent, [StringComparison]::OrdinalIgnoreCase)) { throw "Invalid temporary path." }
    Remove-Item -LiteralPath $resolvedTemporary -Recurse -Force
}
