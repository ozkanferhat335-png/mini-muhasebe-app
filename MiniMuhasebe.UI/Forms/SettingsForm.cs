using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    public class SettingsForm : Form
    {
        private TabControl tabControl;
        private TabPage tabGeneral, tabCompany, tabPeriods, tabUsers, tabBackup, tabAuditLog;

        private readonly CompanyService _companyService;
        private readonly FiscalPeriodService _periodService;
        private readonly UserService _userService;
        private readonly BackupService _backupService;
        private readonly AuditLogService _auditLogService;
        private readonly AppSettingsRepository _settingsRepo;

        public SettingsForm()
        {
            _companyService = new CompanyService(AppSession.ConnectionString);
            _periodService = new FiscalPeriodService(AppSession.ConnectionString);
            _userService = new UserService(AppSession.ConnectionString);
            _backupService = new BackupService(AppSession.ConnectionString);
            _auditLogService = new AuditLogService(AppSession.ConnectionString);
            _settingsRepo = new AppSettingsRepository(AppSession.ConnectionString);

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Ayarlar";
            this.BackColor = Color.FromArgb(245, 247, 250);

            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.White };
            var lblTitle = new Label
            {
                Text = "⚙️ Ayarlar",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(10, 15)
            };
            pnlToolbar.Controls.Add(lblTitle);

            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10)
            };

            tabGeneral = new TabPage("Genel Ayarlar");
            tabCompany = new TabPage("Firma Yönetimi");
            tabPeriods = new TabPage("Mali Dönemler");
            tabUsers = new TabPage("Kullanıcı Yönetimi");
            tabBackup = new TabPage("Yedekleme");
            tabAuditLog = new TabPage("Denetim Günlüğü");

            tabControl.TabPages.AddRange(new TabPage[] { tabGeneral, tabCompany, tabPeriods, tabUsers, tabBackup, tabAuditLog });

            BuildGeneralTab();
            BuildCompanyTab();
            BuildPeriodsTab();
            BuildUsersTab();
            BuildBackupTab();
            BuildAuditLogTab();

            this.Controls.AddRange(new Control[] { tabControl, pnlToolbar });
        }

        private void BuildGeneralTab()
        {
            tabGeneral.BackColor = Color.White;
            tabGeneral.Padding = new Padding(15);

            int y = 20, lx = 20, cx = 200, w = 250;

            var settings = _settingsRepo.GetAll();
            var settingDict = new Dictionary<string, AppSetting>();
            foreach (var s in settings) settingDict[s.SettingKey] = s;

            AddSectionLabel(tabGeneral, "Genel Uygulama Ayarları", lx, y); y += 35;

            var controls = new Dictionary<string, Control>();

            foreach (var key in new[] { "DefaultCurrency", "DateFormat", "DecimalSeparator", "ThousandsSeparator", "LogLevel" })
            {
                string value = settingDict.ContainsKey(key) ? settingDict[key].SettingValue : "";
                AddLabel(tabGeneral, key + ":", lx, y);
                var tb = new TextBox { Location = new Point(cx, y), Size = new Size(w, 25), Font = new Font("Segoe UI", 9), Text = value };
                tabGeneral.Controls.Add(tb);
                controls[key] = tb;
                y += 40;
            }

            y += 10;
            AddSectionLabel(tabGeneral, "Eşleştirme Ayarları", lx, y); y += 35;

            foreach (var key in new[] { "AmountTolerance", "DateTolerance" })
            {
                string value = settingDict.ContainsKey(key) ? settingDict[key].SettingValue : "";
                AddLabel(tabGeneral, key + ":", lx, y);
                var tb = new TextBox { Location = new Point(cx, y), Size = new Size(150, 25), Font = new Font("Segoe UI", 9), Text = value };
                tabGeneral.Controls.Add(tb);
                controls[key] = tb;
                y += 40;
            }

            var btnSave = new Button
            {
                Text = "💾 Ayarları Kaydet",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(39, 174, 96),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(lx, y + 10),
                Size = new Size(160, 38),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) =>
            {
                foreach (var kvp in controls)
                    _settingsRepo.SetValue(kvp.Key, ((TextBox)kvp.Value).Text.Trim());
                MessageBox.Show("Ayarlar kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            tabGeneral.Controls.Add(btnSave);

            // Şifre değiştirme
            y += 60;
            AddSectionLabel(tabGeneral, "Şifre Değiştir", lx, y); y += 35;
            AddLabel(tabGeneral, "Eski Şifre:", lx, y);
            var txtOld = new TextBox { Location = new Point(cx, y), Size = new Size(w, 25), Font = new Font("Segoe UI", 9), PasswordChar = '●' };
            tabGeneral.Controls.Add(txtOld); y += 40;
            AddLabel(tabGeneral, "Yeni Şifre:", lx, y);
            var txtNew = new TextBox { Location = new Point(cx, y), Size = new Size(w, 25), Font = new Font("Segoe UI", 9), PasswordChar = '●' };
            tabGeneral.Controls.Add(txtNew); y += 40;
            AddLabel(tabGeneral, "Yeni Şifre (Tekrar):", lx, y);
            var txtConfirm = new TextBox { Location = new Point(cx, y), Size = new Size(w, 25), Font = new Font("Segoe UI", 9), PasswordChar = '●' };
            tabGeneral.Controls.Add(txtConfirm); y += 40;

            var btnChangePass = new Button
            {
                Text = "🔒 Şifreyi Değiştir",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(41, 128, 185),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(lx, y),
                Size = new Size(160, 38),
                Cursor = Cursors.Hand
            };
            btnChangePass.FlatAppearance.BorderSize = 0;
            btnChangePass.Click += (s, e) =>
            {
                if (txtNew.Text != txtConfirm.Text) { MessageBox.Show("Yeni şifreler eşleşmiyor.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                if (string.IsNullOrEmpty(txtNew.Text)) { MessageBox.Show("Yeni şifre boş olamaz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                bool success = _userService.ChangePassword(AppSession.CurrentUser.UserId, txtOld.Text, txtNew.Text);
                if (success) { MessageBox.Show("Şifre başarıyla değiştirildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information); txtOld.Clear(); txtNew.Clear(); txtConfirm.Clear(); }
                else MessageBox.Show("Şifre değiştirilemedi. Eski şifrenizi kontrol edin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            tabGeneral.Controls.Add(btnChangePass);
        }

        private void BuildCompanyTab()
        {
            tabCompany.BackColor = Color.White;

            var dgv = CreateSimpleGrid();
            dgv.Dock = DockStyle.Fill;
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompanyId", HeaderText = "ID", Width = 50 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompanyName", HeaderText = "Firma Adı", FillWeight = 30 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TaxOffice", HeaderText = "Vergi Dairesi", FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TaxNumber", HeaderText = "Vergi No", FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "Telefon", FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "E-posta", FillWeight = 20 });

            var pnlBtn = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(235, 240, 245) };
            var btnNew = CreateSmallBtn("➕ Yeni Firma", Color.FromArgb(39, 174, 96), 10, 8);
            var btnEdit = CreateSmallBtn("✏️ Düzenle", Color.FromArgb(41, 128, 185), 120, 8);
            var btnSetActive = CreateSmallBtn("✓ Aktif Yap", Color.FromArgb(230, 126, 34), 230, 8);
            pnlBtn.Controls.AddRange(new Control[] { btnNew, btnEdit, btnSetActive });

            Action loadCompanies = () =>
            {
                dgv.Rows.Clear();
                var companies = _companyService.GetAllCompanies();
                foreach (var c in companies)
                    dgv.Rows.Add(c.CompanyId, c.CompanyName, c.TaxOffice, c.TaxNumber, c.Phone, c.Email);
            };

            btnNew.Click += (s, e) =>
            {
                var d = new CompanyEditDialog(null);
                if (d.ShowDialog() == DialogResult.OK)
                {
                    var data = d.CompanyData;
                    _companyService.CreateCompany(data.CompanyName, data.TaxOffice, data.TaxNumber, data.Phone, data.Email, data.Address);
                    loadCompanies();
                }
            };

            btnEdit.Click += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["CompanyId"].Value);
                var company = _companyService.GetCompanyById(id);
                var d = new CompanyEditDialog(company);
                if (d.ShowDialog() == DialogResult.OK) { _companyService.UpdateCompany(d.CompanyData); loadCompanies(); }
            };

            btnSetActive.Click += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["CompanyId"].Value);
                var company = _companyService.GetCompanyById(id);
                if (company != null)
                {
                    AppSession.CurrentCompany = company;
                    AppSession.CurrentPeriod = _periodService.GetActivePeriod(id);
                    MessageBox.Show($"Aktif firma: {company.CompanyName}", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            tabCompany.Controls.AddRange(new Control[] { dgv, pnlBtn });
            loadCompanies();
        }

        private void BuildPeriodsTab()
        {
            tabPeriods.BackColor = Color.White;

            var dgv = CreateSimpleGrid();
            dgv.Dock = DockStyle.Fill;
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "PeriodId", HeaderText = "ID", Width = 50 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "PeriodName", HeaderText = "Dönem Adı", FillWeight = 25 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "StartDate", HeaderText = "Başlangıç", FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "EndDate", HeaderText = "Bitiş", FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "IsClosed", HeaderText = "Durum", FillWeight = 10 });

            var pnlBtn = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(235, 240, 245) };
            var btnNew = CreateSmallBtn("➕ Yeni Dönem", Color.FromArgb(39, 174, 96), 10, 8);
            var btnYear = CreateSmallBtn("📅 Yıllık Oluştur", Color.FromArgb(41, 128, 185), 120, 8);
            var btnClose = CreateSmallBtn("🔒 Kapat", Color.FromArgb(192, 57, 43), 240, 8);
            var btnSetActive = CreateSmallBtn("✓ Aktif Yap", Color.FromArgb(230, 126, 34), 330, 8);
            pnlBtn.Controls.AddRange(new Control[] { btnNew, btnYear, btnClose, btnSetActive });

            Action loadPeriods = () =>
            {
                dgv.Rows.Clear();
                if (AppSession.CurrentCompany == null) return;
                var periods = _periodService.GetPeriodsByCompany(AppSession.CurrentCompany.CompanyId);
                foreach (var p in periods)
                    dgv.Rows.Add(p.PeriodId, p.PeriodName, p.StartDate.ToString("dd.MM.yyyy"), p.EndDate.ToString("dd.MM.yyyy"), p.IsClosed ? "Kapalı" : "Açık");
            };

            btnNew.Click += (s, e) =>
            {
                if (AppSession.CurrentCompany == null) return;
                var d = new PeriodEditDialog(null, AppSession.CurrentCompany.CompanyId);
                if (d.ShowDialog() == DialogResult.OK)
                {
                    _periodService.CreatePeriod(AppSession.CurrentCompany.CompanyId, d.PeriodName, d.StartDate, d.EndDate);
                    loadPeriods();
                }
            };

            btnYear.Click += (s, e) =>
            {
                if (AppSession.CurrentCompany == null) return;
                int year = DateTime.Now.Year;
                _periodService.CreateYearlyPeriods(AppSession.CurrentCompany.CompanyId, year);
                loadPeriods();
                MessageBox.Show($"{year} yılı için 12 aylık dönem oluşturuldu.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            btnClose.Click += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["PeriodId"].Value);
                if (MessageBox.Show("Bu dönemi kapatmak istediğinizden emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                { _periodService.ClosePeriod(id); loadPeriods(); }
            };

            btnSetActive.Click += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["PeriodId"].Value);
                var period = _periodService.GetPeriodById(id);
                if (period != null) { AppSession.CurrentPeriod = period; MessageBox.Show($"Aktif dönem: {period.PeriodName}", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            };

            tabPeriods.Controls.AddRange(new Control[] { dgv, pnlBtn });
            loadPeriods();
        }

        private void BuildUsersTab()
        {
            tabUsers.BackColor = Color.White;

            if (!AppSession.IsAdmin)
            {
                tabUsers.Controls.Add(new Label
                {
                    Text = "Bu sekmeye erişim için yönetici yetkisi gereklidir.",
                    Font = new Font("Segoe UI", 12),
                    ForeColor = Color.Gray,
                    AutoSize = true,
                    Location = new Point(20, 20)
                });
                return;
            }

            var dgv = CreateSimpleGrid();
            dgv.Dock = DockStyle.Fill;
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserId", HeaderText = "ID", Width = 50 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Username", HeaderText = "Kullanıcı Adı", FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "E-posta", FillWeight = 25 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "RoleId", HeaderText = "Rol", FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "IsActive", HeaderText = "Durum", FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastLogin", HeaderText = "Son Giriş", FillWeight = 20 });

            var pnlBtn = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(235, 240, 245) };
            var btnNew = CreateSmallBtn("➕ Yeni Kullanıcı", Color.FromArgb(39, 174, 96), 10, 8);
            pnlBtn.Controls.Add(btnNew);

            Action loadUsers = () =>
            {
                dgv.Rows.Clear();
                var users = _userService.GetAllUsers();
                foreach (var u in users)
                    dgv.Rows.Add(u.UserId, u.Username, u.Email, u.RoleId == 1 ? "Yönetici" : "Standart",
                        u.IsActive ? "Aktif" : "Pasif", u.LastLoginAt?.ToString("dd.MM.yyyy HH:mm") ?? "-");
            };

            btnNew.Click += (s, e) =>
            {
                var d = new UserEditDialog();
                if (d.ShowDialog() == DialogResult.OK)
                {
                    _userService.CreateUser(d.Username, d.Email, d.Password, d.RoleId);
                    loadUsers();
                }
            };

            tabUsers.Controls.AddRange(new Control[] { dgv, pnlBtn });
            loadUsers();
        }

        private void BuildBackupTab()
        {
            tabBackup.BackColor = Color.White;
            tabBackup.Padding = new Padding(15);

            int y = 20;

            AddSectionLabel(tabBackup, "Yedekleme İşlemleri", 20, y); y += 40;

            var btnBackup = new Button
            {
                Text = "💾 Şimdi Yedek Al",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(39, 174, 96),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(20, y),
                Size = new Size(180, 45),
                Cursor = Cursors.Hand
            };
            btnBackup.FlatAppearance.BorderSize = 0;
            btnBackup.Click += (s, e) =>
            {
                var backup = _backupService.CreateBackup();
                if (backup != null)
                    MessageBox.Show($"Yedek oluşturuldu:\n{backup.BackupPath}\nBoyut: {backup.BackupSize / 1024} KB", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show("Yedekleme başarısız!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            tabBackup.Controls.Add(btnBackup);

            var btnRestore = new Button
            {
                Text = "📂 Yedekten Geri Yükle",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(230, 126, 34),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(210, y),
                Size = new Size(200, 45),
                Cursor = Cursors.Hand
            };
            btnRestore.FlatAppearance.BorderSize = 0;
            btnRestore.Click += (s, e) =>
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Filter = "SQLite Veritabanı|*.db";
                    ofd.Title = "Yedek Dosyası Seçin";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        if (MessageBox.Show("Geri yükleme işlemi mevcut verilerin üzerine yazacak. Devam etmek istiyor musunuz?",
                            "Uyarı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                        {
                            bool success = _backupService.RestoreBackup(ofd.FileName);
                            MessageBox.Show(success ? "Geri yükleme başarılı! Uygulamayı yeniden başlatın." : "Geri yükleme başarısız!",
                                success ? "Başarılı" : "Hata", MessageBoxButtons.OK,
                                success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                        }
                    }
                }
            };
            tabBackup.Controls.Add(btnRestore);

            y += 65;
            AddSectionLabel(tabBackup, "Mevcut Yedekler", 20, y); y += 35;

            var dgv = CreateSimpleGrid();
            dgv.Location = new Point(20, y);
            dgv.Size = new Size(700, 300);
            dgv.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "FileName", HeaderText = "Dosya Adı", FillWeight = 40 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Size", HeaderText = "Boyut", FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Tarih", FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Path", HeaderText = "Yol", FillWeight = 40 });

            var backups = _backupService.GetBackupList();
            foreach (var b in backups)
                dgv.Rows.Add(b.BackupFileName, $"{b.BackupSize / 1024} KB", b.CreatedAt.ToString("dd.MM.yyyy HH:mm"), b.BackupPath);

            tabBackup.Controls.Add(dgv);
        }

        private void BuildAuditLogTab()
        {
            tabAuditLog.BackColor = Color.White;

            var dgv = CreateSimpleGrid();
            dgv.Dock = DockStyle.Fill;
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "AuditLogId", HeaderText = "ID", Width = 50 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedAt", HeaderText = "Tarih/Saat", FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserId", HeaderText = "Kullanıcı ID", FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Action", HeaderText = "İşlem", FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TableName", HeaderText = "Tablo", FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "RecordId", HeaderText = "Kayıt ID", FillWeight = 8 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "NewValue", HeaderText = "Detay", FillWeight = 40 });

            var pnlBtn = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(235, 240, 245) };
            var btnRefresh = CreateSmallBtn("🔄 Yenile", Color.FromArgb(127, 140, 141), 10, 8);
            pnlBtn.Controls.Add(btnRefresh);

            Action loadLogs = () =>
            {
                dgv.Rows.Clear();
                var logs = _auditLogService.GetRecentLogs(200);
                foreach (var l in logs)
                    dgv.Rows.Add(l.AuditLogId, l.CreatedAt.ToString("dd.MM.yyyy HH:mm:ss"),
                        l.UserId?.ToString() ?? "-", l.Action, l.TableName, l.RecordId?.ToString() ?? "-", l.NewValue);
            };

            btnRefresh.Click += (s, e) => loadLogs();
            tabAuditLog.Controls.AddRange(new Control[] { dgv, pnlBtn });
            loadLogs();
        }

        private DataGridView CreateSimpleGrid()
        {
            var dgv = new DataGridView
            {
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
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(44, 62, 80);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersHeight = 35;
            return dgv;
        }

        private Button CreateSmallBtn(string text, Color color, int x, int y)
        {
            var btn = new Button
            {
                Text = text, Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White, BackColor = color, FlatStyle = FlatStyle.Flat,
                Location = new Point(x, y), Size = new Size(105, 28), Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void AddSectionLabel(Control parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(41, 128, 185),
                AutoSize = true,
                Location = new Point(x, y)
            });
        }

        private void AddLabel(Control parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(x, y + 3)
            });
        }
    }

    // Firma düzenleme dialog
    public class CompanyEditDialog : Form
    {
        public Company CompanyData { get; private set; }
        private TextBox txtName, txtTaxOffice, txtTaxNumber, txtPhone, txtEmail, txtAddress;

        public CompanyEditDialog(Company existing)
        {
            CompanyData = existing ?? new Company { IsActive = true };
            this.Text = CompanyData.CompanyId == 0 ? "Yeni Firma" : "Firma Düzenle";
            this.Size = new Size(450, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            int y = 20, lx = 20, cx = 150, w = 260;
            AddLabel("Firma Adı:", lx, y); txtName = AddTB(cx, y, w, CompanyData.CompanyName); y += 40;
            AddLabel("Vergi Dairesi:", lx, y); txtTaxOffice = AddTB(cx, y, w, CompanyData.TaxOffice); y += 40;
            AddLabel("Vergi No:", lx, y); txtTaxNumber = AddTB(cx, y, w, CompanyData.TaxNumber); y += 40;
            AddLabel("Telefon:", lx, y); txtPhone = AddTB(cx, y, w, CompanyData.Phone); y += 40;
            AddLabel("E-posta:", lx, y); txtEmail = AddTB(cx, y, w, CompanyData.Email); y += 40;
            AddLabel("Adres:", lx, y); txtAddress = AddTB(cx, y, w, CompanyData.Address); y += 40;

            var btnOk = new Button { Text = "Kaydet", DialogResult = DialogResult.OK, Location = new Point(cx, y), Size = new Size(100, 35), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Firma adı gereklidir.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                CompanyData.CompanyName = txtName.Text.Trim(); CompanyData.TaxOffice = txtTaxOffice.Text.Trim();
                CompanyData.TaxNumber = txtTaxNumber.Text.Trim(); CompanyData.Phone = txtPhone.Text.Trim();
                CompanyData.Email = txtEmail.Text.Trim(); CompanyData.Address = txtAddress.Text.Trim();
                this.DialogResult = DialogResult.OK;
            };
            var btnCancel = new Button { Text = "İptal", DialogResult = DialogResult.Cancel, Location = new Point(cx + 110, y), Size = new Size(100, 35), BackColor = Color.FromArgb(127, 140, 141), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.FlatAppearance.BorderSize = 0;
            this.Controls.AddRange(new Control[] { btnOk, btnCancel });
            this.AcceptButton = btnOk; this.CancelButton = btnCancel;
        }

        private void AddLabel(string text, int x, int y) => this.Controls.Add(new Label { Text = text, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(44, 62, 80), AutoSize = true, Location = new Point(x, y + 3) });
        private TextBox AddTB(int x, int y, int w, string text = "") { var tb = new TextBox { Location = new Point(x, y), Size = new Size(w, 25), Font = new Font("Segoe UI", 9), Text = text ?? "" }; this.Controls.Add(tb); return tb; }
    }

    // Dönem düzenleme dialog
    public class PeriodEditDialog : Form
    {
        public string PeriodName { get; private set; }
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }

        private TextBox txtName;
        private DateTimePicker dtpStart, dtpEnd;

        public PeriodEditDialog(FiscalPeriod existing, int companyId)
        {
            this.Text = "Yeni Mali Dönem";
            this.Size = new Size(400, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            int y = 20, lx = 20, cx = 150, w = 210;
            AddLabel("Dönem Adı:", lx, y); txtName = new TextBox { Location = new Point(cx, y), Size = new Size(w, 25), Font = new Font("Segoe UI", 9), Text = existing?.PeriodName ?? $"{DateTime.Now.Year} - {DateTime.Now:MMMM}" }; this.Controls.Add(txtName); y += 40;
            AddLabel("Başlangıç:", lx, y); dtpStart = new DateTimePicker { Location = new Point(cx, y), Size = new Size(w, 25), Format = DateTimePickerFormat.Short, Value = existing?.StartDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1) }; this.Controls.Add(dtpStart); y += 40;
            AddLabel("Bitiş:", lx, y); dtpEnd = new DateTimePicker { Location = new Point(cx, y), Size = new Size(w, 25), Format = DateTimePickerFormat.Short, Value = existing?.EndDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)) }; this.Controls.Add(dtpEnd); y += 40;

            var btnOk = new Button { Text = "Kaydet", DialogResult = DialogResult.OK, Location = new Point(cx, y), Size = new Size(100, 35), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) => { PeriodName = txtName.Text.Trim(); StartDate = dtpStart.Value; EndDate = dtpEnd.Value; this.DialogResult = DialogResult.OK; };
            var btnCancel = new Button { Text = "İptal", DialogResult = DialogResult.Cancel, Location = new Point(cx + 110, y), Size = new Size(100, 35), BackColor = Color.FromArgb(127, 140, 141), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.FlatAppearance.BorderSize = 0;
            this.Controls.AddRange(new Control[] { btnOk, btnCancel });
            this.AcceptButton = btnOk; this.CancelButton = btnCancel;
        }

        private void AddLabel(string text, int x, int y) => this.Controls.Add(new Label { Text = text, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(44, 62, 80), AutoSize = true, Location = new Point(x, y + 3) });
    }

    // Kullanıcı oluşturma dialog
    public class UserEditDialog : Form
    {
        public string Username { get; private set; }
        public string Email { get; private set; }
        public string Password { get; private set; }
        public int RoleId { get; private set; }

        private TextBox txtUsername, txtEmail, txtPassword, txtConfirm;
        private ComboBox cmbRole;

        public UserEditDialog()
        {
            this.Text = "Yeni Kullanıcı";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            int y = 20, lx = 20, cx = 150, w = 210;
            AddLabel("Kullanıcı Adı:", lx, y); txtUsername = AddTB(cx, y, w); y += 40;
            AddLabel("E-posta:", lx, y); txtEmail = AddTB(cx, y, w); y += 40;
            AddLabel("Şifre:", lx, y); txtPassword = AddTB(cx, y, w); txtPassword.PasswordChar = '●'; y += 40;
            AddLabel("Şifre (Tekrar):", lx, y); txtConfirm = AddTB(cx, y, w); txtConfirm.PasswordChar = '●'; y += 40;
            AddLabel("Rol:", lx, y);
            cmbRole = new ComboBox { Location = new Point(cx, y), Size = new Size(150, 25), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9) };
            cmbRole.Items.AddRange(new object[] { "Standart Kullanıcı", "Yönetici" });
            cmbRole.SelectedIndex = 0;
            this.Controls.Add(cmbRole); y += 40;

            var btnOk = new Button { Text = "Kaydet", DialogResult = DialogResult.OK, Location = new Point(cx, y), Size = new Size(100, 35), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtUsername.Text)) { MessageBox.Show("Kullanıcı adı gereklidir.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (txtPassword.Text != txtConfirm.Text) { MessageBox.Show("Şifreler eşleşmiyor.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                if (string.IsNullOrEmpty(txtPassword.Text)) { MessageBox.Show("Şifre boş olamaz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                Username = txtUsername.Text.Trim(); Email = txtEmail.Text.Trim();
                Password = txtPassword.Text; RoleId = cmbRole.SelectedIndex == 1 ? 1 : 2;
                this.DialogResult = DialogResult.OK;
            };
            var btnCancel = new Button { Text = "İptal", DialogResult = DialogResult.Cancel, Location = new Point(cx + 110, y), Size = new Size(100, 35), BackColor = Color.FromArgb(127, 140, 141), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.FlatAppearance.BorderSize = 0;
            this.Controls.AddRange(new Control[] { btnOk, btnCancel });
            this.AcceptButton = btnOk; this.CancelButton = btnCancel;
        }

        private void AddLabel(string text, int x, int y) => this.Controls.Add(new Label { Text = text, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(44, 62, 80), AutoSize = true, Location = new Point(x, y + 3) });
        private TextBox AddTB(int x, int y, int w) { var tb = new TextBox { Location = new Point(x, y), Size = new Size(w, 25), Font = new Font("Segoe UI", 9) }; this.Controls.Add(tb); return tb; }
    }
}
