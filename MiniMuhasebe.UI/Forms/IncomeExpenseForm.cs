using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    public class IncomeExpenseForm : Form
    {
        private DataGridView dgvTransactions;
        private Panel pnlToolbar;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnRefresh;
        private ComboBox cmbPeriod;
        private Label lblPeriod;
        private Label lblTotal;

        private readonly User _currentUser;
        private readonly Company _activeCompany;
        private FiscalPeriod _activePeriod;

        private readonly IncomeExpenseService _incomeExpenseService;
        private readonly FiscalPeriodRepository _periodRepository;
        private readonly AccountRepository _accountRepository;
        private readonly BankAccountRepository _bankAccountRepository;
        private readonly CurrentAccountRepository _currentAccountRepository;

        private List<IncomeExpenseTransaction> _transactions;

        public IncomeExpenseForm(User user, Company company, FiscalPeriod period)
        {
            _currentUser = user;
            _activeCompany = company;
            _activePeriod = period;
            _incomeExpenseService = new IncomeExpenseService(Program.ConnectionString);
            _periodRepository = new FiscalPeriodRepository(Program.ConnectionString);
            _accountRepository = new AccountRepository(Program.ConnectionString);
            _bankAccountRepository = new BankAccountRepository(Program.ConnectionString, Program.EncryptionKey);
            _currentAccountRepository = new CurrentAccountRepository(Program.ConnectionString);

            InitializeComponent();
            LoadPeriods();
            LoadTransactions();
        }

        private void InitializeComponent()
        {
            this.Text = "Gelir / Gider İşlemleri";
            this.Size = new Size(1100, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 9f);

            // Araç çubuğu
            pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.White,
                Padding = new Padding(10, 8, 10, 8)
            };

            btnAdd = CreateToolbarButton("➕ Yeni Kayıt", Color.FromArgb(39, 174, 96));
            btnAdd.Click += BtnAdd_Click;

            btnEdit = CreateToolbarButton("✏️ Düzenle", Color.FromArgb(41, 128, 185));
            btnEdit.Location = new Point(130, 8);
            btnEdit.Click += BtnEdit_Click;

            btnDelete = CreateToolbarButton("🗑️ Sil", Color.FromArgb(231, 76, 60));
            btnDelete.Location = new Point(250, 8);
            btnDelete.Click += BtnDelete_Click;

            btnRefresh = CreateToolbarButton("🔄 Yenile", Color.FromArgb(149, 165, 166));
            btnRefresh.Location = new Point(370, 8);
            btnRefresh.Click += (s, e) => LoadTransactions();

            lblPeriod = new Label
            {
                Text = "Dönem:",
                AutoSize = true,
                Location = new Point(510, 18),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            cmbPeriod = new ComboBox
            {
                Location = new Point(560, 14),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "PeriodName"
            };
            cmbPeriod.SelectedIndexChanged += (s, e) =>
            {
                if (cmbPeriod.SelectedItem is FiscalPeriod p)
                {
                    _activePeriod = p;
                    LoadTransactions();
                }
            };

            lblTotal = new Label
            {
                Text = "Toplam: ₺0,00",
                AutoSize = true,
                Location = new Point(800, 18),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(41, 128, 185)
            };

            pnlToolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnRefresh, lblPeriod, cmbPeriod, lblTotal });

            // DataGridView
            dgvTransactions = new DataGridView
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
            dgvTransactions.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(41, 128, 185);
            dgvTransactions.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvTransactions.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgvTransactions.EnableHeadersVisualStyles = false;

            dgvTransactions.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", Width = 50, FillWeight = 5 },
                new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "Tarih", FillWeight = 10 },
                new DataGridViewTextBoxColumn { Name = "colDocNo", HeaderText = "Belge No", FillWeight = 10 },
                new DataGridViewTextBoxColumn { Name = "colDesc", HeaderText = "Açıklama", FillWeight = 25 },
                new DataGridViewTextBoxColumn { Name = "colAccount", HeaderText = "Hesap", FillWeight = 15 },
                new DataGridViewTextBoxColumn { Name = "colAmount", HeaderText = "Tutar", FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "colVat", HeaderText = "KDV", FillWeight = 8, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "colNet", HeaderText = "Net Tutar", FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "colPayment", HeaderText = "Ödeme Tipi", FillWeight = 10 },
                new DataGridViewTextBoxColumn { Name = "colNotes", HeaderText = "Notlar", FillWeight = 15 }
            });

            this.Controls.AddRange(new Control[] { dgvTransactions, pnlToolbar });
        }

        private Button CreateToolbarButton(string text, Color color)
        {
            return new Button
            {
                Text = text,
                Size = new Size(115, 35),
                Location = new Point(10, 8),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
        }

        private void LoadPeriods()
        {
            cmbPeriod.Items.Clear();
            if (_activeCompany == null) return;

            var periods = _periodRepository.GetByCompanyId(_activeCompany.CompanyId);
            foreach (var p in periods)
                cmbPeriod.Items.Add(p);

            if (_activePeriod != null)
            {
                for (int i = 0; i < cmbPeriod.Items.Count; i++)
                {
                    if (((FiscalPeriod)cmbPeriod.Items[i]).PeriodId == _activePeriod.PeriodId)
                    {
                        cmbPeriod.SelectedIndex = i;
                        break;
                    }
                }
            }
            else if (cmbPeriod.Items.Count > 0)
            {
                cmbPeriod.SelectedIndex = 0;
                _activePeriod = (FiscalPeriod)cmbPeriod.Items[0];
            }
        }

        private void LoadTransactions()
        {
            dgvTransactions.Rows.Clear();
            if (_activePeriod == null) return;

            try
            {
                _transactions = _incomeExpenseService.GetTransactionsByPeriod(_activePeriod.PeriodId);
                var accounts = _accountRepository.GetByCompanyId(_activeCompany?.CompanyId ?? 0);

                decimal total = 0;
                foreach (var tx in _transactions)
                {
                    string accountName = accounts.Find(a => a.AccountId == tx.AccountId)?.AccountName ?? tx.AccountId.ToString();
                    string paymentType = tx.PaymentType switch
                    {
                        "Cash" => "Nakit",
                        "Bank" => "Banka",
                        "CurrentAccount" => "Cari",
                        _ => tx.PaymentType
                    };

                    dgvTransactions.Rows.Add(
                        tx.TransactionId,
                        tx.TransactionDate.ToString("dd.MM.yyyy"),
                        tx.DocumentNumber,
                        tx.Description,
                        accountName,
                        $"₺{tx.Amount:N2}",
                        $"%{tx.VatRate:N0} (₺{tx.VatAmount:N2})",
                        $"₺{tx.NetAmount:N2}",
                        paymentType,
                        tx.Notes
                    );
                    total += tx.Amount;
                }

                lblTotal.Text = $"Toplam: ₺{total:N2} ({_transactions.Count} kayıt)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veriler yüklenirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (_activeCompany == null || _activePeriod == null)
            {
                MessageBox.Show("Lütfen önce firma ve dönem seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dlg = new IncomeExpenseEditForm(_currentUser, _activeCompany, _activePeriod, null))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    LoadTransactions();
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dgvTransactions.SelectedRows.Count == 0) return;

            int id = Convert.ToInt32(dgvTransactions.SelectedRows[0].Cells["colId"].Value);
            var tx = _transactions?.Find(t => t.TransactionId == id);
            if (tx == null) return;

            using (var dlg = new IncomeExpenseEditForm(_currentUser, _activeCompany, _activePeriod, tx))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    LoadTransactions();
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvTransactions.SelectedRows.Count == 0) return;

            int id = Convert.ToInt32(dgvTransactions.SelectedRows[0].Cells["colId"].Value);
            string desc = dgvTransactions.SelectedRows[0].Cells["colDesc"].Value?.ToString();

            if (MessageBox.Show($"'{desc}' kaydını silmek istediğinizden emin misiniz?",
                "Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (_incomeExpenseService.DeleteTransaction(id))
                {
                    MessageBox.Show("Kayıt silindi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadTransactions();
                }
                else
                {
                    MessageBox.Show("Kayıt silinirken hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    /// <summary>
    /// Gelir/Gider ekleme/düzenleme formu
    /// </summary>
    public class IncomeExpenseEditForm : Form
    {
        private Label lblDate, lblDocNo, lblDesc, lblAccount, lblAmount, lblVat, lblPayment, lblBankAccount, lblCurrentAccount, lblNotes;
        private DateTimePicker dtpDate;
        private TextBox txtDocNo, txtDesc, txtAmount, txtNotes;
        private ComboBox cmbAccount, cmbVat, cmbPayment, cmbBankAccount, cmbCurrentAccount;
        private Button btnSave, btnCancel;
        private Label lblVatAmount, lblNetAmount;

        private readonly User _currentUser;
        private readonly Company _company;
        private readonly FiscalPeriod _period;
        private readonly IncomeExpenseTransaction _editingTransaction;
        private readonly IncomeExpenseService _incomeExpenseService;
        private readonly AccountRepository _accountRepository;
        private readonly BankAccountRepository _bankAccountRepository;
        private readonly CurrentAccountRepository _currentAccountRepository;

        public IncomeExpenseEditForm(User user, Company company, FiscalPeriod period, IncomeExpenseTransaction tx)
        {
            _currentUser = user;
            _company = company;
            _period = period;
            _editingTransaction = tx;
            _incomeExpenseService = new IncomeExpenseService(Program.ConnectionString);
            _accountRepository = new AccountRepository(Program.ConnectionString);
            _bankAccountRepository = new BankAccountRepository(Program.ConnectionString, Program.EncryptionKey);
            _currentAccountRepository = new CurrentAccountRepository(Program.ConnectionString);

            InitializeComponent();
            LoadComboBoxes();
            if (tx != null) PopulateFields(tx);
        }

        private void InitializeComponent()
        {
            this.Text = _editingTransaction == null ? "Yeni Gelir/Gider Kaydı" : "Kaydı Düzenle";
            this.Size = new Size(520, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9f);

            int labelX = 20, controlX = 150, controlW = 320, rowH = 35, startY = 20;

            void AddRow(ref int y, string labelText, out Label lbl, out Control ctrl, Control control)
            {
                lbl = new Label { Text = labelText, AutoSize = true, Location = new Point(labelX, y + 5), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
                control.Location = new Point(controlX, y);
                control.Size = new Size(controlW, 25);
                ctrl = control;
                this.Controls.AddRange(new Control[] { lbl, control });
                y += rowH;
            }

            int y = startY;

            // Tarih
            lblDate = new Label { Text = "Tarih:", AutoSize = true, Location = new Point(labelX, y + 5), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            dtpDate = new DateTimePicker { Location = new Point(controlX, y), Size = new Size(controlW, 25), Format = DateTimePickerFormat.Short };
            this.Controls.AddRange(new Control[] { lblDate, dtpDate });
            y += rowH;

            // Belge No
            lblDocNo = new Label { Text = "Belge No:", AutoSize = true, Location = new Point(labelX, y + 5), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            txtDocNo = new TextBox { Location = new Point(controlX, y), Size = new Size(controlW, 25) };
            this.Controls.AddRange(new Control[] { lblDocNo, txtDocNo });
            y += rowH;

            // Açıklama
            lblDesc = new Label { Text = "Açıklama:*", AutoSize = true, Location = new Point(labelX, y + 5), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            txtDesc = new TextBox { Location = new Point(controlX, y), Size = new Size(controlW, 25) };
            this.Controls.AddRange(new Control[] { lblDesc, txtDesc });
            y += rowH;

            // Hesap
            lblAccount = new Label { Text = "Hesap:*", AutoSize = true, Location = new Point(labelX, y + 5), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            cmbAccount = new ComboBox { Location = new Point(controlX, y), Size = new Size(controlW, 25), DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "AccountName" };
            this.Controls.AddRange(new Control[] { lblAccount, cmbAccount });
            y += rowH;

            // Tutar
            lblAmount = new Label { Text = "Tutar:*", AutoSize = true, Location = new Point(labelX, y + 5), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            txtAmount = new TextBox { Location = new Point(controlX, y), Size = new Size(controlW, 25), Text = "0,00" };
            txtAmount.TextChanged += (s, e) => CalculateVat();
            this.Controls.AddRange(new Control[] { lblAmount, txtAmount });
            y += rowH;

            // KDV
            lblVat = new Label { Text = "KDV Oranı:", AutoSize = true, Location = new Point(labelX, y + 5), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            cmbVat = new ComboBox { Location = new Point(controlX, y), Size = new Size(100, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbVat.Items.AddRange(new object[] { "0", "1", "8", "18" });
            cmbVat.SelectedIndex = 0;
            cmbVat.SelectedIndexChanged += (s, e) => CalculateVat();

            lblVatAmount = new Label { Text = "KDV: ₺0,00 | Net: ₺0,00", AutoSize = true, Location = new Point(controlX + 110, y + 5), ForeColor = Color.Gray };
            this.Controls.AddRange(new Control[] { lblVat, cmbVat, lblVatAmount });
            y += rowH;

            // Ödeme Tipi
            lblPayment = new Label { Text = "Ödeme Tipi:*", AutoSize = true, Location = new Point(labelX, y + 5), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            cmbPayment = new ComboBox { Location = new Point(controlX, y), Size = new Size(controlW, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbPayment.Items.AddRange(new object[] { "Nakit", "Banka", "Cari" });
            cmbPayment.SelectedIndex = 0;
            cmbPayment.SelectedIndexChanged += CmbPayment_SelectedIndexChanged;
            this.Controls.AddRange(new Control[] { lblPayment, cmbPayment });
            y += rowH;

            // Banka Hesabı
            lblBankAccount = new Label { Text = "Banka Hesabı:", AutoSize = true, Location = new Point(labelX, y + 5), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            cmbBankAccount = new ComboBox { Location = new Point(controlX, y), Size = new Size(controlW, 25), DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "BankName", Enabled = false };
            this.Controls.AddRange(new Control[] { lblBankAccount, cmbBankAccount });
            y += rowH;

            // Cari Hesap
            lblCurrentAccount = new Label { Text = "Cari Hesap:", AutoSize = true, Location = new Point(labelX, y + 5), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            cmbCurrentAccount = new ComboBox { Location = new Point(controlX, y), Size = new Size(controlW, 25), DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Title", Enabled = false };
            this.Controls.AddRange(new Control[] { lblCurrentAccount, cmbCurrentAccount });
            y += rowH;

            // Notlar
            lblNotes = new Label { Text = "Notlar:", AutoSize = true, Location = new Point(labelX, y + 5), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            txtNotes = new TextBox { Location = new Point(controlX, y), Size = new Size(controlW, 25) };
            this.Controls.AddRange(new Control[] { lblNotes, txtNotes });
            y += rowH + 10;

            // Butonlar
            btnSave = new Button
            {
                Text = "💾 Kaydet",
                Size = new Size(150, 38),
                Location = new Point(controlX, y),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.None
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text = "İptal",
                Size = new Size(100, 38),
                Location = new Point(controlX + 160, y),
                BackColor = Color.FromArgb(236, 240, 241),
                ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
            this.ClientSize = new Size(490, y + 60);
        }

        private void LoadComboBoxes()
        {
            // Hesaplar
            cmbAccount.Items.Clear();
            if (_company != null)
            {
                var accounts = _accountRepository.GetByCompanyId(_company.CompanyId);
                foreach (var a in accounts)
                    cmbAccount.Items.Add(a);
            }

            // Banka hesapları
            cmbBankAccount.Items.Clear();
            if (_company != null)
            {
                var bankAccounts = _bankAccountRepository.GetByCompanyId(_company.CompanyId);
                foreach (var ba in bankAccounts)
                    cmbBankAccount.Items.Add(ba);
            }

            // Cari hesaplar
            cmbCurrentAccount.Items.Clear();
            if (_company != null)
            {
                var currentAccounts = _currentAccountRepository.GetByCompanyId(_company.CompanyId);
                foreach (var ca in currentAccounts)
                    cmbCurrentAccount.Items.Add(ca);
            }
        }

        private void PopulateFields(IncomeExpenseTransaction tx)
        {
            dtpDate.Value = tx.TransactionDate;
            txtDocNo.Text = tx.DocumentNumber;
            txtDesc.Text = tx.Description;
            txtAmount.Text = tx.Amount.ToString("N2");
            txtNotes.Text = tx.Notes;

            // Hesap seç
            for (int i = 0; i < cmbAccount.Items.Count; i++)
            {
                if (((Account)cmbAccount.Items[i]).AccountId == tx.AccountId)
                { cmbAccount.SelectedIndex = i; break; }
            }

            // KDV
            string vatStr = ((int)tx.VatRate).ToString();
            int vatIdx = cmbVat.Items.IndexOf(vatStr);
            if (vatIdx >= 0) cmbVat.SelectedIndex = vatIdx;

            // Ödeme tipi
            cmbPayment.SelectedIndex = tx.PaymentType switch { "Bank" => 1, "CurrentAccount" => 2, _ => 0 };
        }

        private void CmbPayment_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmbBankAccount.Enabled = cmbPayment.SelectedIndex == 1;
            cmbCurrentAccount.Enabled = cmbPayment.SelectedIndex == 2;
        }

        private void CalculateVat()
        {
            if (decimal.TryParse(txtAmount.Text.Replace(",", "."), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal amount))
            {
                decimal vatRate = decimal.Parse(cmbVat.SelectedItem?.ToString() ?? "0");
                decimal vatAmount = (amount * vatRate) / 100;
                decimal netAmount = amount - vatAmount;
                lblVatAmount.Text = $"KDV: ₺{vatAmount:N2} | Net: ₺{netAmount:N2}";
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDesc.Text))
            { MessageBox.Show("Açıklama zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (cmbAccount.SelectedItem == null)
            { MessageBox.Show("Hesap seçimi zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (!decimal.TryParse(txtAmount.Text.Replace(",", "."), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
            { MessageBox.Show("Geçerli bir tutar girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            try
            {
                int accountId = ((Account)cmbAccount.SelectedItem).AccountId;
                decimal vatRate = decimal.Parse(cmbVat.SelectedItem?.ToString() ?? "0");
                string paymentType = cmbPayment.SelectedIndex switch { 1 => "Bank", 2 => "CurrentAccount", _ => "Cash" };
                int? bankAccountId = (cmbPayment.SelectedIndex == 1 && cmbBankAccount.SelectedItem is BankAccount ba) ? ba.BankAccountId : (int?)null;
                int? currentAccountId = (cmbPayment.SelectedIndex == 2 && cmbCurrentAccount.SelectedItem is CurrentAccount ca) ? ca.CurrentAccountId : (int?)null;

                if (_editingTransaction == null)
                {
                    var result = _incomeExpenseService.CreateTransaction(
                        _company.CompanyId, _period.PeriodId, accountId,
                        dtpDate.Value, txtDesc.Text.Trim(), amount, vatRate, paymentType,
                        bankAccountId, currentAccountId, _currentUser.UserId,
                        txtDocNo.Text.Trim(), txtNotes.Text.Trim());

                    if (result != null)
                    {
                        MessageBox.Show("Kayıt başarıyla eklendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.DialogResult = DialogResult.OK;
                    }
                    else
                    {
                        MessageBox.Show("Kayıt eklenirken hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    _editingTransaction.TransactionDate = dtpDate.Value;
                    _editingTransaction.DocumentNumber = txtDocNo.Text.Trim();
                    _editingTransaction.Description = txtDesc.Text.Trim();
                    _editingTransaction.AccountId = accountId;
                    _editingTransaction.Amount = amount;
                    _editingTransaction.VatRate = vatRate;
                    _editingTransaction.PaymentType = paymentType;
                    _editingTransaction.BankAccountId = bankAccountId;
                    _editingTransaction.CurrentAccountId = currentAccountId;
                    _editingTransaction.Notes = txtNotes.Text.Trim();

                    if (_incomeExpenseService.UpdateTransaction(_editingTransaction, _currentUser.UserId))
                    {
                        MessageBox.Show("Kayıt güncellendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.DialogResult = DialogResult.OK;
                    }
                    else
                    {
                        MessageBox.Show("Kayıt güncellenirken hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
