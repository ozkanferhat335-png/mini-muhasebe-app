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
        /// API bağlantısını test et
        /// </summary>
        bool TestConnection(BankAccount account);

        /// <summary>
        /// Belirtilen tarih aralığındaki banka hareketlerini çek
        /// </summary>
        List<BankTransaction> FetchTransactions(BankAccount account, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Güncel hesap bakiyesini çek
        /// </summary>
        decimal? FetchCurrentBalance(BankAccount account);

        /// <summary>
        /// API sağlayıcı adı
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Desteklenen API tipi (REST, SOAP, OpenBanking)
        /// </summary>
        string ApiType { get; }
    }

    /// <summary>
    /// Banka API yanıt modeli
    /// </summary>
    public class BankApiResponse
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string RawResponse { get; set; }
        public int StatusCode { get; set; }
    }

    /// <summary>
    /// Banka hareketi API modeli (ham veri)
    /// </summary>
    public class BankApiTransaction
    {
        public string ExternalId { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public decimal? Balance { get; set; }
        public string ReferenceNumber { get; set; }
        public string TransactionType { get; set; } // Debit / Credit
    }
}
