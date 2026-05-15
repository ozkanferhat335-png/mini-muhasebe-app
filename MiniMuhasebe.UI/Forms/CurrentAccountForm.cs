using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    public class CurrentAccountForm : Form
    {
        private DataGridView dgvAccounts;
        private DataGridView dgvTransactions;
        private Panel pnlToolbar;
        private Panel pnlSplit;
        private Button btnNew, btnEdit, btnDelete, btnRefresh, btnAddTransaction;
        private ComboBox cmbType;
        private Label lblBalance;

        private readonly CurrentAccountService _service;
        private readonly CurrentAccountTransactionService _transactionService;
        private List<CurrentAccount> _accounts;

        public CurrentAccountForm()
        {
            _service = new CurrentAccountService(AppSession.ConnectionString);
            _transactionService = new CurrentAccountTransactionService(AppSession.ConnectionString);
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Cari Hesap Yönetimi";
            this.BackColor = Color.FromArgb(245, 247, 250);

            // Toolbar
            pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.White };

            var lblTitle = new Label
            {
                Text = "👥 Cari Hesap Yönetimi",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(10, 15)
            };

            btnNew = CreateBtn("➕ Yeni Cari", Color.FromArgb(39, 174, 96), 230, 12);
            btnNew.Click += (s, e) => OpenEditDialog(null);

            btnEdit = CreateBtn("✏️ Düzenle", Color.FromArgb(41, 128, 185), 320, 12);
            btnEdit.Click += (s, e) => EditSelected();

            btnDelete = CreateBtn("🗑️ Sil", Color.FromArgb(192, 57, 43), 410, 12);
            btnDelete.Click += (s, e) => DeleteSelected();

            btnRefresh = CreateBtn("🔄 Yenile", Color.FromArgb(127, 140, 141), 500, 12);
            btnRefresh.Click += (s, e) => LoadData();

            btnAddTransaction = CreateBtn("💰 Hareket Ekle", Color.FromArgb(230, 126, 34), 590, 12);
            btnAddTransaction.Width = 120;
            btnAddTransaction.Click += (s, e) => AddTransaction();

            var lblType = new Label { Text = "Tür:", Font = new Font("Segoe UI", 9), AutoSize = true, Location = new Point(725, 18) };
            cmbType = new ComboBox
            {
                Location = new Point(755, 15),
                Size = new Size(130, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            cmbType.Items.AddRange(new object[] { "Tümü", "Müşteri", "Tedarikçi" });
            cmbType.SelectedIndex = 0;
            cmbType.SelectedIndexChanged += (s, e) => LoadData();

            pnlToolbar.Controls.AddRange(new Control[] { lblTitle, btnNew, btnEdit, btnDelete, btnRefresh, btnAddTransaction, lblType, cmbType });

            // Split panel
            pnlSplit = new Panel { Dock = DockStyle.Fill };

            // Cari listesi (üst)
            dgvAccounts = CreateGrid();
            dgvAccounts.Dock = DockStyle.Top;
            dgvAccounts.Height = 250;
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentAccountId", HeaderText = "ID", Width = 50 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Unvan", FillWeight = 30 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccountType", HeaderText = "Tür", FillWeight = 10 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "TaxNumber", HeaderText = "Vergi No", FillWeight = 15 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "Telefon", FillWeight = 15 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "E-posta", FillWeight = 20 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "Bakiye (₺)", FillWeight = 15 });
            dgvAccounts.SelectionChanged += DgvAccounts_SelectionChanged;
            dgvAccounts.DoubleClick += (s, e) => EditSelected();

            // Bakiye etiketi
            lblBalance = new Label
            {
                Text = "Seçili Cari Bakiyesi: -",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(5, 5, 0, 5)
            };

            // Hareket listesi (alt)
            var lblTxTitle = new Label
            {
                Text = "Cari Hareketler",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(5, 5, 0, 0)
            };

            dgvTransactions = CreateGrid();
            dgvTransactions.Dock = DockStyle.Fill;
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransactionId", HeaderText = "ID", Width = 50 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransactionDate", HeaderText = "Tarih", FillWeight = 15 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Açıklama", FillWeight = 35 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransactionType", HeaderText = "Tür", FillWeight = 10 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Tutar (₺)", FillWeight = 15 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "DocumentNumber", HeaderText = "Belge No", FillWeight = 15 });

            pnlSplit.Controls.AddRange(new Control[] { dgvTransactions, lblTxTitle, lblBalance, dgvAccounts });

            this.Controls.AddRange(new Control[] { pnlSplit, pnlToolbar });
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

        private void LoadData()
        {
            if (AppSession.CurrentCompany == null) return;
            int companyId = AppSession.CurrentCompany.CompanyId;

            string typeFilter = cmbType.SelectedItem?.ToString() ?? "Tümü";
            if (typeFilter == "Müşteri")
                _accounts = _service.GetAccountsByCompanyAndType(companyId, "Customer");
            else if (typeFilter == "Tedarikçi")
                _accounts = _service.GetAccountsByCompanyAndType(companyId, "Supplier");
            else
                _accounts = _service.GetAccountsByCompany(companyId);

            dgvAccounts.Rows.Clear();
            foreach (var a in _accounts)
            {
                decimal balance = _transactionService.GetBalance(a.CurrentAccountId);
                string typeDisplay = a.AccountType == "Customer" ? "Müşteri" : "Tedarikçi";
                dgvAccounts.Rows.Add(a.CurrentAccountId, a.Title, typeDisplay, a.TaxNumber, a.Phone, a.Email, balance.ToString("N2"));
            }
        }

        private void DgvAccounts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvAccounts.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(dgvAccounts.SelectedRows[0].Cells["CurrentAccountId"].Value);
            LoadTransactions(id);
            decimal balance = _transactionService.GetBalance(id);
            string title = dgvAccounts.SelectedRows[0].Cells["Title"].Value?.ToString();
            lblBalance.Text = $"Seçili Cari: {title}  |  Bakiye: {balance:N2} ₺";
            lblBalance.ForeColor = balance >= 0 ? Color.FromArgb(39, 174, 96) : Color.FromArgb(192, 57, 43);
        }

        private void LoadTransactions(int currentAccountId)
        {
            dgvTransactions.Rows.Clear();
            var transactions = _transactionService.GetTransactionsByAccount(currentAccountId);
            foreach (var t in transactions)
            {
                string typeDisplay = t.TransactionType == "Debit" ? "Borç" : "Alacak";
                dgvTransactions.Rows.Add(t.TransactionId, t.TransactionDate.ToString("dd.MM.yyyy"),
                    t.Description, typeDisplay, t.Amount.ToString("N2"), t.RelatedDocumentNumber);
            }
        }

        private void OpenEditDialog(CurrentAccount existing)
        {
            if (AppSession.CurrentCompany == null) return;
            var dialog = new CurrentAccountEditDialog(existing, AppSession.CurrentCompany.CompanyId);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var data = dialog.AccountData;
                if (existing == null)
                    _service.CreateCurrentAccount(data.CompanyId, data.Title, data.AccountType,
                        data.TaxNumber, data.TaxId, data.Phone, data.Email, data.Address);
                else
                    _service.UpdateCurrentAccount(data);
                LoadData();
            }
        }

        private void EditSelected()
        {
            if (dgvAccounts.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(dgvAccounts.SelectedRows[0].Cells["CurrentAccountId"].Value);
            var account = _accounts.Find(a => a.CurrentAccountId == id);
            if (account != null) OpenEditDialog(account);
        }

        private void DeleteSelected()
        {
            if (dgvAccounts.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(dgvAccounts.SelectedRows[0].Cells["CurrentAccountId"].Value);
            if (MessageBox.Show("Bu cari kartı silmek istediğinizden emin misiniz?", "Silme Onayı",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                if (_service.DeleteCurrentAccount(id)) LoadData();
            }
        }

        private void AddTransaction()
        {
            if (dgvAccounts.SelectedRows.Count == 0)
            { MessageBox.Show("Lütfen bir cari hesap seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            int id = Convert.ToInt32(dgvAccounts.SelectedRows[0].Cells["CurrentAccountId"].Value);
            string title = dgvAccounts.SelectedRows[0].Cells["Title"].Value?.ToString();

            var dialog = new CurrentAccountTransactionDialog(id, title);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _transactionService.AddTransaction(id, dialog.TransactionDate, dialog.Amount,
                    dialog.TransactionType, dialog.Description, dialog.DocumentNumber,
                    null, AppSession.CurrentUser?.UserId);
                LoadTransactions(id);
                LoadData();
            }
        }
    }

    public class CurrentAccountEditDialog : Form
    {
        public CurrentAccount AccountData { get; private set; }
        private TextBox txtTitle, txtTaxNumber, txtTaxId, txtPhone, txtEmail, txtAddress, txtNotes;
        private ComboBox cmbType;

        public CurrentAccountEditDialog(CurrentAccount existing, int companyId)
        {
            AccountData = existing ?? new CurrentAccount { CompanyId = companyId, AccountType = "Customer", IsActive = true };
            InitializeComponent();
            if (existing != null) PopulateFields(existing);
        }

        private void InitializeComponent()
        {
            this.Text = AccountData.CurrentAccountId == 0 ? "Yeni Cari Kart" : "Cari Kart Düzenle";
            this.Size = new Size(450, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            int y = 20, lx = 20, cx = 140, w = 270;

            AddLabel("Unvan:", lx, y); txtTitle = AddTB(cx, y, w, AccountData.Title); y += 40;
            AddLabel("Tür:", lx, y);
            cmbType = new ComboBox { Location = new Point(cx, y), Size = new Size(150, 25), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9) };
            cmbType.Items.AddRange(new object[] { "Müşteri", "Tedarikçi" });
            cmbType.SelectedIndex = AccountData.AccountType == "Supplier" ? 1 : 0;
            this.Controls.Add(cmbType); y += 40;
            AddLabel("Vergi No:", lx, y); txtTaxNumber = AddTB(cx, y, w, AccountData.TaxNumber); y += 40;
            AddLabel("TC Kimlik:", lx, y); txtTaxId = AddTB(cx, y, w, AccountData.TaxId); y += 40;
            AddLabel("Telefon:", lx, y); txtPhone = AddTB(cx, y, w, AccountData.Phone); y += 40;
            AddLabel("E-posta:", lx, y); txtEmail = AddTB(cx, y, w, AccountData.Email); y += 40;
            AddLabel("Adres:", lx, y); txtAddress = AddTB(cx, y, w, AccountData.Address); y += 40;

            var btnOk = new Button { Text = "Kaydet", DialogResult = DialogResult.OK, Location = new Point(cx, y), Size = new Size(100, 35), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += BtnOk_Click;
            var btnCancel = new Button { Text = "İptal", DialogResult = DialogResult.Cancel, Location = new Point(cx + 110, y), Size = new Size(100, 35), BackColor = Color.FromArgb(127, 140, 141), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.FlatAppearance.BorderSize = 0;
            this.Controls.AddRange(new Control[] { btnOk, btnCancel });
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void PopulateFields(CurrentAccount a)
        {
            txtTitle.Text = a.Title; txtTaxNumber.Text = a.TaxNumber; txtTaxId.Text = a.TaxId;
            txtPhone.Text = a.Phone; txtEmail.Text = a.Email; txtAddress.Text = a.Address;
            cmbType.SelectedIndex = a.AccountType == "Supplier" ? 1 : 0;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text)) { MessageBox.Show("Unvan gereklidir.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            AccountData.Title = txtTitle.Text.Trim();
            AccountData.AccountType = cmbType.SelectedIndex == 1 ? "Supplier" : "Customer";
            AccountData.TaxNumber = txtTaxNumber.Text.Trim();
            AccountData.TaxId = txtTaxId.Text.Trim();
            AccountData.Phone = txtPhone.Text.Trim();
            AccountData.Email = txtEmail.Text.Trim();
            AccountData.Address = txtAddress.Text.Trim();
            this.DialogResult = DialogResult.OK;
        }

        private void AddLabel(string text, int x, int y) => this.Controls.Add(new Label { Text = text, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(44, 62, 80), AutoSize = true, Location = new Point(x, y + 3) });
        private TextBox AddTB(int x, int y, int w, string text = "") { var tb = new TextBox { Location = new Point(x, y), Size = new Size(w, 25), Font = new Font("Segoe UI", 9), Text = text ?? "" }; this.Controls.Add(tb); return tb; }
    }

    public class CurrentAccountTransactionDialog : Form
    {
        public DateTime TransactionDate { get; private set; }
        public decimal Amount { get; private set; }
        public string TransactionType { get; private set; }
        public string Description { get; private set; }
        public string DocumentNumber { get; private set; }

        private DateTimePicker dtpDate;
        private TextBox txtAmount, txtDescription, txtDocNumber;
        private ComboBox cmbType;

        public CurrentAccountTransactionDialog(int currentAccountId, string accountTitle)
        {
            this.Text = $"Hareket Ekle - {accountTitle}";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            int y = 20, lx = 20, cx = 140, w = 220;

            AddLabel("Tarih:", lx, y); dtpDate = new DateTimePicker { Location = new Point(cx, y), Size = new Size(w, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Today }; this.Controls.Add(dtpDate); y += 40;
            AddLabel("Tür:", lx, y); cmbType = new ComboBox { Location = new Point(cx, y), Size = new Size(150, 25), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9) }; cmbType.Items.AddRange(new object[] { "Borç (Debit)", "Alacak (Credit)" }); cmbType.SelectedIndex = 0; this.Controls.Add(cmbType); y += 40;
            AddLabel("Tutar (₺):", lx, y); txtAmount = AddTB(cx, y, w); y += 40;
            AddLabel("Açıklama:", lx, y); txtDescription = AddTB(cx, y, w); y += 40;
            AddLabel("Belge No:", lx, y); txtDocNumber = AddTB(cx, y, w); y += 40;

            var btnOk = new Button { Text = "Kaydet", DialogResult = DialogResult.OK, Location = new Point(cx, y), Size = new Size(100, 35), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                if (!decimal.TryParse(txtAmount.Text.Replace(",", "."), out decimal amt) || amt <= 0) { MessageBox.Show("Geçerli tutar girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                TransactionDate = dtpDate.Value;
                Amount = amt;
                TransactionType = cmbType.SelectedIndex == 0 ? "Debit" : "Credit";
                Description = txtDescription.Text.Trim();
                DocumentNumber = txtDocNumber.Text.Trim();
                this.DialogResult = DialogResult.OK;
            };
            var btnCancel = new Button { Text = "İptal", DialogResult = DialogResult.Cancel, Location = new Point(cx + 110, y), Size = new Size(100, 35), BackColor = Color.FromArgb(127, 140, 141), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.FlatAppearance.BorderSize = 0;
            this.Controls.AddRange(new Control[] { btnOk, btnCancel });
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void AddLabel(string text, int x, int y) => this.Controls.Add(new Label { Text = text, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(44, 62, 80), AutoSize = true, Location = new Point(x, y + 3) });
        private TextBox AddTB(int x, int y, int w) { var tb = new TextBox { Location = new Point(x, y), Size = new Size(w, 25), Font = new Font("Segoe UI", 9) }; this.Controls.Add(tb); return tb; }
    }
}
