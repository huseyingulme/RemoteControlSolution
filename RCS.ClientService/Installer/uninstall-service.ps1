# RemoteControlSolution - Windows Service Uninstaller Script

param(
    [string]$ServiceName = "RCS.ClientService"
)

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "HATA: Bu script'i Administrator olarak çalıştırmanız gerekir!" -ForegroundColor Red
    exit 1
}

Write-Host "RCS Client Service kaldırılıyor..." -ForegroundColor Yellow

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if (-not $service) {
    Write-Host "Servis bulunamadı: $ServiceName" -ForegroundColor Yellow
    exit 0
}

if ($service.Status -eq 'Running') {
    Write-Host "Servis durduruluyor..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force
    Start-Sleep -Seconds 2
}

Write-Host "Servis siliniyor..." -ForegroundColor Yellow
sc.exe delete $ServiceName

if ($LASTEXITCODE -eq 0) {
    Write-Host "Servis başarıyla kaldırıldı!" -ForegroundColor Green
} else {
    Write-Host "HATA: Servis kaldırılamadı!" -ForegroundColor Red
    exit 1
}

