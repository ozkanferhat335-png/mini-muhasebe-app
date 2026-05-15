using System;
using System.Drawing;
using System.Windows.Forms;
using MiniMuhasebe.Business.Services;

namespace MiniMuhasebe.UI.Forms
{
    public class LoginForm : Form
    {
        private Panel pnlLeft;
        private Panel pnlRight;
        private Label lblTitle;
        private Label lblSubtitle;
        private Label lblVersion;
        private Label lblUsername;
        private Label lblPassword;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblError;
        private CheckBox chkRemember;
        private PictureBox picLogo;

        private readonly UserService _userService;
        private readonly CompanyService _companyService;
        private readonly FiscalPeriodService _periodService;
        private readonly AuditLogService _auditLogService;

        public LoginForm()
        {
            _userService = new UserService(AppSession.ConnectionString);
            _companyService = new CompanyService(AppSession.ConnectionString);
            _periodService = new FiscalPeriodService(AppSession.ConnectionString);
            _auditLogService = new AuditLogService(AppSession.ConnectionString);

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Mini Muhasebe - Giriş";
            this.Size = new Size(800, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            // Sol panel (mavi)
            pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 350,
                BackColor = Color.FromArgb(41, 128, 185)
            };

            lblTitle = new Label
            {
                Text = "Mini\nMuhasebe",
                Font = new Font("Segoe UI", 28, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(300, 100),
                Location = new Point(25, 120),
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblSubtitle = new Label
            {
                Text = "Küçük İşletmeler İçin\nMuhasebe Çözümü",
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.FromArgb(200, 230, 255),
                AutoSize = false,
                Size = new Size(300, 60),
                Location = new Point(25, 230),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblFeatures = new Label
            {
                Text = "✓ Gelir-Gider Takibi\n✓ Cari Hesap Yönetimi\n✓ Banka Entegrasyonu\n✓ Eşleştirme Sistemi\n✓ Raporlama",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(180, 220, 255),
                AutoSize = false,
                Size = new Size(300, 120),
                Location = new Point(25, 310),
                TextAlign = ContentAlignment.TopLeft
            };

            lblVersion = new Label
            {
                Text = "v1.0.0 © 2026",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(150, 200, 240),
                AutoSize = false,
                Size = new Size(300, 25),
                Location = new Point(25, 450),
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlLeft.Controls.AddRange(new Control[] { lblTitle, lblSubtitle, lblFeatures, lblVersion });

            // Sağ panel (giriş formu)
            pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(40)
            };

            var lblWelcome = new Label
            {
                Text = "Hoş Geldiniz",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(40, 80)
            };

            var lblSignIn = new Label
            {
                Text = "Hesabınıza giriş yapın",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(40, 115)
            };

            lblUsername = new Label
            {
                Text = "Kullanıcı Adı",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(40, 170)
            };

            txtUsername = new TextBox
            {
                Location = new Point(40, 192),
                Size = new Size(330, 35),
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.FixedSingle,
                Text = "admin"
            };

            lblPassword = new Label
            {
                Text = "Şifre",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(40, 245)
            };

            txtPassword = new TextBox
            {
                Location = new Point(40, 267),
                Size = new Size(330, 35),
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = '●',
                Text = "Admin123!"
            };

            chkRemember = new CheckBox
            {
                Text = "Beni Hatırla",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.Gray,
                Location = new Point(40, 315),
                AutoSize = true
            };

            btnLogin = new Button
            {
                Text = "GİRİŞ YAP",
                Location = new Point(40, 350),
                Size = new Size(330, 45),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(192, 57, 43),
                AutoSize = false,
                Size = new Size(330, 30),
                Location = new Point(40, 405),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblHint = new Label
            {
                Text = "Varsayılan: admin / Admin123!",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.LightGray,
                AutoSize = true,
                Location = new Point(40, 440)
            };

            pnlRight.Controls.AddRange(new Control[] {
                lblWelcome, lblSignIn, lblUsername, txtUsername,
                lblPassword, txtPassword, chkRemember, btnLogin, lblError, lblHint
            });

            this.Controls.AddRange(new Control[] { pnlLeft, pnlRight });

            // Enter tuşu ile giriş
            this.AcceptButton = btnLogin;
            txtUsername.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtPassword.Focus(); };
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            lblError.Text = "";
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblError.Text = "Kullanıcı adı ve şifre gereklidir.";
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "Giriş yapılıyor...";

            try
            {
                var user = _userService.Login(username, password);
                if (user == null)
                {
                    lblError.Text = "Kullanıcı adı veya şifre hatalı!";
                    txtPassword.Clear();
                    txtPassword.Focus();
                    return;
                }

                AppSession.CurrentUser = user;
                _auditLogService.LogLogin(user.UserId, user.Username);

                // Firma seç
                var companies = _companyService.GetAllCompanies();
                if (companies.Count == 0)
                {
                    MessageBox.Show("Sistemde kayıtlı firma bulunamadı. Lütfen önce firma ekleyin.",
                        "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    AppSession.CurrentCompany = companies[0];
                    var period = _periodService.GetActivePeriod(companies[0].CompanyId);
                    AppSession.CurrentPeriod = period;
                }

                var dashboard = new MainDashboardForm();
                dashboard.Show();
                this.Hide();
                dashboard.FormClosed += (s2, e2) => this.Close();
            }
            catch (Exception ex)
            {
                lblError.Text = "Giriş sırasında hata oluştu.";
                MessageBox.Show(ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "GİRİŞ YAP";
            }
        }
    }
}
