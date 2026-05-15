using System;
using System.Collections.Generic;

namespace MiniMuhasebe.Business.Interfaces
{
    public interface IReportService
    {
        (decimal totalIncome, decimal totalExpense, decimal netResult) GetMonthlyIncomeExpenseSummary(int companyId, int periodId);
        Dictionary<DateTime, decimal> GetCashFlowReport(int companyId, DateTime startDate, DateTime endDate);
        (decimal systemBalance, decimal bankBalance, decimal difference) GetBankReconciliationReport(int bankAccountId);
        bool ExportReportToCSV(string filePath, List<Dictionary<string, string>> data);
    }
}
