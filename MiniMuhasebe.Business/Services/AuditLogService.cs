using System;
using System.Collections.Generic;
using MiniMuhasebe.Data;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Services
{
    /// <summary>
    /// Denetim günlüğü servisi
    /// </summary>
    public class AuditLogService
    {
        private readonly AuditLogRepository _auditLogRepository;
        private readonly Logger _logger;

        public AuditLogService(string connectionString)
        {
            _auditLogRepository = new AuditLogRepository(connectionString);
            _logger = new Logger();
        }

        /// <summary>
        /// Yeni audit log kaydı ekle
        /// </summary>
        public AuditLog Log(string action, string tableName, int? recordId = null,
            int? userId = null, string oldValue = null, string newValue = null, string ipAddress = null)
        {
            try
            {
                var log = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    TableName = tableName,
                    RecordId = recordId,
                    OldValue = oldValue,
                    NewValue = newValue,
                    IpAddress = ipAddress ?? "localhost",
                    CreatedAt = DateTime.Now
                };

                int logId = _auditLogRepository.Add(log);
                log.AuditLogId = logId;
                return log;
            }
            catch (Exception ex)
            {
                _logger.Error("Audit log kaydı eklenirken hata", ex);
                return null;
            }
        }

        /// <summary>
        /// Giriş işlemini logla
        /// </summary>
        public void LogLogin(int userId, string username, bool success)
        {
            string action = success ? "LOGIN_SUCCESS" : "LOGIN_FAILED";
            Log(action, "Users", userId, userId, null, $"Kullanıcı: {username}");
        }

        /// <summary>
        /// Çıkış işlemini logla
        /// </summary>
        public void LogLogout(int userId, string username)
        {
            Log("LOGOUT", "Users", userId, userId, null, $"Kullanıcı: {username}");
        }

        /// <summary>
        /// Kayıt ekleme işlemini logla
        /// </summary>
        public void LogInsert(string tableName, int recordId, int? userId, string newValue)
        {
            Log("INSERT", tableName, recordId, userId, null, newValue);
        }

        /// <summary>
        /// Kayıt güncelleme işlemini logla
        /// </summary>
        public void LogUpdate(string tableName, int recordId, int? userId, string oldValue, string newValue)
        {
            Log("UPDATE", tableName, recordId, userId, oldValue, newValue);
        }

        /// <summary>
        /// Kayıt silme işlemini logla
        /// </summary>
        public void LogDelete(string tableName, int recordId, int? userId, string oldValue)
        {
            Log("DELETE", tableName, recordId, userId, oldValue, null);
        }

        /// <summary>
        /// Tüm audit logları getir
        /// </summary>
        public List<AuditLog> GetAllLogs()
        {
            try
            {
                return _auditLogRepository.GetAll();
            }
            catch (Exception ex)
            {
                _logger.Error("Audit loglar alınırken hata", ex);
                return new List<AuditLog>();
            }
        }

        /// <summary>
        /// Kullanıcıya ait logları getir
        /// </summary>
        public List<AuditLog> GetLogsByUser(int userId)
        {
            try
            {
                return _auditLogRepository.GetByUserId(userId);
            }
            catch (Exception ex)
            {
                _logger.Error("Kullanıcı audit logları alınırken hata", ex);
                return new List<AuditLog>();
            }
        }

        /// <summary>
        /// Tabloya ait logları getir
        /// </summary>
        public List<AuditLog> GetLogsByTable(string tableName)
        {
            try
            {
                return _auditLogRepository.GetByTableName(tableName);
            }
            catch (Exception ex)
            {
                _logger.Error("Tablo audit logları alınırken hata", ex);
                return new List<AuditLog>();
            }
        }

        /// <summary>
        /// Tarih aralığına göre logları getir
        /// </summary>
        public List<AuditLog> GetLogsByDateRange(DateTime startDate, DateTime endDate)
        {
            try
            {
                return _auditLogRepository.GetByDateRange(startDate, endDate);
            }
            catch (Exception ex)
            {
                _logger.Error("Tarih aralığı audit logları alınırken hata", ex);
                return new List<AuditLog>();
            }
        }
    }
}
