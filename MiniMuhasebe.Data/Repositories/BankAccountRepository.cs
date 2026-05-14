using System;
using System.Collections.Generic;
using System.Data.SQLite;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Data.Repositories
{
    public class BankAccountRepository : BaseRepository<BankAccount>
    {
        private readonly EncryptionHelper _encryptionHelper;

        public BankAccountRepository(string connectionString, string encryptionKey) : base(connectionString)
        {
            _encryptionHelper = new EncryptionHelper(encryptionKey);
        }

        public override BankAccount GetById(int id)
        {
            string query = "SELECT * FROM BankAccounts WHERE BankAccountId = @BankAccountId";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@BankAccountId", id)))
                {
                    if (reader.Read())
                    {
                        return MapBankAccount(reader);
                    }
                }
            }
            return null;
        }

        public List<BankAccount> GetByCompanyId(int companyId)
        {
            var accounts = new List<BankAccount>();
            string query = "SELECT * FROM BankAccounts WHERE CompanyId = @CompanyId AND IsActive = 1 ORDER BY BankName";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@CompanyId", companyId)))
                {
                    while (reader.Read())
                    {
                        accounts.Add(MapBankAccount(reader));
                    }
                }
            }
            return accounts;
        }

        public override List<BankAccount> GetAll()
        {
            var accounts = new List<BankAccount>();
            string query = "SELECT * FROM BankAccounts WHERE IsActive = 1 ORDER BY BankName";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection))
                {
                    while (reader.Read())
                    {
                        accounts.Add(MapBankAccount(reader));
                    }
                }
            }
            return accounts;
        }

        public override int Add(BankAccount entity)
        {
            string query = @"INSERT INTO BankAccounts (CompanyId, BankName, AccountName, IBAN, Currency, InitialBalance, CurrentBalance, 
                           IsApiEnabled, ApiProviderType, ApiBaseUrl, ApiClientId, ApiClientSecret, ApiKey, ApiUsername, ApiPassword, 
                           ApiAccountId, IsActive, CreatedAt, UpdatedAt) 
                           VALUES (@CompanyId, @BankName, @AccountName, @IBAN, @Currency, @InitialBalance, @CurrentBalance, 
                           @IsApiEnabled, @ApiProviderType, @ApiBaseUrl, @ApiClientId, @ApiClientSecret, @ApiKey, @ApiUsername, 
                           @ApiPassword, @ApiAccountId, @IsActive, @CreatedAt, @UpdatedAt)";
            
            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@CompanyId", entity.CompanyId),
                new SQLiteParameter("@BankName", entity.BankName),
                new SQLiteParameter("@AccountName", entity.AccountName ?? string.Empty),
                new SQLiteParameter("@IBAN", entity.IBAN ?? string.Empty),
                new SQLiteParameter("@Currency", entity.Currency ?? "TRY"),
                new SQLiteParameter("@InitialBalance", entity.InitialBalance),
                new SQLiteParameter("@CurrentBalance", entity.CurrentBalance),
                new SQLiteParameter("@IsApiEnabled", entity.IsApiEnabled ? 1 : 0),
                new SQLiteParameter("@ApiProviderType", entity.ApiProviderType ?? string.Empty),
                new SQLiteParameter("@ApiBaseUrl", entity.ApiBaseUrl ?? string.Empty),
                new SQLiteParameter("@ApiClientId", entity.ApiClientId ?? string.Empty),
                new SQLiteParameter("@ApiClientSecret", !string.IsNullOrEmpty(entity.ApiClientSecret) ? _encryptionHelper.Encrypt(entity.ApiClientSecret) : string.Empty),
                new SQLiteParameter("@ApiKey", !string.IsNullOrEmpty(entity.ApiKey) ? _encryptionHelper.Encrypt(entity.ApiKey) : string.Empty),
                new SQLiteParameter("@ApiUsername", entity.ApiUsername ?? string.Empty),
                new SQLiteParameter("@ApiPassword", !string.IsNullOrEmpty(entity.ApiPassword) ? _encryptionHelper.Encrypt(entity.ApiPassword) : string.Empty),
                new SQLiteParameter("@ApiAccountId", entity.ApiAccountId ?? string.Empty),
                new SQLiteParameter("@IsActive", entity.IsActive ? 1 : 0),
                new SQLiteParameter("@CreatedAt", DateTime.Now),
                new SQLiteParameter("@UpdatedAt", DateTime.Now)
            };

            ExecuteNonQuery(query, parameters);
            return Convert.ToInt32(ExecuteScalar("SELECT last_insert_rowid()"));
        }

        public override bool Update(BankAccount entity)
        {
            string query = @"UPDATE BankAccounts SET BankName = @BankName, AccountName = @AccountName, IBAN = @IBAN, Currency = @Currency, 
                           CurrentBalance = @CurrentBalance, IsApiEnabled = @IsApiEnabled, ApiProviderType = @ApiProviderType, 
                           ApiBaseUrl = @ApiBaseUrl, ApiClientId = @ApiClientId, ApiClientSecret = @ApiClientSecret, ApiKey = @ApiKey, 
                           ApiUsername = @ApiUsername, ApiPassword = @ApiPassword, ApiAccountId = @ApiAccountId, LastSyncAt = @LastSyncAt, 
                           IsActive = @IsActive, UpdatedAt = @UpdatedAt WHERE BankAccountId = @BankAccountId";
            
            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@BankName", entity.BankName),
                new SQLiteParameter("@AccountName", entity.AccountName ?? string.Empty),
                new SQLiteParameter("@IBAN", entity.IBAN ?? string.Empty),
                new SQLiteParameter("@Currency", entity.Currency ?? "TRY"),
                new SQLiteParameter("@CurrentBalance", entity.CurrentBalance),
                new SQLiteParameter("@IsApiEnabled", entity.IsApiEnabled ? 1 : 0),
                new SQLiteParameter("@ApiProviderType", entity.ApiProviderType ?? string.Empty),
                new SQLiteParameter("@ApiBaseUrl", entity.ApiBaseUrl ?? string.Empty),
                new SQLiteParameter("@ApiClientId", entity.ApiClientId ?? string.Empty),
                new SQLiteParameter("@ApiClientSecret", !string.IsNullOrEmpty(entity.ApiClientSecret) ? _encryptionHelper.Encrypt(entity.ApiClientSecret) : string.Empty),
                new SQLiteParameter("@ApiKey", !string.IsNullOrEmpty(entity.ApiKey) ? _encryptionHelper.Encrypt(entity.ApiKey) : string.Empty),
                new SQLiteParameter("@ApiUsername", entity.ApiUsername ?? string.Empty),
                new SQLiteParameter("@ApiPassword", !string.IsNullOrEmpty(entity.ApiPassword) ? _encryptionHelper.Encrypt(entity.ApiPassword) : string.Empty),
                new SQLiteParameter("@ApiAccountId", entity.ApiAccountId ?? string.Empty),
                new SQLiteParameter("@LastSyncAt", entity.LastSyncAt ?? (object)DBNull.Value),
                new SQLiteParameter("@IsActive", entity.IsActive ? 1 : 0),
                new SQLiteParameter("@UpdatedAt", DateTime.Now),
                new SQLiteParameter("@BankAccountId", entity.BankAccountId)
            };

            return ExecuteNonQuery(query, parameters) > 0;
        }

        public override bool Delete(int id)
        {
            string query = "UPDATE BankAccounts SET IsActive = 0 WHERE BankAccountId = @BankAccountId";
            return ExecuteNonQuery(query, new SQLiteParameter("@BankAccountId", id)) > 0;
        }

        private BankAccount MapBankAccount(SQLiteDataReader reader)
        {
            var account = new BankAccount
            {
                BankAccountId = Convert.ToInt32(reader["BankAccountId"]),
                CompanyId = Convert.ToInt32(reader["CompanyId"]),
                BankName = reader["BankName"].ToString(),
                AccountName = reader["AccountName"].ToString(),
                IBAN = reader["IBAN"].ToString(),
                Currency = reader["Currency"].ToString(),
                InitialBalance = Convert.ToDecimal(reader["InitialBalance"]),
                CurrentBalance = Convert.ToDecimal(reader["CurrentBalance"]),
                IsApiEnabled = Convert.ToInt32(reader["IsApiEnabled"]) == 1,
                ApiProviderType = reader["ApiProviderType"].ToString(),
                ApiBaseUrl = reader["ApiBaseUrl"].ToString(),
                ApiClientId = reader["ApiClientId"].ToString(),
                ApiClientSecret = TryDecrypt(reader["ApiClientSecret"].ToString()),
                ApiKey = TryDecrypt(reader["ApiKey"].ToString()),
                ApiUsername = reader["ApiUsername"].ToString(),
                ApiPassword = TryDecrypt(reader["ApiPassword"].ToString()),
                ApiAccountId = reader["ApiAccountId"].ToString(),
                LastSyncAt = reader["LastSyncAt"] != DBNull.Value ? DateTime.Parse(reader["LastSyncAt"].ToString()) : (DateTime?)null,
                IsActive = Convert.ToInt32(reader["IsActive"]) == 1,
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString()),
                UpdatedAt = DateTime.Parse(reader["UpdatedAt"].ToString())
            };

            return account;
        }

        private string TryDecrypt(string encryptedValue)
        {
            if (string.IsNullOrEmpty(encryptedValue))
                return string.Empty;

            try
            {
                return _encryptionHelper.Decrypt(encryptedValue);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
