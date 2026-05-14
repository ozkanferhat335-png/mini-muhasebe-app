using System;
using System.Collections.Generic;
using MiniMuhasebe.Data;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Services
{
    /// <summary>
    /// Kullanıcı işlemleri servisi
    /// </summary>
    public class UserService
    {
        private readonly UserRepository _userRepository;
        private readonly Logger _logger;

        public UserService(string connectionString)
        {
            _userRepository = new UserRepository(connectionString);
            _logger = new Logger();
        }

        /// <summary>
        /// Kullanıcı giriş yaptırır (kimlik doğrulaması)
        /// </summary>
        public User Login(string username, string password)
        {
            try
            {
                var user = _userRepository.GetByUsername(username);
                
                if (user == null)
                {
                    _logger.Warning($"Giriş denemesi başarısız: Kullanıcı bulunamadı - {username}");
                    return null;
                }

                if (!user.IsActive)
                {
                    _logger.Warning($"Giriş denemesi başarısız: Kullanıcı deaktif - {username}");
                    return null;
                }

                // Şifre doğrulaması
                if (!PasswordHelper.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
                {
                    _logger.Warning($"Giriş denemesi başarısız: Yanlış Şifre - {username}");
                    return null;
                }

                // Son giriş zamanını güncelle
                _userRepository.UpdateLastLogin(user.UserId);
                _logger.Info($"Başarılı giriş: {username}");

                return user;
            }
            catch (Exception ex)
            {
                _logger.Error("Giriş işlemi sırasında hata oluştu", ex);
                return null;
            }
        }

        /// <summary>
        /// Şifre değiştirme
        /// </summary>
        public bool ChangePassword(int userId, string oldPassword, string newPassword)
        {
            try
            {
                var user = _userRepository.GetById(userId);
                if (user == null)
                    return false;

                // Eski Şifre kontrolü
                if (!PasswordHelper.VerifyPassword(oldPassword, user.PasswordHash, user.PasswordSalt))
                {
                    _logger.Warning($"Yanlış eski Şifre: {user.Username}");
                    return false;
                }

                // Yeni Şifre hash'le
                var (hash, salt) = PasswordHelper.HashPassword(newPassword);
                
                bool success = _userRepository.UpdatePassword(userId, hash, salt);
                if (success)
                {
                    _logger.Info($"Şifre değiştirildi: {user.Username}");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.Error("Şifre değiştirme sırasında hata", ex);
                return false;
            }
        }

        /// <summary>
        /// Yeni kullanıcı oluşturma
        /// </summary>
        public User CreateUser(string username, string email, string password, int roleId)
        {
            try
            {
                // Kullanıcı adının benzersiz olduğunu kontrol et
                var existingUser = _userRepository.GetByUsername(username);
                if (existingUser != null)
                {
                    _logger.Warning($"Kullanıcı oluşturulamadı: Kullanıcı adı zaten mevcut - {username}");
                    return null;
                }

                // Şifre hash'le
                var (hash, salt) = PasswordHelper.HashPassword(password);

                var newUser = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    RoleId = roleId,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                int userId = _userRepository.Add(newUser);
                newUser.UserId = userId;

                _logger.Info($"Yeni kullanıcı oluşturuldu: {username} (ID: {userId})");
                return newUser;
            }
            catch (Exception ex)
            {
                _logger.Error("Kullanıcı oluşturma sırasında hata", ex);
                return null;
            }
        }

        /// <summary>
        /// Tüm aktif kullanıcıları getir
        /// </summary>
        public List<User> GetAllUsers()
        {
            try
            {
                return _userRepository.GetAll();
            }
            catch (Exception ex)
            {
                _logger.Error("Kullanıcı listesi alınırken hata", ex);
                return new List<User>();
            }
        }

        /// <summary>
        /// ID'ye göre kullanıcı getir
        /// </summary>
        public User GetUserById(int userId)
        {
            try
            {
                return _userRepository.GetById(userId);
            }
            catch (Exception ex)
            {
                _logger.Error($"Kullanıcı alınırken hata (ID: {userId})", ex);
                return null;
            }
        }
    }
}
