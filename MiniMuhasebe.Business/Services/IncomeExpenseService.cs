using System;
using System.Collections.Generic;
using MiniMuhasebe.Data;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Services
{
    /// <summary>
    /// Gelir-Gider işlemleri servisi
    /// </summary>
    public class IncomeExpenseService
    {
        private readonly IncomeExpenseTransactionRepository _transactionRepository;
        private readonly Logger _logger;

        public IncomeExpenseService(string connectionString)
        {
            _transactionRepository = new IncomeExpenseTransactionRepository(connectionString);
            _logger = new Logger();
        }

        /// <summary>
        /// Yeni gelir/gider işlemi ekle
        /// </summary>
        public IncomeExpenseTransaction CreateTransaction(int companyId, int periodId, int accountId, DateTime transactionDate,
            string description, decimal amount, decimal vatRate, string paymentType, int? bankAccountId = null, 
            int? currentAccountId = null, int? createdBy = null, string documentNumber = null, string notes = null)
        {
            try
            {
                // KDV hesapla
                decimal vatAmount = (amount * vatRate) / 100;
                decimal netAmount = amount - vatAmount;

                var newTransaction = new IncomeExpenseTransaction
                {
                    CompanyId = companyId,
                    PeriodId = periodId,
                    AccountId = accountId,
                    TransactionDate = transactionDate,
                    DocumentNumber = documentNumber,
                    Description = description,
                    Amount = amount,
                    VatRate = vatRate,
                    VatAmount = vatAmount,
                    NetAmount = netAmount,
                    PaymentType = paymentType,
                    BankAccountId = bankAccountId,
                    CurrentAccountId = currentAccountId,
                    Notes = notes,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                int transactionId = _transactionRepository.Add(newTransaction);
                newTransaction.TransactionId = transactionId;

                _logger.Info($"Yeni işlem oluşturuldu: {description} (ID: {transactionId}) - Tutar: {amount}");
                return newTransaction;
            }
            catch (Exception ex)
            {
                _logger.Error("Işlem oluşturma sırasında hata", ex);
                return null;
            }
        }

        /// <summary>
        /// Işlemi güncelle
        /// </summary>
        public bool UpdateTransaction(IncomeExpenseTransaction transaction, int? updatedBy = null)
        {
            try
            {
                // KDV hesapla
                decimal vatAmount = (transaction.Amount * transaction.VatRate) / 100;
                transaction.VatAmount = vatAmount;
                transaction.NetAmount = transaction.Amount - vatAmount;

                transaction.UpdatedBy = updatedBy;
                transaction.UpdatedAt = DateTime.Now;

                bool success = _transactionRepository.Update(transaction);
                if (success)
                {
                    _logger.Info($"Işlem güncellendi: {transaction.Description} (ID: {transaction.TransactionId})");
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.Error("Işlem güncelleme sırasında hata", ex);
                return false;
            }
        }

        /// <summary>
        /// Mali dönem içindeki tüm işlemleri getir
        /// </summary>
        public List<IncomeExpenseTransaction> GetTransactionsByPeriod(int periodId)
        {
            try
            {
                return _transactionRepository.GetByPeriodId(periodId);
            }
            catch (Exception ex)
            {
                _logger.Error("Dönem işlemleri alınırken hata", ex);
                return new List<IncomeExpenseTransaction>();
            }
        }

        /// <summary>
        /// Tarih aralığına göre işlemleri getir
        /// </summary>
        public List<IncomeExpenseTransaction> GetTransactionsByDateRange(int companyId, DateTime startDate, DateTime endDate)
        {
            try
            {
                return _transactionRepository.GetByCompanyAndDateRange(companyId, startDate, endDate);
            }
            catch (Exception ex)
            {
                _logger.Error("Tarih aralığı işlemleri alınırken hata", ex);
                return new List<IncomeExpenseTransaction>();
            }
        }

        /// <summary>
        /// Işlemi sil
        /// </summary>
        public bool DeleteTransaction(int transactionId)
        {
            try
            {
                bool success = _transactionRepository.Delete(transactionId);
                if (success)
                {
                    _logger.Info($"Işlem silindi: ID {transactionId}");
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.Error("Işlem silme sırasında hata", ex);
                return false;
            }
        }

        /// <summary>
        /// Kategori bazlı toplam gelir/gider hesapla
        /// </summary>
        public decimal GetTotalByAccountAndPeriod(int accountId, int periodId)
        {
            try
            {
                var transactions = _transactionRepository.GetByPeriodId(periodId);
                decimal total = 0;
                foreach (var t in transactions)
                {
                    if (t.AccountId == accountId)
                    {
                        total += t.Amount;
                    }
                }
                return total;
            }
            catch (Exception ex)
            {
                _logger.Error("Toplam hesaplama sırasında hata", ex);
                return 0;
            }
        }
    }
}
