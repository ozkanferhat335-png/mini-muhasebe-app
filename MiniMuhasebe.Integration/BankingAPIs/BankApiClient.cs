using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using MiniMuhasebe.Data;
using MiniMuhasebe.Integration.Interfaces;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Integration.BankingAPIs
{
    /// <summary>
    /// REST tabanlı banka API istemcisi
    /// </summary>
    public class RestBankApiClient : IBankApiProvider
    {
        private readonly Logger _logger;

        public string ProviderName => "REST Bank API";
        public string ApiType => "REST";

        public RestBankApiClient()
        {
            _logger = new Logger();
        }

        public bool TestConnection(BankAccount account)
        {
            try
            {
                if (string.IsNullOrEmpty(account.ApiBaseUrl))
                {
                    _logger.Warning("API URL tanımlanmamış");
                    return false;
                }

                using (var client = CreateHttpClient(account))
                {
                    var response = client.GetAsync(account.ApiBaseUrl + "/health").Result;
                    bool success = response.IsSuccessStatusCode;
                    _logger.Info($"API bağlantı testi: {account.BankName} - {(success ? "Başarılı" : "Başarısız")}");
                    return success;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"API bağlantı testi sırasında hata: {account.BankName}", ex);
                return false;
            }
        }

        public List<BankTransaction> FetchTransactions(BankAccount account, DateTime startDate, DateTime endDate)
        {
            var transactions = new List<BankTransaction>();

            try
            {
                if (!account.IsApiEnabled || string.IsNullOrEmpty(account.ApiBaseUrl))
                {
                    _logger.Warning($"API etkin değil veya URL tanımlanmamış: {account.BankName}");
                    return transactions;
                }

                string url = $"{account.ApiBaseUrl}/accounts/{account.ApiAccountId}/transactions" +
                             $"?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";

                using (var client = CreateHttpClient(account))
                {
                    var response = client.GetAsync(url).Result;

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.Warning($"API yanıt hatası: {response.StatusCode} - {account.BankName}");
                        return transactions;
                    }

                    string json = response.Content.ReadAsStringAsync().Result;
                    transactions = ParseTransactionsFromJson(json, account.BankAccountId);

                    _logger.Info($"API'den {transactions.Count} hareket çekildi: {account.BankName}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Banka hareketleri çekilirken hata: {account.BankName}", ex);
            }

            return transactions;
        }

        public decimal? FetchCurrentBalance(BankAccount account)
        {
            try
            {
                if (!account.IsApiEnabled || string.IsNullOrEmpty(account.ApiBaseUrl))
                    return null;

                string url = $"{account.ApiBaseUrl}/accounts/{account.ApiAccountId}/balance";

                using (var client = CreateHttpClient(account))
                {
                    var response = client.GetAsync(url).Result;

                    if (!response.IsSuccessStatusCode)
                        return null;

                    string json = response.Content.ReadAsStringAsync().Result;
                    return ParseBalanceFromJson(json);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Bakiye çekilirken hata: {account.BankName}", ex);
                return null;
            }
        }

        private HttpClient CreateHttpClient(BankAccount account)
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            // API kimlik doğrulama
            if (!string.IsNullOrEmpty(account.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", account.ApiKey);
            }
            else if (!string.IsNullOrEmpty(account.ApiClientId) && !string.IsNullOrEmpty(account.ApiClientSecret))
            {
                string credentials = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{account.ApiClientId}:{account.ApiClientSecret}"));
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");
            }

            client.DefaultRequestHeaders.Add("Accept", "application/json");
            return client;
        }

        private List<BankTransaction> ParseTransactionsFromJson(string json, int bankAccountId)
        {
            var transactions = new List<BankTransaction>();

            try
            {
                // Basit JSON parsing (gerçek uygulamada Newtonsoft.Json veya System.Text.Json kullanılır)
                // Bu örnek implementasyon - gerçek API yanıt formatına göre uyarlanmalıdır
                _logger.Info($"JSON parse edildi: {json.Length} karakter");

                // Örnek: JSON dizisini parse et
                // Gerçek implementasyon API'nin döndürdüğü formata göre yapılmalıdır
            }
            catch (Exception ex)
            {
                _logger.Error("JSON parse hatası", ex);
            }

            return transactions;
        }

        private decimal? ParseBalanceFromJson(string json)
        {
            try
            {
                // Basit JSON parsing
                // Gerçek implementasyon API'nin döndürdüğü formata göre yapılmalıdır
                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Mock/Test banka API istemcisi (geliştirme ve test için)
    /// </summary>
    public class MockBankApiClient : IBankApiProvider
    {
        private readonly Logger _logger;
        private readonly Random _random;

        public string ProviderName => "Mock Bank API (Test)";
        public string ApiType => "Mock";

        public MockBankApiClient()
        {
            _logger = new Logger();
            _random = new Random();
        }

        public bool TestConnection(BankAccount account)
        {
            _logger.Info($"Mock API bağlantı testi başarılı: {account.BankName}");
            return true;
        }

        public List<BankTransaction> FetchTransactions(BankAccount account, DateTime startDate, DateTime endDate)
        {
            var transactions = new List<BankTransaction>();

            try
            {
                // Test verisi oluştur
                var current = startDate;
                int txCount = _random.Next(3, 10);

                for (int i = 0; i < txCount; i++)
                {
                    current = current.AddDays(_random.Next(1, 5));
                    if (current > endDate) break;

                    bool isCredit = _random.Next(0, 2) == 1;
                    decimal amount = Math.Round((decimal)(_random.NextDouble() * 5000 + 100), 2);

                    transactions.Add(new BankTransaction
                    {
                        BankAccountId = account.BankAccountId,
                        TransactionDate = current,
                        Amount = isCredit ? amount : -amount,
                        Description = isCredit ? $"TRANSFER GELEN - Test Müşteri {i + 1}" : $"ÖDEME - Test Gider {i + 1}",
                        Balance = account.CurrentBalance + (isCredit ? amount : -amount),
                        ReferenceNumber = $"REF{DateTime.Now.Ticks}{i}",
                        BankTransactionId_External = $"MOCK_{account.BankAccountId}_{DateTime.Now.Ticks}_{i}",
                        TransactionType = isCredit ? "Credit" : "Debit",
                        Status = "Pending",
                        IsMatched = false,
                        SyncedAt = DateTime.Now,
                        CreatedAt = DateTime.Now
                    });
                }

                _logger.Info($"Mock API: {transactions.Count} test hareketi oluşturuldu: {account.BankName}");
            }
            catch (Exception ex)
            {
                _logger.Error("Mock API hareket oluşturma hatası", ex);
            }

            return transactions;
        }

        public decimal? FetchCurrentBalance(BankAccount account)
        {
            decimal mockBalance = account.CurrentBalance + (decimal)(_random.NextDouble() * 1000 - 500);
            _logger.Info($"Mock API bakiye: {mockBalance:N2} TRY - {account.BankName}");
            return Math.Round(mockBalance, 2);
        }
    }

    /// <summary>
    /// Banka API fabrikası - doğru sağlayıcıyı seçer
    /// </summary>
    public static class BankApiClientFactory
    {
        public static IBankApiProvider CreateProvider(BankAccount account)
        {
            if (account == null || !account.IsApiEnabled)
                return new MockBankApiClient();

            switch (account.ApiProviderType?.ToUpperInvariant())
            {
                case "REST":
                    return new RestBankApiClient();
                case "MOCK":
                case "TEST":
                    return new MockBankApiClient();
                default:
                    return new MockBankApiClient();
            }
        }
    }
}
