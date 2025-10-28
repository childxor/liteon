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
                    Keyword = x.Keyword,
                    Th = x.Th,
                    En = x.En,
                    Cn = x.Cn,
                    CreatedBy = EF.Property<string>(x, "CreatedBy"),
                    CreatedDate = EF.Property<DateTime?>(x, "CreatedDate"),
                    UpdatedBy = EF.Property<string>(x, "UpdatedBy"),
                    UpdatedDate = EF.Property<DateTime?>(x, "UpdatedDate"),
                    RecordStatus = x.RecordStatus
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

                // ปรับค่าเริ่มต้นให้ครบ
                model.Keyword = (model.Keyword ?? string.Empty).Trim();
                model.ModuleId = (model.ModuleId ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(model.ModuleId))
                {
                    model.ModuleId = "1";
                }
                model.Th = model.Th ?? string.Empty;
                model.En = model.En ?? string.Empty;
                model.Cn = model.Cn ?? string.Empty;
                model.RecordStatus = string.IsNullOrEmpty(model.RecordStatus) ? "N" : model.RecordStatus;
                model.CreatedDate = model.CreatedDate ?? DateTime.Now;
                model.UpdatedDate = DateTime.Now;
                model.CreatedBy = string.IsNullOrEmpty(model.CreatedBy) ? (User.Identity?.Name ?? "System") : model.CreatedBy;
                model.UpdatedBy = User.Identity?.Name ?? "System";

                if (string.IsNullOrEmpty(model.Keyword))
                {
                    return Json(new { status = false, message = "กรุณาระบุ Keyword" });
                }

                // เช็คซ้ำ keyword แบบ global ไม่สน moduleId
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

                // อัปเดตแบบ null-safe
                existingData.Keyword = (model.Keyword ?? existingData.Keyword)?.Trim();
                existingData.Th = model.Th ?? existingData.Th;
                existingData.En = model.En ?? existingData.En;
                existingData.Cn = model.Cn ?? existingData.Cn;
                var normalizedModuleId = string.IsNullOrWhiteSpace(model.ModuleId)
                    ? (string.IsNullOrWhiteSpace(existingData.ModuleId) ? "1" : existingData.ModuleId)
                    : model.ModuleId.Trim();
                existingData.ModuleId = normalizedModuleId;
                existingData.RecordStatus = string.IsNullOrEmpty(model.RecordStatus) ? (existingData.RecordStatus ?? "N") : model.RecordStatus;
                existingData.UpdatedDate = DateTime.Now;
                existingData.UpdatedBy = string.IsNullOrEmpty(model.UpdatedBy) ? (User.Identity?.Name ?? "System") : model.UpdatedBy;

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

                _context.sys_language.Remove(data);
                await _context.SaveChangesAsync();
                return Json(new { status = true, message = "ลบข้อมูลสำเร็จ" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLanguageDataByModule(string moduleId)
        {
            try
            {
                if (string.IsNullOrEmpty(moduleId))
                {
                    return Json(new { success = false, message = "ไม่พบ Module ID" });
                }

                var languageData = await _context.sys_language
                    .Where(x => EF.Property<string>(x, "ModuleId") == moduleId && x.RecordStatus != "D")
                    .OrderByDescending(x => x.Id)
                    .Take(200)
                    .Select(x => new 
                    {
                        id = x.Id,
                        keyword = x.Keyword,
                        moduleId = EF.Property<string>(x, "ModuleId"),
                        th = x.Th,
                        en = x.En,
                        cn = x.Cn,
                        createdBy = EF.Property<string>(x, "CreatedBy"),
                        createdDate = EF.Property<DateTime?>(x, "CreatedDate"),
                        updatedBy = EF.Property<string>(x, "UpdatedBy"),
                        updatedDate = EF.Property<DateTime?>(x, "UpdatedDate"),
                        recordStatus = x.RecordStatus
                    })
                    .ToListAsync();

                return Json(new { success = true, data = languageData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLanguageData(string q)
        {
            try
            {
                var query = _context.sys_language
                    .Where(x => x.RecordStatus != "D");

                if (!string.IsNullOrWhiteSpace(q))
                {
                    q = q.Trim();
                    query = query.Where(x =>
                        (x.Keyword ?? "").Contains(q) ||
                        (EF.Property<string>(x, "ModuleId") ?? "").Contains(q) ||
                        (x.Th ?? "").Contains(q) ||
                        (x.En ?? "").Contains(q) ||
                        (x.Cn ?? "").Contains(q)
                    );
                }

                var languageData = await query
                    .OrderByDescending(x => x.Id)
                    .Take(1000)
                    .Select(x => new
                    {
                        id = x.Id,
                        keyword = x.Keyword,
                        moduleId = EF.Property<string>(x, "ModuleId"),
                        th = x.Th,
                        en = x.En,
                        cn = x.Cn,
                        createdBy = EF.Property<string>(x, "CreatedBy"),
                        createdDate = EF.Property<DateTime?>(x, "CreatedDate"),
                        updatedBy = EF.Property<string>(x, "UpdatedBy"),
                        updatedDate = EF.Property<DateTime?>(x, "UpdatedDate"),
                        recordStatus = x.RecordStatus
                    })
                    .ToListAsync();

                return Json(new { success = true, data = languageData });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> IsKeywordAvailable(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return Json(new { success = false, message = "กรุณาระบุ Keyword" });
                }

                var exists = await _context.sys_language
                    .AnyAsync(x => x.Keyword == keyword && x.RecordStatus != "D");

                return Json(new { success = true, available = !exists });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetKeywordCount(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return Json(new { success = true, count = 0 });
                }

                var count = await _context.sys_language
                    .CountAsync(x => x.Keyword == keyword && x.RecordStatus != "D");

                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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