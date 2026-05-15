using System;
using System.Collections.Generic;
using System.IO;
using MiniMuhasebe.Data;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Services
{
    /// <summary>
    /// Veritabanı yedekleme ve geri yükleme servisi
    /// </summary>
    public class BackupService
    {
        private readonly BackupRepository _backupRepository;
        private readonly string _databasePath;
        private readonly string _backupDirectory;
        private readonly Logger _logger;

        public BackupService(string connectionString, string backupDirectory = "DatabaseBackups")
        {
            _backupRepository = new BackupRepository(connectionString);
            _backupDirectory = backupDirectory;
            _logger = new Logger();

            // Bağlantı stringinden veritabanı yolunu çıkar
            try
            {
                var builder = new System.Data.SQLite.SQLiteConnectionStringBuilder(connectionString);
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
        /// Veritabanı yedeği al
        /// </summary>
        public Backup CreateBackup(string customPath = null)
        {
            try
            {
                if (!File.Exists(_databasePath))
                    throw new FileNotFoundException($"Veritabanı dosyası bulunamadı: {_databasePath}");

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string fileName = $"MiniMuhasebe_{timestamp}.db";
                string targetDir = customPath ?? _backupDirectory;
                string targetPath = Path.Combine(targetDir, fileName);

                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                File.Copy(_databasePath, targetPath, overwrite: false);

                var fileInfo = new FileInfo(targetPath);
                var backup = new Backup
                {
                    BackupFileName = fileName,
                    BackupPath = targetPath,
                    BackupSize = fileInfo.Length,
                    CreatedAt = DateTime.Now
                };

                int backupId = _backupRepository.Add(backup);
                backup.BackupId = backupId;

                _logger.Info($"Yedek alındı: {fileName} ({fileInfo.Length} byte)");

                // Eski yedekleri temizle (max 10 yedek)
                CleanOldBackups(10);

                return backup;
            }
            catch (Exception ex)
            {
                _logger.Error("Yedek alma sırasında hata", ex);
                return null;
            }
        }

        /// <summary>
        /// Yedekten geri yükle
        /// </summary>
        public bool RestoreBackup(int backupId)
        {
            try
            {
                var backup = _backupRepository.GetById(backupId);
                if (backup == null)
                    throw new ArgumentException($"Yedek bulunamadı: ID {backupId}");

                if (!File.Exists(backup.BackupPath))
                    throw new FileNotFoundException($"Yedek dosyası bulunamadı: {backup.BackupPath}");

                // Mevcut veritabanını geçici olarak yedekle
                string tempBackup = _databasePath + ".restore_temp";
                if (File.Exists(_databasePath))
                    File.Copy(_databasePath, tempBackup, overwrite: true);

                try
                {
                    File.Copy(backup.BackupPath, _databasePath, overwrite: true);
                    _backupRepository.MarkAsRestored(backupId);

                    // Geçici yedeği sil
                    if (File.Exists(tempBackup))
                        File.Delete(tempBackup);

                    _logger.Info($"Yedekten geri yüklendi: {backup.BackupFileName}");
                    return true;
                }
                catch
                {
                    // Geri yükleme başarısız, orijinal veritabanını geri getir
                    if (File.Exists(tempBackup))
                        File.Copy(tempBackup, _databasePath, overwrite: true);
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
        /// Yedekten geri yükle (dosya yolu ile)
        /// </summary>
        public bool RestoreBackupFromFile(string backupFilePath)
        {
            try
            {
                if (!File.Exists(backupFilePath))
                    throw new FileNotFoundException($"Yedek dosyası bulunamadı: {backupFilePath}");

                string tempBackup = _databasePath + ".restore_temp";
                if (File.Exists(_databasePath))
                    File.Copy(_databasePath, tempBackup, overwrite: true);

                try
                {
                    File.Copy(backupFilePath, _databasePath, overwrite: true);

                    if (File.Exists(tempBackup))
                        File.Delete(tempBackup);

                    _logger.Info($"Dosyadan geri yüklendi: {backupFilePath}");
                    return true;
                }
                catch
                {
                    if (File.Exists(tempBackup))
                        File.Copy(tempBackup, _databasePath, overwrite: true);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Dosyadan geri yükleme sırasında hata", ex);
                return false;
            }
        }

        /// <summary>
        /// Tüm yedekleri listele
        /// </summary>
        public List<Backup> GetAllBackups()
        {
            try
            {
                return _backupRepository.GetAll();
            }
            catch (Exception ex)
            {
                _logger.Error("Yedekler listelenirken hata", ex);
                return new List<Backup>();
            }
        }

        /// <summary>
        /// En son yedeği getir
        /// </summary>
        public Backup GetLatestBackup()
        {
            try
            {
                return _backupRepository.GetLatest();
            }
            catch (Exception ex)
            {
                _logger.Error("Son yedek alınırken hata", ex);
                return null;
            }
        }

        /// <summary>
        /// Yedek kaydını sil (dosyayı da sil)
        /// </summary>
        public bool DeleteBackup(int backupId, bool deleteFile = true)
        {
            try
            {
                var backup = _backupRepository.GetById(backupId);
                if (backup == null) return false;

                if (deleteFile && File.Exists(backup.BackupPath))
                    File.Delete(backup.BackupPath);

                bool success = _backupRepository.Delete(backupId);
                if (success)
                    _logger.Info($"Yedek silindi: {backup.BackupFileName}");
                return success;
            }
            catch (Exception ex)
            {
                _logger.Error("Yedek silme sırasında hata", ex);
                return false;
            }
        }

        /// <summary>
        /// Eski yedekleri temizle (maxCount'tan fazlasını sil)
        /// </summary>
        private void CleanOldBackups(int maxCount)
        {
            try
            {
                var backups = _backupRepository.GetAll();
                if (backups.Count > maxCount)
                {
                    // En eski yedekleri sil
                    for (int i = maxCount; i < backups.Count; i++)
                    {
                        DeleteBackup(backups[i].BackupId, deleteFile: true);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning("Eski yedekler temizlenirken hata", ex);
            }
        }
    }
}
