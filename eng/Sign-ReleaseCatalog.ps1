[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$CatalogPath,
    [Parameter(Mandatory)][string]$PrivateKeyPath,
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
    $rsa.ImportFromPem([IO.File]::ReadAllText((Resolve-Path -LiteralPath $PrivateKeyPath).Path))
    if ($rsa.KeySize -lt 3072 -or $rsa.KeySize -gt 8192) { throw 'Publisher RSA keys must be 3072 to 8192 bits.' }
    $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
    $expiresAt = $IssuedAt + ([long]$ValidDays * 86400)
    $message = [Text.Encoding]::UTF8.GetBytes("Briosa release catalog signature v1`n$hash`n$IssuedAt`n$expiresAt`n")
    $signature = $rsa.SignData($message, [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pss)
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
