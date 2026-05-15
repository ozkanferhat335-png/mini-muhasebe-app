using System;
using System.Collections.Generic;
using MiniMuhasebe.Data;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Services
{
    /// <summary>
    /// Hesap kategorisi işlemleri servisi
    /// </summary>
    public class AccountService
    {
        private readonly AccountRepository _accountRepository;
        private readonly Logger _logger;

        public AccountService(string connectionString)
        {
            _accountRepository = new AccountRepository(connectionString);
            _logger = new Logger();
        }

        /// <summary>
        /// Yeni hesap kategorisi oluştur
        /// </summary>
        public Account CreateAccount(int companyId, string accountName, string accountType,
            string accountCode = null, string description = null, int? parentAccountId = null)
        {
            try
            {
                var validTypes = new[] { "Income", "Expense", "Bank", "Cash", "CurrentAccount" };
                bool isValidType = false;
                foreach (var t in validTypes)
                    if (t == accountType) { isValidType = true; break; }

                if (!isValidType)
                    throw new ArgumentException($"Geçersiz hesap tipi: {accountType}. Geçerli tipler: Income, Expense, Bank, Cash, CurrentAccount");

                var newAccount = new Account
                {
                    CompanyId = companyId,
                    AccountName = accountName,
                    AccountType = accountType,
                    AccountCode = accountCode,
                    Description = description,
                    IsActive = true,
                    ParentAccountId = parentAccountId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                int accountId = _accountRepository.Add(newAccount);
                newAccount.AccountId = accountId;

                _logger.Info($"Yeni hesap oluşturuldu: {accountName} ({accountType}) (ID: {accountId})");
                return newAccount;
            }
            catch (Exception ex)
            {
                _logger.Error("Hesap oluşturma sırasında hata", ex);
                return null;
            }
        }

        /// <summary>
        /// Hesabı güncelle
        /// </summary>
        public bool UpdateAccount(Account account)
        {
            try
            {
                account.UpdatedAt = DateTime.Now;
                bool success = _accountRepository.Update(account);
                if (success)
                    _logger.Info($"Hesap güncellendi: {account.AccountName}");
                return success;
            }
            catch (Exception ex)
            {
                _logger.Error("Hesap güncelleme sırasında hata", ex);
                return false;
            }
        }

        /// <summary>
        /// Firmaya ait tüm hesapları getir
        /// </summary>
        public List<Account> GetAccountsByCompany(int companyId)
        {
            try
            {
                return _accountRepository.GetByCompanyId(companyId);
            }
            catch (Exception ex)
            {
                _logger.Error("Hesaplar alınırken hata", ex);
                return new List<Account>();
            }
        }

        /// <summary>
        /// Firmaya ait hesapları türe göre getir
        /// </summary>
        public List<Account> GetAccountsByCompanyAndType(int companyId, string accountType)
        {
            try
            {
                return _accountRepository.GetByCompanyIdAndType(companyId, accountType);
            }
            catch (Exception ex)
            {
                _logger.Error("Hesaplar alınırken hata", ex);
                return new List<Account>();
            }
        }

        /// <summary>
        /// ID'ye göre hesap getir
        /// </summary>
        public Account GetAccountById(int accountId)
        {
            try
            {
                return _accountRepository.GetById(accountId);
            }
            catch (Exception ex)
            {
                _logger.Error($"Hesap alınırken hata (ID: {accountId})", ex);
                return null;
            }
        }

        /// <summary>
        /// Hesabı sil (deaktif yap)
        /// </summary>
        public bool DeleteAccount(int accountId)
        {
            try
            {
                bool success = _accountRepository.Delete(accountId);
                if (success)
                    _logger.Info($"Hesap silindi: ID {accountId}");
                return success;
            }
            catch (Exception ex)
            {
                _logger.Error("Hesap silme sırasında hata", ex);
                return false;
            }
        }

        /// <summary>
        /// Firmaya varsayılan hesap kategorilerini oluştur
        /// </summary>
        public void CreateDefaultAccounts(int companyId)
        {
            try
            {
                var defaults = new[]
                {
                    ("Satış Gelirleri", "Income", "600"),
                    ("Hizmet Gelirleri", "Income", "601"),
                    ("Diğer Gelirler", "Income", "602"),
                    ("Personel Giderleri", "Expense", "700"),
                    ("Kira Giderleri", "Expense", "701"),
                    ("Elektrik/Su/Doğalgaz", "Expense", "702"),
                    ("Ofis Malzemeleri", "Expense", "703"),
                    ("Ulaşım Giderleri", "Expense", "704"),
                    ("Reklam Giderleri", "Expense", "705"),
                    ("Diğer Giderler", "Expense", "706"),
                    ("Kasa", "Cash", "100"),
                    ("Banka", "Bank", "102")
                };

                foreach (var (name, type, code) in defaults)
                {
                    CreateAccount(companyId, name, type, code);
                }

                _logger.Info($"Firma {companyId} için varsayılan hesaplar oluşturuldu.");
            }
            catch (Exception ex)
            {
                _logger.Error("Varsayılan hesaplar oluşturulurken hata", ex);
            }
        }
    }
}
