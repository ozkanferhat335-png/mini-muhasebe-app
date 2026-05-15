using System.Collections.Generic;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Interfaces
{
    public interface IMatchingService
    {
        TransactionMatch CreateManualMatch(int bankTransactionId, int incomeExpenseTransactionId, int? createdBy);
        List<TransactionMatch> RunAutoMatching(int companyId, int bankAccountId);
        bool RemoveMatch(int matchId);
        List<BankTransaction> GetPendingBankTransactions(int bankAccountId);
        List<IncomeExpenseTransaction> GetUnmatchedIncomeExpenseTransactions(int companyId);
        decimal CalculateMatchScore(BankTransaction bankTx, IncomeExpenseTransaction ieTx, MatchingRule rule);
    }
}
