using System;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    /// <summary>
    /// Gelir-Gider kayıt ekleme/düzenleme formu
    /// </summary>
    public class IncomeExpenseEditForm : Form
    {
        private DateTimePicker dtpDate;
        private TextBox txtDocNumber, txtDescription, txtAmount, txtNotes;
        private ComboBox cmbAccount, cmbPaymentType, cmbBankAccount, cmbCurrentAccount, cmbVatRate;
        private Label lblBankAccount, lblCurrentAccount;
        private Button btnSave, btnCancel;
        private Label lblVatAmount, lblNetAmount;

        private IncomeExpenseTransaction _transaction;
        private IncomeExpenseService _service;
        private AccountService _accountService;
        private BankService _bankService;
        private CurrentAccountService _currentAccountService;

        public IncomeExpenseEditForm(IncomeExpenseTransaction transaction)
        {
            _transaction = transaction;
            _service = new IncomeExpenseService(Program.ConnectionString);
            _accountService = new AccountService(Program.ConnectionString);
            _bankService = new BankService(Program.ConnectionString, Program.EncryptionKey);
            _currentAccountService = new CurrentAccountService(Program.ConnectionString);
            InitializeComponent();
            LoadComboBoxes();
            if (_transaction != null) FillForm();
        }

        private void InitializeComponent()
        {
            this.Text = _transaction == null ? "Yeni Gelir/Gider Kaydı" : "Kaydı Düzenle";
            this.Size = new Size(520, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 250);

            int y = 20;
            int labelW = 130, controlW = 320, labelX = 20, controlX = 160;

            AddLabel("Tarih:", labelX, y); dtpDate = new DateTimePicker { Location = new Point(controlX, y), Size = new Size(controlW, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Today }; this.Controls.Add(dtpDate); y += 35;
            AddLabel("Belge No:", labelX, y); txtDocNumber = AddTextBox(controlX, y, controlW); y += 35;
            AddLabel("Açıklama: *", labelX, y); txtDescription = AddTextBox(controlX, y, controlW); y += 35;
            AddLabel("Hesap Kategorisi: *", labelX, y); cmbAccount = AddComboBox(controlX, y, controlW); y += 35;
            AddLabel("Tutar (₺): *", labelX, y); txtAmount = AddTextBox(controlX, y, 150); txtAmount.TextChanged += RecalculateVat; y += 35;
            AddLabel("KDV Oranı (%):", labelX, y); cmbVatRate = AddComboBox(controlX, y, 100); cmbVatRate.Items.AddRange(new object[] { 0, 1, 8, 18, 20 }); cmbVatRate.SelectedIndex = 0; cmbVatRate.SelectedIndexChanged += RecalculateVat; y += 35;
            AddLabel("KDV Tutarı:", labelX, y); lblVatAmount = new Label { Location = new Point(controlX, y), Size = new Size(150, 25), Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(41, 128, 185) }; this.Controls.Add(lblVatAmount); y += 35;
            AddLabel("Net Tutar:", labelX, y); lblNetAmount = new Label { Location = new Point(controlX, y), Size = new Size(150, 25), Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(39, 174, 96) }; this.Controls.Add(lblNetAmount); y += 35;
            AddLabel("Ödeme Tipi: *", labelX, y); cmbPaymentType = AddComboBox(controlX, y, 150); cmbPaymentType.Items.AddRange(new[] { "Cash", "Bank", "CurrentAccount" }); cmbPaymentType.SelectedIndex = 0; cmbPaymentType.SelectedIndexChanged += CmbPaymentType_Changed; y += 35;
            lblBankAccount = AddLabel("Banka Hesabı:", labelX, y); cmbBankAccount = AddComboBox(controlX, y, controlW); cmbBankAccount.Visible = false; lblBankAccount.Visible = false; y += 35;
            lblCurrentAccount = AddLabel("Cari Hesap:", labelX, y); cmbCurrentAccount = AddComboBox(controlX, y, controlW); cmbCurrentAccount.Visible = false; lblCurrentAccount.Visible = false; y += 35;
            AddLabel("Notlar:", labelX, y); txtNotes = AddTextBox(controlX, y, controlW); y += 45;

            btnSave = new Button { Text = "💾 Kaydet", Location = new Point(controlX, y), Size = new Size(150, 35), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button { Text = "İptal", Location = new Point(controlX + 160, y), Size = new Size(100, 35), BackColor = Color.FromArgb(127, 140, 141), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
            this.Height = y + 80;
        }

        private Label AddLabel(string text, int x, int y)
        {
            var lbl = new Label { Text = text, Location = new Point(x, y + 3), Size = new Size(135, 20), Font = new Font("Segoe UI", 9) };
            this.Controls.Add(lbl);
            return lbl;
        }

        private TextBox AddTextBox(int x, int y, int width)
        {
            var txt = new TextBox { Location = new Point(x, y), Size = new Size(width, 25), Font = new Font("Segoe UI", 9) };
            this.Controls.Add(txt);
            return txt;
        }

        private ComboBox AddComboBox(int x, int y, int width)
        {
            var cmb = new ComboBox { Location = new Point(x, y), Size = new Size(width, 25), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9) };
            this.Controls.Add(cmb);
            return cmb;
        }

        private void LoadComboBoxes()
        {
            if (!Program.ActiveCompanyId.HasValue) return;

            var accounts = _accountService.GetAccountsByCompany(Program.ActiveCompanyId.Value);
            cmbAccount.DataSource = accounts;
            cmbAccount.DisplayMember = "AccountName";
            cmbAccount.ValueMember = "AccountId";

            var bankAccounts = _bankService.GetAccountsByCompany(Program.ActiveCompanyId.Value);
            cmbBankAccount.DataSource = bankAccounts;
            cmbBankAccount.DisplayMember = "BankName";
            cmbBankAccount.ValueMember = "BankAccountId";

            var currentAccounts = _currentAccountService.GetAccountsByCompany(Program.ActiveCompanyId.Value);
            cmbCurrentAccount.DataSource = currentAccounts;
            cmbCurrentAccount.DisplayMember = "Title";
            cmbCurrentAccount.ValueMember = "CurrentAccountId";
        }

        private void FillForm()
        {
            dtpDate.Value = _transaction.TransactionDate;
            txtDocNumber.Text = _transaction.DocumentNumber;
            txtDescription.Text = _transaction.Description;
            txtAmount.Text = _transaction.Amount.ToString("N2");
            txtNotes.Text = _transaction.Notes;

            // KDV oranı seç
            for (int i = 0; i < cmbVatRate.Items.Count; i++)
                if (Convert.ToDecimal(cmbVatRate.Items[i]) == _transaction.VatRate) { cmbVatRate.SelectedIndex = i; break; }

            // Ödeme tipi
            cmbPaymentType.SelectedItem = _transaction.PaymentType;
        }

        private void CmbPaymentType_Changed(object sender, EventArgs e)
        {
            string type = cmbPaymentType.SelectedItem?.ToString();
            cmbBankAccount.Visible = lblBankAccount.Visible = (type == "Bank");
            cmbCurrentAccount.Visible = lblCurrentAccount.Visible = (type == "CurrentAccount");
        }

        private void RecalculateVat(object sender, EventArgs e)
        {
            if (decimal.TryParse(txtAmount.Text.Replace(",", "."), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal amount) &&
                cmbVatRate.SelectedItem != null)
            {
                decimal vatRate = Convert.ToDecimal(cmbVatRate.SelectedItem);
                decimal vatAmount = (amount * vatRate) / 100;
                decimal netAmount = amount - vatAmount;
                lblVatAmount.Text = vatAmount.ToString("N2") + " ₺";
                lblNetAmount.Text = netAmount.ToString("N2") + " ₺";
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                MessageBox.Show("Açıklama zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtAmount.Text.Replace(",", "."), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Geçerli bir tutar girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbAccount.SelectedItem == null)
            {
                MessageBox.Show("Hesap kategorisi seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Program.ActivePeriodId.HasValue)
            {
                MessageBox.Show("Aktif dönem seçilmemiş.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int accountId = Convert.ToInt32(cmbAccount.SelectedValue);
                decimal vatRate = Convert.ToDecimal(cmbVatRate.SelectedItem);
                string paymentType = cmbPaymentType.SelectedItem?.ToString() ?? "Cash";
                int? bankAccountId = paymentType == "Bank" && cmbBankAccount.SelectedValue != null ? Convert.ToInt32(cmbBankAccount.SelectedValue) : (int?)null;
                int? currentAccountId = paymentType == "CurrentAccount" && cmbCurrentAccount.SelectedValue != null ? Convert.ToInt32(cmbCurrentAccount.SelectedValue) : (int?)null;

                if (_transaction == null)
                {
                    var result = _service.CreateTransaction(
                        Program.ActiveCompanyId.Value, Program.ActivePeriodId.Value, accountId,
                        dtpDate.Value, txtDescription.Text.Trim(), amount, vatRate, paymentType,
                        bankAccountId, currentAccountId, Program.CurrentUserId,
                        txtDocNumber.Text.Trim(), txtNotes.Text.Trim());

                    if (result != null)
                    {
                        var auditService = new AuditLogService(Program.ConnectionString);
                        auditService.LogInsert("IncomeExpenseTransactions", result.TransactionId, Program.CurrentUserId, txtDescription.Text);
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                        MessageBox.Show("Kayıt eklenemedi.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    _transaction.TransactionDate = dtpDate.Value;
                    _transaction.DocumentNumber = txtDocNumber.Text.Trim();
                    _transaction.Description = txtDescription.Text.Trim();
                    _transaction.Amount = amount;
                    _transaction.VatRate = vatRate;
                    _transaction.PaymentType = paymentType;
                    _transaction.BankAccountId = bankAccountId;
                    _transaction.CurrentAccountId = currentAccountId;
                    _transaction.Notes = txtNotes.Text.Trim();
                    _transaction.AccountId = accountId;

                    if (_service.UpdateTransaction(_transaction, Program.CurrentUserId))
                    {
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                        MessageBox.Show("Güncelleme başarısız.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kayıt sırasında hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
