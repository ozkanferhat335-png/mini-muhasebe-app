using System;
using System.Collections.Generic;
using System.Data.SQLite;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Data.Repositories
{
    public class TransactionMatchRepository : BaseRepository<TransactionMatch>
    {
        public TransactionMatchRepository(string connectionString) : base(connectionString) { }

        public override TransactionMatch GetById(int id)
        {
            string query = "SELECT * FROM TransactionMatches WHERE MatchId = @MatchId";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@MatchId", id)))
                {
                    if (reader.Read())
                        return MapMatch(reader);
                }
            }
            return null;
        }

        public override List<TransactionMatch> GetAll()
        {
            var matches = new List<TransactionMatch>();
            string query = "SELECT * FROM TransactionMatches ORDER BY CreatedAt DESC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection))
                {
                    while (reader.Read())
                        matches.Add(MapMatch(reader));
                }
            }
            return matches;
        }

        public TransactionMatch GetByBankTransactionId(int bankTransactionId)
        {
            string query = "SELECT * FROM TransactionMatches WHERE BankTransactionId = @BankTransactionId LIMIT 1";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@BankTransactionId", bankTransactionId)))
                {
                    if (reader.Read())
                        return MapMatch(reader);
                }
            }
            return null;
        }

        public List<TransactionMatch> GetByIncomeExpenseTransactionId(int incomeExpenseTransactionId)
        {
            var matches = new List<TransactionMatch>();
            string query = "SELECT * FROM TransactionMatches WHERE IncomeExpenseTransactionId = @IncomeExpenseTransactionId";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@IncomeExpenseTransactionId", incomeExpenseTransactionId)))
                {
                    while (reader.Read())
                        matches.Add(MapMatch(reader));
                }
            }
            return matches;
        }

        public bool MatchExists(int bankTransactionId, int incomeExpenseTransactionId)
        {
            string query = "SELECT COUNT(*) FROM TransactionMatches WHERE BankTransactionId = @BankTransactionId AND IncomeExpenseTransactionId = @IncomeExpenseTransactionId";
            var result = ExecuteScalar(query,
                new SQLiteParameter("@BankTransactionId", bankTransactionId),
                new SQLiteParameter("@IncomeExpenseTransactionId", incomeExpenseTransactionId));
            return Convert.ToInt32(result) > 0;
        }

        public override int Add(TransactionMatch entity)
        {
            string query = @"INSERT INTO TransactionMatches (BankTransactionId, IncomeExpenseTransactionId, MatchScore, MatchType, CreatedBy, CreatedAt)
                           VALUES (@BankTransactionId, @IncomeExpenseTransactionId, @MatchScore, @MatchType, @CreatedBy, @CreatedAt)";

            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@BankTransactionId", entity.BankTransactionId),
                new SQLiteParameter("@IncomeExpenseTransactionId", entity.IncomeExpenseTransactionId),
                new SQLiteParameter("@MatchScore", entity.MatchScore ?? (object)DBNull.Value),
                new SQLiteParameter("@MatchType", entity.MatchType ?? "Manual"),
                new SQLiteParameter("@CreatedBy", entity.CreatedBy ?? (object)DBNull.Value),
                new SQLiteParameter("@CreatedAt", DateTime.Now)
            };

            ExecuteNonQuery(query, parameters);
            return Convert.ToInt32(ExecuteScalar("SELECT last_insert_rowid()"));
        }

        public override bool Update(TransactionMatch entity)
        {
            string query = @"UPDATE TransactionMatches SET MatchScore = @MatchScore, MatchType = @MatchType WHERE MatchId = @MatchId";
            return ExecuteNonQuery(query,
                new SQLiteParameter("@MatchScore", entity.MatchScore ?? (object)DBNull.Value),
                new SQLiteParameter("@MatchType", entity.MatchType ?? "Manual"),
                new SQLiteParameter("@MatchId", entity.MatchId)) > 0;
        }

        public override bool Delete(int id)
        {
            string query = "DELETE FROM TransactionMatches WHERE MatchId = @MatchId";
            return ExecuteNonQuery(query, new SQLiteParameter("@MatchId", id)) > 0;
        }

        public bool DeleteByBankTransactionId(int bankTransactionId)
        {
            string query = "DELETE FROM TransactionMatches WHERE BankTransactionId = @BankTransactionId";
            return ExecuteNonQuery(query, new SQLiteParameter("@BankTransactionId", bankTransactionId)) > 0;
        }

        private TransactionMatch MapMatch(SQLiteDataReader reader)
        {
            return new TransactionMatch
            {
                MatchId = Convert.ToInt32(reader["MatchId"]),
                BankTransactionId = Convert.ToInt32(reader["BankTransactionId"]),
                IncomeExpenseTransactionId = Convert.ToInt32(reader["IncomeExpenseTransactionId"]),
                MatchScore = reader["MatchScore"] != DBNull.Value ? Convert.ToDecimal(reader["MatchScore"]) : (decimal?)null,
                MatchType = reader["MatchType"].ToString(),
                CreatedBy = reader["CreatedBy"] != DBNull.Value ? Convert.ToInt32(reader["CreatedBy"]) : (int?)null,
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString())
            };
        }
    }
}
