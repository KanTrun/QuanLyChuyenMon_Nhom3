param(
    [string]$EnvPath = ".env",
    [string]$SubscriberId,
    [string]$SerialNumber,
    [switch]$ShowCertificateSubject
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Read-EnvFile([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Env file not found: $Path"
    }

    $values = @{}
    foreach ($rawLine in Get-Content -LiteralPath $Path) {
        $line = $rawLine.Trim()
        if (-not $line -or $line.StartsWith("#") -or -not $line.Contains("=")) {
            continue
        }

        $idx = $line.IndexOf("=")
        $key = $line.Substring(0, $idx).Trim()
        $value = $line.Substring($idx + 1).Trim().Trim('"')
        $values[$key] = $value
    }

    return $values
}

function Get-Required([hashtable]$Values, [string]$Key) {
    if (-not $Values.ContainsKey($Key) -or [string]::IsNullOrWhiteSpace($Values[$Key])) {
        throw "Missing $Key in .env"
    }

    return $Values[$Key]
}

$envValues = Read-EnvFile $EnvPath
$baseUrl = Get-Required $envValues "SMARTCA_BASE_URL"
$apiPrefix = Get-Required $envValues "SMARTCA_API_PREFIX"
$spId = Get-Required $envValues "SMARTCA_SP_ID"
$spPassword = Get-Required $envValues "SMARTCA_SP_PASSWORD"

if ([string]::IsNullOrWhiteSpace($SubscriberId)) {
    $SubscriberId = Get-Required $envValues "SMARTCA_DEFAULT_USER_ID"
}

$url = $baseUrl.TrimEnd("/") + "/" + $apiPrefix.Trim("/").TrimEnd("/") + "/v1/credentials/get_certificate"
$payload = @{
    sp_id = $spId
    sp_password = $spPassword
    user_id = $SubscriberId.Trim()
    serial_number = if ([string]::IsNullOrWhiteSpace($SerialNumber)) { $null } else { $SerialNumber.Trim() }
    transaction_id = "QLCM-CERT-" + [Guid]::NewGuid().ToString("N")
}

try {
    $response = Invoke-RestMethod `
        -Uri $url `
        -Method Post `
        -ContentType "application/json" `
        -Body ($payload | ConvertTo-Json -Depth 5) `
        -TimeoutSec 45

    $certificates = @()
    if ($response.data -and $response.data.user_certificates) {
        $certificates = @($response.data.user_certificates)
    }

    Write-Host "SmartCA credential check"
    Write-Host "  HTTP transport: OK"
    Write-Host "  VNPT status: $($response.status_code)"
    Write-Host "  VNPT message: $($response.message)"
    Write-Host "  Subscriber: $SubscriberId"
    Write-Host "  Certificates: $($certificates.Count)"

    if ($certificates.Count -gt 0) {
        Write-Host "  First serial: $($certificates[0].serial_number)"
        if ($ShowCertificateSubject) {
            Write-Host "  First subject: $($certificates[0].cert_subject)"
        }
    }

    if ($response.status_code -ne 200) {
        exit 1
    }
}
catch {
    $body = $null
    if ($_.Exception.Response -and $_.Exception.Response.GetResponseStream()) {
        $reader = [IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
        try {
            $body = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }

    Write-Host "SmartCA credential check"
    Write-Host "  HTTP transport: ERROR"
    Write-Host "  Error: $($_.Exception.Message)"
    if (-not [string]::IsNullOrWhiteSpace($body)) {
        Write-Host "  Response body: $body"
    }
    exit 1
}
