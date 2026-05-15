using System.Collections.Generic;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Interfaces
{
    public interface IUserService
    {
        User Login(string username, string password);
        bool ChangePassword(int userId, string oldPassword, string newPassword);
        User CreateUser(string username, string email, string password, int roleId);
        List<User> GetAllUsers();
        User GetUserById(int userId);
    }
}
