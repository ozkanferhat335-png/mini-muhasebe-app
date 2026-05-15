using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    public class ReportsForm : Form
    {
        private TabControl tabControl;
        private TabPage tabSummary;
        private TabPage tabCashFlow;
        private TabPage tabCurrentAccount;
        private TabPage tabBankReport;

        private readonly User _currentUser;
        private readonly Company _activeCompany;
        private FiscalPeriod _activePeriod;

        private readonly ReportService _reportService;
        private readonly IncomeExpenseService _incomeExpenseService;
        private readonly BankService _bankService;
        private readonly CurrentAccountService _currentAccountService;
        private readonly FiscalPeriodRepository _periodRepository;
        private readonly AccountRepository _accountRepository;
        private readonly CurrentAccountTransactionRepository _catRepository;

        public ReportsForm(User user, Company company, FiscalPeriod period)
        {
            _currentUser = user;
            _activeCompany = company;
            _activePeriod = period;
            _reportService = new ReportService(Program.ConnectionString, Program.EncryptionKey);
            _incomeExpenseService = new IncomeExpenseService(Program.ConnectionString);
            _bankService = new BankService(Program.ConnectionString, Program.EncryptionKey);
            _currentAccountService = new CurrentAccountService(Program.ConnectionString);
            _periodRepository = new FiscalPeriodRepository(Program.ConnectionString);
            _accountRepository = new AccountRepository(Program.ConnectionString);
            _catRepository = new CurrentAccountTransactionRepository(Program.ConnectionString);

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Raporlar";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 9f);

            tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5f) };

            tabSummary = BuildSummaryTab();
            tabCashFlow = BuildCashFlowTab();
            tabCurrentAccount = BuildCurrentAccountTab();
            tabBankReport = BuildBankReportTab();

            tabControl.TabPages.AddRange(new TabPage[] { tabSummary, tabCashFlow, tabCurrentAccount, tabBankReport });
            this.Controls.Add(tabControl);
        }

        private TabPage BuildSummaryTab()
        {
            var tab = new TabPage("📊 Gelir-Gider Özeti") { BackColor = Color.White };

            var pnlFilter = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.White, Padding = new Padding(8) };

            var lblPeriod = new Label { Text = "Dönem:", AutoSize = true, Location = new Point(10, 18), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var cmbPeriod = new ComboBox
            {
                Location = new Point(70, 14),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "PeriodName"
            };

            if (_activeCompany != null)
            {
                var periods = _periodRepository.GetByCompanyId(_activeCompany.CompanyId);
                foreach (var p in periods) cmbPeriod.Items.Add(p);
                if (_activePeriod != null)
                {
                    for (int i = 0; i < cmbPeriod.Items.Count; i++)
                        if (((FiscalPeriod)cmbPeriod.Items[i]).PeriodId == _activePeriod.PeriodId)
                        { cmbPeriod.SelectedIndex = i; break; }
                }
                else if (cmbPeriod.Items.Count > 0) cmbPeriod.SelectedIndex = 0;
            }

            var dgv = CreateReportDgv();
            dgv.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colAccount", HeaderText = "Hesap", FillWeight = 30 },
                new DataGridViewTextBoxColumn { Name = "colType", HeaderText = "Tür", FillWeight = 15 },
                new DataGridViewTextBoxColumn { Name = "colCount", HeaderText = "İşlem Sayısı", FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "colTotal", HeaderText = "Toplam Tutar", FillWeight = 20, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "colVat", HeaderText = "Toplam KDV", FillWeight = 20, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } }
            });

            var pnlSummaryCards = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = Color.FromArgb(245, 247, 250) };
            var lblIncome = new Label { Text = "Toplam Gelir: ₺0,00", AutoSize = true, Location = new Point(20, 25), Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(39, 174, 96) };
            var lblExpense = new Label { Text = "Toplam Gider: ₺0,00", AutoSize = true, Location = new Point(280, 25), Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(231, 76, 60) };
            var lblNet = new Label { Text = "Net Sonuç: ₺0,00", AutoSize = true, Location = new Point(560, 25), Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(41, 128, 185) };
            pnlSummaryCards.Controls.AddRange(new Control[] { lblIncome, lblExpense, lblNet });

            var btnLoad = new Button
            {
                Text = "📊 Raporu Yükle",
                Size = new Size(140, 28),
                Location = new Point(285, 13),
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };

            var btnExport = new Button
            {
                Text = "📥 CSV Dışa Aktar",
                Size = new Size(140, 28),
                Location = new Point(435, 13),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };

            btnLoad.Click += (s, e) =>
            {
                if (cmbPeriod.SelectedItem is not FiscalPeriod selectedPeriod) return;
                dgv.Rows.Clear();

                var transactions = _incomeExpenseService.GetTransactionsByPeriod(selectedPeriod.PeriodId);
                var accounts = _accountRepository.GetByCompanyId(_activeCompany?.CompanyId ?? 0);

                var grouped = transactions.GroupBy(t => t.AccountId);
                decimal totalIncome = 0, totalExpense = 0;

                foreach (var group in grouped)
                {
                    var account = accounts.Find(a => a.AccountId == group.Key);
                    decimal total = group.Sum(t => t.Amount);
                    decimal totalVat = group.Sum(t => t.VatAmount);
                    string type = account?.AccountType ?? "Bilinmiyor";

                    dgv.Rows.Add(account?.AccountName ?? $"Hesap #{group.Key}", type, group.Count(), $"₺{total:N2}", $"₺{totalVat:N2}");

                    if (type == "Income") totalIncome += total;
                    else if (type == "Expense") totalExpense += total;
                }

                decimal net = totalIncome - totalExpense;
                lblIncome.Text = $"Toplam Gelir: ₺{totalIncome:N2}";
                lblExpense.Text = $"Toplam Gider: ₺{totalExpense:N2}";
                lblNet.Text = $"Net Sonuç: ₺{net:N2}";
                lblNet.ForeColor = net >= 0 ? Color.FromArgb(39, 174, 96) : Color.FromArgb(231, 76, 60);
            };

            btnExport.Click += (s, e) => ExportDgvToCsv(dgv, "GelirGiderOzeti");

            pnlFilter.Controls.AddRange(new Control[] { lblPeriod, cmbPeriod, btnLoad, btnExport });
            tab.Controls.AddRange(new Control[] { pnlSummaryCards, dgv, pnlFilter });
            return tab;
        }

        private TabPage BuildCashFlowTab()
        {
            var tab = new TabPage("💵 Nakit Akış Raporu") { BackColor = Color.White };

            var pnlFilter = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.White, Padding = new Padding(8) };

            var lblStart = new Label { Text = "Başlangıç:", AutoSize = true, Location = new Point(10, 18), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var dtpStart = new DateTimePicker { Location = new Point(85, 14), Size = new Size(130, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Now.AddMonths(-1) };

            var lblEnd = new Label { Text = "Bitiş:", AutoSize = true, Location = new Point(230, 18), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var dtpEnd = new DateTimePicker { Location = new Point(270, 14), Size = new Size(130, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Now };

            var dgv = CreateReportDgv();
            dgv.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "Tarih", FillWeight = 15 },
                new DataGridViewTextBoxColumn { Name = "colIncome", HeaderText = "Gelir", FillWeight = 20, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "colExpense", HeaderText = "Gider", FillWeight = 20, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "colNet", HeaderText = "Net", FillWeight = 20, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "colCumulative", HeaderText = "Kümülatif", FillWeight = 25, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } }
            });

            var btnLoad = new Button
            {
                Text = "📊 Raporu Yükle",
                Size = new Size(140, 28),
                Location = new Point(415, 13),
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };

            var btnExport = new Button
            {
                Text = "📥 CSV Dışa Aktar",
                Size = new Size(140, 28),
                Location = new Point(565, 13),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };

            btnLoad.Click += (s, e) =>
            {
                if (_activeCompany == null) return;
                dgv.Rows.Clear();

                var transactions = _incomeExpenseService.GetTransactionsByDateRange(_activeCompany.CompanyId, dtpStart.Value, dtpEnd.Value);
                var accounts = _accountRepository.GetByCompanyId(_activeCompany.CompanyId);

                var grouped = transactions.GroupBy(t => t.TransactionDate.Date).OrderBy(g => g.Key);
                decimal cumulative = 0;

                foreach (var group in grouped)
                {
                    decimal income = 0, expense = 0;
                    foreach (var tx in group)
                    {
                        var acc = accounts.Find(a => a.AccountId == tx.AccountId);
                        if (acc?.AccountType == "Income") income += tx.Amount;
                        else if (acc?.AccountType == "Expense") expense += tx.Amount;
                    }
                    decimal net = income - expense;
                    cumulative += net;

                    var row = dgv.Rows.Add(group.Key.ToString("dd.MM.yyyy"), $"₺{income:N2}", $"₺{expense:N2}", $"₺{net:N2}", $"₺{cumulative:N2}");
                    dgv.Rows[row].DefaultCellStyle.ForeColor = net >= 0 ? Color.FromArgb(39, 174, 96) : Color.FromArgb(231, 76, 60);
                }
            };

            btnExport.Click += (s, e) => ExportDgvToCsv(dgv, "NakitAkisRaporu");

            pnlFilter.Controls.AddRange(new Control[] { lblStart, dtpStart, lblEnd, dtpEnd, btnLoad, btnExport });
            tab.Controls.AddRange(new Control[] { dgv, pnlFilter });
            return tab;
        }

        private TabPage BuildCurrentAccountTab()
        {
            var tab = new TabPage("👥 Cari Ekstre") { BackColor = Color.White };

            var pnlFilter = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.White, Padding = new Padding(8) };

            var lblAcc = new Label { Text = "Cari Hesap:", AutoSize = true, Location = new Point(10, 18), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var cmbAcc = new ComboBox
            {
                Location = new Point(100, 14),
                Size = new Size(250, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "Title"
            };

            if (_activeCompany != null)
            {
                var accounts = _currentAccountService.GetAccountsByCompany(_activeCompany.CompanyId);
                foreach (var a in accounts) cmbAcc.Items.Add(a);
                if (cmbAcc.Items.Count > 0) cmbAcc.SelectedIndex = 0;
            }

            var dgv = CreateReportDgv();
            dgv.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "Tarih", FillWeight = 12 },
                new DataGridViewTextBoxColumn { Name = "colType", HeaderText = "Tür", FillWeight = 10 },
                new DataGridViewTextBoxColumn { Name = "colDesc", HeaderText = "Açıklama", FillWeight = 35 },
                new DataGridViewTextBoxColumn { Name = "colDocNo", HeaderText = "Belge No", FillWeight = 13 },
                new DataGridViewTextBoxColumn { Name = "colDebit", HeaderText = "Borç", FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "colCredit", HeaderText = "Alacak", FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } }
            });

            var pnlBalance = new Panel { Dock = DockStyle.Bottom, Height = 50, BackColor = Color.FromArgb(245, 247, 250) };
            var lblBalance = new Label { Text = "Bakiye: ₺0,00", AutoSize = true, Location = new Point(20, 15), Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.FromArgb(41, 128, 185) };
            pnlBalance.Controls.Add(lblBalance);

            var btnLoad = new Button
            {
                Text = "📊 Ekstreyi Yükle",
                Size = new Size(140, 28),
                Location = new Point(365, 13),
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };

            var btnExport = new Button
            {
                Text = "📥 CSV Dışa Aktar",
                Size = new Size(140, 28),
                Location = new Point(515, 13),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };

            btnLoad.Click += (s, e) =>
            {
                if (cmbAcc.SelectedItem is not CurrentAccount selected) return;
                dgv.Rows.Clear();

                var txs = _catRepository.GetByCurrentAccountId(selected.CurrentAccountId);
                decimal balance = _catRepository.GetBalance(selected.CurrentAccountId);

                foreach (var tx in txs)
                {
                    dgv.Rows.Add(
                        tx.TransactionDate.ToString("dd.MM.yyyy"),
                        tx.TransactionType == "Debit" ? "Borç" : "Alacak",
                        tx.Description,
                        tx.RelatedDocumentNumber,
                        tx.TransactionType == "Debit" ? $"₺{tx.Amount:N2}" : "",
                        tx.TransactionType == "Credit" ? $"₺{tx.Amount:N2}" : ""
                    );
                }

                lblBalance.Text = $"Bakiye: ₺{balance:N2}";
                lblBalance.ForeColor = balance >= 0 ? Color.FromArgb(39, 174, 96) : Color.FromArgb(231, 76, 60);
            };

            btnExport.Click += (s, e) => ExportDgvToCsv(dgv, "CariEkstre");

            pnlFilter.Controls.AddRange(new Control[] { lblAcc, cmbAcc, btnLoad, btnExport });
            tab.Controls.AddRange(new Control[] { pnlBalance, dgv, pnlFilter });
            return tab;
        }

        private TabPage BuildBankReportTab()
        {
            var tab = new TabPage("🏦 Banka Hareket Raporu") { BackColor = Color.White };

            var pnlFilter = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.White, Padding = new Padding(8) };

            var lblAcc = new Label { Text = "Banka Hesabı:", AutoSize = true, Location = new Point(10, 18), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var cmbAcc = new ComboBox
            {
                Location = new Point(115, 14),
                Size = new Size(220, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "BankName"
            };

            var lblStart = new Label { Text = "Başlangıç:", AutoSize = true, Location = new Point(350, 18), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var dtpStart = new DateTimePicker { Location = new Point(425, 14), Size = new Size(130, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Now.AddMonths(-1) };
            var lblEnd = new Label { Text = "Bitiş:", AutoSize = true, Location = new Point(565, 18), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var dtpEnd = new DateTimePicker { Location = new Point(605, 14), Size = new Size(130, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Now };

            if (_activeCompany != null)
            {
                var bankAccounts = _bankService.GetAccountsByCompany(_activeCompany.CompanyId);
                foreach (var ba in bankAccounts) cmbAcc.Items.Add(ba);
                if (cmbAcc.Items.Count > 0) cmbAcc.SelectedIndex = 0;
            }

            var dgv = CreateReportDgv();
            dgv.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "Tarih", FillWeight = 12 },
                new DataGridViewTextBoxColumn { Name = "colDesc", HeaderText = "Açıklama", FillWeight = 35 },
                new DataGridViewTextBoxColumn { Name = "colType", HeaderText = "Tür", FillWeight = 10 },
                new DataGridViewTextBoxColumn { Name = "colAmount", HeaderText = "Tutar", FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "colBalance", HeaderText = "Bakiye", FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "Durum", FillWeight = 13 }
            });

            var btnLoad = new Button
            {
                Text = "📊 Raporu Yükle",
                Size = new Size(130, 28),
                Location = new Point(750, 13),
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };

            var btnExport = new Button
            {
                Text = "📥 CSV Dışa Aktar",
                Size = new Size(140, 28),
                Location = new Point(890, 13),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };

            btnLoad.Click += (s, e) =>
            {
                if (cmbAcc.SelectedItem is not BankAccount selected) return;
                dgv.Rows.Clear();

                var txs = _bankService.GetTransactionsByDateRange(selected.BankAccountId, dtpStart.Value, dtpEnd.Value);
                foreach (var tx in txs)
                {
                    var row = dgv.Rows.Add(
                        tx.TransactionDate.ToString("dd.MM.yyyy"),
                        tx.Description,
                        tx.TransactionType == "Credit" ? "✅ Alacak" : "🔴 Borç",
                        $"₺{tx.Amount:N2}",
                        tx.Balance.HasValue ? $"₺{tx.Balance:N2}" : "-",
                        tx.Status switch { "Matched" => "✅ Eşleştirildi", "Unmatched" => "❌ Eşleştirilmedi", _ => "⏳ Bekliyor" }
                    );
                    dgv.Rows[row].DefaultCellStyle.ForeColor = tx.TransactionType == "Credit" ? Color.FromArgb(39, 174, 96) : Color.FromArgb(231, 76, 60);
                }
            };

            btnExport.Click += (s, e) => ExportDgvToCsv(dgv, "BankaHareketRaporu");

            pnlFilter.Controls.AddRange(new Control[] { lblAcc, cmbAcc, lblStart, dtpStart, lblEnd, dtpEnd, btnLoad, btnExport });
            tab.Controls.AddRange(new Control[] { dgv, pnlFilter });
            return tab;
        }

        private DataGridView CreateReportDgv()
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9f),
                GridColor = Color.FromArgb(230, 230, 230),
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(248, 249, 250) }
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            return dgv;
        }

        private void ExportDgvToCsv(DataGridView dgv, string reportName)
        {
            if (dgv.Rows.Count == 0)
            {
                MessageBox.Show("Dışa aktarılacak veri yok.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV Dosyası (*.csv)|*.csv";
                sfd.FileName = $"{reportName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var sb = new StringBuilder();

                        // Başlıklar
                        var headers = new List<string>();
                        foreach (DataGridViewColumn col in dgv.Columns)
                            headers.Add($"\"{col.HeaderText}\"");
                        sb.AppendLine(string.Join(",", headers));

                        // Veriler
                        foreach (DataGridViewRow row in dgv.Rows)
                        {
                            var values = new List<string>();
                            foreach (DataGridViewCell cell in row.Cells)
                                values.Add($"\"{cell.Value?.ToString()?.Replace("\"", "\"\"")}\"");
                            sb.AppendLine(string.Join(",", values));
                        }

                        File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                        MessageBox.Show($"Rapor kaydedildi:\n{sfd.FileName}", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Dışa aktarma sırasında hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
