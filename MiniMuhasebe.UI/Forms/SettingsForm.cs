using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Data;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    public class SettingsForm : Form
    {
        private TabControl tabControl;
        private readonly User _currentUser;
        private readonly string _initialTab;

        private readonly CompanyService _companyService;
        private readonly UserService _userService;
        private readonly BackupService _backupService;
        private readonly FiscalPeriodRepository _periodRepository;
        private readonly AuditLogRepository _auditLogRepository;

        public SettingsForm(User user, string initialTab = "general")
        {
            _currentUser = user;
            _initialTab = initialTab;
            _companyService = new CompanyService(Program.ConnectionString);
            _userService = new UserService(Program.ConnectionString);
            _backupService = new BackupService(Program.ConnectionString);
            _periodRepository = new FiscalPeriodRepository(Program.ConnectionString);
            _auditLogRepository = new AuditLogRepository(Program.ConnectionString);

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Ayarlar";
            this.Size = new Size(950, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 9f);

            tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5f) };

            tabControl.TabPages.Add(BuildCompanyTab());
            tabControl.TabPages.Add(BuildFiscalPeriodTab());
            tabControl.TabPages.Add(BuildUserTab());
            tabControl.TabPages.Add(BuildBackupTab());
            tabControl.TabPages.Add(BuildAuditLogTab());
            tabControl.TabPages.Add(BuildGeneralTab());

            // Başlangıç sekmesini seç
            switch (_initialTab)
            {
                case "company": tabControl.SelectedIndex = 0; break;
                case "period": tabControl.SelectedIndex = 1; break;
                case "user": tabControl.SelectedIndex = 2; break;
                case "backup": tabControl.SelectedIndex = 3; break;
                case "audit": tabControl.SelectedIndex = 4; break;
                default: tabControl.SelectedIndex = 5; break;
            }

            this.Controls.Add(tabControl);
        }

        // ─── Firma Yönetimi ───────────────────────────────────────────────────────
        private TabPage BuildCompanyTab()
        {
            var tab = new TabPage("🏢 Firma Yönetimi") { BackColor = Color.White };

            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White, Padding = new Padding(8) };
            var btnAdd = CreateBtn("➕ Yeni Firma", Color.FromArgb(39, 174, 96), 10);
            var btnEdit = CreateBtn("✏️ Düzenle", Color.FromArgb(41, 128, 185), 130);
            var btnDelete = CreateBtn("🗑️ Sil", Color.FromArgb(231, 76, 60), 250);
            var btnRefresh = CreateBtn("🔄 Yenile", Color.FromArgb(149, 165, 166), 370);

            var dgv = CreateDgv();
            dgv.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", FillWeight = 5 },
                new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Firma Adı", FillWeight = 30 },
                new DataGridViewTextBoxColumn { Name = "colTaxOffice", HeaderText = "Vergi Dairesi", FillWeight = 20 },
                new DataGridViewTextBoxColumn { Name = "colTaxNo", HeaderText = "Vergi No", FillWeight = 15 },
                new DataGridViewTextBoxColumn { Name = "colPhone", HeaderText = "Telefon", FillWeight = 15 },
                new DataGridViewTextBoxColumn { Name = "colEmail", HeaderText = "E-posta", FillWeight = 15 }
            });

            List<Company> companies = new List<Company>();

            Action loadCompanies = () =>
            {
                dgv.Rows.Clear();
                companies = _companyService.GetAllCompanies();
                foreach (var c in companies)
                    dgv.Rows.Add(c.CompanyId, c.CompanyName, c.TaxOffice, c.TaxNumber, c.Phone, c.Email);
            };

            btnRefresh.Click += (s, e) => loadCompanies();
            btnAdd.Click += (s, e) =>
            {
                using (var dlg = new CompanyEditForm(null))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK) loadCompanies();
                }
            };
            btnEdit.Click += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["colId"].Value);
                var company = companies.Find(c => c.CompanyId == id);
                if (company == null) return;
                using (var dlg = new CompanyEditForm(company))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK) loadCompanies();
                }
            };
            btnDelete.Click += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["colId"].Value);
                string name = dgv.SelectedRows[0].Cells["colName"].Value?.ToString();
                if (MessageBox.Show($"'{name}' firmasını silmek istediğinizden emin misiniz?",
                    "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (_companyService.DeleteCompany(id)) loadCompanies();
                }
            };

            pnlToolbar.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnRefresh });
            tab.Controls.AddRange(new Control[] { dgv, pnlToolbar });
            tab.VisibleChanged += (s, e) => { if (tab.Visible) loadCompanies(); };
            return tab;
        }

        // ─── Mali Dönem Yönetimi ──────────────────────────────────────────────────
        private TabPage BuildFiscalPeriodTab()
        {
            var tab = new TabPage("📅 Mali Dönemler") { BackColor = Color.White };

            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White, Padding = new Padding(8) };

            var lblCompany = new Label { Text = "Firma:", AutoSize = true, Location = new Point(10, 15), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var cmbCompany = new ComboBox { Location = new Point(60, 11), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "CompanyName" };

            var btnAdd = CreateBtn("➕ Yeni Dönem", Color.FromArgb(39, 174, 96), 275);
            var btnClose = CreateBtn("🔒 Dönemi Kapat", Color.FromArgb(243, 156, 18), 395);
            var btnDelete = CreateBtn("🗑️ Sil", Color.FromArgb(231, 76, 60), 515);

            var dgv = CreateDgv();
            dgv.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", FillWeight = 5 },
                new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Dönem Adı", FillWeight = 25 },
                new DataGridViewTextBoxColumn { Name = "colStart", HeaderText = "Başlangıç", FillWeight = 20 },
                new DataGridViewTextBoxColumn { Name = "colEnd", HeaderText = "Bitiş", FillWeight = 20 },
                new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "Durum", FillWeight = 15 }
            });

            List<FiscalPeriod> periods = new List<FiscalPeriod>();

            Action loadPeriods = () =>
            {
                dgv.Rows.Clear();
                if (cmbCompany.SelectedItem is not Company selected) return;
                periods = _periodRepository.GetByCompanyId(selected.CompanyId);
                foreach (var p in periods)
                    dgv.Rows.Add(p.PeriodId, p.PeriodName, p.StartDate.ToString("dd.MM.yyyy"), p.EndDate.ToString("dd.MM.yyyy"), p.IsClosed ? "🔒 Kapalı" : "✅ Açık");
            };

            cmbCompany.SelectedIndexChanged += (s, e) => loadPeriods();

            btnAdd.Click += (s, e) =>
            {
                if (cmbCompany.SelectedItem is not Company selected) return;
                using (var dlg = new FiscalPeriodEditForm(selected))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK) loadPeriods();
                }
            };

            btnClose.Click += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["colId"].Value);
                if (MessageBox.Show("Bu dönemi kapatmak istediğinizden emin misiniz? Bu işlem geri alınamaz.",
                    "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    if (_periodRepository.ClosePeriod(id)) loadPeriods();
                }
            };

            btnDelete.Click += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["colId"].Value);
                if (MessageBox.Show("Bu dönemi silmek istediğinizden emin misiniz?",
                    "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (_periodRepository.Delete(id)) loadPeriods();
                }
            };

            pnlToolbar.Controls.AddRange(new Control[] { lblCompany, cmbCompany, btnAdd, btnClose, btnDelete });
            tab.Controls.AddRange(new Control[] { dgv, pnlToolbar });

            tab.VisibleChanged += (s, e) =>
            {
                if (!tab.Visible) return;
                cmbCompany.Items.Clear();
                var companies = _companyService.GetAllCompanies();
                foreach (var c in companies) cmbCompany.Items.Add(c);
                if (cmbCompany.Items.Count > 0) cmbCompany.SelectedIndex = 0;
            };

            return tab;
        }

        // ─── Kullanıcı Yönetimi ───────────────────────────────────────────────────
        private TabPage BuildUserTab()
        {
            var tab = new TabPage("👤 Kullanıcı Yönetimi") { BackColor = Color.White };

            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White, Padding = new Padding(8) };
            var btnAdd = CreateBtn("➕ Yeni Kullanıcı", Color.FromArgb(39, 174, 96), 10);
            var btnChangePwd = CreateBtn("🔑 Şifre Değiştir", Color.FromArgb(243, 156, 18), 130);
            var btnRefresh = CreateBtn("🔄 Yenile", Color.FromArgb(149, 165, 166), 250);

            var dgv = CreateDgv();
            dgv.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", FillWeight = 5 },
                new DataGridViewTextBoxColumn { Name = "colUsername", HeaderText = "Kullanıcı Adı", FillWeight = 20 },
                new DataGridViewTextBoxColumn { Name = "colEmail", HeaderText = "E-posta", FillWeight = 25 },
                new DataGridViewTextBoxColumn { Name = "colRole", HeaderText = "Rol", FillWeight = 15 },
                new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "Durum", FillWeight = 10 },
                new DataGridViewTextBoxColumn { Name = "colLastLogin", HeaderText = "Son Giriş", FillWeight = 20 }
            });

            List<User> users = new List<User>();

            Action loadUsers = () =>
            {
                dgv.Rows.Clear();
                users = _userService.GetAllUsers();
                foreach (var u in users)
                    dgv.Rows.Add(u.UserId, u.Username, u.Email, u.RoleId == 1 ? "Yönetici" : "Kullanıcı",
                        u.IsActive ? "✅ Aktif" : "❌ Pasif", u.LastLoginAt?.ToString("dd.MM.yyyy HH:mm") ?? "Hiç giriş yapılmadı");
            };

            btnRefresh.Click += (s, e) => loadUsers();

            btnAdd.Click += (s, e) =>
            {
                if (_currentUser.RoleId != 1)
                { MessageBox.Show("Bu işlem için yönetici yetkisi gereklidir.", "Yetki Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                using (var dlg = new UserCreateForm())
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK) loadUsers();
                }
            };

            btnChangePwd.Click += (s, e) =>
            {
                using (var dlg = new ChangePasswordForm(_currentUser))
                {
                    dlg.ShowDialog(this);
                }
            };

            pnlToolbar.Controls.AddRange(new Control[] { btnAdd, btnChangePwd, btnRefresh });
            tab.Controls.AddRange(new Control[] { dgv, pnlToolbar });
            tab.VisibleChanged += (s, e) => { if (tab.Visible) loadUsers(); };
            return tab;
        }

        // ─── Yedekleme ────────────────────────────────────────────────────────────
        private TabPage BuildBackupTab()
        {
            var tab = new TabPage("💾 Yedekleme") { BackColor = Color.White };

            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 120, BackColor = Color.White, Padding = new Padding(15) };

            var lblTitle = new Label { Text = "Veritabanı Yedekleme ve Geri Yükleme", Font = new Font("Segoe UI", 12f, FontStyle.Bold), AutoSize = true, Location = new Point(15, 15), ForeColor = Color.FromArgb(41, 128, 185) };

            var btnBackup = new Button
            {
                Text = "💾 Şimdi Yedek Al",
                Size = new Size(160, 40),
                Location = new Point(15, 50),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };

            var btnRestore = new Button
            {
                Text = "📂 Yedekten Geri Yükle",
                Size = new Size(190, 40),
                Location = new Point(185, 50),
                BackColor = Color.FromArgb(243, 156, 18),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };

            var btnClean = new Button
            {
                Text = "🗑️ Eski Yedekleri Temizle",
                Size = new Size(200, 40),
                Location = new Point(385, 50),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };

            var lblStatus = new Label { Text = string.Empty, AutoSize = true, Location = new Point(15, 100), Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(39, 174, 96) };

            var lstBackups = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f) };

            Action loadBackups = () =>
            {
                lstBackups.Items.Clear();
                var files = _backupService.GetBackupFiles();
                foreach (var f in files)
                {
                    var fi = new FileInfo(f);
                    lstBackups.Items.Add($"{fi.Name} ({fi.Length / 1024} KB) - {fi.LastWriteTime:dd.MM.yyyy HH:mm}");
                }
            };

            btnBackup.Click += (s, e) =>
            {
                try
                {
                    Cursor = Cursors.WaitCursor;
                    var backup = _backupService.CreateBackup();
                    if (backup != null)
                    {
                        lblStatus.Text = $"✅ Yedek oluşturuldu: {backup.BackupFileName}";
                        lblStatus.ForeColor = Color.FromArgb(39, 174, 96);
                        loadBackups();
                    }
                    else
                    {
                        lblStatus.Text = "❌ Yedek oluşturulamadı.";
                        lblStatus.ForeColor = Color.FromArgb(231, 76, 60);
                    }
                }
                finally { Cursor = Cursors.Default; }
            };

            btnRestore.Click += (s, e) =>
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Filter = "SQLite Veritabanı (*.db)|*.db|Tüm Dosyalar (*.*)|*.*";
                    ofd.Title = "Geri Yüklenecek Yedek Dosyasını Seçin";

                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        if (MessageBox.Show("Mevcut veritabanı seçilen yedekle değiştirilecek. Devam etmek istiyor musunuz?",
                            "Uyarı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                        {
                            try
                            {
                                Cursor = Cursors.WaitCursor;
                                bool success = _backupService.RestoreBackup(ofd.FileName);
                                lblStatus.Text = success ? "✅ Geri yükleme başarılı. Uygulamayı yeniden başlatın." : "❌ Geri yükleme başarısız.";
                                lblStatus.ForeColor = success ? Color.FromArgb(39, 174, 96) : Color.FromArgb(231, 76, 60);
                            }
                            finally { Cursor = Cursors.Default; }
                        }
                    }
                }
            };

            btnClean.Click += (s, e) =>
            {
                int deleted = _backupService.CleanOldBackups(10);
                lblStatus.Text = $"✅ {deleted} eski yedek silindi.";
                lblStatus.ForeColor = Color.FromArgb(39, 174, 96);
                loadBackups();
            };

            pnlTop.Controls.AddRange(new Control[] { lblTitle, btnBackup, btnRestore, btnClean, lblStatus });
            tab.Controls.AddRange(new Control[] { lstBackups, pnlTop });
            tab.VisibleChanged += (s, e) => { if (tab.Visible) loadBackups(); };
            return tab;
        }

        // ─── Denetim Günlüğü ──────────────────────────────────────────────────────
        private TabPage BuildAuditLogTab()
        {
            var tab = new TabPage("📋 Denetim Günlüğü") { BackColor = Color.White };

            var pnlFilter = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White, Padding = new Padding(8) };

            var lblStart = new Label { Text = "Başlangıç:", AutoSize = true, Location = new Point(10, 15), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var dtpStart = new DateTimePicker { Location = new Point(85, 11), Size = new Size(130, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Now.AddDays(-7) };
            var lblEnd = new Label { Text = "Bitiş:", AutoSize = true, Location = new Point(225, 15), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var dtpEnd = new DateTimePicker { Location = new Point(265, 11), Size = new Size(130, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Now };

            var btnLoad = CreateBtn("🔍 Yükle", Color.FromArgb(41, 128, 185), 410);

            var dgv = CreateDgv();
            dgv.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", FillWeight = 5 },
                new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "Tarih/Saat", FillWeight = 15 },
                new DataGridViewTextBoxColumn { Name = "colUser", HeaderText = "Kullanıcı", FillWeight = 12 },
                new DataGridViewTextBoxColumn { Name = "colAction", HeaderText = "İşlem", FillWeight = 15 },
                new DataGridViewTextBoxColumn { Name = "colTable", HeaderText = "Tablo", FillWeight = 15 },
                new DataGridViewTextBoxColumn { Name = "colRecord", HeaderText = "Kayıt ID", FillWeight = 8 },
                new DataGridViewTextBoxColumn { Name = "colNew", HeaderText = "Yeni Değer", FillWeight = 30 }
            });

            btnLoad.Click += (s, e) =>
            {
                dgv.Rows.Clear();
                var logs = _auditLogRepository.GetByDateRange(dtpStart.Value, dtpEnd.Value.AddDays(1));
                foreach (var log in logs)
                    dgv.Rows.Add(log.AuditLogId, log.CreatedAt.ToString("dd.MM.yyyy HH:mm:ss"),
                        log.UserId?.ToString() ?? "-", log.Action, log.TableName, log.RecordId?.ToString() ?? "-", log.NewValue);
            };

            pnlFilter.Controls.AddRange(new Control[] { lblStart, dtpStart, lblEnd, dtpEnd, btnLoad });
            tab.Controls.AddRange(new Control[] { dgv, pnlFilter });
            return tab;
        }

        // ─── Genel Ayarlar ────────────────────────────────────────────────────────
        private TabPage BuildGeneralTab()
        {
            var tab = new TabPage("⚙️ Genel Ayarlar") { BackColor = Color.White };

            var pnlContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            var lblTitle = new Label
            {
                Text = "Uygulama Bilgileri",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(41, 128, 185),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            var info = new Label
            {
                Text = "Mini Muhasebe v1.0\n\n" +
                       "Küçük esnaf ve KOBİ'ler için geliştirilmiş masaüstü muhasebe uygulaması.\n\n" +
                       "Teknolojiler:\n" +
                       "  • Dil: C# 7.3\n" +
                       "  • UI: Windows Forms\n" +
                       "  • Veritabanı: SQLite\n" +
                       "  • Mimari: Katmanlı Mimari\n\n" +
                       "Özellikler:\n" +
                       "  ✅ Kullanıcı ve Yetki Yönetimi\n" +
                       "  ✅ Firma ve Dönem Yönetimi\n" +
                       "  ✅ Gelir-Gider Takibi\n" +
                       "  ✅ Cari Hesap Yönetimi\n" +
                       "  ✅ Banka Hesapları ve API Entegrasyonu\n" +
                       "  ✅ Otomatik/Manuel Eşleştirme\n" +
                       "  ✅ Raporlama ve CSV Dışa Aktarım\n" +
                       "  ✅ Yedekleme ve Geri Yükleme",
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = true,
                Location = new Point(20, 60)
            };

            pnlContent.Controls.AddRange(new Control[] { lblTitle, info });
            tab.Controls.Add(pnlContent);
            return tab;
        }

        private Button CreateBtn(string text, Color color, int x)
        {
            return new Button
            {
                Text = text,
                Size = new Size(115, 32),
                Location = new Point(x, 8),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
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
                Font = new Font("Segoe UI", 9f),
                GridColor = Color.FromArgb(230, 230, 230),
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(248, 249, 250) }
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(41, 128, 185);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            return dgv;
        }
    }

    // ─── Yardımcı Formlar ─────────────────────────────────────────────────────────

    public class CompanyEditForm : Form
    {
        private TextBox txtName, txtTaxOffice, txtTaxNo, txtPhone, txtEmail, txtAddress;
        private Button btnSave, btnCancel;
        private readonly Company _editing;
        private readonly CompanyService _service;

        public CompanyEditForm(Company editing)
        {
            _editing = editing;
            _service = new CompanyService(Program.ConnectionString);
            InitializeComponent();
            if (editing != null) PopulateFields(editing);
        }

        private void InitializeComponent()
        {
            this.Text = _editing == null ? "Yeni Firma" : "Firma Düzenle";
            this.Size = new Size(430, 360);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9f);

            int y = 20, lx = 15, cx = 140, cw = 255;

            void AddRow(string label, Control ctrl)
            {
                var lbl = new Label { Text = label, AutoSize = true, Location = new Point(lx, y + 4), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
                ctrl.Location = new Point(cx, y);
                ctrl.Size = new Size(cw, 25);
                this.Controls.AddRange(new Control[] { lbl, ctrl });
                y += 32;
            }

            txtName = new TextBox(); AddRow("Firma Adı:*", txtName);
            txtTaxOffice = new TextBox(); AddRow("Vergi Dairesi:", txtTaxOffice);
            txtTaxNo = new TextBox(); AddRow("Vergi No:", txtTaxNo);
            txtPhone = new TextBox(); AddRow("Telefon:", txtPhone);
            txtEmail = new TextBox(); AddRow("E-posta:", txtEmail);
            txtAddress = new TextBox(); AddRow("Adres:", txtAddress);

            btnSave = new Button
            {
                Text = "💾 Kaydet", Size = new Size(120, 35), Location = new Point(cx, y + 5),
                BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text = "İptal", Size = new Size(90, 35), Location = new Point(cx + 130, y + 5),
                BackColor = Color.FromArgb(236, 240, 241), ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
            this.ClientSize = new Size(410, y + 55);
        }

        private void PopulateFields(Company c)
        {
            txtName.Text = c.CompanyName;
            txtTaxOffice.Text = c.TaxOffice;
            txtTaxNo.Text = c.TaxNumber;
            txtPhone.Text = c.Phone;
            txtEmail.Text = c.Email;
            txtAddress.Text = c.Address;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            { MessageBox.Show("Firma adı zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (_editing == null)
            {
                var result = _service.CreateCompany(txtName.Text.Trim(), txtTaxOffice.Text.Trim(), txtTaxNo.Text.Trim(), txtPhone.Text.Trim(), txtEmail.Text.Trim(), txtAddress.Text.Trim());
                if (result != null) { this.DialogResult = DialogResult.OK; }
                else MessageBox.Show("Firma eklenirken hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                _editing.CompanyName = txtName.Text.Trim();
                _editing.TaxOffice = txtTaxOffice.Text.Trim();
                _editing.TaxNumber = txtTaxNo.Text.Trim();
                _editing.Phone = txtPhone.Text.Trim();
                _editing.Email = txtEmail.Text.Trim();
                _editing.Address = txtAddress.Text.Trim();
                if (_service.UpdateCompany(_editing)) { this.DialogResult = DialogResult.OK; }
                else MessageBox.Show("Güncelleme sırasında hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class FiscalPeriodEditForm : Form
    {
        private TextBox txtName;
        private DateTimePicker dtpStart, dtpEnd;
        private Button btnSave, btnCancel;
        private readonly Company _company;
        private readonly FiscalPeriodRepository _repo;

        public FiscalPeriodEditForm(Company company)
        {
            _company = company;
            _repo = new FiscalPeriodRepository(Program.ConnectionString);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Yeni Mali Dönem";
            this.Size = new Size(400, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9f);

            int y = 20, lx = 15, cx = 140, cw = 220;

            void AddRow(string label, Control ctrl)
            {
                var lbl = new Label { Text = label, AutoSize = true, Location = new Point(lx, y + 4), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
                ctrl.Location = new Point(cx, y);
                ctrl.Size = new Size(cw, 25);
                this.Controls.AddRange(new Control[] { lbl, ctrl });
                y += 32;
            }

            txtName = new TextBox { Text = $"{DateTime.Now.Year} - {System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month)}" };
            AddRow("Dönem Adı:*", txtName);

            dtpStart = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1) };
            AddRow("Başlangıç:*", dtpStart);

            dtpEnd = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)) };
            AddRow("Bitiş:*", dtpEnd);

            btnSave = new Button
            {
                Text = "💾 Kaydet", Size = new Size(120, 35), Location = new Point(cx, y + 5),
                BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text = "İptal", Size = new Size(90, 35), Location = new Point(cx + 130, y + 5),
                BackColor = Color.FromArgb(236, 240, 241), ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
            this.ClientSize = new Size(380, y + 55);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            { MessageBox.Show("Dönem adı zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (dtpEnd.Value < dtpStart.Value)
            { MessageBox.Show("Bitiş tarihi başlangıç tarihinden önce olamaz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            var period = new FiscalPeriod
            {
                CompanyId = _company.CompanyId,
                PeriodName = txtName.Text.Trim(),
                StartDate = dtpStart.Value,
                EndDate = dtpEnd.Value,
                IsClosed = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            int id = _repo.Add(period);
            if (id > 0) { this.DialogResult = DialogResult.OK; }
            else MessageBox.Show("Dönem eklenirken hata oluştu.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public class UserCreateForm : Form
    {
        private TextBox txtUsername, txtEmail, txtPassword, txtConfirmPassword;
        private ComboBox cmbRole;
        private Button btnSave, btnCancel;
        private readonly UserService _service;

        public UserCreateForm()
        {
            _service = new UserService(Program.ConnectionString);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Yeni Kullanıcı Oluştur";
            this.Size = new Size(400, 310);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9f);

            int y = 20, lx = 15, cx = 150, cw = 210;

            void AddRow(string label, Control ctrl)
            {
                var lbl = new Label { Text = label, AutoSize = true, Location = new Point(lx, y + 4), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
                ctrl.Location = new Point(cx, y);
                ctrl.Size = new Size(cw, 25);
                this.Controls.AddRange(new Control[] { lbl, ctrl });
                y += 32;
            }

            txtUsername = new TextBox(); AddRow("Kullanıcı Adı:*", txtUsername);
            txtEmail = new TextBox(); AddRow("E-posta:", txtEmail);
            txtPassword = new TextBox { PasswordChar = '●' }; AddRow("Şifre:*", txtPassword);
            txtConfirmPassword = new TextBox { PasswordChar = '●' }; AddRow("Şifre Tekrar:*", txtConfirmPassword);

            cmbRole = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRole.Items.AddRange(new object[] { "Standart Kullanıcı", "Yönetici" });
            cmbRole.SelectedIndex = 0;
            AddRow("Rol:*", cmbRole);

            btnSave = new Button
            {
                Text = "💾 Oluştur", Size = new Size(120, 35), Location = new Point(cx, y + 5),
                BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text = "İptal", Size = new Size(90, 35), Location = new Point(cx + 130, y + 5),
                BackColor = Color.FromArgb(236, 240, 241), ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
            this.ClientSize = new Size(380, y + 55);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            { MessageBox.Show("Kullanıcı adı ve şifre zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (txtPassword.Text != txtConfirmPassword.Text)
            { MessageBox.Show("Şifreler eşleşmiyor.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (txtPassword.Text.Length < 6)
            { MessageBox.Show("Şifre en az 6 karakter olmalıdır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            int roleId = cmbRole.SelectedIndex == 1 ? 1 : 2;
            var result = _service.CreateUser(txtUsername.Text.Trim(), txtEmail.Text.Trim(), txtPassword.Text, roleId);

            if (result != null)
            {
                MessageBox.Show("Kullanıcı oluşturuldu.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("Kullanıcı oluşturulamadı. Kullanıcı adı zaten mevcut olabilir.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class ChangePasswordForm : Form
    {
        private TextBox txtOldPassword, txtNewPassword, txtConfirmPassword;
        private Button btnSave, btnCancel;
        private readonly User _user;
        private readonly UserService _service;

        public ChangePasswordForm(User user)
        {
            _user = user;
            _service = new UserService(Program.ConnectionString);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Şifre Değiştir";
            this.Size = new Size(380, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9f);

            int y = 20, lx = 15, cx = 150, cw = 190;

            void AddRow(string label, Control ctrl)
            {
                var lbl = new Label { Text = label, AutoSize = true, Location = new Point(lx, y + 4), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
                ctrl.Location = new Point(cx, y);
                ctrl.Size = new Size(cw, 25);
                this.Controls.AddRange(new Control[] { lbl, ctrl });
                y += 32;
            }

            txtOldPassword = new TextBox { PasswordChar = '●' }; AddRow("Mevcut Şifre:*", txtOldPassword);
            txtNewPassword = new TextBox { PasswordChar = '●' }; AddRow("Yeni Şifre:*", txtNewPassword);
            txtConfirmPassword = new TextBox { PasswordChar = '●' }; AddRow("Yeni Şifre Tekrar:*", txtConfirmPassword);

            btnSave = new Button
            {
                Text = "🔑 Değiştir", Size = new Size(120, 35), Location = new Point(cx, y + 5),
                BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text = "İptal", Size = new Size(90, 35), Location = new Point(cx + 130, y + 5),
                BackColor = Color.FromArgb(236, 240, 241), ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
            this.ClientSize = new Size(360, y + 55);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtOldPassword.Text) || string.IsNullOrWhiteSpace(txtNewPassword.Text))
            { MessageBox.Show("Tüm alanlar zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (txtNewPassword.Text != txtConfirmPassword.Text)
            { MessageBox.Show("Yeni şifreler eşleşmiyor.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (txtNewPassword.Text.Length < 6)
            { MessageBox.Show("Yeni şifre en az 6 karakter olmalıdır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            bool success = _service.ChangePassword(_user.UserId, txtOldPassword.Text, txtNewPassword.Text);
            if (success)
            {
                MessageBox.Show("Şifre başarıyla değiştirildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("Şifre değiştirilemedi. Mevcut şifrenizi kontrol edin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
