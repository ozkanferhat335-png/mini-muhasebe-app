using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Data;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    public class MainDashboardForm : Form
    {
        private MenuStrip menuStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatusUser;
        private ToolStripStatusLabel lblStatusCompany;
        private ToolStripStatusLabel lblStatusTime;
        private Panel pnlContent;
        private Panel pnlSidebar;
        private Label lblWelcome;
        private Timer timerClock;

        // Dashboard kartları
        private Panel pnlCardIncome;
        private Panel pnlCardExpense;
        private Panel pnlCardBalance;
        private Panel pnlCardPending;

        private readonly User _currentUser;
        private Company _activeCompany;
        private FiscalPeriod _activePeriod;

        private readonly CompanyService _companyService;
        private readonly IncomeExpenseService _incomeExpenseService;
        private readonly BankService _bankService;
        private readonly Logger _logger;

        public MainDashboardForm(User user)
        {
            _currentUser = user;
            _companyService = new CompanyService(Program.ConnectionString);
            _incomeExpenseService = new IncomeExpenseService(Program.ConnectionString);
            _bankService = new BankService(Program.ConnectionString, Program.EncryptionKey);
            _logger = new Logger();

            InitializeComponent();
            LoadActiveCompany();
            LoadDashboardData();

            _logger.Info($"Dashboard açıldı: {user.Username}");
        }

        private void InitializeComponent()
        {
            this.Text = "Mini Muhasebe - Ana Panel";
            this.Size = new Size(1200, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 9f);
            this.WindowState = FormWindowState.Maximized;

            // Menü çubuğu
            menuStrip = new MenuStrip { BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White };

            var mnuFirma = CreateMenu("🏢 Firma", Color.White);
            mnuFirma.DropDownItems.Add("Firma Yönetimi", null, (s, e) => OpenForm(new SettingsForm(_currentUser, "company")));
            mnuFirma.DropDownItems.Add("Mali Dönem Seç", null, (s, e) => SelectPeriod());
            mnuFirma.DropDownItems.Add(new ToolStripSeparator());
            mnuFirma.DropDownItems.Add("Çıkış Yap", null, (s, e) => Logout());

            var mnuGelirGider = CreateMenu("💰 Gelir/Gider", Color.White);
            mnuGelirGider.DropDownItems.Add("Gelir/Gider Girişi", null, (s, e) => OpenForm(new IncomeExpenseForm(_currentUser, _activeCompany, _activePeriod)));

            var mnuCari = CreateMenu("👥 Cari", Color.White);
            mnuCari.DropDownItems.Add("Cari Hesaplar", null, (s, e) => OpenForm(new CurrentAccountForm(_currentUser, _activeCompany)));

            var mnuBanka = CreateMenu("🏦 Banka", Color.White);
            mnuBanka.DropDownItems.Add("Banka Hesapları", null, (s, e) => OpenForm(new BankAccountsForm(_currentUser, _activeCompany)));
            mnuBanka.DropDownItems.Add("Banka Hareketleri", null, (s, e) => OpenForm(new BankTransactionsForm(_currentUser, _activeCompany)));
            mnuBanka.DropDownItems.Add("Eşleştirme", null, (s, e) => OpenForm(new MatchingForm(_currentUser, _activeCompany)));

            var mnuRapor = CreateMenu("📊 Raporlar", Color.White);
            mnuRapor.DropDownItems.Add("Raporlar", null, (s, e) => OpenForm(new ReportsForm(_currentUser, _activeCompany, _activePeriod)));

            var mnuAyarlar = CreateMenu("⚙️ Ayarlar", Color.White);
            mnuAyarlar.DropDownItems.Add("Uygulama Ayarları", null, (s, e) => OpenForm(new SettingsForm(_currentUser, "general")));
            mnuAyarlar.DropDownItems.Add("Yedekleme", null, (s, e) => OpenForm(new SettingsForm(_currentUser, "backup")));

            menuStrip.Items.AddRange(new ToolStripItem[] { mnuFirma, mnuGelirGider, mnuCari, mnuBanka, mnuRapor, mnuAyarlar });
            this.MainMenuStrip = menuStrip;

            // Durum çubuğu
            statusStrip = new StatusStrip { BackColor = Color.FromArgb(41, 128, 185) };
            lblStatusUser = new ToolStripStatusLabel { ForeColor = Color.White };
            lblStatusCompany = new ToolStripStatusLabel { ForeColor = Color.White, Spring = true };
            lblStatusTime = new ToolStripStatusLabel { ForeColor = Color.White };
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatusUser, lblStatusCompany, lblStatusTime });

            // Saat güncelleme
            timerClock = new Timer { Interval = 1000 };
            timerClock.Tick += (s, e) => lblStatusTime.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            timerClock.Start();

            // İçerik paneli
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(245, 247, 250)
            };

            // Hoşgeldiniz etiketi
            lblWelcome = new Label
            {
                Text = $"Hoş Geldiniz, {_currentUser.Username}!",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(41, 128, 185),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            // Dashboard kartları
            pnlCardIncome = CreateDashboardCard("Toplam Gelir", "₺0,00", Color.FromArgb(39, 174, 96), 20, 70);
            pnlCardExpense = CreateDashboardCard("Toplam Gider", "₺0,00", Color.FromArgb(231, 76, 60), 220, 70);
            pnlCardBalance = CreateDashboardCard("Net Bakiye", "₺0,00", Color.FromArgb(41, 128, 185), 420, 70);
            pnlCardPending = CreateDashboardCard("Bekleyen İşlem", "0", Color.FromArgb(243, 156, 18), 620, 70);

            // Hızlı erişim butonları
            var pnlQuickAccess = new Panel
            {
                Location = new Point(20, 220),
                Size = new Size(900, 200),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            pnlQuickAccess.Paint += (s, e) =>
                e.Graphics.DrawRectangle(new Pen(Color.FromArgb(220, 220, 220)), 0, 0, pnlQuickAccess.Width - 1, pnlQuickAccess.Height - 1);

            var lblQuickTitle = new Label
            {
                Text = "Hızlı Erişim",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = true,
                Location = new Point(15, 15)
            };

            var btnQuickIncome = CreateQuickButton("💰 Gelir/Gider Girişi", 15, 50, Color.FromArgb(39, 174, 96));
            btnQuickIncome.Click += (s, e) => OpenForm(new IncomeExpenseForm(_currentUser, _activeCompany, _activePeriod));

            var btnQuickCari = CreateQuickButton("👥 Cari Hesaplar", 175, 50, Color.FromArgb(41, 128, 185));
            btnQuickCari.Click += (s, e) => OpenForm(new CurrentAccountForm(_currentUser, _activeCompany));

            var btnQuickBank = CreateQuickButton("🏦 Banka Hareketleri", 335, 50, Color.FromArgb(142, 68, 173));
            btnQuickBank.Click += (s, e) => OpenForm(new BankTransactionsForm(_currentUser, _activeCompany));

            var btnQuickMatch = CreateQuickButton("🔗 Eşleştirme", 495, 50, Color.FromArgb(243, 156, 18));
            btnQuickMatch.Click += (s, e) => OpenForm(new MatchingForm(_currentUser, _activeCompany));

            var btnQuickReport = CreateQuickButton("📊 Raporlar", 655, 50, Color.FromArgb(231, 76, 60));
            btnQuickReport.Click += (s, e) => OpenForm(new ReportsForm(_currentUser, _activeCompany, _activePeriod));

            var btnQuickBackup = CreateQuickButton("💾 Yedekleme", 815, 50, Color.FromArgb(52, 73, 94));
            btnQuickBackup.Click += (s, e) => OpenForm(new SettingsForm(_currentUser, "backup"));

            pnlQuickAccess.Controls.AddRange(new Control[] {
                lblQuickTitle, btnQuickIncome, btnQuickCari, btnQuickBank,
                btnQuickMatch, btnQuickReport, btnQuickBackup
            });

            pnlContent.Controls.AddRange(new Control[] {
                lblWelcome, pnlCardIncome, pnlCardExpense, pnlCardBalance, pnlCardPending, pnlQuickAccess
            });

            this.Controls.AddRange(new Control[] { menuStrip, pnlContent, statusStrip });
        }

        private ToolStripMenuItem CreateMenu(string text, Color foreColor)
        {
            return new ToolStripMenuItem(text)
            {
                ForeColor = foreColor,
                Font = new Font("Segoe UI", 9.5f)
            };
        }

        private Panel CreateDashboardCard(string title, string value, Color color, int x, int y)
        {
            var panel = new Panel
            {
                Size = new Size(180, 100),
                Location = new Point(x, y),
                BackColor = color,
                Cursor = Cursors.Default
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(220, 255, 255, 255),
                AutoSize = true,
                Location = new Point(10, 10)
            };

            var lblValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(10, 35),
                Tag = "value"
            };

            panel.Controls.AddRange(new Control[] { lblTitle, lblValue });
            return panel;
        }

        private Button CreateQuickButton(string text, int x, int y, Color color)
        {
            return new Button
            {
                Text = text,
                Size = new Size(145, 60),
                Location = new Point(x, y),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
        }

        private void LoadActiveCompany()
        {
            try
            {
                var companies = _companyService.GetAllCompanies();
                if (companies.Count > 0)
                {
                    _activeCompany = companies[0];

                    // Aktif dönemi yükle
                    var periodRepo = new FiscalPeriodRepository(Program.ConnectionString);
                    _activePeriod = periodRepo.GetCurrentPeriod(_activeCompany.CompanyId);
                    if (_activePeriod == null)
                    {
                        var periods = periodRepo.GetByCompanyId(_activeCompany.CompanyId);
                        if (periods.Count > 0) _activePeriod = periods[0];
                    }
                }

                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                _logger.Error("Aktif firma yüklenirken hata", ex);
            }
        }

        private void LoadDashboardData()
        {
            try
            {
                if (_activeCompany == null || _activePeriod == null) return;

                var transactions = _incomeExpenseService.GetTransactionsByPeriod(_activePeriod.PeriodId);
                decimal totalIncome = 0, totalExpense = 0;

                // Hesap tipine göre gelir/gider ayrımı için AccountRepository kullan
                var accountRepo = new AccountRepository(Program.ConnectionString);
                var accounts = accountRepo.GetByCompanyId(_activeCompany.CompanyId);

                foreach (var tx in transactions)
                {
                    var account = accounts.Find(a => a.AccountId == tx.AccountId);
                    if (account != null)
                    {
                        if (account.AccountType == "Income")
                            totalIncome += tx.Amount;
                        else if (account.AccountType == "Expense")
                            totalExpense += tx.Amount;
                    }
                }

                decimal netBalance = totalIncome - totalExpense;

                // Bekleyen banka işlemleri
                var bankAccounts = _bankService.GetAccountsByCompany(_activeCompany.CompanyId);
                int pendingCount = 0;
                foreach (var ba in bankAccounts)
                    pendingCount += _bankService.GetUnmatchedTransactions(ba.BankAccountId).Count;

                // Kartları güncelle
                UpdateCardValue(pnlCardIncome, $"₺{totalIncome:N2}");
                UpdateCardValue(pnlCardExpense, $"₺{totalExpense:N2}");
                UpdateCardValue(pnlCardBalance, $"₺{netBalance:N2}");
                UpdateCardValue(pnlCardPending, pendingCount.ToString());
            }
            catch (Exception ex)
            {
                _logger.Error("Dashboard verisi yüklenirken hata", ex);
            }
        }

        private void UpdateCardValue(Panel card, string value)
        {
            foreach (Control ctrl in card.Controls)
            {
                if (ctrl is Label lbl && lbl.Tag?.ToString() == "value")
                {
                    lbl.Text = value;
                    break;
                }
            }
        }

        private void UpdateStatusBar()
        {
            lblStatusUser.Text = $"👤 {_currentUser.Username} ({(_currentUser.RoleId == 1 ? "Yönetici" : "Kullanıcı")})";
            lblStatusCompany.Text = _activeCompany != null
                ? $"🏢 {_activeCompany.CompanyName} | 📅 {_activePeriod?.PeriodName ?? "Dönem seçilmedi"}"
                : "Firma seçilmedi";
            lblStatusTime.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
        }

        private void SelectPeriod()
        {
            if (_activeCompany == null)
            {
                MessageBox.Show("Önce bir firma seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var periodRepo = new FiscalPeriodRepository(Program.ConnectionString);
            var periods = periodRepo.GetByCompanyId(_activeCompany.CompanyId);

            if (periods.Count == 0)
            {
                MessageBox.Show("Bu firmaya ait mali dönem bulunamadı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new Form())
            {
                dlg.Text = "Mali Dönem Seç";
                dlg.Size = new Size(350, 250);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;

                var lst = new ListBox { Dock = DockStyle.Fill };
                foreach (var p in periods)
                    lst.Items.Add(p);
                lst.DisplayMember = "PeriodName";

                var btnOk = new Button
                {
                    Text = "Seç",
                    Dock = DockStyle.Bottom,
                    Height = 35,
                    BackColor = Color.FromArgb(41, 128, 185),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btnOk.Click += (s, e) =>
                {
                    if (lst.SelectedItem is FiscalPeriod selected)
                    {
                        _activePeriod = selected;
                        UpdateStatusBar();
                        LoadDashboardData();
                        dlg.Close();
                    }
                };

                dlg.Controls.AddRange(new Control[] { lst, btnOk });
                dlg.ShowDialog(this);
            }
        }

        private void OpenForm(Form form)
        {
            form.MdiParent = null;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.ShowDialog(this);
            LoadDashboardData(); // Veriyi yenile
        }

        private void Logout()
        {
            timerClock.Stop();
            _logger.Info($"Çıkış yapıldı: {_currentUser.Username}");
            this.Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            timerClock?.Stop();
            timerClock?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
