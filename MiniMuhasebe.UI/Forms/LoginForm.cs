using System;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Data.Repositories;

namespace MiniMuhasebe.UI.Forms
{
    /// <summary>
    /// Giriş formu
    /// </summary>
    public class LoginForm : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblTitle;
        private Label lblUsername;
        private Label lblPassword;
        private Label lblVersion;
        private Panel pnlMain;
        private int _failedAttempts = 0;
        private const int MaxFailedAttempts = 5;

        public LoginForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Mini Muhasebe - Giriş";
            this.Size = new Size(420, 380);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 250);

            // Ana panel
            pnlMain = new Panel
            {
                Size = new Size(360, 300),
                Location = new Point(30, 30),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Başlık
            lblTitle = new Label
            {
                Text = "Mini Muhasebe",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(41, 128, 185),
                Location = new Point(20, 20),
                Size = new Size(320, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblSubtitle = new Label
            {
                Text = "Küçük İşletme Muhasebe Sistemi",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                Location = new Point(20, 58),
                Size = new Size(320, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var separator = new Panel
            {
                BackColor = Color.FromArgb(41, 128, 185),
                Location = new Point(20, 85),
                Size = new Size(320, 2)
            };

            // Kullanıcı adı
            lblUsername = new Label
            {
                Text = "Kullanıcı Adı:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(30, 105),
                Size = new Size(300, 20)
            };

            txtUsername = new TextBox
            {
                Location = new Point(30, 128),
                Size = new Size(300, 28),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Şifre
            lblPassword = new Label
            {
                Text = "Şifre:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(30, 165),
                Size = new Size(300, 20)
            };

            txtPassword = new TextBox
            {
                Location = new Point(30, 188),
                Size = new Size(300, 28),
                Font = new Font("Segoe UI", 10),
                PasswordChar = '●',
                BorderStyle = BorderStyle.FixedSingle
            };

            // Giriş butonu
            btnLogin = new Button
            {
                Text = "Giriş Yap",
                Location = new Point(30, 230),
                Size = new Size(300, 40),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            // Enter tuşu ile giriş
            txtPassword.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) BtnLogin_Click(s, e); };
            txtUsername.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) txtPassword.Focus(); };

            pnlMain.Controls.AddRange(new Control[] {
                lblTitle, lblSubtitle, separator,
                lblUsername, txtUsername,
                lblPassword, txtPassword,
                btnLogin
            });

            // Versiyon etiketi
            lblVersion = new Label
            {
                Text = "v1.0.0 © 2026 Mini Muhasebe",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                Location = new Point(30, 345),
                Size = new Size(360, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            this.Controls.AddRange(new Control[] { pnlMain, lblVersion });
            this.AcceptButton = btnLogin;
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            if (_failedAttempts >= MaxFailedAttempts)
            {
                MessageBox.Show("Çok fazla başarısız giriş denemesi. Lütfen birkaç dakika bekleyin.",
                    "Hesap Kilitlendi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Kullanıcı adı ve şifre boş bırakılamaz.",
                    "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "Giriş yapılıyor...";

            try
            {
                var userService = new UserService(Program.ConnectionString);
                var user = userService.Login(username, password);

                if (user != null)
                {
                    Program.CurrentUserId = user.UserId;
                    Program.CurrentUsername = user.Username;

                    // Rol bilgisini al
                    var roleRepo = new Data.Repositories.UserRepository(Program.ConnectionString);
                    Program.CurrentUserRole = user.RoleId == 1 ? "Admin" : "User";

                    // Audit log
                    var auditService = new AuditLogService(Program.ConnectionString);
                    auditService.LogLogin(user.UserId, user.Username, true);

                    _failedAttempts = 0;
                    this.Hide();

                    var dashboard = new MainDashboardForm();
                    dashboard.FormClosed += (s2, e2) => this.Close();
                    dashboard.Show();
                }
                else
                {
                    _failedAttempts++;
                    int remaining = MaxFailedAttempts - _failedAttempts;
                    MessageBox.Show(
                        $"Kullanıcı adı veya şifre hatalı.\n{remaining} deneme hakkınız kaldı.",
                        "Giriş Başarısız", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Giriş sırasında hata oluştu:\n{ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "Giriş Yap";
            }
        }
    }
}
