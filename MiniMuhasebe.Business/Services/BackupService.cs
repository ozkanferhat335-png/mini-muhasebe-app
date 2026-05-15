using System;
using System.Data.SQLite;
using System.IO;
using MiniMuhasebe.Data;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Services
{
    /// <summary>
    /// Yedekleme ve geri yükleme servisi
    /// </summary>
    public class BackupService
    {
        private readonly string _connectionString;
        private readonly string _databasePath;
        private readonly string _backupDirectory;
        private readonly Logger _logger;

        public BackupService(string connectionString, string backupDirectory = "Backups")
        {
            _connectionString = connectionString;
            _backupDirectory = backupDirectory;
            _logger = new Logger();

            // Bağlantı stringinden veritabanı yolunu çıkar
            try
            {
                var builder = new SQLiteConnectionStringBuilder(connectionString);
                _databasePath = builder.DataSource;
            }
            catch
            {
                _databasePath = "MiniMuhasebe.db";
            }

            // Yedek klasörünü oluştur
            if (!Directory.Exists(_backupDirectory))
                Directory.CreateDirectory(_backupDirectory);
        }

        /// <summary>
        /// Tek tık yedek alma
        /// </summary>
        public Backup CreateBackup(string customName = null)
        {
            try
            {
                if (!File.Exists(_databasePath))
                {
                    _logger.Error($"Veritabanı dosyası bulunamadı: {_databasePath}");
                    return null;
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = customName ?? $"MiniMuhasebe_Backup_{timestamp}.db";
                string backupPath = Path.Combine(_backupDirectory, fileName);

                // SQLite online backup API kullan
                using (var sourceConn = new SQLiteConnection(_connectionString))
                using (var destConn = new SQLiteConnection($"Data Source={backupPath};Version=3;"))
                {
                    sourceConn.Open();
                    destConn.Open();
                    sourceConn.BackupDatabase(destConn, "main", "main", -1, null, 0);
                }

                var fileInfo = new FileInfo(backupPath);
                var backup = new Backup
                {
                    BackupFileName = fileName,
                    BackupPath = backupPath,
                    BackupSize = fileInfo.Length,
                    CreatedAt = DateTime.Now
                };

                // Yedek kaydını veritabanına ekle
                SaveBackupRecord(backup);

                _logger.Info($"Yedek oluşturuldu: {fileName} ({fileInfo.Length / 1024} KB)");
                return backup;
            }
            catch (Exception ex)
            {
                _logger.Error("Yedek oluşturma sırasında hata", ex);
                return null;
            }
        }

        /// <summary>
        /// Yedekten geri yükle
        /// </summary>
        public bool RestoreBackup(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    _logger.Error($"Yedek dosyası bulunamadı: {backupPath}");
                    return false;
                }

                // Mevcut veritabanını geçici olarak yedekle
                string tempBackup = _databasePath + ".temp_" + DateTime.Now.Ticks;
                File.Copy(_databasePath, tempBackup, true);

                try
                {
                    // Geri yükle
                    using (var sourceConn = new SQLiteConnection($"Data Source={backupPath};Version=3;"))
                    using (var destConn = new SQLiteConnection(_connectionString))
                    {
                        sourceConn.Open();
                        destConn.Open();
                        sourceConn.BackupDatabase(destConn, "main", "main", -1, null, 0);
                    }

                    // Geçici yedeği sil
                    if (File.Exists(tempBackup))
                        File.Delete(tempBackup);

                    // Geri yükleme kaydını güncelle
                    UpdateRestoreRecord(backupPath);

                    _logger.Info($"Yedekten geri yükleme başarılı: {backupPath}");
                    return true;
                }
                catch
                {
                    // Hata durumunda orijinal veritabanını geri yükle
                    if (File.Exists(tempBackup))
                    {
                        File.Copy(tempBackup, _databasePath, true);
                        File.Delete(tempBackup);
                    }
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Geri yükleme sırasında hata", ex);
                return false;
            }
        }

        /// <summary>
        /// Eski yedekleri temizle (maksimum sayıyı aşanları sil)
        /// </summary>
        public int CleanOldBackups(int maxBackupCount = 10)
        {
            try
            {
                var backupFiles = Directory.GetFiles(_backupDirectory, "MiniMuhasebe_Backup_*.db");
                Array.Sort(backupFiles); // Tarihe göre sırala (eski → yeni)

                int deleted = 0;
                int toDelete = backupFiles.Length - maxBackupCount;

                for (int i = 0; i < toDelete; i++)
                {
                    File.Delete(backupFiles[i]);
                    deleted++;
                    _logger.Info($"Eski yedek silindi: {backupFiles[i]}");
                }

                return deleted;
            }
            catch (Exception ex)
            {
                _logger.Error("Eski yedek temizleme sırasında hata", ex);
                return 0;
            }
        }

        /// <summary>
        /// Yedek klasöründeki tüm yedekleri listele
        /// </summary>
        public string[] GetBackupFiles()
        {
            try
            {
                if (!Directory.Exists(_backupDirectory))
                    return new string[0];

                var files = Directory.GetFiles(_backupDirectory, "*.db");
                Array.Sort(files);
                Array.Reverse(files); // En yeni önce
                return files;
            }
            catch (Exception ex)
            {
                _logger.Error("Yedek listesi alınırken hata", ex);
                return new string[0];
            }
        }

        private void SaveBackupRecord(Backup backup)
        {
            try
            {
                string query = @"INSERT INTO Backups (BackupFileName, BackupPath, BackupSize, CreatedAt) 
                               VALUES (@FileName, @Path, @Size, @CreatedAt)";
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@FileName", backup.BackupFileName);
                        cmd.Parameters.AddWithValue("@Path", backup.BackupPath);
                        cmd.Parameters.AddWithValue("@Size", backup.BackupSize ?? 0);
                        cmd.Parameters.AddWithValue("@CreatedAt", backup.CreatedAt);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning("Yedek kaydı veritabanına eklenirken hata", ex);
            }
        }

        private void UpdateRestoreRecord(string backupPath)
        {
            try
            {
                string query = "UPDATE Backups SET RestoredAt = @RestoredAt WHERE BackupPath = @BackupPath";
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@RestoredAt", DateTime.Now);
                        cmd.Parameters.AddWithValue("@BackupPath", backupPath);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning("Geri yükleme kaydı güncellenirken hata", ex);
            }
        }
    }
}
