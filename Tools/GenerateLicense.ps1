<#
.SYNOPSIS
    Generates a signed license key for the Aglipay Attendance system.
    Only robram3000@gmail.com holds the private key required to run this script.
    Requires PowerShell 7+ (pwsh) and .NET 5+.

.PARAMETER Licensee
    Email or name of the organization/person being licensed.

.PARAMETER ExpireDays
    Number of days the license is valid from today. Default: 365.

.PARAMETER PrivateKeyPath
    Path to the RSA private key PEM file. Default: .\PrivateKey.pem
    NEVER commit this file.

.EXAMPLE
    .\GenerateLicense.ps1 -Licensee "client@company.com" -ExpireDays 365
    .\GenerateLicense.ps1 -Licensee "hospital-abc" -ExpireDays 30 -PrivateKeyPath "C:\keys\PrivateKey.pem"
#>
param(
    [Parameter(Mandatory)]
    [string]$Licensee,

    [int]$ExpireDays = 365,

    [string]$PrivateKeyPath = "$PSScriptRoot\PrivateKey.pem"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path $PrivateKeyPath)) {
    Write-Error "Private key not found at: $PrivateKeyPath`nKeep PrivateKey.pem alongside this script (it is gitignored)."
    exit 1
}

$privateKeyPem = Get-Content $PrivateKeyPath -Raw

# Build JSON payload
$now       = [DateTime]::UtcNow
$expiresAt = $now.AddDays($ExpireDays)

$payloadObj = [ordered]@{
    Licensee  = $Licensee
    IssuedBy  = "robram3000@gmail.com"
    IssuedAt  = $now.ToString("yyyy-MM-ddTHH:mm:ssZ")
    ExpiresAt = $expiresAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
}
$payloadJson  = $payloadObj | ConvertTo-Json -Compress
$payloadBytes = [System.Text.Encoding]::UTF8.GetBytes($payloadJson)
$payloadB64   = [Convert]::ToBase64String($payloadBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')

# Import private key and sign payload
$rsa = [System.Security.Cryptography.RSA]::Create()
$rsa.ImportFromPem($privateKeyPem)

$sigBytes = $rsa.SignData(
    $payloadBytes,
    [System.Security.Cryptography.HashAlgorithmName]::SHA256,
    [System.Security.Cryptography.RSASignaturePadding]::Pkcs1
)
$sigB64 = [Convert]::ToBase64String($sigBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')

$licenseKey = "$payloadB64.$sigB64"

Write-Host ""
Write-Host "=== LICENSE KEY GENERATED ===" -ForegroundColor Cyan
Write-Host "  Licensee : $Licensee"
Write-Host "  Issued   : $($now.ToString('yyyy-MM-dd'))"
Write-Host "  Expires  : $($expiresAt.ToString('yyyy-MM-dd')) ($ExpireDays days)"
Write-Host ""
Write-Host $licenseKey -ForegroundColor Green
Write-Host ""
Write-Host "Send this key to the licensee. They paste it at the license activation prompt." -ForegroundColor DarkGray
Write-Host "Activation endpoint: POST /api/license/activate  { licenseKey: '<key>' }" -ForegroundColor DarkGray
