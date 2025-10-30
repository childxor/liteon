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
    public class Employees_ListController : BaseController
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

        private readonly ILogger<Employees_ListController> _logger;

        public Employees_ListController(
            IConfiguration configuration,
            ApplicationDbContext context,
            ILogger<Employees_ListController> logger
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
        public async Task<IActionResult> Employees_List()
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

                    // โหลดรายการไลน์ผลิตจากตาราง sys_pdline (ถ้ามี)
                    try
                    {
                        var pdlines = await defaultconnection.QueryAsync<dynamic>(
                            @"SELECT PDLINE_ID as id, PDLINE_NAME as name FROM sys_pdline ORDER BY PDLINE_NAME"
                        );
                        ViewBag.Pdlines = pdlines;
                    }
                    catch { ViewBag.Pdlines = new List<dynamic>(); }
                }

                await LoadPermissions("Employees_List", "employees_List");

                // ส่งข้อมูลแผนกของผู้ใช้ไปยัง View
                ViewData["CurrentUserDepartment"] = HttpContext.Session.GetString("WorkArea");

                return View("~/Views/ESMMS/Employees_List.cshtml");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Employees_List action: {ex.Message}");
                return View("~/Views/ESMMS/Employees_List.cshtml");
            }
        }
    }
}
