using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    /// <summary>
    /// Gelir-Gider kayıtları formu
    /// </summary>
    public class IncomeExpenseForm : Form
    {
        private DataGridView dgvTransactions;
        private Button btnNew, btnEdit, btnDelete, btnRefresh, btnExport;
        private DateTimePicker dtpStart, dtpEnd;
        private ComboBox cmbPaymentType;
        private TextBox txtSearch;
        private Label lblTotal;

        private IncomeExpenseService _service;
        private AccountService _accountService;
        private List<IncomeExpenseTransaction> _transactions;

        public IncomeExpenseForm()
        {
            _service = new IncomeExpenseService(Program.ConnectionString);
            _accountService = new AccountService(Program.ConnectionString);
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Gelir-Gider Kayıtları";
            this.Size = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 247, 250);

            // Filtre paneli
            var pnlFilter = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.White, Padding = new Padding(10, 10, 10, 5) };

            var lblStart = new Label { Text = "Başlangıç:", Location = new Point(10, 18), Size = new Size(70, 20), Font = new Font("Segoe UI", 9) };
            dtpStart = new DateTimePicker { Location = new Point(85, 15), Size = new Size(130, 25), Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1) };

            var lblEnd = new Label { Text = "Bitiş:", Location = new Point(225, 18), Size = new Size(40, 20), Font = new Font("Segoe UI", 9) };
            dtpEnd = new DateTimePicker { Location = new Point(270, 15), Size = new Size(130, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Now };

            var lblType = new Label { Text = "Ödeme:", Location = new Point(415, 18), Size = new Size(55, 20), Font = new Font("Segoe UI", 9) };
            cmbPaymentType = new ComboBox { Location = new Point(475, 15), Size = new Size(110, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbPaymentType.Items.AddRange(new[] { "Tümü", "Cash", "Bank", "CurrentAccount" });
            cmbPaymentType.SelectedIndex = 0;

            var lblSearch = new Label { Text = "Ara:", Location = new Point(600, 18), Size = new Size(35, 20), Font = new Font("Segoe UI", 9) };
            txtSearch = new TextBox { Location = new Point(640, 15), Size = new Size(150, 25) };

            var btnFilter = new Button { Text = "Filtrele", Location = new Point(800, 13), Size = new Size(80, 28), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnFilter.FlatAppearance.BorderSize = 0;
            btnFilter.Click += (s, e) => LoadData();

            pnlFilter.Controls.AddRange(new Control[] { lblStart, dtpStart, lblEnd, dtpEnd, lblType, cmbPaymentType, lblSearch, txtSearch, btnFilter });

            // Araç çubuğu
            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(52, 73, 94), Padding = new Padding(10, 7, 10, 7) };

            btnNew = CreateToolbarButton("➕ Yeni", Color.FromArgb(39, 174, 96));
            btnNew.Click += BtnNew_Click;
            btnEdit = CreateToolbarButton("✏️ Düzenle", Color.FromArgb(41, 128, 185));
            btnEdit.Click += BtnEdit_Click;
            btnDelete = CreateToolbarButton("🗑️ Sil", Color.FromArgb(231, 76, 60));
            btnDelete.Click += BtnDelete_Click;
            btnRefresh = CreateToolbarButton("🔄 Yenile", Color.FromArgb(127, 140, 141));
            btnRefresh.Click += (s, e) => LoadData();
            btnExport = CreateToolbarButton("📥 CSV Dışa Aktar", Color.FromArgb(142, 68, 173));
            btnExport.Click += BtnExport_Click;

            int btnX = 10;
            foreach (var btn in new[] { btnNew, btnEdit, btnDelete, btnRefresh, btnExport })
            {
                btn.Location = new Point(btnX, 7);
                pnlToolbar.Controls.Add(btn);
                btnX += btn.Width + 5;
            }

            // Grid
            dgvTransactions = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9),
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(248, 249, 250) }
            };
            dgvTransactions.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94);
            dgvTransactions.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvTransactions.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvTransactions.EnableHeadersVisualStyles = false;

            // Toplam etiketi
            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 35, BackColor = Color.FromArgb(52, 73, 94) };
            lblTotal = new Label { Text = "Toplam: 0 kayıt", ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(10, 8), Size = new Size(600, 20) };
            pnlBottom.Controls.Add(lblTotal);

            this.Controls.Add(dgvTransactions);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlFilter);
            this.Controls.Add(pnlToolbar);
        }

        private Button CreateToolbarButton(string text, Color color)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(130, 30),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void LoadData()
        {
            try
            {
                if (!Program.ActiveCompanyId.HasValue) return;

                _transactions = _service.GetTransactionsByDateRange(
                    Program.ActiveCompanyId.Value, dtpStart.Value, dtpEnd.Value);

                // Filtrele
                string paymentFilter = cmbPaymentType.SelectedItem?.ToString();
                string searchText = txtSearch.Text.Trim().ToLowerInvariant();

                var filtered = _transactions;
                if (paymentFilter != "Tümü" && !string.IsNullOrEmpty(paymentFilter))
                    filtered = filtered.FindAll(t => t.PaymentType == paymentFilter);
                if (!string.IsNullOrEmpty(searchText))
                    filtered = filtered.FindAll(t => t.Description.ToLowerInvariant().Contains(searchText) ||
                        (t.DocumentNumber ?? "").ToLowerInvariant().Contains(searchText));

                dgvTransactions.Columns.Clear();
                dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransactionId", HeaderText = "ID", DataPropertyName = "TransactionId", Width = 50 });
                dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "TransactionDate", HeaderText = "Tarih", DataPropertyName = "TransactionDate", DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } });
                dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "DocumentNumber", HeaderText = "Belge No", DataPropertyName = "DocumentNumber" });
                dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Açıklama", DataPropertyName = "Description" });
                dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Tutar (₺)", DataPropertyName = "Amount", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
                dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "VatRate", HeaderText = "KDV %", DataPropertyName = "VatRate", DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } });
                dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "NetAmount", HeaderText = "Net Tutar (₺)", DataPropertyName = "NetAmount", DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
                dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "PaymentType", HeaderText = "Ödeme Tipi", DataPropertyName = "PaymentType" });

                dgvTransactions.DataSource = filtered;

                decimal totalAmount = 0;
                foreach (var t in filtered) totalAmount += t.Amount;
                lblTotal.Text = $"Toplam: {filtered.Count} kayıt | Toplam Tutar: {totalAmount:N2} ₺";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veri yüklenirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            var form = new IncomeExpenseEditForm(null);
            if (form.ShowDialog(this) == DialogResult.OK)
                LoadData();
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dgvTransactions.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen düzenlenecek kaydı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int id = Convert.ToInt32(dgvTransactions.SelectedRows[0].Cells["TransactionId"].Value);
            var tx = _transactions.Find(t => t.TransactionId == id);
            var form = new IncomeExpenseEditForm(tx);
            if (form.ShowDialog(this) == DialogResult.OK)
                LoadData();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (Program.CurrentUserRole != "Admin")
            {
                MessageBox.Show("Silme işlemi yalnızca yöneticiler tarafından yapılabilir.", "Yetki Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dgvTransactions.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen silinecek kaydı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Seçili kaydı silmek istediğinizden emin misiniz?", "Onay",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                int id = Convert.ToInt32(dgvTransactions.SelectedRows[0].Cells["TransactionId"].Value);
                if (_service.DeleteTransaction(id))
                {
                    var auditService = new AuditLogService(Program.ConnectionString);
                    auditService.LogDelete("IncomeExpenseTransactions", id, Program.CurrentUserId, $"TransactionId={id}");
                    LoadData();
                }
                else
                    MessageBox.Show("Silme işlemi başarısız.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog { Filter = "CSV Dosyası|*.csv", FileName = $"GelirGider_{DateTime.Now:yyyyMMdd}.csv" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var reportService = new ReportService(Program.ConnectionString, Program.EncryptionKey);
                    var data = reportService.TransactionsToCSVData(_transactions);
                    if (reportService.ExportToCSV(sfd.FileName, data))
                        MessageBox.Show("Dışa aktarma başarılı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    else
                        MessageBox.Show("Dışa aktarma başarısız.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
