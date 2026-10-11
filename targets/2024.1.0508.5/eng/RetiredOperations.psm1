Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Reviewed operation retirements (docs/development/retired-operations.json).
# The protocol checks call this on an ephemeral baseline descriptor before the
# FILE breaking comparison. It removes exactly the listed RPCs and the request,
# result, and enum types that only they use. It fails closed on any mismatch, so
# every other removal or change still meets the normal breaking rules. See
# docs/development/operation-retirement.md at the repository root.

function Get-ProtocolIndex {
    param([Parameter(Mandatory)][hashtable]$Descriptor)

    $types = @{}
    $services = @{}
    foreach ($file in $Descriptor.file) {
        foreach ($kind in @('messageType', 'enumType')) {
            if ($file.Contains($kind)) {
                foreach ($type in $file[$kind]) {
                    $types["$($file.package).$($type.name)"] = @{ File = $file; Kind = $kind; Name = $type.name }
                }
            }
        }
        if ($file.Contains('service')) {
            foreach ($service in $file.service) {
                $services["$($file.package).$($service.name)"] = $service
            }
        }
    }
    @{ Types = $types; Services = $services }
}

function Get-ProtocolMethod {
    param([Parameter(Mandatory)][hashtable]$Index, [Parameter(Mandatory)][string]$Service, [Parameter(Mandatory)][string]$Rpc)

    $definition = $Index.Services[$Service]
    if ($null -eq $definition -or -not $definition.Contains('method')) { return $null }
    $methods = @($definition.method | Where-Object { $_.name -ceq $Rpc })
    if ($methods.Count -gt 1) { throw "Duplicate RPC $Service/$Rpc." }
    if ($methods.Count -eq 0) { return $null }
    $methods[0]
}

function Get-ReferencedTypeName {
    param([Parameter(Mandatory)]$Messages)

    foreach ($message in $Messages) {
        if ($message.Contains('field')) {
            foreach ($field in $message.field) {
                if ($field.Contains('typeName')) { $field.typeName }
            }
        }
        if ($message.Contains('nestedType')) { Get-ReferencedTypeName $message.nestedType }
    }
}

function Remove-ReviewedOperationRetirement {
    <#
    .SYNOPSIS
    Removes the reviewed retired operations from a baseline descriptor in place.

    .DESCRIPTION
    For each ledger entry the current descriptor must not define the RPC or its
    types. When the baseline defines the RPC, its request and result must be the
    listed messages, the listed types must all exist, and they are removed with
    the RPC. When the baseline does not define the RPC (already retired), it must
    not define the listed types either. Afterwards no retained baseline element
    may still reference a removed type. Returns the removed operation IDs.
    #>
    [CmdletBinding()]
    [OutputType([string[]])]
    param(
        [Parameter(Mandatory)][hashtable]$Baseline,
        [Parameter(Mandatory)][hashtable]$Current,
        [Parameter(Mandatory)][string]$LedgerPath
    )

    $ledger = Get-Content -LiteralPath $LedgerPath -Raw | ConvertFrom-Json -AsHashtable
    if ($ledger.schema_version -ne 1) { throw 'Unsupported operation retirement ledger.' }
    $entries = @($ledger.operations)
    if ($entries.Count -eq 0) { throw 'The operation retirement ledger lists no operations.' }

    $baselineIndex = Get-ProtocolIndex $Baseline
    $currentIndex = Get-ProtocolIndex $Current
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $removedTypes = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $removed = [Collections.Generic.List[string]]::new()

    foreach ($entry in $entries) {
        foreach ($key in @('operation_id', 'service', 'rpc', 'request', 'result')) {
            if ($entry[$key] -isnot [string] -or [string]::IsNullOrWhiteSpace($entry[$key])) {
                throw "Retirement entry is missing '$key'."
            }
        }
        $id = $entry.operation_id
        $enums = @($entry.enums)
        $types = @($entry.request, $entry.result) + $enums
        foreach ($name in @($id, "$($entry.service)/$($entry.rpc)") + $types) {
            if (-not $seen.Add($name)) { throw "Duplicate retirement entry or type: $name" }
        }
        foreach ($name in @($entry.service) + $types) {
            if ($name -cnotmatch '^briosa\.[A-Za-z][A-Za-z0-9]*$') { throw "Invalid retired protocol name: $name" }
        }

        $stillDefined = @($types | Where-Object { $currentIndex.Types.ContainsKey($_) })
        if ($null -ne (Get-ProtocolMethod $currentIndex $entry.service $entry.rpc)) { $stillDefined += "$($entry.service)/$($entry.rpc)" }
        if ($stillDefined.Count -gt 0) {
            throw "Retired operation $id is still in the current protocol ($($stillDefined -join ', ')). Remove its retirement entry before reintroducing it."
        }

        $method = Get-ProtocolMethod $baselineIndex $entry.service $entry.rpc
        $present = @($types | Where-Object { $baselineIndex.Types.ContainsKey($_) })
        if ($null -eq $method) {
            if ($present.Count -gt 0) {
                throw "The baseline defines $($present -join ', ') without the retired RPC $($entry.service)/$($entry.rpc); the retirement of $id does not match it."
            }
            continue
        }

        $streaming = ($method.Contains('clientStreaming') -and $method.clientStreaming) -or
            ($method.Contains('serverStreaming') -and $method.serverStreaming)
        if ($method.inputType -cne ".$($entry.request)" -or $method.outputType -cne ".$($entry.result)" -or $streaming) {
            throw "The baseline RPC $($entry.service)/$($entry.rpc) does not have the reviewed request and result of $id."
        }
        if ($present.Count -ne $types.Count) {
            $missing = @($types | Where-Object { $_ -cnotin $present })
            throw "The baseline lacks $($missing -join ', ') for the retired operation $id."
        }

        $service = $baselineIndex.Services[$entry.service]
        $service['method'] = @($service.method | Where-Object { $_.name -cne $entry.rpc })
        foreach ($name in $types) {
            $type = $baselineIndex.Types[$name]
            $type.File[$type.Kind] = @($type.File[$type.Kind] | Where-Object { $_.name -cne $type.Name })
            $null = $removedTypes.Add(".$name")
        }
        $removed.Add($id)
    }

    # A retained element that still uses a removed type would make the removal
    # hide a change to that element, so it fails instead.
    foreach ($file in $Baseline.file) {
        $references = @()
        if ($file.Contains('messageType')) { $references += @(Get-ReferencedTypeName $file.messageType) }
        if ($file.Contains('extension')) { $references += @($file.extension | Where-Object { $_.Contains('typeName') } | ForEach-Object { $_.typeName }) }
        if ($file.Contains('service')) {
            foreach ($service in $file.service) {
                if ($service.Contains('method')) { $references += @($service.method | ForEach-Object { $_.inputType; $_.outputType }) }
            }
        }
        $retained = @($references | Where-Object { $removedTypes.Contains($_) } | Select-Object -Unique)
        if ($retained.Count -gt 0) {
            throw "A retained baseline element in $($file.name) still uses the retired type(s) $($retained -join ', ')."
        }
    }

    $removed.ToArray()
}

Export-ModuleMember -Function Remove-ReviewedOperationRetirement
