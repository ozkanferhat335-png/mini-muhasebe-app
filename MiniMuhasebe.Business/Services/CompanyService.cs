using System;
using System.Collections.Generic;
using MiniMuhasebe.Data;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Business.Interfaces;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Services
{
    /// <summary>
    /// Firma işlemleri servisi
    /// </summary>
    public class CompanyService : ICompanyService
    {
        private readonly CompanyRepository _companyRepository;
        private readonly Logger _logger;

        public CompanyService(string connectionString)
        {
            _companyRepository = new CompanyRepository(connectionString);
            _logger = new Logger();
        }

        /// <summary>
        /// Yeni firma oluştur
        /// </summary>
        public Company CreateCompany(string companyName, string taxOffice, string taxNumber, string phone, string email, string address)
        {
            try
            {
                var newCompany = new Company
                {
                    CompanyName = companyName,
                    TaxOffice = taxOffice,
                    TaxNumber = taxNumber,
                    Phone = phone,
                    Email = email,
                    Address = address,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                int companyId = _companyRepository.Add(newCompany);
                newCompany.CompanyId = companyId;

                _logger.Info($"Yeni firma oluşturuldu: {companyName} (ID: {companyId})");
                return newCompany;
            }
            catch (Exception ex)
            {
                _logger.Error("Firma oluşturma sırasında hata", ex);
                return null;
            }
        }

        /// <summary>
        /// Firmanı güncelle
        /// </summary>
        public bool UpdateCompany(Company company)
        {
            try
            {
                company.UpdatedAt = DateTime.Now;
                bool success = _companyRepository.Update(company);
                if (success)
                {
                    _logger.Info($"Firma güncellendi: {company.CompanyName}");
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.Error("Firma güncelleme sırasında hata", ex);
                return false;
            }
        }

        /// <summary>
        /// Tüm aktif firmaları getir
        /// </summary>
        public List<Company> GetAllCompanies()
        {
            try
            {
                return _companyRepository.GetAll();
            }
            catch (Exception ex)
            {
                _logger.Error("Firma listesi alınırken hata", ex);
                return new List<Company>();
            }
        }

        /// <summary>
        /// ID'ye göre firma getir
        /// </summary>
        public Company GetCompanyById(int companyId)
        {
            try
            {
                return _companyRepository.GetById(companyId);
            }
            catch (Exception ex)
            {
                _logger.Error($"Firma alınırken hata (ID: {companyId})", ex);
                return null;
            }
        }

        /// <summary>
        /// Firmayı sil (deaktif yap)
        /// </summary>
        public bool DeleteCompany(int companyId)
        {
            try
            {
                bool success = _companyRepository.Delete(companyId);
                if (success)
                {
                    _logger.Info($"Firma silindi (deaktif yapıldı): ID {companyId}");
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.Error("Firma silme sırasında hata", ex);
                return false;
            }
        }
    }
}
