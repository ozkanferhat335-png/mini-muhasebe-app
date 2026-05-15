using System;
using System.Collections.Generic;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Integration.Interfaces
{
    /// <summary>
    /// Banka API sağlayıcısı arayüzü
    /// </summary>
    public interface IBankApiProvider
    {
        /// <summary>
        /// API'ye bağlan ve kimlik doğrulaması yap
        /// </summary>
        bool Connect(BankApiSettings settings);

        /// <summary>
        /// Bağlantıyı test et
        /// </summary>
        bool TestConnection(BankApiSettings settings);

        /// <summary>
        /// Belirtilen tarih aralığındaki hareketleri çek
        /// </summary>
        List<BankTransactionDto> FetchTransactions(string accountId, DateTime startDate, DateTime endDate, int pageSize = 100);

        /// <summary>
        /// Hesap listesini getir
        /// </summary>
        List<BankAccountDto> GetAccounts();

        /// <summary>
        /// Hesap bakiyesini getir
        /// </summary>
        decimal GetBalance(string accountId);

        /// <summary>
        /// Bağlantıyı kapat
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Sağlayıcı adı
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Desteklenen API tipi
        /// </summary>
        string ApiType { get; }
    }

    /// <summary>
    /// Banka API ayarları
    /// </summary>
    public class BankApiSettings
    {
        public string ApiBaseUrl { get; set; }
        public string ApiClientId { get; set; }
        public string ApiClientSecret { get; set; }
        public string ApiKey { get; set; }
        public string ApiUsername { get; set; }
        public string ApiPassword { get; set; }
        public string ApiAccountId { get; set; }
        public string ApiProviderType { get; set; } // REST, SOAP, OpenBanking
        public int TimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Banka hareketi DTO (API'den gelen ham veri)
    /// </summary>
    public class BankTransactionDto
    {
        public string ExternalId { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public decimal? Balance { get; set; }
        public string ReferenceNumber { get; set; }
        public string TransactionType { get; set; } // Debit / Credit
    }

    /// <summary>
    /// Banka hesabı DTO (API'den gelen)
    /// </summary>
    public class BankAccountDto
    {
        public string AccountId { get; set; }
        public string AccountName { get; set; }
        public string IBAN { get; set; }
        public string Currency { get; set; }
        public decimal Balance { get; set; }
    }
}
