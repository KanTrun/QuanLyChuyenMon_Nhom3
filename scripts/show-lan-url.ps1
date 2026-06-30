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

$addresses = Get-NetIPAddress -AddressFamily IPv4 |
    Where-Object {
        $_.IPAddress -notlike '127.*' -and
        $_.PrefixOrigin -ne 'WellKnown'
    } |
    Select-Object -ExpandProperty IPAddress -Unique

if (-not $addresses) {
    Write-Warning 'No LAN IPv4 address found. Check network connection.'
    exit 1
}

Write-Host "QLCM Pro - URLs for other machines on the LAN (port $Port):"
Write-Host ''
foreach ($ip in $addresses) {
    Write-Host ('  http://{0}:{1}' -f $ip, $Port)
}
Write-Host ''
Write-Host ('On this server machine, use: http://localhost:{0}' -f $Port)
