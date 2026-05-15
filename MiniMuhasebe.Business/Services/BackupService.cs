using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using MiniMuhasebe.Data;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Services
{
    public class BackupService
    {
        private readonly string _connectionString;
        private readonly string _databasePath;
        private readonly AppSettingsRepository _settingsRepository;
        private readonly Logger _logger;

        public BackupService(string connectionString)
        {
            _connectionString = connectionString;
            _settingsRepository = new AppSettingsRepository(connectionString);
            _logger = new Logger();

            // Veritabanı yolunu çıkar
            try
            {
                var builder = new SQLiteConnectionStringBuilder(connectionString);
                _databasePath = builder.DataSource;
            }
            catch
            {
                _databasePath = "MiniMuhasebe.db";
            }
        }

        /// <summary>
        /// Veritabanını yedekler
        /// </summary>
        public Backup CreateBackup(string backupDirectory = null)
        {
            try
            {
                if (string.IsNullOrEmpty(backupDirectory))
                    backupDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");

                if (!Directory.Exists(backupDirectory))
                    Directory.CreateDirectory(backupDirectory);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFileName = $"MiniMuhasebe_Backup_{timestamp}.db";
                string backupPath = Path.Combine(backupDirectory, backupFileName);

                // SQLite online backup API kullan
                using (var sourceConn = new SQLiteConnection(_connectionString))
                {
                    sourceConn.Open();
                    using (var destConn = new SQLiteConnection($"Data Source={backupPath};Version=3;"))
                    {
                        destConn.Open();
                        sourceConn.BackupDatabase(destConn, "main", "main", -1, null, 0);
                    }
                }

                var fileInfo = new FileInfo(backupPath);
                var backup = new Backup
                {
                    BackupFileName = backupFileName,
                    BackupPath = backupPath,
                    BackupSize = fileInfo.Length,
                    CreatedAt = DateTime.Now
                };

                // Yedek kaydını veritabanına ekle
                SaveBackupRecord(backup);

                // Eski yedekleri temizle
                CleanOldBackups(backupDirectory);

                _logger.Info($"Yedek oluşturuldu: {backupFileName} ({fileInfo.Length / 1024} KB)");
                return backup;
            }
            catch (Exception ex)
            {
                _logger.Error("Yedekleme hatası", ex);
                return null;
            }
        }

        /// <summary>
        /// Yedeği geri yükler
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

                // Mevcut veritabanını yedekle
                CreateBackup();

                // Yedeği geri yükle
                using (var sourceConn = new SQLiteConnection($"Data Source={backupPath};Version=3;"))
                {
                    sourceConn.Open();
                    using (var destConn = new SQLiteConnection(_connectionString))
                    {
                        destConn.Open();
                        sourceConn.BackupDatabase(destConn, "main", "main", -1, null, 0);
                    }
                }

                _logger.Info($"Yedek geri yüklendi: {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("Geri yükleme hatası", ex);
                return false;
            }
        }

        /// <summary>
        /// Yedek listesini döndürür
        /// </summary>
        public List<Backup> GetBackupList(string backupDirectory = null)
        {
            var backups = new List<Backup>();
            try
            {
                if (string.IsNullOrEmpty(backupDirectory))
                    backupDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");

                if (!Directory.Exists(backupDirectory))
                    return backups;

                var files = Directory.GetFiles(backupDirectory, "MiniMuhasebe_Backup_*.db");
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    backups.Add(new Backup
                    {
                        BackupFileName = fileInfo.Name,
                        BackupPath = file,
                        BackupSize = fileInfo.Length,
                        CreatedAt = fileInfo.CreationTime
                    });
                }

                backups.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
            }
            catch (Exception ex)
            {
                _logger.Error("Yedek listesi alınırken hata", ex);
            }
            return backups;
        }

        /// <summary>
        /// Eski yedekleri temizler
        /// </summary>
        private void CleanOldBackups(string backupDirectory)
        {
            try
            {
                string maxCountStr = _settingsRepository.GetValue("MaxBackupCount", "10");
                int maxCount = int.TryParse(maxCountStr, out int mc) ? mc : 10;

                var files = Directory.GetFiles(backupDirectory, "MiniMuhasebe_Backup_*.db");
                if (files.Length <= maxCount) return;

                Array.Sort(files);
                for (int i = 0; i < files.Length - maxCount; i++)
                {
                    File.Delete(files[i]);
                    _logger.Info($"Eski yedek silindi: {files[i]}");
                }
            }
            catch (Exception ex)
            {
                _logger.Warning("Eski yedek temizleme hatası", ex);
            }
        }

        private void SaveBackupRecord(Backup backup)
        {
            try
            {
                string query = @"INSERT INTO Backups (BackupFileName, BackupPath, BackupSize, CreatedAt)
                               VALUES (@BackupFileName, @BackupPath, @BackupSize, @CreatedAt)";
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@BackupFileName", backup.BackupFileName);
                        cmd.Parameters.AddWithValue("@BackupPath", backup.BackupPath);
                        cmd.Parameters.AddWithValue("@BackupSize", backup.BackupSize ?? 0);
                        cmd.Parameters.AddWithValue("@CreatedAt", backup.CreatedAt);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { /* Yedek kaydı başarısız olsa da yedekleme devam eder */ }
        }
    }
}
