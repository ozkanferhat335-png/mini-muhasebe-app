using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    /// <summary>
    /// Hesap kategorileri yönetimi formu
    /// </summary>
    public class AccountForm : Form
    {
        private DataGridView dgv;
        private Button btnNew, btnEdit, btnDelete, btnDefaults;
        private ComboBox cmbType;
        private AccountService _service;
        private List<Account> _accounts;

        public AccountForm()
        {
            _service = new AccountService(Program.ConnectionString);
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Hesap Kategorileri";
            this.Size = new Size(850, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 247, 250);

            var pnlFilter = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White, Padding = new Padding(10) };
            var lblType = new Label { Text = "Tür:", Location = new Point(10, 15), Size = new Size(35, 20) };
            cmbType = new ComboBox { Location = new Point(50, 12), Size = new Size(150, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbType.Items.AddRange(new[] { "Tümü", "Income", "Expense", "Bank", "Cash", "CurrentAccount" });
            cmbType.SelectedIndex = 0;
            var btnFilter = new Button { Text = "Filtrele", Location = new Point(215, 10), Size = new Size(80, 28), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnFilter.FlatAppearance.BorderSize = 0;
            btnFilter.Click += (s, e) => LoadData();
            pnlFilter.Controls.AddRange(new Control[] { lblType, cmbType, btnFilter });

            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(52, 73, 94), Padding = new Padding(10, 7, 10, 7) };
            btnNew = CreateBtn("➕ Yeni Hesap", Color.FromArgb(39, 174, 96)); btnNew.Click += BtnNew_Click;
            btnEdit = CreateBtn("✏️ Düzenle", Color.FromArgb(41, 128, 185)); btnEdit.Click += BtnEdit_Click;
            btnDelete = CreateBtn("🗑️ Sil", Color.FromArgb(231, 76, 60)); btnDelete.Click += BtnDelete_Click;
            btnDefaults = CreateBtn("⚡ Varsayılanları Ekle", Color.FromArgb(142, 68, 173)); btnDefaults.Click += BtnDefaults_Click;

            int x = 10;
            foreach (var btn in new[] { btnNew, btnEdit, btnDelete, btnDefaults })
            { btn.Location = new Point(x, 7); pnlToolbar.Controls.Add(btn); x += btn.Width + 5; }

            dgv = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, RowHeadersVisible = false, Font = new Font("Segoe UI", 9), AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(248, 249, 250) } };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;

            this.Controls.Add(dgv);
            this.Controls.Add(pnlFilter);
            this.Controls.Add(pnlToolbar);
        }

        private Button CreateBtn(string text, Color color)
        {
            var btn = new Button { Text = text, Size = new Size(155, 30), BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8, FontStyle.Bold), Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void LoadData()
        {
            if (!Program.ActiveCompanyId.HasValue) return;
            string typeFilter = cmbType.SelectedItem?.ToString();
            if (typeFilter == "Tümü")
                _accounts = _service.GetAccountsByCompany(Program.ActiveCompanyId.Value);
            else
                _accounts = _service.GetAccountsByCompanyAndType(Program.ActiveCompanyId.Value, typeFilter);

            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccountId", HeaderText = "ID", DataPropertyName = "AccountId", Width = 50 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccountCode", HeaderText = "Kod", DataPropertyName = "AccountCode", Width = 70 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccountName", HeaderText = "Hesap Adı", DataPropertyName = "AccountName" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "AccountType", HeaderText = "Tür", DataPropertyName = "AccountType", Width = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Açıklama", DataPropertyName = "Description" });
            dgv.DataSource = _accounts;
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            var form = new AccountEditForm(null);
            if (form.ShowDialog(this) == DialogResult.OK) LoadData();
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0) { MessageBox.Show("Lütfen bir hesap seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["AccountId"].Value);
            var account = _accounts.Find(a => a.AccountId == id);
            var form = new AccountEditForm(account);
            if (form.ShowDialog(this) == DialogResult.OK) LoadData();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0) { MessageBox.Show("Lütfen bir hesap seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show("Hesabı silmek istediğinizden emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["AccountId"].Value);
                if (_service.DeleteAccount(id)) LoadData();
                else MessageBox.Show("Silme başarısız.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDefaults_Click(object sender, EventArgs e)
        {
            if (!Program.ActiveCompanyId.HasValue) return;
            if (MessageBox.Show("Varsayılan hesap kategorileri eklenecek. Devam etmek istiyor musunuz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _service.CreateDefaultAccounts(Program.ActiveCompanyId.Value);
                LoadData();
                MessageBox.Show("Varsayılan hesaplar eklendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    public class AccountEditForm : Form
    {
        private TextBox txtName, txtCode, txtDescription;
        private ComboBox cmbType;
        private Button btnSave, btnCancel;
        private Account _account;
        private AccountService _service;

        public AccountEditForm(Account account)
        {
            _account = account;
            _service = new AccountService(Program.ConnectionString);
            InitializeComponent();
            if (_account != null) FillForm();
        }

        private void InitializeComponent()
        {
            this.Text = _account == null ? "Yeni Hesap" : "Hesap Düzenle";
            this.Size = new Size(420, 290);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 250);

            int y = 20, lx = 20, cx = 150, cw = 230;
            AddLbl("Hesap Adı: *", lx, y); txtName = AddTxt(cx, y, cw); y += 35;
            AddLbl("Hesap Türü: *", lx, y); cmbType = new ComboBox { Location = new Point(cx, y), Size = new Size(cw, 25), DropDownStyle = ComboBoxStyle.DropDownList }; cmbType.Items.AddRange(new[] { "Income", "Expense", "Bank", "Cash", "CurrentAccount" }); cmbType.SelectedIndex = 0; this.Controls.Add(cmbType); y += 35;
            AddLbl("Hesap Kodu:", lx, y); txtCode = AddTxt(cx, y, 100); y += 35;
            AddLbl("Açıklama:", lx, y); txtDescription = AddTxt(cx, y, cw); y += 45;

            btnSave = new Button { Text = "💾 Kaydet", Location = new Point(cx, y), Size = new Size(110, 35), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            btnCancel = new Button { Text = "İptal", Location = new Point(cx + 120, y), Size = new Size(80, 35), BackColor = Color.FromArgb(127, 140, 141), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
            this.Height = y + 80;
        }

        private void AddLbl(string t, int x, int y) { var l = new Label { Text = t, Location = new Point(x, y + 3), Size = new Size(125, 20), Font = new Font("Segoe UI", 9) }; this.Controls.Add(l); }
        private TextBox AddTxt(int x, int y, int w) { var t = new TextBox { Location = new Point(x, y), Size = new Size(w, 25), Font = new Font("Segoe UI", 9) }; this.Controls.Add(t); return t; }

        private void FillForm()
        {
            txtName.Text = _account.AccountName;
            cmbType.SelectedItem = _account.AccountType;
            txtCode.Text = _account.AccountCode;
            txtDescription.Text = _account.Description;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Hesap adı zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!Program.ActiveCompanyId.HasValue) return;

            if (_account == null)
            {
                var result = _service.CreateAccount(Program.ActiveCompanyId.Value, txtName.Text.Trim(), cmbType.SelectedItem?.ToString() ?? "Expense", txtCode.Text.Trim(), txtDescription.Text.Trim());
                if (result != null) { this.DialogResult = DialogResult.OK; this.Close(); }
                else MessageBox.Show("Kayıt eklenemedi.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                _account.AccountName = txtName.Text.Trim();
                _account.AccountType = cmbType.SelectedItem?.ToString() ?? "Expense";
                _account.AccountCode = txtCode.Text.Trim();
                _account.Description = txtDescription.Text.Trim();
                if (_service.UpdateAccount(_account)) { this.DialogResult = DialogResult.OK; this.Close(); }
                else MessageBox.Show("Güncelleme başarısız.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
