using System;
using System.Windows.Forms;
using MiniMuhasebe.Data;
using MiniMuhasebe.UI.Forms;

namespace MiniMuhasebe.UI
{
    static class Program
    {
        /// <summary>
        /// Uygulama bağlantı dizesi
        /// </summary>
        public static string ConnectionString { get; private set; }

        /// <summary>
        /// Şifreleme anahtarı (üretimde güvenli bir yerden okunmalı)
        /// </summary>
        public static string EncryptionKey { get; private set; } = "MiniMuhasebe@2026!SecureKey#XYZ";

        /// <summary>
        /// Oturum açmış kullanıcı ID'si
        /// </summary>
        public static int? CurrentUserId { get; set; }

        /// <summary>
        /// Oturum açmış kullanıcı adı
        /// </summary>
        public static string CurrentUsername { get; set; }

        /// <summary>
        /// Aktif firma ID'si
        /// </summary>
        public static int? ActiveCompanyId { get; set; }

        /// <summary>
        /// Aktif firma adı
        /// </summary>
        public static string ActiveCompanyName { get; set; }

        /// <summary>
        /// Aktif dönem ID'si
        /// </summary>
        public static int? ActivePeriodId { get; set; }

        /// <summary>
        /// Aktif dönem adı
        /// </summary>
        public static string ActivePeriodName { get; set; }

        /// <summary>
        /// Kullanıcı rolü (Admin / User)
        /// </summary>
        public static string CurrentUserRole { get; set; }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // Veritabanı bağlantısını yapılandır
                string dbPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "MiniMuhasebe.db");
                ConnectionString = $"Data Source={dbPath};Version=3;";

                // Veritabanını başlat
                var initializer = new DatabaseInitializer(ConnectionString);
                initializer.Initialize();

                // Giriş formunu aç
                Application.Run(new LoginForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Uygulama başlatılırken kritik hata oluştu:\n\n{ex.Message}\n\nUygulama kapatılacak.",
                    "Kritik Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
