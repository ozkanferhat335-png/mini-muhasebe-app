using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MiniMuhasebe.Data;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Services
{
    /// <summary>
    /// Raporlama servisi
    /// </summary>
    public class ReportService
    {
        private readonly IncomeExpenseService _incomeExpenseService;
        private readonly BankService _bankService;
        private readonly AccountRepository _accountRepository;
        private readonly CashTransactionRepository _cashTransactionRepository;
        private readonly CurrentAccountTransactionRepository _currentAccountTransactionRepository;
        private readonly Logger _logger;

        public ReportService(string connectionString, string encryptionKey = "DefaultEncryptionKey123!")
        {
            _incomeExpenseService = new IncomeExpenseService(connectionString);
            _bankService = new BankService(connectionString, encryptionKey);
            _accountRepository = new AccountRepository(connectionString);
            _cashTransactionRepository = new CashTransactionRepository(connectionString);
            _currentAccountTransactionRepository = new CurrentAccountTransactionRepository(connectionString);
            _logger = new Logger();
        }

        /// <summary>
        /// Aylık gelir-gider özeti (hesap tipine göre doğru ayrım)
        /// </summary>
        public IncomeExpenseSummary GetIncomeExpenseSummary(int companyId, int periodId)
        {
            try
            {
                var transactions = _incomeExpenseService.GetTransactionsByPeriod(periodId);
                var accounts = _accountRepository.GetByCompanyId(companyId);

                // Hesap tipine göre gelir/gider ayrımı
                var incomeAccountIds = accounts
                    .Where(a => a.AccountType == "Income")
                    .Select(a => a.AccountId)
                    .ToHashSet();

                var expenseAccountIds = accounts
                    .Where(a => a.AccountType == "Expense")
                    .Select(a => a.AccountId)
                    .ToHashSet();

                var summary = new IncomeExpenseSummary();

                foreach (var tx in transactions)
                {
                    if (incomeAccountIds.Contains(tx.AccountId))
                    {
                        summary.TotalIncome += tx.Amount;
                        summary.TotalIncomeVat += tx.VatAmount;

                        // Kategori bazlı gruplama
                        var account = accounts.FirstOrDefault(a => a.AccountId == tx.AccountId);
                        string categoryName = account?.AccountName ?? "Diğer";
                        if (!summary.IncomeByCategory.ContainsKey(categoryName))
                            summary.IncomeByCategory[categoryName] = 0;
                        summary.IncomeByCategory[categoryName] += tx.Amount;
                    }
                    else if (expenseAccountIds.Contains(tx.AccountId))
                    {
                        summary.TotalExpense += tx.Amount;
                        summary.TotalExpenseVat += tx.VatAmount;

                        var account = accounts.FirstOrDefault(a => a.AccountId == tx.AccountId);
                        string categoryName = account?.AccountName ?? "Diğer";
                        if (!summary.ExpenseByCategory.ContainsKey(categoryName))
                            summary.ExpenseByCategory[categoryName] = 0;
                        summary.ExpenseByCategory[categoryName] += tx.Amount;
                    }
                }

                summary.NetResult = summary.TotalIncome - summary.TotalExpense;
                summary.TransactionCount = transactions.Count;

                _logger.Info($"Gelir-gider özeti: Gelir={summary.TotalIncome:N2}, Gider={summary.TotalExpense:N2}, Net={summary.NetResult:N2}");
                return summary;
            }
            catch (Exception ex)
            {
                _logger.Error("Gelir-gider özeti oluşturma sırasında hata", ex);
                return new IncomeExpenseSummary();
            }
        }

        /// <summary>
        /// Nakit akış raporu (günlük)
        /// </summary>
        public List<CashFlowEntry> GetCashFlowReport(int companyId, int periodId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var cashTransactions = _cashTransactionRepository.GetByCompanyAndDateRange(companyId, startDate, endDate);
                var result = new Dictionary<DateTime, CashFlowEntry>();

                foreach (var tx in cashTransactions)
                {
                    var date = tx.TransactionDate.Date;
                    if (!result.ContainsKey(date))
                        result[date] = new CashFlowEntry { Date = date };

                    if (tx.TransactionType == "Income")
                        result[date].Income += tx.Amount;
                    else
                        result[date].Expense += tx.Amount;
                }

                // Kümülatif bakiye hesapla
                var entries = result.Values.OrderBy(e => e.Date).ToList();
                decimal cumulative = 0;
                foreach (var entry in entries)
                {
                    entry.NetFlow = entry.Income - entry.Expense;
                    cumulative += entry.NetFlow;
                    entry.CumulativeBalance = cumulative;
                }

                _logger.Info($"Nakit akış raporu: {entries.Count} günlük veri");
                return entries;
            }
            catch (Exception ex)
            {
                _logger.Error("Nakit akış raporu oluşturma sırasında hata", ex);
                return new List<CashFlowEntry>();
            }
        }

        /// <summary>
        /// Cari hesap ekstre raporu
        /// </summary>
        public CurrentAccountStatement GetCurrentAccountStatement(int currentAccountId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var transactions = _currentAccountTransactionRepository
                    .GetByCurrentAccountAndDateRange(currentAccountId, startDate, endDate);

                var statement = new CurrentAccountStatement
                {
                    CurrentAccountId = currentAccountId,
                    StartDate = startDate,
                    EndDate = endDate,
                    Transactions = transactions
                };

                foreach (var tx in transactions)
                {
                    if (tx.TransactionType == "Debit")
                        statement.TotalDebit += tx.Amount;
                    else
                        statement.TotalCredit += tx.Amount;
                }

                statement.Balance = statement.TotalCredit - statement.TotalDebit;

                _logger.Info($"Cari ekstre: Borç={statement.TotalDebit:N2}, Alacak={statement.TotalCredit:N2}, Bakiye={statement.Balance:N2}");
                return statement;
            }
            catch (Exception ex)
            {
                _logger.Error("Cari ekstre raporu oluşturma sırasında hata", ex);
                return new CurrentAccountStatement();
            }
        }

        /// <summary>
        /// Banka mutabakatı raporu
        /// </summary>
        public BankReconciliationReport GetBankReconciliationReport(int bankAccountId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var account = _bankService.GetAccountById(bankAccountId);
                if (account == null)
                    return new BankReconciliationReport();

                var bankTransactions = _bankService.GetTransactionsByDateRange(bankAccountId, startDate, endDate);
                var unmatchedTransactions = _bankService.GetUnmatchedTransactions(bankAccountId);

                decimal bankInflow = bankTransactions.Where(t => t.TransactionType == "Credit").Sum(t => t.Amount);
                decimal bankOutflow = bankTransactions.Where(t => t.TransactionType == "Debit").Sum(t => t.Amount);

                var report = new BankReconciliationReport
                {
                    BankAccountId = bankAccountId,
                    BankName = account.BankName,
                    IBAN = account.IBAN,
                    StartDate = startDate,
                    EndDate = endDate,
                    SystemBalance = account.InitialBalance + bankInflow - bankOutflow,
                    BankBalance = account.CurrentBalance,
                    TotalTransactions = bankTransactions.Count,
                    MatchedTransactions = bankTransactions.Count(t => t.IsMatched),
                    UnmatchedTransactions = unmatchedTransactions
                };

                report.Difference = Math.Abs(report.SystemBalance - report.BankBalance);

                _logger.Info($"Banka mutabakatı: Sistem={report.SystemBalance:N2}, Banka={report.BankBalance:N2}, Fark={report.Difference:N2}");
                return report;
            }
            catch (Exception ex)
            {
                _logger.Error("Banka mutabakatı raporu oluşturma sırasında hata", ex);
                return new BankReconciliationReport();
            }
        }

        /// <summary>
        /// Raporu CSV formatında dışa aktar
        /// </summary>
        public bool ExportToCSV(string filePath, List<Dictionary<string, string>> data)
        {
            try
            {
                if (data == null || data.Count == 0)
                    return false;

                var csv = new StringBuilder();
                var headers = data[0].Keys.ToList();
                csv.AppendLine(string.Join(",", headers.Select(h => $"\"{h}\"")));

                foreach (var row in data)
                {
                    var values = headers.Select(h =>
                    {
                        string val = row.ContainsKey(h) ? row[h] ?? "" : "";
                        return $"\"{val.Replace("\"", "\"\"")}\"";
                    });
                    csv.AppendLine(string.Join(",", values));
                }

                File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);
                _logger.Info($"CSV raporu kaydedildi: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("CSV dışa aktarma sırasında hata", ex);
                return false;
            }
        }

        /// <summary>
        /// Gelir-gider işlemlerini CSV için dönüştür
        /// </summary>
        public List<Dictionary<string, string>> TransactionsToCSVData(List<IncomeExpenseTransaction> transactions)
        {
            var data = new List<Dictionary<string, string>>();
            foreach (var tx in transactions)
            {
                data.Add(new Dictionary<string, string>
                {
                    { "Tarih", tx.TransactionDate.ToString("dd.MM.yyyy") },
                    { "Belge No", tx.DocumentNumber ?? "" },
                    { "Açıklama", tx.Description },
                    { "Tutar", tx.Amount.ToString("N2") },
                    { "KDV Oranı", tx.VatRate.ToString("N2") },
                    { "KDV Tutarı", tx.VatAmount.ToString("N2") },
                    { "Net Tutar", tx.NetAmount.ToString("N2") },
                    { "Ödeme Tipi", tx.PaymentType },
                    { "Notlar", tx.Notes ?? "" }
                });
            }
            return data;
        }
    }

    // ---- Rapor DTO'ları ----

    public class IncomeExpenseSummary
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalIncomeVat { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal TotalExpenseVat { get; set; }
        public decimal NetResult { get; set; }
        public int TransactionCount { get; set; }
        public Dictionary<string, decimal> IncomeByCategory { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> ExpenseByCategory { get; set; } = new Dictionary<string, decimal>();
    }

    public class CashFlowEntry
    {
        public DateTime Date { get; set; }
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
        public decimal NetFlow { get; set; }
        public decimal CumulativeBalance { get; set; }
    }

    public class CurrentAccountStatement
    {
        public int CurrentAccountId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal Balance { get; set; }
        public List<CurrentAccountTransaction> Transactions { get; set; } = new List<CurrentAccountTransaction>();
    }

    public class BankReconciliationReport
    {
        public int BankAccountId { get; set; }
        public string BankName { get; set; }
        public string IBAN { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal SystemBalance { get; set; }
        public decimal BankBalance { get; set; }
        public decimal Difference { get; set; }
        public int TotalTransactions { get; set; }
        public int MatchedTransactions { get; set; }
        public List<BankTransaction> UnmatchedTransactions { get; set; } = new List<BankTransaction>();
    }
}
