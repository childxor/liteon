using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using IPS_TH.Data;
using IPS_TH.Extensions; // เพิ่ม namespace สำหรับ Extension Methods
using IPS_TH.Models.Employee;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace IPS_TH.Controllers.DoorPermission
{
    public class DoorPermissionController : BaseController
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

        private readonly ILogger<DoorPermissionController> _logger;

        public DoorPermissionController(
            IConfiguration configuration,
            ApplicationDbContext context,
            ILogger<DoorPermissionController> logger
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

        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("DoorPermission Index: Starting to load data");
                
                // ดึงข้อมูลแผนก
                using (var defaultconnection = new SqlConnection(_connectionString))
                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    try
                    {
                        // ดึงข้อมูลกะจากฐานข้อมูลหลัก
                        var shiftDeptQuery =
                            @"SELECT * FROM emp_shift WHERE record_status = 'N' ORDER BY shift_group";
                        var shiftDept = await defaultconnection.QueryAsync<dynamic>(shiftDeptQuery);
                        ViewBag.Shiftdept = shiftDept;
                        _logger.LogInformation($"DoorPermission Index: Loaded {shiftDept?.Count() ?? 0} shifts from main DB");

                        // ดึงข้อมูลแผนกจาก SQL944
                        var deptQuery = @"SELECT * FROM Dept";
                        var dept = await connection.QueryAsync<dynamic>(deptQuery);
                        ViewBag.Dept = dept;
                        _logger.LogInformation($"DoorPermission Index: Loaded {dept?.Count() ?? 0} departments from SQL944");

                        // ดึงข้อมูลประตูจาก SQL944 
                        var doorsSql =
                            @"SELECT DISTINCT doorID, doorName FROM PubDoor ORDER BY doorName";
                        var doors = await connection.QueryAsync<dynamic>(doorsSql);
                        ViewBag.Doors = doors;
                        _logger.LogInformation($"DoorPermission Index: Loaded {doors?.Count() ?? 0} doors from SQL944");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"DoorPermission Index: Database query error - {ex.Message}");
                        // ตั้งค่า default values เพื่อไม่ให้หน้า view crash
                        ViewBag.Shiftdept = new List<dynamic>();
                        ViewBag.Dept = new List<dynamic>();
                        ViewBag.Doors = new List<dynamic>();
                    }
                }

                // console log session ทั้งหมดที่มีในระบบ
                Console.WriteLine(JsonConvert.SerializeObject(HttpContext.Session.Keys));

                try
                {
                    await LoadPermissions("DoorPermission", "Index");
                    _logger.LogInformation("DoorPermission Index: Permissions loaded successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"DoorPermission Index: Permission loading error - {ex.Message}");
                }

                // ส่งข้อมูลแผนกของผู้ใช้ไปยัง View
                ViewData["CurrentUserDepartment"] = CurrentUserDepartment;
                _logger.LogInformation($"DoorPermission Index: Current user department: {CurrentUserDepartment ?? "Not set"}");

                return View("doorPermission");
            }
            catch (Exception ex)
            {
                _logger.LogError($"DoorPermission Index: Critical error - {ex.Message}");
                _logger.LogError($"DoorPermission Index: Stack trace - {ex.StackTrace}");
                
                // ส่งข้อมูล error ไปยัง View เพื่อแสดงข้อความที่เหมาะสม
                ViewBag.ErrorMessage = "เกิดข้อผิดพลาดในการโหลดข้อมูล กรุณาลองใหม่อีกครั้ง";
                ViewBag.ErrorDetails = ex.Message;
                
                // ตั้งค่า default values เพื่อไม่ให้หน้า view crash
                ViewBag.Shiftdept = new List<dynamic>();
                ViewBag.Dept = new List<dynamic>();
                ViewBag.Doors = new List<dynamic>();
                
                return View("doorPermission");
            }
        }

    }
}