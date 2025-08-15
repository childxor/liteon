using System;
using System.Collections.Generic;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace IPS_TH.Extensions
{
    public static class SessionExtensions
    {
        private static IConfiguration _configuration;

        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        public static T? Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonConvert.DeserializeObject<T>(value);
        }

        public static string? GetString(this ISession session, string key)
        {
            byte[]? data = session.Get(key);
            if (data == null)
            {
                return null;
            }
            return Encoding.UTF8.GetString(data);
        }

        public static void SetString(this ISession session, string key, string value)
        {
            session.Set(key, Encoding.UTF8.GetBytes(value));
        }

        public static string GetTranslation(this ISession session, string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return string.Empty;
            }

            try
            {
                if (keyword == "upcoming_holidays")
                {
                    Console.WriteLine("test.");
                }

                // ดึงข้อมูลภาษาจาก Session
                var languageDataJson = session.GetString("LanguageData");
                if (string.IsNullOrEmpty(languageDataJson))
                {
                    return keyword;
                }

                // แปลงข้อมูลจาก JSON เป็น Dictionary
                var languageData = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    languageDataJson
                );

                // ดึงข้อความแปลตาม keyword
                if (languageData != null && languageData.ContainsKey(keyword))
                {
                    return languageData[keyword];
                }

                return keyword;
            }
            catch
            {
                // กรณีเกิดข้อผิดพลาด ให้คืนค่า keyword เดิม
                return keyword;
            }
        }

        public static string GetCurrentLanguage(this ISession session)
        {
            return session.GetString("Language") ?? "th";
        }

        public static void SetLanguage(this ISession session, string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                languageCode = "th";
            }

            // กำหนดภาษาใหม่
            session.SetString("Language", languageCode);

            // โหลดข้อมูลการแปลตามภาษาที่เลือก
            var connectionString = _configuration?.GetConnectionString("HR_IPS");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'HR_IPS' not found in configuration.");
            }

            var translations = LoadLanguageTranslationsFromDb(languageCode, connectionString);
            session.SetString("LanguageData", JsonConvert.SerializeObject(translations));
        }

        public static void InitializeLanguage(this ISession session, string defaultLanguage = "th")
        {
            var currentLanguage = session.GetString("Language");
            if (string.IsNullOrEmpty(currentLanguage))
            {
                session.SetLanguage(defaultLanguage);
            }
        }

        public static Dictionary<string, string> LoadLanguageTranslationsFromDb(
            string languageCode,
            string connectionString
        )
        {
             var translations = new Dictionary<string, string>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql =
                    "SELECT Keyword, Th, En, Cn FROM sys_language WHERE RecordStatus = 'N'";
                var results = conn.Query<dynamic>(sql);

                foreach (var row in results)
                {
                    string keyword = row.Keyword?.ToString();
                    if (string.IsNullOrEmpty(keyword))
                        continue;

                    string value = languageCode switch
                    {
                        "th" => row.Th?.ToString(),
                        "en" => row.En?.ToString(),
                        "zh" => row.Cn?.ToString(),
                        _ => row.Th?.ToString(),
                    };

                    if (!string.IsNullOrEmpty(value) && !translations.ContainsKey(keyword))
                    {
                        translations.Add(keyword, value);
                    }
                }
            }
            return translations;
        }
    }
}
