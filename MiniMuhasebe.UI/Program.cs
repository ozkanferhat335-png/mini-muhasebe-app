using System;
using System.IO;
using System.Windows.Forms;
using MiniMuhasebe.Data;
using MiniMuhasebe.UI.Forms;

namespace MiniMuhasebe.UI
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // Veritabanı yolunu ayarla
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MiniMuhasebe");

                if (!Directory.Exists(appDataPath))
                    Directory.CreateDirectory(appDataPath);

                string dbPath = Path.Combine(appDataPath, "MiniMuhasebe.db");
                AppSession.ConnectionString = $"Data Source={dbPath};Version=3;Foreign Keys=True;";

                // Veritabanını başlat
                var initializer = new DatabaseInitializer(AppSession.ConnectionString);
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
