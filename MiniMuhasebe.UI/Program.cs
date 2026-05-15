using System;
using System.Windows.Forms;
using MiniMuhasebe.Data;
using MiniMuhasebe.UI.Forms;

namespace MiniMuhasebe.UI
{
    static class Program
    {
        public static string ConnectionString { get; private set; }
        public static string EncryptionKey { get; private set; }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // Veritabanı bağlantı ayarları
                string dbPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MiniMuhasebe", "MiniMuhasebe.db");

                ConnectionString = $"Data Source={dbPath};Version=3;Foreign Keys=True;";
                EncryptionKey = "MiniMuhasebe_SecureKey_2026!";

                // Veritabanını başlat
                var initializer = new DatabaseInitializer(ConnectionString);
                initializer.Initialize();

                // Giriş formunu aç
                Application.Run(new LoginForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Uygulama başlatılırken kritik hata oluştu:\n\n{ex.Message}\n\nUygulama kapatılıyor.",
                    "Kritik Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
