using System;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.UI
{
    /// <summary>
    /// Uygulama oturumu - aktif kullanıcı, firma ve dönem bilgilerini tutar
    /// </summary>
    public static class AppSession
    {
        public static User CurrentUser { get; set; }
        public static Company CurrentCompany { get; set; }
        public static FiscalPeriod CurrentPeriod { get; set; }
        public static string ConnectionString { get; set; }
        public static string EncryptionKey { get; set; } = "MiniMuhasebe2026SecretKey!@#$%^&*";

        public static bool IsAdmin => CurrentUser?.RoleId == 1;
        public static bool IsLoggedIn => CurrentUser != null;

        public static void Clear()
        {
            CurrentUser = null;
            CurrentCompany = null;
            CurrentPeriod = null;
        }

        public static string GetDisplayInfo()
        {
            string company = CurrentCompany?.CompanyName ?? "Firma Seçilmedi";
            string period = CurrentPeriod?.PeriodName ?? "Dönem Seçilmedi";
            string user = CurrentUser?.Username ?? "";
            return $"{company} | {period} | {user}";
        }
    }
}
