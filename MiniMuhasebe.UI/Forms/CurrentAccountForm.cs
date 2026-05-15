using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    public class CurrentAccountForm : Form
    {
        private TabControl tabControl;
        private TabPage tabAccounts;
        private TabPage tabTransactions;
        private TabPage tabStatement;

        // Cari Kartlar sekmesi
        private DataGridView dgvAccounts;
        private Panel pnlAccountToolbar;
        private Button btnAddAccount, btnEditAccount, btnDeleteAccount, btnRefreshAccounts;
        private ComboBox cmbAccountTypeFilter;

        // Cari Hareketler sekmesi
        private DataGridView dgvTransactions;
        private Panel pnlTxToolbar;
        private Button btnAddTx, btnDeleteTx, btnRefreshTx;
        private ComboBox cmbSelectAccount;
        private Label lblBalance;

        private readonly User _currentUser;
        private readonly Company _activeCompany;
        private readonly CurrentAccountService _currentAccountService;
        private readonly CurrentAccountTransactionRepository _txRepository;

        private List<CurrentAccount> _accounts;
        private List<CurrentAccountTransaction> _transactions;

        public CurrentAccountForm(User user, Company company)
        {
            _currentUser = user;
            _activeCompany = company;
            _currentAccountService = new CurrentAccountService(Program.ConnectionString);
            _txRepository = new CurrentAccountTransactionRepository(Program.ConnectionString);

            InitializeComponent();
            LoadAccounts();
        }

        private void InitializeComponent()
        {
            this.Text = "Cari Hesap Yönetimi";
            this.Size = new Size(1050, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 9f);

            tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5f) };

            // --- Cari Kartlar Sekmesi ---
            tabAccounts = new TabPage("👥 Cari Kartlar");
            tabAccounts.BackColor = Color.White;

            pnlAccountToolbar = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White, Padding = new Padding(8) };

            btnAddAccount = CreateBtn("➕ Yeni Cari", Color.FromArgb(39, 174, 96), 10);
            btnAddAccount.Click += BtnAddAccount_Click;

            btnEditAccount = CreateBtn("✏️ Düzenle", Color.FromArgb(41, 128, 185), 130);
            btnEditAccount.Click += BtnEditAccount_Click;

            btnDeleteAccount = CreateBtn("🗑️ Sil", Color.FromArgb(231, 76, 60), 250);
            btnDeleteAccount.Click += BtnDeleteAccount_Click;

            btnRefreshAccounts = CreateBtn("🔄 Yenile", Color.FromArgb(149, 165, 166), 370);
            btnRefreshAccounts.Click += (s, e) => LoadAccounts();

            var lblFilter = new Label { Text = "Tür:", AutoSize = true, Location = new Point(510, 15), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            cmbAccountTypeFilter = new ComboBox
            {
                Location = new Point(545, 11),
                Size = new Size(130, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbAccountTypeFilter.Items.AddRange(new object[] { "Tümü", "Müşteri", "Tedarikçi" });
            cmbAccountTypeFilter.SelectedIndex = 0;
            cmbAccountTypeFilter.SelectedIndexChanged += (s, e) => LoadAccounts();

            pnlAccountToolbar.Controls.AddRange(new Control[] { btnAddAccount, btnEditAccount, btnDeleteAccount, btnRefreshAccounts, lblFilter, cmbAccountTypeFilter });

            dgvAccounts = CreateDgv();
            dgvAccounts.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", FillWeight = 5 },
                new DataGridViewTextBoxColumn { Name = "colTitle", HeaderText = "Unvan", FillWeight = 25 },
                new DataGridViewTextBoxColumn { Name = "colType", HeaderText = "Tür", FillWeight = 10 },
                new DataGridViewTextBoxColumn { Name = "colTaxNo", HeaderText = "Vergi No", FillWeight = 12 },
                new DataGridViewTextBoxColumn { Name = "colPhone", HeaderText = "Telefon", FillWeight = 12 },
                new DataGridViewTextBoxColumn { Name = "colEmail", HeaderText = "E-posta", FillWeight = 18 },
                new DataGridViewTextBoxColumn { Name = "colBalance", HeaderText = "Bakiye", FillWeight = 12, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "colNotes", HeaderText = "Notlar", FillWeight = 16 }
            });
            dgvAccounts.SelectionChanged += DgvAccounts_SelectionChanged;

            tabAccounts.Controls.AddRange(new Control[] { dgvAccounts, pnlAccountToolbar });

            // --- Cari Hareketler Sekmesi ---
            tabTransactions = new TabPage("📋 Cari Hareketler");
            tabTransactions.BackColor = Color.White;

            pnlTxToolbar = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White, Padding = new Padding(8) };

            var lblSelectAcc = new Label { Text = "Cari:", AutoSize = true, Location = new Point(10, 15), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            cmbSelectAccount = new ComboBox
            {
                Location = new Point(55, 11),
                Size = new Size(250, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "Title"
            };
            cmbSelectAccount.SelectedIndexChanged += (s, e) => LoadTransactions();

            btnAddTx = CreateBtn("➕ Hareket Ekle", Color.FromArgb(39, 174, 96), 320);
            btnAddTx.Click += BtnAddTx_Click;

            btnDeleteTx = CreateBtn("🗑️ Sil", Color.FromArgb(231, 76, 60), 440);
            btnDeleteTx.Click += BtnDeleteTx_Click;

            btnRefreshTx = CreateBtn("🔄 Yenile", Color.FromArgb(149, 165, 166), 560);
            btnRefreshTx.Click += (s, e) => LoadTransactions();

            lblBalance = new Label
            {
                Text = "Bakiye: ₺0,00",
                AutoSize = true,
                Location = new Point(700, 15),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(41, 128, 185)
            };

            pnlTxToolbar.Controls.AddRange(new Control[] { lblSelectAcc, cmbSelectAccount, btnAddTx, btnDeleteTx, btnRefreshTx, lblBalance });

            dgvTransactions = CreateDgv();
            dgvTransactions.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", FillWeight = 5 },
                new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "Tarih", FillWeight = 12 },
                new DataGridViewTextBoxColumn { Name = "colType", HeaderText = "Tür", FillWeight = 10 },
                new DataGridViewTextBoxColumn { Name = "colDesc", HeaderText = "Açıklama", FillWeight = 30 },
                new DataGridViewTextBoxColumn { Name = "colDocNo", HeaderText = "Belge No", FillWeight = 12 },
                new DataGridViewTextBoxColumn { Name = "colAmount", HeaderText = "Tutar", FillWeight = 12, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "colNotes", HeaderText = "Notlar", FillWeight = 19 }
            });

            tabTransactions.Controls.AddRange(new Control[] { dgvTransactions, pnlTxToolbar });

            tabControl.TabPages.AddRange(new TabPage[] { tabAccounts, tabTransactions });
            this.Controls.Add(tabControl);
        }

        private Button CreateBtn(string text, Color color, int x)
        {
            return new Button
            {
                Text = text,
                Size = new Size(115, 32),
                Location = new Point(x, 8),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
        }

        private DataGridView CreateDgv()
        {
            var dgv = new DataGridView
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
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(41, 128, 185);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            return dgv;
        }

        private void LoadAccounts()
        {
            dgvAccounts.Rows.Clear();
            cmbSelectAccount.Items.Clear();

            if (_activeCompany == null) return;

            string typeFilter = cmbAccountTypeFilter.SelectedIndex switch { 1 => "Customer", 2 => "Supplier", _ => null };

            _accounts = typeFilter != null
                ? _currentAccountService.GetAccountsByCompanyAndType(_activeCompany.CompanyId, typeFilter)
                : _currentAccountService.GetAccountsByCompany(_activeCompany.CompanyId);

            foreach (var acc in _accounts)
            {
                decimal balance = _txRepository.GetBalance(acc.CurrentAccountId);
                dgvAccounts.Rows.Add(
                    acc.CurrentAccountId,
                    acc.Title,
                    acc.AccountType == "Customer" ? "Müşteri" : "Tedarikçi",
                    acc.TaxNumber,
                    acc.Phone,
                    acc.Email,
                    $"₺{balance:N2}",
                    acc.Notes
                );
                cmbSelectAccount.Items.Add(acc);
            }

            if (cmbSelectAccount.Items.Count > 0)
                cmbSelectAccount.SelectedIndex = 0;
        }

        private void LoadTransactions()
        {
            dgvTransactions.Rows.Clear();
            if (cmbSelectAccount.SelectedItem is not CurrentAccount selected) return;

            _transactions = _txRepository.GetByCurrentAccountId(selected.CurrentAccountId);
            decimal balance = _txRepository.GetBalance(selected.CurrentAccountId);
            lblBalance.Text = $"Bakiye: ₺{balance:N2}";
            lblBalance.ForeColor = balance >= 0 ? Color.FromArgb(39, 174, 96) : Color.FromArgb(231, 76, 60);

            foreach (var tx in _transactions)
            {
                dgvTransactions.Rows.Add(
                    tx.TransactionId,
                    tx.TransactionDate.ToString("dd.MM.yyyy"),
                    tx.TransactionType == "Debit" ? "Borç" : "Alacak",
                    tx.Description,
                    tx.RelatedDocumentNumber,
                    $"₺{tx.Amount:N2}",
                    tx.Notes
                );
            }
        }

        private void DgvAccounts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvAccounts.SelectedRows.Count > 0)
            {
                int id = Convert.ToInt32(dgvAccounts.SelectedRows[0].Cells["colId"].Value);
                for (int i = 0; i < cmbSelectAccount.Items.Count; i++)
                {
                    if (((CurrentAccount)cmbSelectAccount.Items[i]).CurrentAccountId == id)
                    {
                        cmbSelectAccount.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void BtnAddAccount_Click(object sender, EventArgs e)
        {
            using (var dlg = new CurrentAccountEditForm(_activeCompany, null))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    LoadAccounts();
            }
        }

        private void BtnEditAccount_Click(object sender, EventArgs e)
        {
            if (dgvAccounts.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(dgvAccounts.SelectedRows[0].Cells["colId"].Value);
            var acc = _accounts?.Find(a => a.CurrentAccountId == id);
            if (acc == null) return;

            using (var dlg = new CurrentAccountEditForm(_activeCompany, acc))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    LoadAccounts();
            }
        }

        private void BtnDeleteAccount_Click(object sender, EventArgs e)
        {
            if (dgvAccounts.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(dgvAccounts.SelectedRows[0].Cells["colId"].Value);
            string title = dgvAccounts.SelectedRows[0].Cells["colTitle"].Value?.ToString();

            if (MessageBox.Show($"'{title}' cari kartını silmek istediğinizden emin misiniz?",
                "Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (_currentAccountService.DeleteCurrentAccount(id))
                    LoadAccounts();
                else
                    MessageBox.Show("Silme işlemi başarısız.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAddTx_Click(object sender, EventArgs e)
        {
            if (cmbSelectAccount.SelectedItem is not CurrentAccount selected)
            {
                MessageBox.Show("Lütfen bir cari hesap seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dlg = new CurrentAccountTxEditForm(selected, _currentUser))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    LoadTransactions();
            }
        }

        private void BtnDeleteTx_Click(object sender, EventArgs e)
        {
            if (dgvTransactions.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(dgvTransactions.SelectedRows[0].Cells["colId"].Value);

            if (MessageBox.Show("Bu hareketi silmek istediğinizden emin misiniz?",
                "Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (_txRepository.Delete(id))
                    LoadTransactions();
                else
                    MessageBox.Show("Silme işlemi başarısız.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class CurrentAccountEditForm : Form
    {
        private TextBox txtTitle, txtTaxNo, txtTaxId, txtPhone, txtEmail, txtAddress, txtNotes;
        private ComboBox cmbType;
        private Button btnSave, btnCancel;
        private readonly Company _company;
        private readonly CurrentAccount _editing;
        private readonly CurrentAccountService _service;

        public CurrentAccountEditForm(Company company, CurrentAccount editing)
        {
            _company = company;
            _editing = editing;
            _service = new CurrentAccountService(Program.ConnectionString);
            InitializeComponent();
            if (editing != null) PopulateFields(editing);
        }

        private void InitializeComponent()
        {
            this.Text = _editing == null ? "Yeni Cari Kart" : "Cari Kart Düzenle";
            this.Size = new Size(450, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9f);

            int y = 20, lx = 20, cx = 140, cw = 270;

            Control AddField(string label, Control ctrl)
            {
                var lbl = new Label { Text = label, AutoSize = true, Location = new Point(lx, y + 4), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
                ctrl.Location = new Point(cx, y);
                ctrl.Size = new Size(cw, 25);
                this.Controls.AddRange(new Control[] { lbl, ctrl });
                y += 33;
                return ctrl;
            }

            cmbType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbType.Items.AddRange(new object[] { "Müşteri", "Tedarikçi" });
            cmbType.SelectedIndex = 0;
            AddField("Tür:*", cmbType);

            txtTitle = new TextBox(); AddField("Unvan:*", txtTitle);
            txtTaxNo = new TextBox(); AddField("Vergi No:", txtTaxNo);
            txtTaxId = new TextBox(); AddField("TC Kimlik:", txtTaxId);
            txtPhone = new TextBox(); AddField("Telefon:", txtPhone);
            txtEmail = new TextBox(); AddField("E-posta:", txtEmail);
            txtAddress = new TextBox(); AddField("Adres:", txtAddress);
            txtNotes = new TextBox(); AddField("Notlar:", txtNotes);

            btnSave = new Button
            {
                Text = "💾 Kaydet", Size = new Size(130, 35), Location = new Point(cx, y + 5),
                BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text = "İptal", Size = new Size(90, 35), Location = new Point(cx + 140, y + 5),
                BackColor = Color.FromArgb(236, 240, 241), ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
            this.ClientSize = new Size(430, y + 55);
        }

        private void PopulateFields(CurrentAccount acc)
        {
            cmbType.SelectedIndex = acc.AccountType == "Supplier" ? 1 : 0;
            txtTitle.Text = acc.Title;
            txtTaxNo.Text = acc.TaxNumber;
            txtTaxId.Text = acc.TaxId;
            txtPhone.Text = acc.Phone;
            txtEmail.Text = acc.Email;
            txtAddress.Text = acc.Address;
            txtNotes.Text = acc.Notes;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            { MessageBox.Show("Unvan zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            string type = cmbType.SelectedIndex == 1 ? "Supplier" : "Customer";

            if (_editing == null)
            {
                var result = _service.CreateCurrentAccount(_company.CompanyId, txtTitle.Text.Trim(), type,
                    txtTaxNo.Text.Trim(), txtTaxId.Text.Trim(), txtPhone.Text.Trim(), txtEmail.Text.Trim(), txtAddress.Text.Trim());
                if (result != null) { this.DialogResult = DialogResult.OK; }
                else MessageBox.Show("Kayıt eklenirken hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                _editing.AccountType = type;
                _editing.Title = txtTitle.Text.Trim();
                _editing.TaxNumber = txtTaxNo.Text.Trim();
                _editing.TaxId = txtTaxId.Text.Trim();
                _editing.Phone = txtPhone.Text.Trim();
                _editing.Email = txtEmail.Text.Trim();
                _editing.Address = txtAddress.Text.Trim();
                _editing.Notes = txtNotes.Text.Trim();

                if (_service.UpdateCurrentAccount(_editing)) { this.DialogResult = DialogResult.OK; }
                else MessageBox.Show("Güncelleme sırasında hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class CurrentAccountTxEditForm : Form
    {
        private DateTimePicker dtpDate;
        private ComboBox cmbType;
        private TextBox txtDesc, txtAmount, txtDocNo, txtNotes;
        private Button btnSave, btnCancel;
        private readonly CurrentAccount _account;
        private readonly User _user;
        private readonly CurrentAccountTransactionRepository _repo;

        public CurrentAccountTxEditForm(CurrentAccount account, User user)
        {
            _account = account;
            _user = user;
            _repo = new CurrentAccountTransactionRepository(Program.ConnectionString);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = $"Cari Hareket Ekle - {_account.Title}";
            this.Size = new Size(420, 340);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9f);

            int y = 20, lx = 20, cx = 140, cw = 240;

            void AddRow(string label, Control ctrl)
            {
                var lbl = new Label { Text = label, AutoSize = true, Location = new Point(lx, y + 4), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
                ctrl.Location = new Point(cx, y);
                ctrl.Size = new Size(cw, 25);
                this.Controls.AddRange(new Control[] { lbl, ctrl });
                y += 33;
            }

            dtpDate = new DateTimePicker { Format = DateTimePickerFormat.Short };
            AddRow("Tarih:", dtpDate);

            cmbType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbType.Items.AddRange(new object[] { "Borç (Debit)", "Alacak (Credit)" });
            cmbType.SelectedIndex = 0;
            AddRow("Hareket Tipi:*", cmbType);

            txtDesc = new TextBox(); AddRow("Açıklama:*", txtDesc);
            txtAmount = new TextBox { Text = "0,00" }; AddRow("Tutar:*", txtAmount);
            txtDocNo = new TextBox(); AddRow("Belge No:", txtDocNo);
            txtNotes = new TextBox(); AddRow("Notlar:", txtNotes);

            btnSave = new Button
            {
                Text = "💾 Kaydet", Size = new Size(120, 35), Location = new Point(cx, y + 5),
                BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text = "İptal", Size = new Size(90, 35), Location = new Point(cx + 130, y + 5),
                BackColor = Color.FromArgb(236, 240, 241), ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
            this.ClientSize = new Size(400, y + 55);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDesc.Text))
            { MessageBox.Show("Açıklama zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (!decimal.TryParse(txtAmount.Text.Replace(",", "."), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
            { MessageBox.Show("Geçerli bir tutar girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            var tx = new CurrentAccountTransaction
            {
                CurrentAccountId = _account.CurrentAccountId,
                TransactionDate = dtpDate.Value,
                TransactionType = cmbType.SelectedIndex == 0 ? "Debit" : "Credit",
                Description = txtDesc.Text.Trim(),
                Amount = amount,
                RelatedDocumentNumber = txtDocNo.Text.Trim(),
                Notes = txtNotes.Text.Trim(),
                CreatedBy = _user.UserId,
                CreatedAt = DateTime.Now
            };

            int id = _repo.Add(tx);
            if (id > 0)
            {
                MessageBox.Show("Hareket eklendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("Hareket eklenirken hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
