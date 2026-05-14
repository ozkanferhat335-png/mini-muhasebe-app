-- Mini Muhasebe Uygulaması - SQLite Veritabanı Şeması
-- Özkan Ferhat - 2026-05-14

-- Roles Tablosu
CREATE TABLE IF NOT EXISTS Roles (
    RoleId INTEGER PRIMARY KEY AUTOINCREMENT,
    RoleName TEXT NOT NULL UNIQUE,
    Description TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Users Tablosu
CREATE TABLE IF NOT EXISTS Users (
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
);

-- Companies Tablosu
CREATE TABLE IF NOT EXISTS Companies (
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
);

-- FiscalPeriods Tablosu
CREATE TABLE IF NOT EXISTS FiscalPeriods (
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
);

-- Accounts (Hesap Kategorileri) Tablosu
CREATE TABLE IF NOT EXISTS Accounts (
    AccountId INTEGER PRIMARY KEY AUTOINCREMENT,
    CompanyId INTEGER NOT NULL,
    AccountName TEXT NOT NULL,
    AccountType TEXT NOT NULL, -- 'Income', 'Expense', 'Bank', 'Cash', 'CurrentAccount'
    AccountCode TEXT,
    Description TEXT,
    IsActive INTEGER DEFAULT 1,
    ParentAccountId INTEGER,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId),
    FOREIGN KEY (ParentAccountId) REFERENCES Accounts(AccountId)
);

-- CurrentAccounts (Cari Kartlar) Tablosu
CREATE TABLE IF NOT EXISTS CurrentAccounts (
    CurrentAccountId INTEGER PRIMARY KEY AUTOINCREMENT,
    CompanyId INTEGER NOT NULL,
    Title TEXT NOT NULL,
    AccountType TEXT NOT NULL, -- 'Customer' / 'Supplier'
    TaxNumber TEXT,
    TaxId TEXT, -- TCKN
    Phone TEXT,
    Email TEXT,
    Address TEXT,
    Notes TEXT,
    IsActive INTEGER DEFAULT 1,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId)
);

-- BankAccounts Tablosu
CREATE TABLE IF NOT EXISTS BankAccounts (
    BankAccountId INTEGER PRIMARY KEY AUTOINCREMENT,
    CompanyId INTEGER NOT NULL,
    BankName TEXT NOT NULL,
    AccountName TEXT,
    IBAN TEXT UNIQUE,
    Currency TEXT DEFAULT 'TRY',
    InitialBalance DECIMAL(15, 2) DEFAULT 0,
    CurrentBalance DECIMAL(15, 2) DEFAULT 0,
    IsApiEnabled INTEGER DEFAULT 0,
    ApiProviderType TEXT, -- 'REST', 'SOAP', 'OpenBanking'
    ApiBaseUrl TEXT,
    ApiClientId TEXT,
    ApiClientSecret TEXT, -- şifreli saklanır
    ApiKey TEXT, -- şifreli saklanır
    ApiUsername TEXT,
    ApiPassword TEXT, -- şifreli saklanır
    ApiAccountId TEXT, -- Banka tarafından verilen hesap ID
    LastSyncAt DATETIME,
    IsActive INTEGER DEFAULT 1,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId)
);

-- BankTransactions (API'den gelen hareketler) Tablosu
CREATE TABLE IF NOT EXISTS BankTransactions (
    BankTransactionId INTEGER PRIMARY KEY AUTOINCREMENT,
    BankAccountId INTEGER NOT NULL,
    TransactionDate DATE NOT NULL,
    Amount DECIMAL(15, 2) NOT NULL,
    Description TEXT,
    Balance DECIMAL(15, 2),
    ReferenceNumber TEXT,
    BankTransactionId_External TEXT UNIQUE, -- Banka tarafından verilen unique ID (mükerrer kayıt engelleme)
    TransactionType TEXT, -- 'Debit' / 'Credit'
    Status TEXT DEFAULT 'Pending', -- 'Pending', 'Matched', 'Unmatched'
    IsMatched INTEGER DEFAULT 0,
    SyncedAt DATETIME,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (BankAccountId) REFERENCES BankAccounts(BankAccountId)
);

-- IncomeExpenseTransactions Tablosu
CREATE TABLE IF NOT EXISTS IncomeExpenseTransactions (
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
    PaymentType TEXT NOT NULL, -- 'Cash', 'Bank', 'CurrentAccount'
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
);

-- CashTransactions Tablosu
CREATE TABLE IF NOT EXISTS CashTransactions (
    CashTransactionId INTEGER PRIMARY KEY AUTOINCREMENT,
    CompanyId INTEGER NOT NULL,
    PeriodId INTEGER NOT NULL,
    TransactionDate DATE NOT NULL,
    Description TEXT NOT NULL,
    Amount DECIMAL(15, 2) NOT NULL,
    TransactionType TEXT NOT NULL, -- 'Income', 'Expense'
    AccountId INTEGER,
    CurrentAccountId INTEGER,
    Notes TEXT,
    CreatedBy INTEGER,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId),
    FOREIGN KEY (PeriodId) REFERENCES FiscalPeriods(PeriodId),
    FOREIGN KEY (AccountId) REFERENCES Accounts(AccountId),
    FOREIGN KEY (CurrentAccountId) REFERENCES CurrentAccounts(CurrentAccountId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

-- CurrentAccountTransactions (Cari Hareket) Tablosu
CREATE TABLE IF NOT EXISTS CurrentAccountTransactions (
    TransactionId INTEGER PRIMARY KEY AUTOINCREMENT,
    CurrentAccountId INTEGER NOT NULL,
    TransactionDate DATE NOT NULL,
    Amount DECIMAL(15, 2) NOT NULL,
    TransactionType TEXT NOT NULL, -- 'Debit' / 'Credit'
    Description TEXT,
    RelatedDocumentNumber TEXT,
    IncomeExpenseTransactionId INTEGER,
    Notes TEXT,
    CreatedBy INTEGER,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CurrentAccountId) REFERENCES CurrentAccounts(CurrentAccountId),
    FOREIGN KEY (IncomeExpenseTransactionId) REFERENCES IncomeExpenseTransactions(TransactionId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

-- TransactionMatches (Eşleştirmeler) Tablosu
CREATE TABLE IF NOT EXISTS TransactionMatches (
    MatchId INTEGER PRIMARY KEY AUTOINCREMENT,
    BankTransactionId INTEGER NOT NULL,
    IncomeExpenseTransactionId INTEGER NOT NULL,
    MatchScore DECIMAL(5, 2), -- 0-100: Eşleştirme yüzdesini gösterir
    MatchType TEXT, -- 'Automatic', 'Manual'
    CreatedBy INTEGER,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (BankTransactionId) REFERENCES BankTransactions(BankTransactionId),
    FOREIGN KEY (IncomeExpenseTransactionId) REFERENCES IncomeExpenseTransactions(TransactionId),
    FOREIGN KEY (CreatedBy) REFERENCES Users(UserId),
    UNIQUE(BankTransactionId, IncomeExpenseTransactionId)
);

-- MatchingRules (Otomatik Eşleştirme Kuralları) Tablosu
CREATE TABLE IF NOT EXISTS MatchingRules (
    RuleId INTEGER PRIMARY KEY AUTOINCREMENT,
    CompanyId INTEGER NOT NULL,
    RuleName TEXT NOT NULL,
    AmountTolerance DECIMAL(10, 2) DEFAULT 0.01,
    DateTolerance INTEGER DEFAULT 3, -- Gün cinsinden
    KeywordPatterns TEXT, -- JSON format: ["pattern1", "pattern2"]
    IsActive INTEGER DEFAULT 1,
    Priority INTEGER DEFAULT 1,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId)
);

-- AuditLogs Tablosu
CREATE TABLE IF NOT EXISTS AuditLogs (
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
);

-- AppSettings Tablosu
CREATE TABLE IF NOT EXISTS AppSettings (
    SettingId INTEGER PRIMARY KEY AUTOINCREMENT,
    SettingKey TEXT NOT NULL UNIQUE,
    SettingValue TEXT,
    SettingType TEXT, -- 'String', 'Integer', 'Boolean', 'Decimal'
    Description TEXT,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Backups Tablosu
CREATE TABLE IF NOT EXISTS Backups (
    BackupId INTEGER PRIMARY KEY AUTOINCREMENT,
    BackupFileName TEXT NOT NULL,
    BackupPath TEXT NOT NULL,
    BackupSize INTEGER, -- Byte cinsinden
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    RestoredAt DATETIME
);

-- İndeksler (Performance iyileştirmesi)
CREATE INDEX idx_users_username ON Users(Username);
CREATE INDEX idx_companies_isactive ON Companies(IsActive);
CREATE INDEX idx_fiscalperiods_companyid ON FiscalPeriods(CompanyId);
CREATE INDEX idx_accounts_companyid ON Accounts(CompanyId);
CREATE INDEX idx_currentaccounts_companyid ON CurrentAccounts(CompanyId);
CREATE INDEX idx_bankaccounts_companyid ON BankAccounts(CompanyId);
CREATE INDEX idx_banktransactions_bankaccountid ON BankTransactions(BankAccountId);
CREATE INDEX idx_banktransactions_external_id ON BankTransactions(BankTransactionId_External);
CREATE INDEX idx_incomeexpense_companyid ON IncomeExpenseTransactions(CompanyId);
CREATE INDEX idx_incomeexpense_periodid ON IncomeExpenseTransactions(PeriodId);
CREATE INDEX idx_incomeexpense_transactiondate ON IncomeExpenseTransactions(TransactionDate);
CREATE INDEX idx_casht_companyid ON CashTransactions(CompanyId);
CREATE INDEX idx_currentaccountt_currentaccountid ON CurrentAccountTransactions(CurrentAccountId);
CREATE INDEX idx_auditlogs_userid ON AuditLogs(UserId);
CREATE INDEX idx_auditlogs_tablename ON AuditLogs(TableName);
CREATE INDEX idx_auditlogs_createdat ON AuditLogs(CreatedAt);
