using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    /// <summary>
    /// Manuel eşleştirme formu
    /// </summary>
    public class MatchingForm : Form
    {
        private DataGridView dgvBankTx, dgvIncomeTx;
        private Button btnMatch, btnRemoveMatch, btnAutoMatch;
        private ComboBox cmbBankAccount;
        private Label lblInfo;
        private BankService _bankService;
        private IncomeExpenseService _incomeExpenseService;
        private MatchingService _matchingService;

        public MatchingForm()
        {
            _bankService = new BankService(Program.ConnectionString, Program.EncryptionKey);
            _incomeExpenseService = new IncomeExpenseService(Program.ConnectionString);
            _matchingService = new MatchingService(Program.ConnectionString);
            InitializeComponent();
            LoadBankAccounts();
        }

        private void InitializeComponent()
        {
            this.Text = "Banka Hareketi Eşleştirme";
            this.Size = new Size(1100, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 247, 250);

            // Üst panel
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White, Padding = new Padding(10) };
            var lblAcc = new Label { Text = "Banka Hesabı:", Location = new Point(10, 15), Size = new Size(90, 20) };
            cmbBankAccount = new ComboBox { Location = new Point(105, 12), Size = new Size(220, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbBankAccount.SelectedIndexChanged += (s, e) => LoadUnmatchedTransactions();
            var btnLoad = new Button { Text = "Listele", Location = new Point(340, 10), Size = new Size(80, 28), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnLoad.FlatAppearance.BorderSize = 0;
            btnLoad.Click += (s, e) => LoadUnmatchedTransactions();
            pnlTop.Controls.AddRange(new Control[] { lblAcc, cmbBankAccount, btnLoad });

            // Araç çubuğu
            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(52, 73, 94), Padding = new Padding(10, 7, 10, 7) };
            btnMatch = CreateBtn("🔗 Eşleştir", Color.FromArgb(39, 174, 96));
            btnMatch.Click += BtnMatch_Click;
            btnRemoveMatch = CreateBtn("❌ Eşleştirmeyi Kaldır", Color.FromArgb(231, 76, 60));
            btnRemoveMatch.Click += BtnRemoveMatch_Click;
            btnAutoMatch = CreateBtn("⚡ Otomatik Eşleştir", Color.FromArgb(142, 68, 173));
            btnAutoMatch.Click += BtnAutoMatch_Click;

            int x = 10;
            foreach (var btn in new[] { btnMatch, btnRemoveMatch, btnAutoMatch })
            { btn.Location = new Point(x, 7); pnlToolbar.Controls.Add(btn); x += btn.Width + 5; }

            // Split panel
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 280,
                BackColor = Color.FromArgb(245, 247, 250)
            };

            // Üst: Banka hareketleri
            var lblBank = new Label { Text = "📋 Eşleşmeyen Banka Hareketleri", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(41, 128, 185), Dock = DockStyle.Top, Height = 28, Padding = new Padding(5, 5, 0, 0) };
            dgvBankTx = CreateGrid();
            splitContainer.Panel1.Controls.Add(dgvBankTx);
            splitContainer.Panel1.Controls.Add(lblBank);

            // Alt: Gelir/gider kayıtları
            var lblIncome = new Label { Text = "📋 Gelir-Gider Kayıtları (Eşleştirmek için seçin)", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(39, 174, 96), Dock = DockStyle.Top, Height = 28, Padding = new Padding(5, 5, 0, 0) };
            dgvIncomeTx = CreateGrid();
            splitContainer.Panel2.Controls.Add(dgvIncomeTx);
            splitContainer.Panel2.Controls.Add(lblIncome);

            // Bilgi etiketi
            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 35, BackColor = Color.FromArgb(52, 73, 94) };
            lblInfo = new Label { ForeColor = Color.White, Font = new Font("Segoe UI", 9), Location = new Point(10, 8), Size = new Size(900, 20) };
            lblInfo.Text = "Banka hareketi ve gelir/gider kaydı seçerek 'Eşleştir' butonuna tıklayın.";
            pnlBottom.Controls.Add(lblInfo);

            this.Controls.Add(splitContainer);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);
            this.Controls.Add(pnlToolbar);

            LoadIncomeTransactions();
        }

        private DataGridView CreateGrid()
        {
            var dgv = new DataGridView
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
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            return dgv;
        }

        private Button CreateBtn(string text, Color color)
        {
            var btn = new Button { Text = text, Size = new Size(160, 30), BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8, FontStyle.Bold), Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void LoadBankAccounts()
        {
            if (!Program.ActiveCompanyId.HasValue) return;
            var accounts = _bankService.GetAccountsByCompany(Program.ActiveCompanyId.Value);
            cmbBankAccount.DataSource = accounts;
            cmbBankAccount.DisplayMember = "BankName";
            cmbBankAccount.ValueMember = "BankAccountId";
        }

        private void LoadUnmatchedTransactions()
        {
            if (cmbBankAccount.SelectedValue == null) return;
            int bankAccountId = Convert.ToInt32(cmbBankAccount.SelectedValue);
            var transactions = _bankService.GetUnmatchedTransactions(bankAccountId);

            dgvBankTx.Columns.Clear();
            dgvBankTx.Columns.Add(new DataGridViewTextBoxColumn { Name = "BankTransactionId", HeaderText = "ID", DataPropertyName = "BankTransactionId", Width = 50 });
            dgvBankTx.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransactionDate", HeaderText = "Tarih", DataPropertyName = "TransactionDate", DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } });
            dgvBankTx.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Açıklama", DataPropertyName = "Description" });
            dgvBankTx.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Tutar (₺)", DataPropertyName = "Amount", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgvBankTx.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransactionType", HeaderText = "Tür", DataPropertyName = "TransactionType", Width = 80 });
            dgvBankTx.DataSource = transactions;
        }

        private void LoadIncomeTransactions()
        {
            if (!Program.ActivePeriodId.HasValue) return;
            var transactions = _incomeExpenseService.GetTransactionsByPeriod(Program.ActivePeriodId.Value);

            dgvIncomeTx.Columns.Clear();
            dgvIncomeTx.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransactionId", HeaderText = "ID", DataPropertyName = "TransactionId", Width = 50 });
            dgvIncomeTx.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransactionDate", HeaderText = "Tarih", DataPropertyName = "TransactionDate", DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } });
            dgvIncomeTx.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Açıklama", DataPropertyName = "Description" });
            dgvIncomeTx.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Tutar (₺)", DataPropertyName = "Amount", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgvIncomeTx.Columns.Add(new DataGridViewTextBoxColumn { Name = "PaymentType", HeaderText = "Ödeme Tipi", DataPropertyName = "PaymentType", Width = 100 });
            dgvIncomeTx.DataSource = transactions;
        }

        private void BtnMatch_Click(object sender, EventArgs e)
        {
            if (dgvBankTx.SelectedRows.Count == 0 || dgvIncomeTx.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen hem banka hareketi hem de gelir/gider kaydı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int bankTxId = Convert.ToInt32(dgvBankTx.SelectedRows[0].Cells["BankTransactionId"].Value);
            int incomeTxId = Convert.ToInt32(dgvIncomeTx.SelectedRows[0].Cells["TransactionId"].Value);

            var result = _matchingService.ManualMatch(bankTxId, incomeTxId, Program.CurrentUserId);
            if (result != null)
            {
                MessageBox.Show("Eşleştirme başarılı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadUnmatchedTransactions();
            }
            else
                MessageBox.Show("Eşleştirme başarısız. Bu hareket zaten eşleştirilmiş olabilir.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void BtnRemoveMatch_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Eşleştirmeyi kaldırmak için Banka Hareketleri ekranından eşleşmiş hareketi seçin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnAutoMatch_Click(object sender, EventArgs e)
        {
            if (!Program.ActiveCompanyId.HasValue || cmbBankAccount.SelectedValue == null) return;
            int bankAccountId = Convert.ToInt32(cmbBankAccount.SelectedValue);
            var result = _matchingService.AutoMatch(Program.ActiveCompanyId.Value, bankAccountId, Program.CurrentUserId);
            MessageBox.Show($"Otomatik eşleştirme tamamlandı.\nEşleştirilen: {result.MatchedCount} / {result.TotalProcessed}", "Sonuç", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadUnmatchedTransactions();
        }
    }
}
