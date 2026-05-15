using System;
using System.Collections.Generic;
using System.Data.SQLite;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Data.Repositories
{
    public class AppSettingsRepository : BaseRepository<AppSetting>
    {
        public AppSettingsRepository(string connectionString) : base(connectionString) { }

        public override AppSetting GetById(int id)
        {
            string query = "SELECT * FROM AppSettings WHERE SettingId = @SettingId";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@SettingId", id)))
                {
                    if (reader.Read())
                        return MapSetting(reader);
                }
            }
            return null;
        }

        public AppSetting GetByKey(string key)
        {
            string query = "SELECT * FROM AppSettings WHERE SettingKey = @SettingKey";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@SettingKey", key)))
                {
                    if (reader.Read())
                        return MapSetting(reader);
                }
            }
            return null;
        }

        public string GetValue(string key, string defaultValue = null)
        {
            var setting = GetByKey(key);
            return setting?.SettingValue ?? defaultValue;
        }

        public bool SetValue(string key, string value)
        {
            var existing = GetByKey(key);
            if (existing != null)
            {
                existing.SettingValue = value;
                return Update(existing);
            }
            return false;
        }

        public override List<AppSetting> GetAll()
        {
            var settings = new List<AppSetting>();
            string query = "SELECT * FROM AppSettings ORDER BY SettingKey";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection))
                {
                    while (reader.Read())
                        settings.Add(MapSetting(reader));
                }
            }
            return settings;
        }

        public override int Add(AppSetting entity)
        {
            string query = @"INSERT INTO AppSettings (SettingKey, SettingValue, SettingType, Description, UpdatedAt)
                           VALUES (@SettingKey, @SettingValue, @SettingType, @Description, @UpdatedAt)";

            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@SettingKey", entity.SettingKey),
                new SQLiteParameter("@SettingValue", entity.SettingValue ?? string.Empty),
                new SQLiteParameter("@SettingType", entity.SettingType ?? "String"),
                new SQLiteParameter("@Description", entity.Description ?? string.Empty),
                new SQLiteParameter("@UpdatedAt", DateTime.Now)
            };

            ExecuteNonQuery(query, parameters);
            return Convert.ToInt32(ExecuteScalar("SELECT last_insert_rowid()"));
        }

        public override bool Update(AppSetting entity)
        {
            string query = @"UPDATE AppSettings SET SettingValue = @SettingValue, SettingType = @SettingType, 
                           Description = @Description, UpdatedAt = @UpdatedAt WHERE SettingKey = @SettingKey";

            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@SettingValue", entity.SettingValue ?? string.Empty),
                new SQLiteParameter("@SettingType", entity.SettingType ?? "String"),
                new SQLiteParameter("@Description", entity.Description ?? string.Empty),
                new SQLiteParameter("@UpdatedAt", DateTime.Now),
                new SQLiteParameter("@SettingKey", entity.SettingKey)
            };

            return ExecuteNonQuery(query, parameters) > 0;
        }

        public override bool Delete(int id)
        {
            string query = "DELETE FROM AppSettings WHERE SettingId = @SettingId";
            return ExecuteNonQuery(query, new SQLiteParameter("@SettingId", id)) > 0;
        }

        private AppSetting MapSetting(SQLiteDataReader reader)
        {
            return new AppSetting
            {
                SettingId = Convert.ToInt32(reader["SettingId"]),
                SettingKey = reader["SettingKey"].ToString(),
                SettingValue = reader["SettingValue"].ToString(),
                SettingType = reader["SettingType"].ToString(),
                Description = reader["Description"].ToString(),
                UpdatedAt = DateTime.Parse(reader["UpdatedAt"].ToString())
            };
        }
    }
}
