using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using IPS_TH.Data;
using IPS_TH.Extensions; // เพิ่ม namespace สำหรับ Extension Methods
using IPS_TH.Models.Employee;
using IPS_TH.Models.ESMMS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient; 
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging; 
using Newtonsoft.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.Net.Mail;

namespace IPS_TH.Controllers.ESMMS
{
    public class ExamController : BaseController
    {
        // เมธอดช่วยสุ่มตัวเลือกจากคำถาม A–H ให้เหลือ 4 ข้อ (สุ่มตำแหน่ง) และคืนตำแหน่งคำตอบถูกเป็น A–D
        private static bool TryBuildRandomizedOptions(
            esmms_question q,
            out string? destA,
            out string? destB,
            out string? destC,
            out string? destD,
            out string? destAImg,
            out string? destBImg,
            out string? destCImg,
            out string? destDImg,
            out string correctKey
        )
        {
            destA = destB = destC = destD = null;
            destAImg = destBImg = destCImg = destDImg = null;
            correctKey = string.Empty;

            if (q == null || string.IsNullOrWhiteSpace(q.correct_answer))
                return false;

            var upperCorrect = q.correct_answer.Trim().ToUpperInvariant();
            if (upperCorrect.Length != 1)
                return false;

            char correctLetter = upperCorrect[0];
            if (correctLetter < 'A' || correctLetter > 'H')
                return false;

            // รวบรวมตัวเลือกที่มีค่า (A–H)
            var letterToText = new Dictionary<char, string?>
            {
                ['A'] = q.choice_a,
                ['B'] = q.choice_b,
                ['C'] = q.choice_c,
                ['D'] = q.choice_d,
                ['E'] = q.choice_e,
                ['F'] = q.choice_f,
                ['G'] = q.choice_g,
                ['H'] = q.choice_h,
            };
            var letterToImage = new Dictionary<char, string?>
            {
                ['A'] = q.choice_a_image_path,
                ['B'] = q.choice_b_image_path,
                ['C'] = q.choice_c_image_path,
                ['D'] = q.choice_d_image_path,
                ['E'] = q.choice_e_image_path,
                ['F'] = q.choice_f_image_path,
                ['G'] = q.choice_g_image_path,
                ['H'] = q.choice_h_image_path,
            };

            // ต้องมีข้อความหรือรูปของคำตอบถูก และอย่างน้อย 3 ตัวเลือกอื่น ๆ
            if (!letterToText.TryGetValue(correctLetter, out var correctText)) return false;
            var correctImg = letterToImage.GetValueOrDefault(correctLetter);
            var hasCorrectContent = !string.IsNullOrWhiteSpace(correctText) || !string.IsNullOrWhiteSpace(correctImg);
            if (!hasCorrectContent) return false;

            var available = letterToText
                .Select(kv => kv.Key)
                .Where(k =>
                {
                    var txt = letterToText.GetValueOrDefault(k);
                    var img = letterToImage.GetValueOrDefault(k);
                    return !string.IsNullOrWhiteSpace(txt) || !string.IsNullOrWhiteSpace(img);
                })
                .ToList();

            // ต้องมีรวมอย่างน้อย 4 ตัวเลือก
            if (available.Count < 4)
                return false;

            // รายการตัวหลอก (ยกเว้นคำตอบถูก)
            var distractorPool = available.Where(x => x != correctLetter).ToList();
            if (distractorPool.Count < 3)
                return false;

            // สุ่ม 3 ตัวหลอกจาก distractorPool
            var randomDistractors = distractorPool
                .OrderBy(_ => Guid.NewGuid())
                .Take(3)
                .ToList();

            // รวมถูก + หลอก 3 แล้วสลับตำแหน่งเป็นช่อง A–D
            var selectedLetters = new List<char> { correctLetter };
            selectedLetters.AddRange(randomDistractors);
            var shuffled = selectedLetters
                .OrderBy(_ => Guid.NewGuid())
                .ToList();

            // กำหนดข้อความและรูปไปยัง A–D ตามลำดับใหม่
            var aLetter = shuffled[0];
            var bLetter = shuffled[1];
            var cLetter = shuffled[2];
            var dLetter = shuffled[3];

            destA = letterToText[aLetter];
            destB = letterToText[bLetter];
            destC = letterToText[cLetter];
            destD = letterToText[dLetter];

            destAImg = letterToImage.GetValueOrDefault(aLetter);
            destBImg = letterToImage.GetValueOrDefault(bLetter);
            destCImg = letterToImage.GetValueOrDefault(cLetter);
            destDImg = letterToImage.GetValueOrDefault(dLetter);

            // ระบุว่าคำตอบถูกไปอยู่ตำแหน่งใด: A/B/C/D
            if (aLetter == correctLetter) correctKey = "A";
            else if (bLetter == correctLetter) correctKey = "B";
            else if (cLetter == correctLetter) correctKey = "C";
            else if (dLetter == correctLetter) correctKey = "D";
            else return false;

            // ถ้าข้อความว่างแต่มีรูป ให้ใช้ข้อความสั้นๆเพื่อให้ UI ที่แสดงเฉพาะข้อความพอมีข้อมูลอ่านได้
            if (string.IsNullOrWhiteSpace(destA) && !string.IsNullOrWhiteSpace(destAImg)) destA = "(รูปภาพ)";
            if (string.IsNullOrWhiteSpace(destB) && !string.IsNullOrWhiteSpace(destBImg)) destB = "(รูปภาพ)";
            if (string.IsNullOrWhiteSpace(destC) && !string.IsNullOrWhiteSpace(destCImg)) destC = "(รูปภาพ)";
            if (string.IsNullOrWhiteSpace(destD) && !string.IsNullOrWhiteSpace(destDImg)) destD = "(รูปภาพ)";

            return true;
        }
        private static string CleanTextForForms(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var s = input.Trim();
            s = Regex.Replace(s, "[\\u0000-\\u001F\\u007F]", string.Empty);
            s = Regex.Replace(s, "[ ]{2,}", " ");
            s = s.Replace("%%", "%");
            s = Regex.Replace(s, "\\s%", "%");
            return s;
        }

        private class MsFormsQuestionDto
        {
            [JsonPropertyName("order")] public int order { get; set; }
            [JsonPropertyName("question_text")] public string question_text { get; set; } = string.Empty;
            [JsonPropertyName("choice_a")] public string? choice_a { get; set; }
            [JsonPropertyName("choice_b")] public string? choice_b { get; set; }
            [JsonPropertyName("choice_c")] public string? choice_c { get; set; }
            [JsonPropertyName("choice_d")] public string? choice_d { get; set; }
            [JsonPropertyName("correct_answer")] public string? correct_answer { get; set; }
        }

        private class MsFormsExportDto
        {
            [JsonPropertyName("title")] public string title { get; set; } = string.Empty;
            [JsonPropertyName("description")] public string? description { get; set; }
            [JsonPropertyName("questions")] public List<MsFormsQuestionDto> questions { get; set; } = new List<MsFormsQuestionDto>();
        }
        private class SQL944DataResult
        {
            public HashSet<string> sql944Employees { get; set; } 
            public Dictionary<string, object> cardDict { get; set; } =
                new Dictionary<string, object>();
            public Dictionary<string, object> fingerprintDict { get; set; } =
                new Dictionary<string, object>();
            public Dictionary<string, string> nameDict { get; set; }
            public Dictionary<string, string> deptNameDict { get; set; }
            public Dictionary<string, string> deptIDDict { get; set; }
            public Dictionary<string, string> cardNumberDict { get; set; }
            public Dictionary<string, int> accessCountDict { get; set; }
            public HashSet<string> fingerprintDataIds { get; set; }
        }

        private readonly string _connectionString;

        private readonly string _hrIpsConnectionString;

        private readonly IConfiguration _configuration;

        // sql944
        private readonly string _sql944ConnectionString;

        // เพิ่มตัวแปรสำหรับเชื่อมต่อกับ CES941
        private readonly string _ces941ConnectionString;

        private readonly ILogger<ExamController> _logger;

        public ExamController(
            IConfiguration configuration,
            ApplicationDbContext context,
            ILogger<ExamController> logger
        )
            : base(context)
        {
            _connectionString = _context.Database.GetDbConnection().ConnectionString;
            _configuration = configuration;
            _sql944ConnectionString = _configuration.GetConnectionString("SQL944");
            _hrIpsConnectionString = _configuration.GetConnectionString("HR_IPS");
            _ces941ConnectionString = _configuration.GetConnectionString("CES941");
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ExportToMsFormsJson(int id)
        {
            var exam = await _context.esmms_exam.FirstOrDefaultAsync(e => e.id == id);
            if (exam == null)
                return Json(new { error = "not_found" });

            var items = await (
                from d in _context.esmms_exam_detail
                join q in _context.esmms_question on d.question_id equals q.id
                where d.exam_id == id
                orderby d.sort_order
                select new ExamPrintItem
                {
                    sort_order = d.sort_order,
                    question_text = q.question_text,
                    choice_a = q.choice_a,
                    choice_b = q.choice_b,
                    choice_c = q.choice_c,
                    choice_d = q.choice_d,
                    correct_answer = q.correct_answer
                }
            ).ToListAsync();

            var dto = new MsFormsExportDto
            {
                title = CleanTextForForms(exam.title),
                description = CleanTextForForms(exam.description)
            };

            foreach (var q in items)
            {
                dto.questions.Add(new MsFormsQuestionDto
                {
                    order = q.sort_order,
                    question_text = CleanTextForForms(q.question_text),
                    choice_a = CleanTextForForms(q.choice_a),
                    choice_b = CleanTextForForms(q.choice_b),
                    choice_c = CleanTextForForms(q.choice_c),
                    choice_d = CleanTextForForms(q.choice_d),
                    correct_answer = CleanTextForForms(q.correct_answer)
                });
            }

            return Json(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendToPowerAutomate(int id, string flowUrl)
        {
            if (string.IsNullOrWhiteSpace(flowUrl))
                return ErrorResponse("กรุณาระบุ URL ของ Power Automate");

            var jsonResult = await ExportToMsFormsJson(id) as JsonResult;
            if (jsonResult == null)
                return ErrorResponse("ไม่สามารถสร้างข้อมูล JSON ได้");

            var json = System.Text.Json.JsonSerializer.Serialize(jsonResult.Value);

            using var http = new HttpClient();
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await http.PostAsync(flowUrl, content);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                return ErrorResponse($"ส่งข้อมูลไป Power Automate ไม่สำเร็จ: {(int)resp.StatusCode} {body}");
            }
            return SuccessResponse(message: "ส่งข้อมูลไป Power Automate สำเร็จ");
        }
        public async Task<IActionResult> Index()
        {
            try
            {
                // ดึงข้อมูลแผนก
                using (var defaultconnection = new SqlConnection(_connectionString))
                using (var connection = new SqlConnection(_sql944ConnectionString))
                using (var connectionces = new SqlConnection(_ces941ConnectionString))
                {
                    // เพิ่มข้อมูล Shiftdept
                    var shiftDeptQuery =
                        @"SELECT DISTINCT WrkPlanID as wrkPlanID
                        FROM vEmployee
                        WHERE (WrkPlanID IS NOT NULL)
                        ORDER BY WrkPlanID";
                    var shiftDept = await connectionces.QueryAsync<dynamic>(shiftDeptQuery);
                    ViewBag.Shiftdept = shiftDept;

                    var deptQuery = @"SELECT DISTINCT WorkArea as deptID
                        FROM vEmployee
                        WHERE (WorkArea IS NOT NULL)
                        ORDER BY WorkArea";
                    var dept = await connectionces.QueryAsync<dynamic>(deptQuery);
                    ViewBag.Dept = dept;
 
                    // เพิ่มการดึงข้อมูลประตู
                    var doorsSql =
                        @"SELECT DISTINCT doorID, doorName 
                        FROM PubDoor 
                        ORDER BY doorName";
                    var doors = await connection.QueryAsync<dynamic>(doorsSql);
                    ViewBag.Doors = doors;

                    // shift dept
                    var shiftfromces941Query =
                        @"SELECT WrkPlanID
                        FROM MEmpGroup
                        ORDER BY WrkPlanID";
                    var shiftfromces941 = await connectionces.QueryAsync<dynamic>(shiftfromces941Query);
                    ViewBag.Shiftfromces941 = shiftfromces941;
                }

                await LoadPermissions("Exam", "esmms_exam"); 

                // ส่งข้อมูลแผนกของผู้ใช้ไปยัง View
                ViewData["CurrentUserDepartment"] = HttpContext.Session.GetString("WorkArea");

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in E-SMMS action: {ex.Message}");
                return View();
            }
        }

        [HttpGet] 
        public async Task<IActionResult> esmms_exam()
        {
            await LoadPermissions("Exam", "Exam");
            var items = await _context
                .esmms_exam
                .OrderByDescending(x => x.created_at)
                .ToListAsync();

            var courses = await _context.esmms_course
                .OrderBy(x => x.course_code)
                .Select(x => new esmms_course { id = x.id, course_code = x.course_code, description_en = x.description_en })
                .ToListAsync();

            ViewBag.Courses = courses; // สำหรับ dropdown เลือกคอร์ส
            ViewBag.CourseDict = courses.ToDictionary(x => x.id, x => x.course_code); // map id->code สำหรับแสดงผล

            return View("~/Views/ESMMS/exam.cshtml", items);
        }

        [HttpGet]
        public async Task<IActionResult> GetExams()
        {
            try
            {
                _logger.LogInformation("GetExams: Starting to fetch exams data");
                
                var items = await (
                    from e in _context.esmms_exam
                    join c in _context.esmms_course on e.course_id equals c.id into gc
                    from c in gc.DefaultIfEmpty()
                    orderby e.created_at descending
                    select new
                    {
                        e.id,
                        e.title,
                        e.description,
                        e.course_id,
                        course_code = c != null ? c.course_code : null,
                        course_name = c != null ? c.description_en : null,
                        e.random_question_count,
                        e.duration_minutes,
                        e.start_at,
                        e.end_at,
                        e.attempts_limit,
                        e.target_group,
                        e.forms_url,
                        e.is_active
                    }
                ).ToListAsync();
                
                _logger.LogInformation($"GetExams: Found {items.Count} exams from database");
            var withImage = items
                .Select(x =>
                {
                    var meta = ReadExamMeta(x.id);
                    int? pdlineId = null;
                    int? factoryId = null;
                    int? stageId = null;
                    string? employeesCsv = null;
                    try
                    {
                        if (meta != null)
                        {
                            if (meta.TryGetValue("factory_id", out var fa) && fa != null)
                            {
                                int tmp;
                                if (fa is long lfa) tmp = (int)lfa; else if (!int.TryParse(fa.ToString(), out tmp)) tmp = 0;
                                factoryId = tmp > 0 ? tmp : null;
                            }
                            if (meta.TryGetValue("stage_id", out var st) && st != null)
                            {
                                int tmp;
                                if (st is long lst) tmp = (int)lst; else if (!int.TryParse(st.ToString(), out tmp)) tmp = 0;
                                stageId = tmp > 0 ? tmp : null;
                            }
                            if (meta.TryGetValue("pdline_id", out var pd) && pd != null)
                            {
                                int tmp;
                                if (pd is long lpd) tmp = (int)lpd; else if (!int.TryParse(pd.ToString(), out tmp)) tmp = 0;
                                pdlineId = tmp > 0 ? tmp : null;
                            }
                            if (meta.TryGetValue("selected_employees", out var emps) && emps != null)
                            {
                                employeesCsv = emps.ToString();
                            }
                        }
                    }
                    catch { }

                    var selectedEmployeesCount = string.IsNullOrWhiteSpace(employeesCsv)
                        ? 0
                        : employeesCsv.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

                    return new
                    {
                        x.id,
                        x.title,
                        x.description,
                        x.course_id,
                        x.course_code,
                        x.course_name,
                        x.random_question_count,
                        x.duration_minutes,
                        x.start_at,
                        x.end_at,
                        x.attempts_limit,
                        x.target_group,
                        x.forms_url,
                        x.is_active,
                        image_url = GetExamImageVirtualPath(x.id),
                        is_completed = GetExamCompleted(x.id),
                        factory_id = factoryId,
                        stage_id = stageId,
                        pdline_id = pdlineId,
                        selected_employees = employeesCsv,
                        selected_employees_count = selectedEmployeesCount,
                        excel_url = GetExamExcelVirtualPath(x.id)
                    };
                })
                .ToList();
                
                _logger.LogInformation($"GetExams: Processed {withImage.Count} exams with metadata");
                return Json(new { data = withImage });
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetExams: Error - {ex.Message}");
                return Json(new { data = new List<object>(), error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExam(esmms_exam input, IFormFile? image, int? factory_id, int? stage_id, int? pdline_id, string? selected_employees)
        {
            // title จะถูกระบบตั้งให้ อนุโลมไม่ตรวจ title จาก ModelState
            ModelState.Remove("title");
            if (!ModelState.IsValid)
                return ErrorResponse("กรุณากรอกข้อมูลให้ครบถ้วน");

            // ตรวจสอบก่อน: ต้องเลือกคอร์สและจำนวนข้อสุ่มมากกว่า 0
            if (!input.course_id.HasValue || input.course_id.Value <= 0)
            {
                TempData["ErrorMessage"] = "กรุณาเลือกคอร์ส";
                return RedirectToAction(nameof(esmms_exam));
            }
            if (input.random_question_count <= 0)
            {
                TempData["ErrorMessage"] = "กรุณากำหนดจำนวนข้อสุ่มมากกว่า 0";
                return RedirectToAction(nameof(esmms_exam));
            }

            // ตรวจว่ามีจำนวนคำถามเพียงพอในคอร์ส
            var availableQuestionCount = await _context.esmms_question
                .Where(q => q.is_active && q.course_id == input.course_id.Value)
                .CountAsync();
            if (availableQuestionCount < input.random_question_count)
            {
                TempData["ErrorMessage"] = $"จำนวนคำถามในคอร์สมีไม่พอ (มี {availableQuestionCount} ข้อ) ไม่สามารถสร้างชุดข้อสอบ {input.random_question_count} ข้อได้";
                return RedirectToAction(nameof(esmms_exam));
            }

            // ตั้งชื่อชุดสอบอัตโนมัติ: LITEONIPSTH + yyyyMMdd + ลำดับ 2 หลักของวันนั้น
            var now = DateTime.Now;
            var startOfDay = now.Date;
            var endOfDay = startOfDay.AddDays(1);
            var todayCount = await _context.esmms_exam
                .CountAsync(x => x.created_at >= startOfDay && x.created_at < endOfDay);
            var seq = todayCount + 1;
            input.title = $"LITEONIPSTH{now:yyyyMMdd}{seq:00}";

            input.created_at = DateTime.Now;
            input.updated_at = DateTime.Now;
            input.created_by = GetCurrentUserName();
            input.updated_by = GetCurrentUserName();

            _context.esmms_exam.Add(input);
            await _context.SaveChangesAsync();

            // หลังสร้างชุดสอบ สุ่มคำถามจากคอร์สและบันทึกลงตารางรายละเอียด (กันค่า null ครบถ้วน)
            try
            {
                var courseId = input.course_id.HasValue ? input.course_id.Value : 0;
                var countToTake = input.random_question_count > 0 ? input.random_question_count : 0;
                if (courseId > 0 && countToTake > 0)
                {
                    var qList = await _context.esmms_question
                        .Where(q => q.is_active && q.course_id == courseId)
                        .OrderBy(x => Guid.NewGuid())
                        .Take(countToTake)
                        .Select(x => x.id)
                        .ToListAsync();

                    if (qList != null && qList.Count > 0)
                    {
                        var userName = GetCurrentUserName() ?? "system";
                        int order = 1;
                        foreach (var qid in qList)
                        {
                            var q = await _context.esmms_question.AsNoTracking().FirstOrDefaultAsync(x => x.id == qid);
                            if (q == null)
                                continue;

                            if (!TryBuildRandomizedOptions(q, out string? ra, out string? rb, out string? rc, out string? rd, out string? raImg, out string? rbImg, out string? rcImg, out string? rdImg, out string correctKey))
                                continue;

                            _context.esmms_exam_detail.Add(new esmms_exam_detail
                            {
                                exam_id = input.id,
                                course_id = courseId,
                                question_id = qid,
                                sort_order = order++,
                                choice_a = ra,
                                choice_b = rb,
                                choice_c = rc,
                                choice_d = rd,
                                choice_a_image_path = raImg,
                                choice_b_image_path = rbImg,
                                choice_c_image_path = rcImg,
                                choice_d_image_path = rdImg,
                                corect_randoms = correctKey,
                                created_at = DateTime.Now,
                                updated_at = DateTime.Now,
                                created_by = userName,
                                updated_by = userName
                            });
                        }
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch
            {
                // ไม่ให้กระทบการสร้างชุดสอบหลัก หากบันทึก detail ล้มเหลว
            }

            // บันทึกรูปภาพ (เช่น QR Code) หากผู้ใช้อัปโหลดมา
            if (image != null && image.Length > 0)
            {
                try { await SaveExamImageAsync(input.id, image); } catch { }
            }

            // บันทึกเมตา: ไลน์ผลิต และรายชื่อพนักงานที่เลือก (CSV)
            try
            {
                UpdateExamMeta(input.id, new Dictionary<string, object>
                {
                    { "factory_id", factory_id ?? 0 },
                    { "stage_id", stage_id ?? 0 },
                    { "pdline_id", pdline_id ?? 0 },
                    { "selected_employees", selected_employees ?? string.Empty }
                });
                await UpdateExamEmployeesAsync(input.id, input.course_id, selected_employees, pdline_id);
            }
            catch { }
            return RedirectToAction(nameof(esmms_exam));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditExam(esmms_exam input, IFormFile? image, bool? remove_image, int? factory_id, int? stage_id, int? pdline_id, string? selected_employees)
        {
            // ไม่ตรวจ title จาก ModelState เพราะระบบไม่ให้แก้ไข
            ModelState.Remove("title");
            if (!ModelState.IsValid)
                return ErrorResponse("กรุณากรอกข้อมูลให้ครบถ้วน");

            var entity = await _context.esmms_exam.FirstOrDefaultAsync(x => x.id == input.id);
            if (entity == null)
                return ErrorResponse("ไม่พบข้อมูลสำหรับแก้ไข");

            // ไม่อนุญาตให้แก้ไขชื่อชุดข้อสอบ เพื่อคงรูปแบบที่ระบบกำหนด
            entity.description = input.description;
            entity.random_question_count = input.random_question_count;
            entity.duration_minutes = input.duration_minutes;
            entity.start_at = input.start_at;
            entity.end_at = input.end_at;
            entity.attempts_limit = input.attempts_limit;
            entity.target_group = input.target_group;
            entity.forms_url = input.forms_url;
            entity.course_id = input.course_id;
            entity.is_active = input.is_active;
            entity.updated_at = DateTime.Now;
            entity.updated_by = GetCurrentUserName();

            await _context.SaveChangesAsync(); 

            // จัดการรูปภาพประกอบชุดสอบ
            try
            {
                if (remove_image == true)
                {
                    DeleteExamImage(input.id);
                }
                else if (image != null && image.Length > 0)
                {
                    await SaveExamImageAsync(input.id, image);
                }

                // อัปเดตเมตา: ไลน์ผลิต และรายชื่อพนักงาน
                UpdateExamMeta(input.id, new Dictionary<string, object>
                {
                    { "factory_id", factory_id ?? 0 },
                    { "stage_id", stage_id ?? 0 },
                    { "pdline_id", pdline_id ?? 0 },
                    { "selected_employees", selected_employees ?? string.Empty }
                });

                await UpdateExamEmployeesAsync(input.id, input.course_id, selected_employees, pdline_id);
            }
            catch { }
            return RedirectToAction(nameof(esmms_exam));
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployees(string? q, int take = 100)
        {
            try
            {
                if (take <= 0) take = 50;
                if (take > 200) take = 200;
                var query = _context.emp_person.AsQueryable();
                if (!string.IsNullOrWhiteSpace(q))
                {
                    var keyword = q.Trim();
                    query = query.Where(e =>
                        (e.personID != null && e.personID.Contains(keyword)) ||
                        (e.name != null && e.name.Contains(keyword)) ||
                        (e.deptID != null && e.deptID.Contains(keyword))
                    );
                }
                var list = await query
                    .OrderBy(e => e.personID)
                    .Take(take)
                    .Select(e => new
                    {
                        person_id = e.personID,
                        name = e.name,
                        dept_id = e.deptID,
                        dept_name = e.deptName
                    })
                    .ToListAsync();
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return Json(new { data = new object[0], message = ex.Message });
            }
        }

        private async Task UpdateExamEmployeesAsync(int examId, int? courseId, string? employeesCsv, int? pdlineId)
        {
            var userName = GetCurrentUserName() ?? "system";
            var pdLineValue = pdlineId.HasValue && pdlineId.Value > 0 ? pdlineId.Value.ToString() : null;
            var newIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(employeesCsv))
            {
                var parts = employeesCsv
                    .Split(new[] { ',', '\n', '\r', '\t', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s));
                foreach (var id in parts) newIds.Add(id);
            }

            var exists = await _context.esmms_exam_employee
                .Where(x => x.exam_id == examId)
                .ToListAsync();

            // อัปเดต pd_line และ course_id ให้ข้อมูลเดิมตามค่าล่าสุดของหน้าจอ
            foreach (var rec in exists)
            {
                rec.course_id = courseId;
                rec.pd_line = pdLineValue;
                rec.updated_at = DateTime.Now;
                rec.updated_by = userName;
            }

            var toRemove = exists.Where(x => !newIds.Contains(x.emp_no)).ToList();
            if (toRemove.Count > 0)
            {
                _context.esmms_exam_employee.RemoveRange(toRemove);
            }

            var existingSet = new HashSet<string>(exists.Select(x => x.emp_no), StringComparer.OrdinalIgnoreCase);
            var toAdd = newIds.Where(id => !existingSet.Contains(id)).ToList();
            foreach (var empNo in toAdd)
            {
                _context.esmms_exam_employee.Add(new esmms_exam_employee
                {
                    emp_no = empNo,
                    exam_id = examId,
                    course_id = courseId,
                    pd_line = pdLineValue,
                    is_active = true,
                    assigned_at = DateTime.Now,
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now,
                    created_by = userName,
                    updated_by = userName
                });
            }

            await _context.SaveChangesAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExam(int id)
        {
            var entity = await _context.esmms_exam.FirstOrDefaultAsync(x => x.id == id);
            if (entity == null)
                return ErrorResponse("ไม่พบข้อมูลสำหรับลบ");

            // ใช้ทรานแซกชันและลบตารางลูกก่อน เพื่อลดปัญหา FK constraint
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // ลบไฟล์รูปประกอบ (ถ้ามี)
                DeleteExamImage(id);
                // ลบไฟล์ Excel ผลสอบ (ถ้ามี)
                DeleteExamExcelById(id);
                DeleteExamMeta(id);

                var details = await _context.esmms_exam_detail
                    .Where(d => d.exam_id == id)
                    .ToListAsync();
                if (details.Count > 0)
                {
                    _context.esmms_exam_detail.RemoveRange(details);
                    await _context.SaveChangesAsync();
                }

                _context.esmms_exam.Remove(entity);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();
                return SuccessResponse(message: "ลบข้อมูลสำเร็จ");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return ErrorResponse($"ลบไม่สำเร็จ: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetExamDetails(int id)
        {
            // หากยังไม่มีรายละเอียด ให้สร้างจากการสุ่มตามคอร์สของชุดสอบ
            var hasDetails = await _context.esmms_exam_detail.AnyAsync(d => d.exam_id == id);
            if (!hasDetails)
            {
                var exam = await _context.esmms_exam.FirstOrDefaultAsync(e => e.id == id);
                if (exam != null && exam.course_id.HasValue && exam.random_question_count > 0)
                {
                    var qIds = await _context.esmms_question
                        .Where(q => q.is_active && q.course_id == exam.course_id.Value)
                        .OrderBy(x => Guid.NewGuid())
                        .Take(exam.random_question_count)
                        .Select(x => x.id)
                        .ToListAsync();

                    int order = 1;
                    foreach (var qid in qIds)
                    {
                        var q = await _context.esmms_question.AsNoTracking().FirstOrDefaultAsync(x => x.id == qid);
                        if (q == null)
                            continue;

                        if (!TryBuildRandomizedOptions(q, out string? ra, out string? rb, out string? rc, out string? rd, out string? raImg, out string? rbImg, out string? rcImg, out string? rdImg, out string correctKey))
                            continue;

                        _context.esmms_exam_detail.Add(new esmms_exam_detail
                        {
                            exam_id = id,
                            course_id = exam.course_id.Value,
                            question_id = qid,
                            sort_order = order++,
                            choice_a = ra,
                            choice_b = rb,
                            choice_c = rc,
                            choice_d = rd,
                            choice_a_image_path = raImg,
                            choice_b_image_path = rbImg,
                            choice_c_image_path = rcImg,
                            choice_d_image_path = rdImg,
                            corect_randoms = correctKey,
                            created_at = DateTime.Now,
                            updated_at = DateTime.Now,
                            created_by = GetCurrentUserName(),
                            updated_by = GetCurrentUserName(),
                        });
                    }
                    if (qIds.Count > 0)
                    {
                        await _context.SaveChangesAsync();
                    }
                }
            }
            else
            {
                // Backfill: หากรายละเอียดที่มีอยู่ยังไม่มีตัวเลือกแบบสุ่ม ให้สุ่มและบันทึกค่าลง detail
                var detailsToFix = await _context.esmms_exam_detail
                    .Where(d => d.exam_id == id &&
                                (
                                    (d.choice_a == null && d.choice_b == null && d.choice_c == null && d.choice_d == null) ||
                                    string.IsNullOrWhiteSpace(d.corect_randoms)
                                ))
                    .ToListAsync();

                if (detailsToFix.Count > 0)
                {
                    var userName = GetCurrentUserName() ?? "system";
                    foreach (var d in detailsToFix)
                    {
                        var q = await _context.esmms_question.AsNoTracking().FirstOrDefaultAsync(x => x.id == d.question_id);
                        if (q == null) continue;

                        if (TryBuildRandomizedOptions(q, out string? ra, out string? rb, out string? rc, out string? rd, out string? raImg, out string? rbImg, out string? rcImg, out string? rdImg, out string correctKey))
                        {
                            d.choice_a = ra;
                            d.choice_b = rb;
                            d.choice_c = rc;
                            d.choice_d = rd;
                            d.choice_a_image_path = raImg;
                            d.choice_b_image_path = rbImg;
                            d.choice_c_image_path = rcImg;
                            d.choice_d_image_path = rdImg;
                            d.corect_randoms = correctKey;
                            d.updated_at = DateTime.Now;
                            d.updated_by = userName;
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }

            // ดึงรายละเอียดพร้อมข้อมูลคำถาม/คอร์ส
            var items = await (
                from d in _context.esmms_exam_detail
                join q in _context.esmms_question on d.question_id equals q.id
                join c in _context.esmms_course on d.course_id equals c.id into gc
                from c in gc.DefaultIfEmpty()
                where d.exam_id == id
                orderby d.sort_order
                select new
                {
                    d.id,
                    d.sort_order,
                    d.question_id,
                    question_text = q.question_text,
                    // ส่งออกตัวเลือกแบบสุ่มที่บันทึกใน detail
                    choice_a = d.choice_a,
                    choice_b = d.choice_b,
                    choice_c = d.choice_c,
                    choice_d = d.choice_d,
                    choice_a_image_path = d.choice_a_image_path,
                    choice_b_image_path = d.choice_b_image_path,
                    choice_c_image_path = d.choice_c_image_path,
                    choice_d_image_path = d.choice_d_image_path,
                    correct_choice = d.corect_randoms,
                    course_code = c != null ? c.course_code : null,
                    course_name = c != null ? c.description_en : null,
                }
            ).ToListAsync();

            return Json(new { data = items });
        }

        [HttpGet]
        public async Task<IActionResult> GetExamEmployeePdlines(int id)
        {
            try
            {
                var values = await _context.esmms_exam_employee
                    .Where(x => x.exam_id == id && x.pd_line != null && x.pd_line != "")
                    .Select(x => x.pd_line)
                    .Distinct()
                    .ToListAsync();
                return Json(new { data = values });
            }
            catch (Exception ex)
            {
                return Json(new { data = new string[0], message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetExamEmployeePdlineMap(int id)
        {
            try
            {
                var list = await _context.esmms_exam_employee
                    .Where(x => x.exam_id == id)
                    .Select(x => new { x.emp_no, x.pd_line })
                    .ToListAsync();
                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return Json(new { data = new object[0], message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTrainingStatusForEmployees(string personIds)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(personIds))
                    return Json(new { data = new object[0] });

                var ids = personIds
                    .Split(new[] { ',', '\n', '\r', '\t', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (ids.Count == 0)
                    return Json(new { data = new object[0] });

                var list = await (
                    from ee in _context.esmms_exam_employee
                    join e in _context.esmms_exam on ee.exam_id equals e.id into ge
                    from e in ge.DefaultIfEmpty()
                    join c in _context.esmms_course on e.course_id equals c.id into gc
                    from c in gc.DefaultIfEmpty()
                    where ids.Contains(ee.emp_no)
                    select new
                    {
                        emp_no = ee.emp_no,
                        pd_line = ee.pd_line,
                        exam_id = (int?)e.id,
                        exam_title = e.title,
                        course_code = c != null ? c.course_code : null,
                        course_name = c != null ? c.description_en : null,
                        expire_at = ee.expire_at,
                        assigned_at = (DateTime?)ee.assigned_at,
                        updated_at = (DateTime?)ee.updated_at,
                        score = ee.score,
                        // ใช้ snapshot ถ้ามี ไม่งั้น fallback ไปที่ค่าในคอร์ส
                        pass_score = ee.pass_score ?? (c != null ? (int?)c.pass_score : null),
                        is_passed = ee.is_passed
                    }
                ).ToListAsync();

                // เลือกเรคอร์ดที่เหมาะสมที่สุดต่อ emp: future ใกล้สุด ถ้าไม่มีให้ใช้ล่าสุด
                var now = DateTime.Now;
                var result = new List<object>();
                foreach (var group in list.GroupBy(x => x.emp_no, StringComparer.OrdinalIgnoreCase))
                {
                    // เลือกเรคอร์ดล่าสุดตาม updated_at > assigned_at > expire_at
                    var chosen = group
                        .OrderByDescending(x => x.updated_at ?? x.assigned_at ?? x.expire_at ?? DateTime.MinValue)
                        .FirstOrDefault();

                    if (chosen == null)
                    {
                        result.Add(new
                        {
                            emp_no = group.Key,
                            pd_line = (string)null,
                            exam_title = (string)null,
                            course_code = (string)null,
                            course_name = (string)null,
                            end_at = (DateTime?)null,
                            status = "red",
                            days_left = (int?)null
                        });
                        continue;
                    }

                    int? daysLeft = null;
                    string status;
                    bool? isPassed = chosen.is_passed; // ใช้ค่าที่บันทึก หากไม่มีให้คำนวณ
                    int? score = chosen.score;
                    int? passScore = chosen.pass_score;
                    if (!isPassed.HasValue && score.HasValue && passScore.HasValue)
                    {
                        isPassed = score.Value >= passScore.Value;
                    }
                    if (!chosen.expire_at.HasValue)
                    {
                        // มีชุดสอบแล้วแต่ยังไม่มีวันหมดอายุ แปลว่ายังดำเนินการอยู่
                        status = chosen.exam_id.HasValue ? "blue" : "red";
                    }
                    else
                    {
                        daysLeft = (int)Math.Floor((chosen.expire_at.Value - now).TotalDays);
                        if (daysLeft <= 0) status = "red"; // หมดอายุ
                        else if (daysLeft <= 30) status = "yellow"; // ใกล้หมดอายุ
                        else status = "green"; // ปกติ
                    }

                    result.Add(new
                    {
                        emp_no = chosen.emp_no,
                        pd_line = chosen.pd_line,
                        exam_title = chosen.exam_title,
                        course_code = chosen.course_code,
                        course_name = chosen.course_name,
                        end_at = chosen.expire_at,
                        assigned_at = chosen.assigned_at,
                        status,
                        days_left = daysLeft,
                        score = score,
                        pass_score = passScore,
                        is_passed = isPassed
                    });
                }

                // เติมพนักงานที่ไม่มีข้อมูลเลย
                var existingEmp = new HashSet<string>(result.Select(r => (string)r.GetType().GetProperty("emp_no").GetValue(r)), StringComparer.OrdinalIgnoreCase);
                foreach (var id in ids)
                {
                    if (!existingEmp.Contains(id))
                    {
                        result.Add(new
                        {
                            emp_no = id,
                            pd_line = (string)null,
                            exam_title = (string)null,
                            course_code = (string)null,
                            course_name = (string)null,
                            end_at = (DateTime?)null,
                            status = "red",
                            days_left = (int?)null
                        });
                    }
                }

                return Json(new { data = result });
            }
            catch (Exception ex)
            {
                return Json(new { data = new object[0], message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetExamHistoryForEmployee(string empNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(empNo))
                    return Json(new { data = new object[0] });

                var list = await (
                    from ee in _context.esmms_exam_employee
                    join e in _context.esmms_exam on ee.exam_id equals e.id into ge
                    from e in ge.DefaultIfEmpty()
                    join c in _context.esmms_course on e.course_id equals c.id into gc
                    from c in gc.DefaultIfEmpty()
                    where ee.emp_no == empNo
                    orderby ee.assigned_at descending
                    select new
                    {
                        exam_id = (int?)e.id,
                        exam_title = e.title,
                        course_code = c != null ? c.course_code : null,
                        course_name = c != null ? c.description_en : null,
                        score = ee.score,
                        pd_line = ee.pd_line,
                        expire_at = ee.expire_at,
                        assigned_at = ee.assigned_at
                    }
                ).ToListAsync();

                return Json(new { data = list });
            }
            catch (Exception ex)
            {
                return Json(new { data = new object[0], message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateExam(int id)
        {
            var exam = await _context.esmms_exam.FirstOrDefaultAsync(e => e.id == id);
            if (exam == null)
                return ErrorResponse("ไม่พบข้อมูลชุดสอบ");

            if (!exam.course_id.HasValue || exam.random_question_count <= 0)
                return ErrorResponse("ข้อมูลคอร์สหรือจำนวนข้อสุ่มไม่ถูกต้อง");

            // ลบ detail เดิม
            var details = _context.esmms_exam_detail.Where(d => d.exam_id == id);
            _context.esmms_exam_detail.RemoveRange(details);
            await _context.SaveChangesAsync();

            // สุ่มใหม่
            var qIds = await _context.esmms_question
                .Where(q => q.is_active && q.course_id == exam.course_id.Value)
                .OrderBy(x => Guid.NewGuid())
                .Take(exam.random_question_count)
                .Select(x => x.id)
                .ToListAsync();

            if (qIds.Count == 0)
                return ErrorResponse("ไม่พบคำถามที่ใช้งานในคอร์สนี้");

            var userName = GetCurrentUserName() ?? "system";
            int order = 1;
            foreach (var qid in qIds)
            {
                var q = await _context.esmms_question.AsNoTracking().FirstOrDefaultAsync(x => x.id == qid);
                if (q == null)
                    continue;

                if (!TryBuildRandomizedOptions(q, out string? ra, out string? rb, out string? rc, out string? rd, out string? raImg, out string? rbImg, out string? rcImg, out string? rdImg, out string correctKey))
                    continue;

                _context.esmms_exam_detail.Add(new esmms_exam_detail
                {
                    exam_id = id,
                    course_id = exam.course_id.Value,
                    question_id = qid,
                    sort_order = order++,
                    choice_a = ra,
                    choice_b = rb,
                    choice_c = rc,
                    choice_d = rd,
                    choice_a_image_path = raImg,
                    choice_b_image_path = rbImg,
                    choice_c_image_path = rcImg,
                    choice_d_image_path = rdImg,
                    corect_randoms = correctKey,
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now,
                    created_by = userName,
                    updated_by = userName
                });
            }
            await _context.SaveChangesAsync();

            return SuccessResponse(message: "สุ่มและเรียงข้อใหม่เรียบร้อยแล้ว");
        }

        [HttpGet]
        public async Task<IActionResult> PrintExam(int id)
        {
            var exam = await _context.esmms_exam.FirstOrDefaultAsync(e => e.id == id);
            if (exam == null)
                return Content("ไม่พบชุดสอบ");

            // ให้แน่ใจว่ามีรายละเอียดแล้ว
            var hasDetails = await _context.esmms_exam_detail.AnyAsync(d => d.exam_id == id);
            if (!hasDetails)
            {
                var gen = await GetExamDetails(id) as JsonResult; // สร้าง detail หากยังไม่มี
            }

            // Backfill: หากมี detail ที่ยังไม่มีตัวเลือกแบบสุ่ม ให้สุ่มและบันทึกก่อนพิมพ์
            var needsFill = await _context.esmms_exam_detail
                .Where(d => d.exam_id == id &&
                            ((d.choice_a == null && d.choice_b == null && d.choice_c == null && d.choice_d == null) ||
                             string.IsNullOrWhiteSpace(d.corect_randoms)))
                .ToListAsync();
            if (needsFill.Count > 0)
            {
                var userName = GetCurrentUserName() ?? "system";
                foreach (var d in needsFill)
                {
                    var q = await _context.esmms_question.AsNoTracking().FirstOrDefaultAsync(x => x.id == d.question_id);
                    if (q == null) continue;
                    if (TryBuildRandomizedOptions(q, out string? ra, out string? rb, out string? rc, out string? rd, out string? raImg, out string? rbImg, out string? rcImg, out string? rdImg, out string correctKey))
                    {
                        d.choice_a = ra;
                        d.choice_b = rb;
                        d.choice_c = rc;
                        d.choice_d = rd;
                        d.corect_randoms = correctKey;
                        d.updated_at = DateTime.Now;
                        d.updated_by = userName;
                        d.choice_a_image_path = raImg;
                        d.choice_b_image_path = rbImg;
                        d.choice_c_image_path = rcImg;
                        d.choice_d_image_path = rdImg;
                    }
                }
                await _context.SaveChangesAsync();
            }

            var items = await (
                from d in _context.esmms_exam_detail
                join q in _context.esmms_question on d.question_id equals q.id
                where d.exam_id == id
                orderby d.sort_order
                select new ExamPrintItem
                {
                    sort_order = d.sort_order,
                    question_text = q.question_text,
                    choice_a = d.choice_a,
                    choice_b = d.choice_b,
                    choice_c = d.choice_c,
                    choice_d = d.choice_d,
                    correct_answer = d.corect_randoms,
                    image_path = q.image_path,
                    choice_a_image_path = d.choice_a_image_path,
                    choice_b_image_path = d.choice_b_image_path,
                    choice_c_image_path = d.choice_c_image_path,
                    choice_d_image_path = d.choice_d_image_path
                }
            ).ToListAsync();

            ViewBag.ExamTitle = exam.title;
            ViewBag.Description = exam.description;
            ViewBag.ExamImageUrl = GetExamImageVirtualPath(id);
            return View("~/Views/ESMMS/exam_print.cshtml", items);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadExamDocx(int id)
        {
            var exam = await _context.esmms_exam.FirstOrDefaultAsync(e => e.id == id);
            if (exam == null)
                return Content("ไม่พบชุดสอบ");

            var items = await (
                from d in _context.esmms_exam_detail
                join q in _context.esmms_question on d.question_id equals q.id
                where d.exam_id == id
                orderby d.sort_order
                select new ExamPrintItem
                {
                    sort_order = d.sort_order,
                    question_text = q.question_text,
                    choice_a = q.choice_a,
                    choice_b = q.choice_b,
                    choice_c = q.choice_c,
                    choice_d = q.choice_d,
                    correct_answer = q.correct_answer,
                    image_path = q.image_path
                }
            ).ToListAsync();

            using var mem = new MemoryStream();
            using (var doc = WordprocessingDocument.Create(mem, WordprocessingDocumentType.Document, true))
            {
                var main = doc.AddMainDocumentPart();
                main.Document = new Document(new Body());
                var body = main.Document.Body;

                // ฟังก์ชันช่วยสร้างย่อหน้าด้วยฟอนต์ Arial
                Paragraph MakePara(string text, bool bold = false)
                {
                    var rPr = new RunProperties();
                    rPr.Append(new RunFonts() { Ascii = "Arial", HighAnsi = "Arial", EastAsia = "Arial", ComplexScript = "Arial" });
                    if (bold) rPr.Append(new Bold());
                    var run = new Run(rPr, new Text(text ?? string.Empty) { Space = SpaceProcessingModeValues.Preserve });
                    var pPr = new ParagraphProperties();
                    pPr.Append(new ParagraphMarkRunProperties(new RunFonts() { Ascii = "Arial", HighAnsi = "Arial", EastAsia = "Arial", ComplexScript = "Arial" }));
                    var p = new Paragraph(pPr);
                    p.Append(run);
                    return p;
                }

                // แปลง path จาก DB ให้เป็น path จริงใน wwwroot
                string? MapImagePhysicalPath(string? imagePath)
                {
                    if (string.IsNullOrWhiteSpace(imagePath)) return null;
                    try
                    {
                        var input = imagePath.Trim();
                        if (System.IO.Path.IsPathRooted(input))
                        {
                            return System.IO.File.Exists(input) ? input : null;
                        }

                        input = input.Replace("\\", "/");
                        if (input.StartsWith("~/")) input = input.Substring(2);
                        if (input.StartsWith("/")) input = input.Substring(1);

                        var combined = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", input);
                        return System.IO.File.Exists(combined) ? combined : null;
                    }
                    catch
                    {
                        return null;
                    }
                }

                // ระบุชนิดรูปภาพจากนามสกุลไฟล์
                PartTypeInfo GetImagePartTypeByExtension(string filePath)
                {
                    var ext = System.IO.Path.GetExtension(filePath)?.ToLowerInvariant();
                    return ext switch
                    {
                        ".png" => ImagePartType.Png,
                        ".gif" => ImagePartType.Gif,
                        ".bmp" => ImagePartType.Bmp,
                        ".tiff" => ImagePartType.Tiff,
                        ".ico" => ImagePartType.Icon,
                        _ => ImagePartType.Jpeg,
                    };
                }

                // เพิ่มย่อหน้ารูปภาพ โดยฝังเป็น ImagePart และแทรก Drawing
                void AppendImageParagraph(MainDocumentPart mainPart, string physicalPath, string displayName)
                {
                    var imgType = GetImagePartTypeByExtension(physicalPath);
                    var imagePart = mainPart.AddImagePart(imgType);
                    using (var stream = System.IO.File.OpenRead(physicalPath))
                    {
                        imagePart.FeedData(stream);
                    }

                    var relId = mainPart.GetIdOfPart(imagePart);

                    long pxToEmu(int px) => (long)px * 9525L;
                    var cx = pxToEmu(500);
                    var cy = pxToEmu(350);

                    var element = new Drawing(
                        new DW.Inline(
                            new DW.Extent() { Cx = cx, Cy = cy },
                            new DW.EffectExtent() { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                            new DW.DocProperties() { Id = 1U, Name = displayName ?? "Picture" },
                            new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks() { NoChangeAspect = true }),
                            new A.Graphic(
                                new A.GraphicData(
                                    new PIC.Picture(
                                        new PIC.NonVisualPictureProperties(
                                            new PIC.NonVisualDrawingProperties() { Id = 0U, Name = displayName ?? "Image" },
                                            new PIC.NonVisualPictureDrawingProperties()
                                        ),
                                        new PIC.BlipFill(
                                            new A.Blip() { Embed = relId, CompressionState = A.BlipCompressionValues.Print },
                                            new A.Stretch(new A.FillRectangle())
                                        ),
                                        new PIC.ShapeProperties(
                                            new A.Transform2D(
                                                new A.Offset() { X = 0L, Y = 0L },
                                                new A.Extents() { Cx = cx, Cy = cy }
                                            ),
                                            new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }
                                        )
                                    )
                                ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                            )
                        ) { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U }
                    );

                    var rPr = new RunProperties();
                    rPr.Append(new RunFonts() { Ascii = "Arial", HighAnsi = "Arial", EastAsia = "Arial", ComplexScript = "Arial" });
                    var run = new Run(rPr);
                    run.Append(element);
                    var pPr = new ParagraphProperties();
                    pPr.Append(new ParagraphMarkRunProperties(new RunFonts() { Ascii = "Arial", HighAnsi = "Arial", EastAsia = "Arial", ComplexScript = "Arial" }));
                    var paragraph = new Paragraph(pPr);
                    paragraph.Append(run);
                    body.AppendChild(paragraph);
                }

                body.AppendChild(MakePara(exam.title ?? "Exam", bold: true));

                if (!string.IsNullOrWhiteSpace(exam.description))
                {
                    body.AppendChild(MakePara(exam.description));
                }

                int idx = 1;
                foreach (var q in items)
                {
                    body.AppendChild(MakePara($"{idx}. {q.question_text}"));
                    if (!string.IsNullOrWhiteSpace(q.choice_a)) body.AppendChild(MakePara($"A. {q.choice_a}"));
                    if (!string.IsNullOrWhiteSpace(q.choice_b)) body.AppendChild(MakePara($"B. {q.choice_b}"));
                    if (!string.IsNullOrWhiteSpace(q.choice_c)) body.AppendChild(MakePara($"C. {q.choice_c}"));
                    if (!string.IsNullOrWhiteSpace(q.choice_d)) body.AppendChild(MakePara($"D. {q.choice_d}"));

                    var imgPath = MapImagePhysicalPath(q.image_path);
                    if (!string.IsNullOrWhiteSpace(imgPath))
                    {
                        try
                        {
                            AppendImageParagraph(main, imgPath!, System.IO.Path.GetFileName(imgPath));
                        }
                        catch { }
                    }

                    body.AppendChild(new Paragraph(new Run(new Break())));
                    idx++;
                }

                main.Document.Save();
            }

            mem.Position = 0;
            var fileName = (exam.title ?? "Exam") + ".docx";
            return File(mem.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadExamDocxForForms(int id)
        {
            var exam = await _context.esmms_exam.FirstOrDefaultAsync(e => e.id == id);
            if (exam == null)
                return Content("ไม่พบชุดสอบ");

            var items = await (
                from d in _context.esmms_exam_detail
                join q in _context.esmms_question on d.question_id equals q.id
                where d.exam_id == id
                orderby d.sort_order
                select new ExamPrintItem
                {
                    sort_order = d.sort_order,
                    question_text = q.question_text,
                    choice_a = q.choice_a,
                    choice_b = q.choice_b,
                    choice_c = q.choice_c,
                    choice_d = q.choice_d,
                    correct_answer = q.correct_answer
                }
            ).ToListAsync();

            string SanitizeForForms(string? input)
            {
                if (string.IsNullOrWhiteSpace(input)) return string.Empty;
                var s = input.Trim(); 
                // ลบอักขระควบคุมที่ไม่พิมพ์
                s = Regex.Replace(s, "[\\u0000-\\u001F\\u007F]", string.Empty);
                // รวมช่องว่างซ้ำ ๆ
                s = Regex.Replace(s, "[ ]{2,}", " ");
                // แก้ % ให้เป็นเดียว และตัดช่องว่างก่อน %
                s = s.Replace("%%", "%");
                s = Regex.Replace(s, "\\s%", "%");
                return s;
            }

            using var mem = new MemoryStream();
            using (var doc = WordprocessingDocument.Create(mem, WordprocessingDocumentType.Document, true))
            {
                var main = doc.AddMainDocumentPart();
                main.Document = new Document(new Body());
                var body = main.Document.Body;

                Paragraph MakePlainPara(string text)
                {
                    var rPr = new RunProperties();
                    rPr.Append(new RunFonts() { Ascii = "Arial", HighAnsi = "Arial", EastAsia = "Arial", ComplexScript = "Arial" });
                    var run = new Run(rPr, new Text(text ?? string.Empty) { Space = SpaceProcessingModeValues.Preserve });
                    var pPr = new ParagraphProperties();
                    pPr.Append(new ParagraphMarkRunProperties(new RunFonts() { Ascii = "Arial", HighAnsi = "Arial", EastAsia = "Arial", ComplexScript = "Arial" }));
                    var p = new Paragraph(pPr);
                    p.Append(run);
                    return p;
                }

                // หัวกระดาษแบบเรียบง่าย (ไม่ใช้ Heading เพื่อไม่ให้ Forms แยก Section)
                var title = string.IsNullOrWhiteSpace(exam.title) ? "Exam" : exam.title;
                body.AppendChild(MakePlainPara(SanitizeForForms(title)));
                if (!string.IsNullOrWhiteSpace(exam.description))
                {
                    body.AppendChild(MakePlainPara(SanitizeForForms(exam.description)));
                }
                body.AppendChild(MakePlainPara(""));

                int idx = 1;
                foreach (var q in items)
                {
                    body.AppendChild(MakePlainPara($"{idx}. {SanitizeForForms(q.question_text)}"));
                    if (!string.IsNullOrWhiteSpace(q.choice_a)) body.AppendChild(MakePlainPara($"A. {SanitizeForForms(q.choice_a)}"));
                    if (!string.IsNullOrWhiteSpace(q.choice_b)) body.AppendChild(MakePlainPara($"B. {SanitizeForForms(q.choice_b)}"));
                    if (!string.IsNullOrWhiteSpace(q.choice_c)) body.AppendChild(MakePlainPara($"C. {SanitizeForForms(q.choice_c)}"));
                    if (!string.IsNullOrWhiteSpace(q.choice_d)) body.AppendChild(MakePlainPara($"D. {SanitizeForForms(q.choice_d)}"));

                    // เว้นบรรทัดคั่นคำถาม (ไม่ใช้ PageBreak/SectionBreak เพื่อกัน Forms แยก Section)
                    body.AppendChild(MakePlainPara(""));
                    idx++;
                }

                main.Document.Save();
            }

            mem.Position = 0;
            var fileName = (exam.title ?? "Exam") + "_for_MSForms.docx";
            return File(mem.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }

        // ==================== Email Recipients (Summary after Excel upload) ====================
        [HttpGet]
        public async Task<IActionResult> GetEmailRecipients(int? course_id)
        {
            try
            {
                // ดึงข้อมูลจาก esmms_employees_receive_mail เท่านั้น
                var existingRecipients = await _context.esmms_employees_receive_mail
                    .Where(x => course_id == null || x.course_id == course_id)
                    .ToListAsync();

                _logger.LogInformation($"GetEmailRecipients: Found {existingRecipients.Count} recipients in database");

                // ถ้าไม่มีข้อมูลเลย ให้ return ข้อมูลว่าง
                if (!existingRecipients.Any())
                {
                    return Json(new { data = new List<object>() });
                }

                // ดึงข้อมูลพนักงานจาก CES941 เพื่อเอาเฉพาะชื่อ
                var employeesWithEmail = await GetEmployeesWithEmailFromCES941();
                var empNameDict = employeesWithEmail.ToDictionary(
                    x => x.EmpNo?.ToString()?.Trim() ?? "", 
                    x => $"{x.PrefixName} {x.EmpName} {x.EmpLName}".Trim()
                );

                // สร้างผลลัพธ์จากข้อมูลที่มีอยู่แล้วเท่านั้น
                var result = new List<object>();
                
                foreach (var recipient in existingRecipients)
                {
                    var fullName = empNameDict.ContainsKey(recipient.emp_no) 
                        ? empNameDict[recipient.emp_no] 
                        : recipient.emp_no; // fallback ถ้าไม่เจอชื่อ
                    
                    result.Add(new
                    {
                        id = recipient.id,
                        emp_no = recipient.emp_no,
                        email = recipient.email,
                        full_name = fullName,
                        course_id = recipient.course_id,
                        is_active = recipient.is_active,
                        created_at = recipient.created_at,
                        updated_at = recipient.updated_at
                    });
                }

                // ไม่ต้องลบข้อมูลที่ inactive ออกโดยอัตโนมัติ
                // ให้แสดงเฉพาะข้อมูลที่ active เท่านั้น
                result = result.Where(x => {
                    var obj = (dynamic)x;
                    return obj.is_active is bool b ? b : bool.TryParse(obj.is_active?.ToString(), out bool parsed) && parsed;
                }).ToList();

                _logger.LogInformation($"GetEmailRecipients: Returning {result.Count} recipients");
                return Json(new { data = result.OrderByDescending(x => {
                    var obj = (dynamic)x;
                    return obj.updated_at is DateTime dt ? dt : DateTime.TryParse(obj.updated_at?.ToString(), out DateTime parsed) ? parsed : DateTime.MinValue;
                }) });
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetEmailRecipients: Error - {ex.Message}");
                return Json(new { data = new object[0], message = ex.Message });
            }
        }

        private async Task<List<dynamic>> GetEmployeesWithEmailFromCES941()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("CES941");
                using (var connection = new SqlConnection(connectionString))
                {
                    var sql = @"
                        SELECT EmpNo, PrefixName, EmpName, EmpLName, eMailAdd, WorkArea
                        FROM vEmployee 
                        WHERE Language = 'en' 
                        AND eMailAdd IS NOT NULL 
                        AND eMailAdd != ''";
                    
                    var employees = await connection.QueryAsync(sql);
                    return employees.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching employees with email from CES941: {ex.Message}");
                return new List<dynamic>();
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetAvailableEmployees(int? course_id)
        {
            try
            {
                // ดึงข้อมูลพนักงานทั้งหมดที่มี email จาก CES941
                var employeesWithEmail = await GetEmployeesWithEmailFromCES941();
                
                // ดึงข้อมูลที่มีอยู่แล้วใน esmms_employees_receive_mail
                var existingEmpNos = await _context.esmms_employees_receive_mail
                    .Where(x => course_id == null || x.course_id == course_id)
                    .Select(x => x.emp_no)
                    .ToListAsync();

                // กรองเฉพาะคนที่ยังไม่มีใน esmms_employees_receive_mail
                var availableEmployees = employeesWithEmail
                    .Where(emp => 
                    {
                        var empNo = emp.EmpNo?.ToString()?.Trim();
                        return !string.IsNullOrEmpty(empNo) && 
                               !string.IsNullOrEmpty(emp.eMailAdd?.ToString()?.Trim()) &&
                               !existingEmpNos.Contains(empNo);
                    })
                    .Select(emp => new
                    {
                        emp_no = emp.EmpNo?.ToString()?.Trim(),
                        email = emp.eMailAdd?.ToString()?.Trim(),
                        full_name = $"{emp.PrefixName} {emp.EmpName} {emp.EmpLName}".Trim(),
                        work_area = emp.WorkArea?.ToString()?.Trim()
                    })
                    .OrderBy(x => x.full_name)
                    .ToList();

                return Json(new { data = availableEmployees });
            }
            catch (Exception ex)
            {
                return Json(new { data = new object[0], message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmailRecipient(string emp_no,string full_name, string email, int? course_id, bool is_active = true)
        {
            // ไทย: เพิ่ม/อัปเดตรายชื่อผู้รับอีเมลสรุป / 简中: 新增/更新汇总邮件收件人
            if (string.IsNullOrWhiteSpace(emp_no) || string.IsNullOrWhiteSpace(email))
                return ErrorResponse("กรุณาระบุ emp_no และ email");

            var now = DateTime.Now;
            var userName = GetCurrentUserName() ?? "system";

            // กันข้อมูลซ้ำตามคู่ emp_no + course_id (หรือเฉพาะ emp_no ถ้า course_id เป็น null)
            var exists = await _context.esmms_employees_receive_mail
                .Where(x => x.emp_no == emp_no && x.course_id == course_id)
                .FirstOrDefaultAsync();

            if (exists == null)
            {
                _context.esmms_employees_receive_mail.Add(new esmms_employees_receive_mail
                {
                    emp_no = emp_no.Trim(),
                    email = email.Trim(),
                    full_name = full_name?.Trim(),
                    course_id = course_id,
                    is_active = is_active,
                    created_at = now,
                    updated_at = now,
                    created_by = userName,
                    updated_by = userName
                });
            }
            else
            {
                exists.email = email.Trim();
                exists.full_name = full_name?.Trim();
                exists.is_active = is_active;
                exists.updated_at = now;
                exists.updated_by = userName;
            }

            await _context.SaveChangesAsync();
            return SuccessResponse(message: "บันทึกผู้รับอีเมลสรุปสำเร็จ");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEmailRecipient(int id, bool is_active)
        {
            try
            {
                if (!is_active)
                {
                    // ถ้าปิด active ให้ลบข้อมูลออกโดยตรง
                    var deletedRows = await _context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM esmms_employees_receive_mail WHERE id = {0}", id);
                    
                    if (deletedRows > 0)
                    {
                        return SuccessResponse(message: "ลบข้อมูลสำเร็จ");
                    }
                    else
                    {
                        return ErrorResponse("ไม่พบข้อมูลหรือข้อมูลถูกลบไปแล้ว");
                    }
                }
                else
                {
                    // ถ้าเปิด active ให้อัปเดตสถานะ
                    var updatedRows = await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE esmms_employees_receive_mail SET is_active = 1, updated_at = {0}, updated_by = {1} WHERE id = {2}",
                        DateTime.Now, GetCurrentUserName() ?? "System", id);
                    
                    if (updatedRows > 0)
                    {
                        return SuccessResponse(message: "เปิดใช้งานสำเร็จ");
                    }
                    else
                    {
                        return ErrorResponse("ไม่พบข้อมูลหรือข้อมูลถูกลบไปแล้ว");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ToggleEmailRecipient: Error - {ex.Message}");
                return ErrorResponse($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CleanupInactiveRecipients()
        {
            try
            {
                // ลบข้อมูลที่ inactive ออกจากฐานข้อมูล
                var deletedRows = await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM esmms_employees_receive_mail WHERE is_active = 0");
                
                _logger.LogInformation($"CleanupInactiveRecipients: Deleted {deletedRows} inactive recipients");
                return SuccessResponse(message: $"ลบข้อมูล inactive {deletedRows} รายการสำเร็จ");
            }
            catch (Exception ex)
            {
                _logger.LogError($"CleanupInactiveRecipients: Error - {ex.Message}");
                return ErrorResponse($"เกิดข้อผิดพลาด: {ex.Message}");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmailRecipient(int id)
        {
            // ไทย: ลบผู้รับ / 简中: 删除收件人
            var entity = await _context.esmms_employees_receive_mail.FirstOrDefaultAsync(x => x.id == id);
            if (entity == null) return ErrorResponse("ไม่พบข้อมูล");
            _context.esmms_employees_receive_mail.Remove(entity);
            await _context.SaveChangesAsync();
            return SuccessResponse();
        }

        // ---------------------- Helper: Exam image handling ----------------------
        private static readonly string[] AllowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };

        private string EnsureUploadsExamDirectory()
        {
            var root = Directory.GetCurrentDirectory();
            var dir = Path.Combine(root, "wwwroot", "uploads", "exams");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        private string GetExamImagePhysicalPathByIdAndExt(int id, string ext)
        {
            var dir = EnsureUploadsExamDirectory();
            var safeExt = ext.StartsWith('.') ? ext.ToLowerInvariant() : "." + ext.ToLowerInvariant();
            var file = Path.Combine(dir, $"exam_{id}{safeExt}");
            return file;
        }

        private string? FindExistingExamImagePhysicalPath(int id)
        {
            var dir = EnsureUploadsExamDirectory();
            foreach (var ext in AllowedImageExtensions)
            {
                var candidate = Path.Combine(dir, $"exam_{id}{ext}");
                if (System.IO.File.Exists(candidate)) return candidate;
            }
            return null;
        }

        private string? GetExamImageVirtualPath(int id)
        {
            var path = FindExistingExamImagePhysicalPath(id);
            if (string.IsNullOrWhiteSpace(path)) return null;
            var fileName = Path.GetFileName(path);
            return Url.Content($"~/uploads/exams/{fileName}");
        }

        // ---------------------- Helper: Exam excel (.xlsx) handling ----------------------
        private string GetExamExcelPhysicalPathById(int id)
        {
            var dir = EnsureUploadsExamDirectory();
            return Path.Combine(dir, $"exam_{id}.xlsx");
        }

        private string? GetExamExcelVirtualPath(int id)
        {
            var physical = GetExamExcelPhysicalPathById(id);
            if (!System.IO.File.Exists(physical)) return null;
            var fileName = Path.GetFileName(physical);
            return Url.Content($"~/uploads/exams/{fileName}");
        }

        private async Task<string> SaveExamExcelAsync(int id, IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
            if (ext != ".xlsx") throw new InvalidOperationException("ชนิดไฟล์ไม่รองรับ ให้ใช้ .xlsx เท่านั้น");

            var fullPath = GetExamExcelPhysicalPathById(id);
            using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }

            var virtualPath = $"~/uploads/exams/{Path.GetFileName(fullPath)}";
            return virtualPath;
        }

        private void DeleteExamExcelById(int id)
        {
            try
            {
                var fullPath = GetExamExcelPhysicalPathById(id);
                if (!string.IsNullOrWhiteSpace(fullPath) && System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch { }
        }

        private void DeleteExamImage(int id)
        {
            try
            {
                var existing = FindExistingExamImagePhysicalPath(id);
                if (!string.IsNullOrWhiteSpace(existing) && System.IO.File.Exists(existing))
                {
                    System.IO.File.Delete(existing);
                }
            }
            catch { }
        }

        private async Task SaveExamImageAsync(int id, IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
            if (!AllowedImageExtensions.Contains(ext))
                throw new InvalidOperationException("ชนิดไฟล์ไม่รองรับ ให้ใช้ .jpg .jpeg .png หรือ .gif");

            // ลบรูปเดิมก่อนเพื่อให้เหลือไฟล์เดียวต่อ id
            DeleteExamImage(id);

            var fullPath = GetExamImagePhysicalPathByIdAndExt(id, ext);
            using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }
        }

        private string GetExamMetaPath(int id)
        {
            var dir = EnsureUploadsExamDirectory();
            return Path.Combine(dir, $"exam_{id}.json");
        }

        private bool GetExamCompleted(int id)
        {
            try
            {
                var path = GetExamMetaPath(id);
                if (!System.IO.File.Exists(path)) return false;
                var json = System.IO.File.ReadAllText(path);
                var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                if (dict != null && dict.TryGetValue("is_completed", out var val) && val != null)
                {
                    if (val is bool b) return b;
                    if (bool.TryParse(val.ToString(), out var parsed)) return parsed;
                }
            }
            catch { }
            return false;
        }

        private void SetExamCompletedMeta(int id, bool isCompleted)
        {
            try { UpdateExamMeta(id, new Dictionary<string, object> { { "is_completed", isCompleted } }); }
            catch { }
        }

        private void DeleteExamMeta(int id)
        {
            try
            {
                var path = GetExamMetaPath(id);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            catch { }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetExamCompleted(int id, bool is_completed)
        {
            var exists = _context.esmms_exam.Any(e => e.id == id);
            if (!exists) return ErrorResponse("ไม่พบชุดสอบ");

            if (!is_completed)
            {
                try
                {
                    var exam = _context.esmms_exam.FirstOrDefault(e => e.id == id);
                    if (exam != null)
                    {
                        int? courseId = exam.course_id;
                        // ลบเฉพาะข้อมูลของคอร์สนี้ หากไม่มี course_id ให้ลบเฉพาะที่อ้างถึง exam นี้
                        // เลี่ยงการ materialize entity ทั้งตัว (กันปัญหา cast bool/string)
                        var idsToRemove = courseId.HasValue
                            ? _context.esmms_exam_employee
                                .Where(ee => ee.course_id == courseId.Value)
                                .Select(ee => ee.id)
                                .ToList()
                            : _context.esmms_exam_employee
                                .Where(ee => ee.exam_id == id)
                                .Select(ee => ee.id)
                                .ToList();

                        if (idsToRemove.Count > 0)
                        {
                            var stubs = idsToRemove.Select(x => new esmms_exam_employee { id = x }).ToList();
                            _context.esmms_exam_employee.AttachRange(stubs);
                            _context.esmms_exam_employee.RemoveRange(stubs);
                            _context.SaveChanges();
                        }
                    }
                }
                catch (Exception ex)
                {
                    return ErrorResponse("ลบรายชื่อพนักงานไม่สำเร็จ: " + ex.Message);
                }
            }

            SetExamCompletedMeta(id, is_completed);
            if (is_completed)
            {
                try { await SendCourseSummaryEmail(id); } catch { }
            }
            return SuccessResponse(new { id, is_completed }, message: "อัปเดตสถานะแล้ว");
        }

        private Dictionary<string, object> ReadExamMeta(int id)
        {
            try
            {
                var path = GetExamMetaPath(id);
                if (!System.IO.File.Exists(path)) return new Dictionary<string, object>();
                var json = System.IO.File.ReadAllText(path);
                var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                return dict ?? new Dictionary<string, object>();
            }
            catch { return new Dictionary<string, object>(); }
        }

        private void UpdateExamMeta(int id, Dictionary<string, object> updates)
        {
            var meta = ReadExamMeta(id);
            if (updates != null)
            {
                foreach (var kv in updates)
                {
                    // ถ้า value เป็น int และเท่ากับ 0 ให้ยังคงค่าเดิม (หลีกเลี่ยง set null/0 โดยไม่ตั้งใจ)
                    if (kv.Value is int iv && iv == 0)
                    {
                        if (!meta.ContainsKey(kv.Key)) meta[kv.Key] = iv;
                        continue;
                    }
                    // เก็บค่าเดิมไว้ตามประเภทข้อมูล ไม่แปลงเป็น string
                    meta[kv.Key] = kv.Value ?? string.Empty;
                }
            }
            var path = GetExamMetaPath(id);
            System.IO.File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(meta));
        }

        // ดึงข้อมูลพนักงานทั้งหมดสำหรับการส่งอีเมล
        private async Task<List<dynamic>> GetEmployeeDataForEmail()
        {
            try
            {
                using (var conn = new SqlConnection(_ces941ConnectionString))
                {
                    var sql = @"
                        SELECT 
                            WorkArea AS deptName, 
                            PrefixName + ' ' + EmpName + ' ' + EmpLName AS name, 
                            EmpNo AS emp_no
                        FROM vEmployee
                        WHERE (Language = 'EN')";
                    return (await conn.QueryAsync(sql)).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting employee data for email: {ex.Message}");
                return new List<dynamic>();
            }
        }

        // Helper methods for data conversion
        private static bool ConvertToBoolean(object? value)
        {
            if (value == null) return false;
            if (value is bool b) return b;
            if (value is string s)
            {
                if (bool.TryParse(s, out var parsed)) return parsed;
                if (s.Equals("1", StringComparison.OrdinalIgnoreCase) || 
                    s.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                    s.Equals("Y", StringComparison.OrdinalIgnoreCase) ||
                    s.Equals("Yes", StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }
            return false;
        }

        private static bool? ConvertToNullableBoolean(object? value)
        {
            if (value == null) return null;
            if (value is bool b) return b;
            if (value is string s)
            {
                if (bool.TryParse(s, out var parsed)) return parsed;
                if (s.Equals("1", StringComparison.OrdinalIgnoreCase) || 
                    s.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                    s.Equals("Y", StringComparison.OrdinalIgnoreCase) ||
                    s.Equals("Yes", StringComparison.OrdinalIgnoreCase)) return true;
                if (s.Equals("0", StringComparison.OrdinalIgnoreCase) || 
                    s.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                    s.Equals("N", StringComparison.OrdinalIgnoreCase) ||
                    s.Equals("No", StringComparison.OrdinalIgnoreCase)) return false;
                return null;
            }
            return null;
        }

        // ---------------------- Upload Exam Results (.xlsx) ----------------------
        private static DateTime? ComputeExpireAtFromPeriod(string? period, DateTime start)
        {
            if (string.IsNullOrWhiteSpace(period)) return null;
            var s = period.Trim().ToLowerInvariant();
            // Extract leading integer
            var numStr = new string(s.TakeWhile(char.IsDigit).ToArray());
            if (!int.TryParse(numStr, out var n) || n <= 0)
            {
                // try alternative: split by space
                var parts = s.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && int.TryParse(parts[0], out var n2)) n = n2;
            }
            if (n <= 0) return null;
            // Determine unit
            if (s.Contains("year")) return start.AddYears(n);
            if (s.Contains("month")) return start.AddMonths(n);
            if (s.Contains("day")) return start.AddDays(n);
            if (s.Contains("week")) return start.AddDays(7 * n);
            // Thai words (basic)
            if (s.Contains("ปี")) return start.AddYears(n);
            if (s.Contains("เดือน")) return start.AddMonths(n);
            if (s.Contains("วัน")) return start.AddDays(n);
            return null;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExamResults(int id, IFormFile? file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return ErrorResponse("กรุณาเลือกไฟล์ .xlsx");

                var exam = await _context.esmms_exam.FirstOrDefaultAsync(e => e.id == id);
                if (exam == null) return ErrorResponse("ไม่พบชุดสอบ");

                // Course/expire period
                esmms_course? course = null;
                if (exam.course_id.HasValue)
                {
                    course = await _context.esmms_course.FirstOrDefaultAsync(c => c.id == exam.course_id.Value);
                }

                // รายการผู้เข้าสอบเดิม (ถ้ามี) เพื่อนำมาอัปเดตหรือเพิ่มใหม่เมื่อพบมากกว่าที่ลงทะเบียนไว้
                // ใช้ raw SQL query เพื่อหลีกเลี่ยงปัญหา Boolean casting
                var existingExamEmployees = new List<esmms_exam_employee>();
                try
                {
                    using (var conn = new SqlConnection(_connectionString))
                    {
                        var sql = @"
                            SELECT id, emp_no, course_id, pd_line, exam_id, expire_period_snapshot, 
                                   expire_at, is_active, score, assigned_at, created_at, updated_at, 
                                   created_by, updated_by, is_passed, pass_score
                            FROM esmms_exam_employee 
                            WHERE exam_id = @examId";
                        var rows = await conn.QueryAsync(sql, new { examId = id });
                        
                        foreach (var row in rows)
                        {
                            existingExamEmployees.Add(new esmms_exam_employee
                            {
                                id = row.id,
                                emp_no = row.emp_no ?? string.Empty,
                                course_id = row.course_id,
                                pd_line = row.pd_line,
                                exam_id = row.exam_id,
                                expire_period_snapshot = row.expire_period_snapshot,
                                expire_at = row.expire_at,
                                is_active = ConvertToBoolean(row.is_active),
                                score = row.score,
                                assigned_at = row.assigned_at,
                                created_at = row.created_at,
                                updated_at = row.updated_at,
                                created_by = row.created_by,
                                updated_by = row.updated_by,
                                is_passed = ConvertToNullableBoolean(row.is_passed),
                                pass_score = row.pass_score
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // ถ้า raw SQL ไม่ได้ ให้ใช้ Entity Framework แต่จัดการ exception
                    try
                    {
                        existingExamEmployees = await _context.esmms_exam_employee
                            .Where(x => x.exam_id == id)
                            .ToListAsync();
                    }
                    catch
                    {
                        // ถ้ายังไม่ได้ ให้สร้าง list ว่าง
                        existingExamEmployees = new List<esmms_exam_employee>();
                    }
                }
                var existingEmpSet = new HashSet<string>(existingExamEmployees
                    .Where(x => !string.IsNullOrWhiteSpace(x.emp_no))
                    .Select(x => x.emp_no!.Trim()), StringComparer.OrdinalIgnoreCase);

                // Parse Excel using EPPlus (EPPlus v8+: set license info explicitly)
                ExcelPackage.License.SetNonCommercialPersonal("IPS-TH");
                var excelRows = new List<(string Name, string? PdlineName, int? Score, string? EmpNo)>();
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    ms.Position = 0;
                    using (var package = new ExcelPackage(ms))
                    {
                        var ws = package.Workbook.Worksheets["Sheet1"] ?? package.Workbook.Worksheets.FirstOrDefault();
                        if (ws == null) return ErrorResponse("ไม่พบชีตข้อมูลในไฟล์");

                        var dim = ws.Dimension;
                        if (dim == null || dim.Rows < 2) return ErrorResponse("ไฟล์ไม่มีข้อมูล");

                        // Find header columns
                        int nameCol = -1, scoreCol = -1, pdlineCol = -1, empNoCol = -1;
                        for (int c = dim.Start.Column; c <= dim.End.Column; c++)
                        {
                            var hRaw = ws.Cells[dim.Start.Row, c].Text ?? string.Empty;
                            var h = (hRaw ?? string.Empty).Replace("\"", string.Empty).Trim().ToLowerInvariant();
                            var isPointsOrFeedback = h.StartsWith("points ") || h.StartsWith("points-") || h.StartsWith("feedback ") || h.StartsWith("feedback-");

                            if (nameCol < 0 && (h == "name" || h.Contains("name") || h.Contains("ชื่อ"))) nameCol = c;

                            if (pdlineCol < 0 && !isPointsOrFeedback && (
                                h.Contains("ไลน์ผลิต") || h.Contains("pdline") || h.Contains("pd line") || h.Contains("production line")))
                            {
                                pdlineCol = c;
                            }

                            if (scoreCol < 0 && (h.Contains("score") || h.Contains("คะแนน") || h.Contains("total points"))) scoreCol = c;

                            // รองรับคอลัมน์รหัสพนักงานจาก Excel โดยตรง (ยกเว้นคอลัมน์ Points/Feedback)
                            if (empNoCol < 0 && !isPointsOrFeedback && (
                                h == "emp_no" || h.Contains("empno") || h.Contains("emp no") ||
                                h.Contains("employee id") || h.Contains("employee_code") || h.Contains("employee code") ||
                                h.Contains("รหัสพนักงาน") || h.Contains("personid")))
                            {
                                empNoCol = c;
                            }
                        }
                        // ต้องมีอย่างน้อยหนึ่งคอลัมน์ระบุพนักงาน: ชื่อ หรือ รหัสพนักงาน
                        if (nameCol < 0 && empNoCol < 0) return ErrorResponse("ไม่พบคอลัมน์พนักงาน (Name/รหัสพนักงาน)");

                        for (int r = dim.Start.Row + 1; r <= dim.End.Row; r++)
                        {
                            var nameText = nameCol > 0 ? (ws.Cells[r, nameCol]?.Text ?? string.Empty).Trim() : string.Empty;
                            var scoreCell = scoreCol > 0 ? ws.Cells[r, scoreCol]?.Value : null;
                            string? pdlineName = pdlineCol > 0 ? (ws.Cells[r, pdlineCol]?.Text ?? string.Empty).Trim() : null;
                            string? empNoText = empNoCol > 0 ? (ws.Cells[r, empNoCol]?.Text ?? string.Empty).Trim() : null;
                            if (string.IsNullOrWhiteSpace(nameText) && string.IsNullOrWhiteSpace(empNoText)) continue; // ต้องมีอย่างน้อยชื่อหรือรหัสพนักงาน

                            int? scoreVal = null;
                            if (scoreCell != null)
                            {
                                if (scoreCell is double d2) scoreVal = (int)Math.Round(d2);
                                else if (scoreCell is decimal m2) scoreVal = (int)Math.Round((double)m2);
                                else if (int.TryParse((scoreCell?.ToString() ?? string.Empty).Trim(), out var parsed)) scoreVal = parsed;
                            }
                            excelRows.Add((nameText, string.IsNullOrWhiteSpace(pdlineName) ? null : pdlineName, scoreVal, string.IsNullOrWhiteSpace(empNoText) ? null : empNoText));
                        }
                    }
                }

                // เตรียมแม็ป Name -> emp_no จาก emp_person
                var nameSet = new HashSet<string>(excelRows.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);
                var empPersons = await _context.emp_person
                    .Where(e => e.name != null && nameSet.Contains(e.name!.Trim()))
                    .Select(e => new { e.personID, e.name })
                    .ToListAsync();
                var nameToEmpNo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in empPersons)
                {
                    var key = (p.name ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(key)) continue;
                    if (!nameToEmpNo.ContainsKey(key)) nameToEmpNo[key] = (p.personID ?? string.Empty).Trim();
                }

                // Fallback: สำหรับชื่อที่ยังไม่แม็ป ลองค้นหาแบบ Contains ทีละชื่อ หากได้ผลลัพธ์เดียวจึงยอมรับ
                var unresolvedNames = excelRows.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase)
                    .Where(n => !string.IsNullOrWhiteSpace(n) && !nameToEmpNo.ContainsKey(n.Trim()))
                    .Select(n => n.Trim())
                    .ToList();
                foreach (var nm in unresolvedNames)
                {
                    try
                    {
                        var candidates = await _context.emp_person
                            .Where(e => e.name != null && e.name.Contains(nm))
                            .Select(e => new { e.personID, e.name })
                            .Take(2)
                            .ToListAsync();
                        if (candidates.Count == 1)
                        {
                            var c = candidates[0];
                            var key = (c.name ?? string.Empty).Trim();
                            var val = (c.personID ?? string.Empty).Trim();
                            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(val))
                            {
                                if (!nameToEmpNo.ContainsKey(key)) nameToEmpNo[key] = val;
                                // ผูกชื่อเดิม nm ด้วย เพื่อรองรับเคสที่ Excel ใช้ชื่ออีกฟอร์แมต
                                if (!nameToEmpNo.ContainsKey(nm)) nameToEmpNo[nm] = val;
                            }
                        }
                    }
                    catch { }
                }

                // เตรียมแม็ป PDLINE_NAME -> PDLINE_ID (หากมี)
                var pdlineDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    using (var conn = new SqlConnection(_connectionString))
                    {
                        var rows = await conn.QueryAsync("SELECT PDLINE_ID, PDLINE_NAME FROM sys_pdline");
                        foreach (var row in rows)
                        {
                            var nm = (row.PDLINE_NAME?.ToString() ?? string.Empty).Trim();
                            var idStr = (row.PDLINE_ID?.ToString() ?? string.Empty).Trim();
                            if (!string.IsNullOrWhiteSpace(nm) && !pdlineDict.ContainsKey(nm)) pdlineDict[nm] = idStr;
                        }
                    }
                }
                catch
                {
                    // ถ้าไม่สามารถดึงตาราง sys_pdline ได้ จะไม่แม็ป และจะคงค่าไลน์ผลิตตามที่อ่านได้
                }

                // Update DB
                var userName = GetCurrentUserName() ?? "system";
                var now = DateTime.Now;
                var coursePeriod = course?.expire_period;
                var expireAt = ComputeExpireAtFromPeriod(coursePeriod, now);

                // ดึงค่า pdline_id เริ่มต้นจาก meta ของชุดสอบ (ถ้ามี)
                int? defaultPdlineId = null;
                try
                {
                    var meta = ReadExamMeta(id);
                    if (meta != null && meta.TryGetValue("pdline_id", out var pdm) && pdm != null)
                    {
                        if (pdm is int ipd && ipd > 0) defaultPdlineId = ipd;
                        else if (int.TryParse(pdm.ToString(), out var ipd2) && ipd2 > 0) defaultPdlineId = ipd2;
                    }
                }
                catch { }

                int updated = 0, inserted = 0, skipped = 0;
                var processedEmpNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var collectedPdlineIds = new HashSet<int>();
                // อัปเดตข้อมูลเดิม และเพิ่มข้อมูลใหม่หากพบมากกว่าที่ลงทะเบียนไว้
                foreach (var item in excelRows)
                {
                    // ใช้เฉพาะ emp_no ที่มาจาก Excel เท่านั้น หากไม่มี ให้ข้าม
                    var empNo = string.IsNullOrWhiteSpace(item.EmpNo) ? null : item.EmpNo.Trim();
                    if (string.IsNullOrWhiteSpace(empNo))
                    {
                        skipped++;
                        continue; // ข้ามกรณีไม่มี emp_no ใน Excel
                    }
                    if (processedEmpNos.Contains(empNo))
                    {
                        // กันข้อมูลซ้ำภายในไฟล์เดียวกัน
                        continue;
                    }

                    // บันทึกค่าไลน์ผลิตตามที่อยู่ใน Excel ตรง ๆ; ถ้าไม่มีให้ใช้ค่า default จาก meta (ถ้ามี)
                    string? pdlineValue = null;
                    if (!string.IsNullOrWhiteSpace(item.PdlineName))
                    {
                        pdlineValue = item.PdlineName;
                    }
                    if (pdlineValue == null && defaultPdlineId.HasValue && defaultPdlineId.Value > 0)
                    {
                        pdlineValue = defaultPdlineId.Value.ToString();
                    }

                    // คำนวณผลผ่าน/ไม่ผ่านตาม pass_score ของคอร์ส (snapshot ลงตารางผลสอบ)
                    int? passScoreSnapshot = course?.pass_score;
                    bool? isPassedVal = (item.Score.HasValue && passScoreSnapshot.HasValue)
                        ? item.Score.Value >= passScoreSnapshot.Value
                        : (bool?)null;

                    var existing = existingExamEmployees.FirstOrDefault(x => x.emp_no != null && x.emp_no.Trim().Equals(empNo, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        if (item.Score.HasValue) existing.score = item.Score;
                        if (pdlineValue != null) existing.pd_line = pdlineValue;
                        existing.expire_period_snapshot = coursePeriod;
                        existing.expire_at = expireAt;
                        // snapshot เกณฑ์ผ่านและสถานะผ่าน/ไม่ผ่าน ณ วันที่อัปโหลดไฟล์
                        existing.pass_score = passScoreSnapshot;
                        existing.is_passed = isPassedVal;
                        existing.updated_at = now;
                        existing.updated_by = userName;
                        if (exam.course_id.HasValue) existing.course_id = exam.course_id.Value;
                        updated++;
                    }
                    else
                    {
                        _context.esmms_exam_employee.Add(new esmms_exam_employee
                        {
                            emp_no = empNo,
                            exam_id = id,
                            course_id = exam.course_id,
                            pd_line = pdlineValue,
                            is_active = true,
                            assigned_at = now,
                            created_at = now,
                            updated_at = now,
                            created_by = userName,
                            updated_by = userName,
                            score = item.Score,
                            // snapshot เกณฑ์ผ่านและสถานะผ่าน/ไม่ผ่าน ณ วันที่อัปโหลดไฟล์
                            pass_score = passScoreSnapshot,
                            is_passed = isPassedVal,
                            expire_period_snapshot = coursePeriod,
                            expire_at = expireAt
                        });
                        inserted++;
                    }
 
                    processedEmpNos.Add(empNo);
                }

                // ลบพนักงานที่เคยอยู่ในชุดสอบนี้แต่ไม่มีในไฟล์ Excel ที่อัปโหลดครั้งนี้
                try
                {
                    var toRemove = existingExamEmployees
                        .Where(x => x.emp_no != null && !processedEmpNos.Contains(x.emp_no))
                        .ToList();
                    if (toRemove.Count > 0)
                    {
                        _context.esmms_exam_employee.RemoveRange(toRemove);
                    }
                }
                catch { }

                await _context.SaveChangesAsync();

                // เติมเมตาชุดสอบ selected_employees และ pdline_id จากไฟล์ที่อัปโหลด หากยังไม่ได้ตั้งมาก่อน
                try
                {
                    var employeesCsv = string.Join(",", processedEmpNos.OrderBy(x => x));
                    int pdlineIdMeta = 0;
                    if (collectedPdlineIds.Count == 1)
                    {
                        pdlineIdMeta = collectedPdlineIds.First();
                    }
                    UpdateExamMeta(id, new Dictionary<string, object>
                    {
                        { "selected_employees", employeesCsv },
                        { "pdline_id", pdlineIdMeta }
                    });
                }
                catch { }

                // Mark exam completed
                SetExamCompletedMeta(id, true);

                // Save uploaded excel file and persist excel_path
                try
                {
                    var vpath = await SaveExamExcelAsync(id, file);
                    var examEntity = await _context.esmms_exam.FirstOrDefaultAsync(e => e.id == id);
                    if (examEntity != null)
                    {
                        examEntity.excel_path = vpath;
                        await _context.SaveChangesAsync();
                    }
                }
                catch { }

                var summaryMsg = $"อัปโหลดสำเร็จ อัปเดต {updated} รายการ, เพิ่มใหม่ {inserted} รายการ, ข้าม {skipped} รายการ";
                try { await SendCourseSummaryEmail(id); } catch { }
                return SuccessResponse(message: summaryMsg);
            }
            catch (Exception ex)
            {
                return ErrorResponse("อัปโหลดไม่สำเร็จ: " + ex.Message);
            }
        }

        // ส่งสรุปผลคอร์สไปยังผู้รับอีเมลที่กำหนดไว้ (SMTP 10.1.15.143:25) 
        private async Task<bool> SendCourseSummaryEmail(int examId)
        {
            try {
            var exam = await _context.esmms_exam.FirstOrDefaultAsync(e => e.id == examId);
            if (exam == null) return false;
            esmms_course? course = null;
            if (exam.course_id.HasValue) course = await _context.esmms_course.FirstOrDefaultAsync(c => c.id == exam.course_id.Value);

            var recipients = await _context.esmms_employees_receive_mail
                .Where(r => r.is_active) 
                .Select(r => r.email)
                .Distinct()
                .ToListAsync();
            if (recipients.Count == 0) return false;

            var now = DateTime.Now;
            
            // ดึงข้อมูลพนักงานทั้งหมดจาก EmployeeController แบบ offline
            var employeeData = await GetEmployeeDataForEmail();
            var employeeLookup = employeeData
                .Where(x => !string.IsNullOrWhiteSpace(x.emp_no?.ToString()))
                .GroupBy<dynamic, string>(x => x.emp_no?.ToString()?.Trim() ?? "", StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => new { 
                        name = g.First().name?.ToString()?.Trim() ?? "", 
                        deptName = g.First().deptName?.ToString()?.Trim() ?? "",
                        deptID = g.First().deptID?.ToString()?.Trim() ?? ""
                    },
                    StringComparer.OrdinalIgnoreCase
                );
            
            // ใช้ raw SQL query เพื่อหลีกเลี่ยงปัญหา Boolean casting
            var empList = new List<dynamic>();
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var sql = @"
                        SELECT 
                            ee.emp_no,
                            ee.pd_line,
                            ee.score,
                            ee.pass_score,
                            ee.assigned_at,
                            ee.expire_at,
                            c.pass_score as course_pass_score,
                            c.random_question_count as course_total_score
                        FROM esmms_exam_employee ee
                        LEFT JOIN esmms_course c ON ee.course_id = c.id
                        WHERE ee.exam_id = @examId";
                    var rawData = await conn.QueryAsync(sql, new { examId });
                    
                    // เพิ่มข้อมูลชื่อและแผนกจาก offline lookup
                    foreach (var row in rawData)
                    {
                        var empNo = row.emp_no?.ToString()?.Trim() ?? "";
                        var empInfo = employeeLookup.ContainsKey(empNo) ? employeeLookup[empNo] : null;
                        
                        empList.Add(new
                        {
                            emp_no = row.emp_no,
                            name = empInfo?.name ?? "ไม่พบบุคคลที่มีเลขพนักงานนี้",
                            deptName = empInfo?.deptName ?? "",
                            deptID = empInfo?.deptID ?? "",
                            pd_line = row.pd_line,
                            score = row.score,
                            pass_score = row.pass_score,
                            assigned_at = row.assigned_at,
                            expire_at = row.expire_at,
                            course_pass_score = row.course_pass_score,
                            course_total_score = row.course_total_score
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback to Entity Framework if raw SQL fails
                try
                {
                    var rawData = await (
                        from ee in _context.esmms_exam_employee
                        join c in _context.esmms_course on ee.course_id equals c.id into gc
                        from c in gc.DefaultIfEmpty()
                        where ee.exam_id == examId
                        select new
                        {
                            emp_no = ee.emp_no,
                            pd_line = ee.pd_line,
                            score = ee.score,
                            pass_score = ee.pass_score,
                            assigned_at = ee.assigned_at,
                            expire_at = ee.expire_at,
                            course_pass_score = c != null ? c.pass_score : (int?)null,
                            course_total_score = c != null ? c.random_question_count : (int?)null
                        }
                    ).ToListAsync();
                    
                    // เพิ่มข้อมูลชื่อและแผนกจาก offline lookup
                    foreach (var row in rawData)
                    {
                        var empNo = row.emp_no?.Trim() ?? "";
                        var empInfo = employeeLookup.ContainsKey(empNo) ? employeeLookup[empNo] : null;
                        
                        empList.Add(new
                        {
                            emp_no = row.emp_no,
                            name = empInfo?.name ?? "ไม่พบบุคคลที่มีเลขพนักงานนี้",
                            deptName = empInfo?.deptName ?? "",
                            deptID = empInfo?.deptID ?? "",
                            pd_line = row.pd_line,
                            score = row.score,
                            pass_score = row.pass_score,
                            assigned_at = row.assigned_at,
                            expire_at = row.expire_at,
                            course_pass_score = row.course_pass_score,
                            course_total_score = row.course_total_score
                        });
                    }
                }
                catch
                {
                    empList = new List<dynamic>();
                }
            }

            // Helper functions for data processing
            string Safe(object? v) => (v?.ToString() ?? string.Empty).Replace("<", "&lt;").Replace(">", "&gt;");
            string FormatDate(DateTime? d, string fmt = "dd/MM/yyyy HH:mm") => d.HasValue ? d.Value.ToString(fmt) : "-";
            string FormatDateShort(DateTime? d) => d.HasValue ? d.Value.ToString("dd/MM/yyyy") : "-";
            int? DaysLeft(DateTime? d) => d.HasValue ? (int)Math.Floor((d.Value - now).TotalDays) : (int?)null;
            
            // Helper function to get employee name and department
            string GetEmployeeName(dynamic emp) 
            {
                var name = Safe(emp.name);
                if (string.IsNullOrWhiteSpace(name) || name == "ไม่พบบุคคลที่มีเลขพนักงานนี้")
                {
                    return "<span style='color:#dc2626;font-weight:600'>⚠️ ไม่พบบุคคลที่มีเลขพนักงานนี้</span>";
                }
                return name;
            }
            
            string GetEmployeeDept(dynamic emp) 
            {
                var dept = Safe(emp.deptName ?? emp.deptID);
                if (string.IsNullOrWhiteSpace(dept) || dept == "ไม่ระบุแผนก")
                {
                    return "<span style='color:#dc2626;font-weight:600'>⚠️ ไม่ระบุแผนก</span>";
                }
                return dept;
            }
            
            // Helper function to check if employee data is missing
            bool IsEmployeeDataMissing(dynamic emp)
            {
                var name = Safe(emp.name);
                return string.IsNullOrWhiteSpace(name) || name == "ไม่พบบุคคลที่มีเลขพนักงานนี้";
            }
            
            // Helper function to get score display
            string GetScoreDisplay(dynamic emp)
            {
                var score = emp.score;
                var totalScore = emp.course_total_score ?? emp.pass_score;
                if (score == null) return "-";
                if (totalScore == null) return score.ToString();
                return $"{score}/{totalScore}";
            }
            
            // Helper function to get pass score
            string GetPassScore(dynamic emp)
            {
                var passScore = emp.pass_score ?? emp.course_pass_score;
                return passScore?.ToString() ?? "-";
            }
            
            // Helper function to check if passed
            bool? IsPassed(dynamic emp)
            {
                var score = emp.score;
                var passScore = emp.pass_score ?? emp.course_pass_score;
                if (score == null || passScore == null) return null;
                return score >= passScore;
            }

            var taken = empList.Where(x => x.score != null).ToList();
            var notTaken = empList.Where(x => x.score == null).ToList();
            var expiring = empList.Where(x => x.expire_at != null && (x.expire_at - now).TotalDays <= 30 && (x.expire_at - now).TotalDays > 0).ToList();
            var notTakenByDept = notTaken
                .GroupBy(x => GetEmployeeDept(x))
                .Select(g => new { dept = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToList();

            var sb = new StringBuilder();
            sb.Append("<div style='font-family:Segoe UI,Arial,sans-serif;font-size:14px'>");
            sb.Append($"<h3 style='margin:0 0 8px'>สรุปผลคอร์ส/ชุดสอบ</h3>");
            sb.Append("<div style='margin:6px 0 12px'>");
            sb.Append($"<div><b>Exam:</b> {Safe(exam.title)}{(string.IsNullOrWhiteSpace(exam.description)?"":$" - {Safe(exam.description)}")}</div>");
            if (course != null) sb.Append($"<div><b>Course:</b> {Safe(course.course_code)}{(string.IsNullOrWhiteSpace(course.description_en)?"":$" - {Safe(course.description_en)}")}</div>");
            sb.Append($"<div><b>ช่วงสอบ:</b> {FormatDate(exam.start_at)} - {FormatDate(exam.end_at)}</div>");
            sb.Append($"<div><b>สรุปจำนวน:</b> ผู้มีรายการ {empList.Count} คน, สอบแล้ว {taken.Count} คน, ยังไม่สอบ {notTaken.Count} คน</div>");
            sb.Append("</div>");

            sb.Append("<h4 style='margin:12px 0 6px'>ผู้ที่สอบแล้ว</h4>");
            if (taken.Count == 0) sb.Append("<div>- ไม่พบข้อมูล</div>");
            else {
                sb.Append("<table border='1' cellspacing='0' cellpadding='4' style='border-collapse:collapse;width:100%'>");
                sb.Append("<thead><tr style='background:#f2f2f2'><th>Emp No</th><th>ชื่อ</th><th>แผนก</th><th>ไลน์ผลิต</th><th>คะแนน</th><th>คะแนนผ่าน</th><th>ผ่าน</th><th>กำหนด</th><th>หมดอายุ</th><th>เหลือวัน</th></tr></thead><tbody>");
                foreach (var x in taken.OrderByDescending(x => x.assigned_at))
                {
                    var days = DaysLeft(x.expire_at);
                    var daysTxt = days.HasValue ? days.Value.ToString() : "-";
                    if (days.HasValue && days.Value <= 30 && days.Value >= 0) daysTxt = $"<span style='color:#d97706;font-weight:600'>{days.Value}</span>";
                    
                    var isPassed = IsPassed(x);
                    var passTxt = isPassed == true ? "<span style='color:#16a34a;font-weight:600'>ผ่าน</span>" : (isPassed == false ? "<span style='color:#dc2626;font-weight:600'>ไม่ผ่าน</span>" : "-");
                    
                    var scoreTxt = GetScoreDisplay(x);
                    var passScoreTxt = GetPassScore(x);
                    var empName = GetEmployeeName(x);
                    var empDept = GetEmployeeDept(x);
                    
                    // ตรวจสอบว่าข้อมูลพนักงานหายไปหรือไม่
                    var isDataMissing = IsEmployeeDataMissing(x);
                    var rowStyle = isDataMissing ? "style='background-color:#fef2f2;border-left:4px solid #dc2626;'" : "";
                    var empNoStyle = isDataMissing ? "style='color:#dc2626;font-weight:600;'" : "";
                    
                    sb.Append($"<tr {rowStyle}><td {empNoStyle}>{Safe(x.emp_no)}</td><td>{empName}</td><td>{empDept}</td><td>{Safe(x.pd_line)}</td><td style='text-align:center'>{scoreTxt}</td><td style='text-align:center'>{passScoreTxt}</td><td style='text-align:center'>{passTxt}</td><td>{FormatDateShort(x.assigned_at)}</td><td>{FormatDateShort(x.expire_at)}</td><td style='text-align:center'>{daysTxt}</td></tr>");
                }
                sb.Append("</tbody></table>");
            }

            sb.Append("<h4 style='margin:16px 0 6px'>ผู้ที่ยังไม่สอบ</h4>");
            if (notTaken.Count == 0) sb.Append("<div>- ไม่พบข้อมูล</div>");
            else {
                sb.Append("<div style='margin:6px 0'><b>สรุปตามแผนก</b></div>");
                sb.Append("<table border='1' cellspacing='0' cellpadding='4' style='border-collapse:collapse;width:auto;margin-bottom:8px'>");
                sb.Append("<thead><tr style='background:#f2f2f2'><th>แผนก</th><th style='text-align:right'>ยังไม่สอบ</th></tr></thead><tbody>");
                foreach (var d in notTakenByDept) sb.Append($"<tr><td>{Safe(d.dept)}</td><td style='text-align:right'>{d.count}</td></tr>");
                sb.Append("</tbody></table>");

                sb.Append("<table border='1' cellspacing='0' cellpadding='4' style='border-collapse:collapse;width:100%'>");
                sb.Append("<thead><tr style='background:#f2f2f2'><th>Emp No</th><th>ชื่อ</th><th>แผนก</th><th>ไลน์ผลิต</th><th>กำหนด</th><th>หมดอายุ</th><th>เหลือวัน</th></tr></thead><tbody>");
                foreach (var x in notTaken.OrderBy(x => GetEmployeeDept(x)).ThenBy(x => x.emp_no))
                {
                    var days = DaysLeft(x.expire_at);
                    var daysTxt = days.HasValue ? days.Value.ToString() : "-";
                    if (days.HasValue && days.Value <= 30 && days.Value >= 0) daysTxt = $"<span style='color:#d97706;font-weight:600'>{days.Value}</span>";
                    
                    var empName = GetEmployeeName(x);
                    var empDept = GetEmployeeDept(x);
                    
                    // ตรวจสอบว่าข้อมูลพนักงานหายไปหรือไม่
                    var isDataMissing = IsEmployeeDataMissing(x);
                    var rowStyle = isDataMissing ? "style='background-color:#fef2f2;border-left:4px solid #dc2626;'" : "";
                    var empNoStyle = isDataMissing ? "style='color:#dc2626;font-weight:600;'" : "";
                    
                    sb.Append($"<tr {rowStyle}><td {empNoStyle}>{Safe(x.emp_no)}</td><td>{empName}</td><td>{empDept}</td><td>{Safe(x.pd_line)}</td><td>{FormatDateShort(x.assigned_at)}</td><td>{FormatDateShort(x.expire_at)}</td><td style='text-align:center'>{daysTxt}</td></tr>");
                }
                sb.Append("</tbody></table>");
            }

            sb.Append("<h4 style='margin:16px 0 6px'>ใกล้หมดอายุ (ภายใน 30 วัน)</h4>");
            if (expiring.Count == 0) sb.Append("<div>- ไม่พบข้อมูล</div>");
            else {
                sb.Append("<table border='1' cellspacing='0' cellpadding='4' style='border-collapse:collapse;width:100%'>");
                sb.Append("<thead><tr style='background:#f2f2f2'><th>Emp No</th><th>ชื่อ</th><th>แผนก</th><th>ไลน์ผลิต</th><th>คะแนน</th><th>ผ่าน</th><th>หมดอายุ</th><th>เหลือวัน</th></tr></thead><tbody>");
                foreach (var x in expiring.OrderBy(x => x.expire_at))
                {
                    var days = DaysLeft(x.expire_at) ?? 0;
                    var daysTxt = $"<span style='color:#d97706;font-weight:600'>{days}</span>";
                    
                    var isPassed = IsPassed(x);
                    var passTxt = isPassed == true ? "<span style='color:#16a34a;font-weight:600'>ผ่าน</span>" : (isPassed == false ? "<span style='color:#dc2626;font-weight:600'>ไม่ผ่าน</span>" : "-");
                    
                    var scoreTxt = GetScoreDisplay(x);
                    var empName = GetEmployeeName(x);
                    var empDept = GetEmployeeDept(x);
                    
                    // ตรวจสอบว่าข้อมูลพนักงานหายไปหรือไม่
                    var isDataMissing = IsEmployeeDataMissing(x);
                    var rowStyle = isDataMissing ? "style='background-color:#fef2f2;border-left:4px solid #dc2626;'" : "";
                    var empNoStyle = isDataMissing ? "style='color:#dc2626;font-weight:600;'" : "";
                    
                    sb.Append($"<tr {rowStyle}><td {empNoStyle}>{Safe(x.emp_no)}</td><td>{empName}</td><td>{empDept}</td><td>{Safe(x.pd_line)}</td><td style='text-align:center'>{scoreTxt}</td><td style='text-align:center'>{passTxt}</td><td>{FormatDateShort(x.expire_at)}</td><td style='text-align:center'><b>{daysTxt}</b></td></tr>");
                }
                sb.Append("</tbody></table>");
            }

            sb.Append("<div style='margin-top:15px;padding:10px;background-color:#f9fafb;border-left:4px solid #3b82f6;font-size:12px;'>");
            sb.Append("<strong>หมายเหตุ:</strong><br/>");
            sb.Append("• <span style='color:#dc2626;font-weight:600'>⚠️</span> = ข้อมูลพนักงานไม่พบในระบบ<br/>");
            sb.Append("• แถวที่มีพื้นหลังสีชมพูอ่อนและขอบซ้ายสีแดง = มีปัญหาข้อมูลพนักงาน<br/>");
            sb.Append("• รหัสพนักงานสีแดง = ไม่พบข้อมูลในระบบ");
            sb.Append("</div>");
            
            sb.Append("<div style='margin-top:10px;color:#6b7280'>อีเมลนี้ถูกส่งอัตโนมัติจากระบบ E-SMMS</div>");
            sb.Append("</div>");

            using var message = new MailMessage();
            message.From = new MailAddress("noreply@ipsth.local", "E-SMMS");
            foreach (var em in recipients) { try { message.To.Add(new MailAddress(em)); } catch { } }
            
            // แก้ไข encoding สำหรับ subject ภาษาไทย
            var subject = $"[E-SMMS] สรุปผลชุดสอบ {exam.title}" + (course != null ? $" | {course.course_code}" : string.Empty);
            message.Subject = subject;
            message.SubjectEncoding = System.Text.Encoding.UTF8;
            
            message.Body = sb.ToString();
            message.BodyEncoding = System.Text.Encoding.UTF8;
            message.IsBodyHtml = true;

            using var client = new SmtpClient("10.1.15.143", 25)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = false,
                UseDefaultCredentials = true,
            };
            await client.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}

