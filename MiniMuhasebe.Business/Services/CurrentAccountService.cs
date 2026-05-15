using System;
using System.Collections.Generic;
using MiniMuhasebe.Data;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Business.Interfaces;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Services
{
    /// <summary>
    /// Cari hesap işlemleri servisi
    /// </summary>
    public class CurrentAccountService : ICurrentAccountService
    {
        private readonly CurrentAccountRepository _currentAccountRepository;
        private readonly Logger _logger;

        public CurrentAccountService(string connectionString)
        {
            _currentAccountRepository = new CurrentAccountRepository(connectionString);
            _logger = new Logger();
        }

        /// <summary>
        /// Yeni cari kart oluştur
        /// </summary>
        public CurrentAccount CreateCurrentAccount(int companyId, string title, string accountType, 
            string taxNumber = null, string taxId = null, string phone = null, string email = null, string address = null)
        {
            try
            {
                var newAccount = new CurrentAccount
                {
                    CompanyId = companyId,
                    Title = title,
                    AccountType = accountType, // Customer / Supplier
                    TaxNumber = taxNumber,
                    TaxId = taxId,
                    Phone = phone,
                    Email = email,
                    Address = address,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                int accountId = _currentAccountRepository.Add(newAccount);
                newAccount.CurrentAccountId = accountId;

                _logger.Info($"Yeni cari kart oluşturuldu: {title} (ID: {accountId})");
                return newAccount;
            }
            catch (Exception ex)
            {
                _logger.Error("Cari kart oluşturma sırasında hata", ex);
                return null;
            }
        }

        /// <summary>
        /// Cari kartı güncelle
        /// </summary>
        public bool UpdateCurrentAccount(CurrentAccount account)
        {
            try
            {
                account.UpdatedAt = DateTime.Now;
                bool success = _currentAccountRepository.Update(account);
                if (success)
                {
                    _logger.Info($"Cari kart güncellendi: {account.Title}");
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.Error("Cari kart güncelleme sırasında hata", ex);
                return false;
            }
        }

        /// <summary>
        /// Firmaya ait tüm cari kartları getir
        /// </summary>
        public List<CurrentAccount> GetAccountsByCompany(int companyId)
        {
            try
            {
                return _currentAccountRepository.GetByCompanyId(companyId);
            }
            catch (Exception ex)
            {
                _logger.Error("Cari kartlar alınırken hata", ex);
                return new List<CurrentAccount>();
            }
        }

        /// <summary>
        /// Firmaya ait cari kartları türe göre getir
        /// </summary>
        public List<CurrentAccount> GetAccountsByCompanyAndType(int companyId, string accountType)
        {
            try
            {
                return _currentAccountRepository.GetByCompanyIdAndType(companyId, accountType);
            }
            catch (Exception ex)
            {
                _logger.Error("Cari kartlar alınırken hata", ex);
                return new List<CurrentAccount>();
            }
        }

        /// <summary>
        /// ID'ye göre cari kart getir
        /// </summary>
        public CurrentAccount GetAccountById(int accountId)
        {
            try
            {
                return _currentAccountRepository.GetById(accountId);
            }
            catch (Exception ex)
            {
                _logger.Error($"Cari kart alınırken hata (ID: {accountId})", ex);
                return null;
            }
        }

        /// <summary>
        /// Cari kartı sil
        /// </summary>
        public bool DeleteCurrentAccount(int accountId)
        {
            try
            {
                bool success = _currentAccountRepository.Delete(accountId);
                if (success)
                {
                    _logger.Info($"Cari kart silindi: ID {accountId}");
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.Error("Cari kart silme sırasında hata", ex);
                return false;
            }
        }
    }
}
