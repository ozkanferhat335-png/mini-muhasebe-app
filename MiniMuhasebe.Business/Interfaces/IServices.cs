using System;
using System.Collections.Generic;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Interfaces
{
    /// <summary>
    /// Kullanıcı servisi arayüzü
    /// </summary>
    public interface IUserService
    {
        User Login(string username, string password);
        bool ChangePassword(int userId, string oldPassword, string newPassword);
        User CreateUser(string username, string email, string password, int roleId);
        List<User> GetAllUsers();
        User GetUserById(int userId);
    }

    /// <summary>
    /// Firma servisi arayüzü
    /// </summary>
    public interface ICompanyService
    {
        Company CreateCompany(string companyName, string taxOffice, string taxNumber, string phone, string email, string address);
        bool UpdateCompany(Company company);
        List<Company> GetAllCompanies();
        Company GetCompanyById(int companyId);
        bool DeleteCompany(int companyId);
    }

    /// <summary>
    /// Mali dönem servisi arayüzü
    /// </summary>
    public interface IFiscalPeriodService
    {
        FiscalPeriod CreatePeriod(int companyId, string periodName, DateTime startDate, DateTime endDate);
        bool UpdatePeriod(FiscalPeriod period);
        bool ClosePeriod(int periodId);
        List<FiscalPeriod> GetPeriodsByCompany(int companyId);
        List<FiscalPeriod> GetOpenPeriodsByCompany(int companyId);
        FiscalPeriod GetCurrentPeriod(int companyId);
        FiscalPeriod GetPeriodById(int periodId);
        bool DeletePeriod(int periodId);
    }

    /// <summary>
    /// Hesap kategorisi servisi arayüzü
    /// </summary>
    public interface IAccountService
    {
        Account CreateAccount(int companyId, string accountName, string accountType,
            string accountCode = null, string description = null, int? parentAccountId = null);
        bool UpdateAccount(Account account);
        List<Account> GetAccountsByCompany(int companyId);
        List<Account> GetAccountsByCompanyAndType(int companyId, string accountType);
        Account GetAccountById(int accountId);
        bool DeleteAccount(int accountId);
        void CreateDefaultAccounts(int companyId);
    }

    /// <summary>
    /// Gelir-gider servisi arayüzü
    /// </summary>
    public interface IIncomeExpenseService
    {
        IncomeExpenseTransaction CreateTransaction(int companyId, int periodId, int accountId, DateTime transactionDate,
            string description, decimal amount, decimal vatRate, string paymentType, int? bankAccountId = null,
            int? currentAccountId = null, int? createdBy = null, string documentNumber = null, string notes = null);
        bool UpdateTransaction(IncomeExpenseTransaction transaction, int? updatedBy = null);
        List<IncomeExpenseTransaction> GetTransactionsByPeriod(int periodId);
        List<IncomeExpenseTransaction> GetTransactionsByDateRange(int companyId, DateTime startDate, DateTime endDate);
        bool DeleteTransaction(int transactionId);
        decimal GetTotalByAccountAndPeriod(int accountId, int periodId);
    }

    /// <summary>
    /// Cari hesap servisi arayüzü
    /// </summary>
    public interface ICurrentAccountService
    {
        CurrentAccount CreateCurrentAccount(int companyId, string title, string accountType,
            string taxNumber = null, string taxId = null, string phone = null, string email = null, string address = null);
        bool UpdateCurrentAccount(CurrentAccount account);
        List<CurrentAccount> GetAccountsByCompany(int companyId);
        List<CurrentAccount> GetAccountsByCompanyAndType(int companyId, string accountType);
        CurrentAccount GetAccountById(int accountId);
        bool DeleteCurrentAccount(int accountId);
    }

    /// <summary>
    /// Banka servisi arayüzü
    /// </summary>
    public interface IBankService
    {
        BankAccount CreateBankAccount(int companyId, string bankName, string accountName, string iban,
            string currency, decimal initialBalance, bool isApiEnabled,
            string apiProviderType, string apiBaseUrl, string apiClientId,
            string apiClientSecret, string apiKey, string apiUsername,
            string apiPassword, string apiAccountId);
        bool UpdateBankAccount(BankAccount account);
        List<BankAccount> GetAccountsByCompany(int companyId);
        BankAccount GetAccountById(int accountId);
        BankTransaction AddBankTransaction(int bankAccountId, BankTransaction transaction);
        List<BankTransaction> GetTransactionsByDateRange(int bankAccountId, DateTime startDate, DateTime endDate);
        List<BankTransaction> GetUnmatchedTransactions(int bankAccountId);
        bool DeleteBankAccount(int accountId);
        bool UpdateBankBalance(int bankAccountId, decimal newBalance);
    }

    /// <summary>
    /// Eşleştirme servisi arayüzü
    /// </summary>
    public interface IMatchingService
    {
        TransactionMatch ManualMatch(int bankTransactionId, int incomeExpenseTransactionId, int? createdBy = null);
        bool RemoveMatch(int matchId);
        AutoMatchResult AutoMatch(int companyId, int bankAccountId, int? createdBy = null);
        MatchingRule CreateMatchingRule(int companyId, string ruleName,
            decimal amountTolerance, int dateTolerance, string keywordPatterns, int priority);
        List<MatchingRule> GetMatchingRules(int companyId);
        TransactionMatch GetMatchByBankTransaction(int bankTransactionId);
    }

    /// <summary>
    /// Yedekleme servisi arayüzü
    /// </summary>
    public interface IBackupService
    {
        Backup CreateBackup(string customPath = null);
        bool RestoreBackup(int backupId);
        bool RestoreBackupFromFile(string backupFilePath);
        List<Backup> GetAllBackups();
        Backup GetLatestBackup();
        bool DeleteBackup(int backupId, bool deleteFile = true);
    }

    /// <summary>
    /// Audit log servisi arayüzü
    /// </summary>
    public interface IAuditLogService
    {
        AuditLog Log(string action, string tableName, int? recordId, int? userId,
            string oldValue, string newValue, string ipAddress);
        void LogLogin(int userId, string username, bool success);
        void LogLogout(int userId, string username);
        void LogInsert(string tableName, int recordId, int? userId, string newValue);
        void LogUpdate(string tableName, int recordId, int? userId, string oldValue, string newValue);
        void LogDelete(string tableName, int recordId, int? userId, string oldValue);
        List<AuditLog> GetAllLogs();
        List<AuditLog> GetLogsByUser(int userId);
        List<AuditLog> GetLogsByTable(string tableName);
        List<AuditLog> GetLogsByDateRange(DateTime startDate, DateTime endDate);
    }
}
