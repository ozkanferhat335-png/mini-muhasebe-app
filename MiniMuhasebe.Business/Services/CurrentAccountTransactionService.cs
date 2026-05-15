using System;
using System.Collections.Generic;
using MiniMuhasebe.Data;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Services
{
    public class CurrentAccountTransactionService
    {
        private readonly CurrentAccountTransactionRepository _repository;
        private readonly Logger _logger;

        public CurrentAccountTransactionService(string connectionString)
        {
            _repository = new CurrentAccountTransactionRepository(connectionString);
            _logger = new Logger();
        }

        public CurrentAccountTransaction AddTransaction(int currentAccountId, DateTime date, decimal amount,
            string transactionType, string description, string documentNumber = null,
            int? incomeExpenseTransactionId = null, int? createdBy = null)
        {
            try
            {
                var tx = new CurrentAccountTransaction
                {
                    CurrentAccountId = currentAccountId,
                    TransactionDate = date,
                    Amount = amount,
                    TransactionType = transactionType,
                    Description = description,
                    RelatedDocumentNumber = documentNumber,
                    IncomeExpenseTransactionId = incomeExpenseTransactionId,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now
                };
                int id = _repository.Add(tx);
                tx.TransactionId = id;
                return tx;
            }
            catch (Exception ex) { _logger.Error("Cari hareket ekleme hatası", ex); return null; }
        }

        public List<CurrentAccountTransaction> GetTransactionsByAccount(int currentAccountId)
        {
            try { return _repository.GetByCurrentAccountId(currentAccountId); }
            catch (Exception ex) { _logger.Error("Cari hareketler alınırken hata", ex); return new List<CurrentAccountTransaction>(); }
        }

        public decimal GetBalance(int currentAccountId)
        {
            try { return _repository.GetBalance(currentAccountId); }
            catch (Exception ex) { _logger.Error("Bakiye hesaplama hatası", ex); return 0; }
        }

        public bool DeleteTransaction(int transactionId)
        {
            try { return _repository.Delete(transactionId); }
            catch (Exception ex) { _logger.Error("Cari hareket silme hatası", ex); return false; }
        }
    }
}
