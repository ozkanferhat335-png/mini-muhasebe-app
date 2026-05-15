using System.Collections.Generic;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Interfaces
{
    public interface ICurrentAccountService
    {
        CurrentAccount CreateCurrentAccount(int companyId, string title, string accountType,
            string taxNumber, string taxId, string phone, string email, string address);
        bool UpdateCurrentAccount(CurrentAccount account);
        List<CurrentAccount> GetAccountsByCompany(int companyId);
        List<CurrentAccount> GetAccountsByCompanyAndType(int companyId, string accountType);
        CurrentAccount GetAccountById(int accountId);
        bool DeleteCurrentAccount(int accountId);
    }
}
