using System;
using System.Collections.Generic;
using MiniMuhasebe.Data;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Business.Interfaces;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Services
{
    /// <summary>
    /// Banka işlemleri servisi
    /// </summary>
    public class BankService : IBankService
    {
        private readonly BankAccountRepository _bankAccountRepository;
        private readonly BankTransactionRepository _bankTransactionRepository;
        private readonly Logger _logger;
        private readonly string _encryptionKey;

        public BankService(string connectionString, string encryptionKey = "DefaultEncryptionKey123!")
        {
            _bankAccountRepository = new BankAccountRepository(connectionString, encryptionKey);
            _bankTransactionRepository = new BankTransactionRepository(connectionString);
            _logger = new Logger();
            _encryptionKey = encryptionKey;
        }

        /// <summary>
        /// Yeni banka hesabı oluştur
        /// </summary>
        public BankAccount CreateBankAccount(int companyId, string bankName, string accountName, string iban,
            string currency = "TRY", decimal initialBalance = 0, bool isApiEnabled = false, 
            string apiProviderType = null, string apiBaseUrl = null, string apiClientId = null,
            string apiClientSecret = null, string apiKey = null, string apiUsername = null, 
            string apiPassword = null, string apiAccountId = null)
        {
            try
            {
                var newAccount = new BankAccount
                {
                    CompanyId = companyId,
                    BankName = bankName,
                    AccountName = accountName,
                    IBAN = iban,
                    Currency = currency,
                    InitialBalance = initialBalance,
                    CurrentBalance = initialBalance,
                    IsApiEnabled = isApiEnabled,
                    ApiProviderType = apiProviderType,
                    ApiBaseUrl = apiBaseUrl,
                    ApiClientId = apiClientId,
                    ApiClientSecret = apiClientSecret,
                    ApiKey = apiKey,
                    ApiUsername = apiUsername,
                    ApiPassword = apiPassword,
                    ApiAccountId = apiAccountId,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                int accountId = _bankAccountRepository.Add(newAccount);
                newAccount.BankAccountId = accountId;

                _logger.Info($"Yeni banka hesabı oluşturuldu: {bankName} - {iban} (ID: {accountId})");
                return newAccount;
            }
            catch (Exception ex)
            {
                _logger.Error("Banka hesabı oluşturma sırasında hata", ex);
                return null;
            }
        }

        /// <summary>
        /// Banka hesabını güncelle
        /// </summary>
        public bool UpdateBankAccount(BankAccount account)
        {
            try
            {
                account.UpdatedAt = DateTime.Now;
                bool success = _bankAccountRepository.Update(account);
                if (success)
                {
                    _logger.Info($"Banka hesabı güncellendi: {account.BankName}");
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.Error("Banka hesabı güncelleme sırasında hata", ex);
                return false;
            }
        }

        /// <summary>
        /// Firmaya ait tüm banka hesaplarını getir
        /// </summary>
        public List<BankAccount> GetAccountsByCompany(int companyId)
        {
            try
            {
                return _bankAccountRepository.GetByCompanyId(companyId);
            }
            catch (Exception ex)
            {
                _logger.Error("Banka hesapları alınırken hata", ex);
                return new List<BankAccount>();
            }
        }

        /// <summary>
        /// ID'ye göre banka hesabı getir
        /// </summary>
        public BankAccount GetAccountById(int accountId)
        {
            try
            {
                return _bankAccountRepository.GetById(accountId);
            }
            catch (Exception ex)
            {
                _logger.Error($"Banka hesabı alınırken hata (ID: {accountId})", ex);
                return null;
            }
        }

        /// <summary>
        /// Banka hareketleri ekle (API'den)
        /// </summary>
        public BankTransaction AddBankTransaction(int bankAccountId, BankTransaction transaction)
        {
            try
            {
                // Mükerrer kayıt kontrolü
                if (!string.IsNullOrEmpty(transaction.BankTransactionId_External))
                {
                    var existing = _bankTransactionRepository.GetByExternalId(transaction.BankTransactionId_External);
                    if (existing != null)
                    {
                        _logger.Warning($"Mükerrer banka hareketi engellendi: {transaction.BankTransactionId_External}");
                        return null;
                    }
                }

                transaction.BankAccountId = bankAccountId;
                transaction.Status = "Pending";
                int transactionId = _bankTransactionRepository.Add(transaction);
                transaction.BankTransactionId = transactionId;

                _logger.Info($"Banka hareketi eklendi: {transaction.Description} (ID: {transactionId})");
                return transaction;
            }
            catch (Exception ex)
            {
                _logger.Error("Banka hareketi ekleme sırasında hata", ex);
                return null;
            }
        }

        /// <summary>
        /// Banka hareketlerini tarih aralığına göre getir
        /// </summary>
        public List<BankTransaction> GetTransactionsByDateRange(int bankAccountId, DateTime startDate, DateTime endDate)
        {
            try
            {
                return _bankTransactionRepository.GetByBankAccountAndDateRange(bankAccountId, startDate, endDate);
            }
            catch (Exception ex)
            {
                _logger.Error("Banka hareketleri alınırken hata", ex);
                return new List<BankTransaction>();
            }
        }

        /// <summary>
        /// Eşleşmeyen banka hareketlerini getir
        /// </summary>
        public List<BankTransaction> GetUnmatchedTransactions(int bankAccountId)
        {
            try
            {
                return _bankTransactionRepository.GetUnmatchedByBankAccount(bankAccountId);
            }
            catch (Exception ex)
            {
                _logger.Error("Eşleşmeyen hareketler alınırken hata", ex);
                return new List<BankTransaction>();
            }
        }

        /// <summary>
        /// Banka hesabını sil
        /// </summary>
        public bool DeleteBankAccount(int accountId)
        {
            try
            {
                bool success = _bankAccountRepository.Delete(accountId);
                if (success)
                {
                    _logger.Info($"Banka hesabı silindi: ID {accountId}");
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.Error("Banka hesabı silme sırasında hata", ex);
                return false;
            }
        }

        /// <summary>
        /// Banka bakiyesini güncelle
        /// </summary>
        public bool UpdateBankBalance(int bankAccountId, decimal newBalance)
        {
            try
            {
                var account = _bankAccountRepository.GetById(bankAccountId);
                if (account == null)
                    return false;

                account.CurrentBalance = newBalance;
                return _bankAccountRepository.Update(account);
            }
            catch (Exception ex)
            {
                _logger.Error("Banka bakiyesi güncelleme sırasında hata", ex);
                return false;
            }
        }
    }
}
