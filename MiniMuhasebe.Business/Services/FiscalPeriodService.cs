using System;
using System.Collections.Generic;
using MiniMuhasebe.Data;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Services
{
    public class FiscalPeriodService
    {
        private readonly FiscalPeriodRepository _periodRepository;
        private readonly Logger _logger;

        public FiscalPeriodService(string connectionString)
        {
            _periodRepository = new FiscalPeriodRepository(connectionString);
            _logger = new Logger();
        }

        public FiscalPeriod CreatePeriod(int companyId, string periodName, DateTime startDate, DateTime endDate)
        {
            try
            {
                var period = new FiscalPeriod
                {
                    CompanyId = companyId,
                    PeriodName = periodName,
                    StartDate = startDate,
                    EndDate = endDate,
                    IsClosed = false,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                int id = _periodRepository.Add(period);
                period.PeriodId = id;
                _logger.Info($"Mali dönem oluşturuldu: {periodName} (ID: {id})");
                return period;
            }
            catch (Exception ex)
            {
                _logger.Error("Mali dönem oluşturma hatası", ex);
                return null;
            }
        }

        public List<FiscalPeriod> GetPeriodsByCompany(int companyId)
        {
            try { return _periodRepository.GetByCompanyId(companyId); }
            catch (Exception ex) { _logger.Error("Mali dönemler alınırken hata", ex); return new List<FiscalPeriod>(); }
        }

        public FiscalPeriod GetActivePeriod(int companyId)
        {
            try { return _periodRepository.GetActiveByCompanyId(companyId); }
            catch (Exception ex) { _logger.Error("Aktif dönem alınırken hata", ex); return null; }
        }

        public FiscalPeriod GetPeriodById(int periodId)
        {
            try { return _periodRepository.GetById(periodId); }
            catch (Exception ex) { _logger.Error($"Dönem alınırken hata (ID: {periodId})", ex); return null; }
        }

        public bool ClosePeriod(int periodId)
        {
            try
            {
                var period = _periodRepository.GetById(periodId);
                if (period == null) return false;
                period.IsClosed = true;
                bool success = _periodRepository.Update(period);
                if (success) _logger.Info($"Mali dönem kapatıldı: {period.PeriodName}");
                return success;
            }
            catch (Exception ex) { _logger.Error("Dönem kapatma hatası", ex); return false; }
        }

        public bool UpdatePeriod(FiscalPeriod period)
        {
            try
            {
                period.UpdatedAt = DateTime.Now;
                return _periodRepository.Update(period);
            }
            catch (Exception ex) { _logger.Error("Dönem güncelleme hatası", ex); return false; }
        }

        public bool DeletePeriod(int periodId)
        {
            try { return _periodRepository.Delete(periodId); }
            catch (Exception ex) { _logger.Error("Dönem silme hatası", ex); return false; }
        }

        /// <summary>
        /// Yıl için 12 aylık dönem oluşturur
        /// </summary>
        public List<FiscalPeriod> CreateYearlyPeriods(int companyId, int year)
        {
            var periods = new List<FiscalPeriod>();
            string[] monthNames = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
                                    "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };

            for (int month = 1; month <= 12; month++)
            {
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);
                string name = $"{year} - {monthNames[month - 1]}";

                var period = CreatePeriod(companyId, name, startDate, endDate);
                if (period != null) periods.Add(period);
            }
            return periods;
        }
    }
}
