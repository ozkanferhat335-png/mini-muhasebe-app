using System;
using System.Collections.Generic;
using System.Data.SQLite;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Data.Repositories
{
    public class MatchingRepository : BaseRepository<TransactionMatch>
    {
        public MatchingRepository(string connectionString) : base(connectionString) { }

        public override TransactionMatch GetById(int id)
        {
            string query = "SELECT * FROM TransactionMatches WHERE MatchId = @MatchId";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@MatchId", id)))
                {
                    if (reader.Read()) return MapMatch(reader);
                }
            }
            return null;
        }

        public TransactionMatch GetByBankTransactionId(int bankTransactionId)
        {
            string query = "SELECT * FROM TransactionMatches WHERE BankTransactionId = @BankTransactionId";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@BankTransactionId", bankTransactionId)))
                {
                    if (reader.Read()) return MapMatch(reader);
                }
            }
            return null;
        }

        public List<TransactionMatch> GetByIncomeExpenseTransactionId(int transactionId)
        {
            var matches = new List<TransactionMatch>();
            string query = "SELECT * FROM TransactionMatches WHERE IncomeExpenseTransactionId = @TransactionId";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@TransactionId", transactionId)))
                {
                    while (reader.Read()) matches.Add(MapMatch(reader));
                }
            }
            return matches;
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
                    while (reader.Read()) matches.Add(MapMatch(reader));
                }
            }
            return matches;
        }

        public override int Add(TransactionMatch entity)
        {
            string query = @"INSERT OR IGNORE INTO TransactionMatches (BankTransactionId, IncomeExpenseTransactionId, MatchScore, MatchType, CreatedBy, CreatedAt)
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

        public override bool Update(TransactionMatch entity) => false;

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

    public class MatchingRuleRepository : BaseRepository<MatchingRule>
    {
        public MatchingRuleRepository(string connectionString) : base(connectionString) { }

        public override MatchingRule GetById(int id)
        {
            string query = "SELECT * FROM MatchingRules WHERE RuleId = @RuleId";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@RuleId", id)))
                {
                    if (reader.Read()) return MapRule(reader);
                }
            }
            return null;
        }

        public List<MatchingRule> GetByCompanyId(int companyId)
        {
            var rules = new List<MatchingRule>();
            string query = "SELECT * FROM MatchingRules WHERE CompanyId = @CompanyId AND IsActive = 1 ORDER BY Priority";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@CompanyId", companyId)))
                {
                    while (reader.Read()) rules.Add(MapRule(reader));
                }
            }
            return rules;
        }

        public override List<MatchingRule> GetAll()
        {
            var rules = new List<MatchingRule>();
            string query = "SELECT * FROM MatchingRules WHERE IsActive = 1 ORDER BY Priority";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection))
                {
                    while (reader.Read()) rules.Add(MapRule(reader));
                }
            }
            return rules;
        }

        public override int Add(MatchingRule entity)
        {
            string query = @"INSERT INTO MatchingRules (CompanyId, RuleName, AmountTolerance, DateTolerance, KeywordPatterns, IsActive, Priority, CreatedAt, UpdatedAt)
                           VALUES (@CompanyId, @RuleName, @AmountTolerance, @DateTolerance, @KeywordPatterns, @IsActive, @Priority, @CreatedAt, @UpdatedAt)";
            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@CompanyId", entity.CompanyId),
                new SQLiteParameter("@RuleName", entity.RuleName),
                new SQLiteParameter("@AmountTolerance", entity.AmountTolerance),
                new SQLiteParameter("@DateTolerance", entity.DateTolerance),
                new SQLiteParameter("@KeywordPatterns", entity.KeywordPatterns ?? "[]"),
                new SQLiteParameter("@IsActive", entity.IsActive ? 1 : 0),
                new SQLiteParameter("@Priority", entity.Priority),
                new SQLiteParameter("@CreatedAt", DateTime.Now),
                new SQLiteParameter("@UpdatedAt", DateTime.Now)
            };
            ExecuteNonQuery(query, parameters);
            return Convert.ToInt32(ExecuteScalar("SELECT last_insert_rowid()"));
        }

        public override bool Update(MatchingRule entity)
        {
            string query = @"UPDATE MatchingRules SET RuleName = @RuleName, AmountTolerance = @AmountTolerance, DateTolerance = @DateTolerance,
                           KeywordPatterns = @KeywordPatterns, IsActive = @IsActive, Priority = @Priority, UpdatedAt = @UpdatedAt
                           WHERE RuleId = @RuleId";
            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@RuleName", entity.RuleName),
                new SQLiteParameter("@AmountTolerance", entity.AmountTolerance),
                new SQLiteParameter("@DateTolerance", entity.DateTolerance),
                new SQLiteParameter("@KeywordPatterns", entity.KeywordPatterns ?? "[]"),
                new SQLiteParameter("@IsActive", entity.IsActive ? 1 : 0),
                new SQLiteParameter("@Priority", entity.Priority),
                new SQLiteParameter("@UpdatedAt", DateTime.Now),
                new SQLiteParameter("@RuleId", entity.RuleId)
            };
            return ExecuteNonQuery(query, parameters) > 0;
        }

        public override bool Delete(int id)
        {
            string query = "UPDATE MatchingRules SET IsActive = 0 WHERE RuleId = @RuleId";
            return ExecuteNonQuery(query, new SQLiteParameter("@RuleId", id)) > 0;
        }

        private MatchingRule MapRule(SQLiteDataReader reader)
        {
            return new MatchingRule
            {
                RuleId = Convert.ToInt32(reader["RuleId"]),
                CompanyId = Convert.ToInt32(reader["CompanyId"]),
                RuleName = reader["RuleName"].ToString(),
                AmountTolerance = Convert.ToDecimal(reader["AmountTolerance"]),
                DateTolerance = Convert.ToInt32(reader["DateTolerance"]),
                KeywordPatterns = reader["KeywordPatterns"].ToString(),
                IsActive = Convert.ToInt32(reader["IsActive"]) == 1,
                Priority = Convert.ToInt32(reader["Priority"]),
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString()),
                UpdatedAt = DateTime.Parse(reader["UpdatedAt"].ToString())
            };
        }
    }
}
