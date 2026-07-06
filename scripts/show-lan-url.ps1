<#
.SYNOPSIS
  Prints URLs other LAN machines can use to reach QLCM Pro on this host.

.EXAMPLE
  .\scripts\show-lan-url.ps1
  .\scripts\show-lan-url.ps1 -Port 9090
#>
param(
    [int] $Port = 8080
)

$defaultRoute = Get-NetRoute -AddressFamily IPv4 |
    Where-Object {
        $_.DestinationPrefix -eq '0.0.0.0/0' -and
        $_.NextHop -ne '0.0.0.0'
    } |
    Sort-Object RouteMetric, ifMetric |
    Select-Object -First 1

$allAddresses = Get-NetIPAddress -AddressFamily IPv4 |
    Where-Object {
        $_.IPAddress -notlike '127.*' -and
        $_.PrefixOrigin -ne 'WellKnown' -and
        $_.IPAddress -notlike '169.254.*'
    }

$preferredAddress = if ($defaultRoute) {
    $allAddresses |
        Where-Object { $_.InterfaceIndex -eq $defaultRoute.InterfaceIndex } |
        Select-Object -ExpandProperty IPAddress -First 1
}
else {
    $null
}

$addresses = $allAddresses |
    Select-Object -ExpandProperty IPAddress -Unique

if (-not $addresses) {
    Write-Warning 'No LAN IPv4 address found. Check network connection.'
    exit 1
}

Write-Host "QLCM Pro - URLs for other machines on the LAN (port $Port):"
Write-Host ''
if ($preferredAddress) {
    Write-Host ('Recommended LAN URL: http://{0}:{1}' -f $preferredAddress, $Port)
    Write-Host ''
}
foreach ($ip in $addresses) {
    Write-Host ('  http://{0}:{1}' -f $ip, $Port)
}
Write-Host ''
Write-Host ('On this server machine, use: http://localhost:{0}' -f $Port)
