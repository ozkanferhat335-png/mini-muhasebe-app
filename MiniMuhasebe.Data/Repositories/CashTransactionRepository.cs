using System;
using System.Collections.Generic;
using System.Data.SQLite;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Data.Repositories
{
    public class CashTransactionRepository : BaseRepository<CashTransaction>
    {
        public CashTransactionRepository(string connectionString) : base(connectionString) { }

        public override CashTransaction GetById(int id)
        {
            string query = "SELECT * FROM CashTransactions WHERE CashTransactionId = @CashTransactionId";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@CashTransactionId", id)))
                {
                    if (reader.Read())
                        return MapCashTransaction(reader);
                }
            }
            return null;
        }

        public override List<CashTransaction> GetAll()
        {
            var transactions = new List<CashTransaction>();
            string query = "SELECT * FROM CashTransactions ORDER BY TransactionDate DESC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection))
                {
                    while (reader.Read())
                        transactions.Add(MapCashTransaction(reader));
                }
            }
            return transactions;
        }

        public List<CashTransaction> GetByCompanyAndPeriod(int companyId, int periodId)
        {
            var transactions = new List<CashTransaction>();
            string query = "SELECT * FROM CashTransactions WHERE CompanyId = @CompanyId AND PeriodId = @PeriodId ORDER BY TransactionDate DESC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection,
                    new SQLiteParameter("@CompanyId", companyId),
                    new SQLiteParameter("@PeriodId", periodId)))
                {
                    while (reader.Read())
                        transactions.Add(MapCashTransaction(reader));
                }
            }
            return transactions;
        }

        public List<CashTransaction> GetByCompanyAndDateRange(int companyId, DateTime startDate, DateTime endDate)
        {
            var transactions = new List<CashTransaction>();
            string query = @"SELECT * FROM CashTransactions 
                           WHERE CompanyId = @CompanyId AND TransactionDate >= @StartDate AND TransactionDate <= @EndDate 
                           ORDER BY TransactionDate DESC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection,
                    new SQLiteParameter("@CompanyId", companyId),
                    new SQLiteParameter("@StartDate", startDate),
                    new SQLiteParameter("@EndDate", endDate)))
                {
                    while (reader.Read())
                        transactions.Add(MapCashTransaction(reader));
                }
            }
            return transactions;
        }

        /// <summary>
        /// Kasa bakiyesini hesaplar (Gelir - Gider)
        /// </summary>
        public decimal GetCashBalance(int companyId, int periodId)
        {
            string query = @"SELECT 
                               SUM(CASE WHEN TransactionType = 'Income' THEN Amount ELSE -Amount END) 
                           FROM CashTransactions 
                           WHERE CompanyId = @CompanyId AND PeriodId = @PeriodId";
            var result = ExecuteScalar(query,
                new SQLiteParameter("@CompanyId", companyId),
                new SQLiteParameter("@PeriodId", periodId));
            return result != DBNull.Value && result != null ? Convert.ToDecimal(result) : 0;
        }

        public override int Add(CashTransaction entity)
        {
            string query = @"INSERT INTO CashTransactions 
                           (CompanyId, PeriodId, TransactionDate, Description, Amount, TransactionType, AccountId, CurrentAccountId, Notes, CreatedBy, CreatedAt)
                           VALUES (@CompanyId, @PeriodId, @TransactionDate, @Description, @Amount, @TransactionType, @AccountId, @CurrentAccountId, @Notes, @CreatedBy, @CreatedAt)";

            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@CompanyId", entity.CompanyId),
                new SQLiteParameter("@PeriodId", entity.PeriodId),
                new SQLiteParameter("@TransactionDate", entity.TransactionDate),
                new SQLiteParameter("@Description", entity.Description),
                new SQLiteParameter("@Amount", entity.Amount),
                new SQLiteParameter("@TransactionType", entity.TransactionType),
                new SQLiteParameter("@AccountId", entity.AccountId ?? (object)DBNull.Value),
                new SQLiteParameter("@CurrentAccountId", entity.CurrentAccountId ?? (object)DBNull.Value),
                new SQLiteParameter("@Notes", entity.Notes ?? string.Empty),
                new SQLiteParameter("@CreatedBy", entity.CreatedBy ?? (object)DBNull.Value),
                new SQLiteParameter("@CreatedAt", DateTime.Now)
            };

            ExecuteNonQuery(query, parameters);
            return Convert.ToInt32(ExecuteScalar("SELECT last_insert_rowid()"));
        }

        public override bool Update(CashTransaction entity)
        {
            string query = @"UPDATE CashTransactions SET 
                           TransactionDate = @TransactionDate, Description = @Description, Amount = @Amount, 
                           TransactionType = @TransactionType, AccountId = @AccountId, CurrentAccountId = @CurrentAccountId, Notes = @Notes 
                           WHERE CashTransactionId = @CashTransactionId";

            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@TransactionDate", entity.TransactionDate),
                new SQLiteParameter("@Description", entity.Description),
                new SQLiteParameter("@Amount", entity.Amount),
                new SQLiteParameter("@TransactionType", entity.TransactionType),
                new SQLiteParameter("@AccountId", entity.AccountId ?? (object)DBNull.Value),
                new SQLiteParameter("@CurrentAccountId", entity.CurrentAccountId ?? (object)DBNull.Value),
                new SQLiteParameter("@Notes", entity.Notes ?? string.Empty),
                new SQLiteParameter("@CashTransactionId", entity.CashTransactionId)
            };

            return ExecuteNonQuery(query, parameters) > 0;
        }

        public override bool Delete(int id)
        {
            string query = "DELETE FROM CashTransactions WHERE CashTransactionId = @CashTransactionId";
            return ExecuteNonQuery(query, new SQLiteParameter("@CashTransactionId", id)) > 0;
        }

        private CashTransaction MapCashTransaction(SQLiteDataReader reader)
        {
            return new CashTransaction
            {
                CashTransactionId = Convert.ToInt32(reader["CashTransactionId"]),
                CompanyId = Convert.ToInt32(reader["CompanyId"]),
                PeriodId = Convert.ToInt32(reader["PeriodId"]),
                TransactionDate = DateTime.Parse(reader["TransactionDate"].ToString()),
                Description = reader["Description"].ToString(),
                Amount = Convert.ToDecimal(reader["Amount"]),
                TransactionType = reader["TransactionType"].ToString(),
                AccountId = reader["AccountId"] != DBNull.Value ? Convert.ToInt32(reader["AccountId"]) : (int?)null,
                CurrentAccountId = reader["CurrentAccountId"] != DBNull.Value ? Convert.ToInt32(reader["CurrentAccountId"]) : (int?)null,
                Notes = reader["Notes"].ToString(),
                CreatedBy = reader["CreatedBy"] != DBNull.Value ? Convert.ToInt32(reader["CreatedBy"]) : (int?)null,
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString())
            };
        }
    }
}
