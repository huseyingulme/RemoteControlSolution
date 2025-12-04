# RemoteControlSolution - Windows Service Installer Script
# Bu script'i Administrator olarak çalıştırmanız gerekir

param(
    [string]$ServiceName = "RCS.ClientService",
    [string]$DisplayName = "Remote Control Solution - Client Service",
    [string]$Description = "İzlenecek bilgisayarda çalışan uzak kontrol agent servisi",
    [string]$ExecutablePath = ""
)

# Administrator kontrolü
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "HATA: Bu script'i Administrator olarak çalıştırmanız gerekir!" -ForegroundColor Red
    Write-Host "PowerShell'i sağ tıklayıp 'Run as Administrator' seçin." -ForegroundColor Yellow
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RCS Client Service Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Executable path'i bul
if ([string]::IsNullOrEmpty($ExecutablePath)) {
    $scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
    $projectPath = Split-Path -Parent $scriptPath
    $exePath = Join-Path $projectPath "bin\Release\net9.0\RCS.ClientService.exe"
    
    if (-not (Test-Path $exePath)) {
        Write-Host "HATA: Executable bulunamadı: $exePath" -ForegroundColor Red
        Write-Host "Lütfen önce projeyi Release modunda derleyin:" -ForegroundColor Yellow
        Write-Host "  dotnet build -c Release" -ForegroundColor Yellow
        exit 1
    }
} else {
    $exePath = $ExecutablePath
}

Write-Host "Service Name: $ServiceName" -ForegroundColor Green
Write-Host "Executable: $exePath" -ForegroundColor Green
Write-Host ""

# Mevcut servisi kontrol et ve kaldır
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "Mevcut servis bulundu, kaldırılıyor..." -ForegroundColor Yellow
    
    if ($existingService.Status -eq 'Running') {
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 2
    }
    
    sc.exe delete $ServiceName
    Start-Sleep -Seconds 2
}

# Yeni servisi oluştur
Write-Host "Yeni servis oluşturuluyor..." -ForegroundColor Yellow

$result = sc.exe create $ServiceName `
    binPath= "`"$exePath`"" `
    DisplayName= "$DisplayName" `
    start= auto `
    type= own

if ($LASTEXITCODE -ne 0) {
    Write-Host "HATA: Servis oluşturulamadı!" -ForegroundColor Red
    exit 1
}

# Açıklama ekle
sc.exe description $ServiceName "$Description"

Write-Host "Servis başarıyla oluşturuldu!" -ForegroundColor Green
Write-Host ""

# Servisi başlat
Write-Host "Servis başlatılıyor..." -ForegroundColor Yellow
Start-Service -Name $ServiceName

if ($LASTEXITCODE -eq 0) {
    Write-Host "Servis başarıyla başlatıldı!" -ForegroundColor Green
} else {
    Write-Host "UYARI: Servis başlatılamadı, manuel olarak başlatabilirsiniz:" -ForegroundColor Yellow
    Write-Host "  Start-Service -Name $ServiceName" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Kurulum tamamlandı!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Servis komutları:" -ForegroundColor Yellow
Write-Host "  Başlat:   Start-Service -Name $ServiceName" -ForegroundColor White
Write-Host "  Durdur:   Stop-Service -Name $ServiceName" -ForegroundColor White
Write-Host "  Durum:    Get-Service -Name $ServiceName" -ForegroundColor White
Write-Host "  Kaldır:   sc.exe delete $ServiceName" -ForegroundColor White
Write-Host ""

