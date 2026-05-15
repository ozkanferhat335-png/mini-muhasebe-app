using System.Collections.Generic;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Interfaces
{
    public interface ICompanyService
    {
        Company CreateCompany(string companyName, string taxOffice, string taxNumber, string phone, string email, string address);
        bool UpdateCompany(Company company);
        List<Company> GetAllCompanies();
        Company GetCompanyById(int companyId);
        bool DeleteCompany(int companyId);
    }
}
