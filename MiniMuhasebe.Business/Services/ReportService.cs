using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniMuhasebe.Data;

using MiniMuhasebe.Business.Interfaces;

namespace MiniMuhasebe.Business.Services
{
    /// <summary>
    /// Raporlama servisi
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly IncomeExpenseService _incomeExpenseService;
        private readonly BankService _bankService;
        private readonly Logger _logger;

        public ReportService(string connectionString, string encryptionKey = "DefaultEncryptionKey123!")
        {
            _incomeExpenseService = new IncomeExpenseService(connectionString);
            _bankService = new BankService(connectionString, encryptionKey);
            _logger = new Logger();
        }

        /// <summary>
        /// Aylık gelir-gider özeti
        /// </summary>
        public (decimal totalIncome, decimal totalExpense, decimal netResult) GetMonthlyIncomeExpenseSummary(int companyId, int periodId)
        {
            try
            {
                var transactions = _incomeExpenseService.GetTransactionsByPeriod(periodId);
                // AccountType bilgisi için Account repository'ye ihtiyaç var; burada Description/Notes üzerinden ayırt edilir.
                // Gelir hesapları pozitif, gider hesapları negatif tutar olarak kaydedilir.
                decimal income = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
                decimal expense = transactions.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount));
                decimal netResult = income - expense;

                _logger.Info($"Aylık özet raporu oluşturuldu: Gelir: {income}, Gider: {expense}, Net: {netResult}");
                return (income, expense, netResult);
            }
            catch (Exception ex)
            {
                _logger.Error("Aylık özet oluşturma sırasında hata", ex);
                return (0, 0, 0);
            }
        }

        /// <summary>
        /// Nakit akış raporu
        /// </summary>
        public Dictionary<DateTime, decimal> GetCashFlowReport(int companyId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var transactions = _incomeExpenseService.GetTransactionsByDateRange(companyId, startDate, endDate);
                var cashFlow = new Dictionary<DateTime, decimal>();

                // Günlük toplamı hesapla
                var groupedByDate = transactions.GroupBy(t => t.TransactionDate);
                foreach (var group in groupedByDate)
                {
                    decimal dailyTotal = group.Sum(t => t.PaymentType == "Cash" ? t.Amount : 0);
                    if (dailyTotal != 0)
                    {
                        cashFlow[group.Key] = dailyTotal;
                    }
                }

                _logger.Info($"Nakit akış raporu oluşturuldu: {cashFlow.Count} günün verisi");
                return cashFlow;
            }
            catch (Exception ex)
            {
                _logger.Error("Nakit akış raporu oluşturma sırasında hata", ex);
                return new Dictionary<DateTime, decimal>();
            }
        }

        /// <summary>
        /// Banka mutabakatı raporu
        /// </summary>
        public (decimal systemBalance, decimal bankBalance, decimal difference) GetBankReconciliationReport(int bankAccountId)
        {
            try
            {
                var account = _bankService.GetAccountById(bankAccountId);
                if (account == null)
                    return (0, 0, 0);

                decimal systemBalance = account.InitialBalance;
                decimal bankBalance = account.CurrentBalance;
                decimal difference = Math.Abs(systemBalance - bankBalance);

                _logger.Info($"Banka mutabakatı raporu: Sistem: {systemBalance}, Banka: {bankBalance}, Fark: {difference}");
                return (systemBalance, bankBalance, difference);
            }
            catch (Exception ex)
            {
                _logger.Error("Banka mutabakatı raporu oluşturma sırasında hata", ex);
                return (0, 0, 0);
            }
        }

        /// <summary>
        /// Raporu CSV formatında dışa aktar
        /// </summary>
        public bool ExportReportToCSV(string filePath, List<Dictionary<string, string>> data)
        {
            try
            {
                if (data == null || data.Count == 0)
                    return false;

                var csv = new System.Text.StringBuilder();

                // Başlık satırı
                var headers = data[0].Keys.ToList();
                csv.AppendLine(string.Join(",", headers));

                // Veri satırları
                foreach (var row in data)
                {
                    var values = headers.Select(h => row.ContainsKey(h) ? row[h] : "");
                    csv.AppendLine(string.Join(",", values));
                }

                File.WriteAllText(filePath, csv.ToString());
                _logger.Info($"Rapor CSV olarak kaydedildi: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Rapor dışa aktarma sırasında hata", ex);
                return false;
            }
        }
    }
}
