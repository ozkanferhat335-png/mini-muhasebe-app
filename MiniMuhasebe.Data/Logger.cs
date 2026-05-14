using System;
using System.IO;
using System.Text;

namespace MiniMuhasebe.Data
{
    /// <summary>
    /// Loglama işlemleri
    /// </summary>
    public class Logger
    {
        private readonly string _logDirectory;
        private readonly string _logFileName;
        private readonly LogLevel _minLevel;

        public enum LogLevel
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3,
            Critical = 4
        }

        public Logger(string logDirectory = "Logs", string logFileName = "MiniMuhasebe.log", LogLevel minLevel = LogLevel.Info)
        {
            _logDirectory = logDirectory;
            _logFileName = logFileName;
            _minLevel = minLevel;

            // Log klasörünü oluştur
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        /// <summary>
        /// Loglama işlemini gerçekleştirir
        /// </summary>
        private void Log(LogLevel level, string message, Exception ex = null)
        {
            if (level < _minLevel)
                return;

            try
            {
                string logPath = Path.Combine(_logDirectory, _logFileName);
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logMessage = $"[{timestamp}] [{level.ToString().ToUpper()}] {message}";

                if (ex != null)
                {
                    logMessage += Environment.NewLine + $"Exception: {ex.Message}" + Environment.NewLine + $"StackTrace: {ex.StackTrace}";
                }

                lock (this) // Thread-safe yazma
                {
                    File.AppendAllText(logPath, logMessage + Environment.NewLine);
                }

                // Console'a da yaz (geliştirme sırasında)
                Console.WriteLine(logMessage);
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"Log yazma hatası: {logEx.Message}");
            }
        }

        public void Debug(string message) => Log(LogLevel.Debug, message);
        public void Info(string message) => Log(LogLevel.Info, message);
        public void Warning(string message) => Log(LogLevel.Warning, message);
        public void Warning(string message, Exception ex) => Log(LogLevel.Warning, message, ex);
        public void Error(string message) => Log(LogLevel.Error, message);
        public void Error(string message, Exception ex) => Log(LogLevel.Error, message, ex);
        public void Critical(string message) => Log(LogLevel.Critical, message);
        public void Critical(string message, Exception ex) => Log(LogLevel.Critical, message, ex);

        /// <summary>
        /// Hassas verileri log'dan çıkart
        /// </summary>
        public static string SanitizeLogMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return message;

            // Şifre, token, anahtar içeren kısımları gizle
            string sanitized = System.Text.RegularExpressions.Regex.Replace(
                message,
                @"(password|token|secret|apikey|authorization)[\s:=]*([\w\d\.\-]+)",
                "$1=***REDACTED***",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            return sanitized;
        }
    }
}
