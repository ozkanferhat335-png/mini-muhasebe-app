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
        private Button btnAdd, btnEdit, btnDelete, btnRefresh, btnSync, btnTestApi;
        private Label lblTotal;

        private readonly User _currentUser;
        private readonly Company _activeCompany;
        private readonly BankService _bankService;
        private List<BankAccount> _accounts;

        public BankAccountsForm(User user, Company company)
        {
            _currentUser = user;
            _activeCompany = company;
            _bankService = new BankService(Program.ConnectionString, Program.EncryptionKey);

            InitializeComponent();
            LoadAccounts();
        }

        private void InitializeComponent()
        {
            this.Text = "Banka Hesapları";
            this.Size = new Size(1050, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 9f);

            pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.White, Padding = new Padding(8) };

            btnAdd = CreateBtn("➕ Yeni Hesap", Color.FromArgb(39, 174, 96), 10);
            btnAdd.Click += BtnAdd_Click;

            btnEdit = CreateBtn("✏️ Düzenle", Color.FromArgb(41, 128, 185), 130);
            btnEdit.Click += BtnEdit_Click;

            btnDelete = CreateBtn("🗑️ Sil", Color.FromArgb(231, 76, 60), 250);
            btnDelete.Click += BtnDelete_Click;

            btnSync = CreateBtn("🔄 API Senkronize", Color.FromArgb(142, 68, 173), 370);
            btnSync.Click += BtnSync_Click;

            btnTestApi = CreateBtn("🔌 API Test", Color.FromArgb(243, 156, 18), 500);
            btnTestApi.Click += BtnTestApi_Click;

            btnRefresh = CreateBtn("🔄 Yenile", Color.FromArgb(149, 165, 166), 630);
            btnRefresh.Click += (s, e) => LoadAccounts();

            lblTotal = new Label
            {
                Text = "Toplam Bakiye: ₺0,00",
                AutoSize = true,
                Location = new Point(760, 18),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(41, 128, 185)
            };

            pnlToolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnSync, btnTestApi, btnRefresh, lblTotal });

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
                Font = new Font("Segoe UI", 9f),
                GridColor = Color.FromArgb(230, 230, 230),
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(248, 249, 250) }
            };
            dgvAccounts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(41, 128, 185);
            dgvAccounts.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvAccounts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgvAccounts.EnableHeadersVisualStyles = false;

            dgvAccounts.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", FillWeight = 5 },
                new DataGridViewTextBoxColumn { Name = "colBank", HeaderText = "Banka Adı", FillWeight = 18 },
                new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Hesap Adı", FillWeight = 15 },
                new DataGridViewTextBoxColumn { Name = "colIban", HeaderText = "IBAN", FillWeight = 22 },
                new DataGridViewTextBoxColumn { Name = "colCurrency", HeaderText = "Para Birimi", FillWeight = 8 },
                new DataGridViewTextBoxColumn { Name = "colInitial", HeaderText = "Başlangıç Bakiye", FillWeight = 12, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "colCurrent", HeaderText = "Güncel Bakiye", FillWeight = 12, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "colApi", HeaderText = "API Durumu", FillWeight = 10 },
                new DataGridViewTextBoxColumn { Name = "colLastSync", HeaderText = "Son Senkronizasyon", FillWeight = 15 }
            });

            this.Controls.AddRange(new Control[] { dgvAccounts, pnlToolbar });
        }

        private Button CreateBtn(string text, Color color, int x)
        {
            return new Button
            {
                Text = text,
                Size = new Size(115, 35),
                Location = new Point(x, 8),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
        }

        private void LoadAccounts()
        {
            dgvAccounts.Rows.Clear();
            if (_activeCompany == null) return;

            _accounts = _bankService.GetAccountsByCompany(_activeCompany.CompanyId);
            decimal totalBalance = 0;

            foreach (var acc in _accounts)
            {
                dgvAccounts.Rows.Add(
                    acc.BankAccountId,
                    acc.BankName,
                    acc.AccountName,
                    acc.IBAN,
                    acc.Currency,
                    $"₺{acc.InitialBalance:N2}",
                    $"₺{acc.CurrentBalance:N2}",
                    acc.IsApiEnabled ? "✅ Aktif" : "❌ Pasif",
                    acc.LastSyncAt?.ToString("dd.MM.yyyy HH:mm") ?? "Hiç senkronize edilmedi"
                );
                totalBalance += acc.CurrentBalance;
            }

            lblTotal.Text = $"Toplam Bakiye: ₺{totalBalance:N2}";
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var dlg = new BankAccountEditForm(_activeCompany, null))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    LoadAccounts();
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dgvAccounts.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(dgvAccounts.SelectedRows[0].Cells["colId"].Value);
            var acc = _accounts?.Find(a => a.BankAccountId == id);
            if (acc == null) return;

            using (var dlg = new BankAccountEditForm(_activeCompany, acc))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    LoadAccounts();
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvAccounts.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(dgvAccounts.SelectedRows[0].Cells["colId"].Value);
            string bankName = dgvAccounts.SelectedRows[0].Cells["colBank"].Value?.ToString();

            if (MessageBox.Show($"'{bankName}' hesabını silmek istediğinizden emin misiniz?",
                "Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (_bankService.DeleteBankAccount(id))
                    LoadAccounts();
                else
                    MessageBox.Show("Silme işlemi başarısız.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSync_Click(object sender, EventArgs e)
        {
            if (dgvAccounts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen senkronize edilecek hesabı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = Convert.ToInt32(dgvAccounts.SelectedRows[0].Cells["colId"].Value);
            var acc = _accounts?.Find(a => a.BankAccountId == id);
            if (acc == null) return;

            if (!acc.IsApiEnabled)
            {
                MessageBox.Show("Bu hesap için API entegrasyonu etkin değil.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                var provider = BankApiClientFactory.CreateProvider(acc);
                var transactions = provider.FetchTransactions(acc, DateTime.Now.AddDays(-30), DateTime.Now);

                int added = 0;
                foreach (var tx in transactions)
                {
                    var result = _bankService.AddBankTransaction(acc.BankAccountId, tx);
                    if (result != null) added++;
                }

                // Bakiyeyi güncelle
                decimal? newBalance = provider.FetchCurrentBalance(acc);
                if (newBalance.HasValue)
                    _bankService.UpdateBankBalance(acc.BankAccountId, newBalance.Value);

                acc.LastSyncAt = DateTime.Now;
                _bankService.UpdateBankAccount(acc);

                LoadAccounts();
                MessageBox.Show($"Senkronizasyon tamamlandı.\n{added} yeni hareket eklendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Senkronizasyon sırasında hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void BtnTestApi_Click(object sender, EventArgs e)
        {
            if (dgvAccounts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen test edilecek hesabı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int id = Convert.ToInt32(dgvAccounts.SelectedRows[0].Cells["colId"].Value);
            var acc = _accounts?.Find(a => a.BankAccountId == id);
            if (acc == null) return;

            try
            {
                Cursor = Cursors.WaitCursor;
                var provider = BankApiClientFactory.CreateProvider(acc);
                bool success = provider.TestConnection(acc);

                MessageBox.Show(
                    success ? $"✅ API bağlantısı başarılı!\nSağlayıcı: {provider.ProviderName}" : "❌ API bağlantısı başarısız.",
                    "API Test",
                    MessageBoxButtons.OK,
                    success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"API test sırasında hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
    }

    public class BankAccountEditForm : Form
    {
        private TextBox txtBankName, txtAccountName, txtIban, txtInitialBalance;
        private TextBox txtApiBaseUrl, txtApiClientId, txtApiClientSecret, txtApiKey, txtApiUsername, txtApiPassword, txtApiAccountId;
        private ComboBox cmbCurrency, cmbApiType;
        private CheckBox chkApiEnabled;
        private Button btnSave, btnCancel;
        private Panel pnlApiSettings;

        private readonly Company _company;
        private readonly BankAccount _editing;
        private readonly BankService _bankService;

        public BankAccountEditForm(Company company, BankAccount editing)
        {
            _company = company;
            _editing = editing;
            _bankService = new BankService(Program.ConnectionString, Program.EncryptionKey);
            InitializeComponent();
            if (editing != null) PopulateFields(editing);
        }

        private void InitializeComponent()
        {
            this.Text = _editing == null ? "Yeni Banka Hesabı" : "Banka Hesabı Düzenle";
            this.Size = new Size(520, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9f);
            this.AutoScroll = true;

            int y = 15, lx = 15, cx = 160, cw = 310;

            void AddRow(string label, Control ctrl)
            {
                var lbl = new Label { Text = label, AutoSize = true, Location = new Point(lx, y + 4), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
                ctrl.Location = new Point(cx, y);
                ctrl.Size = new Size(cw, 25);
                this.Controls.AddRange(new Control[] { lbl, ctrl });
                y += 32;
            }

            txtBankName = new TextBox(); AddRow("Banka Adı:*", txtBankName);
            txtAccountName = new TextBox(); AddRow("Hesap Adı:", txtAccountName);
            txtIban = new TextBox(); AddRow("IBAN:", txtIban);

            cmbCurrency = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCurrency.Items.AddRange(new object[] { "TRY", "USD", "EUR", "GBP" });
            cmbCurrency.SelectedIndex = 0;
            AddRow("Para Birimi:", cmbCurrency);

            txtInitialBalance = new TextBox { Text = "0,00" }; AddRow("Başlangıç Bakiye:", txtInitialBalance);

            // API Ayarları
            chkApiEnabled = new CheckBox
            {
                Text = "API Entegrasyonu Etkin",
                Location = new Point(lx, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(41, 128, 185)
            };
            chkApiEnabled.CheckedChanged += (s, e) => pnlApiSettings.Visible = chkApiEnabled.Checked;
            this.Controls.Add(chkApiEnabled);
            y += 30;

            pnlApiSettings = new Panel
            {
                Location = new Point(lx, y),
                Size = new Size(470, 220),
                BackColor = Color.FromArgb(248, 249, 250),
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle
            };

            int py = 10, plx = 10, pcx = 150, pcw = 290;
            void AddApiRow(string label, Control ctrl)
            {
                var lbl = new Label { Text = label, AutoSize = true, Location = new Point(plx, py + 4), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
                ctrl.Location = new Point(pcx, py);
                ctrl.Size = new Size(pcw, 23);
                pnlApiSettings.Controls.AddRange(new Control[] { lbl, ctrl });
                py += 30;
            }

            cmbApiType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbApiType.Items.AddRange(new object[] { "REST", "SOAP", "OpenBanking", "Mock" });
            cmbApiType.SelectedIndex = 0;
            AddApiRow("API Tipi:", cmbApiType);

            txtApiBaseUrl = new TextBox(); AddApiRow("API URL:", txtApiBaseUrl);
            txtApiClientId = new TextBox(); AddApiRow("Client ID:", txtApiClientId);
            txtApiClientSecret = new TextBox { PasswordChar = '●' }; AddApiRow("Client Secret:", txtApiClientSecret);
            txtApiKey = new TextBox { PasswordChar = '●' }; AddApiRow("API Key:", txtApiKey);
            txtApiUsername = new TextBox(); AddApiRow("Kullanıcı Adı:", txtApiUsername);
            txtApiPassword = new TextBox { PasswordChar = '●' }; AddApiRow("Şifre:", txtApiPassword);
            txtApiAccountId = new TextBox(); AddApiRow("Hesap ID:", txtApiAccountId);

            this.Controls.Add(pnlApiSettings);
            y += 230;

            btnSave = new Button
            {
                Text = "💾 Kaydet", Size = new Size(130, 35), Location = new Point(cx, y + 10),
                BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text = "İptal", Size = new Size(90, 35), Location = new Point(cx + 140, y + 10),
                BackColor = Color.FromArgb(236, 240, 241), ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
            this.ClientSize = new Size(500, y + 60);
        }

        private void PopulateFields(BankAccount acc)
        {
            txtBankName.Text = acc.BankName;
            txtAccountName.Text = acc.AccountName;
            txtIban.Text = acc.IBAN;
            int currIdx = cmbCurrency.Items.IndexOf(acc.Currency);
            if (currIdx >= 0) cmbCurrency.SelectedIndex = currIdx;
            txtInitialBalance.Text = acc.InitialBalance.ToString("N2");
            chkApiEnabled.Checked = acc.IsApiEnabled;

            if (acc.IsApiEnabled)
            {
                int apiIdx = cmbApiType.Items.IndexOf(acc.ApiProviderType ?? "REST");
                if (apiIdx >= 0) cmbApiType.SelectedIndex = apiIdx;
                txtApiBaseUrl.Text = acc.ApiBaseUrl;
                txtApiClientId.Text = acc.ApiClientId;
                txtApiClientSecret.Text = acc.ApiClientSecret;
                txtApiKey.Text = acc.ApiKey;
                txtApiUsername.Text = acc.ApiUsername;
                txtApiPassword.Text = acc.ApiPassword;
                txtApiAccountId.Text = acc.ApiAccountId;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBankName.Text))
            { MessageBox.Show("Banka adı zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (!decimal.TryParse(txtInitialBalance.Text.Replace(",", "."), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal initialBalance))
                initialBalance = 0;

            if (_editing == null)
            {
                var result = _bankService.CreateBankAccount(
                    _company.CompanyId, txtBankName.Text.Trim(), txtAccountName.Text.Trim(),
                    txtIban.Text.Trim(), cmbCurrency.SelectedItem?.ToString() ?? "TRY", initialBalance,
                    chkApiEnabled.Checked,
                    chkApiEnabled.Checked ? cmbApiType.SelectedItem?.ToString() : null,
                    chkApiEnabled.Checked ? txtApiBaseUrl.Text.Trim() : null,
                    chkApiEnabled.Checked ? txtApiClientId.Text.Trim() : null,
                    chkApiEnabled.Checked ? txtApiClientSecret.Text.Trim() : null,
                    chkApiEnabled.Checked ? txtApiKey.Text.Trim() : null,
                    chkApiEnabled.Checked ? txtApiUsername.Text.Trim() : null,
                    chkApiEnabled.Checked ? txtApiPassword.Text.Trim() : null,
                    chkApiEnabled.Checked ? txtApiAccountId.Text.Trim() : null);

                if (result != null) { this.DialogResult = DialogResult.OK; }
                else MessageBox.Show("Hesap eklenirken hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                _editing.BankName = txtBankName.Text.Trim();
                _editing.AccountName = txtAccountName.Text.Trim();
                _editing.IBAN = txtIban.Text.Trim();
                _editing.Currency = cmbCurrency.SelectedItem?.ToString() ?? "TRY";
                _editing.IsApiEnabled = chkApiEnabled.Checked;
                _editing.ApiProviderType = chkApiEnabled.Checked ? cmbApiType.SelectedItem?.ToString() : null;
                _editing.ApiBaseUrl = chkApiEnabled.Checked ? txtApiBaseUrl.Text.Trim() : null;
                _editing.ApiClientId = chkApiEnabled.Checked ? txtApiClientId.Text.Trim() : null;
                _editing.ApiClientSecret = chkApiEnabled.Checked ? txtApiClientSecret.Text.Trim() : null;
                _editing.ApiKey = chkApiEnabled.Checked ? txtApiKey.Text.Trim() : null;
                _editing.ApiUsername = chkApiEnabled.Checked ? txtApiUsername.Text.Trim() : null;
                _editing.ApiPassword = chkApiEnabled.Checked ? txtApiPassword.Text.Trim() : null;
                _editing.ApiAccountId = chkApiEnabled.Checked ? txtApiAccountId.Text.Trim() : null;

                if (_bankService.UpdateBankAccount(_editing)) { this.DialogResult = DialogResult.OK; }
                else MessageBox.Show("Güncelleme sırasında hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
