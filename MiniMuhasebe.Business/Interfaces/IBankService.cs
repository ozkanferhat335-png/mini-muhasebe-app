using System;
using System.Collections.Generic;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Interfaces
{
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
}
