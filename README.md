# Mini Muhasebe Uygulaması

Küçük işletmeler için geliştirilmiş kapsamlı muhasebe yönetim sistemi.

## Teknolojiler

- **Dil:** C# 7.3
- **UI Framework:** Windows Forms (WinForms)
- **Veritabanı:** SQLite
- **Mimari:** Katmanlı Mimari (UI, Business Logic, Data Access, Integration)
- **Hedef Framework:** .NET Framework 4.7.2

## Proje Yapısı

```
MiniMuhasebe.sln
├── MiniMuhasebe.Models/          # Veri modelleri
│   └── Models.cs                 # Tüm entity sınıfları
│
├── MiniMuhasebe.Data/            # Veri erişim katmanı
│   ├── BaseRepository.cs         # Temel repository
│   ├── DatabaseInitializer.cs    # DB başlatma ve seed
│   ├── Logger.cs                 # Loglama
│   ├── SecurityHelper.cs         # Şifreleme (PBKDF2 + AES)
│   └── Repositories/
│       ├── UserRepository.cs
│       ├── CompanyRepository.cs
│       ├── FiscalPeriodRepository.cs
│       ├── AccountRepository.cs
│       ├── BankAccountRepository.cs
│       ├── BankTransactionRepository.cs
│       ├── CurrentAccountRepository.cs
│       ├── CurrentAccountTransactionRepository.cs
│       ├── IncomeExpenseTransactionRepository.cs
│       ├── MatchingRepository.cs
│       ├── AuditLogRepository.cs
│       └── AppSettingsRepository.cs
│
├── MiniMuhasebe.Integration/     # Dış API entegrasyonu
│   ├── Interfaces/
│   │   └── IBankApiProvider.cs
│   └── BankingAPIs/
│       ├── BankApiClient.cs
│       └── MockBankApiProvider.cs
│
├── MiniMuhasebe.Business/        # İş mantığı katmanı
│   └── Services/
│       ├── UserService.cs
│       ├── CompanyService.cs
│       ├── FiscalPeriodService.cs
│       ├── AccountService.cs
│       ├── IncomeExpenseService.cs
│       ├── CurrentAccountService.cs
│       ├── CurrentAccountTransactionService.cs
│       ├── BankService.cs
│       ├── MatchingService.cs
│       ├── BackupService.cs
│       ├── AuditLogService.cs
│       └── ReportService.cs
│
├── MiniMuhasebe.UI/              # Windows Forms UI
│   ├── Program.cs
│   ├── AppSession.cs             # Oturum yönetimi
│   └── Forms/
│       ├── LoginForm.cs
│       ├── MainDashboardForm.cs
│       ├── IncomeExpenseForm.cs
│       ├── CurrentAccountForm.cs
│       ├── BankAccountsForm.cs
│       ├── BankTransactionsForm.cs
│       ├── MatchingForm.cs
│       ├── ReportsForm.cs
│       └── SettingsForm.cs
│
└── Database/
    ├── schema.sql                # Veritabanı şeması
    ├── seed-data.sql             # Örnek veriler
    └── seed-data-clean.sql       # Minimal başlangıç verisi
```

## Kurulum

### Gereksinimler
- Windows 10/11
- .NET Framework 4.7.2 veya üzeri
- Visual Studio 2019/2022

### Adımlar

1. Projeyi klonlayın veya indirin
2. `MiniMuhasebe.sln` dosyasını Visual Studio ile açın
3. NuGet paketlerini geri yükleyin:
   ```
   Tools → NuGet Package Manager → Restore NuGet Packages
   ```
4. Projeyi derleyin (Build → Build Solution veya `Ctrl+Shift+B`)
5. `MiniMuhasebe.UI` projesini başlangıç projesi olarak ayarlayın
6. Uygulamayı çalıştırın (`F5`)

### İlk Giriş

- **Kullanıcı Adı:** `admin`
- **Şifre:** `Admin123!`

Veritabanı ilk çalıştırmada otomatik olarak oluşturulur.
Konum: `%AppData%\MiniMuhasebe\MiniMuhasebe.db`

## Özellikler

### ✅ Kullanıcı ve Yetki Yönetimi
- Giriş ekranı (kullanıcı adı/şifre)
- İki rol sistemi (Yönetici, Standart Kullanıcı)
- PBKDF2 + SHA256 ile güvenli şifre saklama
- AES-256 ile API anahtarı şifreleme
- Audit log (oturum açma/kapama, CRUD işlemleri)

### ✅ Firma ve Dönem Yönetimi
- Birden fazla firma kaydı
- Mali dönem tanımlaması (aylık/yıllık)
- Aktif firma/dönem seçimi
- Yıllık 12 aylık dönem otomatik oluşturma

### ✅ Gelir-Gider Takibi
- Manuel fiş/kayıt girişi (CRUD)
- Tarih, belge no, açıklama, tutar, KDV
- Ödeme tipi seçimi (nakit/banka/cari)
- CSV dışa aktarım

### ✅ Cari Hesap Yönetimi
- Müşteri ve tedarikçi kartı
- Cari hareket takibi (borç/alacak)
- Bakiye ve ekstre görüntüleme

### ✅ Banka Hesapları
- Birden fazla banka hesabı
- API entegrasyonu (REST, SOAP, OpenBanking)
- Mock API ile test modu
- Otomatik hareket çekme
- Mükerrer kayıt engelleme (External ID)

### ✅ Eşleştirme Sistemi
- Banka hareketlerini muhasebe kayıtlarıyla eşleştirme
- Otomatik eşleme (tutar, tarih, açıklama bazlı skorlama)
- Manuel eşleştirme
- Eşleştirme kuralları yönetimi

### ✅ Raporlama
- Gelir-Gider Özeti
- Nakit Akış Raporu
- Banka Hareket Raporu
- Cari Ekstre Raporu
- Dönem Özeti
- Eşleştirme Raporu
- CSV dışa aktarım

### ✅ Yedekleme
- Tek tık yedek alma (SQLite Online Backup API)
- Yedek listesi görüntüleme
- Geri yükleme fonksiyonu
- Otomatik eski yedek temizleme

## Güvenlik

- Şifreler PBKDF2 + SHA256 + 10.000 iterasyon ile hash'lenir
- API anahtarları AES-256-CBC ile şifrelenir
- Tüm SQL sorguları parametreli (SQL injection koruması)
- Audit log ile tüm işlemler izlenir

## Geliştirici Notları

- Banka API entegrasyonu için `MockBankApiProvider` test amaçlıdır
- Gerçek banka API'si için `RestBankApiProvider` sınıfını genişletin
- `IBankApiProvider` arayüzünü implement ederek yeni sağlayıcı ekleyebilirsiniz
- Veritabanı yolu: `%AppData%\MiniMuhasebe\MiniMuhasebe.db`
- Log dosyaları: `Logs\MiniMuhasebe.log`
- Yedekler: `Backups\` klasörü

## Lisans

Bu proje eğitim amaçlı geliştirilmiştir.
Özkan Ferhat - 2026
