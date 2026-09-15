[CmdletBinding(DefaultParameterSetName = 'Local')]
param(
    [Parameter(Mandatory)][string]$CatalogPath,
    [Parameter(Mandatory, ParameterSetName = 'Local')][string]$PrivateKeyPath,
    [Parameter(Mandatory, ParameterSetName = 'Azure')]
    [ValidatePattern('^https://[a-zA-Z0-9-]+\.vault\.azure\.net/keys/[a-zA-Z0-9-]+/[a-fA-F0-9]{32}$')]
    [string]$AzureKeyId,
    [Parameter(Mandatory, ParameterSetName = 'Azure')][string]$PublicKeyPath,
    [ValidateRange(1, 90)][int]$ValidDays = 7,
    [long]$IssuedAt = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$catalogFile = (Resolve-Path -LiteralPath $CatalogPath).Path
$bytes = [IO.File]::ReadAllBytes($catalogFile)
if ($bytes.Length -gt 1MB) { throw 'Catalog exceeds the metadata size limit.' }
$schema = Join-Path $PSScriptRoot '../schemas/releases/v1/catalog.schema.json'
if (-not (Test-Json -Json ([Text.Encoding]::UTF8.GetString($bytes)) -SchemaFile $schema)) { throw 'Catalog schema validation failed.' }
$rsa = [Security.Cryptography.RSA]::Create()
try {
    $keyPath = if ($PSCmdlet.ParameterSetName -eq 'Azure') { $PublicKeyPath } else { $PrivateKeyPath }
    $rsa.ImportFromPem([IO.File]::ReadAllText((Resolve-Path -LiteralPath $keyPath).Path))
    if ($rsa.KeySize -lt 3072 -or $rsa.KeySize -gt 8192) { throw 'Publisher RSA keys must be 3072 to 8192 bits.' }
    $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
    $expiresAt = $IssuedAt + ([long]$ValidDays * 86400)
    $message = [Text.Encoding]::UTF8.GetBytes("Briosa release catalog signature v1`n$hash`n$IssuedAt`n$expiresAt`n")
    if ($PSCmdlet.ParameterSetName -eq 'Azure') {
        # Key Vault signs a digest, whereas RSA.SignData hashes the message internally.
        $digest = [Convert]::ToBase64String([Security.Cryptography.SHA256]::HashData($message))
        $resultJson = & az keyvault key sign --id $AzureKeyId --algorithm PS256 --digest $digest --only-show-errors --output json
        if ($LASTEXITCODE -ne 0) { throw 'Azure Key Vault catalog signing failed.' }
        $result = ($resultJson -join "`n") | ConvertFrom-Json
        if ($result.keyId -cne $AzureKeyId -or $result.algorithm -cne 'PS256') { throw 'Key Vault returned an unexpected key version or algorithm.' }
        $encoded = $result.signature.Replace('-', '+').Replace('_', '/')
        $signature = [Convert]::FromBase64String($encoded.PadRight($encoded.Length + ((4 - $encoded.Length % 4) % 4), '='))
    } else {
        $signature = $rsa.SignData($message, [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pss)
    }
    if (-not $rsa.VerifyData($message, $signature, [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pss)) {
        throw 'Signature does not match the pinned publisher public key; previous signature is preserved.'
    }
    $envelope = [ordered]@{ schemaVersion = 1; algorithm = 'RSA-PSS-SHA256'; catalogSha256 = $hash; issuedAt = $IssuedAt; expiresAt = $expiresAt; signature = [Convert]::ToBase64String($signature) }
    $output = "$catalogFile.signature.json"
    $temporary = "$output.$([Guid]::NewGuid().ToString('N')).tmp"
    try {
        [IO.File]::WriteAllText($temporary, ($envelope | ConvertTo-Json) + "`n", [Text.UTF8Encoding]::new($false))
        [IO.File]::Move($temporary, $output, $true)
    } finally { if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary } }
    $fingerprint = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($rsa.ExportSubjectPublicKeyInfo())).ToLowerInvariant()
    Write-Output "Signed catalog. Publisher SHA-256 fingerprint: $fingerprint"
} finally { $rsa.Dispose() }
