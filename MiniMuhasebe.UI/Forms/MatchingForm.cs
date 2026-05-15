using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    public class MatchingForm : Form
    {
        private DataGridView dgvBankTransactions;
        private DataGridView dgvIncomeExpense;
        private Panel pnlToolbar;
        private Panel pnlLeft;
        private Panel pnlRight;
        private Panel pnlCenter;
        private Button btnMatch, btnUnmatch, btnAutoMatch, btnRefresh;
        private Label lblBankTitle, lblIETitle, lblMatchInfo;
        private ComboBox cmbBankAccount;

        private readonly MatchingService _matchingService;
        private readonly BankService _bankService;
        private readonly IncomeExpenseService _incomeExpenseService;
        private List<BankTransaction> _bankTransactions;
        private List<IncomeExpenseTransaction> _ieTransactions;
        private int _preselectedBankTransactionId;

        public MatchingForm(int preselectedBankTransactionId = 0)
        {
            _matchingService = new MatchingService(AppSession.ConnectionString);
            _bankService = new BankService(AppSession.ConnectionString, AppSession.EncryptionKey);
            _incomeExpenseService = new IncomeExpenseService(AppSession.ConnectionString);
            _preselectedBankTransactionId = preselectedBankTransactionId;

            InitializeComponent();
            LoadBankAccounts();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Eşleştirme Sistemi";
            this.BackColor = Color.FromArgb(245, 247, 250);

            // Toolbar
            pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.White };

            var lblTitle = new Label
            {
                Text = "🔗 Eşleştirme Sistemi",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(10, 15)
            };

            var lblBank = new Label { Text = "Banka:", Font = new Font("Segoe UI", 9), AutoSize = true, Location = new Point(230, 18) };
            cmbBankAccount = new ComboBox { Location = new Point(270, 15), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9) };
            cmbBankAccount.SelectedIndexChanged += (s, e) => LoadData();

            btnAutoMatch = CreateBtn("⚡ Otomatik Eşleştir", Color.FromArgb(230, 126, 34), 485, 12);
            btnAutoMatch.Width = 150;
            btnAutoMatch.Click += (s, e) => AutoMatch();

            btnMatch = CreateBtn("✓ Eşleştir", Color.FromArgb(39, 174, 96), 645, 12);
            btnMatch.Click += (s, e) => ManualMatch();

            btnUnmatch = CreateBtn("✗ Eşleştirmeyi Kaldır", Color.FromArgb(192, 57, 43), 735, 12);
            btnUnmatch.Width = 150;
            btnUnmatch.Click += (s, e) => RemoveMatch();

            btnRefresh = CreateBtn("🔄 Yenile", Color.FromArgb(127, 140, 141), 895, 12);
            btnRefresh.Click += (s, e) => LoadData();

            pnlToolbar.Controls.AddRange(new Control[] { lblTitle, lblBank, cmbBankAccount, btnAutoMatch, btnMatch, btnUnmatch, btnRefresh });

            // Main split layout
            var pnlMain = new Panel { Dock = DockStyle.Fill };

            // Left - Bank Transactions
            pnlLeft = new Panel { Dock = DockStyle.Left, Width = 480, Padding = new Padding(5) };

            lblBankTitle = new Label
            {
                Text = "🏦 Banka Hareketleri (Eşleştirilmemiş)",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(0, 5, 0, 0)
            };

            dgvBankTransactions = CreateGrid();
            dgvBankTransactions.Dock = DockStyle.Fill;
            dgvBankTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "BankTransactionId", HeaderText = "ID", Width = 50 });
            dgvBankTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransactionDate", HeaderText = "Tarih", FillWeight = 15 });
            dgvBankTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Açıklama", FillWeight = 40 });
            dgvBankTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Tutar (₺)", FillWeight = 15 });
            dgvBankTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Tür", FillWeight = 10 });
            dgvBankTransactions.SelectionChanged += DgvBankTransactions_SelectionChanged;

            pnlLeft.Controls.AddRange(new Control[] { dgvBankTransactions, lblBankTitle });

            // Center - Match info
            pnlCenter = new Panel { Dock = DockStyle.Left, Width = 20, BackColor = Color.FromArgb(200, 210, 220) };

            // Right - Income/Expense Transactions
            pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };

            lblIETitle = new Label
            {
                Text = "📊 Muhasebe Kayıtları",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(0, 5, 0, 0)
            };

            dgvIncomeExpense = CreateGrid();
            dgvIncomeExpense.Dock = DockStyle.Fill;
            dgvIncomeExpense.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransactionId", HeaderText = "ID", Width = 50 });
            dgvIncomeExpense.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransactionDate", HeaderText = "Tarih", FillWeight = 15 });
            dgvIncomeExpense.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Açıklama", FillWeight = 35 });
            dgvIncomeExpense.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Tutar (₺)", FillWeight = 15 });
            dgvIncomeExpense.Columns.Add(new DataGridViewTextBoxColumn { Name = "PaymentType", HeaderText = "Ödeme", FillWeight = 12 });
            dgvIncomeExpense.Columns.Add(new DataGridViewTextBoxColumn { Name = "Score", HeaderText = "Eşleşme %", FillWeight = 12 });

            pnlRight.Controls.AddRange(new Control[] { dgvIncomeExpense, lblIETitle });

            // Match info bar
            lblMatchInfo = new Label
            {
                Text = "Eşleştirmek için sol taraftan banka hareketi, sağ taraftan muhasebe kaydı seçin ve 'Eşleştir' butonuna tıklayın.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(80, 100, 120),
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0),
                BackColor = Color.FromArgb(235, 240, 245)
            };

            pnlMain.Controls.AddRange(new Control[] { pnlRight, pnlCenter, pnlLeft });

            this.Controls.AddRange(new Control[] { pnlMain, lblMatchInfo, pnlToolbar });
        }

        private DataGridView CreateGrid()
        {
            var dgv = new DataGridView
            {
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
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersHeight = 35;
            return dgv;
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
                var accounts = _bankService.GetAccountsByCompany(AppSession.CurrentCompany.CompanyId);
                foreach (var a in accounts)
                    cmbBankAccount.Items.Add(new ComboItem($"{a.BankName} - {a.AccountName}", a.BankAccountId));
            }
            cmbBankAccount.SelectedIndex = 0;
        }

        private void LoadData()
        {
            if (AppSession.CurrentCompany == null) return;

            int companyId = AppSession.CurrentCompany.CompanyId;
            int selectedBankId = cmbBankAccount.SelectedItem != null ? ((ComboItem)cmbBankAccount.SelectedItem).Value : 0;

            // Eşleştirilmemiş banka hareketleri
            dgvBankTransactions.Rows.Clear();
            _bankTransactions = new List<BankTransaction>();

            if (selectedBankId > 0)
            {
                _bankTransactions = _bankService.GetUnmatchedTransactions(selectedBankId);
            }
            else
            {
                var accounts = _bankService.GetAccountsByCompany(companyId);
                foreach (var a in accounts)
                    _bankTransactions.AddRange(_bankService.GetUnmatchedTransactions(a.BankAccountId));
            }

            foreach (var t in _bankTransactions)
            {
                string typeDisplay = t.TransactionType == "Credit" ? "Alacak" : "Borç";
                int rowIdx = dgvBankTransactions.Rows.Add(t.BankTransactionId, t.TransactionDate.ToString("dd.MM.yyyy"),
                    t.Description, t.Amount.ToString("N2"), typeDisplay);

                if (_preselectedBankTransactionId > 0 && t.BankTransactionId == _preselectedBankTransactionId)
                    dgvBankTransactions.Rows[rowIdx].Selected = true;
            }

            // Muhasebe kayıtları
            LoadIncomeExpenseTransactions(companyId);

            lblBankTitle.Text = $"🏦 Banka Hareketleri (Eşleştirilmemiş: {_bankTransactions.Count})";
        }

        private void LoadIncomeExpenseTransactions(int companyId, int? bankTransactionId = null)
        {
            dgvIncomeExpense.Rows.Clear();
            _ieTransactions = _incomeExpenseService.GetTransactionsByDateRange(companyId,
                DateTime.Now.AddMonths(-3), DateTime.Now);

            List<(IncomeExpenseTransaction, decimal)> suggestions = null;
            if (bankTransactionId.HasValue)
            {
                suggestions = _matchingService.GetMatchSuggestions(bankTransactionId.Value, companyId,
                    cmbBankAccount.SelectedItem != null ? ((ComboItem)cmbBankAccount.SelectedItem).Value : 0);
            }

            foreach (var t in _ieTransactions)
            {
                string paymentDisplay = t.PaymentType == "Cash" ? "Nakit" : t.PaymentType == "Bank" ? "Banka" : "Cari";
                decimal score = 0;

                if (suggestions != null)
                {
                    var match = suggestions.Find(s => s.Item1.TransactionId == t.TransactionId);
                    score = match.Item1 != null ? match.Item2 : 0;
                }

                int rowIdx = dgvIncomeExpense.Rows.Add(t.TransactionId, t.TransactionDate.ToString("dd.MM.yyyy"),
                    t.Description, t.Amount.ToString("N2"), paymentDisplay,
                    score > 0 ? $"%{score:N0}" : "-");

                if (score >= 70)
                    dgvIncomeExpense.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
                else if (score >= 40)
                    dgvIncomeExpense.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 220);
            }
        }

        private void DgvBankTransactions_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvBankTransactions.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(dgvBankTransactions.SelectedRows[0].Cells["BankTransactionId"].Value);
            string desc = dgvBankTransactions.SelectedRows[0].Cells["Description"].Value?.ToString();
            string amount = dgvBankTransactions.SelectedRows[0].Cells["Amount"].Value?.ToString();

            lblMatchInfo.Text = $"Seçili Banka Hareketi: {desc} | Tutar: {amount} ₺ | Sağ taraftan eşleştirilecek muhasebe kaydını seçin.";

            if (AppSession.CurrentCompany != null)
                LoadIncomeExpenseTransactions(AppSession.CurrentCompany.CompanyId, id);
        }

        private void ManualMatch()
        {
            if (dgvBankTransactions.SelectedRows.Count == 0 || dgvIncomeExpense.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen her iki taraftan da bir kayıt seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int bankTxId = Convert.ToInt32(dgvBankTransactions.SelectedRows[0].Cells["BankTransactionId"].Value);
            int ieTxId = Convert.ToInt32(dgvIncomeExpense.SelectedRows[0].Cells["TransactionId"].Value);

            var match = _matchingService.CreateManualMatch(bankTxId, ieTxId, AppSession.CurrentUser?.UserId);
            if (match != null)
            {
                MessageBox.Show("✓ Eşleştirme başarıyla oluşturuldu!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadData();
            }
            else
            {
                MessageBox.Show("Eşleştirme oluşturulamadı. Bu hareket zaten eşleştirilmiş olabilir.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RemoveMatch()
        {
            if (dgvBankTransactions.SelectedRows.Count == 0) return;
            int bankTxId = Convert.ToInt32(dgvBankTransactions.SelectedRows[0].Cells["BankTransactionId"].Value);

            var existingMatch = _matchingService.GetMatchByBankTransactionId(bankTxId);
            if (existingMatch == null)
            { MessageBox.Show("Bu hareket için eşleştirme bulunamadı.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (MessageBox.Show("Eşleştirmeyi kaldırmak istediğinizden emin misiniz?", "Onay",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (_matchingService.RemoveMatch(existingMatch.MatchId))
                {
                    MessageBox.Show("Eşleştirme kaldırıldı.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData();
                }
            }
        }

        private void AutoMatch()
        {
            if (AppSession.CurrentCompany == null) return;
            int selectedBankId = cmbBankAccount.SelectedItem != null ? ((ComboItem)cmbBankAccount.SelectedItem).Value : 0;

            if (selectedBankId == 0)
            { MessageBox.Show("Otomatik eşleştirme için bir banka hesabı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            Cursor = Cursors.WaitCursor;
            var matches = _matchingService.AutoMatch(AppSession.CurrentCompany.CompanyId, selectedBankId);
            Cursor = Cursors.Default;

            MessageBox.Show($"Otomatik eşleştirme tamamlandı!\n{matches.Count} yeni eşleştirme oluşturuldu.",
                "Otomatik Eşleştirme", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadData();
        }
    }
}
