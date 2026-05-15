using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    /// <summary>
    /// Ana dashboard formu
    /// </summary>
    public class MainDashboardForm : Form
    {
        private MenuStrip menuStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
        private ToolStripStatusLabel lblCompany;
        private ToolStripStatusLabel lblPeriod;
        private ToolStripStatusLabel lblUser;
        private Panel pnlDashboard;
        private ComboBox cmbCompany;
        private ComboBox cmbPeriod;

        private CompanyService _companyService;
        private FiscalPeriodService _periodService;
        private ReportService _reportService;

        public MainDashboardForm()
        {
            _companyService = new CompanyService(Program.ConnectionString);
            _periodService = new FiscalPeriodService(Program.ConnectionString);
            _reportService = new ReportService(Program.ConnectionString, Program.EncryptionKey);

            InitializeComponent();
            LoadCompanies();
        }

        private void InitializeComponent()
        {
            this.Text = "Mini Muhasebe - Ana Ekran";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.FormClosing += MainDashboardForm_FormClosing;

            // Menü
            menuStrip = new MenuStrip { BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White };

            var mnuFirma = CreateMenu("🏢 Firma", Color.White);
            mnuFirma.DropDownItems.Add("Firma Yönetimi", null, (s, e) => OpenForm(new CompanyForm()));
            mnuFirma.DropDownItems.Add("Mali Dönemler", null, (s, e) => OpenForm(new FiscalPeriodForm()));

            var mnuGelirGider = CreateMenu("📋 Gelir-Gider", Color.White);
            mnuGelirGider.DropDownItems.Add("Gelir-Gider Kayıtları", null, (s, e) => OpenForm(new IncomeExpenseForm()));
            mnuGelirGider.DropDownItems.Add("Hesap Kategorileri", null, (s, e) => OpenForm(new AccountForm()));

            var mnuCari = CreateMenu("👥 Cari Hesaplar", Color.White);
            mnuCari.DropDownItems.Add("Cari Kartlar", null, (s, e) => OpenForm(new CurrentAccountForm()));

            var mnuBanka = CreateMenu("🏦 Banka", Color.White);
            mnuBanka.DropDownItems.Add("Banka Hesapları", null, (s, e) => OpenForm(new BankAccountsForm()));
            mnuBanka.DropDownItems.Add("Banka Hareketleri", null, (s, e) => OpenForm(new BankTransactionsForm()));
            mnuBanka.DropDownItems.Add("Eşleştirme", null, (s, e) => OpenForm(new MatchingForm()));

            var mnuRapor = CreateMenu("📊 Raporlar", Color.White);
            mnuRapor.DropDownItems.Add("Gelir-Gider Özeti", null, (s, e) => OpenForm(new ReportsForm("IncomeExpense")));
            mnuRapor.DropDownItems.Add("Nakit Akış Raporu", null, (s, e) => OpenForm(new ReportsForm("CashFlow")));
            mnuRapor.DropDownItems.Add("Cari Ekstre", null, (s, e) => OpenForm(new ReportsForm("CurrentAccount")));
            mnuRapor.DropDownItems.Add("Banka Mutabakatı", null, (s, e) => OpenForm(new ReportsForm("BankReconciliation")));

            var mnuAyarlar = CreateMenu("⚙️ Ayarlar", Color.White);
            mnuAyarlar.DropDownItems.Add("Kullanıcı Yönetimi", null, (s, e) => OpenForm(new SettingsForm("Users")));
            mnuAyarlar.DropDownItems.Add("Yedekleme", null, (s, e) => OpenForm(new SettingsForm("Backup")));
            mnuAyarlar.DropDownItems.Add("Şifre Değiştir", null, (s, e) => OpenForm(new SettingsForm("Password")));
            mnuAyarlar.DropDownItems.Add(new ToolStripSeparator());
            mnuAyarlar.DropDownItems.Add("Çıkış", null, (s, e) => this.Close());

            menuStrip.Items.AddRange(new ToolStripItem[] { mnuFirma, mnuGelirGider, mnuCari, mnuBanka, mnuRapor, mnuAyarlar });

            // Firma/Dönem seçim paneli
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(52, 73, 94),
                Padding = new Padding(10, 8, 10, 8)
            };

            var lblFirma = new Label { Text = "Firma:", ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(10, 15), Size = new Size(50, 20) };
            cmbCompany = new ComboBox { Location = new Point(65, 12), Size = new Size(250, 25), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9) };
            cmbCompany.SelectedIndexChanged += CmbCompany_SelectedIndexChanged;

            var lblDonem = new Label { Text = "Dönem:", ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(330, 15), Size = new Size(55, 20) };
            cmbPeriod = new ComboBox { Location = new Point(390, 12), Size = new Size(220, 25), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9) };
            cmbPeriod.SelectedIndexChanged += CmbPeriod_SelectedIndexChanged;

            pnlTop.Controls.AddRange(new Control[] { lblFirma, cmbCompany, lblDonem, cmbPeriod });

            // Dashboard içerik paneli
            pnlDashboard = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                AutoScroll = true
            };

            BuildDashboardCards();

            // Status bar
            statusStrip = new StatusStrip { BackColor = Color.FromArgb(41, 128, 185) };
            lblUser = new ToolStripStatusLabel { Text = $"👤 {Program.CurrentUsername} ({Program.CurrentUserRole})", ForeColor = Color.White };
            lblCompany = new ToolStripStatusLabel { Text = "🏢 Firma seçilmedi", ForeColor = Color.White };
            lblPeriod = new ToolStripStatusLabel { Text = "📅 Dönem seçilmedi", ForeColor = Color.White };
            lblStatus = new ToolStripStatusLabel { Text = "Hazır", ForeColor = Color.LightGreen, Spring = true, TextAlign = ContentAlignment.MiddleRight };
            statusStrip.Items.AddRange(new ToolStripItem[] { lblUser, new ToolStripSeparator(), lblCompany, new ToolStripSeparator(), lblPeriod, lblStatus });

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(pnlDashboard);
            this.Controls.Add(pnlTop);
            this.Controls.Add(menuStrip);
            this.Controls.Add(statusStrip);
        }

        private ToolStripMenuItem CreateMenu(string text, Color foreColor)
        {
            return new ToolStripMenuItem(text) { ForeColor = foreColor, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        }

        private void BuildDashboardCards()
        {
            pnlDashboard.Controls.Clear();

            var lblWelcome = new Label
            {
                Text = $"Hoş Geldiniz, {Program.CurrentUsername}!",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(41, 128, 185),
                Location = new Point(20, 20),
                Size = new Size(500, 35)
            };

            var lblDate = new Label
            {
                Text = $"📅 {DateTime.Now:dddd, dd MMMM yyyy}",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.Gray,
                Location = new Point(20, 58),
                Size = new Size(400, 25)
            };

            pnlDashboard.Controls.Add(lblWelcome);
            pnlDashboard.Controls.Add(lblDate);

            if (Program.ActiveCompanyId.HasValue && Program.ActivePeriodId.HasValue)
            {
                // İstatistik kartları
                var summary = _reportService.GetIncomeExpenseSummary(
                    Program.ActiveCompanyId.Value, Program.ActivePeriodId.Value);

                int cardY = 100;
                AddStatCard("💰 Toplam Gelir", summary.TotalIncome.ToString("N2") + " ₺", Color.FromArgb(39, 174, 96), 20, cardY);
                AddStatCard("💸 Toplam Gider", summary.TotalExpense.ToString("N2") + " ₺", Color.FromArgb(231, 76, 60), 220, cardY);
                AddStatCard("📈 Net Sonuç", summary.NetResult.ToString("N2") + " ₺",
                    summary.NetResult >= 0 ? Color.FromArgb(39, 174, 96) : Color.FromArgb(231, 76, 60), 420, cardY);
                AddStatCard("📋 İşlem Sayısı", summary.TransactionCount.ToString(), Color.FromArgb(41, 128, 185), 620, cardY);
            }
            else
            {
                var lblInfo = new Label
                {
                    Text = "ℹ️ İstatistikleri görmek için lütfen firma ve dönem seçin.",
                    Font = new Font("Segoe UI", 10),
                    ForeColor = Color.FromArgb(41, 128, 185),
                    Location = new Point(20, 110),
                    Size = new Size(700, 30)
                };
                pnlDashboard.Controls.Add(lblInfo);
            }

            // Hızlı erişim butonları
            int btnY = Program.ActiveCompanyId.HasValue ? 240 : 160;
            var lblQuick = new Label
            {
                Text = "Hızlı Erişim",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                Location = new Point(20, btnY),
                Size = new Size(200, 30)
            };
            pnlDashboard.Controls.Add(lblQuick);

            btnY += 40;
            AddQuickButton("➕ Yeni Gelir/Gider", Color.FromArgb(39, 174, 96), 20, btnY, () => OpenForm(new IncomeExpenseForm()));
            AddQuickButton("👥 Cari Hesaplar", Color.FromArgb(41, 128, 185), 200, btnY, () => OpenForm(new CurrentAccountForm()));
            AddQuickButton("🏦 Banka Hareketleri", Color.FromArgb(142, 68, 173), 380, btnY, () => OpenForm(new BankTransactionsForm()));
            AddQuickButton("📊 Raporlar", Color.FromArgb(230, 126, 34), 560, btnY, () => OpenForm(new ReportsForm("IncomeExpense")));
        }

        private void AddStatCard(string title, string value, Color color, int x, int y)
        {
            var card = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(180, 100),
                BackColor = color,
                BorderStyle = BorderStyle.None
            };

            var lblCardTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 255, 255, 255),
                Location = new Point(10, 10),
                Size = new Size(160, 20)
            };

            var lblCardValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(10, 40),
                Size = new Size(160, 45),
                TextAlign = ContentAlignment.MiddleLeft
            };

            card.Controls.AddRange(new Control[] { lblCardTitle, lblCardValue });
            pnlDashboard.Controls.Add(card);
        }

        private void AddQuickButton(string text, Color color, int x, int y, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(165, 50),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => onClick();
            pnlDashboard.Controls.Add(btn);
        }

        private void LoadCompanies()
        {
            var companies = _companyService.GetAllCompanies();
            cmbCompany.DataSource = companies;
            cmbCompany.DisplayMember = "CompanyName";
            cmbCompany.ValueMember = "CompanyId";

            if (companies.Count > 0)
                cmbCompany.SelectedIndex = 0;
        }

        private void CmbCompany_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbCompany.SelectedItem is Company company)
            {
                Program.ActiveCompanyId = company.CompanyId;
                Program.ActiveCompanyName = company.CompanyName;
                lblCompany.Text = $"🏢 {company.CompanyName}";

                // Dönemleri yükle
                var periods = _periodService.GetOpenPeriodsByCompany(company.CompanyId);
                cmbPeriod.DataSource = periods;
                cmbPeriod.DisplayMember = "PeriodName";
                cmbPeriod.ValueMember = "PeriodId";

                if (periods.Count > 0)
                    cmbPeriod.SelectedIndex = 0;
            }
        }

        private void CmbPeriod_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbPeriod.SelectedItem is FiscalPeriod period)
            {
                Program.ActivePeriodId = period.PeriodId;
                Program.ActivePeriodName = period.PeriodName;
                lblPeriod.Text = $"📅 {period.PeriodName}";
                BuildDashboardCards();
            }
        }

        private void OpenForm(Form form)
        {
            if (Program.ActiveCompanyId == null)
            {
                MessageBox.Show("Lütfen önce bir firma seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            form.MdiParent = null;
            form.StartPosition = FormStartPosition.CenterParent;
            form.ShowDialog(this);
            BuildDashboardCards(); // Veri değişmiş olabilir, yenile
        }

        private void MainDashboardForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Program.CurrentUserId.HasValue)
            {
                try
                {
                    var auditService = new AuditLogService(Program.ConnectionString);
                    auditService.LogLogout(Program.CurrentUserId.Value, Program.CurrentUsername);

                    // Otomatik yedek al
                    var backupService = new BackupService(Program.ConnectionString);
                    backupService.CreateBackup();
                }
                catch { /* Kapatma sırasında hata gösterme */ }
            }
        }
    }
}
