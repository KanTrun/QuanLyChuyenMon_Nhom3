param(
    [string]$BaseUrl = "http://localhost:8080",
    [string]$CallbackSecret = "",
    [string]$TransactionReference = "smoke-missing-transaction"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http

function Invoke-SmokeRequest {
    param(
        [string]$Name,
        [scriptblock]$Request,
        [int[]]$ExpectedStatusCodes
    )

    try {
        $response = & $Request
        $statusCode = if ($response -is [int]) { $response } else { [int]$response.StatusCode }
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

function Invoke-NoRedirectStatus {
    param(
        [string]$Method,
        [string]$Url,
        [hashtable]$Headers = @{},
        [string]$Body = ""
    )

    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $client = [System.Net.Http.HttpClient]::new($handler)
    try {
        $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($Method), $Url)
        foreach ($header in $Headers.GetEnumerator()) {
            [void]$request.Headers.TryAddWithoutValidation($header.Key, [string]$header.Value)
        }
        if ($Body.Length -gt 0) {
            $request.Content = [System.Net.Http.StringContent]::new($Body, [System.Text.Encoding]::UTF8, "application/json")
        }

        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        return [int]$response.StatusCode
    }
    finally {
        $client.Dispose()
        $handler.Dispose()
    }
}

$base = $BaseUrl.TrimEnd("/")
$readinessUrl = "$base/api/signatures/smartca/readiness"
$callbackUrl = "$base/api/signatures/smartca/callback"

Invoke-SmokeRequest `
    -Name "Health endpoint" `
    -ExpectedStatusCodes @(200) `
    -Request { Invoke-WebRequest -UseBasicParsing "$base/health" }

Invoke-SmokeRequest `
    -Name "SmartCA readiness requires app authentication" `
    -ExpectedStatusCodes @(302, 401, 403) `
    -Request {
        Invoke-NoRedirectStatus -Method "GET" -Url $readinessUrl
    }

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
