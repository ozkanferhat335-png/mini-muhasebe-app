-- Mini Muhasebe Uygulaması - Örnek Veriler
-- Özkan Ferhat - 2026-05-14

-- Rolleri Ekle
INSERT OR IGNORE INTO Roles (RoleId, RoleName, Description) VALUES
(1, 'Admin', 'Yönetici - Tüm izinlere sahip'),
(2, 'User', 'Standart Kullanıcı - Okuma ve temel yazma izni');

-- Admin Kullanıcısı Ekle (Varsayılan)
-- Username: admin, Password: Admin123!
-- Password Hash ve Salt (PBKDF2)
INSERT OR IGNORE INTO Users (UserId, Username, Email, PasswordHash, PasswordSalt, RoleId, IsActive) VALUES
(1, 'admin', 'admin@minimuhasebe.local', 
    '2A9E9C7F9D8B1E5C5F5E5D5C5B5A59585756555453525150', -- Hash
    '1A2B3C4D5E6F7A8B9C0D1E2F3A4B5C6D', -- Salt
    1, 1);

-- Test İşlet (Firma) Ekle
INSERT OR IGNORE INTO Companies (CompanyId, CompanyName, TaxOffice, TaxNumber, Phone, Email) VALUES
(1, 'Örnek İşlet A.Ş.', 'Göktuğ Vergi Müdürlüğü', '1234567890', '0212 123 45 67', 'info@ornekislet.com'),
(2, 'Test Öğrenci Projesi Ltd.Şı.', 'Beyazit Vergi Müdürlüğü', '9876543210', '0216 987 65 43', 'test@project.local');

-- Mali Dönemleri Ekle
INSERT OR IGNORE INTO FiscalPeriods (PeriodId, CompanyId, PeriodName, StartDate, EndDate, IsClosed) VALUES
(1, 1, '2026 - Ocak', '2026-01-01', '2026-01-31', 0),
(2, 1, '2026 - Şubat', '2026-02-01', '2026-02-28', 0),
(3, 1, '2026 - Mart', '2026-03-01', '2026-03-31', 0),
(4, 2, '2026 - Ocak', '2026-01-01', '2026-01-31', 0);

-- Hesap Kategorileri Ekle
INSERT OR IGNORE INTO Accounts (AccountId, CompanyId, AccountName, AccountType, AccountCode, Description) VALUES
-- İşlet 1 Hesapları
(1, 1, 'Satış Gelirleri', 'Income', '4001', 'Ana ürün satış gelirleri'),
(2, 1, 'Hizmet Gelirleri', 'Income', '4002', 'Hizmet sunumu gelirleri'),
(3, 1, 'Diğer Gelirler', 'Income', '4009', 'Faiz, kur farkı vb.'),
(4, 1, 'Gida Giderleri', 'Expense', '5001', 'Gıda ve içki giderleri'),
(5, 1, 'Eléktrik Giderleri', 'Expense', '5002', 'Elektrik ve enerji giderleri'),
(6, 1, 'Kiralama Giderleri', 'Expense', '5003', 'Ofis/dükçan kiralaması'),
(7, 1, 'Personel Giderleri', 'Expense', '5004', 'Maaş ve ödemenekler'),
(8, 1, 'Vergi ve Harç', 'Expense', '5005', 'Vergi, harç ve resim'),
(9, 1, 'Ana Banka Hesabı', 'Bank', '1001', 'Birinci Banka Hesabı'),
(10, 1, 'Kasa', 'Cash', '1000', 'Kasa ve el nakitleri'),
(11, 1, 'Muşteriler', 'CurrentAccount', '1200', 'Müşteri cari hesapları'),
(12, 1, 'Tedarikçiler', 'CurrentAccount', '2000', 'Tedarikçi cari hesapları'),
-- İşlet 2 Hesapları
(13, 2, 'Satış Gelirleri', 'Income', '4001', 'Satış Geliri'),
(14, 2, 'Giderleri', 'Expense', '5001', 'Operasyonel Giderler'),
(15, 2, 'Ana Banka', 'Bank', '1001', 'Banka Hesabı'),
(16, 2, 'Kasa', 'Cash', '1000', 'Kasa');

-- Test Cari Kartları Ekle
INSERT OR IGNORE INTO CurrentAccounts (CurrentAccountId, CompanyId, Title, AccountType, TaxNumber, Phone, Email) VALUES
(1, 1, 'Ahmet Müşteri Ltd.Şı.', 'Customer', '1234567890', '0532 123 45 67', 'ahmet@musteri.com'),
(2, 1, 'Fatma Tedarikçi A.Ş.', 'Supplier', '9876543210', '0533 987 65 43', 'fatma@tedarikci.com'),
(3, 1, 'Mehmet Müşteri', 'Customer', NULL, '0534 555 66 77', 'mehmet@example.com'),
(4, 2, 'Test Satıcısı', 'Customer', '5555555555', '0535 111 22 33', 'satici@test.com');

-- Banka Hesapları Ekle
INSERT OR IGNORE INTO BankAccounts (BankAccountId, CompanyId, BankName, AccountName, IBAN, Currency, InitialBalance, CurrentBalance) VALUES
(1, 1, 'Birinci Banka', 'Ticari Hesap', 'TR12 0001 1111 1111 1111 1111 11', 'TRY', 50000.00, 50000.00),
(2, 1, 'İkinci Banka', 'Maaş Hesabı', 'TR12 0002 2222 2222 2222 2222 22', 'TRY', 25000.00, 25000.00),
(3, 2, 'Test Banka', 'İşletme Hesabı', 'TR12 0003 3333 3333 3333 3333 33', 'TRY', 10000.00, 10000.00);

-- Örnek Gelir-Gider Hareketleri
INSERT OR IGNORE INTO IncomeExpenseTransactions 
(CompanyId, PeriodId, AccountId, TransactionDate, DocumentNumber, Description, Amount, VatRate, VatAmount, NetAmount, PaymentType, BankAccountId, Notes, CreatedBy) VALUES
-- İşlet 1
(1, 1, 1, '2026-05-05', 'FAT-001', 'Ürün Satışı', 1000.00, 18, 180.00, 820.00, 'Bank', 1, 'Ahmet Müşteri', 1),
(1, 1, 2, '2026-05-10', 'FAT-002', 'Hizmet Gelirleri', 500.00, 18, 90.00, 410.00, 'Bank', 1, 'Danışmanlık Hizmetı', 1),
(1, 1, 4, '2026-05-08', 'HRC-001', 'Yemek Harcaması', 200.00, 0, 0.00, 200.00, 'Cash', NULL, 'Ofis Yemeği', 1),
(1, 1, 5, '2026-05-12', 'HRC-002', 'Elektrik Faturası', 350.00, 0, 0.00, 350.00, 'Bank', 1, 'Aylık Elektrik', 1),
(1, 1, 6, '2026-05-01', 'HRC-003', 'Ofis Kiralatı', 2000.00, 0, 0.00, 2000.00, 'Bank', 1, 'Aylık Kira Ödemesi', 1),
-- İşlet 2
(2, 4, 13, '2026-05-06', 'INV-001', 'Test Satış', 300.00, 18, 54.00, 246.00, 'Bank', 3, 'Deneme', 1);

-- Örnek Banka Hareketleri
INSERT OR IGNORE INTO BankTransactions 
(BankAccountId, TransactionDate, Amount, Description, Balance, ReferenceNumber, BankTransactionId_External, TransactionType, Status) VALUES
(1, '2026-05-05', 1000.00, 'TRANSFER GELEN - Ahmet Müşteri Kodu: 001', 51000.00, 'REF001', 'BNK001', 'Credit', 'Matched'),
(1, '2026-05-08', -200.00, 'HARCAMA - Yemek Ücreti', 50800.00, 'REF002', 'BNK002', 'Debit', 'Unmatched'),
(1, '2026-05-10', 500.00, 'TRANSFER GELEN - Hizmet Bedeli', 51300.00, 'REF003', 'BNK003', 'Credit', 'Matched'),
(1, '2026-05-12', -350.00, 'Elektrik Şirketi Ödeme', 50950.00, 'REF004', 'BNK004', 'Debit', 'Matched'),
(2, '2026-05-01', -2000.00, 'KIRA ÖDEMESİ - Mal Sahibi Adı', 23000.00, 'REF005', 'BNK005', 'Debit', 'Matched');

-- Örnek Cari Hareketleri
INSERT OR IGNORE INTO CurrentAccountTransactions 
(CurrentAccountId, TransactionDate, Amount, TransactionType, Description, RelatedDocumentNumber) VALUES
(1, '2026-05-05', 1000.00, 'Debit', 'Fatura FAT-001 Alındı', 'FAT-001'),
(2, '2026-05-08', 500.00, 'Credit', 'Malzeme Alımı', 'SIP-001'),
(3, '2026-05-15', 250.00, 'Debit', 'Peşin Satış', 'KBZ-001');

-- Örnek Ayarları Ekle
INSERT OR IGNORE INTO AppSettings (SettingKey, SettingValue, SettingType, Description) VALUES
('DefaultCurrency', 'TRY', 'String', 'Varsayılan Para Birimi'),
('DateFormat', 'dd.MM.yyyy', 'String', 'Tarih Formatı'),
('DecimalSeparator', ',', 'String', 'Ondalık Ayraçı'),
('ThousandsSeparator', '.', 'String', 'Binler Ayraçı'),
('VatRates', '[0, 1, 8, 18]', 'String', 'KDV Oranları'),
('AmountTolerance', '0.01', 'Decimal', 'Eşleştirme Toleransı'),
('DateTolerance', '3', 'Integer', 'Gün Cinsinden Tarih Toleransı'),
('AutoBackupEnabled', '1', 'Boolean', 'Otomatik Yedekleme Aktif'),
('AutoBackupTime', '23:00', 'String', 'Otomatik Yedekleme Saati'),
('MaxBackupCount', '10', 'Integer', 'En Fazla Yedek Sayısı'),
('LogLevel', 'Info', 'String', 'Log Seviyesi');
