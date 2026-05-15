using System;
using System.Collections.Generic;
using MiniMuhasebe.Data;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Services
{
    public class AuditLogService
    {
        private readonly AuditLogRepository _auditLogRepository;
        private readonly Logger _logger;

        public AuditLogService(string connectionString)
        {
            _auditLogRepository = new AuditLogRepository(connectionString);
            _logger = new Logger();
        }

        public void Log(string action, string tableName, int? userId = null, int? recordId = null,
            string oldValue = null, string newValue = null)
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
                    IpAddress = "localhost",
                    CreatedAt = DateTime.Now
                };
                _auditLogRepository.Add(log);
            }
            catch (Exception ex)
            {
                _logger.Error("Audit log yazma hatası", ex);
            }
        }

        public void LogLogin(int userId, string username)
        {
            Log("LOGIN", "Users", userId, userId, null, $"Kullanıcı giriş yaptı: {username}");
        }

        public void LogLogout(int userId, string username)
        {
            Log("LOGOUT", "Users", userId, userId, null, $"Kullanıcı çıkış yaptı: {username}");
        }

        public void LogCreate(string tableName, int? userId, int recordId, string description)
        {
            Log("CREATE", tableName, userId, recordId, null, description);
        }

        public void LogUpdate(string tableName, int? userId, int recordId, string oldValue, string newValue)
        {
            Log("UPDATE", tableName, userId, recordId, oldValue, newValue);
        }

        public void LogDelete(string tableName, int? userId, int recordId, string description)
        {
            Log("DELETE", tableName, userId, recordId, description, null);
        }

        public List<AuditLog> GetRecentLogs(int count = 100)
        {
            try
            {
                var all = _auditLogRepository.GetAll();
                return all.Count > count ? all.GetRange(0, count) : all;
            }
            catch (Exception ex) { _logger.Error("Log alınırken hata", ex); return new List<AuditLog>(); }
        }

        public List<AuditLog> GetLogsByDateRange(DateTime startDate, DateTime endDate)
        {
            try { return _auditLogRepository.GetByDateRange(startDate, endDate); }
            catch (Exception ex) { _logger.Error("Log alınırken hata", ex); return new List<AuditLog>(); }
        }

        public List<AuditLog> GetLogsByUser(int userId)
        {
            try { return _auditLogRepository.GetByUserId(userId); }
            catch (Exception ex) { _logger.Error("Log alınırken hata", ex); return new List<AuditLog>(); }
        }
    }
}
