using System;
using System.Collections.Generic;
using System.Linq;
using MiniMuhasebe.Data;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Business.Interfaces;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Services
{
    /// <summary>
    /// Banka hareketi - muhasebe kaydı eşleştirme servisi
    /// </summary>
    public class MatchingService : IMatchingService
    {
        private readonly TransactionMatchRepository _matchRepository;
        private readonly BankTransactionRepository _bankTransactionRepository;
        private readonly IncomeExpenseTransactionRepository _incomeExpenseRepository;
        private readonly Logger _logger;

        // Eşleştirme kuralları için varsayılan toleranslar
        private const decimal DefaultAmountTolerance = 0.01m;
        private const int DefaultDateToleranceDays = 3;

        public MatchingService(string connectionString)
        {
            _matchRepository = new TransactionMatchRepository(connectionString);
            _bankTransactionRepository = new BankTransactionRepository(connectionString);
            _incomeExpenseRepository = new IncomeExpenseTransactionRepository(connectionString);
            _logger = new Logger();
        }

        /// <summary>
        /// Manuel eşleştirme oluştur
        /// </summary>
        public TransactionMatch CreateManualMatch(int bankTransactionId, int incomeExpenseTransactionId, int? createdBy = null)
        {
            try
            {
                // Zaten eşleştirilmiş mi kontrol et
                if (_matchRepository.ExistsMatch(bankTransactionId, incomeExpenseTransactionId))
                {
                    _logger.Warning($"Eşleştirme zaten mevcut: BankTx={bankTransactionId}, IETx={incomeExpenseTransactionId}");
                    return null;
                }

                var bankTx = _bankTransactionRepository.GetById(bankTransactionId);
                var ieTx = _incomeExpenseRepository.GetById(incomeExpenseTransactionId);

                if (bankTx == null || ieTx == null)
                {
                    _logger.Warning("Eşleştirme için geçersiz işlem ID'leri");
                    return null;
                }

                decimal score = CalculateMatchScore(bankTx, ieTx, null);

                var match = new TransactionMatch
                {
                    BankTransactionId = bankTransactionId,
                    IncomeExpenseTransactionId = incomeExpenseTransactionId,
                    MatchScore = score,
                    MatchType = "Manual",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now
                };

                int matchId = _matchRepository.Add(match);
                match.MatchId = matchId;

                // Banka hareketini eşleştirildi olarak işaretle
                bankTx.IsMatched = true;
                bankTx.Status = "Matched";
                _bankTransactionRepository.Update(bankTx);

                _logger.Info($"Manuel eşleştirme oluşturuldu: MatchId={matchId}, Skor={score:F1}");
                return match;
            }
            catch (Exception ex)
            {
                _logger.Error("Manuel eşleştirme oluşturma sırasında hata", ex);
                return null;
            }
        }

        /// <summary>
        /// Otomatik eşleştirme çalıştır
        /// </summary>
        public List<TransactionMatch> RunAutoMatching(int companyId, int bankAccountId)
        {
            var createdMatches = new List<TransactionMatch>();

            try
            {
                var unmatchedBankTxs = _bankTransactionRepository.GetUnmatchedByBankAccount(bankAccountId);
                var unmatchedIETxs = _incomeExpenseRepository.GetByCompanyAndDateRange(
                    companyId,
                    DateTime.Now.AddYears(-1),
                    DateTime.Now.AddDays(1));

                // Sadece eşleştirilmemiş gelir-gider işlemlerini al
                var unmatchedIE = unmatchedIETxs
                    .Where(t => _matchRepository.GetByIncomeExpenseTransactionId(t.TransactionId) == null)
                    .ToList();

                foreach (var bankTx in unmatchedBankTxs)
                {
                    TransactionMatch bestMatch = null;
                    decimal bestScore = 0;
                    IncomeExpenseTransaction bestIETx = null;

                    foreach (var ieTx in unmatchedIE)
                    {
                        decimal score = CalculateMatchScore(bankTx, ieTx, DefaultAmountTolerance, DefaultDateToleranceDays);
                        if (score > bestScore && score >= 70) // Minimum %70 eşleşme skoru
                        {
                            bestScore = score;
                            bestIETx = ieTx;
                        }
                    }

                    if (bestIETx != null)
                    {
                        var match = new TransactionMatch
                        {
                            BankTransactionId = bankTx.BankTransactionId,
                            IncomeExpenseTransactionId = bestIETx.TransactionId,
                            MatchScore = bestScore,
                            MatchType = "Automatic",
                            CreatedAt = DateTime.Now
                        };

                        int matchId = _matchRepository.Add(match);
                        match.MatchId = matchId;

                        bankTx.IsMatched = true;
                        bankTx.Status = "Matched";
                        _bankTransactionRepository.Update(bankTx);

                        createdMatches.Add(match);
                        unmatchedIE.Remove(bestIETx); // Bir kez eşleştir

                        _logger.Info($"Otomatik eşleştirme: BankTx={bankTx.BankTransactionId}, IETx={bestIETx.TransactionId}, Skor={bestScore:F1}");
                    }
                }

                _logger.Info($"Otomatik eşleştirme tamamlandı: {createdMatches.Count} eşleştirme oluşturuldu");
                return createdMatches;
            }
            catch (Exception ex)
            {
                _logger.Error("Otomatik eşleştirme sırasında hata", ex);
                return createdMatches;
            }
        }

        /// <summary>
        /// Eşleştirme skoru hesapla (0-100)
        /// </summary>
        public decimal CalculateMatchScore(BankTransaction bankTx, IncomeExpenseTransaction ieTx,
            decimal? amountTolerance = null, int dateToleranceDays = DefaultDateToleranceDays)
        {
            decimal tolerance = amountTolerance ?? DefaultAmountTolerance;
            decimal score = 0;

            // Tutar eşleşmesi (50 puan)
            decimal bankAmount = Math.Abs(bankTx.Amount);
            decimal ieAmount = ieTx.Amount;
            decimal amountDiff = Math.Abs(bankAmount - ieAmount);

            if (amountDiff <= tolerance)
                score += 50;
            else if (amountDiff <= ieAmount * 0.01m) // %1 tolerans
                score += 40;
            else if (amountDiff <= ieAmount * 0.05m) // %5 tolerans
                score += 20;

            // Tarih eşleşmesi (30 puan)
            int daysDiff = Math.Abs((bankTx.TransactionDate - ieTx.TransactionDate).Days);
            if (daysDiff == 0)
                score += 30;
            else if (daysDiff <= 1)
                score += 25;
            else if (daysDiff <= dateToleranceDays)
                score += 15;
            else if (daysDiff <= dateToleranceDays * 2)
                score += 5;

            // Açıklama eşleşmesi (20 puan)
            if (!string.IsNullOrEmpty(bankTx.Description) && !string.IsNullOrEmpty(ieTx.Description))
            {
                string bankDesc = bankTx.Description.ToLowerInvariant();
                string ieDesc = ieTx.Description.ToLowerInvariant();

                if (bankDesc.Contains(ieDesc) || ieDesc.Contains(bankDesc))
                    score += 20;
                else
                {
                    // Kelime bazlı eşleşme
                    var bankWords = bankDesc.Split(new[] { ' ', '-', '_', '/' }, StringSplitOptions.RemoveEmptyEntries);
                    var ieWords = ieDesc.Split(new[] { ' ', '-', '_', '/' }, StringSplitOptions.RemoveEmptyEntries);
                    int matchingWords = bankWords.Count(w => w.Length > 3 && ieWords.Contains(w));
                    if (matchingWords > 0)
                        score += Math.Min(20, matchingWords * 5);
                }
            }

            return Math.Min(100, score);
        }

        // Overload for MatchingRule parameter (interface compatibility)
        public decimal CalculateMatchScore(BankTransaction bankTx, IncomeExpenseTransaction ieTx, MatchingRule rule)
        {
            decimal tolerance = rule?.AmountTolerance ?? DefaultAmountTolerance;
            int dateTolerance = rule?.DateTolerance ?? DefaultDateToleranceDays;
            return CalculateMatchScore(bankTx, ieTx, tolerance, dateTolerance);
        }

        /// <summary>
        /// Eşleştirmeyi kaldır
        /// </summary>
        public bool RemoveMatch(int matchId)
        {
            try
            {
                var match = _matchRepository.GetById(matchId);
                if (match == null)
                    return false;

                // Banka hareketini eşleştirilmemiş olarak işaretle
                var bankTx = _bankTransactionRepository.GetById(match.BankTransactionId);
                if (bankTx != null)
                {
                    bankTx.IsMatched = false;
                    bankTx.Status = "Unmatched";
                    _bankTransactionRepository.Update(bankTx);
                }

                bool success = _matchRepository.Delete(matchId);
                if (success)
                    _logger.Info($"Eşleştirme kaldırıldı: MatchId={matchId}");

                return success;
            }
            catch (Exception ex)
            {
                _logger.Error("Eşleştirme kaldırma sırasında hata", ex);
                return false;
            }
        }

        /// <summary>
        /// Bekleyen (eşleştirilmemiş) banka hareketlerini getir
        /// </summary>
        public List<BankTransaction> GetPendingBankTransactions(int bankAccountId)
        {
            try
            {
                return _bankTransactionRepository.GetUnmatchedByBankAccount(bankAccountId);
            }
            catch (Exception ex)
            {
                _logger.Error("Bekleyen banka hareketleri alınırken hata", ex);
                return new List<BankTransaction>();
            }
        }

        /// <summary>
        /// Eşleştirilmemiş gelir-gider işlemlerini getir
        /// </summary>
        public List<IncomeExpenseTransaction> GetUnmatchedIncomeExpenseTransactions(int companyId)
        {
            try
            {
                var allTxs = _incomeExpenseRepository.GetByCompanyAndDateRange(
                    companyId, DateTime.Now.AddYears(-2), DateTime.Now.AddDays(1));

                return allTxs
                    .Where(t => _matchRepository.GetByIncomeExpenseTransactionId(t.TransactionId) == null)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.Error("Eşleştirilmemiş işlemler alınırken hata", ex);
                return new List<IncomeExpenseTransaction>();
            }
        }

        /// <summary>
        /// Tüm eşleştirmeleri getir
        /// </summary>
        public List<TransactionMatch> GetAllMatches()
        {
            try
            {
                return _matchRepository.GetAll();
            }
            catch (Exception ex)
            {
                _logger.Error("Eşleştirmeler alınırken hata", ex);
                return new List<TransactionMatch>();
            }
        }
    }
}
