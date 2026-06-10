param(
    [string]$BaseUrl = "http://localhost:8080",
    [string]$CallbackSecret = "",
    [string]$TransactionReference = "smoke-missing-transaction"
)

$ErrorActionPreference = "Stop"

function Invoke-SmokeRequest {
    param(
        [string]$Name,
        [scriptblock]$Request,
        [int[]]$ExpectedStatusCodes
    )

    try {
        $response = & $Request
        $statusCode = [int]$response.StatusCode
    }
    catch {
        if ($_.Exception.Response -eq $null) {
            throw
        }

        $statusCode = [int]$_.Exception.Response.StatusCode
    }

    if ($ExpectedStatusCodes -notcontains $statusCode) {
        throw "$Name failed. Expected $($ExpectedStatusCodes -join ', '), got $statusCode."
    }

    Write-Host "PASS $Name -> HTTP $statusCode"
}

$base = $BaseUrl.TrimEnd("/")
$callbackUrl = "$base/api/signatures/smartca/callback"

Invoke-SmokeRequest `
    -Name "Health endpoint" `
    -ExpectedStatusCodes @(200) `
    -Request { Invoke-WebRequest -UseBasicParsing "$base/health" }

Invoke-SmokeRequest `
    -Name "SmartCA callback without secret is forbidden" `
    -ExpectedStatusCodes @(403) `
    -Request {
        Invoke-WebRequest `
            -UseBasicParsing `
            -MaximumRedirection 0 `
            -Method Post `
            -ContentType "application/json" `
            -Body "{}" `
            $callbackUrl
    }

if (-not [string]::IsNullOrWhiteSpace($CallbackSecret)) {
    $body = @{ transactionCode = $TransactionReference } | ConvertTo-Json -Compress
    Invoke-SmokeRequest `
        -Name "SmartCA callback accepts configured secret" `
        -ExpectedStatusCodes @(400, 404, 503) `
        -Request {
            Invoke-WebRequest `
                -UseBasicParsing `
                -MaximumRedirection 0 `
                -Method Post `
                -ContentType "application/json" `
                -Headers @{ "X-QLCM-SMARTCA-CALLBACK-SECRET" = $CallbackSecret } `
                -Body $body `
                $callbackUrl
        }
}

Write-Host "SmartCA API smoke completed for $base"
