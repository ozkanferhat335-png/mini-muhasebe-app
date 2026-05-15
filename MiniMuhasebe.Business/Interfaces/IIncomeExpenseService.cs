using System;
using System.Collections.Generic;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Interfaces
{
    public interface IIncomeExpenseService
    {
        IncomeExpenseTransaction CreateTransaction(int companyId, int periodId, int accountId, DateTime transactionDate,
            string description, decimal amount, decimal vatRate, string paymentType, int? bankAccountId,
            int? currentAccountId, int? createdBy, string documentNumber, string notes);
        bool UpdateTransaction(IncomeExpenseTransaction transaction, int? updatedBy);
        List<IncomeExpenseTransaction> GetTransactionsByPeriod(int periodId);
        List<IncomeExpenseTransaction> GetTransactionsByDateRange(int companyId, DateTime startDate, DateTime endDate);
        bool DeleteTransaction(int transactionId);
        decimal GetTotalByAccountAndPeriod(int accountId, int periodId);
    }
}
