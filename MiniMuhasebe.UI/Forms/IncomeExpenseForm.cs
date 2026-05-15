using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    public class IncomeExpenseForm : Form
    {
        private DataGridView dgvTransactions;
        private Panel pnlToolbar;
        private Panel pnlFilter;
        private Button btnNew, btnEdit, btnDelete, btnRefresh, btnExport;
        private DateTimePicker dtpStart, dtpEnd;
        private ComboBox cmbPeriod;
        private Label lblTotal;

        private readonly IncomeExpenseService _service;
        private readonly AccountService _accountService;
        private readonly FiscalPeriodService _periodService;
        private readonly BankService _bankService;
        private readonly CurrentAccountService _currentAccountService;
        private List<IncomeExpenseTransaction> _transactions;

        public IncomeExpenseForm()
        {
            _service = new IncomeExpenseService(AppSession.ConnectionString);
            _accountService = new AccountService(AppSession.ConnectionString);
            _periodService = new FiscalPeriodService(AppSession.ConnectionString);
            _bankService = new BankService(AppSession.ConnectionString, AppSession.EncryptionKey);
            _currentAccountService = new CurrentAccountService(AppSession.ConnectionString);

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Gelir-Gider Takibi";
            this.BackColor = Color.FromArgb(245, 247, 250);

            // Toolbar
            pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.White,
                Padding = new Padding(10, 10, 10, 5)
            };

            var lblTitle = new Label
            {
                Text = "📊 Gelir-Gider Takibi",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(10, 15)
            };

            btnNew = CreateToolButton("➕ Yeni", Color.FromArgb(39, 174, 96), 200, 12);
            btnNew.Click += (s, e) => OpenTransactionDialog(null);

            btnEdit = CreateToolButton("✏️ Düzenle", Color.FromArgb(41, 128, 185), 290, 12);
            btnEdit.Click += (s, e) => EditSelected();

            btnDelete = CreateToolButton("🗑️ Sil", Color.FromArgb(192, 57, 43), 390, 12);
            btnDelete.Click += (s, e) => DeleteSelected();

            btnRefresh = CreateToolButton("🔄 Yenile", Color.FromArgb(127, 140, 141), 480, 12);
            btnRefresh.Click += (s, e) => LoadData();

            btnExport = CreateToolButton("📥 CSV Dışa Aktar", Color.FromArgb(230, 126, 34), 570, 12);
            btnExport.Width = 140;
            btnExport.Click += (s, e) => ExportToCSV();

            pnlToolbar.Controls.AddRange(new Control[] { lblTitle, btnNew, btnEdit, btnDelete, btnRefresh, btnExport });

            // Filter panel
            pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Color.FromArgb(235, 240, 245),
                Padding = new Padding(10, 8, 10, 5)
            };

            var lblPeriod = new Label { Text = "Dönem:", Font = new Font("Segoe UI", 9), AutoSize = true, Location = new Point(10, 13) };
            cmbPeriod = new ComboBox
            {
                Location = new Point(60, 10),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            cmbPeriod.SelectedIndexChanged += (s, e) => LoadData();

            var lblFrom = new Label { Text = "Başlangıç:", Font = new Font("Segoe UI", 9), AutoSize = true, Location = new Point(275, 13) };
            dtpStart = new DateTimePicker
            {
                Location = new Point(345, 10),
                Size = new Size(130, 25),
                Format = DateTimePickerFormat.Short,
                Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)
            };

            var lblTo = new Label { Text = "Bitiş:", Font = new Font("Segoe UI", 9), AutoSize = true, Location = new Point(485, 13) };
            dtpEnd = new DateTimePicker
            {
                Location = new Point(520, 10),
                Size = new Size(130, 25),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now
            };

            var btnFilter = CreateToolButton("Filtrele", Color.FromArgb(41, 128, 185), 660, 8);
            btnFilter.Height = 28;
            btnFilter.Click += (s, e) => LoadData();

            lblTotal = new Label
            {
                Text = "Toplam: 0 kayıt",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(760, 13)
            };

            pnlFilter.Controls.AddRange(new Control[] { lblPeriod, cmbPeriod, lblFrom, dtpStart, lblTo, dtpEnd, btnFilter, lblTotal });

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
                Font = new Font("Segoe UI", 9),
                GridColor = Color.FromArgb(230, 235, 240),
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(248, 250, 252) }
            };
            dgvTransactions.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvTransactions.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgvTransactions.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvTransactions.ColumnHeadersHeight = 35;
            dgvTransactions.DoubleClick += (s, e) => EditSelected();

            SetupColumns();

            this.Controls.AddRange(new Control[] { dgvTransactions, pnlFilter, pnlToolbar });

            LoadPeriods();
        }

        private Button CreateToolButton(string text, Color color, int x, int y)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = color,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(x, y),
                Size = new Size(85, 30),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void SetupColumns()
        {
            dgvTransactions.Columns.Clear();
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransactionId", HeaderText = "ID", Width = 50, FillWeight = 5 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransactionDate", HeaderText = "Tarih", FillWeight = 10 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "DocumentNumber", HeaderText = "Belge No", FillWeight = 10 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Açıklama", FillWeight = 25 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Tutar (₺)", FillWeight = 12 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "VatRate", HeaderText = "KDV %", FillWeight = 8 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "VatAmount", HeaderText = "KDV (₺)", FillWeight = 10 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "NetAmount", HeaderText = "Net (₺)", FillWeight = 12 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "PaymentType", HeaderText = "Ödeme Tipi", FillWeight = 10 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "Notlar", FillWeight = 18 });
        }

        private void LoadPeriods()
        {
            cmbPeriod.Items.Clear();
            cmbPeriod.Items.Add(new ComboItem("Tüm Dönemler", 0));

            if (AppSession.CurrentCompany != null)
            {
                var periods = _periodService.GetPeriodsByCompany(AppSession.CurrentCompany.CompanyId);
                foreach (var p in periods)
                    cmbPeriod.Items.Add(new ComboItem(p.PeriodName, p.PeriodId));
            }

            // Aktif dönemi seç
            if (AppSession.CurrentPeriod != null)
            {
                for (int i = 0; i < cmbPeriod.Items.Count; i++)
                {
                    if (((ComboItem)cmbPeriod.Items[i]).Value == AppSession.CurrentPeriod.PeriodId)
                    {
                        cmbPeriod.SelectedIndex = i;
                        return;
                    }
                }
            }
            cmbPeriod.SelectedIndex = 0;
        }

        private void LoadData()
        {
            try
            {
                if (AppSession.CurrentCompany == null) return;

                int companyId = AppSession.CurrentCompany.CompanyId;
                int selectedPeriod = cmbPeriod.SelectedItem != null ? ((ComboItem)cmbPeriod.SelectedItem).Value : 0;

                if (selectedPeriod > 0)
                    _transactions = _service.GetTransactionsByPeriod(selectedPeriod);
                else
                    _transactions = _service.GetTransactionsByDateRange(companyId, dtpStart.Value, dtpEnd.Value);

                dgvTransactions.Rows.Clear();
                decimal totalAmount = 0;

                foreach (var t in _transactions)
                {
                    string paymentTypeDisplay = t.PaymentType == "Cash" ? "Nakit" :
                                               t.PaymentType == "Bank" ? "Banka" : "Cari";
                    dgvTransactions.Rows.Add(
                        t.TransactionId,
                        t.TransactionDate.ToString("dd.MM.yyyy"),
                        t.DocumentNumber,
                        t.Description,
                        t.Amount.ToString("N2"),
                        t.VatRate.ToString("N0") + "%",
                        t.VatAmount.ToString("N2"),
                        t.NetAmount.ToString("N2"),
                        paymentTypeDisplay,
                        t.Notes
                    );
                    totalAmount += t.Amount;
                }

                lblTotal.Text = $"Toplam: {_transactions.Count} kayıt | Tutar: {totalAmount:N2} ₺";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veri yüklenirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenTransactionDialog(IncomeExpenseTransaction existing)
        {
            if (AppSession.CurrentCompany == null || AppSession.CurrentPeriod == null)
            {
                MessageBox.Show("Lütfen önce firma ve dönem seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dialog = new IncomeExpenseEditDialog(existing, AppSession.CurrentCompany.CompanyId,
                AppSession.CurrentPeriod.PeriodId, _accountService, _bankService, _currentAccountService);

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var data = dialog.TransactionData;
                if (existing == null)
                {
                    _service.CreateTransaction(
                        data.CompanyId, data.PeriodId, data.AccountId, data.TransactionDate,
                        data.Description, data.Amount, data.VatRate, data.PaymentType,
                        data.BankAccountId, data.CurrentAccountId, AppSession.CurrentUser?.UserId,
                        data.DocumentNumber, data.Notes);
                }
                else
                {
                    data.UpdatedBy = AppSession.CurrentUser?.UserId;
                    _service.UpdateTransaction(data, AppSession.CurrentUser?.UserId);
                }
                LoadData();
            }
        }

        private void EditSelected()
        {
            if (dgvTransactions.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(dgvTransactions.SelectedRows[0].Cells["TransactionId"].Value);
            var tx = _transactions.Find(t => t.TransactionId == id);
            if (tx != null) OpenTransactionDialog(tx);
        }

        private void DeleteSelected()
        {
            if (dgvTransactions.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(dgvTransactions.SelectedRows[0].Cells["TransactionId"].Value);

            var result = MessageBox.Show("Bu kaydı silmek istediğinizden emin misiniz?", "Silme Onayı",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                if (_service.DeleteTransaction(id))
                    LoadData();
                else
                    MessageBox.Show("Silme işlemi başarısız!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportToCSV()
        {
            if (_transactions == null || _transactions.Count == 0)
            {
                MessageBox.Show("Dışa aktarılacak veri yok.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV Dosyası|*.csv";
                sfd.FileName = $"GelirGider_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine("ID,Tarih,Belge No,Açıklama,Tutar,KDV%,KDV Tutarı,Net Tutar,Ödeme Tipi,Notlar");

                        foreach (var t in _transactions)
                        {
                            sb.AppendLine($"{t.TransactionId},{t.TransactionDate:dd.MM.yyyy},{t.DocumentNumber}," +
                                         $"\"{t.Description}\",{t.Amount},{t.VatRate},{t.VatAmount},{t.NetAmount}," +
                                         $"{t.PaymentType},\"{t.Notes}\"");
                        }

                        System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                        MessageBox.Show($"Dosya kaydedildi:\n{sfd.FileName}", "Başarılı",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Dışa aktarma hatası: {ex.Message}", "Hata",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }

    // Yardımcı sınıf
    public class ComboItem
    {
        public string Text { get; set; }
        public int Value { get; set; }
        public ComboItem(string text, int value) { Text = text; Value = value; }
        public override string ToString() => Text;
    }

    // Gelir-Gider düzenleme dialog
    public class IncomeExpenseEditDialog : Form
    {
        public IncomeExpenseTransaction TransactionData { get; private set; }

        private ComboBox cmbAccount, cmbPaymentType, cmbBankAccount, cmbCurrentAccount, cmbVatRate;
        private DateTimePicker dtpDate;
        private TextBox txtDocNumber, txtDescription, txtAmount, txtNotes;
        private Label lblAmount, lblVat, lblNet;

        private readonly int _companyId;
        private readonly int _periodId;
        private readonly AccountService _accountService;
        private readonly BankService _bankService;
        private readonly CurrentAccountService _currentAccountService;

        public IncomeExpenseEditDialog(IncomeExpenseTransaction existing, int companyId, int periodId,
            AccountService accountService, BankService bankService, CurrentAccountService currentAccountService)
        {
            _companyId = companyId;
            _periodId = periodId;
            _accountService = accountService;
            _bankService = bankService;
            _currentAccountService = currentAccountService;
            TransactionData = existing ?? new IncomeExpenseTransaction
            {
                CompanyId = companyId,
                PeriodId = periodId,
                TransactionDate = DateTime.Today,
                PaymentType = "Bank"
            };

            InitializeComponent();
            LoadComboData();
            if (existing != null) PopulateFields(existing);
        }

        private void InitializeComponent()
        {
            this.Text = TransactionData.TransactionId == 0 ? "Yeni Kayıt" : "Kaydı Düzenle";
            this.Size = new Size(520, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            int y = 20, lx = 20, cx = 160, w = 320;

            AddLabel("Hesap:", lx, y); cmbAccount = AddCombo(cx, y, w); y += 40;
            AddLabel("Tarih:", lx, y); dtpDate = new DateTimePicker { Location = new Point(cx, y), Size = new Size(w, 25), Format = DateTimePickerFormat.Short, Value = TransactionData.TransactionDate }; this.Controls.Add(dtpDate); y += 40;
            AddLabel("Belge No:", lx, y); txtDocNumber = AddTextBox(cx, y, w, TransactionData.DocumentNumber); y += 40;
            AddLabel("Açıklama:", lx, y); txtDescription = AddTextBox(cx, y, w, TransactionData.Description); y += 40;
            AddLabel("Tutar (₺):", lx, y); txtAmount = AddTextBox(cx, y, 150, TransactionData.Amount > 0 ? TransactionData.Amount.ToString("N2") : ""); y += 40;
            AddLabel("KDV Oranı:", lx, y); cmbVatRate = AddCombo(cx, y, 100); y += 40;

            var lblCalc = new Label { Text = "KDV: 0,00 ₺  |  Net: 0,00 ₺", Font = new Font("Segoe UI", 9), ForeColor = Color.Gray, AutoSize = true, Location = new Point(cx, y) };
            lblAmount = lblCalc;
            this.Controls.Add(lblCalc); y += 35;

            AddLabel("Ödeme Tipi:", lx, y); cmbPaymentType = AddCombo(cx, y, 150); y += 40;
            AddLabel("Banka Hesabı:", lx, y); cmbBankAccount = AddCombo(cx, y, w); y += 40;
            AddLabel("Cari Hesap:", lx, y); cmbCurrentAccount = AddCombo(cx, y, w); y += 40;
            AddLabel("Notlar:", lx, y); txtNotes = AddTextBox(cx, y, w, TransactionData.Notes); y += 40;

            var btnOk = new Button { Text = "Kaydet", DialogResult = DialogResult.OK, Location = new Point(cx, y), Size = new Size(100, 35), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += BtnOk_Click;

            var btnCancel = new Button { Text = "İptal", DialogResult = DialogResult.Cancel, Location = new Point(cx + 110, y), Size = new Size(100, 35), BackColor = Color.FromArgb(127, 140, 141), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10) };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] { btnOk, btnCancel });
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;

            txtAmount.TextChanged += (s, e) => UpdateCalculation();
            cmbVatRate.SelectedIndexChanged += (s, e) => UpdateCalculation();
            cmbPaymentType.SelectedIndexChanged += (s, e) => UpdatePaymentFields();
        }

        private void LoadComboData()
        {
            // Hesaplar
            cmbAccount.Items.Add(new ComboItem("-- Hesap Seçin --", 0));
            var accounts = _accountService.GetAccountsByCompany(_companyId);
            foreach (var a in accounts)
                cmbAccount.Items.Add(new ComboItem($"[{a.AccountCode}] {a.AccountName} ({a.AccountType})", a.AccountId));
            cmbAccount.SelectedIndex = 0;

            // KDV oranları
            foreach (int rate in new[] { 0, 1, 8, 18, 20 })
                cmbVatRate.Items.Add(new ComboItem($"%{rate}", rate));
            cmbVatRate.SelectedIndex = 0;

            // Ödeme tipleri
            cmbPaymentType.Items.Add(new ComboItem("Banka", 1));
            cmbPaymentType.Items.Add(new ComboItem("Nakit", 2));
            cmbPaymentType.Items.Add(new ComboItem("Cari Hesap", 3));
            cmbPaymentType.SelectedIndex = 0;

            // Banka hesapları
            cmbBankAccount.Items.Add(new ComboItem("-- Seçin --", 0));
            var bankAccounts = _bankService.GetAccountsByCompany(_companyId);
            foreach (var ba in bankAccounts)
                cmbBankAccount.Items.Add(new ComboItem($"{ba.BankName} - {ba.AccountName}", ba.BankAccountId));
            cmbBankAccount.SelectedIndex = 0;

            // Cari hesaplar
            cmbCurrentAccount.Items.Add(new ComboItem("-- Seçin --", 0));
            var currentAccounts = _currentAccountService.GetAccountsByCompany(_companyId);
            foreach (var ca in currentAccounts)
                cmbCurrentAccount.Items.Add(new ComboItem($"{ca.Title} ({ca.AccountType})", ca.CurrentAccountId));
            cmbCurrentAccount.SelectedIndex = 0;
        }

        private void PopulateFields(IncomeExpenseTransaction t)
        {
            dtpDate.Value = t.TransactionDate;
            txtDocNumber.Text = t.DocumentNumber;
            txtDescription.Text = t.Description;
            txtAmount.Text = t.Amount.ToString("N2");
            txtNotes.Text = t.Notes;

            // Seçimleri ayarla
            for (int i = 0; i < cmbAccount.Items.Count; i++)
                if (((ComboItem)cmbAccount.Items[i]).Value == t.AccountId) { cmbAccount.SelectedIndex = i; break; }

            for (int i = 0; i < cmbVatRate.Items.Count; i++)
                if (((ComboItem)cmbVatRate.Items[i]).Value == (int)t.VatRate) { cmbVatRate.SelectedIndex = i; break; }

            string paymentText = t.PaymentType == "Cash" ? "Nakit" : t.PaymentType == "Bank" ? "Banka" : "Cari Hesap";
            for (int i = 0; i < cmbPaymentType.Items.Count; i++)
                if (((ComboItem)cmbPaymentType.Items[i]).Text == paymentText) { cmbPaymentType.SelectedIndex = i; break; }
        }

        private void UpdateCalculation()
        {
            if (decimal.TryParse(txtAmount.Text.Replace(".", "").Replace(",", "."), out decimal amount))
            {
                int vatRate = cmbVatRate.SelectedItem != null ? ((ComboItem)cmbVatRate.SelectedItem).Value : 0;
                decimal vatAmount = (amount * vatRate) / 100;
                decimal netAmount = amount - vatAmount;
                lblAmount.Text = $"KDV: {vatAmount:N2} ₺  |  Net: {netAmount:N2} ₺";
            }
        }

        private void UpdatePaymentFields()
        {
            if (cmbPaymentType.SelectedItem == null) return;
            string selected = ((ComboItem)cmbPaymentType.SelectedItem).Text;
            cmbBankAccount.Enabled = selected == "Banka";
            cmbCurrentAccount.Enabled = selected == "Cari Hesap";
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (cmbAccount.SelectedItem == null || ((ComboItem)cmbAccount.SelectedItem).Value == 0)
            { MessageBox.Show("Lütfen hesap seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (string.IsNullOrWhiteSpace(txtDescription.Text))
            { MessageBox.Show("Açıklama gereklidir.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (!decimal.TryParse(txtAmount.Text.Replace(".", "").Replace(",", "."), out decimal amount) || amount <= 0)
            { MessageBox.Show("Geçerli bir tutar girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            int vatRate = ((ComboItem)cmbVatRate.SelectedItem).Value;
            decimal vatAmount = (amount * vatRate) / 100;
            string paymentType = ((ComboItem)cmbPaymentType.SelectedItem).Text == "Nakit" ? "Cash" :
                                 ((ComboItem)cmbPaymentType.SelectedItem).Text == "Banka" ? "Bank" : "CurrentAccount";

            TransactionData.AccountId = ((ComboItem)cmbAccount.SelectedItem).Value;
            TransactionData.TransactionDate = dtpDate.Value;
            TransactionData.DocumentNumber = txtDocNumber.Text.Trim();
            TransactionData.Description = txtDescription.Text.Trim();
            TransactionData.Amount = amount;
            TransactionData.VatRate = vatRate;
            TransactionData.VatAmount = vatAmount;
            TransactionData.NetAmount = amount - vatAmount;
            TransactionData.PaymentType = paymentType;
            TransactionData.Notes = txtNotes.Text.Trim();

            int bankId = cmbBankAccount.SelectedItem != null ? ((ComboItem)cmbBankAccount.SelectedItem).Value : 0;
            TransactionData.BankAccountId = bankId > 0 ? bankId : (int?)null;

            int currentId = cmbCurrentAccount.SelectedItem != null ? ((ComboItem)cmbCurrentAccount.SelectedItem).Value : 0;
            TransactionData.CurrentAccountId = currentId > 0 ? currentId : (int?)null;

            this.DialogResult = DialogResult.OK;
        }

        private void AddLabel(string text, int x, int y)
        {
            this.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(x, y + 3)
            });
        }

        private TextBox AddTextBox(int x, int y, int width, string text = "")
        {
            var tb = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(width, 25),
                Font = new Font("Segoe UI", 9),
                Text = text ?? ""
            };
            this.Controls.Add(tb);
            return tb;
        }

        private ComboBox AddCombo(int x, int y, int width)
        {
            var cb = new ComboBox
            {
                Location = new Point(x, y),
                Size = new Size(width, 25),
                Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            this.Controls.Add(cb);
            return cb;
        }
    }
}
