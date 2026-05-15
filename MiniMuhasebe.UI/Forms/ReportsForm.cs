using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    public class ReportsForm : Form
    {
        private Panel pnlToolbar;
        private Panel pnlLeft;
        private Panel pnlContent;
        private ListBox lstReports;
        private Panel pnlFilter;
        private DateTimePicker dtpStart, dtpEnd;
        private ComboBox cmbPeriod;
        private Button btnGenerate, btnExportCSV;
        private DataGridView dgvReport;
        private Label lblReportTitle, lblSummary;

        private readonly IncomeExpenseService _incomeExpenseService;
        private readonly BankService _bankService;
        private readonly FiscalPeriodService _periodService;
        private readonly CurrentAccountService _currentAccountService;
        private readonly CurrentAccountTransactionService _catService;

        public ReportsForm()
        {
            _incomeExpenseService = new IncomeExpenseService(AppSession.ConnectionString);
            _bankService = new BankService(AppSession.ConnectionString, AppSession.EncryptionKey);
            _periodService = new FiscalPeriodService(AppSession.ConnectionString);
            _currentAccountService = new CurrentAccountService(AppSession.ConnectionString);
            _catService = new CurrentAccountTransactionService(AppSession.ConnectionString);

            InitializeComponent();
            LoadPeriods();
        }

        private void InitializeComponent()
        {
            this.Text = "Raporlar";
            this.BackColor = Color.FromArgb(245, 247, 250);

            // Toolbar
            pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.White };
            var lblTitle = new Label
            {
                Text = "📈 Raporlar",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(10, 15)
            };
            pnlToolbar.Controls.Add(lblTitle);

            // Left panel - report list
            pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            var lblReportList = new Label
            {
                Text = "RAPOR TİPLERİ",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.Gray,
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(5, 10, 0, 0)
            };

            lstReports = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ItemHeight = 30
            };
            lstReports.Items.AddRange(new object[]
            {
                "📊 Gelir-Gider Özeti",
                "💰 Nakit Akış Raporu",
                "🏦 Banka Hareket Raporu",
                "👥 Cari Ekstre Raporu",
                "📋 Dönem Özeti",
                "🔗 Eşleştirme Raporu"
            });
            lstReports.SelectedIndexChanged += LstReports_SelectedIndexChanged;

            pnlLeft.Controls.AddRange(new Control[] { lstReports, lblReportList });

            // Content panel
            pnlContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            // Filter panel
            pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(235, 240, 245),
                Padding = new Padding(10, 8, 10, 5)
            };

            var lblPeriod = new Label { Text = "Dönem:", Font = new Font("Segoe UI", 9), AutoSize = true, Location = new Point(10, 13) };
            cmbPeriod = new ComboBox { Location = new Point(60, 10), Size = new Size(180, 25), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9) };

            var lblFrom = new Label { Text = "Başlangıç:", Font = new Font("Segoe UI", 9), AutoSize = true, Location = new Point(250, 13) };
            dtpStart = new DateTimePicker { Location = new Point(320, 10), Size = new Size(120, 25), Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1) };

            var lblTo = new Label { Text = "Bitiş:", Font = new Font("Segoe UI", 9), AutoSize = true, Location = new Point(450, 13) };
            dtpEnd = new DateTimePicker { Location = new Point(490, 10), Size = new Size(120, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Now };

            btnGenerate = new Button
            {
                Text = "📊 Raporu Oluştur",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(41, 128, 185),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(620, 8),
                Size = new Size(140, 30),
                Cursor = Cursors.Hand
            };
            btnGenerate.FlatAppearance.BorderSize = 0;
            btnGenerate.Click += (s, e) => GenerateReport();

            btnExportCSV = new Button
            {
                Text = "📥 CSV Dışa Aktar",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(230, 126, 34),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(770, 8),
                Size = new Size(140, 30),
                Cursor = Cursors.Hand
            };
            btnExportCSV.FlatAppearance.BorderSize = 0;
            btnExportCSV.Click += (s, e) => ExportToCSV();

            pnlFilter.Controls.AddRange(new Control[] { lblPeriod, cmbPeriod, lblFrom, dtpStart, lblTo, dtpEnd, btnGenerate, btnExportCSV });

            // Report title
            lblReportTitle = new Label
            {
                Text = "Rapor türü seçin...",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(0, 10, 0, 0)
            };

            // Summary label
            lblSummary = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(80, 100, 120),
                Dock = DockStyle.Top,
                Height = 30
            };

            // Report grid
            dgvReport = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9),
                GridColor = Color.FromArgb(230, 235, 240),
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(248, 250, 252) }
            };
            dgvReport.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvReport.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgvReport.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvReport.ColumnHeadersHeight = 35;

            pnlContent.Controls.AddRange(new Control[] { dgvReport, lblSummary, lblReportTitle, pnlFilter });

            this.Controls.AddRange(new Control[] { pnlContent, pnlLeft, pnlToolbar });
        }

        private void LoadPeriods()
        {
            cmbPeriod.Items.Clear();
            cmbPeriod.Items.Add(new ComboItem("Tarih Aralığı", 0));

            if (AppSession.CurrentCompany != null)
            {
                var periods = _periodService.GetPeriodsByCompany(AppSession.CurrentCompany.CompanyId);
                foreach (var p in periods)
                    cmbPeriod.Items.Add(new ComboItem(p.PeriodName, p.PeriodId));
            }
            cmbPeriod.SelectedIndex = 0;
        }

        private void LstReports_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstReports.SelectedIndex < 0) return;
            lblReportTitle.Text = lstReports.SelectedItem.ToString();
            GenerateReport();
        }

        private void GenerateReport()
        {
            if (lstReports.SelectedIndex < 0) return;
            if (AppSession.CurrentCompany == null) return;

            int companyId = AppSession.CurrentCompany.CompanyId;
            DateTime start = dtpStart.Value;
            DateTime end = dtpEnd.Value;

            int selectedPeriod = cmbPeriod.SelectedItem != null ? ((ComboItem)cmbPeriod.SelectedItem).Value : 0;
            if (selectedPeriod > 0)
            {
                var period = _periodService.GetPeriodById(selectedPeriod);
                if (period != null) { start = period.StartDate; end = period.EndDate; }
            }

            switch (lstReports.SelectedIndex)
            {
                case 0: GenerateIncomeExpenseSummary(companyId, start, end); break;
                case 1: GenerateCashFlowReport(companyId, start, end); break;
                case 2: GenerateBankTransactionReport(companyId, start, end); break;
                case 3: GenerateCurrentAccountReport(companyId); break;
                case 4: GeneratePeriodSummary(companyId, start, end); break;
                case 5: GenerateMatchingReport(companyId); break;
            }
        }

        private void GenerateIncomeExpenseSummary(int companyId, DateTime start, DateTime end)
        {
            var transactions = _incomeExpenseService.GetTransactionsByDateRange(companyId, start, end);

            dgvReport.Columns.Clear();
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Tarih", FillWeight = 12 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "DocNo", HeaderText = "Belge No", FillWeight = 12 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Açıklama", FillWeight = 30 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Tutar (₺)", FillWeight = 15 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Vat", HeaderText = "KDV (₺)", FillWeight = 12 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Net", HeaderText = "Net (₺)", FillWeight = 12 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Payment", HeaderText = "Ödeme", FillWeight = 10 });

            dgvReport.Rows.Clear();
            decimal totalAmount = 0, totalVat = 0, totalNet = 0;

            foreach (var t in transactions)
            {
                string paymentDisplay = t.PaymentType == "Cash" ? "Nakit" : t.PaymentType == "Bank" ? "Banka" : "Cari";
                dgvReport.Rows.Add(t.TransactionDate.ToString("dd.MM.yyyy"), t.DocumentNumber,
                    t.Description, t.Amount.ToString("N2"), t.VatAmount.ToString("N2"),
                    t.NetAmount.ToString("N2"), paymentDisplay);
                totalAmount += t.Amount;
                totalVat += t.VatAmount;
                totalNet += t.NetAmount;
            }

            lblSummary.Text = $"Toplam: {transactions.Count} kayıt  |  Toplam Tutar: {totalAmount:N2} ₺  |  Toplam KDV: {totalVat:N2} ₺  |  Net: {totalNet:N2} ₺";
        }

        private void GenerateCashFlowReport(int companyId, DateTime start, DateTime end)
        {
            var transactions = _incomeExpenseService.GetTransactionsByDateRange(companyId, start, end);
            var grouped = transactions.GroupBy(t => t.TransactionDate.Date).OrderBy(g => g.Key);

            dgvReport.Columns.Clear();
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Tarih", FillWeight = 20 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Income", HeaderText = "Gelir (₺)", FillWeight = 25 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Expense", HeaderText = "Gider (₺)", FillWeight = 25 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Net", HeaderText = "Net (₺)", FillWeight = 25 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Count", HeaderText = "İşlem Sayısı", FillWeight = 15 });

            dgvReport.Rows.Clear();
            decimal totalIncome = 0, totalExpense = 0;

            foreach (var group in grouped)
            {
                decimal income = group.Where(t => t.PaymentType == "Cash").Sum(t => t.Amount);
                decimal expense = 0; // Simplified
                decimal net = income - expense;
                totalIncome += income;
                totalExpense += expense;

                dgvReport.Rows.Add(group.Key.ToString("dd.MM.yyyy"),
                    income.ToString("N2"), expense.ToString("N2"), net.ToString("N2"), group.Count());
            }

            lblSummary.Text = $"Toplam Nakit Gelir: {totalIncome:N2} ₺  |  Toplam Nakit Gider: {totalExpense:N2} ₺  |  Net: {(totalIncome - totalExpense):N2} ₺";
        }

        private void GenerateBankTransactionReport(int companyId, DateTime start, DateTime end)
        {
            var bankAccounts = _bankService.GetAccountsByCompany(companyId);

            dgvReport.Columns.Clear();
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Bank", HeaderText = "Banka", FillWeight = 15 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Tarih", FillWeight = 12 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Açıklama", FillWeight = 30 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Tür", FillWeight = 10 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Tutar (₺)", FillWeight = 15 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "Bakiye (₺)", FillWeight = 15 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Durum", FillWeight = 10 });

            dgvReport.Rows.Clear();
            int totalCount = 0;
            decimal totalCredit = 0, totalDebit = 0;

            foreach (var ba in bankAccounts)
            {
                var transactions = _bankService.GetTransactionsByDateRange(ba.BankAccountId, start, end);
                foreach (var t in transactions)
                {
                    string typeDisplay = t.TransactionType == "Credit" ? "Alacak" : "Borç";
                    string statusDisplay = t.IsMatched ? "Eşleştirildi" : "Bekliyor";
                    dgvReport.Rows.Add($"{ba.BankName}", t.TransactionDate.ToString("dd.MM.yyyy"),
                        t.Description, typeDisplay, t.Amount.ToString("N2"),
                        t.Balance?.ToString("N2") ?? "-", statusDisplay);

                    if (t.TransactionType == "Credit") totalCredit += t.Amount;
                    else totalDebit += t.Amount;
                    totalCount++;
                }
            }

            lblSummary.Text = $"Toplam: {totalCount} hareket  |  Toplam Alacak: {totalCredit:N2} ₺  |  Toplam Borç: {totalDebit:N2} ₺";
        }

        private void GenerateCurrentAccountReport(int companyId)
        {
            var accounts = _currentAccountService.GetAccountsByCompany(companyId);

            dgvReport.Columns.Clear();
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Unvan", FillWeight = 30 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Tür", FillWeight = 12 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "TaxNo", HeaderText = "Vergi No", FillWeight = 15 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "Telefon", FillWeight = 15 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "Bakiye (₺)", FillWeight = 15 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Durum", FillWeight = 10 });

            dgvReport.Rows.Clear();
            decimal totalBalance = 0;

            foreach (var a in accounts)
            {
                decimal balance = _catService.GetBalance(a.CurrentAccountId);
                string typeDisplay = a.AccountType == "Customer" ? "Müşteri" : "Tedarikçi";
                string status = balance > 0 ? "Alacaklı" : balance < 0 ? "Borçlu" : "Sıfır";

                int rowIdx = dgvReport.Rows.Add(a.Title, typeDisplay, a.TaxNumber, a.Phone, balance.ToString("N2"), status);

                if (balance > 0) dgvReport.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(39, 174, 96);
                else if (balance < 0) dgvReport.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(192, 57, 43);

                totalBalance += balance;
            }

            lblSummary.Text = $"Toplam: {accounts.Count} cari  |  Net Bakiye: {totalBalance:N2} ₺";
        }

        private void GeneratePeriodSummary(int companyId, DateTime start, DateTime end)
        {
            var transactions = _incomeExpenseService.GetTransactionsByDateRange(companyId, start, end);

            dgvReport.Columns.Clear();
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Kategori", FillWeight = 30 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Count", HeaderText = "İşlem Sayısı", FillWeight = 15 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "Toplam (₺)", FillWeight = 20 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Vat", HeaderText = "KDV (₺)", FillWeight = 15 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Net", HeaderText = "Net (₺)", FillWeight = 15 });

            dgvReport.Rows.Clear();

            var grouped = transactions.GroupBy(t => t.PaymentType);
            decimal grandTotal = 0;

            foreach (var group in grouped)
            {
                string catName = group.Key == "Cash" ? "Nakit" : group.Key == "Bank" ? "Banka" : "Cari Hesap";
                decimal total = group.Sum(t => t.Amount);
                decimal vat = group.Sum(t => t.VatAmount);
                decimal net = group.Sum(t => t.NetAmount);
                dgvReport.Rows.Add(catName, group.Count(), total.ToString("N2"), vat.ToString("N2"), net.ToString("N2"));
                grandTotal += total;
            }

            // Toplam satırı
            dgvReport.Rows.Add("TOPLAM", transactions.Count,
                transactions.Sum(t => t.Amount).ToString("N2"),
                transactions.Sum(t => t.VatAmount).ToString("N2"),
                transactions.Sum(t => t.NetAmount).ToString("N2"));

            lblSummary.Text = $"Dönem: {start:dd.MM.yyyy} - {end:dd.MM.yyyy}  |  Toplam İşlem: {transactions.Count}  |  Toplam Tutar: {grandTotal:N2} ₺";
        }

        private void GenerateMatchingReport(int companyId)
        {
            var matches = _matchingService_GetAll();

            dgvReport.Columns.Clear();
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "MatchId", HeaderText = "Eşleşme ID", FillWeight = 10 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "BankTxId", HeaderText = "Banka Hareketi ID", FillWeight = 15 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "IETxId", HeaderText = "Muhasebe Kaydı ID", FillWeight = 15 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Score", HeaderText = "Eşleşme Skoru", FillWeight = 15 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Eşleşme Tipi", FillWeight = 15 });
            dgvReport.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Tarih", FillWeight = 15 });

            dgvReport.Rows.Clear();
            foreach (var m in matches)
            {
                string typeDisplay = m.MatchType == "Automatic" ? "Otomatik" : "Manuel";
                dgvReport.Rows.Add(m.MatchId, m.BankTransactionId, m.IncomeExpenseTransactionId,
                    m.MatchScore?.ToString("N0") + "%" ?? "-", typeDisplay, m.CreatedAt.ToString("dd.MM.yyyy HH:mm"));
            }

            lblSummary.Text = $"Toplam Eşleştirme: {matches.Count}";
        }

        private List<TransactionMatch> _matchingService_GetAll()
        {
            var matchingService = new MatchingService(AppSession.ConnectionString);
            return matchingService.GetAllMatches();
        }

        private void ExportToCSV()
        {
            if (dgvReport.Rows.Count == 0)
            { MessageBox.Show("Dışa aktarılacak veri yok.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV Dosyası|*.csv";
                sfd.FileName = $"Rapor_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var sb = new System.Text.StringBuilder();

                        // Headers
                        var headers = new List<string>();
                        foreach (DataGridViewColumn col in dgvReport.Columns)
                            headers.Add(col.HeaderText);
                        sb.AppendLine(string.Join(",", headers));

                        // Rows
                        foreach (DataGridViewRow row in dgvReport.Rows)
                        {
                            var values = new List<string>();
                            foreach (DataGridViewCell cell in row.Cells)
                                values.Add($"\"{cell.Value}\"");
                            sb.AppendLine(string.Join(",", values));
                        }

                        File.WriteAllText(sfd.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                        MessageBox.Show($"Rapor kaydedildi:\n{sfd.FileName}", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Dışa aktarma hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
