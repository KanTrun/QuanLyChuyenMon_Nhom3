param(
    [string]$EnvPath = ".env",
    [string]$TemplatePath = ".env.example",
    [switch]$NonInteractive,
    [string]$SmartCaBaseUrl = "https://rmgateway.vnptit.vn",
    [string]$SmartCaApiPrefix = "/sca/sp769",
    [string]$SmartCaSpId,
    [string]$SmartCaSpPassword,
    [string]$SmartCaDefaultUserId,
    [string]$SmartCaDefaultSerialNumber,
    [string]$SmartCaSignerUserId,
    [string]$SmartCaSignerUsername,
    [string]$SmartCaUserBindingsJson,
    [string]$SmartCaCallbackUrl,
    [string]$SmartCaCallbackSecret,
    [int]$SmartCaRequestTimeoutSeconds = 45
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Read-RequiredValue([string]$Label, [string]$CurrentValue) {
    if (-not [string]::IsNullOrWhiteSpace($CurrentValue)) {
        return $CurrentValue.Trim()
    }

    $value = Read-Host $Label
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "$Label is required."
    }

    return $value.Trim()
}

function Read-OptionalValue([string]$Label, [string]$CurrentValue) {
    if (-not [string]::IsNullOrWhiteSpace($CurrentValue)) {
        return $CurrentValue.Trim()
    }

    $value = Read-Host "$Label (blank to skip)"
    return $value.Trim()
}

function Read-SecretValue([string]$Label, [string]$CurrentValue) {
    if (-not [string]::IsNullOrWhiteSpace($CurrentValue)) {
        return $CurrentValue
    }

    $secure = Read-Host $Label -AsSecureString
    if ($secure.Length -eq 0) {
        throw "$Label is required."
    }

    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
    }
}

function New-CallbackSecret {
    $bytes = [byte[]]::new(32)
    [Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    return [Convert]::ToBase64String($bytes).TrimEnd("=").Replace("+", "-").Replace("/", "_")
}

function Format-EnvValue([string]$Value) {
    if ($null -eq $Value) {
        return ""
    }

    if ($Value -match '[\s#"'']') {
        $escaped = $Value.Replace("\", "\\").Replace('"', '\"')
        return '"' + $escaped + '"'
    }

    return $Value
}

function Set-EnvValue([System.Collections.Generic.List[string]]$Lines, [string]$Key, [string]$Value) {
    $formatted = Format-EnvValue $Value
    $pattern = "^\s*#?\s*$([Regex]::Escape($Key))\s*="

    for ($i = 0; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -match $pattern) {
            $Lines[$i] = "$Key=$formatted"
            return
        }
    }

    if (-not $script:SmartCaHeaderAdded) {
        if ($Lines.Count -gt 0 -and -not [string]::IsNullOrWhiteSpace($Lines[$Lines.Count - 1])) {
            $Lines.Add("")
        }
        $Lines.Add("# VNPT SmartCA sandbox")
        $script:SmartCaHeaderAdded = $true
    }

    $Lines.Add("$Key=$formatted")
}

function Resolve-OutputPath([string]$Path) {
    if ([IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path (Get-Location) $Path
}

if ($NonInteractive) {
    $missing = @()
    if ([string]::IsNullOrWhiteSpace($SmartCaSpId)) { $missing += "SmartCaSpId" }
    if ([string]::IsNullOrWhiteSpace($SmartCaSpPassword)) { $missing += "SmartCaSpPassword" }
    if ([string]::IsNullOrWhiteSpace($SmartCaDefaultUserId)) { $missing += "SmartCaDefaultUserId" }
    $hasBinding = -not [string]::IsNullOrWhiteSpace($SmartCaSignerUserId) `
        -or -not [string]::IsNullOrWhiteSpace($SmartCaSignerUsername) `
        -or -not [string]::IsNullOrWhiteSpace($SmartCaUserBindingsJson)
    if (-not $hasBinding) { $missing += "SmartCaSignerUsername or SmartCaSignerUserId or SmartCaUserBindingsJson" }
    if ($missing.Count -gt 0) {
        throw "Missing required values: $($missing -join ', ')"
    }
}
else {
    Write-Host "VNPT SmartCA sandbox local .env configurator"
    Write-Host "Do not paste secrets into git. This script writes only to local .env."
    $SmartCaSpId = Read-RequiredValue "VNPT Client_Id / SP_ID" $SmartCaSpId
    $SmartCaSpPassword = Read-SecretValue "VNPT Client_Secret / SP password" $SmartCaSpPassword
    $SmartCaDefaultUserId = Read-RequiredValue "SmartCA subscriber id (CCCD/MST/tenant value from VNPT)" $SmartCaDefaultUserId
    $SmartCaDefaultSerialNumber = Read-OptionalValue "Certificate serial number" $SmartCaDefaultSerialNumber

    if ([string]::IsNullOrWhiteSpace($SmartCaSignerUserId) `
        -and [string]::IsNullOrWhiteSpace($SmartCaSignerUsername) `
        -and [string]::IsNullOrWhiteSpace($SmartCaUserBindingsJson)) {
        $SmartCaSignerUsername = Read-OptionalValue "QLCM username allowed to sign, default admin" "admin"
        if ([string]::IsNullOrWhiteSpace($SmartCaSignerUsername)) {
            $SmartCaSignerUsername = "admin"
        }
    }

    $SmartCaCallbackUrl = Read-OptionalValue "Public callback URL, e.g. https://domain/api/signatures/smartca/callback" $SmartCaCallbackUrl
}

if (-not [string]::IsNullOrWhiteSpace($SmartCaCallbackUrl) -and [string]::IsNullOrWhiteSpace($SmartCaCallbackSecret)) {
    $SmartCaCallbackSecret = New-CallbackSecret
}

$sourcePath = if (Test-Path -LiteralPath $EnvPath) { $EnvPath } elseif (Test-Path -LiteralPath $TemplatePath) { $TemplatePath } else { $null }
$lines = [System.Collections.Generic.List[string]]::new()
if ($sourcePath) {
    foreach ($line in [IO.File]::ReadAllLines((Resolve-Path -LiteralPath $sourcePath))) {
        $lines.Add($line)
    }
}

$script:SmartCaHeaderAdded = $false
Set-EnvValue $lines "SMARTCA_ENABLED" "true"
Set-EnvValue $lines "SMARTCA_BASE_URL" $SmartCaBaseUrl
Set-EnvValue $lines "SMARTCA_API_PREFIX" $SmartCaApiPrefix
Set-EnvValue $lines "SMARTCA_SP_ID" $SmartCaSpId
Set-EnvValue $lines "SMARTCA_SP_PASSWORD" $SmartCaSpPassword
Set-EnvValue $lines "SMARTCA_DEFAULT_USER_ID" $SmartCaDefaultUserId
Set-EnvValue $lines "SMARTCA_DEFAULT_SERIAL_NUMBER" $SmartCaDefaultSerialNumber
Set-EnvValue $lines "SMARTCA_SIGNER_USER_ID" $SmartCaSignerUserId
Set-EnvValue $lines "SMARTCA_SIGNER_USERNAME" $SmartCaSignerUsername
Set-EnvValue $lines "SMARTCA_USER_BINDINGS_JSON" $SmartCaUserBindingsJson
Set-EnvValue $lines "SMARTCA_CALLBACK_URL" $SmartCaCallbackUrl
Set-EnvValue $lines "SMARTCA_CALLBACK_SECRET" $SmartCaCallbackSecret
Set-EnvValue $lines "SMARTCA_REQUEST_TIMEOUT_SECONDS" ([string]$SmartCaRequestTimeoutSeconds)

[IO.File]::WriteAllLines((Resolve-OutputPath $EnvPath), $lines, [Text.UTF8Encoding]::new($false))

Write-Host ""
Write-Host "Updated $EnvPath. This file is gitignored and must stay local."
Write-Host "Next:"
Write-Host "  docker compose up --build -d web"
Write-Host "  .\scripts\smoke-smartca-api.ps1"
if (-not [string]::IsNullOrWhiteSpace($SmartCaCallbackUrl)) {
    Write-Host "Register this callback URL with VNPT: $SmartCaCallbackUrl"
    Write-Host "Callback header: X-QLCM-SMARTCA-CALLBACK-SECRET"
}
