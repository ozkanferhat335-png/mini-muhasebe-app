using System;
using System.Collections.Generic;
using System.Data.SQLite;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Data.Repositories
{
    public class CompanyRepository : BaseRepository<Company>
    {
        public CompanyRepository(string connectionString) : base(connectionString) { }

        public override Company GetById(int id)
        {
            string query = "SELECT * FROM Companies WHERE CompanyId = @CompanyId";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@CompanyId", id)))
                {
                    if (reader.Read())
                    {
                        return MapCompany(reader);
                    }
                }
            }
            return null;
        }

        public override List<Company> GetAll()
        {
            var companies = new List<Company>();
            string query = "SELECT * FROM Companies WHERE IsActive = 1 ORDER BY CompanyName";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection))
                {
                    while (reader.Read())
                    {
                        companies.Add(MapCompany(reader));
                    }
                }
            }
            return companies;
        }

        public override int Add(Company entity)
        {
            string query = @"INSERT INTO Companies (CompanyName, TaxOffice, TaxNumber, Phone, Email, Address, IsActive, CreatedAt, UpdatedAt) 
                           VALUES (@CompanyName, @TaxOffice, @TaxNumber, @Phone, @Email, @Address, @IsActive, @CreatedAt, @UpdatedAt)";
            
            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@CompanyName", entity.CompanyName),
                new SQLiteParameter("@TaxOffice", entity.TaxOffice ?? string.Empty),
                new SQLiteParameter("@TaxNumber", entity.TaxNumber ?? string.Empty),
                new SQLiteParameter("@Phone", entity.Phone ?? string.Empty),
                new SQLiteParameter("@Email", entity.Email ?? string.Empty),
                new SQLiteParameter("@Address", entity.Address ?? string.Empty),
                new SQLiteParameter("@IsActive", entity.IsActive ? 1 : 0),
                new SQLiteParameter("@CreatedAt", DateTime.Now),
                new SQLiteParameter("@UpdatedAt", DateTime.Now)
            };

            ExecuteNonQuery(query, parameters);
            return Convert.ToInt32(ExecuteScalar("SELECT last_insert_rowid()"));
        }

        public override bool Update(Company entity)
        {
            string query = @"UPDATE Companies SET CompanyName = @CompanyName, TaxOffice = @TaxOffice, TaxNumber = @TaxNumber, 
                           Phone = @Phone, Email = @Email, Address = @Address, IsActive = @IsActive, UpdatedAt = @UpdatedAt 
                           WHERE CompanyId = @CompanyId";
            
            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@CompanyName", entity.CompanyName),
                new SQLiteParameter("@TaxOffice", entity.TaxOffice ?? string.Empty),
                new SQLiteParameter("@TaxNumber", entity.TaxNumber ?? string.Empty),
                new SQLiteParameter("@Phone", entity.Phone ?? string.Empty),
                new SQLiteParameter("@Email", entity.Email ?? string.Empty),
                new SQLiteParameter("@Address", entity.Address ?? string.Empty),
                new SQLiteParameter("@IsActive", entity.IsActive ? 1 : 0),
                new SQLiteParameter("@UpdatedAt", DateTime.Now),
                new SQLiteParameter("@CompanyId", entity.CompanyId)
            };

            return ExecuteNonQuery(query, parameters) > 0;
        }

        public override bool Delete(int id)
        {
            string query = "UPDATE Companies SET IsActive = 0 WHERE CompanyId = @CompanyId";
            return ExecuteNonQuery(query, new SQLiteParameter("@CompanyId", id)) > 0;
        }

        private Company MapCompany(SQLiteDataReader reader)
        {
            return new Company
            {
                CompanyId = Convert.ToInt32(reader["CompanyId"]),
                CompanyName = reader["CompanyName"].ToString(),
                TaxOffice = reader["TaxOffice"].ToString(),
                TaxNumber = reader["TaxNumber"].ToString(),
                Phone = reader["Phone"].ToString(),
                Email = reader["Email"].ToString(),
                Address = reader["Address"].ToString(),
                IsActive = Convert.ToInt32(reader["IsActive"]) == 1,
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString()),
                UpdatedAt = DateTime.Parse(reader["UpdatedAt"].ToString())
            };
        }
    }
}
