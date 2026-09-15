[CmdletBinding()]
param([Parameter(Mandatory)][string]$CatalogPath, [Parameter(Mandatory)][string]$PublicKeyPath)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$bytes = [IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $CatalogPath).Path)
$signatureText = [IO.File]::ReadAllText((Resolve-Path -LiteralPath "$CatalogPath.signature.json").Path)
if ($bytes.Length -gt 1MB -or [Text.Encoding]::UTF8.GetByteCount($signatureText) -gt 16KB) { throw 'Catalog metadata exceeds size limits.' }
if (-not (Test-Json -Json $signatureText -SchemaFile (Join-Path $PSScriptRoot '../schemas/releases/v1/catalog-signature.schema.json'))) { throw 'Invalid signature envelope.' }
$envelope = $signatureText | ConvertFrom-Json
$hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
$now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
if ($envelope.catalogSha256 -cne $hash -or $envelope.issuedAt -gt $now + 300 -or $envelope.expiresAt -le $now -or
    $envelope.expiresAt -le $envelope.issuedAt -or $envelope.expiresAt - $envelope.issuedAt -gt 90 * 86400) { throw 'Catalog digest or validity period is invalid.' }
$message = [Text.Encoding]::UTF8.GetBytes("Briosa release catalog signature v1`n$hash`n$($envelope.issuedAt)`n$($envelope.expiresAt)`n")
$rsa = [Security.Cryptography.RSA]::Create()
try {
    $rsa.ImportFromPem([IO.File]::ReadAllText((Resolve-Path -LiteralPath $PublicKeyPath).Path))
    if ($rsa.KeySize -lt 3072 -or $rsa.KeySize -gt 8192 -or
        -not $rsa.VerifyData($message, [Convert]::FromBase64String($envelope.signature), [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pss)) { throw 'Catalog signature verification failed.' }
    Write-Host 'Catalog signature, digest, pinned key, and validity period verified.'
} finally { $rsa.Dispose() }
