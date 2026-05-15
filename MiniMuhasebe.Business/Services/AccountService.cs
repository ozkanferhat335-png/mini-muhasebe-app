using System;
using System.Collections.Generic;
using MiniMuhasebe.Data;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Services
{
    public class AccountService
    {
        private readonly AccountRepository _accountRepository;
        private readonly Logger _logger;

        public AccountService(string connectionString)
        {
            _accountRepository = new AccountRepository(connectionString);
            _logger = new Logger();
        }

        public Account CreateAccount(int companyId, string accountName, string accountType,
            string accountCode = null, string description = null, int? parentAccountId = null)
        {
            try
            {
                var account = new Account
                {
                    CompanyId = companyId,
                    AccountName = accountName,
                    AccountType = accountType,
                    AccountCode = accountCode,
                    Description = description,
                    ParentAccountId = parentAccountId,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                int id = _accountRepository.Add(account);
                account.AccountId = id;
                _logger.Info($"Hesap oluşturuldu: {accountName} (ID: {id})");
                return account;
            }
            catch (Exception ex) { _logger.Error("Hesap oluşturma hatası", ex); return null; }
        }

        public List<Account> GetAccountsByCompany(int companyId)
        {
            try { return _accountRepository.GetByCompanyId(companyId); }
            catch (Exception ex) { _logger.Error("Hesaplar alınırken hata", ex); return new List<Account>(); }
        }

        public List<Account> GetAccountsByType(int companyId, string accountType)
        {
            try { return _accountRepository.GetByCompanyIdAndType(companyId, accountType); }
            catch (Exception ex) { _logger.Error("Hesaplar alınırken hata", ex); return new List<Account>(); }
        }

        public Account GetAccountById(int accountId)
        {
            try { return _accountRepository.GetById(accountId); }
            catch (Exception ex) { _logger.Error($"Hesap alınırken hata (ID: {accountId})", ex); return null; }
        }

        public bool UpdateAccount(Account account)
        {
            try
            {
                account.UpdatedAt = DateTime.Now;
                return _accountRepository.Update(account);
            }
            catch (Exception ex) { _logger.Error("Hesap güncelleme hatası", ex); return false; }
        }

        public bool DeleteAccount(int accountId)
        {
            try { return _accountRepository.Delete(accountId); }
            catch (Exception ex) { _logger.Error("Hesap silme hatası", ex); return false; }
        }
    }
}
