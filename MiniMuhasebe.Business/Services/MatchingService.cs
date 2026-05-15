using System;
using System.Collections.Generic;
using System.Linq;
using MiniMuhasebe.Data;
using MiniMuhasebe.Data.Repositories;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Business.Services
{
    /// <summary>
    /// Banka hareketi - muhasebe kaydı eşleştirme servisi
    /// </summary>
    public class MatchingService
    {
        private readonly MatchingRepository _matchingRepository;
        private readonly MatchingRuleRepository _ruleRepository;
        private readonly BankTransactionRepository _bankTransactionRepository;
        private readonly IncomeExpenseTransactionRepository _incomeExpenseRepository;
        private readonly Logger _logger;

        public MatchingService(string connectionString)
        {
            _matchingRepository = new MatchingRepository(connectionString);
            _ruleRepository = new MatchingRuleRepository(connectionString);
            _bankTransactionRepository = new BankTransactionRepository(connectionString);
            _incomeExpenseRepository = new IncomeExpenseTransactionRepository(connectionString);
            _logger = new Logger();
        }

        /// <summary>
        /// Manuel eşleştirme
        /// </summary>
        public TransactionMatch CreateManualMatch(int bankTransactionId, int incomeExpenseTransactionId, int? createdBy = null)
        {
            try
            {
                var match = new TransactionMatch
                {
                    BankTransactionId = bankTransactionId,
                    IncomeExpenseTransactionId = incomeExpenseTransactionId,
                    MatchScore = 100,
                    MatchType = "Manual",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now
                };

                int matchId = _matchingRepository.Add(match);
                match.MatchId = matchId;

                // Banka hareketini eşleştirildi olarak işaretle
                var bankTx = _bankTransactionRepository.GetById(bankTransactionId);
                if (bankTx != null)
                {
                    bankTx.IsMatched = true;
                    bankTx.Status = "Matched";
                    _bankTransactionRepository.Update(bankTx);
                }

                _logger.Info($"Manuel eşleştirme oluşturuldu: BankTx={bankTransactionId}, IETx={incomeExpenseTransactionId}");
                return match;
            }
            catch (Exception ex)
            {
                _logger.Error("Manuel eşleştirme hatası", ex);
                return null;
            }
        }

        /// <summary>
        /// Eşleştirmeyi kaldır
        /// </summary>
        public bool RemoveMatch(int matchId)
        {
            try
            {
                var match = _matchingRepository.GetById(matchId);
                if (match == null) return false;

                bool deleted = _matchingRepository.Delete(matchId);
                if (deleted)
                {
                    // Banka hareketini eşleştirilmedi olarak işaretle
                    var bankTx = _bankTransactionRepository.GetById(match.BankTransactionId);
                    if (bankTx != null)
                    {
                        bankTx.IsMatched = false;
                        bankTx.Status = "Unmatched";
                        _bankTransactionRepository.Update(bankTx);
                    }
                    _logger.Info($"Eşleştirme kaldırıldı: MatchId={matchId}");
                }
                return deleted;
            }
            catch (Exception ex)
            {
                _logger.Error("Eşleştirme kaldırma hatası", ex);
                return false;
            }
        }

        /// <summary>
        /// Otomatik eşleştirme - tüm bekleyen banka hareketleri için
        /// </summary>
        public List<TransactionMatch> AutoMatch(int companyId, int bankAccountId)
        {
            var matches = new List<TransactionMatch>();
            try
            {
                var rules = _ruleRepository.GetByCompanyId(companyId);
                var unmatchedBankTx = _bankTransactionRepository.GetUnmatchedByBankAccount(bankAccountId);
                var allIETx = _incomeExpenseRepository.GetAll()
                    .Where(t => t.CompanyId == companyId && t.BankAccountId == bankAccountId)
                    .ToList();

                foreach (var bankTx in unmatchedBankTx)
                {
                    var bestMatch = FindBestMatch(bankTx, allIETx, rules);
                    if (bestMatch != null)
                    {
                        var match = CreateManualMatch(bankTx.BankTransactionId, bestMatch.TransactionId);
                        if (match != null)
                        {
                            match.MatchType = "Automatic";
                            matches.Add(match);
                        }
                    }
                }

                _logger.Info($"Otomatik eşleştirme tamamlandı: {matches.Count} eşleşme bulundu");
            }
            catch (Exception ex)
            {
                _logger.Error("Otomatik eşleştirme hatası", ex);
            }
            return matches;
        }

        /// <summary>
        /// En iyi eşleşmeyi bul
        /// </summary>
        private IncomeExpenseTransaction FindBestMatch(BankTransaction bankTx,
            List<IncomeExpenseTransaction> candidates, List<MatchingRule> rules)
        {
            IncomeExpenseTransaction bestMatch = null;
            decimal bestScore = 0;

            decimal amountTolerance = rules.Count > 0 ? rules[0].AmountTolerance : 0.01m;
            int dateTolerance = rules.Count > 0 ? rules[0].DateTolerance : 3;

            foreach (var ieTx in candidates)
            {
                decimal score = CalculateMatchScore(bankTx, ieTx, amountTolerance, dateTolerance);
                if (score > bestScore && score >= 70) // Minimum %70 eşleşme
                {
                    bestScore = score;
                    bestMatch = ieTx;
                }
            }

            return bestMatch;
        }

        /// <summary>
        /// Eşleşme skoru hesapla (0-100)
        /// </summary>
        private decimal CalculateMatchScore(BankTransaction bankTx, IncomeExpenseTransaction ieTx,
            decimal amountTolerance, int dateTolerance)
        {
            decimal score = 0;

            // Tutar eşleşmesi (40 puan)
            decimal amountDiff = Math.Abs(Math.Abs(bankTx.Amount) - ieTx.Amount);
            if (amountDiff <= amountTolerance)
                score += 40;
            else if (amountDiff <= ieTx.Amount * 0.01m) // %1 tolerans
                score += 30;
            else if (amountDiff <= ieTx.Amount * 0.05m) // %5 tolerans
                score += 15;

            // Tarih eşleşmesi (30 puan)
            int daysDiff = Math.Abs((bankTx.TransactionDate - ieTx.TransactionDate).Days);
            if (daysDiff == 0)
                score += 30;
            else if (daysDiff <= dateTolerance)
                score += 20;
            else if (daysDiff <= dateTolerance * 2)
                score += 10;

            // Açıklama eşleşmesi (30 puan)
            if (!string.IsNullOrEmpty(bankTx.Description) && !string.IsNullOrEmpty(ieTx.Description))
            {
                string bankDesc = bankTx.Description.ToLower();
                string ieDesc = ieTx.Description.ToLower();

                if (bankDesc.Contains(ieDesc) || ieDesc.Contains(bankDesc))
                    score += 30;
                else
                {
                    // Kelime bazlı eşleşme
                    var bankWords = bankDesc.Split(' ');
                    var ieWords = ieDesc.Split(' ');
                    int matchingWords = 0;
                    foreach (var word in bankWords)
                    {
                        if (word.Length > 3 && ieWords.Any(w => w.Contains(word) || word.Contains(w)))
                            matchingWords++;
                    }
                    if (matchingWords > 0)
                        score += Math.Min(20, matchingWords * 5);
                }
            }

            return score;
        }

        /// <summary>
        /// Banka hareketi için eşleşme önerileri getir
        /// </summary>
        public List<(IncomeExpenseTransaction Transaction, decimal Score)> GetMatchSuggestions(
            int bankTransactionId, int companyId, int bankAccountId)
        {
            var suggestions = new List<(IncomeExpenseTransaction, decimal)>();
            try
            {
                var bankTx = _bankTransactionRepository.GetById(bankTransactionId);
                if (bankTx == null) return suggestions;

                var rules = _ruleRepository.GetByCompanyId(companyId);
                decimal amountTolerance = rules.Count > 0 ? rules[0].AmountTolerance : 0.01m;
                int dateTolerance = rules.Count > 0 ? rules[0].DateTolerance : 3;

                var candidates = _incomeExpenseRepository.GetAll()
                    .Where(t => t.CompanyId == companyId)
                    .ToList();

                foreach (var ieTx in candidates)
                {
                    decimal score = CalculateMatchScore(bankTx, ieTx, amountTolerance, dateTolerance);
                    if (score >= 30)
                        suggestions.Add((ieTx, score));
                }

                suggestions.Sort((a, b) => b.Item2.CompareTo(a.Item2));
                return suggestions.Take(10).ToList();
            }
            catch (Exception ex)
            {
                _logger.Error("Eşleşme önerileri alınırken hata", ex);
                return suggestions;
            }
        }

        public TransactionMatch GetMatchByBankTransactionId(int bankTransactionId)
        {
            try { return _matchingRepository.GetByBankTransactionId(bankTransactionId); }
            catch (Exception ex) { _logger.Error("Eşleşme alınırken hata", ex); return null; }
        }

        public List<TransactionMatch> GetAllMatches()
        {
            try { return _matchingRepository.GetAll(); }
            catch (Exception ex) { _logger.Error("Eşleşmeler alınırken hata", ex); return new List<TransactionMatch>(); }
        }

        public List<MatchingRule> GetRulesByCompany(int companyId)
        {
            try { return _ruleRepository.GetByCompanyId(companyId); }
            catch (Exception ex) { _logger.Error("Kurallar alınırken hata", ex); return new List<MatchingRule>(); }
        }

        public MatchingRule CreateRule(int companyId, string ruleName, decimal amountTolerance = 0.01m,
            int dateTolerance = 3, string keywordPatterns = "[]", int priority = 1)
        {
            try
            {
                var rule = new MatchingRule
                {
                    CompanyId = companyId,
                    RuleName = ruleName,
                    AmountTolerance = amountTolerance,
                    DateTolerance = dateTolerance,
                    KeywordPatterns = keywordPatterns,
                    IsActive = true,
                    Priority = priority
                };
                int id = _ruleRepository.Add(rule);
                rule.RuleId = id;
                return rule;
            }
            catch (Exception ex) { _logger.Error("Kural oluşturma hatası", ex); return null; }
        }
    }
}
