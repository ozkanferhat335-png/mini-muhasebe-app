using System;
using System.Collections.Generic;
using System.Data.SQLite;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Data.Repositories
{
    public class AccountRepository : BaseRepository<Account>
    {
        public AccountRepository(string connectionString) : base(connectionString) { }

        public override Account GetById(int id)
        {
            string query = "SELECT * FROM Accounts WHERE AccountId = @AccountId";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@AccountId", id)))
                {
                    if (reader.Read())
                        return MapAccount(reader);
                }
            }
            return null;
        }

        public override List<Account> GetAll()
        {
            var accounts = new List<Account>();
            string query = "SELECT * FROM Accounts WHERE IsActive = 1 ORDER BY AccountCode, AccountName";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection))
                {
                    while (reader.Read())
                        accounts.Add(MapAccount(reader));
                }
            }
            return accounts;
        }

        public List<Account> GetByCompanyId(int companyId)
        {
            var accounts = new List<Account>();
            string query = "SELECT * FROM Accounts WHERE CompanyId = @CompanyId AND IsActive = 1 ORDER BY AccountCode, AccountName";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@CompanyId", companyId)))
                {
                    while (reader.Read())
                        accounts.Add(MapAccount(reader));
                }
            }
            return accounts;
        }

        public List<Account> GetByCompanyIdAndType(int companyId, string accountType)
        {
            var accounts = new List<Account>();
            string query = "SELECT * FROM Accounts WHERE CompanyId = @CompanyId AND AccountType = @AccountType AND IsActive = 1 ORDER BY AccountCode, AccountName";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection,
                    new SQLiteParameter("@CompanyId", companyId),
                    new SQLiteParameter("@AccountType", accountType)))
                {
                    while (reader.Read())
                        accounts.Add(MapAccount(reader));
                }
            }
            return accounts;
        }

        public override int Add(Account entity)
        {
            string query = @"INSERT INTO Accounts (CompanyId, AccountName, AccountType, AccountCode, Description, IsActive, ParentAccountId, CreatedAt, UpdatedAt)
                           VALUES (@CompanyId, @AccountName, @AccountType, @AccountCode, @Description, @IsActive, @ParentAccountId, @CreatedAt, @UpdatedAt)";

            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@CompanyId", entity.CompanyId),
                new SQLiteParameter("@AccountName", entity.AccountName),
                new SQLiteParameter("@AccountType", entity.AccountType),
                new SQLiteParameter("@AccountCode", entity.AccountCode ?? string.Empty),
                new SQLiteParameter("@Description", entity.Description ?? string.Empty),
                new SQLiteParameter("@IsActive", entity.IsActive ? 1 : 0),
                new SQLiteParameter("@ParentAccountId", entity.ParentAccountId ?? (object)DBNull.Value),
                new SQLiteParameter("@CreatedAt", DateTime.Now),
                new SQLiteParameter("@UpdatedAt", DateTime.Now)
            };

            ExecuteNonQuery(query, parameters);
            return Convert.ToInt32(ExecuteScalar("SELECT last_insert_rowid()"));
        }

        public override bool Update(Account entity)
        {
            string query = @"UPDATE Accounts SET AccountName = @AccountName, AccountType = @AccountType, AccountCode = @AccountCode, 
                           Description = @Description, IsActive = @IsActive, ParentAccountId = @ParentAccountId, UpdatedAt = @UpdatedAt 
                           WHERE AccountId = @AccountId";

            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@AccountName", entity.AccountName),
                new SQLiteParameter("@AccountType", entity.AccountType),
                new SQLiteParameter("@AccountCode", entity.AccountCode ?? string.Empty),
                new SQLiteParameter("@Description", entity.Description ?? string.Empty),
                new SQLiteParameter("@IsActive", entity.IsActive ? 1 : 0),
                new SQLiteParameter("@ParentAccountId", entity.ParentAccountId ?? (object)DBNull.Value),
                new SQLiteParameter("@UpdatedAt", DateTime.Now),
                new SQLiteParameter("@AccountId", entity.AccountId)
            };

            return ExecuteNonQuery(query, parameters) > 0;
        }

        public override bool Delete(int id)
        {
            string query = "UPDATE Accounts SET IsActive = 0 WHERE AccountId = @AccountId";
            return ExecuteNonQuery(query, new SQLiteParameter("@AccountId", id)) > 0;
        }

        private Account MapAccount(SQLiteDataReader reader)
        {
            return new Account
            {
                AccountId = Convert.ToInt32(reader["AccountId"]),
                CompanyId = Convert.ToInt32(reader["CompanyId"]),
                AccountName = reader["AccountName"].ToString(),
                AccountType = reader["AccountType"].ToString(),
                AccountCode = reader["AccountCode"].ToString(),
                Description = reader["Description"].ToString(),
                IsActive = Convert.ToInt32(reader["IsActive"]) == 1,
                ParentAccountId = reader["ParentAccountId"] != DBNull.Value ? Convert.ToInt32(reader["ParentAccountId"]) : (int?)null,
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString()),
                UpdatedAt = DateTime.Parse(reader["UpdatedAt"].ToString())
            };
        }
    }
}
