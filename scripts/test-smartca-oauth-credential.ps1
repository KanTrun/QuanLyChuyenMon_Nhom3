param(
    [string]$EnvPath = ".env"
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

function Get-Value([hashtable]$Values, [string[]]$Keys) {
    foreach ($key in $Keys) {
        if ($Values.ContainsKey($key) -and -not [string]::IsNullOrWhiteSpace($Values[$key])) {
            return $Values[$key].Trim()
        }
    }

    return $null
}

$envValues = Read-EnvFile $EnvPath
$baseUrl = Get-Value $envValues @("SMARTCA_BASE_URL")
$clientId = Get-Value $envValues @("SMARTCA_OAUTH_CLIENT_ID", "SMARTCA_SP_ID")
$clientSecret = Get-Value $envValues @("SMARTCA_OAUTH_CLIENT_SECRET", "SMARTCA_SP_PASSWORD")
$refreshToken = Get-Value $envValues @("SMARTCA_OAUTH_REFRESH_TOKEN")
$username = Get-Value $envValues @("SMARTCA_OAUTH_USERNAME")
$password = Get-Value $envValues @("SMARTCA_OAUTH_PASSWORD")

Write-Host "SmartCA OAuth credential check"
if ([string]::IsNullOrWhiteSpace($baseUrl) -or [string]::IsNullOrWhiteSpace($clientId) -or [string]::IsNullOrWhiteSpace($clientSecret)) {
    Write-Host "  Config: MISSING SMARTCA_BASE_URL and OAuth client id/secret"
    exit 1
}

if ($clientId -notlike "*.apps.smartcaapi.com") {
    Write-Host "  Config: WARNING client id does not look like *.apps.smartcaapi.com"
}

$body = @{
    client_id = $clientId
    client_secret = $clientSecret
}

if (-not [string]::IsNullOrWhiteSpace($refreshToken)) {
    $body["grant_type"] = "refresh_token"
    $body["refresh_token"] = $refreshToken
    $body["scope"] = "sign offline_access"
}
elseif (-not [string]::IsNullOrWhiteSpace($username) -and -not [string]::IsNullOrWhiteSpace($password)) {
    $body["grant_type"] = "password"
    $body["username"] = $username
    $body["password"] = $password
}
else {
    Write-Host "  Config: OAuth app credential found"
    Write-Host "  Missing: SMARTCA_OAUTH_REFRESH_TOKEN or SMARTCA_OAUTH_USERNAME/PASSWORD"
    Write-Host "  Result: cannot call /csc APIs until a SmartCA user grants access"
    exit 2
}

try {
    $token = Invoke-RestMethod `
        -Uri ($baseUrl.TrimEnd("/") + "/auth/token") `
        -Method Post `
        -ContentType "application/x-www-form-urlencoded" `
        -Body $body `
        -TimeoutSec 45

    if ([string]::IsNullOrWhiteSpace($token.access_token)) {
        Write-Host "  Token: missing access_token"
        exit 1
    }

    $headers = @{ Authorization = "Bearer $($token.access_token)" }
    $credentials = Invoke-RestMethod `
        -Uri ($baseUrl.TrimEnd("/") + "/csc/credentials/list") `
        -Method Post `
        -Headers $headers `
        -ContentType "application/json" `
        -Body "{}" `
        -TimeoutSec 45

    $count = 0
    if ($credentials.content) {
        $count = @($credentials.content).Count
    }

    Write-Host "  Token: OK"
    Write-Host "  Credentials: $count"
    if ($count -eq 0) {
        exit 1
    }
}
catch {
    Write-Host "  HTTP transport: ERROR"
    Write-Host "  Error: $($_.Exception.Message)"
    Write-Host "  Hint: If this happens with password grant, ask VNPT to confirm the app is allowed to use password grant and that the signer account is active for sandbox."
    exit 1
}
