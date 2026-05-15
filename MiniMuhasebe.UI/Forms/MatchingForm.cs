using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    public class MatchingForm : Form
    {
        private SplitContainer splitMain;
        private DataGridView dgvBankTx;
        private DataGridView dgvIETx;
        private DataGridView dgvMatches;
        private Panel pnlToolbar;
        private Button btnAutoMatch, btnManualMatch, btnRemoveMatch, btnRefresh;
        private ComboBox cmbBankAccount;
        private Label lblBankTxTitle, lblIETxTitle, lblMatchesTitle;
        private Label lblStatus;

        private readonly User _currentUser;
        private readonly Company _activeCompany;
        private readonly MatchingService _matchingService;
        private readonly BankService _bankService;

        private List<BankTransaction> _bankTransactions;
        private List<IncomeExpenseTransaction> _ieTxs;
        private List<TransactionMatch> _matches;

        public MatchingForm(User user, Company company)
        {
            _currentUser = user;
            _activeCompany = company;
            _matchingService = new MatchingService(Program.ConnectionString);
            _bankService = new BankService(Program.ConnectionString, Program.EncryptionKey);

            InitializeComponent();
            LoadBankAccounts();
        }

        private void InitializeComponent()
        {
            this.Text = "İşlem Eşleştirme";
            this.Size = new Size(1200, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 9f);

            // Araç çubuğu
            pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.White, Padding = new Padding(8) };

            var lblAccount = new Label { Text = "Banka Hesabı:", AutoSize = true, Location = new Point(10, 18), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            cmbBankAccount = new ComboBox
            {
                Location = new Point(115, 14),
                Size = new Size(220, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "BankName"
            };
            cmbBankAccount.SelectedIndexChanged += (s, e) => LoadData();

            btnAutoMatch = new Button
            {
                Text = "🤖 Otomatik Eşleştir",
                Size = new Size(155, 35),
                Location = new Point(350, 8),
                BackColor = Color.FromArgb(142, 68, 173),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
            btnAutoMatch.Click += BtnAutoMatch_Click;

            btnManualMatch = new Button
            {
                Text = "🔗 Manuel Eşleştir",
                Size = new Size(145, 35),
                Location = new Point(515, 8),
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
            btnManualMatch.Click += BtnManualMatch_Click;

            btnRemoveMatch = new Button
            {
                Text = "❌ Eşleştirmeyi Kaldır",
                Size = new Size(165, 35),
                Location = new Point(670, 8),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
            btnRemoveMatch.Click += BtnRemoveMatch_Click;

            btnRefresh = new Button
            {
                Text = "🔄 Yenile",
                Size = new Size(100, 35),
                Location = new Point(845, 8),
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
            btnRefresh.Click += (s, e) => LoadData();

            lblStatus = new Label
            {
                Text = string.Empty,
                AutoSize = true,
                Location = new Point(960, 18),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(39, 174, 96)
            };

            pnlToolbar.Controls.AddRange(new Control[] { lblAccount, cmbBankAccount, btnAutoMatch, btnManualMatch, btnRemoveMatch, btnRefresh, lblStatus });

            // Ana split container (üst: iki liste, alt: eşleştirmeler)
            splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 350,
                BackColor = Color.FromArgb(245, 247, 250)
            };

            // Üst panel: Banka hareketleri ve Gelir/Gider işlemleri
            var splitTop = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 550,
                BackColor = Color.FromArgb(245, 247, 250)
            };

            // Sol: Banka hareketleri
            var pnlLeft = new Panel { Dock = DockStyle.Fill };
            lblBankTxTitle = new Label
            {
                Text = "🏦 Eşleştirilmemiş Banka Hareketleri",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(41, 128, 185),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };

            dgvBankTx = CreateDgv();
            dgvBankTx.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", FillWeight = 6 },
                new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "Tarih", FillWeight = 14 },
                new DataGridViewTextBoxColumn { Name = "colDesc", HeaderText = "Açıklama", FillWeight = 40 },
                new DataGridViewTextBoxColumn { Name = "colType", HeaderText = "Tür", FillWeight = 10 },
                new DataGridViewTextBoxColumn { Name = "colAmount", HeaderText = "Tutar", FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "colRef", HeaderText = "Ref No", FillWeight = 15 }
            });

            pnlLeft.Controls.AddRange(new Control[] { dgvBankTx, lblBankTxTitle });

            // Sağ: Gelir/Gider işlemleri
            var pnlRight = new Panel { Dock = DockStyle.Fill };
            lblIETxTitle = new Label
            {
                Text = "💰 Eşleştirilmemiş Gelir/Gider İşlemleri",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(39, 174, 96),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };

            dgvIETx = CreateDgv();
            dgvIETx.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", FillWeight = 6 },
                new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "Tarih", FillWeight = 14 },
                new DataGridViewTextBoxColumn { Name = "colDesc", HeaderText = "Açıklama", FillWeight = 40 },
                new DataGridViewTextBoxColumn { Name = "colDocNo", HeaderText = "Belge No", FillWeight = 15 },
                new DataGridViewTextBoxColumn { Name = "colAmount", HeaderText = "Tutar", FillWeight = 15, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "colPayment", HeaderText = "Ödeme", FillWeight = 10 }
            });

            pnlRight.Controls.AddRange(new Control[] { dgvIETx, lblIETxTitle });

            splitTop.Panel1.Controls.Add(pnlLeft);
            splitTop.Panel2.Controls.Add(pnlRight);
            splitMain.Panel1.Controls.Add(splitTop);

            // Alt: Eşleştirmeler
            var pnlBottom = new Panel { Dock = DockStyle.Fill };
            lblMatchesTitle = new Label
            {
                Text = "🔗 Mevcut Eşleştirmeler",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(142, 68, 173),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };

            dgvMatches = CreateDgv();
            dgvMatches.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colMatchId", HeaderText = "Eşleştirme ID", FillWeight = 8 },
                new DataGridViewTextBoxColumn { Name = "colBankTxId", HeaderText = "Banka Tx ID", FillWeight = 8 },
                new DataGridViewTextBoxColumn { Name = "colBankDesc", HeaderText = "Banka Açıklaması", FillWeight = 25 },
                new DataGridViewTextBoxColumn { Name = "colBankAmount", HeaderText = "Banka Tutarı", FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "colIETxId", HeaderText = "Muhasebe Tx ID", FillWeight = 8 },
                new DataGridViewTextBoxColumn { Name = "colIEDesc", HeaderText = "Muhasebe Açıklaması", FillWeight = 25 },
                new DataGridViewTextBoxColumn { Name = "colIEAmount", HeaderText = "Muhasebe Tutarı", FillWeight = 10, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } },
                new DataGridViewTextBoxColumn { Name = "colScore", HeaderText = "Skor", FillWeight = 6 },
                new DataGridViewTextBoxColumn { Name = "colType", HeaderText = "Tür", FillWeight = 8 }
            });

            pnlBottom.Controls.AddRange(new Control[] { dgvMatches, lblMatchesTitle });
            splitMain.Panel2.Controls.Add(pnlBottom);

            this.Controls.AddRange(new Control[] { splitMain, pnlToolbar });
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
                Font = new Font("Segoe UI", 8.5f),
                GridColor = Color.FromArgb(230, 230, 230),
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(248, 249, 250) }
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            return dgv;
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

        private void LoadData()
        {
            LoadBankTransactions();
            LoadIETransactions();
            LoadMatches();
        }

        private void LoadBankTransactions()
        {
            dgvBankTx.Rows.Clear();
            if (cmbBankAccount.SelectedItem is not BankAccount acc) return;

            _bankTransactions = _matchingService.GetPendingBankTransactions(acc.BankAccountId);

            foreach (var tx in _bankTransactions)
            {
                dgvBankTx.Rows.Add(
                    tx.BankTransactionId,
                    tx.TransactionDate.ToString("dd.MM.yyyy"),
                    tx.Description,
                    tx.TransactionType == "Credit" ? "✅ Alacak" : "🔴 Borç",
                    $"₺{tx.Amount:N2}",
                    tx.ReferenceNumber
                );
            }

            lblBankTxTitle.Text = $"🏦 Eşleştirilmemiş Banka Hareketleri ({_bankTransactions.Count})";
        }

        private void LoadIETransactions()
        {
            dgvIETx.Rows.Clear();
            if (_activeCompany == null) return;

            _ieTxs = _matchingService.GetUnmatchedIncomeExpenseTransactions(_activeCompany.CompanyId);

            foreach (var tx in _ieTxs)
            {
                dgvIETx.Rows.Add(
                    tx.TransactionId,
                    tx.TransactionDate.ToString("dd.MM.yyyy"),
                    tx.Description,
                    tx.DocumentNumber,
                    $"₺{tx.Amount:N2}",
                    tx.PaymentType
                );
            }

            lblIETxTitle.Text = $"💰 Eşleştirilmemiş Gelir/Gider İşlemleri ({_ieTxs.Count})";
        }

        private void LoadMatches()
        {
            dgvMatches.Rows.Clear();
            _matches = _matchingService.GetAllMatches();

            foreach (var match in _matches)
            {
                dgvMatches.Rows.Add(
                    match.MatchId,
                    match.BankTransactionId,
                    $"Banka Tx #{match.BankTransactionId}",
                    "-",
                    match.IncomeExpenseTransactionId,
                    $"Muhasebe Tx #{match.IncomeExpenseTransactionId}",
                    "-",
                    $"{match.MatchScore:N0}%",
                    match.MatchType == "Automatic" ? "🤖 Otomatik" : "👤 Manuel"
                );
            }

            lblMatchesTitle.Text = $"🔗 Mevcut Eşleştirmeler ({_matches.Count})";
        }

        private void BtnAutoMatch_Click(object sender, EventArgs e)
        {
            if (cmbBankAccount.SelectedItem is not BankAccount acc)
            {
                MessageBox.Show("Lütfen bir banka hesabı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_activeCompany == null) return;

            try
            {
                Cursor = Cursors.WaitCursor;
                lblStatus.Text = "Otomatik eşleştirme çalışıyor...";
                lblStatus.ForeColor = Color.FromArgb(243, 156, 18);
                Application.DoEvents();

                var matches = _matchingService.RunAutoMatching(_activeCompany.CompanyId, acc.BankAccountId);

                LoadData();
                lblStatus.Text = $"✅ {matches.Count} eşleştirme oluşturuldu";
                lblStatus.ForeColor = Color.FromArgb(39, 174, 96);

                MessageBox.Show($"Otomatik eşleştirme tamamlandı.\n{matches.Count} yeni eşleştirme oluşturuldu.",
                    "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "❌ Hata oluştu";
                lblStatus.ForeColor = Color.FromArgb(231, 76, 60);
                MessageBox.Show($"Otomatik eşleştirme sırasında hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void BtnManualMatch_Click(object sender, EventArgs e)
        {
            if (dgvBankTx.SelectedRows.Count == 0 || dgvIETx.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen eşleştirilecek banka hareketi ve muhasebe kaydını seçin.",
                    "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int bankTxId = Convert.ToInt32(dgvBankTx.SelectedRows[0].Cells["colId"].Value);
            int ieTxId = Convert.ToInt32(dgvIETx.SelectedRows[0].Cells["colId"].Value);

            var match = _matchingService.CreateManualMatch(bankTxId, ieTxId, _currentUser.UserId);

            if (match != null)
            {
                LoadData();
                lblStatus.Text = $"✅ Manuel eşleştirme oluşturuldu (Skor: {match.MatchScore:N0}%)";
                lblStatus.ForeColor = Color.FromArgb(39, 174, 96);
            }
            else
            {
                MessageBox.Show("Eşleştirme oluşturulamadı. Bu işlemler zaten eşleştirilmiş olabilir.",
                    "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnRemoveMatch_Click(object sender, EventArgs e)
        {
            if (dgvMatches.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen kaldırılacak eşleştirmeyi seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int matchId = Convert.ToInt32(dgvMatches.SelectedRows[0].Cells["colMatchId"].Value);

            if (MessageBox.Show("Bu eşleştirmeyi kaldırmak istediğinizden emin misiniz?",
                "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (_matchingService.RemoveMatch(matchId))
                {
                    LoadData();
                    lblStatus.Text = "✅ Eşleştirme kaldırıldı";
                    lblStatus.ForeColor = Color.FromArgb(39, 174, 96);
                }
                else
                {
                    MessageBox.Show("Eşleştirme kaldırılamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
