using System;
using System.Collections.Generic;
using MiniMuhasebe.Integration.Interfaces;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Integration.BankingAPIs
{
    /// <summary>
    /// Banka API istemcisi - farklı sağlayıcıları yönetir
    /// </summary>
    public class BankApiClient
    {
        private readonly Dictionary<string, IBankApiProvider> _providers;

        public BankApiClient()
        {
            _providers = new Dictionary<string, IBankApiProvider>(StringComparer.OrdinalIgnoreCase)
            {
                { "Mock", new MockBankApiProvider() },
                { "REST", new RestBankApiProvider() }
            };
        }

        /// <summary>
        /// Banka hesabına uygun sağlayıcıyı döndürür
        /// </summary>
        public IBankApiProvider GetProvider(BankAccount bankAccount)
        {
            if (!bankAccount.IsApiEnabled)
                return null;

            string providerType = bankAccount.ApiProviderType ?? "Mock";

            // Test/demo modunda Mock kullan
            if (string.IsNullOrEmpty(bankAccount.ApiBaseUrl) || bankAccount.ApiBaseUrl.Contains("mock"))
                return _providers["Mock"];

            if (_providers.ContainsKey(providerType))
                return _providers[providerType];

            return _providers["Mock"]; // Fallback
        }

        /// <summary>
        /// Banka hareketlerini çeker ve mükerrer kayıtları filtreler
        /// </summary>
        public List<BankTransaction> SyncTransactions(BankAccount bankAccount, DateTime startDate, DateTime endDate,
            List<string> existingExternalIds)
        {
            var provider = GetProvider(bankAccount);
            if (provider == null)
                return new List<BankTransaction>();

            var allTransactions = provider.FetchTransactions(bankAccount, startDate, endDate);
            var newTransactions = new List<BankTransaction>();

            foreach (var transaction in allTransactions)
            {
                // Mükerrer kayıt kontrolü
                if (!string.IsNullOrEmpty(transaction.BankTransactionId_External) &&
                    existingExternalIds.Contains(transaction.BankTransactionId_External))
                    continue;

                newTransactions.Add(transaction);
            }

            return newTransactions;
        }

        /// <summary>
        /// API bağlantısını test eder
        /// </summary>
        public bool TestConnection(BankAccount bankAccount)
        {
            var provider = GetProvider(bankAccount);
            return provider?.TestConnection(bankAccount) ?? false;
        }

        /// <summary>
        /// Güncel bakiyeyi çeker
        /// </summary>
        public decimal? GetCurrentBalance(BankAccount bankAccount)
        {
            var provider = GetProvider(bankAccount);
            return provider?.FetchCurrentBalance(bankAccount);
        }

        /// <summary>
        /// Yeni sağlayıcı ekler
        /// </summary>
        public void RegisterProvider(string name, IBankApiProvider provider)
        {
            _providers[name] = provider;
        }

        /// <summary>
        /// Mevcut sağlayıcı listesini döndürür
        /// </summary>
        public IEnumerable<string> GetAvailableProviders()
        {
            return _providers.Keys;
        }
    }
}
