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

namespace IPS_TH.Controllers.ESMMS
{
    public class ESMMSController : BaseController
    {
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

        private readonly ILogger<ESMMSController> _logger;

        public ESMMSController(
            IConfiguration configuration,
            ApplicationDbContext context,
            ILogger<ESMMSController> logger
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

                await LoadPermissions("ESMMS", "question_bank");

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
        public async Task<IActionResult> question_bank()
        {
            await LoadPermissions("ESMMS", "question_bank");
            return RedirectToAction(nameof(course));
        }

        

        // ===== Course =====
        [HttpGet]
        public async Task<IActionResult> course()
        {
            await LoadPermissions("ESMMS", "question_bank");

            var items = await _context
                .esmms_course
                .OrderBy(x => x.course_code)
                .ToListAsync();

            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> GetCourses()
        {
            var items = await _context.esmms_course
                .OrderBy(x => x.course_code)
                .Select(x => new
                {
                    x.id,
                    x.course_code,
                    x.description_th,
                    x.description_en,
                    x.random_question_count,
                    x.pass_score,
                    x.dl,
                    x.idl, 
                    x.wi,
                    x.expire_period,
                    x.trainer
                })
                .ToListAsync();

            return Json(new { data = items });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(esmms_course input)
        {
            if (!ModelState.IsValid)
                return ErrorResponse("กรุณากรอกข้อมูลให้ครบถ้วน");

            input.created_at = DateTime.Now;
            input.updated_at = DateTime.Now;
            input.created_by = GetCurrentUserName();
            input.updated_by = GetCurrentUserName();

            _context.esmms_course.Add(input);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(course));
        }
 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(esmms_course input)
        {
            if (!ModelState.IsValid)
                return ErrorResponse("กรุณากรอกข้อมูลให้ครบถ้วน");

            var entity = await _context.esmms_course.FirstOrDefaultAsync(x => x.id == input.id);
            if (entity == null)
                return ErrorResponse("ไม่พบข้อมูลสำหรับแก้ไข");

            entity.course_code = input.course_code;
            entity.description_th = input.description_th;
            entity.description_en = input.description_en;
            entity.random_question_count = input.random_question_count;
            entity.pass_score = input.pass_score;
            entity.dl = input.dl;
            entity.idl = input.idl;
            entity.wi = input.wi;
            entity.expire_period = input.expire_period;
            entity.trainer = input.trainer;
            entity.updated_at = DateTime.Now;
            entity.updated_by = GetCurrentUserName();

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(course));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.esmms_course.FirstOrDefaultAsync(x => x.id == id);
            if (course == null)
                return ErrorResponse("ไม่พบข้อมูลสำหรับลบ");

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // หา exam ทั้งหมดของคอร์สนี้
                var examIds = await _context.esmms_exam
                    .Where(e => e.course_id == id)
                    .Select(e => e.id)
                    .ToListAsync();

                if (examIds.Count > 0)
                {
                    // ลบรายละเอียดข้อสอบของชุดสอบทั้งหมด
                    var examDetails = _context.esmms_exam_detail.Where(d => examIds.Contains(d.exam_id));
                    _context.esmms_exam_detail.RemoveRange(examDetails);

                    // ลบรายการพนักงานในการสอบโดยอ้างอิง exam_id
                    var examEmpsByExam = _context.esmms_exam_employee
                        .Where(x => x.exam_id.HasValue && examIds.Contains(x.exam_id.Value));
                    _context.esmms_exam_employee.RemoveRange(examEmpsByExam);

                    // ลบชุดสอบ
                    var exams = _context.esmms_exam.Where(e => examIds.Contains(e.id));
                    _context.esmms_exam.RemoveRange(exams);
                }

                // ลบรายการพนักงานที่ผูกด้วย course_id โดยตรง
                var examEmpsByCourse = _context.esmms_exam_employee.Where(x => x.course_id == id);
                _context.esmms_exam_employee.RemoveRange(examEmpsByCourse);

                // ลบธนาคารคำถามของคอร์สนี้ (ถ้ามี)
                var questions = _context.esmms_question.Where(q => q.course_id == id);
                _context.esmms_question.RemoveRange(questions);

                // ลบคอร์ส
                _context.esmms_course.Remove(course);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return RedirectToAction(nameof(course));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return ErrorResponse($"ลบไม่สำเร็จ: {ex.Message}");
            }
        }
    }
}
