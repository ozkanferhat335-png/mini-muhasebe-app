using System;
using System.Collections.Generic;
using System.Data.SQLite;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Data.Repositories
{
    public class UserRepository : BaseRepository<User>
    {
        public UserRepository(string connectionString) : base(connectionString) { }

        public override User GetById(int id)
        {
            string query = "SELECT * FROM Users WHERE UserId = @UserId";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@UserId", id)))
                {
                    if (reader.Read())
                    {
                        return MapUser(reader);
                    }
                }
            }
            return null;
        }

        public User GetByUsername(string username)
        {
            string query = "SELECT * FROM Users WHERE Username = @Username";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@Username", username)))
                {
                    if (reader.Read())
                    {
                        return MapUser(reader);
                    }
                }
            }
            return null;
        }

        public override List<User> GetAll()
        {
            var users = new List<User>();
            string query = "SELECT * FROM Users WHERE IsActive = 1 ORDER BY Username";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection))
                {
                    while (reader.Read())
                    {
                        users.Add(MapUser(reader));
                    }
                }
            }
            return users;
        }

        public override int Add(User entity)
        {
            string query = @"INSERT INTO Users (Username, Email, PasswordHash, PasswordSalt, RoleId, IsActive, CreatedAt, UpdatedAt) 
                           VALUES (@Username, @Email, @PasswordHash, @PasswordSalt, @RoleId, @IsActive, @CreatedAt, @UpdatedAt)";
            
            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@Username", entity.Username),
                new SQLiteParameter("@Email", entity.Email ?? string.Empty),
                new SQLiteParameter("@PasswordHash", entity.PasswordHash),
                new SQLiteParameter("@PasswordSalt", entity.PasswordSalt),
                new SQLiteParameter("@RoleId", entity.RoleId),
                new SQLiteParameter("@IsActive", entity.IsActive ? 1 : 0),
                new SQLiteParameter("@CreatedAt", DateTime.Now),
                new SQLiteParameter("@UpdatedAt", DateTime.Now)
            };

            ExecuteNonQuery(query, parameters);
            
            // Yeni eklenen kullanıcının ID'sini al
            return Convert.ToInt32(ExecuteScalar("SELECT last_insert_rowid()"));
        }

        public override bool Update(User entity)
        {
            string query = @"UPDATE Users SET Email = @Email, RoleId = @RoleId, IsActive = @IsActive, UpdatedAt = @UpdatedAt 
                           WHERE UserId = @UserId";
            
            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@Email", entity.Email ?? string.Empty),
                new SQLiteParameter("@RoleId", entity.RoleId),
                new SQLiteParameter("@IsActive", entity.IsActive ? 1 : 0),
                new SQLiteParameter("@UpdatedAt", DateTime.Now),
                new SQLiteParameter("@UserId", entity.UserId)
            };

            return ExecuteNonQuery(query, parameters) > 0;
        }

        public bool UpdatePassword(int userId, string passwordHash, string passwordSalt)
        {
            string query = "UPDATE Users SET PasswordHash = @Hash, PasswordSalt = @Salt, UpdatedAt = @UpdatedAt WHERE UserId = @UserId";
            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@Hash", passwordHash),
                new SQLiteParameter("@Salt", passwordSalt),
                new SQLiteParameter("@UpdatedAt", DateTime.Now),
                new SQLiteParameter("@UserId", userId)
            };
            return ExecuteNonQuery(query, parameters) > 0;
        }

        public bool UpdateLastLogin(int userId)
        {
            string query = "UPDATE Users SET LastLoginAt = @LastLoginAt WHERE UserId = @UserId";
            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@LastLoginAt", DateTime.Now),
                new SQLiteParameter("@UserId", userId)
            };
            return ExecuteNonQuery(query, parameters) > 0;
        }

        public override bool Delete(int id)
        {
            string query = "UPDATE Users SET IsActive = 0 WHERE UserId = @UserId";
            return ExecuteNonQuery(query, new SQLiteParameter("@UserId", id)) > 0;
        }

        private User MapUser(SQLiteDataReader reader)
        {
            return new User
            {
                UserId = Convert.ToInt32(reader["UserId"]),
                Username = reader["Username"].ToString(),
                Email = reader["Email"].ToString(),
                PasswordHash = reader["PasswordHash"].ToString(),
                PasswordSalt = reader["PasswordSalt"].ToString(),
                RoleId = Convert.ToInt32(reader["RoleId"]),
                IsActive = Convert.ToInt32(reader["IsActive"]) == 1,
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString()),
                UpdatedAt = DateTime.Parse(reader["UpdatedAt"].ToString()),
                LastLoginAt = reader["LastLoginAt"] != DBNull.Value ? DateTime.Parse(reader["LastLoginAt"].ToString()) : (DateTime?)null
            };
        }
    }
}
