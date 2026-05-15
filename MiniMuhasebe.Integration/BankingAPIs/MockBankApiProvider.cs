using System;
using System.Collections.Generic;
using MiniMuhasebe.Integration.Interfaces;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Integration.BankingAPIs
{
    /// <summary>
    /// Test/Demo amaçlı sahte banka API sağlayıcısı
    /// Gerçek API entegrasyonu olmadan sistemi test etmek için kullanılır
    /// </summary>
    public class MockBankApiProvider : IBankApiProvider
    {
        public string ProviderName => "Mock Bank API (Test)";
        public string ProviderType => "REST";

        private static readonly Random _random = new Random();

        public bool TestConnection(BankAccount bankAccount)
        {
            // Simüle edilmiş bağlantı testi - her zaman başarılı
            System.Threading.Thread.Sleep(500); // Gerçekçi gecikme
            return true;
        }

        public List<BankTransaction> FetchTransactions(BankAccount bankAccount, DateTime startDate, DateTime endDate)
        {
            var transactions = new List<BankTransaction>();
            decimal runningBalance = bankAccount.CurrentBalance;

            // Tarih aralığında rastgele işlemler oluştur
            var currentDate = startDate;
            int transactionCount = _random.Next(5, 20);

            for (int i = 0; i < transactionCount; i++)
            {
                currentDate = currentDate.AddDays(_random.Next(0, 3));
                if (currentDate > endDate) break;

                bool isCredit = _random.Next(0, 2) == 1;
                decimal amount = Math.Round((decimal)(_random.NextDouble() * 5000 + 100), 2);
                if (!isCredit) amount = -amount;

                runningBalance += amount;

                var descriptions = isCredit
                    ? new[] { "HAVALE ALINDI", "EFT ALINDI", "FATURA TAHSİLATI", "MÜŞTERİ ÖDEMESİ", "TRANSFER GELDİ" }
                    : new[] { "HAVALE GÖNDERİLDİ", "EFT GÖNDERİLDİ", "FATURA ÖDEMESİ", "KİRA ÖDEMESİ", "PERSONEL MAAŞI" };

                transactions.Add(new BankTransaction
                {
                    BankAccountId = bankAccount.BankAccountId,
                    TransactionDate = currentDate,
                    Amount = Math.Abs(amount),
                    Description = descriptions[_random.Next(descriptions.Length)] + $" - REF{i:D4}",
                    Balance = runningBalance,
                    ReferenceNumber = $"REF{DateTime.Now.Ticks}{i}",
                    BankTransactionId_External = $"MOCK-{bankAccount.BankAccountId}-{currentDate:yyyyMMdd}-{i:D4}",
                    TransactionType = isCredit ? "Credit" : "Debit",
                    Status = "Pending",
                    IsMatched = false,
                    SyncedAt = DateTime.Now,
                    CreatedAt = DateTime.Now
                });
            }

            return transactions;
        }

        public decimal? FetchCurrentBalance(BankAccount bankAccount)
        {
            // Mevcut bakiyeye küçük bir değişim ekle
            decimal change = Math.Round((decimal)(_random.NextDouble() * 1000 - 500), 2);
            return bankAccount.CurrentBalance + change;
        }

        public bool SendPayment(BankAccount fromAccount, string toIban, decimal amount, string description)
        {
            // Simüle edilmiş ödeme - her zaman başarılı
            System.Threading.Thread.Sleep(1000);
            return true;
        }
    }

    /// <summary>
    /// REST tabanlı gerçek banka API istemcisi
    /// </summary>
    public class RestBankApiProvider : IBankApiProvider
    {
        public string ProviderName => "REST Bank API";
        public string ProviderType => "REST";

        public bool TestConnection(BankAccount bankAccount)
        {
            try
            {
                if (string.IsNullOrEmpty(bankAccount.ApiBaseUrl))
                    return false;

                using (var client = new System.Net.WebClient())
                {
                    client.Headers.Add("Authorization", $"Bearer {bankAccount.ApiKey}");
                    client.Headers.Add("X-Client-Id", bankAccount.ApiClientId);
                    // Basit ping endpoint
                    string response = client.DownloadString($"{bankAccount.ApiBaseUrl}/ping");
                    return !string.IsNullOrEmpty(response);
                }
            }
            catch
            {
                return false;
            }
        }

        public List<BankTransaction> FetchTransactions(BankAccount bankAccount, DateTime startDate, DateTime endDate)
        {
            // Gerçek REST API çağrısı - implementasyon bankaya göre değişir
            // Bu örnek implementasyon genel bir yapı göstermektedir
            var transactions = new List<BankTransaction>();

            try
            {
                using (var client = new System.Net.WebClient())
                {
                    client.Headers.Add("Authorization", $"Bearer {bankAccount.ApiKey}");
                    client.Headers.Add("Content-Type", "application/json");

                    string url = $"{bankAccount.ApiBaseUrl}/accounts/{bankAccount.ApiAccountId}/transactions" +
                                 $"?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";

                    string json = client.DownloadString(url);
                    // JSON parse işlemi burada yapılır
                    // Gerçek implementasyonda JSON deserialize edilir
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Hatası: {ex.Message}");
            }

            return transactions;
        }

        public decimal? FetchCurrentBalance(BankAccount bankAccount)
        {
            try
            {
                using (var client = new System.Net.WebClient())
                {
                    client.Headers.Add("Authorization", $"Bearer {bankAccount.ApiKey}");
                    string url = $"{bankAccount.ApiBaseUrl}/accounts/{bankAccount.ApiAccountId}/balance";
                    string json = client.DownloadString(url);
                    // JSON parse
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        public bool SendPayment(BankAccount fromAccount, string toIban, decimal amount, string description)
        {
            // Gerçek ödeme implementasyonu
            return false;
        }
    }
}
