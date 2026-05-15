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
        private readonly TransactionMatchRepository _matchRepository;
        private readonly BankTransactionRepository _bankTransactionRepository;
        private readonly IncomeExpenseTransactionRepository _incomeExpenseRepository;
        private readonly MatchingRuleRepository _matchingRuleRepository;
        private readonly Logger _logger;

        public MatchingService(string connectionString)
        {
            _matchRepository = new TransactionMatchRepository(connectionString);
            _bankTransactionRepository = new BankTransactionRepository(connectionString);
            _incomeExpenseRepository = new IncomeExpenseTransactionRepository(connectionString);
            _matchingRuleRepository = new MatchingRuleRepository(connectionString);
            _logger = new Logger();
        }

        /// <summary>
        /// Manuel eşleştirme: Banka hareketi ile gelir/gider kaydını eşleştir
        /// </summary>
        public TransactionMatch ManualMatch(int bankTransactionId, int incomeExpenseTransactionId, int? createdBy = null)
        {
            try
            {
                // Zaten eşleştirilmiş mi kontrol et
                if (_matchRepository.MatchExists(bankTransactionId, incomeExpenseTransactionId))
                {
                    _logger.Warning($"Eşleştirme zaten mevcut: BankTx={bankTransactionId}, IETx={incomeExpenseTransactionId}");
                    return null;
                }

                var match = new TransactionMatch
                {
                    BankTransactionId = bankTransactionId,
                    IncomeExpenseTransactionId = incomeExpenseTransactionId,
                    MatchScore = 100m,
                    MatchType = "Manual",
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now
                };

                int matchId = _matchRepository.Add(match);
                match.MatchId = matchId;

                // Banka hareketini eşleştirildi olarak işaretle
                var bankTx = _bankTransactionRepository.GetById(bankTransactionId);
                if (bankTx != null)
                {
                    bankTx.IsMatched = true;
                    bankTx.Status = "Matched";
                    _bankTransactionRepository.Update(bankTx);
                }

                _logger.Info($"Manuel eşleştirme yapıldı: BankTx={bankTransactionId}, IETx={incomeExpenseTransactionId}");
                return match;
            }
            catch (Exception ex)
            {
                _logger.Error("Manuel eşleştirme sırasında hata", ex);
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
                var match = _matchRepository.GetById(matchId);
                if (match == null) return false;

                bool deleted = _matchRepository.Delete(matchId);
                if (deleted)
                {
                    // Banka hareketini eşleştirilmedi olarak işaretle
                    var bankTx = _bankTransactionRepository.GetById(match.BankTransactionId);
                    if (bankTx != null)
                    {
                        bankTx.IsMatched = false;
                        bankTx.Status = "Pending";
                        _bankTransactionRepository.Update(bankTx);
                    }
                    _logger.Info($"Eşleştirme kaldırıldı: MatchId={matchId}");
                }
                return deleted;
            }
            catch (Exception ex)
            {
                _logger.Error("Eşleştirme kaldırma sırasında hata", ex);
                return false;
            }
        }

        /// <summary>
        /// Otomatik eşleştirme: Tüm eşleşmeyen banka hareketlerini kural bazlı eşleştir
        /// </summary>
        public AutoMatchResult AutoMatch(int companyId, int bankAccountId, int? createdBy = null)
        {
            var result = new AutoMatchResult();

            try
            {
                var rules = _matchingRuleRepository.GetByCompanyId(companyId);
                if (rules.Count == 0)
                {
                    // Varsayılan kural
                    rules.Add(new MatchingRule
                    {
                        AmountTolerance = 0.01m,
                        DateTolerance = 3,
                        KeywordPatterns = "[]"
                    });
                }

                var unmatchedBankTx = _bankTransactionRepository.GetUnmatchedByBankAccount(bankAccountId);
                var allIncomeTx = _incomeExpenseRepository.GetAll()
                    .Where(t => t.CompanyId == companyId).ToList();

                foreach (var bankTx in unmatchedBankTx)
                {
                    result.TotalProcessed++;
                    bool matched = false;

                    foreach (var rule in rules.OrderBy(r => r.Priority))
                    {
                        var candidates = FindCandidates(bankTx, allIncomeTx, rule);
                        if (candidates.Count > 0)
                        {
                            var best = candidates.OrderByDescending(c => c.score).First();
                            if (best.score >= 60m)
                            {
                                var match = new TransactionMatch
                                {
                                    BankTransactionId = bankTx.BankTransactionId,
                                    IncomeExpenseTransactionId = best.transaction.TransactionId,
                                    MatchScore = best.score,
                                    MatchType = "Automatic",
                                    CreatedBy = createdBy,
                                    CreatedAt = DateTime.Now
                                };

                                if (!_matchRepository.MatchExists(bankTx.BankTransactionId, best.transaction.TransactionId))
                                {
                                    _matchRepository.Add(match);
                                    bankTx.IsMatched = true;
                                    bankTx.Status = "Matched";
                                    _bankTransactionRepository.Update(bankTx);
                                    result.MatchedCount++;
                                    matched = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!matched)
                        result.UnmatchedCount++;
                }

                _logger.Info($"Otomatik eşleştirme tamamlandı: {result.MatchedCount}/{result.TotalProcessed} eşleştirildi.");
            }
            catch (Exception ex)
            {
                _logger.Error("Otomatik eşleştirme sırasında hata", ex);
            }

            return result;
        }

        /// <summary>
        /// Kural bazlı aday eşleştirme hesapla
        /// </summary>
        private List<(IncomeExpenseTransaction transaction, decimal score)> FindCandidates(
            BankTransaction bankTx,
            List<IncomeExpenseTransaction> candidates,
            MatchingRule rule)
        {
            var results = new List<(IncomeExpenseTransaction, decimal)>();

            foreach (var tx in candidates)
            {
                decimal score = 0;

                // Tutar eşleştirmesi (40 puan)
                decimal amountDiff = Math.Abs(Math.Abs(bankTx.Amount) - tx.Amount);
                if (amountDiff <= rule.AmountTolerance)
                    score += 40;
                else if (amountDiff <= rule.AmountTolerance * 10)
                    score += 20;

                // Tarih eşleştirmesi (30 puan)
                int dateDiff = Math.Abs((bankTx.TransactionDate - tx.TransactionDate).Days);
                if (dateDiff == 0)
                    score += 30;
                else if (dateDiff <= rule.DateTolerance)
                    score += 30 - (dateDiff * 5);

                // Açıklama eşleştirmesi (30 puan)
                if (!string.IsNullOrEmpty(bankTx.Description) && !string.IsNullOrEmpty(tx.Description))
                {
                    string bankDesc = bankTx.Description.ToLowerInvariant();
                    string txDesc = tx.Description.ToLowerInvariant();

                    if (bankDesc.Contains(txDesc) || txDesc.Contains(bankDesc))
                        score += 30;
                    else
                    {
                        // Kelime bazlı eşleştirme
                        var bankWords = bankDesc.Split(new[] { ' ', '-', '_', '/' }, StringSplitOptions.RemoveEmptyEntries);
                        var txWords = txDesc.Split(new[] { ' ', '-', '_', '/' }, StringSplitOptions.RemoveEmptyEntries);
                        int commonWords = 0;
                        foreach (var w in bankWords)
                            if (w.Length > 2 && txWords.Any(tw => tw.Contains(w) || w.Contains(tw)))
                                commonWords++;
                        if (commonWords > 0)
                            score += Math.Min(30, commonWords * 10);
                    }
                }

                if (score > 0)
                    results.Add((tx, score));
            }

            return results;
        }

        /// <summary>
        /// Eşleştirme kuralı oluştur
        /// </summary>
        public MatchingRule CreateMatchingRule(int companyId, string ruleName,
            decimal amountTolerance = 0.01m, int dateTolerance = 3,
            string keywordPatterns = "[]", int priority = 1)
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
                    Priority = priority,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                int ruleId = _matchingRuleRepository.Add(rule);
                rule.RuleId = ruleId;

                _logger.Info($"Eşleştirme kuralı oluşturuldu: {ruleName} (ID: {ruleId})");
                return rule;
            }
            catch (Exception ex)
            {
                _logger.Error("Eşleştirme kuralı oluşturma sırasında hata", ex);
                return null;
            }
        }

        /// <summary>
        /// Firmaya ait eşleştirme kurallarını getir
        /// </summary>
        public List<MatchingRule> GetMatchingRules(int companyId)
        {
            try
            {
                return _matchingRuleRepository.GetByCompanyId(companyId);
            }
            catch (Exception ex)
            {
                _logger.Error("Eşleştirme kuralları alınırken hata", ex);
                return new List<MatchingRule>();
            }
        }

        /// <summary>
        /// Banka hareketine ait eşleştirmeyi getir
        /// </summary>
        public TransactionMatch GetMatchByBankTransaction(int bankTransactionId)
        {
            try
            {
                return _matchRepository.GetByBankTransactionId(bankTransactionId);
            }
            catch (Exception ex)
            {
                _logger.Error("Eşleştirme alınırken hata", ex);
                return null;
            }
        }
    }

    /// <summary>
    /// Otomatik eşleştirme sonucu
    /// </summary>
    public class AutoMatchResult
    {
        public int TotalProcessed { get; set; }
        public int MatchedCount { get; set; }
        public int UnmatchedCount { get; set; }
    }
}
