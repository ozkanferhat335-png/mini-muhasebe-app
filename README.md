# Mini Muhasebe Uygulaması

Küçük esnaf ve KOBİ'ler için geliştirilmiş, masaüstü tabanlı mini muhasebe uygulaması.

## 🛠️ Teknolojiler

| Bileşen | Teknoloji |
|---------|-----------|
| Dil | C# 7.3 |
| UI Framework | Windows Forms (WinForms) |
| Veritabanı | SQLite (System.Data.SQLite) |
| Mimari | Katmanlı Mimari (4 katman) |
| Güvenlik | PBKDF2 + SHA256 şifre hash, AES-256 şifreleme |
| API | REST / Mock banka API entegrasyonu |

## 📁 Proje Yapısı

```
MiniMuhasebe.sln
├── MiniMuhasebe.Models/          # Veri modelleri (POCO)
│   └── Models.cs                 # Tüm entity sınıfları
│
├── MiniMuhasebe.Data/            # Veri erişim katmanı
│   ├── BaseRepository.cs         # Generic repository base
│   ├── DatabaseInitializer.cs    # DB oluşturma + seed
│   ├── Logger.cs                 # Dosya tabanlı loglama
│   ├── SecurityHelper.cs         # PBKDF2 + AES şifreleme
│   └── Repositories/
│       ├── UserRepository.cs
│       ├── CompanyRepository.cs
│       ├── FiscalPeriodRepository.cs
│       ├── AccountRepository.cs
│       ├── CurrentAccountRepository.cs
│       ├── CurrentAccountTransactionRepository.cs
│       ├── BankAccountRepository.cs
│       ├── BankTransactionRepository.cs
│       ├── IncomeExpenseTransactionRepository.cs
│       ├── TransactionMatchRepository.cs
│       └── AuditLogRepository.cs
│
├── MiniMuhasebe.Business/        # İş mantığı katmanı
│   ├── Interfaces/               # Servis arayüzleri
│   │   ├── IUserService.cs
│   │   ├── ICompanyService.cs
│   │   ├── IBankService.cs
│   │   ├── IIncomeExpenseService.cs
│   │   ├── ICurrentAccountService.cs
│   │   ├── IMatchingService.cs
│   │   └── IReportService.cs
│   └── Services/
│       ├── UserService.cs        # Kimlik doğrulama, kullanıcı yönetimi
│       ├── CompanyService.cs     # Firma yönetimi
│       ├── IncomeExpenseService.cs # Gelir/gider işlemleri
│       ├── CurrentAccountService.cs # Cari hesap yönetimi
│       ├── BankService.cs        # Banka hesapları + API senkronizasyon
│       ├── MatchingService.cs    # Otomatik/manuel eşleştirme
│       ├── ReportService.cs      # Raporlama + CSV dışa aktarım
│       └── BackupService.cs      # Yedekleme + geri yükleme
│
├── MiniMuhasebe.Integration/     # Harici API entegrasyonu
│   ├── Interfaces/
│   │   └── IBankApiProvider.cs   # Banka API arayüzü
│   └── BankingAPIs/
│       └── BankApiClient.cs      # REST + Mock API istemcileri
│
├── MiniMuhasebe.UI/              # Windows Forms UI
│   ├── Program.cs                # Uygulama giriş noktası
│   └── Forms/
│       ├── LoginForm.cs          # Giriş ekranı
│       ├── MainDashboardForm.cs  # Ana panel + menü
│       ├── IncomeExpenseForm.cs  # Gelir/gider CRUD
│       ├── CurrentAccountForm.cs # Cari hesap + hareketler
│       ├── BankAccountsForm.cs   # Banka hesapları + API
│       ├── BankTransactionsForm.cs # Banka hareketleri
│       ├── MatchingForm.cs       # Eşleştirme ekranı
│       ├── ReportsForm.cs        # Raporlar + CSV dışa aktarım
│       └── SettingsForm.cs       # Ayarlar, yedekleme, kullanıcılar
│
└── Database/
    ├── schema.sql                # Veritabanı şeması
    ├── seed-data.sql             # Test verileri
    └── seed-data-clean.sql       # Temiz başlangıç verileri
```

## ✅ Özellikler

### Kullanıcı ve Yetki Yönetimi
- Giriş ekranı (kullanıcı adı/şifre)
- İki rol: Yönetici (Admin) ve Standart Kullanıcı
- PBKDF2 + SHA256 ile güvenli şifre saklama
- Başarısız giriş denemesi sınırlaması (5 deneme)
- Audit log (oturum açma/kapama, işlem takibi)

### Firma ve Dönem Yönetimi
- Birden fazla firma kaydı
- Mali dönem tanımlaması (aylık/yıllık)
- Dönem kapatma
- Aktif firma/dönem seçimi

### Gelir-Gider Takibi
- Manuel fiş/kayıt girişi (CRUD)
- Tarih, belge no, açıklama, tutar, KDV
- Ödeme tipi: Nakit / Banka / Cari
- Hesap kategorisi bazlı sınıflandırma

### Cari Hesap Yönetimi
- Müşteri ve tedarikçi kartı
- Cari hareket takibi (borç/alacak)
- Bakiye hesaplama
- Ekstre görüntüleme

### Banka Hesapları
- Birden fazla banka hesabı
- REST API entegrasyonu (gerçek + Mock)
- Otomatik hareket çekme
- Mükerrer kayıt engelleme (External ID)
- API kimlik bilgileri AES-256 ile şifreli saklanır

### Eşleştirme Sistemi
- Banka hareketlerini muhasebe kayıtlarıyla eşleştirme
- Otomatik eşleme (tutar + tarih + açıklama skoru, min %70)
- Manuel eşleştirme
- Eşleştirme kaldırma
- Bekleyen işlemler takibi

### Raporlama
- Aylık/dönemsel gelir-gider özeti
- Nakit akış raporu (günlük kümülatif)
- Cari ekstre raporu
- Banka hareket raporu
- CSV dışa aktarım (tüm raporlar)

### Yedekleme
- Tek tık yedek alma (SQLite Online Backup API)
- Yedek listesi görüntüleme
- Geri yükleme fonksiyonu
- Eski yedekleri otomatik temizleme

## 🚀 Kurulum

### Gereksinimler
- Windows 7/8/10/11
- .NET Framework 4.7.2
- Visual Studio 2019+ veya MSBuild

### Derleme
```bash
# NuGet paketlerini yükle
nuget restore MiniMuhasebe.sln

# Derle
msbuild MiniMuhasebe.sln /p:Configuration=Release
```

### İlk Çalıştırma
1. `MiniMuhasebe.exe` dosyasını çalıştırın
2. Veritabanı otomatik olarak `%AppData%\MiniMuhasebe\` klasöründe oluşturulur
3. Varsayılan giriş bilgileri:
   - **Kullanıcı Adı:** `admin`
   - **Şifre:** `Admin123!`
4. İlk girişten sonra şifrenizi değiştirin!

## 🔐 Güvenlik

- Şifreler PBKDF2 + SHA256 + 16 byte random salt ile hash'lenir (10.000 iterasyon)
- API kimlik bilgileri AES-256-CBC ile şifreli saklanır
- SQL Injection koruması: Tüm sorgular parametreli
- Başarısız giriş denemesi sınırlaması
- Audit log ile tüm işlemler kayıt altında

## 📊 Veritabanı Şeması

Ana tablolar:
- `Roles`, `Users` - Kullanıcı yönetimi
- `Companies`, `FiscalPeriods` - Firma/dönem
- `Accounts` - Hesap kategorileri
- `CurrentAccounts`, `CurrentAccountTransactions` - Cari
- `BankAccounts`, `BankTransactions` - Banka
- `IncomeExpenseTransactions` - Gelir/gider
- `TransactionMatches`, `MatchingRules` - Eşleştirme
- `AuditLogs`, `AppSettings`, `Backups` - Sistem

## 👤 Geliştirici

**Özkan Ferhat** - 2026
