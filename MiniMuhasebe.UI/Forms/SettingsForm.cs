using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    /// <summary>
    /// Ayarlar formu (Kullanıcı yönetimi, yedekleme, şifre değiştirme)
    /// </summary>
    public class SettingsForm : Form
    {
        private string _section;
        private TabControl tabControl;

        public SettingsForm(string section)
        {
            _section = section;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Ayarlar";
            this.Size = new Size(800, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 247, 250);

            tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9) };

            var tabPassword = new TabPage("🔑 Şifre Değiştir");
            var tabUsers = new TabPage("👤 Kullanıcı Yönetimi");
            var tabBackup = new TabPage("💾 Yedekleme");

            tabControl.TabPages.AddRange(new[] { tabPassword, tabUsers, tabBackup });

            switch (_section)
            {
                case "Users": tabControl.SelectedTab = tabUsers; break;
                case "Backup": tabControl.SelectedTab = tabBackup; break;
                default: tabControl.SelectedTab = tabPassword; break;
            }

            BuildPasswordTab(tabPassword);
            BuildUsersTab(tabUsers);
            BuildBackupTab(tabBackup);

            this.Controls.Add(tabControl);
        }

        private void BuildPasswordTab(TabPage tab)
        {
            var pnl = new Panel { Location = new Point(50, 30), Size = new Size(400, 280), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            tab.Controls.Add(pnl);

            var lblTitle = new Label { Text = "Şifre Değiştir", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(41, 128, 185), Location = new Point(20, 15), Size = new Size(360, 30) };

            int y = 60;
            var lblOld = new Label { Text = "Mevcut Şifre:", Location = new Point(20, y + 3), Size = new Size(120, 20), Font = new Font("Segoe UI", 9) };
            var txtOld = new TextBox { Location = new Point(150, y), Size = new Size(220, 25), PasswordChar = '●', Font = new Font("Segoe UI", 9) }; y += 40;
            var lblNew = new Label { Text = "Yeni Şifre:", Location = new Point(20, y + 3), Size = new Size(120, 20), Font = new Font("Segoe UI", 9) };
            var txtNew = new TextBox { Location = new Point(150, y), Size = new Size(220, 25), PasswordChar = '●', Font = new Font("Segoe UI", 9) }; y += 40;
            var lblConfirm = new Label { Text = "Şifre Tekrar:", Location = new Point(20, y + 3), Size = new Size(120, 20), Font = new Font("Segoe UI", 9) };
            var txtConfirm = new TextBox { Location = new Point(150, y), Size = new Size(220, 25), PasswordChar = '●', Font = new Font("Segoe UI", 9) }; y += 50;

            var btnChange = new Button { Text = "🔑 Şifreyi Değiştir", Location = new Point(150, y), Size = new Size(160, 35), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnChange.FlatAppearance.BorderSize = 0;
            btnChange.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtOld.Text) || string.IsNullOrEmpty(txtNew.Text)) { MessageBox.Show("Tüm alanları doldurun.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (txtNew.Text != txtConfirm.Text) { MessageBox.Show("Yeni şifreler eşleşmiyor.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (txtNew.Text.Length < 6) { MessageBox.Show("Şifre en az 6 karakter olmalıdır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                var userService = new UserService(Program.ConnectionString);
                if (userService.ChangePassword(Program.CurrentUserId.Value, txtOld.Text, txtNew.Text))
                {
                    MessageBox.Show("Şifre başarıyla değiştirildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtOld.Clear(); txtNew.Clear(); txtConfirm.Clear();
                }
                else
                    MessageBox.Show("Şifre değiştirilemedi. Mevcut şifrenizi kontrol edin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            pnl.Controls.AddRange(new Control[] { lblTitle, lblOld, txtOld, lblNew, txtNew, lblConfirm, txtConfirm, btnChange });
        }

        private void BuildUsersTab(TabPage tab)
        {
            if (Program.CurrentUserRole != "Admin")
            {
                var lblNoAccess = new Label { Text = "⚠️ Bu bölüme erişim için yönetici yetkisi gereklidir.", Font = new Font("Segoe UI", 11), ForeColor = Color.FromArgb(231, 76, 60), Location = new Point(30, 30), Size = new Size(600, 30) };
                tab.Controls.Add(lblNoAccess);
                return;
            }

            var userService = new UserService(Program.ConnectionString);
            var users = userService.GetAllUsers();

            var dgv = new DataGridView { Location = new Point(10, 50), Size = new Size(740, 350), ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, RowHeadersVisible = false, Font = new Font("Segoe UI", 9) };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.EnableHeadersVisualStyles = false;
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "UserId", Width = 50 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kullanıcı Adı", DataPropertyName = "Username" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "E-posta", DataPropertyName = "Email" });
            dgv.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Aktif", DataPropertyName = "IsActive", Width = 60 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Son Giriş", DataPropertyName = "LastLoginAt", DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy HH:mm" } });
            dgv.DataSource = users;

            var btnNewUser = new Button { Text = "➕ Yeni Kullanıcı", Location = new Point(10, 10), Size = new Size(140, 30), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnNewUser.FlatAppearance.BorderSize = 0;
            btnNewUser.Click += (s, e) =>
            {
                var form = new NewUserForm();
                if (form.ShowDialog(tab.FindForm()) == DialogResult.OK)
                {
                    dgv.DataSource = null;
                    dgv.DataSource = userService.GetAllUsers();
                }
            };

            tab.Controls.AddRange(new Control[] { btnNewUser, dgv });
        }

        private void BuildBackupTab(TabPage tab)
        {
            var backupService = new BackupService(Program.ConnectionString);

            var pnlActions = new Panel { Location = new Point(10, 10), Size = new Size(740, 80), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            var btnBackup = new Button { Text = "💾 Yedek Al", Location = new Point(20, 20), Size = new Size(140, 40), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnBackup.FlatAppearance.BorderSize = 0;

            var btnRestore = new Button { Text = "📂 Geri Yükle", Location = new Point(175, 20), Size = new Size(140, 40), BackColor = Color.FromArgb(230, 126, 34), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnRestore.FlatAppearance.BorderSize = 0;

            var dgvBackups = new DataGridView { Location = new Point(10, 100), Size = new Size(740, 300), ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, RowHeadersVisible = false, Font = new Font("Segoe UI", 9) };
            dgvBackups.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94);
            dgvBackups.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvBackups.EnableHeadersVisualStyles = false;

            Action loadBackups = () =>
            {
                var backups = backupService.GetAllBackups();
                dgvBackups.Columns.Clear();
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "BackupId", Width = 50 });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Dosya Adı", DataPropertyName = "BackupFileName" });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Boyut (KB)", DataPropertyName = "BackupSize", DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Oluşturulma", DataPropertyName = "CreatedAt", DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy HH:mm" } });
                dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Geri Yükleme", DataPropertyName = "RestoredAt", DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy HH:mm" } });
                dgvBackups.DataSource = backups;
            };

            btnBackup.Click += (s, e) =>
            {
                var backup = backupService.CreateBackup();
                if (backup != null)
                {
                    MessageBox.Show($"Yedek başarıyla alındı:\n{backup.BackupFileName}", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    loadBackups();
                }
                else
                    MessageBox.Show("Yedek alınamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            btnRestore.Click += (s, e) =>
            {
                if (dgvBackups.SelectedRows.Count == 0) { MessageBox.Show("Lütfen geri yüklenecek yedeği seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (MessageBox.Show("Geri yükleme sonrasında mevcut veriler silinecektir. Devam etmek istiyor musunuz?", "Uyarı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    int id = Convert.ToInt32(dgvBackups.SelectedRows[0].Cells[0].Value);
                    if (backupService.RestoreBackup(id))
                    {
                        MessageBox.Show("Geri yükleme başarılı. Uygulama yeniden başlatılacak.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Application.Restart();
                    }
                    else
                        MessageBox.Show("Geri yükleme başarısız.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            pnlActions.Controls.AddRange(new Control[] { btnBackup, btnRestore });
            tab.Controls.AddRange(new Control[] { pnlActions, dgvBackups });
            loadBackups();
        }
    }

    /// <summary>
    /// Yeni kullanıcı oluşturma formu
    /// </summary>
    public class NewUserForm : Form
    {
        private TextBox txtUsername, txtEmail, txtPassword;
        private ComboBox cmbRole;
        private Button btnSave, btnCancel;
        private UserService _service;

        public NewUserForm()
        {
            _service = new UserService(Program.ConnectionString);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Yeni Kullanıcı";
            this.Size = new Size(400, 280);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 250);

            int y = 20, lx = 20, cx = 140, cw = 220;
            AddLbl("Kullanıcı Adı: *", lx, y); txtUsername = AddTxt(cx, y, cw); y += 35;
            AddLbl("E-posta:", lx, y); txtEmail = AddTxt(cx, y, cw); y += 35;
            AddLbl("Şifre: *", lx, y); txtPassword = AddTxt(cx, y, cw); txtPassword.PasswordChar = '●'; y += 35;
            AddLbl("Rol: *", lx, y); cmbRole = new ComboBox { Location = new Point(cx, y), Size = new Size(cw, 25), DropDownStyle = ComboBoxStyle.DropDownList }; cmbRole.Items.AddRange(new[] { "Admin (1)", "User (2)" }); cmbRole.SelectedIndex = 1; this.Controls.Add(cmbRole); y += 45;

            btnSave = new Button { Text = "💾 Kaydet", Location = new Point(cx, y), Size = new Size(110, 35), BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            btnCancel = new Button { Text = "İptal", Location = new Point(cx + 120, y), Size = new Size(80, 35), BackColor = Color.FromArgb(127, 140, 141), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.AddRange(new Control[] { btnSave, btnCancel });
            this.Height = y + 80;
        }

        private void AddLbl(string t, int x, int y) { var l = new Label { Text = t, Location = new Point(x, y + 3), Size = new Size(115, 20), Font = new Font("Segoe UI", 9) }; this.Controls.Add(l); }
        private TextBox AddTxt(int x, int y, int w) { var t = new TextBox { Location = new Point(x, y), Size = new Size(w, 25), Font = new Font("Segoe UI", 9) }; this.Controls.Add(t); return t; }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text)) { MessageBox.Show("Kullanıcı adı ve şifre zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (txtPassword.Text.Length < 6) { MessageBox.Show("Şifre en az 6 karakter olmalıdır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            int roleId = cmbRole.SelectedIndex == 0 ? 1 : 2;
            var result = _service.CreateUser(txtUsername.Text.Trim(), txtEmail.Text.Trim(), txtPassword.Text, roleId);
            if (result != null) { this.DialogResult = DialogResult.OK; this.Close(); }
            else MessageBox.Show("Kullanıcı oluşturulamadı. Kullanıcı adı zaten mevcut olabilir.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
