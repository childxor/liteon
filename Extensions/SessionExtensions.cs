using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace IPS_TH.Extensions
{
    public static class SessionExtensions
    {
        private static IConfiguration? _configuration;

        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static void Set<T>(this ISession session, string key, T value)
        {
            var settings = new JsonSerializerSettings
            {
                StringEscapeHandling = StringEscapeHandling.Default
            };
            session.SetString(key, JsonConvert.SerializeObject(value, settings));
        }

        public static T? Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            if (value == null) return default;
            
            var settings = new JsonSerializerSettings
            {
                StringEscapeHandling = StringEscapeHandling.Default
            };
            return JsonConvert.DeserializeObject<T>(value, settings);
        }

        // NOTE: อย่ากำหนด GetString/SetString ซ้ำกับของ Microsoft.AspNetCore.Http
        // ให้ใช้ของระบบ (Microsoft.AspNetCore.Http.SessionExtensions) แทน

        public static string GetTranslation(this ISession session, string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return string.Empty;
            }

            try
            {
                // ตรวจสอบว่าภาษาเปลี่ยนหรือไม่
                var currentLanguage = session.GetCurrentLanguage();
                var cachedLanguage = session.GetString($"translation_lang_{keyword}");
                
                // ถ้าภาษาเปลี่ยน ให้ลบ cache เก่า
                if (!string.IsNullOrEmpty(cachedLanguage) && cachedLanguage != currentLanguage)
                {
                    session.Remove($"translation_{keyword}");
                    session.Remove($"translation_lang_{keyword}");
                }

                // ลองดึงข้อมูลจาก Session แบบตรงๆ ก่อน
                var directValue = session.GetString($"translation_{keyword}");
                if (!string.IsNullOrEmpty(directValue))
                {
                    return directValue;
                }

                // ถ้าไม่มี ให้โหลดจากฐานข้อมูลใหม่
                var connectionString = _configuration?.GetConnectionString("HR_IPS");
                if (!string.IsNullOrEmpty(connectionString))
                {
                    var translation = GetTranslationFromDb(keyword, currentLanguage, connectionString);
                    if (!string.IsNullOrEmpty(translation))
                    {
                        // เก็บไว้ใน Session แบบตรงๆ พร้อมกับภาษา
                        session.SetString($"translation_{keyword}", translation);
                        session.SetString($"translation_lang_{keyword}", currentLanguage);
                        return translation;
                    }
                }

                return keyword;
            }
            catch (Exception ex)
            {
                // กรณีเกิดข้อผิดพลาด ให้คืนค่า keyword เดิม
                Console.WriteLine($"Error in GetTranslation: {ex.Message}");
                return keyword;
            }
        }

        private static string GetTranslationFromDb(string keyword, string languageCode, string connectionString)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT Th, En, Cn FROM sys_language WHERE Keyword = @keyword AND RecordStatus = 'N'";
                    var result = conn.QueryFirstOrDefault<dynamic>(sql, new { keyword });

                    if (result != null)
                    {
                        string value = languageCode switch
                        {
                            "th" => result.Th?.ToString() ?? "",
                            "en" => result.En?.ToString() ?? "",
                            "zh" => result.Cn?.ToString() ?? "",
                            _ => result.Th?.ToString() ?? "",
                        };

                        // แปลง Unicode escape sequences เป็นตัวอักษรไทย
                        var unicodeDecoded = System.Text.RegularExpressions.Regex.Replace(value, @"\\u([0-9A-Fa-f]{4})", m => 
                        {
                            int.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber, null, out int code);
                            return ((char)code).ToString();
                        });
                        
                        
                        return unicodeDecoded;
                    }
                }
            }
            catch
            {
                // กรณีเกิดข้อผิดพลาด
            }

            return string.Empty;
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

            // ล้างข้อมูล translation เก่าออกจาก Session
            ClearTranslationCache(session);

            // โหลดข้อมูลการแปลตามภาษาที่เลือก
            var connectionString = _configuration?.GetConnectionString("HR_IPS");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'HR_IPS' not found in configuration.");
            }

            var translations = LoadLanguageTranslationsFromDb(languageCode, connectionString);
            var settings = new JsonSerializerSettings
            {
                StringEscapeHandling = StringEscapeHandling.Default
            };
            session.SetString("LanguageData", JsonConvert.SerializeObject(translations, settings));
        }

        private static void ClearTranslationCache(this ISession session)
        {
            // ลบข้อมูล translation เก่าออกจาก Session
            var keysToRemove = new List<string>();
            
            // เก็บ keys ที่ต้องลบ (translation_* และ translation_lang_*)
            foreach (var key in session.Keys)
            {
                if (key.StartsWith("translation_"))
                {
                    keysToRemove.Add(key);
                }
            }
            
            // ลบ keys ที่เก็บไว้
            foreach (var key in keysToRemove)
            {
                session.Remove(key);
            }
        }

        public static void InitializeLanguage(this ISession session, string defaultLanguage = "th")
        {
            var currentLanguage = session.GetString("Language");
            if (string.IsNullOrEmpty(currentLanguage))
            {
                session.SetLanguage(defaultLanguage);
            }
        }

        public static void ClearAllTranslations(this ISession session)
        {
            // ล้างข้อมูล translation ทั้งหมดออกจาก Session
            ClearTranslationCache(session);
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
                    string keyword = row.Keyword?.ToString() ?? "";
                    if (string.IsNullOrEmpty(keyword))
                        continue;

                    string value = languageCode switch
                    {
                        "th" => row.Th?.ToString() ?? "",
                        "en" => row.En?.ToString() ?? "",
                        "zh" => row.Cn?.ToString() ?? "",
                        _ => row.Th?.ToString() ?? "",
                    };

                    if (!string.IsNullOrEmpty(value) && !translations.ContainsKey(keyword))
                    {
                        // Decode HTML entities เมื่อโหลดจากฐานข้อมูล
                        var decodedValue = HttpUtility.HtmlDecode(value);
                        
                        // Debug: ตรวจสอบว่าข้อมูลถูก encode หรือไม่
                        if (keyword == "phone_count")
                        {
                            Console.WriteLine($"Original value: {value}");
                            Console.WriteLine($"Decoded value: {decodedValue}");
                        }
                        
                        translations.Add(keyword, decodedValue);
                    }
                }
            }
            return translations;
        }
    }
}
