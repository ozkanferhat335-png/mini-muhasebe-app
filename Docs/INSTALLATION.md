# Mini Muhasebe Uygulaması - Kurulum Kılavuzu

## Sistem Gereksinimleri

- **İşletim Sistemi:** Windows 7 SP1 veya üzeri (64-bit önerilir)
- **.NET Framework:** 4.7.2 veya üzeri
- **RAM:** En az 2 GB
- **Disk Alanı:** 500 MB (veritabanı için ek alan)

## Geliştirme Ortamı Kurulumu

### 1. Visual Studio Kurulumu

- Visual Studio 2019 Community Edition veya üzeri indirin
- Kurulum sırasında aşağıdaki workload'ları seçin:
  - Desktop development with C#
  - .NET desktop development

### 2. Repository'i Clone Edin

```bash
git clone https://github.com/ozkanferhat335-png/mini-muhasebe-app.git
cd mini-muhasebe-app
```

### 3. Proje Çözümünü Açın

- Visual Studio'da `MiniMuhasebe.sln` dosyasını açın

### 4. NuGet Paketlerini Geri Yükleyin

```
Tools → NuGet Package Manager → Package Manager Console

Update-Package
```

Veya Visual Studio'da **Rebuild Solution** yapın (otomatik olarak paketleri yükler).

### 5. Veritabanını Oluşturun

#### Seçenek A: Otomatik (Uygulama ilk başlatılırken)

Uygulama ilk başlatıldığında, veritabanı otomatik olarak oluşturulacak ve örnek veriler yüklenecektir.

#### Seçenek B: Manuel

1. SQLite tarayıcı aracı indirin (örneğin: DB Browser for SQLite)
2. `Database/schema.sql` dosyasını açın
3. Sorguyu çalıştırarak tabloları oluşturun
4. `Database/seed-data.sql` ile örnek verileri yükleyin

### 6. Uygulamayı Çalıştırın

1. Solution Explorer'da **MiniMuhasebe.UI** projesine sağ tıklayın
2. **Set as Startup Project** seçin
3. **F5** tuşuna basın veya Debug → Start Debugging

## İlk Giriş

**Varsayılan Kimlik Bilgileri:**
- **Kullanıcı Adı:** admin
- **Şifre:** Admin123!

⚠️ **Uyarı:** İlk giriş yapıldıktan sonra, yönetici şifresini güçlü bir şifre ile değiştirin.

## Banka API Kurulumu

### Banka API Anahtarını Yapılandırın

1. Uygulamayı açın
2. **Ayarlar → Banka API Ayarları** kısmına gidin
3. Banka adı ve API bilgilerini girin:
   - API Base URL
   - Client ID
   - Client Secret
   - API Key (varsa)

4. **Test Bağlantısı** butonuna tıklayarak bağlantıyı doğrulayın

## Veritabanı Yedeklemesi

### Otomatik Yedekleme

- Uygulama her kapanırken otomatik olarak yedek alır
- Yedekler: `DatabaseBackups/` klasöründe saklanır
- Format: `MiniMuhasebe_YYYY-MM-DD_HH-MM-SS.db`

### Manuel Yedekleme

1. **Ayarlar** menüsüne gidin
2. **Yedekleme** kısmında **Yedek Al** butonuna tıklayın
3. Yedek dosyasının konumunu seçin

### Geri Yükleme

1. **Ayarlar** menüsüne gidin
2. **Yedekleme** kısmında **Geri Yükle** butonuna tıklayın
3. Yedek dosyasını seçin
4. Uygulama yeniden başlatılacaktır

## Sorun Giderme

### Veritabanı Dosyası Kilidi

**Sorun:** "Database is locked" hatası

**Çözüm:**
1. Uygulamayı kapatın
2. `MiniMuhasebe.db-journal` dosyasını silin (varsa)
3. Uygulamayı yeniden başlatın

### .NET Framework Hatası

**Sorun:** ".NET Framework 4.7.2 required" hatası

**Çözüm:**
1. https://dotnet.microsoft.com/download/dotnet-framework adresine gidin
2. .NET Framework 4.7.2 veya üzerini indirin ve kurun

### API Bağlantı Sorunu

**Sorun:** "Cannot connect to bank API" hatası

**Çözüm:**
1. İnternet bağlantınızı kontrol edin
2. API anahtarlarını ve URL'lerini doğrulayın (Ayarlar → Banka API)
3. Banka API sağlayıcısının status sayfasını kontrol edin

### NuGet Paket Yükleme Başarısız

**Sorun:** NuGet paketleri yüklenmedi

**Çözüm:**
```bash
Tools → Options → NuGet Package Manager → Package Sources

Package source: https://api.nuget.org/v3/index.json
```

Ardından:
```
Package Manager Console
Update-Package -Reinstall
```

## İletişim ve Destek

Kurulum sırasında sorun yaşarsanız:

1. **Issues** sayfasında benzer sorunları arayın
2. Yeni issue açıp ayrıntılı hata mesajını paylaşın
3. Log dosyalarını (`Logs/` klasöründe) ekleyin

---

**Son Güncelleme:** 2026-05-14
