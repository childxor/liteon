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

namespace IPS_TH.Controllers.ESMMS
{
    public class ExamController : BaseController
    {
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
            [JsonPropertyName("correct_choice")] public string? correct_choice { get; set; }
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
                    correct_choice = q.correct_choice
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
                    correct_choice = CleanTextForForms(q.correct_choice)
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

                await LoadPermissions("Exam", "Exam");

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
            return Json(new { data = withImage });
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
                            _context.esmms_exam_detail.Add(new esmms_exam_detail
                            {
                                exam_id = input.id,
                                course_id = courseId,
                                question_id = qid,
                                sort_order = order++,
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
                        _context.esmms_exam_detail.Add(new esmms_exam_detail
                        {
                            exam_id = id,
                            course_id = exam.course_id.Value,
                            question_id = qid,
                            sort_order = order++,
                            created_at = DateTime.Now,
                            updated_at = DateTime.Now,
                            created_by = GetCurrentUserName(),
                            updated_by = GetCurrentUserName()
                        });
                    }
                    if (qIds.Count > 0)
                    {
                        await _context.SaveChangesAsync();
                    }
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
                    correct_choice = q.correct_choice,
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
                        expire_at = ee.expire_at
                    }
                ).ToListAsync();

                // เลือกเรคอร์ดที่เหมาะสมที่สุดต่อ emp: future ใกล้สุด ถ้าไม่มีให้ใช้ล่าสุด
                var now = DateTime.Now;
                var result = new List<object>();
                foreach (var group in list.GroupBy(x => x.emp_no, StringComparer.OrdinalIgnoreCase))
                {
                    var future = group
                        .Where(x => x.expire_at.HasValue && x.expire_at.Value >= now)
                        .OrderBy(x => x.expire_at)
                        .FirstOrDefault();
                    var chosen = future ?? group
                        .Where(x => x.expire_at.HasValue)
                        .OrderByDescending(x => x.expire_at)
                        .FirstOrDefault() ?? group.FirstOrDefault();

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
                        status,
                        days_left = daysLeft
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
                _context.esmms_exam_detail.Add(new esmms_exam_detail
                {
                    exam_id = id,
                    course_id = exam.course_id.Value,
                    question_id = qid,
                    sort_order = order++,
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
                    correct_choice = q.correct_choice,
                    image_path = q.image_path
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
                    correct_choice = q.correct_choice,
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
                    correct_choice = q.correct_choice
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
        public IActionResult SetExamCompleted(int id, bool is_completed)
        {
            var exists = _context.esmms_exam.Any(e => e.id == id);
            if (!exists) return ErrorResponse("ไม่พบชุดสอบ");
            SetExamCompletedMeta(id, is_completed);
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
                    meta[kv.Key] = kv.Value ?? string.Empty;
                }
            }
            var path = GetExamMetaPath(id);
            System.IO.File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(meta));
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

                // Target employees from esmms_exam_employee
                var targetEmployees = await _context.esmms_exam_employee
                    .Where(x => x.exam_id == id)
                    .Select(x => x.emp_no)
                    .ToListAsync();
                var targetSet = new HashSet<string>(targetEmployees.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);
                if (targetSet.Count == 0)
                    return ErrorResponse("ยังไม่มีผู้เข้าสอบสำหรับชุดนี้");

                // Parse Excel using EPPlus (EPPlus v8+: set license info explicitly)
                ExcelPackage.License.SetNonCommercialPersonal("IPS-TH");
                var empScore = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
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
                        int idCol = -1, scoreCol = -1;
                        for (int c = dim.Start.Column; c <= dim.End.Column; c++)
                        {
                            var h = (ws.Cells[dim.Start.Row, c].Text ?? string.Empty).Trim().ToLowerInvariant();
                            if (idCol < 0 && (h.Contains("emp") && h.Contains("id") || h.Contains("emp_no") || h.Contains("personid") || h.Contains("รหัสพนักงาน") || h == "personid" || h == "person_id")) idCol = c;
                            if (scoreCol < 0 && (h.Contains("score") || h.Contains("คะแนน") || h.Contains("total points"))) scoreCol = c;
                        }
                        if (idCol < 0) return ErrorResponse("ไม่พบคอลัมน์รหัสพนักงานในไฟล์ (เช่น EmpNo/PersonID/รหัสพนักงาน)");
                        if (scoreCol < 0) return ErrorResponse("ไม่พบคอลัมน์คะแนน (score/คะแนน/Total points)");

                        for (int r = dim.Start.Row + 1; r <= dim.End.Row; r++)
                        {
                            var idCell = ws.Cells[r, idCol]?.Value;
                            var scoreCell = ws.Cells[r, scoreCol]?.Value;
                            if (idCell == null) continue;
                            string empNo;
                            switch (idCell)
                            {
                                case double d: empNo = ((long)d).ToString(); break; // excel numeric
                                case decimal m: empNo = ((long)m).ToString(); break;
                                default: empNo = idCell.ToString()?.Trim() ?? string.Empty; break;
                            }
                            if (string.IsNullOrWhiteSpace(empNo)) continue;

                            int scoreVal = 0;
                            if (scoreCell is double d2) scoreVal = (int)Math.Round(d2);
                            else if (scoreCell is decimal m2) scoreVal = (int)Math.Round((double)m2);
                            else int.TryParse((scoreCell?.ToString() ?? "").Trim(), out scoreVal);

                            empScore[empNo] = scoreVal;
                        }
                    }
                }

                // Validate coverage
                var missing = targetSet.Where(emp => !empScore.ContainsKey(emp)).Take(50).ToList();
                if (missing.Count > 0)
                {
                    var msg = "ข้อมูลไม่ครบในไฟล์ (ขาด: " + string.Join(", ", missing) + ")";
                    return ErrorResponse(msg);
                }

                // Update DB
                var userName = GetCurrentUserName() ?? "system";
                var now = DateTime.Now;
                var coursePeriod = course?.expire_period;
                var expireAt = ComputeExpireAtFromPeriod(coursePeriod, now);

                var rows = await _context.esmms_exam_employee.Where(x => x.exam_id == id).ToListAsync();
                foreach (var row in rows)
                {
                    if (!string.IsNullOrWhiteSpace(row.emp_no) && empScore.TryGetValue(row.emp_no.Trim(), out var sc))
                    {
                        row.score = sc;
                        row.expire_period_snapshot = coursePeriod;
                        row.expire_at = expireAt;
                        row.updated_at = now;
                        row.updated_by = userName;
                        // Ensure course_id synced
                        if (exam.course_id.HasValue) row.course_id = exam.course_id.Value;
                    }
                }

                await _context.SaveChangesAsync();

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

                return SuccessResponse(message: "อัปโหลดและบันทึกผลสำเร็จ");
            }
            catch (Exception ex)
            {
                return ErrorResponse("อัปโหลดไม่สำเร็จ: " + ex.Message);
            }
        }
    }
}
