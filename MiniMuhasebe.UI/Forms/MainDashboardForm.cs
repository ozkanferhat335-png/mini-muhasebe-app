using System;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;

namespace MiniMuhasebe.UI.Forms
{
    public class MainDashboardForm : Form
    {
        private Panel pnlSidebar;
        private Panel pnlContent;
        private Panel pnlHeader;
        private Panel pnlStatusBar;
        private Label lblCurrentSection;
        private Label lblStatusInfo;

        private readonly IncomeExpenseService _incomeExpenseService;
        private readonly BankService _bankService;
        private readonly CompanyService _companyService;
        private readonly FiscalPeriodService _periodService;
        private readonly AuditLogService _auditLogService;

        public MainDashboardForm()
        {
            _incomeExpenseService = new IncomeExpenseService(AppSession.ConnectionString);
            _bankService = new BankService(AppSession.ConnectionString, AppSession.EncryptionKey);
            _companyService = new CompanyService(AppSession.ConnectionString);
            _periodService = new FiscalPeriodService(AppSession.ConnectionString);
            _auditLogService = new AuditLogService(AppSession.ConnectionString);

            InitializeComponent();
            LoadDashboard();
        }

        private void InitializeComponent()
        {
            this.Text = "Mini Muhasebe - Ana Panel";
            this.Size = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1024, 600);
            this.BackColor = Color.FromArgb(245, 247, 250);

            // Header
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(41, 128, 185)
            };

            var lblAppName = new Label
            {
                Text = "💼 Mini Muhasebe",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(15, 15)
            };

            var lblCompanyInfo = new Label
            {
                Text = AppSession.CurrentCompany?.CompanyName ?? "Firma Seçilmedi",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(200, 230, 255),
                AutoSize = true,
                Location = new Point(220, 18)
            };

            var btnLogout = new Button
            {
                Text = $"👤 {AppSession.CurrentUser?.Username}  |  Çıkış",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(52, 152, 219),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(180, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += BtnLogout_Click;

            pnlHeader.Controls.AddRange(new Control[] { lblAppName, lblCompanyInfo, btnLogout });
            pnlHeader.Resize += (s, e) => btnLogout.Location = new Point(pnlHeader.Width - 195, 12);
            btnLogout.Location = new Point(this.Width - 210, 12);

            // Sidebar
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Color.FromArgb(44, 62, 80)
            };

            CreateSidebarMenu();

            // Content area
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(20)
            };

            // Status bar
            pnlStatusBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                BackColor = Color.FromArgb(52, 73, 94)
            };

            lblStatusInfo = new Label
            {
                Text = AppSession.GetDisplayInfo(),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(180, 200, 220),
                AutoSize = true,
                Location = new Point(10, 6)
            };

            var lblDateTime = new Label
            {
                Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(180, 200, 220),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            pnlStatusBar.Controls.AddRange(new Control[] { lblStatusInfo, lblDateTime });
            pnlStatusBar.Resize += (s, e) => lblDateTime.Location = new Point(pnlStatusBar.Width - 150, 6);

            // Timer for clock
            var timer = new Timer { Interval = 60000 };
            timer.Tick += (s, e) => lblDateTime.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            timer.Start();

            this.Controls.AddRange(new Control[] { pnlContent, pnlSidebar, pnlHeader, pnlStatusBar });
        }

        private void CreateSidebarMenu()
        {
            int y = 20;

            var lblMenu = new Label
            {
                Text = "MENÜ",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 130, 160),
                AutoSize = true,
                Location = new Point(15, y)
            };
            pnlSidebar.Controls.Add(lblMenu);
            y += 30;

            AddMenuButton("🏠  Ana Panel", y, () => LoadDashboard()); y += 45;
            AddMenuButton("📊  Gelir-Gider", y, () => OpenForm(new IncomeExpenseForm())); y += 45;
            AddMenuButton("👥  Cari Hesaplar", y, () => OpenForm(new CurrentAccountForm())); y += 45;
            AddMenuButton("🏦  Banka Hesapları", y, () => OpenForm(new BankAccountsForm())); y += 45;
            AddMenuButton("📋  Banka Hareketleri", y, () => OpenForm(new BankTransactionsForm())); y += 45;
            AddMenuButton("🔗  Eşleştirme", y, () => OpenForm(new MatchingForm())); y += 45;
            AddMenuButton("📈  Raporlar", y, () => OpenForm(new ReportsForm())); y += 45;

            y += 10;
            var sep = new Panel { Location = new Point(10, y), Size = new Size(200, 1), BackColor = Color.FromArgb(70, 90, 110) };
            pnlSidebar.Controls.Add(sep);
            y += 15;

            AddMenuButton("⚙️  Ayarlar", y, () => OpenForm(new SettingsForm())); y += 45;

            if (AppSession.IsAdmin)
            {
                AddMenuButton("🔒  Yedekleme", y, () => OpenBackupDialog()); y += 45;
            }
        }

        private void AddMenuButton(string text, int y, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(180, 210, 240),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(0, y),
                Size = new Size(220, 40),
                Cursor = Cursors.Hand,
                Padding = new Padding(15, 0, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 73, 94);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(41, 128, 185);
            btn.Click += (s, e) => onClick();
            pnlSidebar.Controls.Add(btn);
        }

        private void OpenForm(Form form)
        {
            pnlContent.Controls.Clear();
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(form);
            form.Show();
        }

        private void LoadDashboard()
        {
            pnlContent.Controls.Clear();

            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

            // Başlık
            var lblTitle = new Label
            {
                Text = "Ana Panel",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(0, 0)
            };
            panel.Controls.Add(lblTitle);

            // Özet kartları
            int cardY = 50;
            int cardX = 0;

            try
            {
                if (AppSession.CurrentPeriod != null)
                {
                    var transactions = _incomeExpenseService.GetTransactionsByPeriod(AppSession.CurrentPeriod.PeriodId);
                    decimal totalIncome = 0, totalExpense = 0;

                    foreach (var t in transactions)
                    {
                        // Hesap tipine göre gelir/gider ayrımı
                        totalIncome += t.Amount; // Basitleştirilmiş
                    }

                    AddSummaryCard(panel, "Dönem İşlemleri", transactions.Count.ToString(), Color.FromArgb(41, 128, 185), cardX, cardY);
                    cardX += 220;
                }

                var bankAccounts = _bankService.GetAccountsByCompany(AppSession.CurrentCompany?.CompanyId ?? 0);
                decimal totalBankBalance = 0;
                foreach (var ba in bankAccounts) totalBankBalance += ba.CurrentBalance;

                AddSummaryCard(panel, "Toplam Banka Bakiyesi", totalBankBalance.ToString("N2") + " ₺", Color.FromArgb(39, 174, 96), cardX, cardY);
                cardX += 220;
                AddSummaryCard(panel, "Banka Hesabı", bankAccounts.Count.ToString(), Color.FromArgb(142, 68, 173), cardX, cardY);
                cardX += 220;
            }
            catch { }

            // Firma ve dönem bilgisi
            var lblInfo = new Label
            {
                Text = $"Aktif Firma: {AppSession.CurrentCompany?.CompanyName ?? "Seçilmedi"}\n" +
                       $"Aktif Dönem: {AppSession.CurrentPeriod?.PeriodName ?? "Seçilmedi"}\n" +
                       $"Kullanıcı: {AppSession.CurrentUser?.Username} ({(AppSession.IsAdmin ? "Yönetici" : "Standart Kullanıcı")})",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(80, 100, 120),
                AutoSize = false,
                Size = new Size(500, 80),
                Location = new Point(0, cardY + 130)
            };
            panel.Controls.Add(lblInfo);

            // Hızlı erişim butonları
            var lblQuick = new Label
            {
                Text = "Hızlı İşlemler",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(0, cardY + 230)
            };
            panel.Controls.Add(lblQuick);

            AddQuickButton(panel, "➕ Yeni Gelir/Gider", Color.FromArgb(41, 128, 185), 0, cardY + 265, () => OpenForm(new IncomeExpenseForm()));
            AddQuickButton(panel, "👥 Cari Hesaplar", Color.FromArgb(39, 174, 96), 160, cardY + 265, () => OpenForm(new CurrentAccountForm()));
            AddQuickButton(panel, "📈 Raporlar", Color.FromArgb(230, 126, 34), 320, cardY + 265, () => OpenForm(new ReportsForm()));
            AddQuickButton(panel, "🔗 Eşleştirme", Color.FromArgb(142, 68, 173), 480, cardY + 265, () => OpenForm(new MatchingForm()));

            pnlContent.Controls.Add(panel);
        }

        private void AddSummaryCard(Panel parent, string title, string value, Color color, int x, int y)
        {
            var card = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(200, 110),
                BackColor = Color.White
            };

            var border = new Panel
            {
                Dock = DockStyle.Top,
                Height = 4,
                BackColor = color
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                AutoSize = false,
                Size = new Size(180, 30),
                Location = new Point(10, 15),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = color,
                AutoSize = false,
                Size = new Size(180, 50),
                Location = new Point(10, 45),
                TextAlign = ContentAlignment.MiddleLeft
            };

            card.Controls.AddRange(new Control[] { border, lblTitle, lblValue });
            parent.Controls.Add(card);
        }

        private void AddQuickButton(Panel parent, string text, Color color, int x, int y, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = color,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(x, y),
                Size = new Size(150, 45),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => onClick();
            parent.Controls.Add(btn);
        }

        private void OpenBackupDialog()
        {
            var backupService = new BackupService(AppSession.ConnectionString);
            var result = MessageBox.Show("Şimdi yedek almak istiyor musunuz?", "Yedekleme",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                var backup = backupService.CreateBackup();
                if (backup != null)
                    MessageBox.Show($"Yedek başarıyla oluşturuldu:\n{backup.BackupPath}", "Başarılı",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show("Yedekleme başarısız!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Çıkış yapmak istediğinizden emin misiniz?", "Çıkış",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _auditLogService.LogLogout(AppSession.CurrentUser.UserId, AppSession.CurrentUser.Username);
                AppSession.Clear();
                this.Close();
            }
        }
    }
}
