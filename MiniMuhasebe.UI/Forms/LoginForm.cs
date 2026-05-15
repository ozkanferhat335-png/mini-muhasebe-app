using System;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI.Forms
{
    public class LoginForm : Form
    {
        private Label lblTitle;
        private Label lblSubtitle;
        private Label lblUsername;
        private Label lblPassword;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Button btnExit;
        private Label lblStatus;
        private Panel pnlMain;
        private PictureBox picLogo;

        private readonly UserService _userService;
        private int _failedAttempts = 0;
        private const int MaxFailedAttempts = 5;

        public LoginForm()
        {
            _userService = new UserService(Program.ConnectionString);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Mini Muhasebe - Giriş";
            this.Size = new Size(420, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 9f);

            // Ana panel
            pnlMain = new Panel
            {
                Size = new Size(360, 420),
                Location = new Point(30, 40),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            pnlMain.Paint += (s, e) =>
            {
                e.Graphics.DrawRectangle(new Pen(Color.FromArgb(220, 220, 220)), 0, 0, pnlMain.Width - 1, pnlMain.Height - 1);
            };

            // Logo alanı
            picLogo = new PictureBox
            {
                Size = new Size(60, 60),
                Location = new Point(150, 30),
                BackColor = Color.FromArgb(41, 128, 185),
                BorderStyle = BorderStyle.None
            };

            // Başlık
            lblTitle = new Label
            {
                Text = "Mini Muhasebe",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.FromArgb(41, 128, 185),
                AutoSize = true,
                Location = new Point(80, 105)
            };

            lblSubtitle = new Label
            {
                Text = "Küçük İşletme Muhasebe Sistemi",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(75, 140)
            };

            // Kullanıcı adı
            lblUsername = new Label
            {
                Text = "Kullanıcı Adı",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = true,
                Location = new Point(30, 185)
            };

            txtUsername = new TextBox
            {
                Size = new Size(300, 30),
                Location = new Point(30, 205),
                Font = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 249, 250)
            };

            // Şifre
            lblPassword = new Label
            {
                Text = "Şifre",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = true,
                Location = new Point(30, 245)
            };

            txtPassword = new TextBox
            {
                Size = new Size(300, 30),
                Location = new Point(30, 265),
                Font = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 249, 250),
                PasswordChar = '●'
            };

            // Durum etiketi
            lblStatus = new Label
            {
                Text = string.Empty,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.Red,
                AutoSize = true,
                Location = new Point(30, 305)
            };

            // Giriş butonu
            btnLogin = new Button
            {
                Text = "Giriş Yap",
                Size = new Size(300, 40),
                Location = new Point(30, 325),
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            // Çıkış butonu
            btnExit = new Button
            {
                Text = "Çıkış",
                Size = new Size(300, 35),
                Location = new Point(30, 375),
                BackColor = Color.FromArgb(236, 240, 241),
                ForeColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.Click += (s, e) => Application.Exit();

            pnlMain.Controls.AddRange(new Control[] {
                picLogo, lblTitle, lblSubtitle,
                lblUsername, txtUsername,
                lblPassword, txtPassword,
                lblStatus, btnLogin, btnExit
            });

            this.Controls.Add(pnlMain);

            // Enter tuşu ile giriş
            this.AcceptButton = btnLogin;
            txtUsername.Focus();
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            if (_failedAttempts >= MaxFailedAttempts)
            {
                lblStatus.Text = "Çok fazla başarısız deneme. Lütfen uygulamayı yeniden başlatın.";
                btnLogin.Enabled = false;
                return;
            }

            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblStatus.Text = "Kullanıcı adı ve şifre boş bırakılamaz.";
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "Giriş yapılıyor...";
            lblStatus.Text = string.Empty;

            try
            {
                User user = _userService.Login(username, password);

                if (user != null)
                {
                    _failedAttempts = 0;
                    var dashboard = new MainDashboardForm(user);
                    this.Hide();
                    dashboard.FormClosed += (s2, e2) => this.Close();
                    dashboard.Show();
                }
                else
                {
                    _failedAttempts++;
                    int remaining = MaxFailedAttempts - _failedAttempts;
                    lblStatus.Text = $"Kullanıcı adı veya şifre hatalı. ({remaining} deneme hakkı kaldı)";
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Giriş sırasında hata oluştu.";
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLogin.Enabled = _failedAttempts < MaxFailedAttempts;
                btnLogin.Text = "Giriş Yap";
            }
        }
    }
}
