using System;
using System.Collections.Generic;
using System.Data.SQLite;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Data.Repositories
{
    public class BackupRepository : BaseRepository<Backup>
    {
        public BackupRepository(string connectionString) : base(connectionString) { }

        public override Backup GetById(int id)
        {
            string query = "SELECT * FROM Backups WHERE BackupId = @BackupId";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@BackupId", id)))
                {
                    if (reader.Read())
                        return MapBackup(reader);
                }
            }
            return null;
        }

        public override List<Backup> GetAll()
        {
            var backups = new List<Backup>();
            string query = "SELECT * FROM Backups ORDER BY CreatedAt DESC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection))
                {
                    while (reader.Read())
                        backups.Add(MapBackup(reader));
                }
            }
            return backups;
        }

        public Backup GetLatest()
        {
            string query = "SELECT * FROM Backups ORDER BY CreatedAt DESC LIMIT 1";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection))
                {
                    if (reader.Read())
                        return MapBackup(reader);
                }
            }
            return null;
        }

        public override int Add(Backup entity)
        {
            string query = @"INSERT INTO Backups (BackupFileName, BackupPath, BackupSize, CreatedAt)
                           VALUES (@BackupFileName, @BackupPath, @BackupSize, @CreatedAt)";

            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@BackupFileName", entity.BackupFileName),
                new SQLiteParameter("@BackupPath", entity.BackupPath),
                new SQLiteParameter("@BackupSize", entity.BackupSize ?? (object)DBNull.Value),
                new SQLiteParameter("@CreatedAt", DateTime.Now)
            };

            ExecuteNonQuery(query, parameters);
            return Convert.ToInt32(ExecuteScalar("SELECT last_insert_rowid()"));
        }

        public override bool Update(Backup entity)
        {
            string query = "UPDATE Backups SET RestoredAt = @RestoredAt WHERE BackupId = @BackupId";
            return ExecuteNonQuery(query,
                new SQLiteParameter("@RestoredAt", entity.RestoredAt ?? (object)DBNull.Value),
                new SQLiteParameter("@BackupId", entity.BackupId)) > 0;
        }

        public bool MarkAsRestored(int backupId)
        {
            string query = "UPDATE Backups SET RestoredAt = @RestoredAt WHERE BackupId = @BackupId";
            return ExecuteNonQuery(query,
                new SQLiteParameter("@RestoredAt", DateTime.Now),
                new SQLiteParameter("@BackupId", backupId)) > 0;
        }

        public override bool Delete(int id)
        {
            string query = "DELETE FROM Backups WHERE BackupId = @BackupId";
            return ExecuteNonQuery(query, new SQLiteParameter("@BackupId", id)) > 0;
        }

        private Backup MapBackup(SQLiteDataReader reader)
        {
            return new Backup
            {
                BackupId = Convert.ToInt32(reader["BackupId"]),
                BackupFileName = reader["BackupFileName"].ToString(),
                BackupPath = reader["BackupPath"].ToString(),
                BackupSize = reader["BackupSize"] != DBNull.Value ? Convert.ToInt64(reader["BackupSize"]) : (long?)null,
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString()),
                RestoredAt = reader["RestoredAt"] != DBNull.Value ? DateTime.Parse(reader["RestoredAt"].ToString()) : (DateTime?)null
            };
        }
    }
}
