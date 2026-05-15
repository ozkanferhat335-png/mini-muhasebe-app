using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    public class BankTransactionsForm : Form
    {
        private DataGridView dgvTransactions;
        private Panel pnlToolbar;
        private Button btnRefresh, btnExport;
        private ComboBox cmbBankAccount;
        private DateTimePicker dtpStart, dtpEnd;
        private Label lblCount, lblTotal;
        private ComboBox cmbStatusFilter;

        private readonly User _currentUser;
        private readonly Company _activeCompany;
        private readonly BankService _bankService;
        private List<BankTransaction> _transactions;

        public BankTransactionsForm(User user, Company company)
        {
            _currentUser = user;
            _activeCompany = company;
            _bankService = new BankService(Program.ConnectionString, Program.EncryptionKey);

            InitializeComponent();
            LoadBankAccounts();
        }

        private void InitializeComponent()
        {
            this.Text = "Banka Hareketleri";
            this.Size = new Size(1100, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 9f);

            pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = Color.White, Padding = new Padding(8) };

            // Satır 1: Hesap seçimi ve tarih filtresi
            var lblAccount = new Label { Text = "Banka Hesabı:", AutoSize = true, Location = new Point(10, 12), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            cmbBankAccount = new ComboBox
            {
                Location = new Point(115, 8),
                Size = new Size(220, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "BankName"
            };
            cmbBankAccount.SelectedIndexChanged += (s, e) => LoadTransactions();

            var lblStart = new Label { Text = "Başlangıç:", AutoSize = true, Location = new Point(350, 12), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            dtpStart = new DateTimePicker
            {
                Location = new Point(425, 8),
                Size = new Size(130, 25),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now.AddMonths(-1)
            };

            var lblEnd = new Label { Text = "Bitiş:", AutoSize = true, Location = new Point(565, 12), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            dtpEnd = new DateTimePicker
            {
                Location = new Point(605, 8),
                Size = new Size(130, 25),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now
            };

            var btnFilter = new Button
            {
                Text = "🔍 Filtrele",
                Size = new Size(100, 28),
                Location = new Point(745, 7),
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
            btnFilter.Click += (s, e) => LoadTransactions();

            // Satır 2: Durum filtresi ve butonlar
            var lblStatus = new Label { Text = "Durum:", AutoSize = true, Location = new Point(10, 52), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            cmbStatusFilter = new ComboBox
            {
                Location = new Point(65, 48),
                Size = new Size(130, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStatusFilter.Items.AddRange(new object[] { "Tümü", "Bekleyen", "Eşleştirildi", "Eşleştirilmedi" });
            cmbStatusFilter.SelectedIndex = 0;
            cmbStatusFilter.SelectedIndexChanged += (s, e) => LoadTransactions();

            btnRefresh = new Button
            {
                Text = "🔄 Yenile",
                Size = new Size(100, 28),
                Location = new Point(210, 48),
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
            btnRefresh.Click += (s, e) => LoadTransactions();

            btnExport = new Button
            {
                Text = "📥 CSV Dışa Aktar",
                Size = new Size(140, 28),
                Location = new Point(320, 48),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
            btnExport.Click += BtnExport_Click;

            lblCount = new Label { Text = "0 kayıt", AutoSize = true, Location = new Point(480, 52), Font = new Font("Segoe UI", 9f), ForeColor = Color.Gray };
            lblTotal = new Label { Text = "Net: ₺0,00", AutoSize = true, Location = new Point(600, 52), Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(41, 128, 185) };

            pnlToolbar.Controls.AddRange(new Control[] {
                lblAccount, cmbBankAccount, lblStart, dtpStart, lblEnd, dtpEnd, btnFilter,
                lblStatus, cmbStatusFilter, btnRefresh, btnExport, lblCount, lblTotal
            });

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
                new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", FillWeight = 5 },
                new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "Tarih", FillWeight = 10 },
                new DataGridViewTextBoxColumn { Name = "colDesc", HeaderText = "Açıklama", FillWeight = 30 },
                new DataGridViewTextBoxColumn { Name = "colType", HeaderText = "Tür", FillWeight = 8 },
                new DataGridViewTextBoxColumn { Name = "colAmount", HeaderText = "Tutar", FillWeight = 12, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "colBalance", HeaderText = "Bakiye", FillWeight = 12, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "colRef", HeaderText = "Referans No", FillWeight = 12 },
                new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "Durum", FillWeight = 10 },
                new DataGridViewTextBoxColumn { Name = "colExtId", HeaderText = "Harici ID", FillWeight = 13 }
            });

            this.Controls.AddRange(new Control[] { dgvTransactions, pnlToolbar });
        }

        private void LoadBankAccounts()
        {
            cmbBankAccount.Items.Clear();
            if (_activeCompany == null) return;

            var accounts = _bankService.GetAccountsByCompany(_activeCompany.CompanyId);
            foreach (var acc in accounts)
                cmbBankAccount.Items.Add(acc);

            if (cmbBankAccount.Items.Count > 0)
                cmbBankAccount.SelectedIndex = 0;
        }

        private void LoadTransactions()
        {
            dgvTransactions.Rows.Clear();
            if (cmbBankAccount.SelectedItem is not BankAccount selectedAccount) return;

            try
            {
                _transactions = _bankService.GetTransactionsByDateRange(
                    selectedAccount.BankAccountId, dtpStart.Value, dtpEnd.Value);

                string statusFilter = cmbStatusFilter.SelectedIndex switch
                {
                    1 => "Pending",
                    2 => "Matched",
                    3 => "Unmatched",
                    _ => null
                };

                decimal totalCredit = 0, totalDebit = 0;
                int count = 0;

                foreach (var tx in _transactions)
                {
                    if (statusFilter != null && tx.Status != statusFilter) continue;

                    string typeDisplay = tx.TransactionType == "Credit" ? "✅ Alacak" : "🔴 Borç";
                    string statusDisplay = tx.Status switch
                    {
                        "Matched" => "✅ Eşleştirildi",
                        "Unmatched" => "❌ Eşleştirilmedi",
                        _ => "⏳ Bekliyor"
                    };

                    var row = dgvTransactions.Rows.Add(
                        tx.BankTransactionId,
                        tx.TransactionDate.ToString("dd.MM.yyyy"),
                        tx.Description,
                        typeDisplay,
                        $"₺{tx.Amount:N2}",
                        tx.Balance.HasValue ? $"₺{tx.Balance:N2}" : "-",
                        tx.ReferenceNumber,
                        statusDisplay,
                        tx.BankTransactionId_External
                    );

                    // Renk kodlaması
                    if (tx.TransactionType == "Credit")
                    {
                        dgvTransactions.Rows[row].DefaultCellStyle.ForeColor = Color.FromArgb(39, 174, 96);
                        totalCredit += tx.Amount;
                    }
                    else
                    {
                        dgvTransactions.Rows[row].DefaultCellStyle.ForeColor = Color.FromArgb(231, 76, 60);
                        totalDebit += Math.Abs(tx.Amount);
                    }

                    count++;
                }

                decimal net = totalCredit - totalDebit;
                lblCount.Text = $"{count} kayıt";
                lblTotal.Text = $"Gelir: ₺{totalCredit:N2} | Gider: ₺{totalDebit:N2} | Net: ₺{net:N2}";
                lblTotal.ForeColor = net >= 0 ? Color.FromArgb(39, 174, 96) : Color.FromArgb(231, 76, 60);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veriler yüklenirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (_transactions == null || _transactions.Count == 0)
            {
                MessageBox.Show("Dışa aktarılacak veri yok.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV Dosyası (*.csv)|*.csv";
                sfd.FileName = $"BankaHareketleri_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine("ID,Tarih,Açıklama,Tür,Tutar,Bakiye,Referans No,Durum,Harici ID");

                        foreach (var tx in _transactions)
                        {
                            sb.AppendLine($"{tx.BankTransactionId},{tx.TransactionDate:dd.MM.yyyy},\"{tx.Description}\",{tx.TransactionType},{tx.Amount:N2},{tx.Balance:N2},{tx.ReferenceNumber},{tx.Status},{tx.BankTransactionId_External}");
                        }

                        System.IO.File.WriteAllText(sfd.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                        MessageBox.Show($"Dosya kaydedildi:\n{sfd.FileName}", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Dışa aktarma sırasında hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
