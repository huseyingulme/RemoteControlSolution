# RemoteControlSolution - Detaylı Kullanım Kılavuzu

Bu doküman, RemoteControlSolution sisteminin nasıl kurulacağını, yapılandırılacağını ve kullanılacağını adım adım anlatır.

## 📋 İçindekiler

1. [Gereksinimler](#gereksinimler)
2. [Ana PC Kurulumu (ServerApp)](#ana-pc-kurulumu-serverapp)
3. [İzlenecek PC Kurulumu (ClientService)](#izlenecek-pc-kurulumu-clientservice)
4. [İlk Kullanım](#ilk-kullanım)
5. [Yapılandırma](#yapılandırma)
6. [Windows Service Olarak Kurulum](#windows-service-olarak-kurulum)
7. [Sorun Giderme](#sorun-giderme)
8. [Güvenlik Notları](#güvenlik-notları)

---

## 🔧 Gereksinimler

### Ana PC (ServerApp)
- Windows 10/11
- .NET 9.0 Runtime (veya SDK)
- Minimum 4 GB RAM
- Ağ bağlantısı

### İzlenecek PC (ClientService)
- Windows 10/11
- .NET 9.0 Runtime (veya SDK)
- Minimum 2 GB RAM
- Ağ bağlantısı (ana PC'ye erişebilmeli)
- Administrator yetkileri (Windows Service kurulumu için)

---

## 💻 Ana PC Kurulumu (ServerApp)

### Adım 1: Projeyi Derle

1. Visual Studio 2022 veya VS Code'u aç
2. `RemoteControlSolution.sln` dosyasını aç
3. Solution Explorer'da `RCS.ServerApp` projesine sağ tık → **Set as Startup Project**
4. **Build** → **Build Solution** (Ctrl+Shift+B)
5. Derleme başarılı olmalı

### Adım 2: Release Modunda Derle (Opsiyonel)

1. **Build** → **Configuration Manager**
2. **Active solution configuration** → **Release** seç
3. **Build Solution** (Ctrl+Shift+B)
4. Exe dosyası: `RCS.ServerApp\bin\Release\net9.0-windows\RCS.ServerApp.exe`

### Adım 3: Firewall Kuralı Ekle

Windows Firewall'un portu açması gerekiyor:

**Yöntem 1: PowerShell Script (Önerilen)**

1. PowerShell'i **Administrator** olarak aç
2. `RCS.ServerApp\Installer\add-firewall-rule.ps1` dosyasına gidin
3. Çalıştır:
   ```powershell
   cd "C:\Users\husey\Desktop\RemoteControlSolution\RCS.ServerApp\Installer"
   .\add-firewall-rule.ps1 -Port 9999
   ```

**Yöntem 2: Manuel**

1. Windows Defender Firewall → Advanced Settings
2. Inbound Rules → New Rule
3. Port → Next
4. TCP → Specific local ports: **9999** → Next
5. Allow the connection → Next
6. Tüm profilleri seç → Next
7. Name: "RCS Server App" → Finish

### Adım 4: İlk Çalıştırma

1. `RCS.ServerApp.exe` dosyasını çalıştır
2. Açılan pencerede:
   - Port numarasını kontrol et (varsayılan: 9999)
   - **Start Listening** butonuna tıkla
3. Status bar'da "Listening on port 9999" yazmalı

### Adım 5: IP Adresini Öğren

ServerApp'in IP adresini öğrenmeniz gerekiyor:

**Yöntem 1: PowerShell**
```powershell
ipconfig
```
**IPv4 Address** değerini not edin (ör: 192.168.1.100)

**Yöntem 2: UI'da Gösterme (Gelecek güncellemede)**

Şu anda IP adresini manuel olarak öğrenmeniz gerekiyor.

---

## 🖥️ İzlenecek PC Kurulumu (ClientService)

### Adım 1: Projeyi Derle

1. Visual Studio'da `RCS.ClientService` projesini aç
2. **Build** → **Build Solution**
3. Release modunda derle (önerilen)

### Adım 2: Dosyaları Kopyala

İzlenecek PC'ye şu dosyaları kopyalayın:

```
RCS.ClientService\
├── bin\Release\net9.0\
│   ├── RCS.ClientService.exe
│   ├── RCS.ClientService.dll
│   ├── RCS.Shared.dll
│   └── (diğer dependency dosyaları)
├── agentsettings.json
└── Installer\ (opsiyonel)
```

**Not:** Tüm `.dll` ve `.exe` dosyalarını kopyalayın.

### Adım 3: Konfigürasyonu Ayarla

`agentsettings.json` dosyasını açın ve düzenleyin:

```json
{
  "ServerIp": "192.168.1.100",  // Ana PC'nin IP adresi
  "ServerPort": 9999,
  "CaptureIntervalMs": 100,      // 100ms = 10 FPS
  "JpegQuality": 75,             // 1-100 arası
  "MaxWidth": null,              // null = orijinal boyut
  "MaxHeight": null
}
```

**Önemli:** 
- `ServerIp`: Ana PC'nin IP adresini yazın
- `ServerPort`: Ana PC'deki port ile aynı olmalı (varsayılan: 9999)
- `CaptureIntervalMs`: Düşük değer = daha yüksek FPS ama daha fazla bant genişliği
  - 50ms = 20 FPS (hızlı, daha fazla bant genişliği)
  - 100ms = 10 FPS (orta, önerilen)
  - 200ms = 5 FPS (yavaş, az bant genişliği)

### Adım 4: İlk Test (Konsol Modu)

1. PowerShell veya CMD açın
2. `RCS.ClientService.exe` klasörüne gidin
3. Çalıştırın:
   ```cmd
   RCS.ClientService.exe
   ```
4. Konsolda bağlantı mesajlarını görmelisiniz
5. Ana PC'de (ServerApp) client listesinde görünmelidir

### Adım 5: Windows Service Olarak Kur (Önerilen)

**Yöntem 1: PowerShell Script (Önerilen)**

1. PowerShell'i **Administrator** olarak aç
2. Script'i çalıştır:
   ```powershell
   cd "C:\Path\To\RCS.ClientService\Installer"
   .\install-service.ps1
   ```
3. Servis otomatik oluşturulur ve başlatılır

**Yöntem 2: Manuel (sc.exe)**

1. CMD'yi **Administrator** olarak aç
2. Çalıştır:
   ```cmd
   cd "C:\Path\To\RCS.ClientService\bin\Release\net9.0"
   sc create RCS.ClientService binPath= "C:\Path\To\RCS.ClientService\bin\Release\net9.0\RCS.ClientService.exe" start= auto
   sc description RCS.ClientService "Remote Control Solution - Client Service"
   sc start RCS.ClientService
   ```

**Servis Komutları:**
```powershell
# Başlat
Start-Service -Name RCS.ClientService

# Durdur
Stop-Service -Name RCS.ClientService

# Durum
Get-Service -Name RCS.ClientService

# Kaldır
sc.exe delete RCS.ClientService
```

### Adım 6: Otomatik Başlatma

Windows Service olarak kurduysanız, Windows açıldığında otomatik başlar.

**Servis Yönetimi:**
1. `Win + R` → `services.msc`
2. "RCS Client Service" servisini bulun
3. Sağ tık → Properties
4. Startup type: **Automatic** olmalı

---

## 🚀 İlk Kullanım

### ServerApp'de Client Görme

1. ServerApp'i çalıştırın
2. **Start Listening** butonuna tıklayın
3. ClientService çalıştığında, client listesinde görünür:
   - Machine Name
   - IP Address
   - Online durumu
   - Thumbnail (canlı önizleme)

### Remote View Açma

1. Client listesinde bir client seçin
2. **Open View** butonuna tıklayın
3. Remote view penceresi açılır
4. Ekran görüntüsü görünmelidir

### Kontrol Modu

1. Remote view penceresinde **Start Control** butonuna tıklayın
2. Artık mouse ve klavye kontrolü aktif:
   - **Mouse:** Hareket, tıklama, scroll
   - **Klavye:** Tuş basma, metin gönderme

**Kontrol Modu Kapatma:**
- **Stop Control** butonuna tıklayın
- Artık sadece görüntüleme modundasınız

---

## ⚙️ Yapılandırma

### ServerApp Konfigürasyonu

`serversettings.json` dosyası:

```json
{
  "Port": 9999,
  "AutoStart": false,           // Uygulama açıldığında otomatik dinlemeye başla
  "HeartbeatTimeoutSeconds": 15,
  "MaxClients": 100,
  "LogDirectory": "Logs"
}
```

**Port Değiştirme:**
1. UI'da port numarasını değiştirin
2. Firewall kuralını güncelleyin
3. ClientService'deki `agentsettings.json`'da da aynı portu kullanın

### ClientService Konfigürasyonu

`agentsettings.json` dosyası:

```json
{
  "ServerIp": "192.168.1.100",
  "ServerPort": 9999,
  "CaptureIntervalMs": 100,
  "JpegQuality": 75,
  "MaxWidth": null,
  "MaxHeight": null
}
```

**Performans Ayarları:**

| Parametre | Değer | Açıklama |
|-----------|-------|----------|
| CaptureIntervalMs | 50-200 | Düşük = yüksek FPS, yüksek bant genişliği |
| JpegQuality | 50-100 | Düşük = küçük dosya, düşük kalite |
| MaxWidth/MaxHeight | null veya sayı | Görüntü boyutunu sınırla (örn: 1920x1080) |

**Örnek Konfigürasyonlar:**

**Yüksek Performans (Hızlı Ağ):**
```json
{
  "CaptureIntervalMs": 50,
  "JpegQuality": 85,
  "MaxWidth": null,
  "MaxHeight": null
}
```

**Düşük Bant Genişliği (Yavaş Ağ):**
```json
{
  "CaptureIntervalMs": 200,
  "JpegQuality": 50,
  "MaxWidth": 1280,
  "MaxHeight": 720
}
```

---

## 🔄 Windows Service Olarak Kurulum

### ClientService'i Windows Service Olarak Kurma

**Adım 1: Release Build**

```cmd
cd RCS.ClientService
dotnet build -c Release
```

**Adım 2: Installer Script Çalıştır**

PowerShell'i Administrator olarak aç:
```powershell
cd RCS.ClientService\Installer
.\install-service.ps1
```

**Adım 3: Servis Durumunu Kontrol Et**

```powershell
Get-Service -Name RCS.ClientService
```

Status: **Running** olmalı.

### Service Kaldırma

```powershell
cd RCS.ClientService\Installer
.\uninstall-service.ps1
```

veya manuel:
```cmd
sc.exe delete RCS.ClientService
```

---

## 🐛 Sorun Giderme

### ServerApp Başlamıyor

**Sorun:** "Port already in use" hatası

**Çözüm:**
1. Port 9999'u kullanan başka uygulama var mı kontrol edin
2. Farklı bir port kullanın (örn: 8888)
3. Veya portu kullanan uygulamayı kapatın

**Kontrol:**
```cmd
netstat -ano | findstr :9999
```

### ClientService Bağlanamıyor

**Sorun:** "Failed to connect to server"

**Kontrol Listesi:**
1. ✅ ServerApp çalışıyor mu?
2. ✅ ServerApp'de "Listening" durumunda mı?
3. ✅ `agentsettings.json`'da doğru IP adresi var mı?
4. ✅ Port numarası eşleşiyor mu?
5. ✅ Firewall kuralı eklendi mi?
6. ✅ Ağ bağlantısı var mı? (ping test edin)

**Ping Test:**
```cmd
ping 192.168.1.100
```

### Görüntü Gelmiyor

**Kontrol:**
1. ClientService çalışıyor mu?
2. Client listesinde "Online" görünüyor mu?
3. Log dosyalarını kontrol edin:
   - `RCS.ClientService\Logs\agent.log`
   - `RCS.ServerApp\Logs\server.log`

**Log Dosyalarını Aç:**
```powershell
# ClientService
notepad RCS.ClientService\Logs\agent.log

# ServerApp
notepad RCS.ServerApp\Logs\server.log
```

### Yavaş Performans

**Çözümler:**
1. `CaptureIntervalMs` değerini artırın (200ms)
2. `JpegQuality` değerini düşürün (50-60)
3. `MaxWidth` ve `MaxHeight` ekleyin (1280x720)
4. Ağ bant genişliğini kontrol edin

### Servis Başlamıyor

**Kontrol:**
```powershell
Get-EventLog -LogName Application -Source "RCS.ClientService" -Newest 10
```

**Çözüm:**
1. Executable path doğru mu kontrol edin
2. `agentsettings.json` dosyası doğru konumda mı?
3. Log dosyalarını kontrol edin

---

## 🔒 Güvenlik Notları

### ⚠️ ÖNEMLİ UYARILAR

1. **Şifreleme Yok:** Bu sistem şu anda şifreleme kullanmıyor. Yerel ağlarda kullanın.

2. **İzin Gereksinimleri:**
   - ClientService Administrator yetkisi gerektirebilir
   - Windows Service kurulumu için Administrator gereklidir

3. **Güvenlik Duvarı:**
   - Port 9999'u sadece güvendiğiniz ağlarda açın
   - Mümkünse sadece yerel ağda kullanın

4. **Yasal Uyarı:**
   - Bu sistemi sadece kendi bilgisayarlarınızda veya izin aldığınız bilgisayarlarda kullanın
   - İzinsiz erişim yasaktır ve suçtur

### Güvenlik İyileştirmeleri (Gelecek)

- TLS/SSL şifreleme
- Kimlik doğrulama (password/token)
- IP whitelist
- Şifreli paketler

---

## 📞 Destek

### Log Dosyaları

Sorun yaşarsanız log dosyalarını kontrol edin:

- **ServerApp:** `Logs\server.log`
- **ClientService:** `Logs\agent.log`

### Hata Raporlama

Hataları raporlarken şunları ekleyin:
1. Log dosyası içeriği
2. Yapılandırma dosyaları
3. Hata mesajı ekran görüntüsü
4. Sistem bilgileri (OS, .NET version)

---

## 🎯 Hızlı Başlangıç Checklist

### Ana PC (ServerApp)
- [ ] Projeyi derledim
- [ ] Firewall kuralı ekledim
- [ ] ServerApp'i çalıştırdım
- [ ] "Start Listening" butonuna tıkladım
- [ ] IP adresimi öğrendim

### İzlenecek PC (ClientService)
- [ ] Projeyi derledim
- [ ] Dosyaları kopyaladım
- [ ] `agentsettings.json`'da IP adresini ayarladım
- [ ] ClientService'i test ettim (konsol modu)
- [ ] Windows Service olarak kurdum (opsiyonel)

### İlk Test
- [ ] ServerApp'de client görünüyor
- [ ] Remote view açılabiliyor
- [ ] Görüntü geliyor
- [ ] Kontrol modu çalışıyor

---

**Başarılar! 🎉**

Sorularınız için README.md ve bu kılavuzu kontrol edin.

