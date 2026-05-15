using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    /// <summary>
    /// Banka hareketleri formu
    /// </summary>
    public class BankTransactionsForm : Form
    {
        private DataGridView dgvTransactions;
        private ComboBox cmbBankAccount;
        private DateTimePicker dtpStart, dtpEnd;
        private Button btnFetch, btnAutoMatch, btnRefresh;
        private Label lblSummary;
        private BankService _bankService;
        private MatchingService _matchingService;
        private List<BankTransaction> _transactions;

        public BankTransactionsForm()
        {
            _bankService = new BankService(Program.ConnectionString, Program.EncryptionKey);
            _matchingService = new MatchingService(Program.ConnectionString);
            InitializeComponent();
            LoadBankAccounts();
        }

        private void InitializeComponent()
        {
            this.Text = "Banka Hareketleri";
            this.Size = new Size(1000, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 247, 250);

            // Filtre paneli
            var pnlFilter = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.White, Padding = new Padding(10, 10, 10, 5) };
            var lblAcc = new Label { Text = "Banka Hesabı:", Location = new Point(10, 15), Size = new Size(90, 20) };
            cmbBankAccount = new ComboBox { Location = new Point(105, 12), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            var lblS = new Label { Text = "Başlangıç:", Location = new Point(320, 15), Size = new Size(70, 20) };
            dtpStart = new DateTimePicker { Location = new Point(395, 12), Size = new Size(130, 25), Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1) };
            var lblE = new Label { Text = "Bitiş:", Location = new Point(540, 15), Size = new Size(40, 20) };
            dtpEnd = new DateTimePicker { Location = new Point(585, 12), Size = new Size(130, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Now };
            btnRefresh = new Button { Text = "Listele", Location = new Point(730, 10), Size = new Size(80, 28), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => LoadTransactions();
            pnlFilter.Controls.AddRange(new Control[] { lblAcc, cmbBankAccount, lblS, dtpStart, lblE, dtpEnd, btnRefresh });

            // Araç çubuğu
            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(52, 73, 94), Padding = new Padding(10, 7, 10, 7) };
            btnFetch = CreateBtn("📥 API'den Çek", Color.FromArgb(142, 68, 173));
            btnFetch.Click += BtnFetch_Click;
            btnAutoMatch = CreateBtn("🔗 Otomatik Eşleştir", Color.FromArgb(39, 174, 96));
            btnAutoMatch.Click += BtnAutoMatch_Click;

            int x = 10;
            foreach (var btn in new[] { btnFetch, btnAutoMatch })
            { btn.Location = new Point(x, 7); pnlToolbar.Controls.Add(btn); x += btn.Width + 5; }

            // Grid
            dgvTransactions = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9),
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(248, 249, 250) }
            };
            dgvTransactions.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94);
            dgvTransactions.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvTransactions.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvTransactions.EnableHeadersVisualStyles = false;

            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 35, BackColor = Color.FromArgb(52, 73, 94) };
            lblSummary = new Label { ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(10, 8), Size = new Size(800, 20) };
            pnlBottom.Controls.Add(lblSummary);

            this.Controls.Add(dgvTransactions);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlFilter);
            this.Controls.Add(pnlToolbar);
        }

        private Button CreateBtn(string text, Color color)
        {
            var btn = new Button { Text = text, Size = new Size(155, 30), BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8, FontStyle.Bold), Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void LoadBankAccounts()
        {
            if (!Program.ActiveCompanyId.HasValue) return;
            var accounts = _bankService.GetAccountsByCompany(Program.ActiveCompanyId.Value);
            cmbBankAccount.DataSource = accounts;
            cmbBankAccount.DisplayMember = "BankName";
            cmbBankAccount.ValueMember = "BankAccountId";
        }

        private void LoadTransactions()
        {
            if (cmbBankAccount.SelectedValue == null) return;
            int bankAccountId = Convert.ToInt32(cmbBankAccount.SelectedValue);
            _transactions = _bankService.GetTransactionsByDateRange(bankAccountId, dtpStart.Value, dtpEnd.Value);

            dgvTransactions.Columns.Clear();
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "BankTransactionId", HeaderText = "ID", DataPropertyName = "BankTransactionId", Width = 50 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransactionDate", HeaderText = "Tarih", DataPropertyName = "TransactionDate", DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Açıklama", DataPropertyName = "Description" });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Tutar (₺)", DataPropertyName = "Amount", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransactionType", HeaderText = "Tür", DataPropertyName = "TransactionType", Width = 80 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Durum", DataPropertyName = "Status", Width = 90 });
            dgvTransactions.Columns.Add(new DataGridViewCheckBoxColumn { Name = "IsMatched", HeaderText = "Eşleşti", DataPropertyName = "IsMatched", Width = 70 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "ReferenceNumber", HeaderText = "Referans No", DataPropertyName = "ReferenceNumber" });
            dgvTransactions.DataSource = _transactions;

            // Eşleşmemiş satırları kırmızı göster
            foreach (DataGridViewRow row in dgvTransactions.Rows)
            {
                if (row.DataBoundItem is BankTransaction tx && !tx.IsMatched)
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(231, 76, 60);
            }

            int matched = _transactions.FindAll(t => t.IsMatched).Count;
            lblSummary.Text = $"Toplam: {_transactions.Count} hareket | Eşleşen: {matched} | Eşleşmeyen: {_transactions.Count - matched}";
        }

        private void BtnFetch_Click(object sender, EventArgs e)
        {
            if (cmbBankAccount.SelectedValue == null) { MessageBox.Show("Lütfen banka hesabı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            int bankAccountId = Convert.ToInt32(cmbBankAccount.SelectedValue);
            var account = _bankService.GetAccountById(bankAccountId);

            if (account == null || !account.IsApiEnabled)
            {
                MessageBox.Show("Bu hesap için API entegrasyonu aktif değil.\nBanka hesabı ayarlarından API'yi etkinleştirin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Mock API ile test
            var apiSettings = new Integration.Interfaces.BankApiSettings
            {
                ApiBaseUrl = account.ApiBaseUrl,
                ApiClientId = account.ApiClientId,
                ApiClientSecret = account.ApiClientSecret,
                ApiKey = account.ApiKey,
                ApiUsername = account.ApiUsername,
                ApiPassword = account.ApiPassword,
                ApiAccountId = account.ApiAccountId,
                ApiProviderType = account.ApiProviderType
            };

            bool useMock = account.ApiProviderType == "Mock" || string.IsNullOrEmpty(account.ApiBaseUrl);
            var provider = Integration.BankingAPIs.BankApiProviderFactory.Create(account.ApiProviderType, useMock);

            if (!provider.Connect(apiSettings))
            {
                MessageBox.Show("API bağlantısı kurulamadı. Ayarları kontrol edin.", "Bağlantı Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var dtos = provider.FetchTransactions(account.ApiAccountId, dtpStart.Value, dtpEnd.Value);
            int added = 0;
            foreach (var dto in dtos)
            {
                var tx = new BankTransaction
                {
                    BankAccountId = bankAccountId,
                    TransactionDate = dto.TransactionDate,
                    Amount = dto.Amount,
                    Description = dto.Description,
                    Balance = dto.Balance,
                    ReferenceNumber = dto.ReferenceNumber,
                    BankTransactionId_External = dto.ExternalId,
                    TransactionType = dto.TransactionType,
                    SyncedAt = DateTime.Now
                };
                var result = _bankService.AddBankTransaction(bankAccountId, tx);
                if (result != null) added++;
            }

            provider.Disconnect();
            MessageBox.Show($"İşlem tamamlandı.\n{dtos.Count} hareket alındı, {added} yeni kayıt eklendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadTransactions();
        }

        private void BtnAutoMatch_Click(object sender, EventArgs e)
        {
            if (!Program.ActiveCompanyId.HasValue || cmbBankAccount.SelectedValue == null) return;
            int bankAccountId = Convert.ToInt32(cmbBankAccount.SelectedValue);

            var result = _matchingService.AutoMatch(Program.ActiveCompanyId.Value, bankAccountId, Program.CurrentUserId);
            MessageBox.Show($"Otomatik eşleştirme tamamlandı.\nEşleştirilen: {result.MatchedCount} / {result.TotalProcessed}", "Sonuç", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadTransactions();
        }
    }
}
