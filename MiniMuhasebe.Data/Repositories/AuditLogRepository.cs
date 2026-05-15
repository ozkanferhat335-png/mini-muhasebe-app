using System;
using System.Collections.Generic;
using System.Data.SQLite;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Data.Repositories
{
    public class AuditLogRepository : BaseRepository<AuditLog>
    {
        public AuditLogRepository(string connectionString) : base(connectionString) { }

        public override AuditLog GetById(int id)
        {
            string query = "SELECT * FROM AuditLogs WHERE AuditLogId = @AuditLogId";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@AuditLogId", id)))
                {
                    if (reader.Read())
                        return MapAuditLog(reader);
                }
            }
            return null;
        }

        public override List<AuditLog> GetAll()
        {
            var logs = new List<AuditLog>();
            string query = "SELECT * FROM AuditLogs ORDER BY CreatedAt DESC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection))
                {
                    while (reader.Read())
                        logs.Add(MapAuditLog(reader));
                }
            }
            return logs;
        }

        public List<AuditLog> GetByUserId(int userId)
        {
            var logs = new List<AuditLog>();
            string query = "SELECT * FROM AuditLogs WHERE UserId = @UserId ORDER BY CreatedAt DESC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@UserId", userId)))
                {
                    while (reader.Read())
                        logs.Add(MapAuditLog(reader));
                }
            }
            return logs;
        }

        public List<AuditLog> GetByTableName(string tableName)
        {
            var logs = new List<AuditLog>();
            string query = "SELECT * FROM AuditLogs WHERE TableName = @TableName ORDER BY CreatedAt DESC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@TableName", tableName)))
                {
                    while (reader.Read())
                        logs.Add(MapAuditLog(reader));
                }
            }
            return logs;
        }

        public List<AuditLog> GetByDateRange(DateTime startDate, DateTime endDate)
        {
            var logs = new List<AuditLog>();
            string query = "SELECT * FROM AuditLogs WHERE CreatedAt >= @StartDate AND CreatedAt <= @EndDate ORDER BY CreatedAt DESC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection,
                    new SQLiteParameter("@StartDate", startDate),
                    new SQLiteParameter("@EndDate", endDate)))
                {
                    while (reader.Read())
                        logs.Add(MapAuditLog(reader));
                }
            }
            return logs;
        }

        public override int Add(AuditLog entity)
        {
            string query = @"INSERT INTO AuditLogs (UserId, Action, TableName, RecordId, OldValue, NewValue, IpAddress, CreatedAt)
                           VALUES (@UserId, @Action, @TableName, @RecordId, @OldValue, @NewValue, @IpAddress, @CreatedAt)";

            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@UserId", entity.UserId ?? (object)DBNull.Value),
                new SQLiteParameter("@Action", entity.Action),
                new SQLiteParameter("@TableName", entity.TableName),
                new SQLiteParameter("@RecordId", entity.RecordId ?? (object)DBNull.Value),
                new SQLiteParameter("@OldValue", entity.OldValue ?? string.Empty),
                new SQLiteParameter("@NewValue", entity.NewValue ?? string.Empty),
                new SQLiteParameter("@IpAddress", entity.IpAddress ?? string.Empty),
                new SQLiteParameter("@CreatedAt", DateTime.Now)
            };

            ExecuteNonQuery(query, parameters);
            return Convert.ToInt32(ExecuteScalar("SELECT last_insert_rowid()"));
        }

        public override bool Update(AuditLog entity)
        {
            // Audit log kayıtları değiştirilemez
            throw new InvalidOperationException("Audit log kayıtları güncellenemez.");
        }

        public override bool Delete(int id)
        {
            // Audit log kayıtları silinemez (güvenlik gereği)
            throw new InvalidOperationException("Audit log kayıtları silinemez.");
        }

        private AuditLog MapAuditLog(SQLiteDataReader reader)
        {
            return new AuditLog
            {
                AuditLogId = Convert.ToInt32(reader["AuditLogId"]),
                UserId = reader["UserId"] != DBNull.Value ? Convert.ToInt32(reader["UserId"]) : (int?)null,
                Action = reader["Action"].ToString(),
                TableName = reader["TableName"].ToString(),
                RecordId = reader["RecordId"] != DBNull.Value ? Convert.ToInt32(reader["RecordId"]) : (int?)null,
                OldValue = reader["OldValue"].ToString(),
                NewValue = reader["NewValue"].ToString(),
                IpAddress = reader["IpAddress"].ToString(),
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString())
            };
        }
    }
}
