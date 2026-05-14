using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace MiniMuhasebe.Data
{
    /// <summary>
    /// Temel repository arayüzü
    /// </summary>
    public interface IRepository<T> where T : class
    {
        T GetById(int id);
        List<T> GetAll();
        int Add(T entity);
        bool Update(T entity);
        bool Delete(int id);
    }

    /// <summary>
    /// Temel repository implementasyonu
    /// </summary>
    public abstract class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly string _connectionString;
        protected readonly Logger _logger;

        protected BaseRepository(string connectionString)
        {
            _connectionString = connectionString;
            _logger = new Logger();
        }

        public abstract T GetById(int id);
        public abstract List<T> GetAll();
        public abstract int Add(T entity);
        public abstract bool Update(T entity);
        public abstract bool Delete(int id);

        /// <summary>
        /// Güvenli SQL sorgusu yürütme (parametreli)
        /// </summary>
        protected object ExecuteScalar(string query, params SQLiteParameter[] parameters)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    return command.ExecuteScalar();
                }
            }
        }

        /// <summary>
        /// Güvenli SQL sorgusu yürütme (non-query)
        /// </summary>
        protected int ExecuteNonQuery(string query, params SQLiteParameter[] parameters)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    return command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Güvenli SQL sorgusu yürütme (reader)
        /// </summary>
        protected SQLiteDataReader ExecuteReader(string query, SQLiteConnection connection, params SQLiteParameter[] parameters)
        {
            using (var command = new SQLiteCommand(query, connection))
            {
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }
                return command.ExecuteReader();
            }
        }
    }
}
