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
        string ProviderName { get; }
        string ProviderType { get; } // REST, SOAP, OpenBanking

        /// <summary>
        /// API bağlantısını test eder
        /// </summary>
        bool TestConnection(BankAccount bankAccount);

        /// <summary>
        /// Belirtilen tarih aralığındaki banka hareketlerini çeker
        /// </summary>
        List<BankTransaction> FetchTransactions(BankAccount bankAccount, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Güncel bakiyeyi çeker
        /// </summary>
        decimal? FetchCurrentBalance(BankAccount bankAccount);

        /// <summary>
        /// Ödeme gönderir (destekleniyorsa)
        /// </summary>
        bool SendPayment(BankAccount fromAccount, string toIban, decimal amount, string description);
    }

    /// <summary>
    /// API bağlantı sonucu
    /// </summary>
    public class ApiResult<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string ErrorMessage { get; set; }
        public int StatusCode { get; set; }

        public static ApiResult<T> Ok(T data) => new ApiResult<T> { Success = true, Data = data, StatusCode = 200 };
        public static ApiResult<T> Fail(string error, int statusCode = 500) => new ApiResult<T> { Success = false, ErrorMessage = error, StatusCode = statusCode };
    }
}
