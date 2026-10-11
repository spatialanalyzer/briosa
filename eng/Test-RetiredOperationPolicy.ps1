[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$TargetDirectory,
    [string]$BufPath = 'buf'
)

# Proves the reviewed operation-retirement exception (#308, #310) is narrow. The
# protocol check removes the RPCs listed in docs/development/retired-operations.json,
# and the request, result and enum types only they use, from an ephemeral baseline.
# This test synthesizes a baseline that still defines the listed operations, then
# shows that the listed removals pass and that unlisted removals, other changes,
# and every ledger mismatch still fail.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$target = (Resolve-Path -LiteralPath $TargetDirectory).Path
$ledger = Get-Content (Join-Path $target 'docs/development/retired-operations.json') -Raw | ConvertFrom-Json -AsHashtable
$temporaryBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixture = Join-Path $temporaryBase "briosa-retirement-policy-$([Guid]::NewGuid().ToString('N'))"
$null = [IO.Directory]::CreateDirectory($fixture)

function Read-Descriptor([string]$Path) { Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -AsHashtable }

function Write-Descriptor($Descriptor, [string]$Name) {
    $path = Join-Path $fixture $Name
    [IO.File]::WriteAllText($path, ($Descriptor | ConvertTo-Json -Depth 100 -Compress))
    $path
}

function Get-ServiceFile($Descriptor, [string]$Service) {
    $files = @($Descriptor.file | Where-Object {
            $_.Contains('service') -and @($_.service | Where-Object { "briosa.$($_.name)" -ceq $Service }).Count -eq 1
        })
    if ($files.Count -ne 1) { throw "Cannot find the file that defines $Service." }
    $files[0]
}

# Adds each listed operation back to a copy of the current descriptor with its
# reviewed RPC shape. The request carries the listed enums, as the real ones did.
function New-RetiredBaseline([scriptblock]$Mutate) {
    $descriptor = Read-Descriptor $currentPath
    foreach ($entry in $ledger.operations) {
        $file = Get-ServiceFile $descriptor $entry.service
        $service = @($file.service | Where-Object { "briosa.$($_.name)" -ceq $entry.service })[0]
        $service['method'] = @($service.method) + @([ordered]@{
                name = $entry.rpc; inputType = ".$($entry.request)"; outputType = ".$($entry.result)"
            })
        $requestFields = @([ordered]@{ name = 'value'; number = 1; label = 'LABEL_OPTIONAL'; type = 'TYPE_STRING'; jsonName = 'value' })
        $number = 2
        foreach ($enum in @($entry.enums)) {
            $short = $enum.Substring('briosa.'.Length)
            $file['enumType'] = @($file.enumType) + @([ordered]@{
                    name = $short; value = @([ordered]@{ name = "$($short.ToUpperInvariant())_UNSPECIFIED"; number = 0 })
                })
            $requestFields += @([ordered]@{
                    name = "choice_$number"; number = $number; label = 'LABEL_OPTIONAL'; type = 'TYPE_ENUM'
                    typeName = ".$enum"; jsonName = "choice$number"
                })
            $number++
        }
        $file['messageType'] = @($file.messageType) + @(
            [ordered]@{ name = $entry.request.Substring('briosa.'.Length); field = $requestFields },
            [ordered]@{ name = $entry.result.Substring('briosa.'.Length); field = @() })
    }
    if ($Mutate) { & $Mutate $descriptor }
    Write-Descriptor $descriptor "baseline-$([Guid]::NewGuid().ToString('N')).json"
}

function Invoke-Checker([string]$Baseline, [string]$Log) {
    & $checker -Against $Baseline -BufPath $BufPath *> (Join-Path $fixture $Log)
}

function Assert-Rejected([string]$Name, [string]$Baseline, [string]$Message) {
    $rejected = $false
    try { Invoke-Checker $Baseline 'rejected.log' }
    catch {
        $rejected = $true
        if ($Message -and -not $_.Exception.Message.Contains($Message, [StringComparison]::Ordinal)) {
            throw "Retirement policy rejected '$Name' for an unexpected reason: $($_.Exception.Message)"
        }
    }
    if (-not $rejected) { throw "Retirement policy accepted an unreviewed change: $Name." }
}

function Use-ProtoMutation([string]$Name, [string]$Pattern, [string]$Replacement, [scriptblock]$Body) {
    $path = Join-Path $fixture 'proto/briosa/construction_operations.proto'
    $original = [IO.File]::ReadAllText($path)
    $mutated = [regex]::Replace($original, $Pattern, $Replacement)
    if ($mutated -ceq $original) { throw "Missing proto mutation fixture: $Name." }
    [IO.File]::WriteAllText($path, $mutated)
    try { & $Body }
    finally { [IO.File]::WriteAllText($path, $original) }
}

try {
    Copy-Item -LiteralPath (Join-Path $target 'proto') -Destination $fixture -Recurse
    Copy-Item -LiteralPath (Join-Path $target 'buf.yaml') -Destination $fixture
    $null = [IO.Directory]::CreateDirectory((Join-Path $fixture 'eng'))
    $null = [IO.Directory]::CreateDirectory((Join-Path $fixture 'docs/development'))
    foreach ($file in @('eng/Test-MpArgumentNameMigration.ps1', 'eng/RetiredOperations.psm1',
            'docs/development/mp-argument-name-migration.json', 'docs/development/retired-operations.json')) {
        Copy-Item -LiteralPath (Join-Path $target $file) -Destination (Join-Path $fixture $file)
    }
    $checker = Join-Path $fixture 'eng/Test-MpArgumentNameMigration.ps1'
    $currentPath = Join-Path $fixture 'current.json'
    Push-Location $fixture
    try { & $BufPath build --exclude-source-info --as-file-descriptor-set -o $currentPath }
    finally { Pop-Location }
    if ($LASTEXITCODE -ne 0) { throw 'Cannot build the current descriptor.' }
    $operations = @($ledger.operations)
    if ($operations.Count -eq 0) { throw 'The retirement ledger lists no operations.' }
    $first = $operations[0]

    # The listed retirements pass, and only they are reported.
    $retiredBaseline = New-RetiredBaseline
    Invoke-Checker $retiredBaseline 'allowed.log'
    $allowed = Get-Content -LiteralPath (Join-Path $fixture 'allowed.log') -Raw
    if (-not $allowed.Contains("Verified $($operations.Count) reviewed operation retirement(s)", [StringComparison]::Ordinal)) {
        throw 'The listed retirements were not verified.'
    }

    # Unlisted removals and other changes keep the FILE rules.
    Use-ProtoMutation 'unlisted RPC removal' '(?m)^  rpc ConstructCircle\(ConstructCircleRequest\) returns \(ConstructCircleResult\);\r?\n' '' {
        Assert-Rejected 'unlisted RPC removal' $retiredBaseline 'buf breaking'
    }
    Use-ProtoMutation 'unlisted operation removal' '(?ms)^  rpc ConstructCircle\(ConstructCircleRequest\) returns \(ConstructCircleResult\);\r?\n(?<between>.*?)^message ConstructCircleRequest \{.*?^\}\r?\nmessage ConstructCircleResult \{.*?^\}\r?\n' '${between}' {
        Assert-Rejected 'unlisted operation removal' $retiredBaseline 'buf breaking'
    }
    Use-ProtoMutation 'retained field type change' '(?m)^  optional double circle_radius = 4;' '  optional string circle_radius = 4;' {
        Assert-Rejected 'retained field type change' $retiredBaseline 'buf breaking'
    }

    # Ledger mismatches fail closed with their own diagnostics.
    Use-ProtoMutation 'reintroduced retired RPC' '(?m)^(service ConstructionOperations \{\r?\n)' "`${1}  rpc $($first.rpc)(ConstructCircleRequest) returns (ConstructCircleResult);`n" {
        Assert-Rejected 'reintroduced retired RPC' $retiredBaseline 'is still in the current protocol'
    }
    Assert-Rejected 'retired RPC with another request' (New-RetiredBaseline {
            param($descriptor)
            $file = Get-ServiceFile $descriptor $first.service
            $method = @(@($file.service | Where-Object { "briosa.$($_.name)" -ceq $first.service })[0].method |
                    Where-Object { $_.name -ceq $first.rpc })[0]
            $method.inputType = '.briosa.ConstructCircleRequest'
        }) 'does not have the reviewed request and result'
    Assert-Rejected 'retained message using a retired type' (New-RetiredBaseline {
            param($descriptor)
            $file = Get-ServiceFile $descriptor $first.service
            $message = @($file.messageType | Where-Object { $_.name -ceq 'ConstructCircleRequest' })[0]
            $message['field'] = @($message.field) + @([ordered]@{
                    name = 'retired_result'; number = 99; label = 'LABEL_OPTIONAL'; type = 'TYPE_MESSAGE'
                    typeName = ".$($first.result)"; jsonName = 'retiredResult'
                })
        }) 'still uses the retired type'
    Assert-Rejected 'retired types without the retired RPC' (New-RetiredBaseline {
            param($descriptor)
            $file = Get-ServiceFile $descriptor $first.service
            $service = @($file.service | Where-Object { "briosa.$($_.name)" -ceq $first.service })[0]
            $service['method'] = @($service.method | Where-Object { $_.name -cne $first.rpc })
        }) 'without the retired RPC'

    # An already-retired baseline receives the ordinary strict check.
    Invoke-Checker $currentPath 'ordinary.log'
    Use-ProtoMutation 'unlisted RPC removal after retirement' '(?m)^  rpc ConstructCircle\(ConstructCircleRequest\) returns \(ConstructCircleResult\);\r?\n' '' {
        Assert-Rejected 'unlisted RPC removal after retirement' $currentPath 'buf breaking'
    }

    Write-Host "Verified operation retirement policy for $($ledger.spatial_analyzer_target): $($operations.Count) listed retirements pass; three unlisted changes and four ledger mismatches fail."
    $global:LASTEXITCODE = 0
}
finally {
    $resolved = [IO.Path]::GetFullPath($fixture)
    $prefix = $temporaryBase.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid fixture cleanup path.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
