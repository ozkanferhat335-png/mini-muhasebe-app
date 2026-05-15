using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using MiniMuhasebe.Integration.Interfaces;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Integration.BankingAPIs
{
    /// <summary>
    /// Genel REST tabanlı banka API istemcisi
    /// </summary>
    public class BankApiClient : IBankApiProvider
    {
        private HttpClient _httpClient;
        private BankApiSettings _settings;
        private string _accessToken;
        private DateTime _tokenExpiry;

        public string ProviderName => "Generic REST Bank API";
        public string ApiType => "REST";

        public BankApiClient()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// API'ye bağlan
        /// </summary>
        public bool Connect(BankApiSettings settings)
        {
            try
            {
                _settings = settings ?? throw new ArgumentNullException(nameof(settings));

                if (string.IsNullOrEmpty(settings.ApiBaseUrl))
                    throw new ArgumentException("API Base URL boş olamaz.");

                _httpClient.BaseAddress = new Uri(settings.ApiBaseUrl);
                _httpClient.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // API Key varsa header'a ekle
                if (!string.IsNullOrEmpty(settings.ApiKey))
                    _httpClient.DefaultRequestHeaders.Add("X-API-Key", settings.ApiKey);

                // OAuth2 / Bearer token al
                if (!string.IsNullOrEmpty(settings.ApiClientId) && !string.IsNullOrEmpty(settings.ApiClientSecret))
                {
                    return ObtainAccessToken();
                }

                // Basic Auth
                if (!string.IsNullOrEmpty(settings.ApiUsername) && !string.IsNullOrEmpty(settings.ApiPassword))
                {
                    string credentials = Convert.ToBase64String(
                        Encoding.ASCII.GetBytes($"{settings.ApiUsername}:{settings.ApiPassword}"));
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Basic", credentials);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API bağlantı hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// OAuth2 access token al
        /// </summary>
        private bool ObtainAccessToken()
        {
            try
            {
                var tokenRequest = new FormUrlEncodedContent(new[]
                {
                    new System.Collections.Generic.KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new System.Collections.Generic.KeyValuePair<string, string>("client_id", _settings.ApiClientId),
                    new System.Collections.Generic.KeyValuePair<string, string>("client_secret", _settings.ApiClientSecret)
                });

                var response = _httpClient.PostAsync("/oauth/token", tokenRequest).Result;
                if (response.IsSuccessStatusCode)
                {
                    string content = response.Content.ReadAsStringAsync().Result;
                    // Basit JSON parsing (Newtonsoft.Json olmadan)
                    _accessToken = ExtractJsonValue(content, "access_token");
                    string expiresIn = ExtractJsonValue(content, "expires_in");
                    if (int.TryParse(expiresIn, out int seconds))
                        _tokenExpiry = DateTime.Now.AddSeconds(seconds - 60);
                    else
                        _tokenExpiry = DateTime.Now.AddHours(1);

                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _accessToken);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Bağlantıyı test et
        /// </summary>
        public bool TestConnection(BankApiSettings settings)
        {
            try
            {
                bool connected = Connect(settings);
                if (!connected) return false;

                // Basit ping isteği
                var response = _httpClient.GetAsync("/api/v1/ping").Result;
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Hareketleri çek
        /// </summary>
        public List<BankTransactionDto> FetchTransactions(string accountId, DateTime startDate, DateTime endDate, int pageSize = 100)
        {
            var transactions = new List<BankTransactionDto>();

            try
            {
                EnsureTokenValid();

                string url = $"/api/v1/accounts/{accountId}/transactions" +
                             $"?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}&pageSize={pageSize}";

                var response = _httpClient.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Hareket çekme hatası: {response.StatusCode}");
                    return transactions;
                }

                string content = response.Content.ReadAsStringAsync().Result;
                transactions = ParseTransactions(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hareket çekme sırasında hata: {ex.Message}");
            }

            return transactions;
        }

        /// <summary>
        /// Hesap listesini getir
        /// </summary>
        public List<BankAccountDto> GetAccounts()
        {
            var accounts = new List<BankAccountDto>();

            try
            {
                EnsureTokenValid();

                var response = _httpClient.GetAsync("/api/v1/accounts").Result;
                if (!response.IsSuccessStatusCode)
                    return accounts;

                string content = response.Content.ReadAsStringAsync().Result;
                accounts = ParseAccounts(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hesap listesi alınırken hata: {ex.Message}");
            }

            return accounts;
        }

        /// <summary>
        /// Hesap bakiyesini getir
        /// </summary>
        public decimal GetBalance(string accountId)
        {
            try
            {
                EnsureTokenValid();

                var response = _httpClient.GetAsync($"/api/v1/accounts/{accountId}/balance").Result;
                if (!response.IsSuccessStatusCode)
                    return 0;

                string content = response.Content.ReadAsStringAsync().Result;
                string balanceStr = ExtractJsonValue(content, "balance");
                return decimal.TryParse(balanceStr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal balance) ? balance : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Bağlantıyı kapat
        /// </summary>
        public void Disconnect()
        {
            _accessToken = null;
            _httpClient?.DefaultRequestHeaders.Authorization = null;
        }

        /// <summary>
        /// Token geçerliliğini kontrol et, gerekirse yenile
        /// </summary>
        private void EnsureTokenValid()
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.Now >= _tokenExpiry)
            {
                ObtainAccessToken();
            }
        }

        /// <summary>
        /// JSON'dan basit değer çıkarma (harici kütüphane gerektirmez)
        /// </summary>
        private string ExtractJsonValue(string json, string key)
        {
            string searchKey = $"\"{key}\"";
            int keyIndex = json.IndexOf(searchKey, StringComparison.OrdinalIgnoreCase);
            if (keyIndex < 0) return null;

            int colonIndex = json.IndexOf(':', keyIndex + searchKey.Length);
            if (colonIndex < 0) return null;

            int valueStart = colonIndex + 1;
            while (valueStart < json.Length && json[valueStart] == ' ') valueStart++;

            if (valueStart >= json.Length) return null;

            if (json[valueStart] == '"')
            {
                int valueEnd = json.IndexOf('"', valueStart + 1);
                return valueEnd > valueStart ? json.Substring(valueStart + 1, valueEnd - valueStart - 1) : null;
            }
            else
            {
                int valueEnd = valueStart;
                while (valueEnd < json.Length && json[valueEnd] != ',' && json[valueEnd] != '}')
                    valueEnd++;
                return json.Substring(valueStart, valueEnd - valueStart).Trim();
            }
        }

        /// <summary>
        /// JSON hareket listesini parse et (basit implementasyon)
        /// </summary>
        private List<BankTransactionDto> ParseTransactions(string json)
        {
            // Gerçek uygulamada Newtonsoft.Json veya System.Text.Json kullanılır
            // Bu implementasyon banka API'sine göre özelleştirilmelidir
            return new List<BankTransactionDto>();
        }

        /// <summary>
        /// JSON hesap listesini parse et
        /// </summary>
        private List<BankAccountDto> ParseAccounts(string json)
        {
            return new List<BankAccountDto>();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// Mock/Test banka API istemcisi (geliştirme ve test için)
    /// </summary>
    public class MockBankApiClient : IBankApiProvider
    {
        public string ProviderName => "Mock Bank API (Test)";
        public string ApiType => "Mock";

        private static readonly Random _random = new Random();

        public bool Connect(BankApiSettings settings) => true;

        public bool TestConnection(BankApiSettings settings) => true;

        public List<BankTransactionDto> FetchTransactions(string accountId, DateTime startDate, DateTime endDate, int pageSize = 100)
        {
            var transactions = new List<BankTransactionDto>();
            var current = startDate;

            while (current <= endDate)
            {
                // Rastgele 0-3 hareket oluştur
                int count = _random.Next(0, 4);
                for (int i = 0; i < count; i++)
                {
                    bool isCredit = _random.Next(0, 2) == 1;
                    decimal amount = Math.Round((decimal)(_random.NextDouble() * 10000 + 100), 2);

                    transactions.Add(new BankTransactionDto
                    {
                        ExternalId = $"MOCK-{accountId}-{current:yyyyMMdd}-{i}",
                        TransactionDate = current,
                        Amount = amount,
                        Description = isCredit ? $"Tahsilat {current:dd.MM.yyyy}" : $"Ödeme {current:dd.MM.yyyy}",
                        TransactionType = isCredit ? "Credit" : "Debit",
                        ReferenceNumber = $"REF{_random.Next(100000, 999999)}"
                    });
                }
                current = current.AddDays(1);
            }

            return transactions;
        }

        public List<BankAccountDto> GetAccounts()
        {
            return new List<BankAccountDto>
            {
                new BankAccountDto
                {
                    AccountId = "MOCK-ACC-001",
                    AccountName = "Test Hesabı",
                    IBAN = "TR000000000000000000000001",
                    Currency = "TRY",
                    Balance = 50000m
                }
            };
        }

        public decimal GetBalance(string accountId) => 50000m;

        public void Disconnect() { }
    }

    /// <summary>
    /// Banka API sağlayıcı fabrikası
    /// </summary>
    public static class BankApiProviderFactory
    {
        /// <summary>
        /// API tipine göre uygun sağlayıcıyı döndür
        /// </summary>
        public static IBankApiProvider Create(string apiProviderType, bool useMock = false)
        {
            if (useMock)
                return new MockBankApiClient();

            switch (apiProviderType?.ToUpperInvariant())
            {
                case "REST":
                case "OPENBANKING":
                    return new BankApiClient();
                case "MOCK":
                    return new MockBankApiClient();
                default:
                    return new BankApiClient();
            }
        }
    }
}
