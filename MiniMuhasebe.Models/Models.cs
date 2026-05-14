using System;

namespace MiniMuhasebe.Models
{
    /// <summary>
    /// Kullanıcı modeli
    /// </summary>
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public int RoleId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Navigation Properties
        public virtual Role Role { get; set; }
    }

    /// <summary>
    /// Rol modeli
    /// </summary>
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Firma/İşletme modeli
    /// </summary>
    public class Company
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string TaxOffice { get; set; }
        public string TaxNumber { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Mali dönem modeli
    /// </summary>
    public class FiscalPeriod
    {
        public int PeriodId { get; set; }
        public int CompanyId { get; set; }
        public string PeriodName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsClosed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        public virtual Company Company { get; set; }
    }

    /// <summary>
    /// Hesap kategorisi modeli
    /// </summary>
    public class Account
    {
        public int AccountId { get; set; }
        public int CompanyId { get; set; }
        public string AccountName { get; set; }
        public string AccountType { get; set; } // Income, Expense, Bank, Cash, CurrentAccount
        public string AccountCode { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public int? ParentAccountId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        public virtual Company Company { get; set; }
        public virtual Account ParentAccount { get; set; }
    }

    /// <summary>
    /// Cari hesap (müşteri/tedarikçi) modeli
    /// </summary>
    public class CurrentAccount
    {
        public int CurrentAccountId { get; set; }
        public int CompanyId { get; set; }
        public string Title { get; set; }
        public string AccountType { get; set; } // Customer / Supplier
        public string TaxNumber { get; set; }
        public string TaxId { get; set; } // TCKN
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        public virtual Company Company { get; set; }
    }

    /// <summary>
    /// Banka hesabı modeli
    /// </summary>
    public class BankAccount
    {
        public int BankAccountId { get; set; }
        public int CompanyId { get; set; }
        public string BankName { get; set; }
        public string AccountName { get; set; }
        public string IBAN { get; set; }
        public string Currency { get; set; }
        public decimal InitialBalance { get; set; }
        public decimal CurrentBalance { get; set; }
        public bool IsApiEnabled { get; set; }
        public string ApiProviderType { get; set; } // REST, SOAP, OpenBanking
        public string ApiBaseUrl { get; set; }
        public string ApiClientId { get; set; }
        public string ApiClientSecret { get; set; } // Şifreli saklanır
        public string ApiKey { get; set; } // Şifreli saklanır
        public string ApiUsername { get; set; }
        public string ApiPassword { get; set; } // Şifreli saklanır
        public string ApiAccountId { get; set; }
        public DateTime? LastSyncAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        public virtual Company Company { get; set; }
    }

    /// <summary>
    /// Banka hareketi modeli (API'den gelen)
    /// </summary>
    public class BankTransaction
    {
        public int BankTransactionId { get; set; }
        public int BankAccountId { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public decimal? Balance { get; set; }
        public string ReferenceNumber { get; set; }
        public string BankTransactionId_External { get; set; } // Mükerrer kayıt engelleme
        public string TransactionType { get; set; } // Debit / Credit
        public string Status { get; set; } // Pending, Matched, Unmatched
        public bool IsMatched { get; set; }
        public DateTime? SyncedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public virtual BankAccount BankAccount { get; set; }
    }

    /// <summary>
    /// Gelir-gider işlemi modeli
    /// </summary>
    public class IncomeExpenseTransaction
    {
        public int TransactionId { get; set; }
        public int CompanyId { get; set; }
        public int PeriodId { get; set; }
        public int AccountId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string DocumentNumber { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public decimal VatRate { get; set; }
        public decimal VatAmount { get; set; }
        public decimal NetAmount { get; set; }
        public string PaymentType { get; set; } // Cash, Bank, CurrentAccount
        public int? BankAccountId { get; set; }
        public int? CurrentAccountId { get; set; }
        public int? BankTransactionId { get; set; }
        public string Notes { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        public virtual Company Company { get; set; }
        public virtual FiscalPeriod FiscalPeriod { get; set; }
        public virtual Account Account { get; set; }
        public virtual BankAccount BankAccount_Nav { get; set; }
        public virtual CurrentAccount CurrentAccount_Nav { get; set; }
        public virtual BankTransaction BankTransaction_Nav { get; set; }
    }

    /// <summary>
    /// Kasa hareketi modeli
    /// </summary>
    public class CashTransaction
    {
        public int CashTransactionId { get; set; }
        public int CompanyId { get; set; }
        public int PeriodId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } // Income, Expense
        public int? AccountId { get; set; }
        public int? CurrentAccountId { get; set; }
        public string Notes { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public virtual Company Company { get; set; }
        public virtual FiscalPeriod FiscalPeriod { get; set; }
    }

    /// <summary>
    /// Cari hesap hareketi modeli
    /// </summary>
    public class CurrentAccountTransaction
    {
        public int TransactionId { get; set; }
        public int CurrentAccountId { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } // Debit / Credit
        public string Description { get; set; }
        public string RelatedDocumentNumber { get; set; }
        public int? IncomeExpenseTransactionId { get; set; }
        public string Notes { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public virtual CurrentAccount CurrentAccount_Nav { get; set; }
    }

    /// <summary>
    /// İşlem eşleştirmesi modeli
    /// </summary>
    public class TransactionMatch
    {
        public int MatchId { get; set; }
        public int BankTransactionId { get; set; }
        public int IncomeExpenseTransactionId { get; set; }
        public decimal? MatchScore { get; set; }
        public string MatchType { get; set; } // Automatic, Manual
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public virtual BankTransaction BankTransaction_Nav { get; set; }
        public virtual IncomeExpenseTransaction IncomeExpenseTransaction_Nav { get; set; }
    }

    /// <summary>
    /// Otomatik eşleştirme kuralı modeli
    /// </summary>
    public class MatchingRule
    {
        public int RuleId { get; set; }
        public int CompanyId { get; set; }
        public string RuleName { get; set; }
        public decimal AmountTolerance { get; set; }
        public int DateTolerance { get; set; } // Gün cinsinden
        public string KeywordPatterns { get; set; } // JSON format
        public bool IsActive { get; set; }
        public int Priority { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        public virtual Company Company { get; set; }
    }

    /// <summary>
    /// Denetim günlüğü modeli
    /// </summary>
    public class AuditLog
    {
        public int AuditLogId { get; set; }
        public int? UserId { get; set; }
        public string Action { get; set; }
        public string TableName { get; set; }
        public int? RecordId { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public virtual User User { get; set; }
    }

    /// <summary>
    /// Uygulama ayarları modeli
    /// </summary>
    public class AppSetting
    {
        public int SettingId { get; set; }
        public string SettingKey { get; set; }
        public string SettingValue { get; set; }
        public string SettingType { get; set; } // String, Integer, Boolean, Decimal
        public string Description { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Yedekleme modeli
    /// </summary>
    public class Backup
    {
        public int BackupId { get; set; }
        public string BackupFileName { get; set; }
        public string BackupPath { get; set; }
        public long? BackupSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RestoredAt { get; set; }
    }
}
