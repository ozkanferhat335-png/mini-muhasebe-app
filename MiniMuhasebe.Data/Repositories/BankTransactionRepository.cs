using System;
using System.Collections.Generic;
using System.Data.SQLite;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Data.Repositories
{
    public class BankTransactionRepository : BaseRepository<BankTransaction>
    {
        public BankTransactionRepository(string connectionString) : base(connectionString) { }

        public override BankTransaction GetById(int id)
        {
            string query = "SELECT * FROM BankTransactions WHERE BankTransactionId = @BankTransactionId";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@BankTransactionId", id)))
                {
                    if (reader.Read())
                    {
                        return MapBankTransaction(reader);
                    }
                }
            }
            return null;
        }

        public BankTransaction GetByExternalId(string externalId)
        {
            string query = "SELECT * FROM BankTransactions WHERE BankTransactionId_External = @ExternalId";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@ExternalId", externalId)))
                {
                    if (reader.Read())
                    {
                        return MapBankTransaction(reader);
                    }
                }
            }
            return null;
        }

        public List<BankTransaction> GetByBankAccountAndDateRange(int bankAccountId, DateTime startDate, DateTime endDate)
        {
            var transactions = new List<BankTransaction>();
            string query = @"SELECT * FROM BankTransactions 
                           WHERE BankAccountId = @BankAccountId AND TransactionDate >= @StartDate AND TransactionDate <= @EndDate 
                           ORDER BY TransactionDate DESC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection,
                    new SQLiteParameter("@BankAccountId", bankAccountId),
                    new SQLiteParameter("@StartDate", startDate),
                    new SQLiteParameter("@EndDate", endDate)))
                {
                    while (reader.Read())
                    {
                        transactions.Add(MapBankTransaction(reader));
                    }
                }
            }
            return transactions;
        }

        public List<BankTransaction> GetUnmatchedByBankAccount(int bankAccountId)
        {
            var transactions = new List<BankTransaction>();
            string query = "SELECT * FROM BankTransactions WHERE BankAccountId = @BankAccountId AND IsMatched = 0 ORDER BY TransactionDate DESC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@BankAccountId", bankAccountId)))
                {
                    while (reader.Read())
                    {
                        transactions.Add(MapBankTransaction(reader));
                    }
                }
            }
            return transactions;
        }

        public override List<BankTransaction> GetAll()
        {
            var transactions = new List<BankTransaction>();
            string query = "SELECT * FROM BankTransactions ORDER BY TransactionDate DESC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection))
                {
                    while (reader.Read())
                    {
                        transactions.Add(MapBankTransaction(reader));
                    }
                }
            }
            return transactions;
        }

        public override int Add(BankTransaction entity)
        {
            string query = @"INSERT INTO BankTransactions 
                           (BankAccountId, TransactionDate, Amount, Description, Balance, ReferenceNumber, BankTransactionId_External, 
                            TransactionType, Status, IsMatched, SyncedAt, CreatedAt) 
                           VALUES (@BankAccountId, @TransactionDate, @Amount, @Description, @Balance, @ReferenceNumber, @ExternalId, 
                            @TransactionType, @Status, @IsMatched, @SyncedAt, @CreatedAt)";
            
            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@BankAccountId", entity.BankAccountId),
                new SQLiteParameter("@TransactionDate", entity.TransactionDate),
                new SQLiteParameter("@Amount", entity.Amount),
                new SQLiteParameter("@Description", entity.Description ?? string.Empty),
                new SQLiteParameter("@Balance", entity.Balance ?? (object)DBNull.Value),
                new SQLiteParameter("@ReferenceNumber", entity.ReferenceNumber ?? string.Empty),
                new SQLiteParameter("@ExternalId", entity.BankTransactionId_External ?? string.Empty),
                new SQLiteParameter("@TransactionType", entity.TransactionType ?? string.Empty),
                new SQLiteParameter("@Status", entity.Status ?? "Pending"),
                new SQLiteParameter("@IsMatched", entity.IsMatched ? 1 : 0),
                new SQLiteParameter("@SyncedAt", entity.SyncedAt ?? (object)DBNull.Value),
                new SQLiteParameter("@CreatedAt", DateTime.Now)
            };

            ExecuteNonQuery(query, parameters);
            return Convert.ToInt32(ExecuteScalar("SELECT last_insert_rowid()"));
        }

        public override bool Update(BankTransaction entity)
        {
            string query = @"UPDATE BankTransactions SET 
                           TransactionDate = @TransactionDate, Amount = @Amount, Description = @Description, Balance = @Balance, 
                           ReferenceNumber = @ReferenceNumber, TransactionType = @TransactionType, Status = @Status, IsMatched = @IsMatched 
                           WHERE BankTransactionId = @BankTransactionId";
            
            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@TransactionDate", entity.TransactionDate),
                new SQLiteParameter("@Amount", entity.Amount),
                new SQLiteParameter("@Description", entity.Description ?? string.Empty),
                new SQLiteParameter("@Balance", entity.Balance ?? (object)DBNull.Value),
                new SQLiteParameter("@ReferenceNumber", entity.ReferenceNumber ?? string.Empty),
                new SQLiteParameter("@TransactionType", entity.TransactionType ?? string.Empty),
                new SQLiteParameter("@Status", entity.Status ?? "Pending"),
                new SQLiteParameter("@IsMatched", entity.IsMatched ? 1 : 0),
                new SQLiteParameter("@BankTransactionId", entity.BankTransactionId)
            };

            return ExecuteNonQuery(query, parameters) > 0;
        }

        public override bool Delete(int id)
        {
            string query = "DELETE FROM BankTransactions WHERE BankTransactionId = @BankTransactionId";
            return ExecuteNonQuery(query, new SQLiteParameter("@BankTransactionId", id)) > 0;
        }

        private BankTransaction MapBankTransaction(SQLiteDataReader reader)
        {
            return new BankTransaction
            {
                BankTransactionId = Convert.ToInt32(reader["BankTransactionId"]),
                BankAccountId = Convert.ToInt32(reader["BankAccountId"]),
                TransactionDate = DateTime.Parse(reader["TransactionDate"].ToString()),
                Amount = Convert.ToDecimal(reader["Amount"]),
                Description = reader["Description"].ToString(),
                Balance = reader["Balance"] != DBNull.Value ? Convert.ToDecimal(reader["Balance"]) : (decimal?)null,
                ReferenceNumber = reader["ReferenceNumber"].ToString(),
                BankTransactionId_External = reader["BankTransactionId_External"].ToString(),
                TransactionType = reader["TransactionType"].ToString(),
                Status = reader["Status"].ToString(),
                IsMatched = Convert.ToInt32(reader["IsMatched"]) == 1,
                SyncedAt = reader["SyncedAt"] != DBNull.Value ? DateTime.Parse(reader["SyncedAt"].ToString()) : (DateTime?)null,
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString())
            };
        }
    }
}
