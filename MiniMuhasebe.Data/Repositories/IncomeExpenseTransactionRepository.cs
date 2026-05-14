using System;
using System.Collections.Generic;
using System.Data.SQLite;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Data.Repositories
{
    public class IncomeExpenseTransactionRepository : BaseRepository<IncomeExpenseTransaction>
    {
        public IncomeExpenseTransactionRepository(string connectionString) : base(connectionString) { }

        public override IncomeExpenseTransaction GetById(int id)
        {
            string query = "SELECT * FROM IncomeExpenseTransactions WHERE TransactionId = @TransactionId";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@TransactionId", id)))
                {
                    if (reader.Read())
                    {
                        return MapTransaction(reader);
                    }
                }
            }
            return null;
        }

        public List<IncomeExpenseTransaction> GetByPeriodId(int periodId)
        {
            var transactions = new List<IncomeExpenseTransaction>();
            string query = "SELECT * FROM IncomeExpenseTransactions WHERE PeriodId = @PeriodId ORDER BY TransactionDate DESC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@PeriodId", periodId)))
                {
                    while (reader.Read())
                    {
                        transactions.Add(MapTransaction(reader));
                    }
                }
            }
            return transactions;
        }

        public List<IncomeExpenseTransaction> GetByCompanyAndDateRange(int companyId, DateTime startDate, DateTime endDate)
        {
            var transactions = new List<IncomeExpenseTransaction>();
            string query = @"SELECT * FROM IncomeExpenseTransactions 
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
                    {
                        transactions.Add(MapTransaction(reader));
                    }
                }
            }
            return transactions;
        }

        public override List<IncomeExpenseTransaction> GetAll()
        {
            var transactions = new List<IncomeExpenseTransaction>();
            string query = "SELECT * FROM IncomeExpenseTransactions ORDER BY TransactionDate DESC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection))
                {
                    while (reader.Read())
                    {
                        transactions.Add(MapTransaction(reader));
                    }
                }
            }
            return transactions;
        }

        public override int Add(IncomeExpenseTransaction entity)
        {
            string query = @"INSERT INTO IncomeExpenseTransactions 
                           (CompanyId, PeriodId, AccountId, TransactionDate, DocumentNumber, Description, Amount, VatRate, VatAmount, NetAmount, 
                            PaymentType, BankAccountId, CurrentAccountId, BankTransactionId, Notes, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt) 
                           VALUES (@CompanyId, @PeriodId, @AccountId, @TransactionDate, @DocumentNumber, @Description, @Amount, @VatRate, @VatAmount, 
                            @NetAmount, @PaymentType, @BankAccountId, @CurrentAccountId, @BankTransactionId, @Notes, @CreatedBy, @CreatedAt, @UpdatedBy, @UpdatedAt)";
            
            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@CompanyId", entity.CompanyId),
                new SQLiteParameter("@PeriodId", entity.PeriodId),
                new SQLiteParameter("@AccountId", entity.AccountId),
                new SQLiteParameter("@TransactionDate", entity.TransactionDate),
                new SQLiteParameter("@DocumentNumber", entity.DocumentNumber ?? string.Empty),
                new SQLiteParameter("@Description", entity.Description),
                new SQLiteParameter("@Amount", entity.Amount),
                new SQLiteParameter("@VatRate", entity.VatRate),
                new SQLiteParameter("@VatAmount", entity.VatAmount),
                new SQLiteParameter("@NetAmount", entity.NetAmount),
                new SQLiteParameter("@PaymentType", entity.PaymentType),
                new SQLiteParameter("@BankAccountId", entity.BankAccountId ?? (object)DBNull.Value),
                new SQLiteParameter("@CurrentAccountId", entity.CurrentAccountId ?? (object)DBNull.Value),
                new SQLiteParameter("@BankTransactionId", entity.BankTransactionId ?? (object)DBNull.Value),
                new SQLiteParameter("@Notes", entity.Notes ?? string.Empty),
                new SQLiteParameter("@CreatedBy", entity.CreatedBy ?? (object)DBNull.Value),
                new SQLiteParameter("@CreatedAt", DateTime.Now),
                new SQLiteParameter("@UpdatedBy", entity.UpdatedBy ?? (object)DBNull.Value),
                new SQLiteParameter("@UpdatedAt", DateTime.Now)
            };

            ExecuteNonQuery(query, parameters);
            return Convert.ToInt32(ExecuteScalar("SELECT last_insert_rowid()"));
        }

        public override bool Update(IncomeExpenseTransaction entity)
        {
            string query = @"UPDATE IncomeExpenseTransactions SET 
                           TransactionDate = @TransactionDate, DocumentNumber = @DocumentNumber, Description = @Description, 
                           Amount = @Amount, VatRate = @VatRate, VatAmount = @VatAmount, NetAmount = @NetAmount, PaymentType = @PaymentType, 
                           BankAccountId = @BankAccountId, CurrentAccountId = @CurrentAccountId, BankTransactionId = @BankTransactionId, 
                           Notes = @Notes, UpdatedBy = @UpdatedBy, UpdatedAt = @UpdatedAt 
                           WHERE TransactionId = @TransactionId";
            
            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@TransactionDate", entity.TransactionDate),
                new SQLiteParameter("@DocumentNumber", entity.DocumentNumber ?? string.Empty),
                new SQLiteParameter("@Description", entity.Description),
                new SQLiteParameter("@Amount", entity.Amount),
                new SQLiteParameter("@VatRate", entity.VatRate),
                new SQLiteParameter("@VatAmount", entity.VatAmount),
                new SQLiteParameter("@NetAmount", entity.NetAmount),
                new SQLiteParameter("@PaymentType", entity.PaymentType),
                new SQLiteParameter("@BankAccountId", entity.BankAccountId ?? (object)DBNull.Value),
                new SQLiteParameter("@CurrentAccountId", entity.CurrentAccountId ?? (object)DBNull.Value),
                new SQLiteParameter("@BankTransactionId", entity.BankTransactionId ?? (object)DBNull.Value),
                new SQLiteParameter("@Notes", entity.Notes ?? string.Empty),
                new SQLiteParameter("@UpdatedBy", entity.UpdatedBy ?? (object)DBNull.Value),
                new SQLiteParameter("@UpdatedAt", DateTime.Now),
                new SQLiteParameter("@TransactionId", entity.TransactionId)
            };

            return ExecuteNonQuery(query, parameters) > 0;
        }

        public override bool Delete(int id)
        {
            string query = "DELETE FROM IncomeExpenseTransactions WHERE TransactionId = @TransactionId";
            return ExecuteNonQuery(query, new SQLiteParameter("@TransactionId", id)) > 0;
        }

        private IncomeExpenseTransaction MapTransaction(SQLiteDataReader reader)
        {
            return new IncomeExpenseTransaction
            {
                TransactionId = Convert.ToInt32(reader["TransactionId"]),
                CompanyId = Convert.ToInt32(reader["CompanyId"]),
                PeriodId = Convert.ToInt32(reader["PeriodId"]),
                AccountId = Convert.ToInt32(reader["AccountId"]),
                TransactionDate = DateTime.Parse(reader["TransactionDate"].ToString()),
                DocumentNumber = reader["DocumentNumber"].ToString(),
                Description = reader["Description"].ToString(),
                Amount = Convert.ToDecimal(reader["Amount"]),
                VatRate = Convert.ToDecimal(reader["VatRate"]),
                VatAmount = Convert.ToDecimal(reader["VatAmount"]),
                NetAmount = Convert.ToDecimal(reader["NetAmount"]),
                PaymentType = reader["PaymentType"].ToString(),
                BankAccountId = reader["BankAccountId"] != DBNull.Value ? Convert.ToInt32(reader["BankAccountId"]) : (int?)null,
                CurrentAccountId = reader["CurrentAccountId"] != DBNull.Value ? Convert.ToInt32(reader["CurrentAccountId"]) : (int?)null,
                BankTransactionId = reader["BankTransactionId"] != DBNull.Value ? Convert.ToInt32(reader["BankTransactionId"]) : (int?)null,
                Notes = reader["Notes"].ToString(),
                CreatedBy = reader["CreatedBy"] != DBNull.Value ? Convert.ToInt32(reader["CreatedBy"]) : (int?)null,
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString()),
                UpdatedBy = reader["UpdatedBy"] != DBNull.Value ? Convert.ToInt32(reader["UpdatedBy"]) : (int?)null,
                UpdatedAt = DateTime.Parse(reader["UpdatedAt"].ToString())
            };
        }
    }
}
