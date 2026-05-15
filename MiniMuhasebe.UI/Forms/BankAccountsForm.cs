using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Integration.BankingAPIs;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    public class BankAccountsForm : Form
    {
        private DataGridView dgvAccounts;
        private Panel pnlToolbar;
        private Panel pnlDetail;
        private Button btnNew, btnEdit, btnDelete, btnRefresh, btnTestApi, btnSync;
        private Label lblDetail;

        private readonly BankService _bankService;
        private readonly BankApiClient _apiClient;
        private List<BankAccount> _accounts;

        public BankAccountsForm()
        {
            _bankService = new BankService(AppSession.ConnectionString, AppSession.EncryptionKey);
            _apiClient = new BankApiClient();
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Banka Hesapları";
            this.BackColor = Color.FromArgb(245, 247, 250);

            pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.White };

            var lblTitle = new Label
            {
                Text = "🏦 Banka Hesapları",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(10, 15)
            };

            btnNew = CreateBtn("➕ Yeni Hesap", Color.FromArgb(39, 174, 96), 200, 12);
            btnNew.Click += (s, e) => OpenEditDialog(null);

            btnEdit = CreateBtn("✏️ Düzenle", Color.FromArgb(41, 128, 185), 295, 12);
            btnEdit.Click += (s, e) => EditSelected();

            btnDelete = CreateBtn("🗑️ Sil", Color.FromArgb(192, 57, 43), 385, 12);
            btnDelete.Click += (s, e) => DeleteSelected();

            btnRefresh = CreateBtn("🔄 Yenile", Color.FromArgb(127, 140, 141), 475, 12);
            btnRefresh.Click += (s, e) => LoadData();

            btnTestApi = CreateBtn("🔌 API Test", Color.FromArgb(142, 68, 173), 565, 12);
            btnTestApi.Click += (s, e) => TestApiConnection();

            btnSync = CreateBtn("⬇️ Hareketleri Çek", Color.FromArgb(230, 126, 34), 655, 12);
            btnSync.Width = 140;
            btnSync.Click += (s, e) => SyncTransactions();

            pnlToolbar.Controls.AddRange(new Control[] { lblTitle, btnNew, btnEdit, btnDelete, btnRefresh, btnTestApi, btnSync });

            // Detail panel
            pnlDetail = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 120,
                BackColor = Color.White,
                Padding = new Padding(15)
            };

            lblDetail = new Label
            {
                Text = "Banka hesabı seçin...",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.Gray,
                Dock = DockStyle.Fill,
                AutoSize = false
            };
            pnlDetail.Controls.Add(lblDetail);

            // Grid
            dgvAccounts = new DataGridView
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
            dgvAccounts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvAccounts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgvAccounts.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvAccounts.ColumnHeadersHeight = 35;

            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "BankAccountId", HeaderText = "ID", Width = 50, FillWeight = 5 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "BankName", HeaderText = "Banka Adı", FillWeight = 20 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccountName", HeaderText = "Hesap Adı", FillWeight = 20 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "IBAN", HeaderText = "IBAN", FillWeight = 25 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Currency", HeaderText = "Para Birimi", FillWeight = 10 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "InitialBalance", HeaderText = "Başlangıç Bakiyesi", FillWeight = 15 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentBalance", HeaderText = "Güncel Bakiye (₺)", FillWeight = 15 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ApiEnabled", HeaderText = "API", FillWeight = 8 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastSync", HeaderText = "Son Senkronizasyon", FillWeight = 15 });

            dgvAccounts.SelectionChanged += DgvAccounts_SelectionChanged;
            dgvAccounts.DoubleClick += (s, e) => EditSelected();

            this.Controls.AddRange(new Control[] { dgvAccounts, pnlDetail, pnlToolbar });
        }

        private Button CreateBtn(string text, Color color, int x, int y)
        {
            var btn = new Button
            {
                Text = text, Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White, BackColor = color, FlatStyle = FlatStyle.Flat,
                Location = new Point(x, y), Size = new Size(90, 30), Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void LoadData()
        {
            if (AppSession.CurrentCompany == null) return;
            _accounts = _bankService.GetAccountsByCompany(AppSession.CurrentCompany.CompanyId);

            dgvAccounts.Rows.Clear();
            foreach (var a in _accounts)
            {
                dgvAccounts.Rows.Add(
                    a.BankAccountId, a.BankName, a.AccountName, a.IBAN, a.Currency,
                    a.InitialBalance.ToString("N2"), a.CurrentBalance.ToString("N2"),
                    a.IsApiEnabled ? "✓ Aktif" : "✗",
                    a.LastSyncAt?.ToString("dd.MM.yyyy HH:mm") ?? "-"
                );
            }
        }

        private void DgvAccounts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvAccounts.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(dgvAccounts.SelectedRows[0].Cells["BankAccountId"].Value);
            var account = _accounts.Find(a => a.BankAccountId == id);
            if (account == null) return;

            lblDetail.Text = $"Banka: {account.BankName}  |  Hesap: {account.AccountName}  |  IBAN: {account.IBAN}\n" +
                            $"Para Birimi: {account.Currency}  |  Başlangıç Bakiyesi: {account.InitialBalance:N2} ₺  |  Güncel Bakiye: {account.CurrentBalance:N2} ₺\n" +
                            $"API Durumu: {(account.IsApiEnabled ? "Aktif" : "Pasif")}  |  API Tipi: {account.ApiProviderType ?? "-"}  |  Son Senkronizasyon: {account.LastSyncAt?.ToString("dd.MM.yyyy HH:mm") ?? "Hiç"}";
        }

        private void OpenEditDialog(BankAccount existing)
        {
            if (AppSession.CurrentCompany == null) return;
            var dialog = new BankAccountEditDialog(existing, AppSession.CurrentCompany.CompanyId);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var data = dialog.AccountData;
                if (existing == null)
                    _bankService.CreateBankAccount(data.CompanyId, data.BankName, data.AccountName, data.IBAN,
                        data.Currency, data.InitialBalance, data.IsApiEnabled, data.ApiProviderType,
                        data.ApiBaseUrl, data.ApiClientId, data.ApiClientSecret, data.ApiKey,
                        data.ApiUsername, data.ApiPassword, data.ApiAccountId);
                else
                    _bankService.UpdateBankAccount(data);
                LoadData();
            }
        }

        private void EditSelected()
        {
            if (dgvAccounts.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(dgvAccounts.SelectedRows[0].Cells["BankAccountId"].Value);
            var account = _accounts.Find(a => a.BankAccountId == id);
            if (account != null) OpenEditDialog(account);
        }

        private void DeleteSelected()
        {
            if (dgvAccounts.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(dgvAccounts.SelectedRows[0].Cells["BankAccountId"].Value);
            if (MessageBox.Show("Bu banka hesabını silmek istediğinizden emin misiniz?", "Silme Onayı",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                if (_bankService.DeleteBankAccount(id)) LoadData();
            }
        }

        private void TestApiConnection()
        {
            if (dgvAccounts.SelectedRows.Count == 0)
            { MessageBox.Show("Lütfen bir banka hesabı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            int id = Convert.ToInt32(dgvAccounts.SelectedRows[0].Cells["BankAccountId"].Value);
            var account = _accounts.Find(a => a.BankAccountId == id);
            if (account == null) return;

            if (!account.IsApiEnabled)
            { MessageBox.Show("Bu hesap için API entegrasyonu aktif değil.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            Cursor = Cursors.WaitCursor;
            bool success = _apiClient.TestConnection(account);
            Cursor = Cursors.Default;

            if (success)
                MessageBox.Show("✓ API bağlantısı başarılı!", "Bağlantı Testi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("✗ API bağlantısı başarısız!", "Bağlantı Testi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void SyncTransactions()
        {
            if (dgvAccounts.SelectedRows.Count == 0)
            { MessageBox.Show("Lütfen bir banka hesabı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            int id = Convert.ToInt32(dgvAccounts.SelectedRows[0].Cells["BankAccountId"].Value);
            var account = _accounts.Find(a => a.BankAccountId == id);
            if (account == null) return;

            if (!account.IsApiEnabled)
            { MessageBox.Show("Bu hesap için API entegrasyonu aktif değil.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            var startDate = DateTime.Now.AddDays(-30);
            var endDate = DateTime.Now;

            Cursor = Cursors.WaitCursor;
            var newTransactions = _apiClient.SyncTransactions(account, startDate, endDate, new List<string>());
            Cursor = Cursors.Default;

            int added = 0;
            foreach (var tx in newTransactions)
            {
                var result = _bankService.AddBankTransaction(account.BankAccountId, tx);
                if (result != null) added++;
            }

            account.LastSyncAt = DateTime.Now;
            _bankService.UpdateBankAccount(account);
            LoadData();

            MessageBox.Show($"Senkronizasyon tamamlandı!\n{added} yeni hareket eklendi.", "Senkronizasyon",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    public class BankAccountEditDialog : Form
    {
        public BankAccount AccountData { get; private set; }
        private TextBox txtBankName, txtAccountName, txtIBAN, txtInitialBalance;
        private TextBox txtApiBaseUrl, txtApiClientId, txtApiClientSecret, txtApiKey, txtApiUsername, txtApiPassword, txtApiAccountId;
        private ComboBox cmbCurrency, cmbApiType;
        private CheckBox chkApiEnabled;
        private Panel pnlApi;

        public BankAccountEditDialog(BankAccount existing, int companyId)
        {
            AccountData = existing ?? new BankAccount { CompanyId = companyId, Currency = "TRY", IsActive = true };
            InitializeComponent();
            if (existing != null) PopulateFields(existing);
            UpdateApiPanel();
        }

        private void InitializeComponent()
        {
            this.Text = AccountData.BankAccountId == 0 ? "Yeni Banka Hesabı" : "Banka Hesabı Düzenle";
            this.Size = new Size(520, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
            this.AutoScroll = true;

            int y = 20, lx = 20, cx = 160, w = 320;

            AddLabel("Banka Adı:", lx, y); txtBankName = AddTB(cx, y, w, AccountData.BankName); y += 40;
            AddLabel("Hesap Adı:", lx, y); txtAccountName = AddTB(cx, y, w, AccountData.AccountName); y += 40;
            AddLabel("IBAN:", lx, y); txtIBAN = AddTB(cx, y, w, AccountData.IBAN); y += 40;
            AddLabel("Para Birimi:", lx, y);
            cmbCurrency = new ComboBox { Location = new Point(cx, y), Size = new Size(100, 25), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9) };
            cmbCurrency.Items.AddRange(new object[] { "TRY", "USD", "EUR", "GBP" });
            cmbCurrency.SelectedItem = AccountData.Currency ?? "TRY";
            this.Controls.Add(cmbCurrency); y += 40;
            AddLabel("Başlangıç Bakiyesi:", lx, y); txtInitialBalance = AddTB(cx, y, 150, AccountData.InitialBalance.ToString("N2")); y += 40;

            chkApiEnabled = new CheckBox { Text = "API Entegrasyonu Aktif", Font = new Font("Segoe UI", 10), Location = new Point(lx, y), AutoSize = true, Checked = AccountData.IsApiEnabled };
            chkApiEnabled.CheckedChanged += (s, e) => UpdateApiPanel();
            this.Controls.Add(chkApiEnabled); y += 35;

            // API Panel
            pnlApi = new Panel { Location = new Point(lx, y), Size = new Size(460, 250), BorderStyle = BorderStyle.FixedSingle };

            int ay = 10, alx = 10, acx = 150, aw = 290;
            AddLabelTo(pnlApi, "API Tipi:", alx, ay);
            cmbApiType = new ComboBox { Location = new Point(acx, ay), Size = new Size(150, 25), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9) };
            cmbApiType.Items.AddRange(new object[] { "Mock", "REST", "SOAP", "OpenBanking" });
            cmbApiType.SelectedItem = AccountData.ApiProviderType ?? "Mock";
            pnlApi.Controls.Add(cmbApiType); ay += 35;

            AddLabelTo(pnlApi, "API URL:", alx, ay); txtApiBaseUrl = AddTBTo(pnlApi, acx, ay, aw, AccountData.ApiBaseUrl); ay += 35;
            AddLabelTo(pnlApi, "Client ID:", alx, ay); txtApiClientId = AddTBTo(pnlApi, acx, ay, aw, AccountData.ApiClientId); ay += 35;
            AddLabelTo(pnlApi, "Client Secret:", alx, ay); txtApiClientSecret = AddTBTo(pnlApi, acx, ay, aw, AccountData.ApiClientSecret); ay += 35;
            AddLabelTo(pnlApi, "API Key:", alx, ay); txtApiKey = AddTBTo(pnlApi, acx, ay, aw, AccountData.ApiKey); ay += 35;
            AddLabelTo(pnlApi, "Kullanıcı Adı:", alx, ay); txtApiUsername = AddTBTo(pnlApi, acx, ay, aw, AccountData.ApiUsername); ay += 35;
            AddLabelTo(pnlApi, "Şifre:", alx, ay); txtApiPassword = AddTBTo(pnlApi, acx, ay, aw, AccountData.ApiPassword); txtApiPassword.PasswordChar = '●'; ay += 35;

            this.Controls.Add(pnlApi);
            y += 260;

            var btnOk = new Button { Text = "Kaydet", DialogResult = DialogResult.OK, Location = new Point(cx, y), Size = new Size(100, 35), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += BtnOk_Click;
            var btnCancel = new Button { Text = "İptal", DialogResult = DialogResult.Cancel, Location = new Point(cx + 110, y), Size = new Size(100, 35), BackColor = Color.FromArgb(127, 140, 141), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.FlatAppearance.BorderSize = 0;
            this.Controls.AddRange(new Control[] { btnOk, btnCancel });
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void UpdateApiPanel() => pnlApi.Enabled = chkApiEnabled.Checked;

        private void PopulateFields(BankAccount a)
        {
            txtBankName.Text = a.BankName; txtAccountName.Text = a.AccountName; txtIBAN.Text = a.IBAN;
            txtInitialBalance.Text = a.InitialBalance.ToString("N2");
            if (cmbCurrency.Items.Contains(a.Currency)) cmbCurrency.SelectedItem = a.Currency;
            txtApiBaseUrl.Text = a.ApiBaseUrl; txtApiClientId.Text = a.ApiClientId;
            txtApiClientSecret.Text = a.ApiClientSecret; txtApiKey.Text = a.ApiKey;
            txtApiUsername.Text = a.ApiUsername; txtApiPassword.Text = a.ApiPassword;
            if (cmbApiType.Items.Contains(a.ApiProviderType)) cmbApiType.SelectedItem = a.ApiProviderType;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBankName.Text)) { MessageBox.Show("Banka adı gereklidir.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!decimal.TryParse(txtInitialBalance.Text.Replace(".", "").Replace(",", "."), out decimal balance)) balance = 0;

            AccountData.BankName = txtBankName.Text.Trim();
            AccountData.AccountName = txtAccountName.Text.Trim();
            AccountData.IBAN = txtIBAN.Text.Trim();
            AccountData.Currency = cmbCurrency.SelectedItem?.ToString() ?? "TRY";
            AccountData.InitialBalance = balance;
            AccountData.CurrentBalance = AccountData.BankAccountId == 0 ? balance : AccountData.CurrentBalance;
            AccountData.IsApiEnabled = chkApiEnabled.Checked;
            AccountData.ApiProviderType = cmbApiType.SelectedItem?.ToString();
            AccountData.ApiBaseUrl = txtApiBaseUrl.Text.Trim();
            AccountData.ApiClientId = txtApiClientId.Text.Trim();
            AccountData.ApiClientSecret = txtApiClientSecret.Text.Trim();
            AccountData.ApiKey = txtApiKey.Text.Trim();
            AccountData.ApiUsername = txtApiUsername.Text.Trim();
            AccountData.ApiPassword = txtApiPassword.Text.Trim();
            this.DialogResult = DialogResult.OK;
        }

        private void AddLabel(string text, int x, int y) => this.Controls.Add(new Label { Text = text, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(44, 62, 80), AutoSize = true, Location = new Point(x, y + 3) });
        private void AddLabelTo(Control parent, string text, int x, int y) => parent.Controls.Add(new Label { Text = text, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(44, 62, 80), AutoSize = true, Location = new Point(x, y + 3) });
        private TextBox AddTB(int x, int y, int w, string text = "") { var tb = new TextBox { Location = new Point(x, y), Size = new Size(w, 25), Font = new Font("Segoe UI", 9), Text = text ?? "" }; this.Controls.Add(tb); return tb; }
        private TextBox AddTBTo(Control parent, int x, int y, int w, string text = "") { var tb = new TextBox { Location = new Point(x, y), Size = new Size(w, 25), Font = new Font("Segoe UI", 9), Text = text ?? "" }; parent.Controls.Add(tb); return tb; }
    }
}
