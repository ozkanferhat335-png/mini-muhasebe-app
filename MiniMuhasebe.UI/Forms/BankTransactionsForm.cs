using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    public class BankTransactionsForm : Form
    {
        private DataGridView dgvTransactions;
        private Panel pnlToolbar;
        private Panel pnlFilter;
        private Button btnRefresh, btnExport, btnMatch;
        private ComboBox cmbBankAccount;
        private DateTimePicker dtpStart, dtpEnd;
        private ComboBox cmbStatus;
        private Label lblSummary;

        private readonly BankService _bankService;
        private List<BankTransaction> _transactions;
        private List<BankAccount> _bankAccounts;

        public BankTransactionsForm()
        {
            _bankService = new BankService(AppSession.ConnectionString, AppSession.EncryptionKey);
            InitializeComponent();
            LoadBankAccounts();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Banka Hareketleri";
            this.BackColor = Color.FromArgb(245, 247, 250);

            pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.White };

            var lblTitle = new Label
            {
                Text = "📋 Banka Hareketleri",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(10, 15)
            };

            btnRefresh = CreateBtn("🔄 Yenile", Color.FromArgb(127, 140, 141), 230, 12);
            btnRefresh.Click += (s, e) => LoadData();

            btnMatch = CreateBtn("🔗 Eşleştir", Color.FromArgb(142, 68, 173), 320, 12);
            btnMatch.Click += (s, e) => OpenMatchingForSelected();

            btnExport = CreateBtn("📥 CSV Dışa Aktar", Color.FromArgb(230, 126, 34), 410, 12);
            btnExport.Width = 130;
            btnExport.Click += (s, e) => ExportToCSV();

            pnlToolbar.Controls.AddRange(new Control[] { lblTitle, btnRefresh, btnMatch, btnExport });

            // Filter panel
            pnlFilter = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(235, 240, 245) };

            var lblBank = new Label { Text = "Banka:", Font = new Font("Segoe UI", 9), AutoSize = true, Location = new Point(10, 13) };
            cmbBankAccount = new ComboBox { Location = new Point(55, 10), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9) };
            cmbBankAccount.SelectedIndexChanged += (s, e) => LoadData();

            var lblFrom = new Label { Text = "Başlangıç:", Font = new Font("Segoe UI", 9), AutoSize = true, Location = new Point(265, 13) };
            dtpStart = new DateTimePicker { Location = new Point(335, 10), Size = new Size(120, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Now.AddMonths(-1) };

            var lblTo = new Label { Text = "Bitiş:", Font = new Font("Segoe UI", 9), AutoSize = true, Location = new Point(465, 13) };
            dtpEnd = new DateTimePicker { Location = new Point(500, 10), Size = new Size(120, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Now };

            var lblStatus = new Label { Text = "Durum:", Font = new Font("Segoe UI", 9), AutoSize = true, Location = new Point(630, 13) };
            cmbStatus = new ComboBox { Location = new Point(675, 10), Size = new Size(120, 25), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9) };
            cmbStatus.Items.AddRange(new object[] { "Tümü", "Bekleyen", "Eşleştirildi", "Eşleştirilmedi" });
            cmbStatus.SelectedIndex = 0;

            var btnFilter = CreateBtn("Filtrele", Color.FromArgb(41, 128, 185), 805, 8);
            btnFilter.Height = 28;
            btnFilter.Click += (s, e) => LoadData();

            lblSummary = new Label { Text = "", Font = new Font("Segoe UI", 9), ForeColor = Color.Gray, AutoSize = true, Location = new Point(900, 13) };

            pnlFilter.Controls.AddRange(new Control[] { lblBank, cmbBankAccount, lblFrom, dtpStart, lblTo, dtpEnd, lblStatus, cmbStatus, btnFilter, lblSummary });

            // Grid
            dgvTransactions = new DataGridView
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
                Font = new Font("Segoe UI", 9),
                GridColor = Color.FromArgb(230, 235, 240),
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(248, 250, 252) }
            };
            dgvTransactions.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvTransactions.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgvTransactions.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvTransactions.ColumnHeadersHeight = 35;

            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "BankTransactionId", HeaderText = "ID", Width = 50, FillWeight = 5 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransactionDate", HeaderText = "Tarih", FillWeight = 10 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Açıklama", FillWeight = 30 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransactionType", HeaderText = "Tür", FillWeight = 8 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Tutar (₺)", FillWeight = 12 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "Bakiye (₺)", FillWeight = 12 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "ReferenceNumber", HeaderText = "Referans No", FillWeight = 12 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Durum", FillWeight = 10 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "IsMatched", HeaderText = "Eşleşme", FillWeight = 8 });

            dgvTransactions.CellFormatting += DgvTransactions_CellFormatting;

            this.Controls.AddRange(new Control[] { dgvTransactions, pnlFilter, pnlToolbar });
        }

        private Button CreateBtn(string text, Color color, int x, int y)
        {
            var btn = new Button
            {
                Text = text, Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White, BackColor = color, FlatStyle = FlatStyle.Flat,
                Location = new Point(x, y), Size = new Size(85, 30), Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void LoadBankAccounts()
        {
            cmbBankAccount.Items.Clear();
            cmbBankAccount.Items.Add(new ComboItem("Tüm Hesaplar", 0));

            if (AppSession.CurrentCompany != null)
            {
                _bankAccounts = _bankService.GetAccountsByCompany(AppSession.CurrentCompany.CompanyId);
                foreach (var ba in _bankAccounts)
                    cmbBankAccount.Items.Add(new ComboItem($"{ba.BankName} - {ba.AccountName}", ba.BankAccountId));
            }
            cmbBankAccount.SelectedIndex = 0;
        }

        private void LoadData()
        {
            try
            {
                dgvTransactions.Rows.Clear();
                _transactions = new List<BankTransaction>();

                int selectedBankId = cmbBankAccount.SelectedItem != null ? ((ComboItem)cmbBankAccount.SelectedItem).Value : 0;
                string statusFilter = cmbStatus.SelectedItem?.ToString() ?? "Tümü";

                if (selectedBankId > 0)
                {
                    _transactions = _bankService.GetTransactionsByDateRange(selectedBankId, dtpStart.Value, dtpEnd.Value);
                }
                else if (_bankAccounts != null)
                {
                    foreach (var ba in _bankAccounts)
                    {
                        var txs = _bankService.GetTransactionsByDateRange(ba.BankAccountId, dtpStart.Value, dtpEnd.Value);
                        _transactions.AddRange(txs);
                    }
                }

                decimal totalCredit = 0, totalDebit = 0;
                int matchedCount = 0;

                foreach (var t in _transactions)
                {
                    // Durum filtresi
                    if (statusFilter == "Bekleyen" && t.Status != "Pending") continue;
                    if (statusFilter == "Eşleştirildi" && !t.IsMatched) continue;
                    if (statusFilter == "Eşleştirilmedi" && t.IsMatched) continue;

                    string typeDisplay = t.TransactionType == "Credit" ? "Alacak" : "Borç";
                    string statusDisplay = t.Status == "Matched" ? "Eşleştirildi" : t.Status == "Pending" ? "Bekliyor" : "Eşleştirilmedi";

                    dgvTransactions.Rows.Add(
                        t.BankTransactionId,
                        t.TransactionDate.ToString("dd.MM.yyyy"),
                        t.Description,
                        typeDisplay,
                        t.Amount.ToString("N2"),
                        t.Balance?.ToString("N2") ?? "-",
                        t.ReferenceNumber,
                        statusDisplay,
                        t.IsMatched ? "✓" : "✗"
                    );

                    if (t.TransactionType == "Credit") totalCredit += t.Amount;
                    else totalDebit += t.Amount;
                    if (t.IsMatched) matchedCount++;
                }

                lblSummary.Text = $"Toplam: {_transactions.Count} | Eşleştirildi: {matchedCount} | Alacak: {totalCredit:N2} ₺ | Borç: {totalDebit:N2} ₺";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veri yüklenirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvTransactions_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvTransactions.Rows[e.RowIndex];

            string type = row.Cells["TransactionType"].Value?.ToString();
            string matched = row.Cells["IsMatched"].Value?.ToString();

            if (type == "Alacak")
                row.DefaultCellStyle.ForeColor = Color.FromArgb(39, 174, 96);
            else if (type == "Borç")
                row.DefaultCellStyle.ForeColor = Color.FromArgb(192, 57, 43);

            if (matched == "✓")
                row.DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
        }

        private void OpenMatchingForSelected()
        {
            if (dgvTransactions.SelectedRows.Count == 0)
            { MessageBox.Show("Lütfen bir hareket seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            int id = Convert.ToInt32(dgvTransactions.SelectedRows[0].Cells["BankTransactionId"].Value);
            var matchingForm = new MatchingForm(id);
            matchingForm.ShowDialog();
            LoadData();
        }

        private void ExportToCSV()
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV Dosyası|*.csv";
                sfd.FileName = $"BankaHareketleri_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine("ID,Tarih,Açıklama,Tür,Tutar,Bakiye,Referans No,Durum,Eşleşme");

                        foreach (DataGridViewRow row in dgvTransactions.Rows)
                        {
                            sb.AppendLine($"{row.Cells["BankTransactionId"].Value},{row.Cells["TransactionDate"].Value}," +
                                         $"\"{row.Cells["Description"].Value}\",{row.Cells["TransactionType"].Value}," +
                                         $"{row.Cells["Amount"].Value},{row.Cells["Balance"].Value}," +
                                         $"{row.Cells["ReferenceNumber"].Value},{row.Cells["Status"].Value},{row.Cells["IsMatched"].Value}");
                        }

                        System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                        MessageBox.Show($"Dosya kaydedildi:\n{sfd.FileName}", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
