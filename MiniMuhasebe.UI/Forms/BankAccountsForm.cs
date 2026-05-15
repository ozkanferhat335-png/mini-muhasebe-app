using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    /// <summary>
    /// Banka hesapları yönetimi formu
    /// </summary>
    public class BankAccountsForm : Form
    {
        private DataGridView dgvAccounts;
        private Button btnNew, btnEdit, btnDelete;
        private BankService _service;
        private List<BankAccount> _accounts;

        public BankAccountsForm()
        {
            _service = new BankService(Program.ConnectionString, Program.EncryptionKey);
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Banka Hesapları";
            this.Size = new Size(900, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 247, 250);

            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(52, 73, 94), Padding = new Padding(10, 7, 10, 7) };
            btnNew = CreateBtn("➕ Yeni Hesap", Color.FromArgb(39, 174, 96)); btnNew.Click += BtnNew_Click;
            btnEdit = CreateBtn("✏️ Düzenle", Color.FromArgb(41, 128, 185)); btnEdit.Click += BtnEdit_Click;
            btnDelete = CreateBtn("🗑️ Sil", Color.FromArgb(231, 76, 60)); btnDelete.Click += BtnDelete_Click;

            int x = 10;
            foreach (var btn in new[] { btnNew, btnEdit, btnDelete })
            { btn.Location = new Point(x, 7); pnlToolbar.Controls.Add(btn); x += btn.Width + 5; }

            dgvAccounts = new DataGridView
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
            dgvAccounts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94);
            dgvAccounts.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvAccounts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvAccounts.EnableHeadersVisualStyles = false;

            this.Controls.Add(dgvAccounts);
            this.Controls.Add(pnlToolbar);
        }

        private Button CreateBtn(string text, Color color)
        {
            var btn = new Button { Text = text, Size = new Size(130, 30), BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8, FontStyle.Bold), Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void LoadData()
        {
            if (!Program.ActiveCompanyId.HasValue) return;
            _accounts = _service.GetAccountsByCompany(Program.ActiveCompanyId.Value);

            dgvAccounts.Columns.Clear();
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "BankAccountId", HeaderText = "ID", DataPropertyName = "BankAccountId", Width = 50 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "BankName", HeaderText = "Banka Adı", DataPropertyName = "BankName" });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccountName", HeaderText = "Hesap Adı", DataPropertyName = "AccountName" });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "IBAN", HeaderText = "IBAN", DataPropertyName = "IBAN" });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Currency", HeaderText = "Para Birimi", DataPropertyName = "Currency", Width = 80 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentBalance", HeaderText = "Bakiye", DataPropertyName = "CurrentBalance", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgvAccounts.Columns.Add(new DataGridViewCheckBoxColumn { Name = "IsApiEnabled", HeaderText = "API Aktif", DataPropertyName = "IsApiEnabled", Width = 80 });
            dgvAccounts.DataSource = _accounts;
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            var form = new BankAccountEditForm(null);
            if (form.ShowDialog(this) == DialogResult.OK) LoadData();
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dgvAccounts.SelectedRows.Count == 0) { MessageBox.Show("Lütfen bir hesap seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int id = Convert.ToInt32(dgvAccounts.SelectedRows[0].Cells["BankAccountId"].Value);
            var account = _accounts.Find(a => a.BankAccountId == id);
            var form = new BankAccountEditForm(account);
            if (form.ShowDialog(this) == DialogResult.OK) LoadData();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvAccounts.SelectedRows.Count == 0) { MessageBox.Show("Lütfen bir hesap seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show("Seçili banka hesabını silmek istediğinizden emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                int id = Convert.ToInt32(dgvAccounts.SelectedRows[0].Cells["BankAccountId"].Value);
                if (_service.DeleteBankAccount(id)) LoadData();
                else MessageBox.Show("Silme başarısız.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>
    /// Banka hesabı ekleme/düzenleme formu
    /// </summary>
    public class BankAccountEditForm : Form
    {
        private TextBox txtBankName, txtAccountName, txtIBAN, txtInitialBalance;
        private TextBox txtApiBaseUrl, txtApiClientId, txtApiClientSecret, txtApiKey, txtApiUsername, txtApiPassword, txtApiAccountId;
        private ComboBox cmbCurrency, cmbApiType;
        private CheckBox chkApiEnabled;
        private Panel pnlApi;
        private Button btnSave, btnCancel;
        private BankAccount _account;
        private BankService _service;

        public BankAccountEditForm(BankAccount account)
        {
            _account = account;
            _service = new BankService(Program.ConnectionString, Program.EncryptionKey);
            InitializeComponent();
            if (_account != null) FillForm();
        }

        private void InitializeComponent()
        {
            this.Text = _account == null ? "Yeni Banka Hesabı" : "Banka Hesabı Düzenle";
            this.Size = new Size(500, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.AutoScroll = true;

            int y = 20, lx = 20, cx = 160, cw = 300;
            AddLbl("Banka Adı: *", lx, y); txtBankName = AddTxt(cx, y, cw); y += 35;
            AddLbl("Hesap Adı:", lx, y); txtAccountName = AddTxt(cx, y, cw); y += 35;
            AddLbl("IBAN:", lx, y); txtIBAN = AddTxt(cx, y, cw); y += 35;
            AddLbl("Para Birimi:", lx, y); cmbCurrency = new ComboBox { Location = new Point(cx, y), Size = new Size(100, 25), DropDownStyle = ComboBoxStyle.DropDownList }; cmbCurrency.Items.AddRange(new[] { "TRY", "USD", "EUR", "GBP" }); cmbCurrency.SelectedIndex = 0; this.Controls.Add(cmbCurrency); y += 35;
            AddLbl("Başlangıç Bakiyesi:", lx, y); txtInitialBalance = AddTxt(cx, y, 120); txtInitialBalance.Text = "0"; y += 35;

            chkApiEnabled = new CheckBox { Text = "API Entegrasyonu Aktif", Location = new Point(lx, y), Size = new Size(250, 25), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            chkApiEnabled.CheckedChanged += (s, e) => pnlApi.Visible = chkApiEnabled.Checked;
            this.Controls.Add(chkApiEnabled); y += 35;

            pnlApi = new Panel { Location = new Point(lx, y), Size = new Size(440, 220), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(248, 249, 250), Visible = false };
            int py = 10;
            AddPanelLbl(pnlApi, "API Türü:", 10, py); cmbApiType = new ComboBox { Location = new Point(130, py), Size = new Size(150, 25), DropDownStyle = ComboBoxStyle.DropDownList }; cmbApiType.Items.AddRange(new[] { "REST", "SOAP", "OpenBanking", "Mock" }); cmbApiType.SelectedIndex = 0; pnlApi.Controls.Add(cmbApiType); py += 30;
            AddPanelLbl(pnlApi, "API Base URL:", 10, py); txtApiBaseUrl = AddPanelTxt(pnlApi, 130, py, 290); py += 30;
            AddPanelLbl(pnlApi, "Client ID:", 10, py); txtApiClientId = AddPanelTxt(pnlApi, 130, py, 290); py += 30;
            AddPanelLbl(pnlApi, "Client Secret:", 10, py); txtApiClientSecret = AddPanelTxt(pnlApi, 130, py, 290); txtApiClientSecret.PasswordChar = '●'; py += 30;
            AddPanelLbl(pnlApi, "API Key:", 10, py); txtApiKey = AddPanelTxt(pnlApi, 130, py, 290); txtApiKey.PasswordChar = '●'; py += 30;
            AddPanelLbl(pnlApi, "Kullanıcı Adı:", 10, py); txtApiUsername = AddPanelTxt(pnlApi, 130, py, 290); py += 30;
            AddPanelLbl(pnlApi, "Şifre:", 10, py); txtApiPassword = AddPanelTxt(pnlApi, 130, py, 290); txtApiPassword.PasswordChar = '●'; py += 30;
            AddPanelLbl(pnlApi, "Hesap ID:", 10, py); txtApiAccountId = AddPanelTxt(pnlApi, 130, py, 290);
            pnlApi.Height = py + 35;
            this.Controls.Add(pnlApi);
            y += pnlApi.Height + 10;

            btnSave = new Button { Text = "💾 Kaydet", Location = new Point(cx, y), Size = new Size(120, 35), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            btnCancel = new Button { Text = "İptal", Location = new Point(cx + 130, y), Size = new Size(80, 35), BackColor = Color.FromArgb(127, 140, 141), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
            this.Height = y + 80;
        }

        private void AddLbl(string t, int x, int y) { var l = new Label { Text = t, Location = new Point(x, y + 3), Size = new Size(135, 20), Font = new Font("Segoe UI", 9) }; this.Controls.Add(l); }
        private TextBox AddTxt(int x, int y, int w) { var t = new TextBox { Location = new Point(x, y), Size = new Size(w, 25), Font = new Font("Segoe UI", 9) }; this.Controls.Add(t); return t; }
        private void AddPanelLbl(Panel p, string t, int x, int y) { var l = new Label { Text = t, Location = new Point(x, y + 3), Size = new Size(115, 20), Font = new Font("Segoe UI", 8) }; p.Controls.Add(l); }
        private TextBox AddPanelTxt(Panel p, int x, int y, int w) { var t = new TextBox { Location = new Point(x, y), Size = new Size(w, 22), Font = new Font("Segoe UI", 8) }; p.Controls.Add(t); return t; }

        private void FillForm()
        {
            txtBankName.Text = _account.BankName;
            txtAccountName.Text = _account.AccountName;
            txtIBAN.Text = _account.IBAN;
            cmbCurrency.SelectedItem = _account.Currency;
            txtInitialBalance.Text = _account.InitialBalance.ToString("N2");
            chkApiEnabled.Checked = _account.IsApiEnabled;
            if (_account.IsApiEnabled)
            {
                cmbApiType.SelectedItem = _account.ApiProviderType;
                txtApiBaseUrl.Text = _account.ApiBaseUrl;
                txtApiClientId.Text = _account.ApiClientId;
                txtApiUsername.Text = _account.ApiUsername;
                txtApiAccountId.Text = _account.ApiAccountId;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBankName.Text)) { MessageBox.Show("Banka adı zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!Program.ActiveCompanyId.HasValue) return;

            decimal.TryParse(txtInitialBalance.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal initialBalance);

            if (_account == null)
            {
                var result = _service.CreateBankAccount(Program.ActiveCompanyId.Value, txtBankName.Text.Trim(),
                    txtAccountName.Text.Trim(), txtIBAN.Text.Trim(), cmbCurrency.SelectedItem?.ToString() ?? "TRY",
                    initialBalance, chkApiEnabled.Checked,
                    chkApiEnabled.Checked ? cmbApiType.SelectedItem?.ToString() : null,
                    chkApiEnabled.Checked ? txtApiBaseUrl.Text.Trim() : null,
                    chkApiEnabled.Checked ? txtApiClientId.Text.Trim() : null,
                    chkApiEnabled.Checked ? txtApiClientSecret.Text.Trim() : null,
                    chkApiEnabled.Checked ? txtApiKey.Text.Trim() : null,
                    chkApiEnabled.Checked ? txtApiUsername.Text.Trim() : null,
                    chkApiEnabled.Checked ? txtApiPassword.Text.Trim() : null,
                    chkApiEnabled.Checked ? txtApiAccountId.Text.Trim() : null);
                if (result != null) { this.DialogResult = DialogResult.OK; this.Close(); }
                else MessageBox.Show("Kayıt eklenemedi.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                _account.BankName = txtBankName.Text.Trim();
                _account.AccountName = txtAccountName.Text.Trim();
                _account.IBAN = txtIBAN.Text.Trim();
                _account.Currency = cmbCurrency.SelectedItem?.ToString() ?? "TRY";
                _account.IsApiEnabled = chkApiEnabled.Checked;
                if (chkApiEnabled.Checked)
                {
                    _account.ApiProviderType = cmbApiType.SelectedItem?.ToString();
                    _account.ApiBaseUrl = txtApiBaseUrl.Text.Trim();
                    _account.ApiClientId = txtApiClientId.Text.Trim();
                    if (!string.IsNullOrEmpty(txtApiClientSecret.Text)) _account.ApiClientSecret = txtApiClientSecret.Text.Trim();
                    if (!string.IsNullOrEmpty(txtApiKey.Text)) _account.ApiKey = txtApiKey.Text.Trim();
                    _account.ApiUsername = txtApiUsername.Text.Trim();
                    if (!string.IsNullOrEmpty(txtApiPassword.Text)) _account.ApiPassword = txtApiPassword.Text.Trim();
                    _account.ApiAccountId = txtApiAccountId.Text.Trim();
                }
                if (_service.UpdateBankAccount(_account)) { this.DialogResult = DialogResult.OK; this.Close(); }
                else MessageBox.Show("Güncelleme başarısız.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
