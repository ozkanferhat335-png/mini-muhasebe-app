using System;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;

namespace MiniMuhasebe.UI.Forms
{
    /// <summary>
    /// Raporlama formu
    /// </summary>
    public class ReportsForm : Form
    {
        private string _reportType;
        private ReportService _reportService;
        private Panel pnlContent;
        private TabControl tabControl;

        public ReportsForm(string reportType)
        {
            _reportType = reportType;
            _reportService = new ReportService(Program.ConnectionString, Program.EncryptionKey);
            InitializeComponent();
            LoadReport();
        }

        private void InitializeComponent()
        {
            this.Text = "Raporlar";
            this.Size = new Size(950, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 247, 250);

            tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9) };

            var tabIncome = new TabPage("💰 Gelir-Gider Özeti");
            var tabCash = new TabPage("💵 Nakit Akış");
            var tabCurrent = new TabPage("👥 Cari Ekstre");
            var tabBank = new TabPage("🏦 Banka Mutabakatı");

            tabControl.TabPages.AddRange(new[] { tabIncome, tabCash, tabCurrent, tabBank });

            // Aktif sekmeyi seç
            switch (_reportType)
            {
                case "CashFlow": tabControl.SelectedTab = tabCash; break;
                case "CurrentAccount": tabControl.SelectedTab = tabCurrent; break;
                case "BankReconciliation": tabControl.SelectedTab = tabBank; break;
                default: tabControl.SelectedTab = tabIncome; break;
            }

            tabControl.SelectedIndexChanged += (s, e) => LoadReport();

            this.Controls.Add(tabControl);

            BuildIncomeExpenseTab(tabIncome);
            BuildCashFlowTab(tabCash);
            BuildCurrentAccountTab(tabCurrent);
            BuildBankReconciliationTab(tabBank);
        }

        private void BuildIncomeExpenseTab(TabPage tab)
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White, Padding = new Padding(10) };
            var btnLoad = new Button { Text = "📊 Raporu Getir", Location = new Point(10, 10), Size = new Size(140, 28), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnLoad.FlatAppearance.BorderSize = 0;

            var rtb = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, Font = new Font("Consolas", 10), BackColor = Color.White };

            btnLoad.Click += (s, e) =>
            {
                if (!Program.ActiveCompanyId.HasValue || !Program.ActivePeriodId.HasValue)
                {
                    rtb.Text = "Lütfen firma ve dönem seçin.";
                    return;
                }

                var summary = _reportService.GetIncomeExpenseSummary(Program.ActiveCompanyId.Value, Program.ActivePeriodId.Value);
                rtb.Clear();
                rtb.SelectionFont = new Font("Consolas", 12, FontStyle.Bold);
                rtb.AppendText($"GELİR-GİDER ÖZETİ - {Program.ActivePeriodName}\n");
                rtb.AppendText(new string('=', 50) + "\n\n");

                rtb.SelectionFont = new Font("Consolas", 10, FontStyle.Bold);
                rtb.SelectionColor = Color.FromArgb(39, 174, 96);
                rtb.AppendText("GELİRLER:\n");
                rtb.SelectionColor = Color.Black;
                rtb.SelectionFont = new Font("Consolas", 10);
                foreach (var kv in summary.IncomeByCategory)
                    rtb.AppendText($"  {kv.Key,-35} {kv.Value,15:N2} ₺\n");
                rtb.AppendText($"\n  {'TOPLAM GELİR',-35} {summary.TotalIncome,15:N2} ₺\n\n");

                rtb.SelectionFont = new Font("Consolas", 10, FontStyle.Bold);
                rtb.SelectionColor = Color.FromArgb(231, 76, 60);
                rtb.AppendText("GİDERLER:\n");
                rtb.SelectionColor = Color.Black;
                rtb.SelectionFont = new Font("Consolas", 10);
                foreach (var kv in summary.ExpenseByCategory)
                    rtb.AppendText($"  {kv.Key,-35} {kv.Value,15:N2} ₺\n");
                rtb.AppendText($"\n  {'TOPLAM GİDER',-35} {summary.TotalExpense,15:N2} ₺\n\n");

                rtb.AppendText(new string('-', 50) + "\n");
                rtb.SelectionFont = new Font("Consolas", 11, FontStyle.Bold);
                rtb.SelectionColor = summary.NetResult >= 0 ? Color.FromArgb(39, 174, 96) : Color.FromArgb(231, 76, 60);
                rtb.AppendText($"  {'NET SONUÇ',-35} {summary.NetResult,15:N2} ₺\n");
                rtb.SelectionColor = Color.Black;
                rtb.AppendText($"\n  Toplam İşlem Sayısı: {summary.TransactionCount}");
            };

            pnl.Controls.Add(btnLoad);
            tab.Controls.Add(rtb);
            tab.Controls.Add(pnl);
        }

        private void BuildCashFlowTab(TabPage tab)
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White, Padding = new Padding(10) };
            var dtpS = new DateTimePicker { Location = new Point(10, 12), Size = new Size(130, 25), Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1) };
            var dtpE = new DateTimePicker { Location = new Point(155, 12), Size = new Size(130, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Now };
            var btnLoad = new Button { Text = "Getir", Location = new Point(300, 10), Size = new Size(80, 28), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnLoad.FlatAppearance.BorderSize = 0;

            var dgv = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, RowHeadersVisible = false, Font = new Font("Segoe UI", 9) };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.EnableHeadersVisualStyles = false;

            btnLoad.Click += (s, e) =>
            {
                if (!Program.ActiveCompanyId.HasValue || !Program.ActivePeriodId.HasValue) return;
                var entries = _reportService.GetCashFlowReport(Program.ActiveCompanyId.Value, Program.ActivePeriodId.Value, dtpS.Value, dtpE.Value);
                dgv.Columns.Clear();
                dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tarih", DataPropertyName = "Date", DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Giriş (₺)", DataPropertyName = "Income", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Çıkış (₺)", DataPropertyName = "Expense", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Net Akış (₺)", DataPropertyName = "NetFlow", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kümülatif (₺)", DataPropertyName = "CumulativeBalance", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
                dgv.DataSource = entries;
            };

            pnl.Controls.AddRange(new Control[] { dtpS, dtpE, btnLoad });
            tab.Controls.Add(dgv);
            tab.Controls.Add(pnl);
        }

        private void BuildCurrentAccountTab(TabPage tab)
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White, Padding = new Padding(10) };
            var cmbAccount = new ComboBox { Location = new Point(10, 12), Size = new Size(220, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            var currentAccountService = new CurrentAccountService(Program.ConnectionString);
            if (Program.ActiveCompanyId.HasValue)
            {
                var accounts = currentAccountService.GetAccountsByCompany(Program.ActiveCompanyId.Value);
                cmbAccount.DataSource = accounts;
                cmbAccount.DisplayMember = "Title";
                cmbAccount.ValueMember = "CurrentAccountId";
            }
            var dtpS = new DateTimePicker { Location = new Point(245, 12), Size = new Size(120, 25), Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Now.Year, 1, 1) };
            var dtpE = new DateTimePicker { Location = new Point(380, 12), Size = new Size(120, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Now };
            var btnLoad = new Button { Text = "Getir", Location = new Point(515, 10), Size = new Size(80, 28), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnLoad.FlatAppearance.BorderSize = 0;

            var dgv = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, RowHeadersVisible = false, Font = new Font("Segoe UI", 9) };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.EnableHeadersVisualStyles = false;

            var lblBalance = new Label { Dock = DockStyle.Bottom, Height = 30, BackColor = Color.FromArgb(52, 73, 94), ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0) };

            btnLoad.Click += (s, e) =>
            {
                if (cmbAccount.SelectedValue == null) return;
                int id = Convert.ToInt32(cmbAccount.SelectedValue);
                var statement = _reportService.GetCurrentAccountStatement(id, dtpS.Value, dtpE.Value);
                dgv.Columns.Clear();
                dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tarih", DataPropertyName = "TransactionDate", DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tür", DataPropertyName = "TransactionType", Width = 80 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Açıklama", DataPropertyName = "Description" });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tutar (₺)", DataPropertyName = "Amount", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
                dgv.DataSource = statement.Transactions;
                lblBalance.Text = $"  Borç: {statement.TotalDebit:N2} ₺  |  Alacak: {statement.TotalCredit:N2} ₺  |  Bakiye: {statement.Balance:N2} ₺";
            };

            pnl.Controls.AddRange(new Control[] { cmbAccount, dtpS, dtpE, btnLoad });
            tab.Controls.Add(dgv);
            tab.Controls.Add(lblBalance);
            tab.Controls.Add(pnl);
        }

        private void BuildBankReconciliationTab(TabPage tab)
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White, Padding = new Padding(10) };
            var bankService = new BankService(Program.ConnectionString, Program.EncryptionKey);
            var cmbBank = new ComboBox { Location = new Point(10, 12), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            if (Program.ActiveCompanyId.HasValue)
            {
                var accounts = bankService.GetAccountsByCompany(Program.ActiveCompanyId.Value);
                cmbBank.DataSource = accounts;
                cmbBank.DisplayMember = "BankName";
                cmbBank.ValueMember = "BankAccountId";
            }
            var dtpS = new DateTimePicker { Location = new Point(225, 12), Size = new Size(120, 25), Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1) };
            var dtpE = new DateTimePicker { Location = new Point(360, 12), Size = new Size(120, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Now };
            var btnLoad = new Button { Text = "Getir", Location = new Point(495, 10), Size = new Size(80, 28), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnLoad.FlatAppearance.BorderSize = 0;

            var rtb = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, Font = new Font("Consolas", 10), BackColor = Color.White };

            btnLoad.Click += (s, e) =>
            {
                if (cmbBank.SelectedValue == null) return;
                int id = Convert.ToInt32(cmbBank.SelectedValue);
                var report = _reportService.GetBankReconciliationReport(id, dtpS.Value, dtpE.Value);
                rtb.Clear();
                rtb.SelectionFont = new Font("Consolas", 12, FontStyle.Bold);
                rtb.AppendText($"BANKA MUTABAKATA RAPORU\n");
                rtb.AppendText($"{report.BankName} - {report.IBAN}\n");
                rtb.AppendText(new string('=', 50) + "\n\n");
                rtb.SelectionFont = new Font("Consolas", 10);
                rtb.AppendText($"Dönem: {report.StartDate:dd.MM.yyyy} - {report.EndDate:dd.MM.yyyy}\n\n");
                rtb.AppendText($"{'Sistem Bakiyesi',-30} {report.SystemBalance,15:N2} ₺\n");
                rtb.AppendText($"{'Banka Bakiyesi',-30} {report.BankBalance,15:N2} ₺\n");
                rtb.AppendText(new string('-', 50) + "\n");
                rtb.SelectionFont = new Font("Consolas", 11, FontStyle.Bold);
                rtb.SelectionColor = report.Difference == 0 ? Color.FromArgb(39, 174, 96) : Color.FromArgb(231, 76, 60);
                rtb.AppendText($"{'FARK',-30} {report.Difference,15:N2} ₺\n");
                rtb.SelectionColor = Color.Black;
                rtb.SelectionFont = new Font("Consolas", 10);
                rtb.AppendText($"\nToplam Hareket: {report.TotalTransactions}\n");
                rtb.AppendText($"Eşleşen: {report.MatchedTransactions}\n");
                rtb.AppendText($"Eşleşmeyen: {report.UnmatchedTransactions.Count}\n");
            };

            pnl.Controls.AddRange(new Control[] { cmbBank, dtpS, dtpE, btnLoad });
            tab.Controls.Add(rtb);
            tab.Controls.Add(pnl);
        }

        private void LoadReport() { }
    }
}
