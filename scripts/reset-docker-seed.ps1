<#
.SYNOPSIS
  Resets Docker SQL data and re-runs seed scripts for a clean shared database.

.DESCRIPTION
  Stops the stack, removes the SQL Server volume, clears session keys (and optionally
  uploads), then rebuilds and starts Compose. All LAN clients must point to this
  single host to share the same data.

.EXAMPLE
  .\scripts\reset-docker-seed.ps1
  .\scripts\reset-docker-seed.ps1 -KeepUploads
#>
param(
    [switch] $KeepUploads,
    [switch] $KeepSessions
)

$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)

Write-Host 'Stopping Docker Compose and removing SQL volume...'
docker compose down --volumes

if (-not $KeepSessions) {
    Write-Host 'Clearing Data Protection session keys...'
    if (Test-Path '.\app-data\dpkeys') {
        Remove-Item -Recurse -Force '.\app-data\dpkeys\*' -ErrorAction SilentlyContinue
    }
    New-Item -ItemType Directory -Force -Path '.\app-data\dpkeys' | Out-Null
}

if (-not $KeepUploads) {
    Write-Host 'Clearing procedure uploads...'
    if (Test-Path '.\procedure-uploads') {
        Get-ChildItem '.\procedure-uploads' -Force | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host 'Rebuilding and starting with fresh seed...'
docker compose up --build -d

Write-Host ''
Write-Host 'Waiting for web health...'
for ($i = 1; $i -le 60; $i++) {
    try {
        $health = Invoke-WebRequest -Uri 'http://localhost:8080/health' -UseBasicParsing -TimeoutSec 5
        if ($health.StatusCode -eq 200) {
            break
        }
    }
    catch {
        Start-Sleep -Seconds 3
    }
}

Write-Host ''
Write-Host 'Reset complete. Bootstrap login:'
Write-Host '  Username: admin'
Write-Host '  Password: Admin@2026'
Write-Host ''
Write-Host 'Other LAN machines: run .\scripts\show-lan-url.ps1 on this server.'
