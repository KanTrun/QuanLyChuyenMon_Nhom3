#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Opens inbound TCP on the host for QLCM Pro LAN access.

.DESCRIPTION
  Creates or updates a Windows Firewall rule allowing other machines on the LAN
  to reach the QLCM web port (default 8080, matching APP_HTTP_PORT in .env).

.EXAMPLE
  .\scripts\open-lan-firewall.ps1
  .\scripts\open-lan-firewall.ps1 -Port 9090
#>
param(
    [int] $Port = 8080,
    [string] $RuleName = "QLCM Pro Web (LAN)"
)

$existing = Get-NetFirewallRule -DisplayName $RuleName -ErrorAction SilentlyContinue
if ($existing) {
    Remove-NetFirewallRule -DisplayName $RuleName
}

New-NetFirewallRule `
    -DisplayName $RuleName `
    -Direction Inbound `
    -Action Allow `
    -Protocol TCP `
    -LocalPort $Port `
    -Profile Private, Public, Domain `
    | Out-Null

$publicWifi = Get-NetConnectionProfile -ErrorAction SilentlyContinue |
    Where-Object { $_.NetworkCategory -eq 'Public' -and $_.InterfaceAlias -match 'Wi-?Fi|WLAN|Ethernet' }
foreach ($profile in $publicWifi) {
    Set-NetConnectionProfile -InterfaceIndex $profile.InterfaceIndex -NetworkCategory Private -ErrorAction SilentlyContinue
    Write-Host "Set network '$($profile.Name)' ($($profile.InterfaceAlias)) to Private (recommended for LAN)."
}

Write-Host "Firewall rule '$RuleName' allows inbound TCP $Port (Private, Public, Domain profiles)."
Write-Host "Run .\scripts\show-lan-url.ps1 -Port $Port to print client URLs."
