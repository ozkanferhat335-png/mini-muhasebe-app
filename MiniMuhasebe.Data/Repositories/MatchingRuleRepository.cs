using System;
using System.Collections.Generic;
using System.Data.SQLite;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Data.Repositories
{
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
                    if (reader.Read())
                        return MapRule(reader);
                }
            }
            return null;
        }

        public override List<MatchingRule> GetAll()
        {
            var rules = new List<MatchingRule>();
            string query = "SELECT * FROM MatchingRules WHERE IsActive = 1 ORDER BY Priority ASC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection))
                {
                    while (reader.Read())
                        rules.Add(MapRule(reader));
                }
            }
            return rules;
        }

        public List<MatchingRule> GetByCompanyId(int companyId)
        {
            var rules = new List<MatchingRule>();
            string query = "SELECT * FROM MatchingRules WHERE CompanyId = @CompanyId AND IsActive = 1 ORDER BY Priority ASC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@CompanyId", companyId)))
                {
                    while (reader.Read())
                        rules.Add(MapRule(reader));
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
                new SQLiteParameter("@KeywordPatterns", entity.KeywordPatterns ?? string.Empty),
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
                new SQLiteParameter("@KeywordPatterns", entity.KeywordPatterns ?? string.Empty),
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
