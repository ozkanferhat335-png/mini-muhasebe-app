using System;
using System.Collections.Generic;
using MiniMuhasebe.Data;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Services
{
    /// <summary>
    /// Mali dönem işlemleri servisi
    /// </summary>
    public class FiscalPeriodService
    {
        private readonly FiscalPeriodRepository _periodRepository;
        private readonly Logger _logger;

        public FiscalPeriodService(string connectionString)
        {
            _periodRepository = new FiscalPeriodRepository(connectionString);
            _logger = new Logger();
        }

        /// <summary>
        /// Yeni mali dönem oluştur
        /// </summary>
        public FiscalPeriod CreatePeriod(int companyId, string periodName, DateTime startDate, DateTime endDate)
        {
            try
            {
                if (startDate >= endDate)
                    throw new ArgumentException("Başlangıç tarihi bitiş tarihinden önce olmalıdır.");

                if (string.IsNullOrWhiteSpace(periodName))
                    periodName = $"{startDate:yyyy} - {startDate:MMMM}";

                var newPeriod = new FiscalPeriod
                {
                    CompanyId = companyId,
                    PeriodName = periodName,
                    StartDate = startDate,
                    EndDate = endDate,
                    IsClosed = false,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                int periodId = _periodRepository.Add(newPeriod);
                newPeriod.PeriodId = periodId;

                _logger.Info($"Yeni mali dönem oluşturuldu: {periodName} (ID: {periodId})");
                return newPeriod;
            }
            catch (Exception ex)
            {
                _logger.Error("Mali dönem oluşturma sırasında hata", ex);
                return null;
            }
        }

        /// <summary>
        /// Mali dönemi güncelle
        /// </summary>
        public bool UpdatePeriod(FiscalPeriod period)
        {
            try
            {
                if (period.IsClosed)
                {
                    _logger.Warning($"Kapalı dönem güncellenemez: {period.PeriodName}");
                    return false;
                }

                period.UpdatedAt = DateTime.Now;
                bool success = _periodRepository.Update(period);
                if (success)
                    _logger.Info($"Mali dönem güncellendi: {period.PeriodName}");
                return success;
            }
            catch (Exception ex)
            {
                _logger.Error("Mali dönem güncelleme sırasında hata", ex);
                return false;
            }
        }

        /// <summary>
        /// Mali dönemi kapat
        /// </summary>
        public bool ClosePeriod(int periodId)
        {
            try
            {
                bool success = _periodRepository.ClosePeriod(periodId);
                if (success)
                    _logger.Info($"Mali dönem kapatıldı: ID {periodId}");
                return success;
            }
            catch (Exception ex)
            {
                _logger.Error("Mali dönem kapatma sırasında hata", ex);
                return false;
            }
        }

        /// <summary>
        /// Firmaya ait tüm dönemleri getir
        /// </summary>
        public List<FiscalPeriod> GetPeriodsByCompany(int companyId)
        {
            try
            {
                return _periodRepository.GetByCompanyId(companyId);
            }
            catch (Exception ex)
            {
                _logger.Error("Mali dönemler alınırken hata", ex);
                return new List<FiscalPeriod>();
            }
        }

        /// <summary>
        /// Firmaya ait açık dönemleri getir
        /// </summary>
        public List<FiscalPeriod> GetOpenPeriodsByCompany(int companyId)
        {
            try
            {
                return _periodRepository.GetOpenPeriodsByCompanyId(companyId);
            }
            catch (Exception ex)
            {
                _logger.Error("Açık mali dönemler alınırken hata", ex);
                return new List<FiscalPeriod>();
            }
        }

        /// <summary>
        /// Aktif (bugünü kapsayan) dönemi getir
        /// </summary>
        public FiscalPeriod GetCurrentPeriod(int companyId)
        {
            try
            {
                return _periodRepository.GetCurrentPeriod(companyId);
            }
            catch (Exception ex)
            {
                _logger.Error("Aktif mali dönem alınırken hata", ex);
                return null;
            }
        }

        /// <summary>
        /// ID'ye göre dönem getir
        /// </summary>
        public FiscalPeriod GetPeriodById(int periodId)
        {
            try
            {
                return _periodRepository.GetById(periodId);
            }
            catch (Exception ex)
            {
                _logger.Error($"Mali dönem alınırken hata (ID: {periodId})", ex);
                return null;
            }
        }

        /// <summary>
        /// Dönemi sil
        /// </summary>
        public bool DeletePeriod(int periodId)
        {
            try
            {
                var period = _periodRepository.GetById(periodId);
                if (period != null && period.IsClosed)
                {
                    _logger.Warning($"Kapalı dönem silinemez: ID {periodId}");
                    return false;
                }

                bool success = _periodRepository.Delete(periodId);
                if (success)
                    _logger.Info($"Mali dönem silindi: ID {periodId}");
                return success;
            }
            catch (Exception ex)
            {
                _logger.Error("Mali dönem silme sırasında hata", ex);
                return false;
            }
        }
    }
}
