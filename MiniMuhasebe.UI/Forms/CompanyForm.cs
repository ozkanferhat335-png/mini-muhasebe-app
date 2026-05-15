using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    /// <summary>
    /// Firma yönetimi formu
    /// </summary>
    public class CompanyForm : Form
    {
        private DataGridView dgv;
        private Button btnNew, btnEdit, btnDelete;
        private CompanyService _service;
        private AccountService _accountService;
        private List<Company> _companies;

        public CompanyForm()
        {
            _service = new CompanyService(Program.ConnectionString);
            _accountService = new AccountService(Program.ConnectionString);
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Firma Yönetimi";
            this.Size = new Size(850, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 247, 250);

            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(52, 73, 94), Padding = new Padding(10, 7, 10, 7) };
            btnNew = CreateBtn("➕ Yeni Firma", Color.FromArgb(39, 174, 96)); btnNew.Click += BtnNew_Click;
            btnEdit = CreateBtn("✏️ Düzenle", Color.FromArgb(41, 128, 185)); btnEdit.Click += BtnEdit_Click;
            btnDelete = CreateBtn("🗑️ Sil", Color.FromArgb(231, 76, 60)); btnDelete.Click += BtnDelete_Click;

            int x = 10;
            foreach (var btn in new[] { btnNew, btnEdit, btnDelete })
            { btn.Location = new Point(x, 7); pnlToolbar.Controls.Add(btn); x += btn.Width + 5; }

            dgv = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, RowHeadersVisible = false, Font = new Font("Segoe UI", 9), AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(248, 249, 250) } };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;

            this.Controls.Add(dgv);
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
            _companies = _service.GetAllCompanies();
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompanyId", HeaderText = "ID", DataPropertyName = "CompanyId", Width = 50 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompanyName", HeaderText = "Firma Adı", DataPropertyName = "CompanyName" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TaxOffice", HeaderText = "Vergi Müdürlüğü", DataPropertyName = "TaxOffice" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TaxNumber", HeaderText = "Vergi No", DataPropertyName = "TaxNumber", Width = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "Telefon", DataPropertyName = "Phone", Width = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "E-posta", DataPropertyName = "Email" });
            dgv.DataSource = _companies;
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            var form = new CompanyEditForm(null);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                LoadData();
                // Yeni firma için varsayılan hesapları oluştur
                if (_companies.Count > 0)
                    _accountService.CreateDefaultAccounts(_companies[_companies.Count - 1].CompanyId);
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0) { MessageBox.Show("Lütfen bir firma seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["CompanyId"].Value);
            var company = _companies.Find(c => c.CompanyId == id);
            var form = new CompanyEditForm(company);
            if (form.ShowDialog(this) == DialogResult.OK) LoadData();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (Program.CurrentUserRole != "Admin") { MessageBox.Show("Bu işlem için yönetici yetkisi gereklidir.", "Yetki Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (dgv.SelectedRows.Count == 0) { MessageBox.Show("Lütfen bir firma seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show("Firmayı silmek istediğinizden emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["CompanyId"].Value);
                if (_service.DeleteCompany(id)) LoadData();
                else MessageBox.Show("Silme başarısız.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class CompanyEditForm : Form
    {
        private TextBox txtName, txtTaxOffice, txtTaxNumber, txtPhone, txtEmail, txtAddress;
        private Button btnSave, btnCancel;
        private Company _company;
        private CompanyService _service;

        public CompanyEditForm(Company company)
        {
            _company = company;
            _service = new CompanyService(Program.ConnectionString);
            InitializeComponent();
            if (_company != null) FillForm();
        }

        private void InitializeComponent()
        {
            this.Text = _company == null ? "Yeni Firma" : "Firma Düzenle";
            this.Size = new Size(460, 360);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 250);

            int y = 20, lx = 20, cx = 160, cw = 260;
            AddLbl("Firma Adı: *", lx, y); txtName = AddTxt(cx, y, cw); y += 35;
            AddLbl("Vergi Müdürlüğü:", lx, y); txtTaxOffice = AddTxt(cx, y, cw); y += 35;
            AddLbl("Vergi No:", lx, y); txtTaxNumber = AddTxt(cx, y, cw); y += 35;
            AddLbl("Telefon:", lx, y); txtPhone = AddTxt(cx, y, cw); y += 35;
            AddLbl("E-posta:", lx, y); txtEmail = AddTxt(cx, y, cw); y += 35;
            AddLbl("Adres:", lx, y); txtAddress = AddTxt(cx, y, cw); y += 45;

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

        private void FillForm()
        {
            txtName.Text = _company.CompanyName;
            txtTaxOffice.Text = _company.TaxOffice;
            txtTaxNumber.Text = _company.TaxNumber;
            txtPhone.Text = _company.Phone;
            txtEmail.Text = _company.Email;
            txtAddress.Text = _company.Address;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Firma adı zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (_company == null)
            {
                var result = _service.CreateCompany(txtName.Text.Trim(), txtTaxOffice.Text.Trim(), txtTaxNumber.Text.Trim(), txtPhone.Text.Trim(), txtEmail.Text.Trim(), txtAddress.Text.Trim());
                if (result != null) { this.DialogResult = DialogResult.OK; this.Close(); }
                else MessageBox.Show("Kayıt eklenemedi.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                _company.CompanyName = txtName.Text.Trim();
                _company.TaxOffice = txtTaxOffice.Text.Trim();
                _company.TaxNumber = txtTaxNumber.Text.Trim();
                _company.Phone = txtPhone.Text.Trim();
                _company.Email = txtEmail.Text.Trim();
                _company.Address = txtAddress.Text.Trim();
                if (_service.UpdateCompany(_company)) { this.DialogResult = DialogResult.OK; this.Close(); }
                else MessageBox.Show("Güncelleme başarısız.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
