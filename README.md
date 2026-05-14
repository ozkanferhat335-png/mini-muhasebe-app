# Mini Muhasebe Uygulaması

**Açıklama:** Küçük esnaf ve KOBİ'ler için geliştirilmiş, masaüstü tabanlı mini muhasebe uygulaması.

## Teknolojiler

- **Dil:** C# 7.3
- **UI Framework:** Windows Forms (WinForms)
- **Veritabanı:** SQLite
- **Mimari:** Katmanlı Mimari (UI, Business Logic, Data Access, Integration)
- **API Entegrasyonu:** Banka API (Ödeme Sistemleri)

## Özellikler

✅ **Kullanıcı ve Yetki Yönetimi**
- Giriş ekranı (kullanıcı adı/şifre)
- İki rol sistemi (Yönetici, Standart Kullanıcı)
- Şifre hash + salt ile güvenli saklama
- Audit log (oturum açma/kapama)

✅ **Firma ve Dönem Yönetimi**
- Birden fazla firma kaydı
- Mali dönem tanımlaması
- Aktif firma/dönem seçimi

✅ **Gelir-Gider Takibi**
- Manuel fiş/kayıt girişi (CRUD)
- Tarih, belge no, açıklama, tutar, KDV
- Ödeme tipi seçimi (nakit/banka/cari)

✅ **Cari Hesap Yönetimi**
- Müşteri ve tedarikçi kartı
- Cari hareket takibi (borç/alacak)
- Bakiye ve ekstre görüntüleme

✅ **Banka Hesapları**
- Birden fazla banka hesabı
- API entegrasyonu
- Otomatik hareket çekme
- Mükerrer kayıt engelleme

✅ **Eşleştirme Sistemi**
- Banka hareketlerini muhasebe kayıtlarıyla eşleştirme
- Otomatik eşleme kuralları
- Manuel eşleştirme
- Bekleyen işlemler takibi

✅ **Raporlama**
- Aylık/haftalık gelir-gider özeti
- Nakit akış raporu
- Cari ekstre raporu
- Banka hareket raporu
- Excel/CSV dışa aktarım

✅ **Yedekleme**
- Tek tık yedek alma
- Otomatik yedekleme
- Geri yükleme fonksiyonu

## Proje Yapısı

```
mini-muhasebe-app/
├── MiniMuhasebe.UI/              # Windows Forms UI
│   ├── Forms/
│   │   ├── LoginForm.cs
│   │   ├── MainDashboardForm.cs
│   │   ├── IncomeExpenseForm.cs
│   │   ├── CurrentAccountForm.cs
│   │   ├── BankAccountsForm.cs
│   │   ├── BankTransactionsForm.cs
│   │   ├── MatchingForm.cs
│   │   ├── ReportsForm.cs
│   │   └── SettingsForm.cs
│   ├── Program.cs
│   └── MiniMuhaseba.UI.csproj
│
├── MiniMuhasebe.Business/        # Business Logic
│   ├── Services/
│   │   ├── UserService.cs
│   │   ├── CompanyService.cs
│   │   ├── IncomeExpenseService.cs
│   │   ├── CurrentAccountService.cs
│   │   ├── BankService.cs
│   │   └── ReportService.cs
│   ├── Interfaces/
│   │   └── I[Service].cs
│   └── MiniMuhasebe.Business.csproj
│
├── MiniMuhasebe.Data/            # Data Access Layer
│   ├── Repositories/
│   │   ├── UserRepository.cs
│   │   ├── CompanyRepository.cs
│   │   ├── IncomeExpenseRepository.cs
│   │   ├── CurrentAccountRepository.cs
│   │   ├── BankRepository.cs
│   │   └── AuditLogRepository.cs
│   ├── Database/
│   │   ├── DbContext.cs
│   │   └── DatabaseInitializer.cs
│   └── MiniMuhasebe.Data.csproj
│
├── MiniMuhasebe.Integration/     # External API Integration
│   ├── BankingAPIs/
│   │   └── BankApiClient.cs
│   ├── Interfaces/
│   │   └── IBankApiProvider.cs
│   └── MiniMuhasebe.Integration.csproj
│
├── MiniMuhasebe.Models/          # Data Models
│   ├── User.cs
│   ├── Company.cs
│   ├── FiscalPeriod.cs
│   ├── IncomeExpenseTransaction.cs
│   ├── CurrentAccount.cs
│   ├── BankAccount.cs
│   ├── BankTransaction.cs
│   ├── TransactionMatch.cs
│   ├── AuditLog.cs
│   └── MiniMuhasebe.Models.csproj
│
├── Database/
│   ├── schema.sql              # Veritabanı şeması
│   └── seed-data.sql           # Örnek veri
│
├── Docs/
│   ├── INSTALLATION.md         # Kurulum talimatı
│   ├── USER_GUIDE.md           # Kullanıcı kılavuzu
│   └── API_INTEGRATION.md      # API entegrasyonu
│
└── .gitignore
```

## Kurulum

Detaylı kurulum talimatları için bkz: [INSTALLATION.md](Docs/INSTALLATION.md)

### Hızlı Başlangıç

1. Repository'i clone edin
2. Visual Studio'da `MiniMuhasebe.sln` açın
3. NuGet paketlerini geri yükleyin
4. `Database/schema.sql` ile SQLite veritabanını oluşturun
5. Uygulamayı çalıştırın

## Kullanıcı Bilgileri (İlk Kurulum)

- **Kullanıcı Adı:** admin
- **Şifre:** Admin123!

⚠️ İlk giriş sonrası şifreyi değiştirmeniz önerilir.

## Veritabanı Tabloları

- **Users** - Kullanıcı hesapları
- **Roles** - Roller (Yönetici, Standart Kullanıcı)
- **Companies** - Firma bilgileri
- **FiscalPeriods** - Mali dönemler
- **Accounts** - Hesap kategorileri
- **CurrentAccounts** - Cari kartlar (müşteri/tedarikçi)
- **CashTransactions** - Kasa hareketleri
- **BankAccounts** - Banka hesapları
- **BankTransactions** - Banka hareketleri (API'den)
- **IncomeExpenseTransactions** - Gelir-gider işlemleri
- **MatchingRules** - Otomatik eşleştirme kuralları
- **TransactionMatches** - İşlem eşleştirmeleri
- **AuditLogs** - Denetim günlüğü
- **AppSettings** - Uygulama ayarları

## Güvenlik Notları

✅ Parametreli SQL sorguları kullanılır (SQL Injection koruması)
✅ Şifreler hash + salt ile saklanır
✅ API anahtarları şifreli saklanır
✅ Tüm işlemler audit log'a kaydedilir
✅ Hassas veriler loglara yazılmaz

## Lisans

MIT License - Ayrıntılar için [LICENSE](LICENSE) dosyasına bakın.

## İletişim

Sorularınız ve önerileriniz için issue açabilirsiniz.
