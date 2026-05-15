using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace MiniMuhasebe.Data
{
    /// <summary>
    /// Veritabanını başlatıp tabloları oluşturan sınıf
    /// </summary>
    public class DatabaseInitializer
    {
        private readonly string _connectionString;
        private readonly string _databasePath;

        public DatabaseInitializer(string connectionString)
        {
            _connectionString = connectionString;
            // Bağlantı stringinden veritabanı yolunu çıkar
            _databasePath = ExtractDatabasePath(connectionString);
        }

        /// <summary>
        /// Bağlantı stringinden veritabanı yolunu çıkartır
        /// </summary>
        private string ExtractDatabasePath(string connectionString)
        {
            try
            {
                var builder = new SQLiteConnectionStringBuilder(connectionString);
                return builder.DataSource;
            }
            catch
            {
                // Fallback: basit string parsing
                var parts = connectionString.Split(';');
                foreach (var part in parts)
                {
                    if (part.StartsWith("Data Source="))
                    {
                        return part.Replace("Data Source=", "").Trim();
                    }
                }
                return "MiniMuhasebe.db";
            }
        }

        /// <summary>
        /// Veritabanını başlatır, tabloları oluşturur ve başlangıç verilerini yükler
        /// </summary>
        public void Initialize()
        {
            try
            {
                // Veritabanı dosyasının klasörünü oluştur
                var directory = Path.GetDirectoryName(_databasePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    // Tabloları oluştur
                    CreateTables(connection);

                    // Başlangıç verilerini yükle (sadece boşsa)
                    SeedInitialData(connection);

                    connection.Close();
                }

                Console.WriteLine("✓ Veritabanı başarıyla başlatıldı.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Veritabanı başlatılırken hata oluştu: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Tüm tabloları oluşturur
        /// </summary>
        private void CreateTables(SQLiteConnection connection)
        {
            var commands = new List<string>
            {
                // Roles Tablosu
                @"CREATE TABLE IF NOT EXISTS Roles (
                    RoleId INTEGER PRIMARY KEY AUTOINCREMENT,
                    RoleName TEXT NOT NULL UNIQUE,
                    Description TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );",

                // Users Tablosu
                @"CREATE TABLE IF NOT EXISTS Users (
                    UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    Email TEXT,
                    PasswordHash TEXT NOT NULL,
                    PasswordSalt TEXT NOT NULL,
                    RoleId INTEGER NOT NULL,
                    IsActive INTEGER DEFAULT 1,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    LastLoginAt DATETIME,
                    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
                );",

                // Companies Tablosu
                @"CREATE TABLE IF NOT EXISTS Companies (
                    CompanyId INTEGER PRIMARY KEY AUTOINCREMENT,
                    CompanyName TEXT NOT NULL,
                    TaxOffice TEXT,
                    TaxNumber TEXT UNIQUE,
                    Phone TEXT,
                    Email TEXT,
                    Address TEXT,
                    IsActive INTEGER DEFAULT 1,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );",

                // FiscalPeriods Tablosu
                @"CREATE TABLE IF NOT EXISTS FiscalPeriods (
                    PeriodId INTEGER PRIMARY KEY AUTOINCREMENT,
                    CompanyId INTEGER NOT NULL,
                    PeriodName TEXT NOT NULL,
                    StartDate DATE NOT NULL,
                    EndDate DATE NOT NULL,
                    IsClosed INTEGER DEFAULT 0,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId),
                    UNIQUE(CompanyId, StartDate, EndDate)
                );",

                // Accounts Tablosu
                @"CREATE TABLE IF NOT EXISTS Accounts (
                    AccountId INTEGER PRIMARY KEY AUTOINCREMENT,
                    CompanyId INTEGER NOT NULL,
                    AccountName TEXT NOT NULL,
                    AccountType TEXT NOT NULL,
                    AccountCode TEXT,
                    Description TEXT,
                    IsActive INTEGER DEFAULT 1,
                    ParentAccountId INTEGER,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId),
                    FOREIGN KEY (ParentAccountId) REFERENCES Accounts(AccountId)
                );",

                // CurrentAccounts Tablosu
                @"CREATE TABLE IF NOT EXISTS CurrentAccounts (
                    CurrentAccountId INTEGER PRIMARY KEY AUTOINCREMENT,
                    CompanyId INTEGER NOT NULL,
                    Title TEXT NOT NULL,
                    AccountType TEXT NOT NULL,
                    TaxNumber TEXT,
                    TaxId TEXT,
                    Phone TEXT,
                    Email TEXT,
                    Address TEXT,
                    Notes TEXT,
                    IsActive INTEGER DEFAULT 1,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId)
                );",

                // BankAccounts Tablosu
                @"CREATE TABLE IF NOT EXISTS BankAccounts (
                    BankAccountId INTEGER PRIMARY KEY AUTOINCREMENT,
                    CompanyId INTEGER NOT NULL,
                    BankName TEXT NOT NULL,
                    AccountName TEXT,
                    IBAN TEXT UNIQUE,
                    Currency TEXT DEFAULT 'TRY',
                    InitialBalance DECIMAL(15, 2) DEFAULT 0,
                    CurrentBalance DECIMAL(15, 2) DEFAULT 0,
                    IsApiEnabled INTEGER DEFAULT 0,
                    ApiProviderType TEXT,
                    ApiBaseUrl TEXT,
                    ApiClientId TEXT,
                    ApiClientSecret TEXT,
                    ApiKey TEXT,
                    ApiUsername TEXT,
                    ApiPassword TEXT,
                    ApiAccountId TEXT,
                    LastSyncAt DATETIME,
                    IsActive INTEGER DEFAULT 1,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId)
                );",

                // BankTransactions Tablosu
                @"CREATE TABLE IF NOT EXISTS BankTransactions (
                    BankTransactionId INTEGER PRIMARY KEY AUTOINCREMENT,
                    BankAccountId INTEGER NOT NULL,
                    TransactionDate DATE NOT NULL,
                    Amount DECIMAL(15, 2) NOT NULL,
                    Description TEXT,
                    Balance DECIMAL(15, 2),
                    ReferenceNumber TEXT,
                    BankTransactionId_External TEXT UNIQUE,
                    TransactionType TEXT,
                    Status TEXT DEFAULT 'Pending',
                    IsMatched INTEGER DEFAULT 0,
                    SyncedAt DATETIME,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (BankAccountId) REFERENCES BankAccounts(BankAccountId)
                );",

                // IncomeExpenseTransactions Tablosu
                @"CREATE TABLE IF NOT EXISTS IncomeExpenseTransactions (
                    TransactionId INTEGER PRIMARY KEY AUTOINCREMENT,
                    CompanyId INTEGER NOT NULL,
                    PeriodId INTEGER NOT NULL,
                    AccountId INTEGER NOT NULL,
                    TransactionDate DATE NOT NULL,
                    DocumentNumber TEXT,
                    Description TEXT NOT NULL,
                    Amount DECIMAL(15, 2) NOT NULL,
                    VatRate DECIMAL(5, 2) DEFAULT 0,
                    VatAmount DECIMAL(15, 2) DEFAULT 0,
                    NetAmount DECIMAL(15, 2),
                    PaymentType TEXT NOT NULL,
                    BankAccountId INTEGER,
                    CurrentAccountId INTEGER,
                    BankTransactionId INTEGER,
                    Notes TEXT,
                    CreatedBy INTEGER,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedBy INTEGER,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId),
                    FOREIGN KEY (PeriodId) REFERENCES FiscalPeriods(PeriodId),
                    FOREIGN KEY (AccountId) REFERENCES Accounts(AccountId),
                    FOREIGN KEY (BankAccountId) REFERENCES BankAccounts(BankAccountId),
                    FOREIGN KEY (CurrentAccountId) REFERENCES CurrentAccounts(CurrentAccountId),
                    FOREIGN KEY (BankTransactionId) REFERENCES BankTransactions(BankTransactionId),
                    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId),
                    FOREIGN KEY (UpdatedBy) REFERENCES Users(UserId)
                );",

                // CashTransactions Tablosu
                @"CREATE TABLE IF NOT EXISTS CashTransactions (
                    CashTransactionId INTEGER PRIMARY KEY AUTOINCREMENT,
                    CompanyId INTEGER NOT NULL,
                    PeriodId INTEGER NOT NULL,
                    TransactionDate DATE NOT NULL,
                    Description TEXT NOT NULL,
                    Amount DECIMAL(15, 2) NOT NULL,
                    TransactionType TEXT NOT NULL,
                    AccountId INTEGER,
                    CurrentAccountId INTEGER,
                    Notes TEXT,
                    CreatedBy INTEGER,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId),
                    FOREIGN KEY (PeriodId) REFERENCES FiscalPeriods(PeriodId),
                    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
                );",

                // CurrentAccountTransactions Tablosu
                @"CREATE TABLE IF NOT EXISTS CurrentAccountTransactions (
                    TransactionId INTEGER PRIMARY KEY AUTOINCREMENT,
                    CurrentAccountId INTEGER NOT NULL,
                    TransactionDate DATE NOT NULL,
                    Amount DECIMAL(15, 2) NOT NULL,
                    TransactionType TEXT NOT NULL,
                    Description TEXT,
                    RelatedDocumentNumber TEXT,
                    IncomeExpenseTransactionId INTEGER,
                    Notes TEXT,
                    CreatedBy INTEGER,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (CurrentAccountId) REFERENCES CurrentAccounts(CurrentAccountId),
                    FOREIGN KEY (IncomeExpenseTransactionId) REFERENCES IncomeExpenseTransactions(TransactionId),
                    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
                );",

                // TransactionMatches Tablosu
                @"CREATE TABLE IF NOT EXISTS TransactionMatches (
                    MatchId INTEGER PRIMARY KEY AUTOINCREMENT,
                    BankTransactionId INTEGER NOT NULL,
                    IncomeExpenseTransactionId INTEGER NOT NULL,
                    MatchScore DECIMAL(5, 2),
                    MatchType TEXT,
                    CreatedBy INTEGER,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (BankTransactionId) REFERENCES BankTransactions(BankTransactionId),
                    FOREIGN KEY (IncomeExpenseTransactionId) REFERENCES IncomeExpenseTransactions(TransactionId),
                    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId),
                    UNIQUE(BankTransactionId, IncomeExpenseTransactionId)
                );",

                // MatchingRules Tablosu
                @"CREATE TABLE IF NOT EXISTS MatchingRules (
                    RuleId INTEGER PRIMARY KEY AUTOINCREMENT,
                    CompanyId INTEGER NOT NULL,
                    RuleName TEXT NOT NULL,
                    AmountTolerance DECIMAL(10, 2) DEFAULT 0.01,
                    DateTolerance INTEGER DEFAULT 3,
                    KeywordPatterns TEXT,
                    IsActive INTEGER DEFAULT 1,
                    Priority INTEGER DEFAULT 1,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId)
                );",

                // AuditLogs Tablosu
                @"CREATE TABLE IF NOT EXISTS AuditLogs (
                    AuditLogId INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER,
                    Action TEXT NOT NULL,
                    TableName TEXT NOT NULL,
                    RecordId INTEGER,
                    OldValue TEXT,
                    NewValue TEXT,
                    IpAddress TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (UserId) REFERENCES Users(UserId)
                );",

                // AppSettings Tablosu
                @"CREATE TABLE IF NOT EXISTS AppSettings (
                    SettingId INTEGER PRIMARY KEY AUTOINCREMENT,
                    SettingKey TEXT NOT NULL UNIQUE,
                    SettingValue TEXT,
                    SettingType TEXT,
                    Description TEXT,
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );",

                // Backups Tablosu
                @"CREATE TABLE IF NOT EXISTS Backups (
                    BackupId INTEGER PRIMARY KEY AUTOINCREMENT,
                    BackupFileName TEXT NOT NULL,
                    BackupPath TEXT NOT NULL,
                    BackupSize INTEGER,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    RestoredAt DATETIME
                );",

                // İndeksler
                "CREATE INDEX IF NOT EXISTS idx_users_username ON Users(Username);",
                "CREATE INDEX IF NOT EXISTS idx_companies_isactive ON Companies(IsActive);",
                "CREATE INDEX IF NOT EXISTS idx_fiscalperiods_companyid ON FiscalPeriods(CompanyId);",
                "CREATE INDEX IF NOT EXISTS idx_banktransactions_bankaccountid ON BankTransactions(BankAccountId);",
                "CREATE INDEX IF NOT EXISTS idx_banktransactions_external_id ON BankTransactions(BankTransactionId_External);",
                "CREATE INDEX IF NOT EXISTS idx_incomeexpense_companyid ON IncomeExpenseTransactions(CompanyId);",
                "CREATE INDEX IF NOT EXISTS idx_incomeexpense_periodid ON IncomeExpenseTransactions(PeriodId);",
                "CREATE INDEX IF NOT EXISTS idx_incomeexpense_transactiondate ON IncomeExpenseTransactions(TransactionDate);",
                "CREATE INDEX IF NOT EXISTS idx_auditlogs_userid ON AuditLogs(UserId);",
                "CREATE INDEX IF NOT EXISTS idx_auditlogs_tablename ON AuditLogs(TableName);",
                "CREATE INDEX IF NOT EXISTS idx_auditlogs_createdat ON AuditLogs(CreatedAt);"
            };

            foreach (var command in commands)
            {
                using (var cmd = new SQLiteCommand(command, connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Başlangıç verilerini yükler (boş veritabanında)
        /// </summary>
        private void SeedInitialData(SQLiteConnection connection)
        {
            // Roller zaten varsa yükleme yapma
            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Roles;", connection))
            {
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count > 0) return; // Veriler zaten var
            }

            // Admin kullanıcısı için gerçek hash oluştur
            var (adminHash, adminSalt) = PasswordHelper.HashPassword("Admin123!");

            var seedCommands = new List<string>
            {
                // Rolleri Ekle
                "INSERT INTO Roles (RoleName, Description) VALUES ('Admin', 'Yönetici - Tüm izinlere sahip');",
                "INSERT INTO Roles (RoleName, Description) VALUES ('User', 'Standart Kullanıcı - Okuma ve temel yazma izni');",

                // Admin Kullanıcısı (username: admin, password: Admin123!)
                $@"INSERT INTO Users (Username, Email, PasswordHash, PasswordSalt, RoleId, IsActive) 
                  VALUES ('admin', 'admin@minimuhasebe.local', '{adminHash}', '{adminSalt}', 1, 1);",

                // Başlangıç Ayarları
                @"INSERT INTO AppSettings (SettingKey, SettingValue, SettingType, Description) VALUES
                  ('DefaultCurrency', 'TRY', 'String', 'Varsayılan Para Birimi'),
                  ('DateFormat', 'dd.MM.yyyy', 'String', 'Tarih Formatı'),
                  ('DecimalSeparator', ',', 'String', 'Ondalık Ayracı'),
                  ('ThousandsSeparator', '.', 'String', 'Binler Ayracı'),
                  ('VatRates', '[0, 1, 8, 18]', 'String', 'KDV Oranları'),
                  ('AmountTolerance', '0.01', 'Decimal', 'Eşleştirme Toleransı'),
                  ('DateTolerance', '3', 'Integer', 'Gün Cinsinden Tarih Toleransı'),
                  ('AutoBackupEnabled', '1', 'Boolean', 'Otomatik Yedekleme Aktif'),
                  ('AutoBackupTime', '23:00', 'String', 'Otomatik Yedekleme Saati'),
                  ('MaxBackupCount', '10', 'Integer', 'En Fazla Yedek Sayısı'),
                  ('LogLevel', 'Info', 'String', 'Log Seviyesi');"
            };

            foreach (var command in seedCommands)
            {
                try
                {
                    using (var cmd = new SQLiteCommand(command, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Seed data yüklenirken uyarı: {ex.Message}");
                    // Devam et, kritik hata değilse
                }
            }
        }

        /// <summary>
        /// Veritabanı varlığını kontrol eder
        /// </summary>
        public bool DatabaseExists()
        {
            return File.Exists(_databasePath);
        }

        /// <summary>
        /// Veritabanını siler (test için)
        /// </summary>
        public void DeleteDatabase()
        {
            if (DatabaseExists())
            {
                try
                {
                    File.Delete(_databasePath);
                    Console.WriteLine("Veritabanı silindi.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Veritabanı silinirken hata: {ex.Message}");
                }
            }
        }
    }
}
