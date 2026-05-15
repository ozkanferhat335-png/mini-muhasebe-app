using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    /// <summary>
    /// Cari hesap yönetimi formu
    /// </summary>
    public class CurrentAccountForm : Form
    {
        private DataGridView dgvAccounts;
        private Button btnNew, btnEdit, btnDelete, btnStatement;
        private ComboBox cmbType;
        private TextBox txtSearch;

        private CurrentAccountService _service;
        private List<CurrentAccount> _accounts;

        public CurrentAccountForm()
        {
            _service = new CurrentAccountService(Program.ConnectionString);
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Cari Hesap Yönetimi";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 247, 250);

            // Filtre paneli
            var pnlFilter = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White, Padding = new Padding(10, 10, 10, 5) };
            var lblType = new Label { Text = "Tür:", Location = new Point(10, 15), Size = new Size(35, 20) };
            cmbType = new ComboBox { Location = new Point(50, 12), Size = new Size(130, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbType.Items.AddRange(new[] { "Tümü", "Customer", "Supplier" });
            cmbType.SelectedIndex = 0;
            var lblSearch = new Label { Text = "Ara:", Location = new Point(200, 15), Size = new Size(35, 20) };
            txtSearch = new TextBox { Location = new Point(240, 12), Size = new Size(200, 25) };
            var btnFilter = new Button { Text = "Filtrele", Location = new Point(455, 10), Size = new Size(80, 28), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnFilter.FlatAppearance.BorderSize = 0;
            btnFilter.Click += (s, e) => LoadData();
            pnlFilter.Controls.AddRange(new Control[] { lblType, cmbType, lblSearch, txtSearch, btnFilter });

            // Araç çubuğu
            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(52, 73, 94), Padding = new Padding(10, 7, 10, 7) };
            btnNew = CreateBtn("➕ Yeni Cari", Color.FromArgb(39, 174, 96)); btnNew.Click += BtnNew_Click;
            btnEdit = CreateBtn("✏️ Düzenle", Color.FromArgb(41, 128, 185)); btnEdit.Click += BtnEdit_Click;
            btnDelete = CreateBtn("🗑️ Sil", Color.FromArgb(231, 76, 60)); btnDelete.Click += BtnDelete_Click;
            btnStatement = CreateBtn("📄 Ekstre", Color.FromArgb(142, 68, 173)); btnStatement.Click += BtnStatement_Click;

            int x = 10;
            foreach (var btn in new[] { btnNew, btnEdit, btnDelete, btnStatement })
            { btn.Location = new Point(x, 7); pnlToolbar.Controls.Add(btn); x += btn.Width + 5; }

            // Grid
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
            this.Controls.Add(pnlFilter);
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

            string typeFilter = cmbType.SelectedItem?.ToString();
            string search = txtSearch.Text.Trim().ToLowerInvariant();

            if (typeFilter == "Tümü")
                _accounts = _service.GetAccountsByCompany(Program.ActiveCompanyId.Value);
            else
                _accounts = _service.GetAccountsByCompanyAndType(Program.ActiveCompanyId.Value, typeFilter);

            if (!string.IsNullOrEmpty(search))
                _accounts = _accounts.FindAll(a => a.Title.ToLowerInvariant().Contains(search) ||
                    (a.TaxNumber ?? "").Contains(search) || (a.Phone ?? "").Contains(search));

            dgvAccounts.Columns.Clear();
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentAccountId", HeaderText = "ID", DataPropertyName = "CurrentAccountId", Width = 50 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Unvan", DataPropertyName = "Title" });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccountType", HeaderText = "Tür", DataPropertyName = "AccountType", Width = 100 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "TaxNumber", HeaderText = "Vergi No", DataPropertyName = "TaxNumber", Width = 120 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "Telefon", DataPropertyName = "Phone", Width = 120 });
            dgvAccounts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "E-posta", DataPropertyName = "Email" });
            dgvAccounts.DataSource = _accounts;
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            var form = new CurrentAccountEditForm(null);
            if (form.ShowDialog(this) == DialogResult.OK) LoadData();
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dgvAccounts.SelectedRows.Count == 0) { MessageBox.Show("Lütfen bir kayıt seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int id = Convert.ToInt32(dgvAccounts.SelectedRows[0].Cells["CurrentAccountId"].Value);
            var account = _accounts.Find(a => a.CurrentAccountId == id);
            var form = new CurrentAccountEditForm(account);
            if (form.ShowDialog(this) == DialogResult.OK) LoadData();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvAccounts.SelectedRows.Count == 0) { MessageBox.Show("Lütfen bir kayıt seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show("Seçili cari kartı silmek istediğinizden emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                int id = Convert.ToInt32(dgvAccounts.SelectedRows[0].Cells["CurrentAccountId"].Value);
                if (_service.DeleteCurrentAccount(id)) LoadData();
                else MessageBox.Show("Silme başarısız.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStatement_Click(object sender, EventArgs e)
        {
            if (dgvAccounts.SelectedRows.Count == 0) { MessageBox.Show("Lütfen bir cari seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int id = Convert.ToInt32(dgvAccounts.SelectedRows[0].Cells["CurrentAccountId"].Value);
            var account = _accounts.Find(a => a.CurrentAccountId == id);
            var form = new CurrentAccountStatementForm(account);
            form.ShowDialog(this);
        }
    }

    /// <summary>
    /// Cari kart ekleme/düzenleme formu
    /// </summary>
    public class CurrentAccountEditForm : Form
    {
        private TextBox txtTitle, txtTaxNumber, txtTaxId, txtPhone, txtEmail, txtAddress, txtNotes;
        private ComboBox cmbType;
        private Button btnSave, btnCancel;
        private CurrentAccount _account;
        private CurrentAccountService _service;

        public CurrentAccountEditForm(CurrentAccount account)
        {
            _account = account;
            _service = new CurrentAccountService(Program.ConnectionString);
            InitializeComponent();
            if (_account != null) FillForm();
        }

        private void InitializeComponent()
        {
            this.Text = _account == null ? "Yeni Cari Kart" : "Cari Kartı Düzenle";
            this.Size = new Size(460, 430);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 250);

            int y = 20, lx = 20, cx = 160, cw = 260;
            AddLbl("Unvan: *", lx, y); txtTitle = AddTxt(cx, y, cw); y += 35;
            AddLbl("Cari Türü: *", lx, y); cmbType = new ComboBox { Location = new Point(cx, y), Size = new Size(cw, 25), DropDownStyle = ComboBoxStyle.DropDownList }; cmbType.Items.AddRange(new[] { "Customer", "Supplier" }); cmbType.SelectedIndex = 0; this.Controls.Add(cmbType); y += 35;
            AddLbl("Vergi No:", lx, y); txtTaxNumber = AddTxt(cx, y, cw); y += 35;
            AddLbl("TCKN:", lx, y); txtTaxId = AddTxt(cx, y, cw); y += 35;
            AddLbl("Telefon:", lx, y); txtPhone = AddTxt(cx, y, cw); y += 35;
            AddLbl("E-posta:", lx, y); txtEmail = AddTxt(cx, y, cw); y += 35;
            AddLbl("Adres:", lx, y); txtAddress = AddTxt(cx, y, cw); y += 35;
            AddLbl("Notlar:", lx, y); txtNotes = AddTxt(cx, y, cw); y += 45;

            btnSave = new Button { Text = "💾 Kaydet", Location = new Point(cx, y), Size = new Size(120, 35), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            btnCancel = new Button { Text = "İptal", Location = new Point(cx + 130, y), Size = new Size(80, 35), BackColor = Color.FromArgb(127, 140, 141), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
            this.Height = y + 80;
        }

        private void AddLbl(string text, int x, int y) { var l = new Label { Text = text, Location = new Point(x, y + 3), Size = new Size(135, 20), Font = new Font("Segoe UI", 9) }; this.Controls.Add(l); }
        private TextBox AddTxt(int x, int y, int w) { var t = new TextBox { Location = new Point(x, y), Size = new Size(w, 25), Font = new Font("Segoe UI", 9) }; this.Controls.Add(t); return t; }

        private void FillForm()
        {
            txtTitle.Text = _account.Title;
            cmbType.SelectedItem = _account.AccountType;
            txtTaxNumber.Text = _account.TaxNumber;
            txtTaxId.Text = _account.TaxId;
            txtPhone.Text = _account.Phone;
            txtEmail.Text = _account.Email;
            txtAddress.Text = _account.Address;
            txtNotes.Text = _account.Notes;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text)) { MessageBox.Show("Unvan zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!Program.ActiveCompanyId.HasValue) return;

            if (_account == null)
            {
                var result = _service.CreateCurrentAccount(Program.ActiveCompanyId.Value, txtTitle.Text.Trim(),
                    cmbType.SelectedItem?.ToString() ?? "Customer", txtTaxNumber.Text.Trim(), txtTaxId.Text.Trim(),
                    txtPhone.Text.Trim(), txtEmail.Text.Trim(), txtAddress.Text.Trim());
                if (result != null) { this.DialogResult = DialogResult.OK; this.Close(); }
                else MessageBox.Show("Kayıt eklenemedi.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                _account.Title = txtTitle.Text.Trim();
                _account.AccountType = cmbType.SelectedItem?.ToString() ?? "Customer";
                _account.TaxNumber = txtTaxNumber.Text.Trim();
                _account.TaxId = txtTaxId.Text.Trim();
                _account.Phone = txtPhone.Text.Trim();
                _account.Email = txtEmail.Text.Trim();
                _account.Address = txtAddress.Text.Trim();
                _account.Notes = txtNotes.Text.Trim();
                if (_service.UpdateCurrentAccount(_account)) { this.DialogResult = DialogResult.OK; this.Close(); }
                else MessageBox.Show("Güncelleme başarısız.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>
    /// Cari ekstre formu
    /// </summary>
    public class CurrentAccountStatementForm : Form
    {
        private CurrentAccount _account;
        private ReportService _reportService;

        public CurrentAccountStatementForm(CurrentAccount account)
        {
            _account = account;
            _reportService = new ReportService(Program.ConnectionString, Program.EncryptionKey);
            InitializeComponent();
            LoadStatement();
        }

        private DataGridView dgv;
        private Label lblBalance;
        private DateTimePicker dtpStart, dtpEnd;

        private void InitializeComponent()
        {
            this.Text = $"Cari Ekstre - {_account.Title}";
            this.Size = new Size(800, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 247, 250);

            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White, Padding = new Padding(10) };
            var lblS = new Label { Text = "Başlangıç:", Location = new Point(10, 15), Size = new Size(70, 20) };
            dtpStart = new DateTimePicker { Location = new Point(85, 12), Size = new Size(130, 25), Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Now.Year, 1, 1) };
            var lblE = new Label { Text = "Bitiş:", Location = new Point(230, 15), Size = new Size(40, 20) };
            dtpEnd = new DateTimePicker { Location = new Point(275, 12), Size = new Size(130, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Now };
            var btnLoad = new Button { Text = "Getir", Location = new Point(420, 10), Size = new Size(70, 28), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnLoad.FlatAppearance.BorderSize = 0;
            btnLoad.Click += (s, e) => LoadStatement();
            pnlTop.Controls.AddRange(new Control[] { lblS, dtpStart, lblE, dtpEnd, btnLoad });

            dgv = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, RowHeadersVisible = false, Font = new Font("Segoe UI", 9) };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.EnableHeadersVisualStyles = false;

            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = Color.FromArgb(52, 73, 94) };
            lblBalance = new Label { ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(10, 10), Size = new Size(700, 22) };
            pnlBottom.Controls.Add(lblBalance);

            this.Controls.Add(dgv);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);
        }

        private void LoadStatement()
        {
            var statement = _reportService.GetCurrentAccountStatement(_account.CurrentAccountId, dtpStart.Value, dtpEnd.Value);
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tarih", DataPropertyName = "TransactionDate", DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tür", DataPropertyName = "TransactionType" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Açıklama", DataPropertyName = "Description" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tutar (₺)", DataPropertyName = "Amount", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgv.DataSource = statement.Transactions;
            lblBalance.Text = $"Toplam Borç: {statement.TotalDebit:N2} ₺  |  Toplam Alacak: {statement.TotalCredit:N2} ₺  |  Bakiye: {statement.Balance:N2} ₺";
        }
    }
}
