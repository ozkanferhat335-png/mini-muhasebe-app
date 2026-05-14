using System;
using System.Collections.Generic;
using System.Data.SQLite;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Data.Repositories
{
    public class CurrentAccountRepository : BaseRepository<CurrentAccount>
    {
        public CurrentAccountRepository(string connectionString) : base(connectionString) { }

        public override CurrentAccount GetById(int id)
        {
            string query = "SELECT * FROM CurrentAccounts WHERE CurrentAccountId = @CurrentAccountId";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@CurrentAccountId", id)))
                {
                    if (reader.Read())
                    {
                        return MapCurrentAccount(reader);
                    }
                }
            }
            return null;
        }

        public List<CurrentAccount> GetByCompanyId(int companyId)
        {
            var accounts = new List<CurrentAccount>();
            string query = "SELECT * FROM CurrentAccounts WHERE CompanyId = @CompanyId AND IsActive = 1 ORDER BY Title";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@CompanyId", companyId)))
                {
                    while (reader.Read())
                    {
                        accounts.Add(MapCurrentAccount(reader));
                    }
                }
            }
            return accounts;
        }

        public List<CurrentAccount> GetByCompanyIdAndType(int companyId, string accountType)
        {
            var accounts = new List<CurrentAccount>();
            string query = "SELECT * FROM CurrentAccounts WHERE CompanyId = @CompanyId AND AccountType = @AccountType AND IsActive = 1 ORDER BY Title";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, 
                    new SQLiteParameter("@CompanyId", companyId),
                    new SQLiteParameter("@AccountType", accountType)))
                {
                    while (reader.Read())
                    {
                        accounts.Add(MapCurrentAccount(reader));
                    }
                }
            }
            return accounts;
        }

        public override List<CurrentAccount> GetAll()
        {
            var accounts = new List<CurrentAccount>();
            string query = "SELECT * FROM CurrentAccounts WHERE IsActive = 1 ORDER BY Title";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection))
                {
                    while (reader.Read())
                    {
                        accounts.Add(MapCurrentAccount(reader));
                    }
                }
            }
            return accounts;
        }

        public override int Add(CurrentAccount entity)
        {
            string query = @"INSERT INTO CurrentAccounts (CompanyId, Title, AccountType, TaxNumber, TaxId, Phone, Email, Address, Notes, IsActive, CreatedAt, UpdatedAt) 
                           VALUES (@CompanyId, @Title, @AccountType, @TaxNumber, @TaxId, @Phone, @Email, @Address, @Notes, @IsActive, @CreatedAt, @UpdatedAt)";
            
            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@CompanyId", entity.CompanyId),
                new SQLiteParameter("@Title", entity.Title),
                new SQLiteParameter("@AccountType", entity.AccountType),
                new SQLiteParameter("@TaxNumber", entity.TaxNumber ?? string.Empty),
                new SQLiteParameter("@TaxId", entity.TaxId ?? string.Empty),
                new SQLiteParameter("@Phone", entity.Phone ?? string.Empty),
                new SQLiteParameter("@Email", entity.Email ?? string.Empty),
                new SQLiteParameter("@Address", entity.Address ?? string.Empty),
                new SQLiteParameter("@Notes", entity.Notes ?? string.Empty),
                new SQLiteParameter("@IsActive", entity.IsActive ? 1 : 0),
                new SQLiteParameter("@CreatedAt", DateTime.Now),
                new SQLiteParameter("@UpdatedAt", DateTime.Now)
            };

            ExecuteNonQuery(query, parameters);
            return Convert.ToInt32(ExecuteScalar("SELECT last_insert_rowid()"));
        }

        public override bool Update(CurrentAccount entity)
        {
            string query = @"UPDATE CurrentAccounts SET Title = @Title, AccountType = @AccountType, TaxNumber = @TaxNumber, TaxId = @TaxId, 
                           Phone = @Phone, Email = @Email, Address = @Address, Notes = @Notes, IsActive = @IsActive, UpdatedAt = @UpdatedAt 
                           WHERE CurrentAccountId = @CurrentAccountId";
            
            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@Title", entity.Title),
                new SQLiteParameter("@AccountType", entity.AccountType),
                new SQLiteParameter("@TaxNumber", entity.TaxNumber ?? string.Empty),
                new SQLiteParameter("@TaxId", entity.TaxId ?? string.Empty),
                new SQLiteParameter("@Phone", entity.Phone ?? string.Empty),
                new SQLiteParameter("@Email", entity.Email ?? string.Empty),
                new SQLiteParameter("@Address", entity.Address ?? string.Empty),
                new SQLiteParameter("@Notes", entity.Notes ?? string.Empty),
                new SQLiteParameter("@IsActive", entity.IsActive ? 1 : 0),
                new SQLiteParameter("@UpdatedAt", DateTime.Now),
                new SQLiteParameter("@CurrentAccountId", entity.CurrentAccountId)
            };

            return ExecuteNonQuery(query, parameters) > 0;
        }

        public override bool Delete(int id)
        {
            string query = "UPDATE CurrentAccounts SET IsActive = 0 WHERE CurrentAccountId = @CurrentAccountId";
            return ExecuteNonQuery(query, new SQLiteParameter("@CurrentAccountId", id)) > 0;
        }

        private CurrentAccount MapCurrentAccount(SQLiteDataReader reader)
        {
            return new CurrentAccount
            {
                CurrentAccountId = Convert.ToInt32(reader["CurrentAccountId"]),
                CompanyId = Convert.ToInt32(reader["CompanyId"]),
                Title = reader["Title"].ToString(),
                AccountType = reader["AccountType"].ToString(),
                TaxNumber = reader["TaxNumber"].ToString(),
                TaxId = reader["TaxId"].ToString(),
                Phone = reader["Phone"].ToString(),
                Email = reader["Email"].ToString(),
                Address = reader["Address"].ToString(),
                Notes = reader["Notes"].ToString(),
                IsActive = Convert.ToInt32(reader["IsActive"]) == 1,
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString()),
                UpdatedAt = DateTime.Parse(reader["UpdatedAt"].ToString())
            };
        }
    }
}
