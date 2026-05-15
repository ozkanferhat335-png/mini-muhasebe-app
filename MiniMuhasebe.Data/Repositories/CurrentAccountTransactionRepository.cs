using System;
using System.Collections.Generic;
using System.Data.SQLite;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Data.Repositories
{
    public class CurrentAccountTransactionRepository : BaseRepository<CurrentAccountTransaction>
    {
        public CurrentAccountTransactionRepository(string connectionString) : base(connectionString) { }

        public override CurrentAccountTransaction GetById(int id)
        {
            string query = "SELECT * FROM CurrentAccountTransactions WHERE TransactionId = @TransactionId";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@TransactionId", id)))
                {
                    if (reader.Read())
                        return MapTransaction(reader);
                }
            }
            return null;
        }

        public override List<CurrentAccountTransaction> GetAll()
        {
            var transactions = new List<CurrentAccountTransaction>();
            string query = "SELECT * FROM CurrentAccountTransactions ORDER BY TransactionDate DESC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection))
                {
                    while (reader.Read())
                        transactions.Add(MapTransaction(reader));
                }
            }
            return transactions;
        }

        public List<CurrentAccountTransaction> GetByCurrentAccountId(int currentAccountId)
        {
            var transactions = new List<CurrentAccountTransaction>();
            string query = "SELECT * FROM CurrentAccountTransactions WHERE CurrentAccountId = @CurrentAccountId ORDER BY TransactionDate DESC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@CurrentAccountId", currentAccountId)))
                {
                    while (reader.Read())
                        transactions.Add(MapTransaction(reader));
                }
            }
            return transactions;
        }

        public List<CurrentAccountTransaction> GetByCurrentAccountAndDateRange(int currentAccountId, DateTime startDate, DateTime endDate)
        {
            var transactions = new List<CurrentAccountTransaction>();
            string query = @"SELECT * FROM CurrentAccountTransactions 
                           WHERE CurrentAccountId = @CurrentAccountId 
                           AND TransactionDate >= @StartDate AND TransactionDate <= @EndDate 
                           ORDER BY TransactionDate DESC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection,
                    new SQLiteParameter("@CurrentAccountId", currentAccountId),
                    new SQLiteParameter("@StartDate", startDate),
                    new SQLiteParameter("@EndDate", endDate)))
                {
                    while (reader.Read())
                        transactions.Add(MapTransaction(reader));
                }
            }
            return transactions;
        }

        /// <summary>
        /// Cari hesap bakiyesini hesaplar (Alacak - Borç)
        /// </summary>
        public (decimal totalDebit, decimal totalCredit, decimal balance) GetBalance(int currentAccountId)
        {
            string query = @"SELECT 
                               SUM(CASE WHEN TransactionType = 'Debit' THEN Amount ELSE 0 END) AS TotalDebit,
                               SUM(CASE WHEN TransactionType = 'Credit' THEN Amount ELSE 0 END) AS TotalCredit
                           FROM CurrentAccountTransactions 
                           WHERE CurrentAccountId = @CurrentAccountId";

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@CurrentAccountId", currentAccountId)))
                {
                    if (reader.Read())
                    {
                        decimal debit = reader["TotalDebit"] != DBNull.Value ? Convert.ToDecimal(reader["TotalDebit"]) : 0;
                        decimal credit = reader["TotalCredit"] != DBNull.Value ? Convert.ToDecimal(reader["TotalCredit"]) : 0;
                        return (debit, credit, credit - debit);
                    }
                }
            }
            return (0, 0, 0);
        }

        public override int Add(CurrentAccountTransaction entity)
        {
            string query = @"INSERT INTO CurrentAccountTransactions 
                           (CurrentAccountId, TransactionDate, Amount, TransactionType, Description, RelatedDocumentNumber, IncomeExpenseTransactionId, Notes, CreatedBy, CreatedAt)
                           VALUES (@CurrentAccountId, @TransactionDate, @Amount, @TransactionType, @Description, @RelatedDocumentNumber, @IncomeExpenseTransactionId, @Notes, @CreatedBy, @CreatedAt)";

            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@CurrentAccountId", entity.CurrentAccountId),
                new SQLiteParameter("@TransactionDate", entity.TransactionDate),
                new SQLiteParameter("@Amount", entity.Amount),
                new SQLiteParameter("@TransactionType", entity.TransactionType),
                new SQLiteParameter("@Description", entity.Description ?? string.Empty),
                new SQLiteParameter("@RelatedDocumentNumber", entity.RelatedDocumentNumber ?? string.Empty),
                new SQLiteParameter("@IncomeExpenseTransactionId", entity.IncomeExpenseTransactionId ?? (object)DBNull.Value),
                new SQLiteParameter("@Notes", entity.Notes ?? string.Empty),
                new SQLiteParameter("@CreatedBy", entity.CreatedBy ?? (object)DBNull.Value),
                new SQLiteParameter("@CreatedAt", DateTime.Now)
            };

            ExecuteNonQuery(query, parameters);
            return Convert.ToInt32(ExecuteScalar("SELECT last_insert_rowid()"));
        }

        public override bool Update(CurrentAccountTransaction entity)
        {
            string query = @"UPDATE CurrentAccountTransactions SET 
                           TransactionDate = @TransactionDate, Amount = @Amount, TransactionType = @TransactionType, 
                           Description = @Description, RelatedDocumentNumber = @RelatedDocumentNumber, Notes = @Notes 
                           WHERE TransactionId = @TransactionId";

            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@TransactionDate", entity.TransactionDate),
                new SQLiteParameter("@Amount", entity.Amount),
                new SQLiteParameter("@TransactionType", entity.TransactionType),
                new SQLiteParameter("@Description", entity.Description ?? string.Empty),
                new SQLiteParameter("@RelatedDocumentNumber", entity.RelatedDocumentNumber ?? string.Empty),
                new SQLiteParameter("@Notes", entity.Notes ?? string.Empty),
                new SQLiteParameter("@TransactionId", entity.TransactionId)
            };

            return ExecuteNonQuery(query, parameters) > 0;
        }

        public override bool Delete(int id)
        {
            string query = "DELETE FROM CurrentAccountTransactions WHERE TransactionId = @TransactionId";
            return ExecuteNonQuery(query, new SQLiteParameter("@TransactionId", id)) > 0;
        }

        private CurrentAccountTransaction MapTransaction(SQLiteDataReader reader)
        {
            return new CurrentAccountTransaction
            {
                TransactionId = Convert.ToInt32(reader["TransactionId"]),
                CurrentAccountId = Convert.ToInt32(reader["CurrentAccountId"]),
                TransactionDate = DateTime.Parse(reader["TransactionDate"].ToString()),
                Amount = Convert.ToDecimal(reader["Amount"]),
                TransactionType = reader["TransactionType"].ToString(),
                Description = reader["Description"].ToString(),
                RelatedDocumentNumber = reader["RelatedDocumentNumber"].ToString(),
                IncomeExpenseTransactionId = reader["IncomeExpenseTransactionId"] != DBNull.Value ? Convert.ToInt32(reader["IncomeExpenseTransactionId"]) : (int?)null,
                Notes = reader["Notes"].ToString(),
                CreatedBy = reader["CreatedBy"] != DBNull.Value ? Convert.ToInt32(reader["CreatedBy"]) : (int?)null,
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString())
            };
        }
    }
}
