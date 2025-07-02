using Microsoft.AspNetCore.Mvc;
using IPS_TH.Data;
using IPS_TH.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IPS_TH.Controllers
{
    public class LanguageController : Controller
    {
        private readonly ApplicationDbContext _context;
 
        public LanguageController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> LanguageView()
        {
            return View(await _context.sys_language
                .Where(x => x.RecordStatus != "D")
                .Select(x => new 
                {
                    x.Id,
                    x.Keyword,
                    x.Th,
                    x.En,
                    x.Jp,
                    x.Cn,
                    x.CreatedBy,
                    x.CreatedDate,
                    x.UpdatedBy,
                    x.UpdatedDate,
                    x.RecordStatus
                })
                .Take(200)
                .ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] sys_language model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { status = false, message = "ข้อมูลไม่ถูกต้อง" });
                }

                model.CreatedDate = DateTime.Now;
                model.UpdatedDate = DateTime.Now;
                model.CreatedBy = User.Identity?.Name ?? "System";
                model.UpdatedBy = User.Identity?.Name ?? "System";
                model.RecordStatus = "N";

                if (string.IsNullOrEmpty(model.Keyword))
                {
                    return Json(new { status = false, message = "กรุณาระบุ Keyword" });
                }

                var exists = await _context.sys_language
                    .AnyAsync(x => x.Keyword == model.Keyword && x.RecordStatus != "D");
                if (exists)
                {
                    return Json(new { status = false, message = "Keyword นี้มีในระบบแล้ว" });
                }

                _context.Add(model);
                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "บันทึกข้อมูลสำเร็จ" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] sys_language model)
        {
            try
            {
                var existingData = await _context.sys_language.FindAsync(model.Id);
                if (existingData == null)
                {
                    return Json(new { status = false, message = "ไม่พบข้อมูล" });
                }

                existingData.Keyword = model.Keyword;
                existingData.Th = model.Th;
                existingData.En = model.En;
                existingData.Jp = model.Jp;
                existingData.Cn = model.Cn;
                existingData.UpdatedDate = DateTime.Now;
                existingData.UpdatedBy = model.UpdatedBy;

                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "แก้ไขข้อมูลสำเร็จ" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var data = await _context.sys_language.FindAsync(id);
                if (data == null)
                {
                    return Json(new { status = false, message = "ไม่พบข้อมูล" });
                }

                data.RecordStatus = "D";
                data.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "ลบข้อมูลสำเร็จ" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TranslateText([FromBody] TranslateRequest request)
        {
            try
            {
                var translations = new Dictionary<string, string>();
                translations[request.SourceLang] = request.Text;

                var languages = new Dictionary<string, string>
                {
                    { "th", "th" },
                    { "en", "en" },
                    { "jp", "ja" },
                    { "cn", "zh-TW" }
                };

                using (var client = new HttpClient())
                {
                    foreach (var lang in languages)
                    {
                        if (lang.Key != request.SourceLang)
                        {
                            var url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(request.Text)}&langpair={languages[request.SourceLang]}|{lang.Value}";
                            
                            var response = await client.GetAsync(url);
                            var result = await response.Content.ReadAsStringAsync();
                            var translationResult = JsonSerializer.Deserialize<TranslationResponse>(result);

                            if (translationResult?.ResponseData?.TranslatedText != null)
                            {
                                translations[lang.Key] = translationResult.ResponseData.TranslatedText;
                            }
                            else
                            {
                                translations[lang.Key] = "Translation error";
                            }

                            // เพื่อป้องกันการ rate limit ของ API
                            await Task.Delay(100);
                        }
                    }
                }

                return Json(new { status = true, data = translations });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
    }

    public class TranslateRequest
    {
        public string Text { get; set; }
        public string SourceLang { get; set; }
    }

    public class TranslationResponse
    {
        [JsonPropertyName("responseData")]
        public ResponseData ResponseData { get; set; }
    }

    public class ResponseData
    {
        [JsonPropertyName("translatedText")]
        public string TranslatedText { get; set; }
    }
}