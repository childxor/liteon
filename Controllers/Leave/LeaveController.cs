using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using IPS_TH.Data;
using IPS_TH.Models.Employee;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient; // ใช้ SqlClient สำหรับการเชื่อมต่อ SQL Server
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace IPS_TH.Controllers.Leave
{
    public class LeaveController : BaseController
    {
        private class LeaveBalanceModel
        {
            public string EmpNo { get; set; }
            public string AttenCode { get; set; }
            public decimal CarryFrLastYear { get; set; }
            public decimal AvailLastYear { get; set; }
            public decimal ThisYearCurrentQty { get; set; }
            public decimal ThisYearTaken { get; set; }
            public decimal TotalEntitled { get; set; }
            public decimal RemainingDays { get; set; }
            public string EmpName { get; set; }
            public int ProgressPercent { get; set; }
            public DateTime ExpiryDate { get; set; }
        }

        private readonly string _connectionString;

        private readonly IConfiguration _configuration;

        private readonly string _sql944ConnectionString;

        private readonly string _ces941ConnectionString;

        public LeaveController(IConfiguration configuration, ApplicationDbContext context)
            : base(context)
        {
            _connectionString = _context.Database.GetDbConnection().ConnectionString;
            _configuration = configuration;
            _ces941ConnectionString = _configuration.GetConnectionString("CES941");
            _sql944ConnectionString = _configuration.GetConnectionString("SQL944");
        }

        public async Task<IActionResult> Summary()
        {
                            await LoadPermissions("Leave", "Summary");
            if (CurrentPermissions["CanApprove"])
            {
                var employeeResult = await GetEmployeeList();
                ViewBag.Employees = (employeeResult as JsonResult)?.Value;
            }
            return View();
        }

        public async Task<JsonResult> GetEmployeeList()
        {
            using (IDbConnection db = new SqlConnection(_sql944ConnectionString))
            {
                string sql =
                    @"
                    SELECT 
                        REPLACE(personID, '-1', '') AS EmpNo,
                        name
                    FROM Person  
                    WHERE (personID LIKE '%-1%') 
                    ORDER BY name";

                try
                {
                    var employees = await db.QueryAsync<dynamic>(sql);
                    return Json(employees);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in GetEmployeeList: {ex.Message}");
                    return Json(new List<dynamic>());
                }
            }
        }

        [HttpGet]
        public JsonResult GetLeaveSummary(string empNo = null, string empName = null)
        {
            using (IDbConnection db = new SqlConnection(_ces941ConnectionString))
            {
                string sql =
                    @"
                    SELECT 
                        TEmpLeave.EmpNo, 
                        @EmpName AS EmpName,
                        TEmpLeave.AttenCode, 
                        TEmpLeave.WrkDate, 
                        TEmpLeave.DteTmeIn,  
                        TEmpLeave.DteTmeOut, 
                        TEmpLeave.LeaveQty, 
                        TEmpLeave.LeaveType, 
                        TEmpLeave.ApprStatus, 
                        TEmpLeave.ApprDate, 
                        TEmpLeave.SecCode, 
                        TEmpLeave.CostCenter, 
                        TEmpLeave.LeaveReqNo, 
                        TEmpLeave.LeaveReqItem, 
                        TEmpLeave.CaseId, 
                        TEmpLeave.CreateDate, 
                        TEmpLeave.CreateBy, 
                        TEmpLeave.NextAppr, 
                        MSecCode.SecDsc, 
                        MSecCode.CostCenter AS Expr1, 
                        MSecCode.BossId AS Expr2, 
                        MEmpBasic.PrefixName, 
                        MEmpBasic.EmpName AS Expr3, 
                        MEmpBasic.EmpLName, 
                        MEmpBasic.Gender, 
                        MSecCode.Language
                    FROM TEmpLeave 
                    INNER JOIN MSecCode ON TEmpLeave.SecCode = MSecCode.SecCode AND MSecCode.Language = 'EN' 
                    INNER JOIN MEmpBasic ON TEmpLeave.EmpNo = MEmpBasic.EmpNo AND MSecCode.Language = 'EN'
                    WHERE TEmpLeave.EmpNo = @EmpNo AND MEmpBasic.Gender IN ('M', 'F') AND TEmpLeave.ApprStatus IN ('Approve', 'On Approve')
                    ORDER BY TEmpLeave.DteTmeIn DESC";

                var currentEmpNo = HttpContext.Session.GetString("EmployeeID");
                var currentEmpName = HttpContext.Session.GetString("EmployeeName");

                // ถ้าไม่มีการเลือกพนักงาน ให้ใช้ข้อมูลตัวเอง
                var targetEmpNo = string.IsNullOrEmpty(empNo) ? currentEmpNo : empNo;
                var targetEmpName = empName ?? currentEmpName;

                // ถ้ามีการเลือกพนักงานและมีสิทธิ์ CanApprove
                if (
                    !string.IsNullOrEmpty(empNo)
                    && CurrentPermissions != null
                    && CurrentPermissions.ContainsKey("CanApprove")
                    && CurrentPermissions["CanApprove"]
                )
                {
                    // ดึงชื่อพนักงานที่เลือก
                    using (IDbConnection empDb = new SqlConnection(_sql944ConnectionString))
                    {
                        var empSql =
                            "SELECT name FROM Person WHERE REPLACE(personID, '-1', '') = @empNo";
                        targetEmpName =
                            empDb.QueryFirstOrDefault<string>(empSql, new { empNo = targetEmpNo })
                            ?? currentEmpName;
                    }
                }

                var leaveSummary = db.Query<dynamic>(
                        sql,
                        new { EmpNo = targetEmpNo, EmpName = targetEmpName }
                    )
                    .ToList();

                return Json(new { data = leaveSummary, empName = targetEmpName });
            }
        }

        [HttpGet]
        public JsonResult GetLeaveBalance(string empNo = null, string empName = null)
        {
            using (IDbConnection db = new SqlConnection(_ces941ConnectionString))
            {
                var currentEmpNo = HttpContext.Session.GetString("EmployeeID");
                var currentEmpName = HttpContext.Session.GetString("EmployeeName");
                var targetEmpNo = string.IsNullOrEmpty(empNo) ? currentEmpNo : empNo;
                var targetEmpName = empName ?? currentEmpName;

                if (
                    !string.IsNullOrEmpty(empNo)
                    && CurrentPermissions?.ContainsKey("CanApprove") == true
                    && CurrentPermissions["CanApprove"]
                )
                {
                    using (IDbConnection empDb = new SqlConnection(_sql944ConnectionString))
                    {
                        targetEmpName =
                            empDb.QueryFirstOrDefault<string>(
                                "SELECT name FROM Person WHERE REPLACE(personID, '-1', '') = @empNo",
                                new { empNo = targetEmpNo }
                            ) ?? currentEmpName;
                    }
                }

                try
                {
                    const string sql =
                        @"
                        WITH LeaveBalance AS (
                            SELECT 
                                EmpNo,
                                AttenCode,
                                ISNULL(LastYearIn, 0) as CarryFrLastYear,
                                ISNULL(AvailLastYear, 0) as AvailLastYear,
                                ISNULL(CurrentQty, 0) as ThisYearCurrentQty,
                                ISNULL(Taken, 0) as ThisYearTaken,
                                (ISNULL(LastYearIn, 0) + ISNULL(CurrentQty, 0)) as TotalEntitled,
                                (ISNULL(LastYearIn, 0) + ISNULL(CurrentQty, 0) - ISNULL(Taken, 0)) as RemainingDays,
                                CASE 
                                    WHEN AttenCode IN ('AL', 'AAL', 'SD', 'SD1') THEN EffDateTo
                                    ELSE NULL
                                END as ExpiryDate,
                                ROW_NUMBER() OVER (PARTITION BY AttenCode ORDER BY EffDateFr DESC) as rn
                            FROM vTEmpLeaveSum
                            WHERE EmpNo LIKE @EmpNo 
                            AND (GETDATE() BETWEEN EffDateFr AND EffDateTo)
                        )
                        SELECT 
                            EmpNo,
                            AttenCode,
                            CarryFrLastYear,
                            AvailLastYear,
                            ThisYearCurrentQty,
                            ThisYearTaken,
                            TotalEntitled,
                            RemainingDays,
                            ExpiryDate
                        FROM LeaveBalance
                        WHERE rn = 1
                        ORDER BY AttenCode";

                    var leaveBalance = db.Query<LeaveBalanceModel>(
                            sql,
                            new { EmpNo = $"%{targetEmpNo}%" }
                        )
                        .ToList();

                    foreach (var item in leaveBalance)
                    {
                        item.EmpName = targetEmpName;
                        item.ProgressPercent =
                            item.TotalEntitled > 0
                                ? (int)
                                    Math.Min((item.RemainingDays / item.TotalEntitled) * 100, 100)
                                : 0;
                    }

                    return Json(
                        new
                        {
                            data = leaveBalance,
                            empName = targetEmpName,
                            summary = new
                            {
                                totalLeaves = leaveBalance.Count,
                                totalEntitled = (int)leaveBalance.Sum(x => x.TotalEntitled),
                                totalTaken = (int)leaveBalance.Sum(x => x.ThisYearTaken),
                                totalRemaining = (int)leaveBalance.Sum(x => x.RemainingDays),
                            },
                        }
                    );
                }
                catch (Exception ex)
                {
                    return Json(
                        new
                        {
                            data = new List<dynamic>(),
                            empName = targetEmpName,
                            error = "เกิดข้อผิดพลาดในการดึงข้อมูล",
                            details = ex.Message,
                        }
                    );
                }
            }
        }

        [HttpGet]
        public JsonResult GetLeaveStats()
        {
            try
            {
                using (IDbConnection db = new SqlConnection(_ces941ConnectionString))
                {
                    // ดึงข้อมูลการลาทั้งหมด
                    string sql =
                        @"
                        SELECT 
                            TEmpLeave.EmpNo,
                            TEmpLeave.AttenCode,
                            TEmpLeave.DteTmeIn,
                            TEmpLeave.DteTmeOut,
                            TEmpLeave.LeaveQty,
                            TEmpLeave.ApprStatus,
                            MEmpBasic.EmpName,
                            MSecCode.SecDsc,
                            TEmpLeave.LeaveType
                        FROM TEmpLeave 
                        INNER JOIN MEmpBasic ON TEmpLeave.EmpNo = MEmpBasic.EmpNo
                        INNER JOIN MSecCode ON TEmpLeave.SecCode = MSecCode.SecCode
                        WHERE TEmpLeave.ApprStatus IN ('Approve', 'On Approve')
                        AND TEmpLeave.DteTmeIn >= DATEADD(MONTH, -12, GETDATE())
                        AND TEmpLeave.NextAppr = 'CLOSED'
                        AND MSecCode.Language = 'EN'
                        AND MEmpBasic.Language = 'EN' 
                        ORDER BY TEmpLeave.DteTmeIn DESC";

                    var leaves = db.Query<dynamic>(sql).ToList();

                    // จัดกลุ่มข้อมูลตามเดือน
                    var leaveByMonth = leaves
                        .GroupBy(l => new DateTime(l.DteTmeIn.Year, l.DteTmeIn.Month, 1))
                        .Select(g => new { month = g.Key.ToString("MMM yyyy"), count = g.Count() })
                        .OrderBy(x => DateTime.ParseExact(x.month, "MMM yyyy", null))
                        .ToList();

                    // จัดกลุ่มข้อมูลตามประเภทการลา
                    var leaveTypeStats = leaves
                        .GroupBy(l => l.AttenCode)
                        .Select(g => new { type = GetLeaveTypeName(g.Key), count = g.Count() })
                        .OrderByDescending(x => x.count)
                        .Take(8)
                        .ToList();

                    // จัดกลุ่มข้อมูลตามสถานะ
                    var leaveStatus = leaves
                        .GroupBy(l => l.ApprStatus)
                        .Select(g => new
                        {
                            status = g.Key == "Approve" ? "อนุมัติแล้ว" : "รออนุมัติ",
                            count = g.Count(),
                        })
                        .ToList();

                    // จัดกลุ่มข้อมูลตามจำนวนวันลาต่อเดือน
                    var leaveDaysByMonth = leaves
                        .GroupBy(l => new DateTime(l.DteTmeIn.Year, l.DteTmeIn.Month, 1))
                        .Select(g => new
                        {
                            month = g.Key.ToString("MMM yyyy"),
                            days = g.Sum(l => (decimal)l.LeaveQty),
                        })
                        .OrderBy(x => DateTime.ParseExact(x.month, "MMM yyyy", null))
                        .ToList();

                    // จัดกลุ่มข้อมูลตามแผนก
                    var leaveByDepartment = leaves
                        .GroupBy(l => l.SecDsc)
                        .Select(g => new { department = g.Key, count = g.Count() })
                        .OrderByDescending(x => x.count)
                        .Take(5)
                        .ToList();

                    // จัดกลุ่มข้อมูลตามช่วงเวลา
                    var leaveTimeSlots = new[]
                    {
                        leaves.Count(l => l.LeaveType == "M"), // เช้า
                        leaves.Count(l => l.LeaveType == "A"), // บ่าย
                        leaves.Count(l => l.LeaveType == "N"), // กะดึก
                        leaves.Count(l =>
                            l.LeaveType == "F"
                        ) // เต็มวัน
                        ,
                    };

                    // เพิ่ม logging เพื่อตรวจสอบข้อมูล
                    Console.WriteLine(
                        $"Leave Time Slots Data: {string.Join(", ", leaveTimeSlots)}"
                    );

                    // จัดกลุ่มข้อมูลตามระยะเวลาการลาเฉลี่ยต่อเดือน
                    var averageLeaveDuration = leaves
                        .GroupBy(l => new DateTime(l.DteTmeIn.Year, l.DteTmeIn.Month, 1))
                        .Select(g => new
                        {
                            month = g.Key.ToString("MMM yyyy"),
                            averageDuration = g.Average(l => (decimal)l.LeaveQty),
                        })
                        .OrderBy(x => DateTime.ParseExact(x.month, "MMM yyyy", null))
                        .ToList();

                    // จัดกลุ่มข้อมูลตามวันทำงาน/วันหยุด
                    var workdayHolidayLeave = leaves
                        .GroupBy(l => new DateTime(l.DteTmeIn.Year, l.DteTmeIn.Month, 1))
                        .Select(g => new
                        {
                            month = g.Key.ToString("MMM yyyy"),
                            workday = g.Count(l => l.LeaveType != "H"),
                            holiday = g.Count(l => l.LeaveType == "H"),
                        })
                        .OrderBy(x => DateTime.ParseExact(x.month, "MMM yyyy", null))
                        .ToList();

                    return Json(
                        new
                        {
                            success = true,
                            leaveByMonth = leaveByMonth,
                            leaveTypes = leaveTypeStats,
                            leaveStatus = leaveStatus,
                            leaveDaysByMonth = leaveDaysByMonth,
                            leaveByDepartment = leaveByDepartment,
                            leaveTimeSlots = leaveTimeSlots,
                            averageLeaveDuration = averageLeaveDuration,
                            workdayHolidayLeave = workdayHolidayLeave,
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                return Json(
                    new
                    {
                        success = false,
                        message = "เกิดข้อผิดพลาดในการดึงข้อมูล",
                        error = ex.Message,
                    }
                );
            }
        }

        private string GetLeaveTypeName(string attenCode)
        {
            var leaveTypeNames = new Dictionary<string, string>
            {
                { "AAL", "ลาพักร้อนล่วงหน้า" },
                { "ACL", "ลาเนื่องจากอุบัติเหตุ" },
                { "AL", "ลาพักร้อนประจำปี" },
                { "ATR", "อบรมภายนอก" },
                { "ATW", "ทำงานนอกสถานที่" },
                { "BL", "ลาเนื่องจากการเสียชีวิตของญาติ" },
                { "CPS", "แลกวันลา" },
                { "ML", "ลารับราชการทหาร" },
                { "MO", "ลาอุปสมบท (มีเงินเดือน)" },
                { "MO1", "ลาอุปสมบท (ไม่มีเงินเดือน)" },
                { "MT", "ลาคลอดบุตร (มีเงินเดือน)" },
                { "MT0", "ลาคลอดบุตร (ไม่มีเงินเดือน)" },
                { "MT1", "ลาคลอดบุตร (ไม่มีเงินเดือน)" },
                { "PL", "ลากิจ" },
                { "PT", "ลาเพื่อการดูแลบุตร" },
                { "SD", "ลาปิดโรงงาน (มีเงินเดือน)" },
                { "SD1", "ลาปิดโรงงาน (ครึ่งวัน)" },
                { "SL", "ลาป่วย (มีเงินเดือน)" },
                { "SL1", "ลาป่วย (ไม่มีเงินเดือน)" },
                { "SP", "พักงาน" },
                { "ST", "ลาทำหมัน" },
                { "UL", "ลากิจพิเศษ (ไม่มีเงินเดือน)" },
                { "UL2", "ลากิจพิเศษ (ไม่มีเงินเดือน)" },
                { "VOT", "ลาไปใช้สิทธิ์เลือกตั้ง" },
                { "WL", "ลาแต่งงาน" },
                { "WN", "จดหมายเตือน" },
            };

            return leaveTypeNames.ContainsKey(attenCode) ? leaveTypeNames[attenCode] : attenCode;
        }
    }
}
