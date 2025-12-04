# Server için Firewall kuralı ekleme script'i

param(
    [int]$Port = 9999,
    [string]$RuleName = "RCS Server App"
)

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "HATA: Bu script'i Administrator olarak çalıştırmanız gerekir!" -ForegroundColor Red
    exit 1
}

Write-Host "Firewall kuralı ekleniyor..." -ForegroundColor Yellow

$existingRule = Get-NetFirewallRule -DisplayName $RuleName -ErrorAction SilentlyContinue

if ($existingRule) {
    Write-Host "Mevcut kural bulundu, güncelleniyor..." -ForegroundColor Yellow
    Remove-NetFirewallRule -DisplayName $RuleName
}

New-NetFirewallRule -DisplayName $RuleName `
    -Direction Inbound `
    -LocalPort $Port `
    -Protocol TCP `
    -Action Allow `
    -Profile Any

if ($LASTEXITCODE -eq 0) {
    Write-Host "Firewall kuralı başarıyla eklendi!" -ForegroundColor Green
    Write-Host "Server artık port $Port'da dinleyebilir." -ForegroundColor Green
} else {
    Write-Host "HATA: Firewall kuralı eklenemedi!" -ForegroundColor Red
    exit 1
}

