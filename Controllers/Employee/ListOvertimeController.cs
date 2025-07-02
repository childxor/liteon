using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using IPS_TH.Data;
using IPS_TH.Models.Employee;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace IPS_TH.Controllers.Employee
{
    public class ListOvertimeController : BaseController
    {
        private readonly string _connectionString;
        private readonly string _hrIpsConnectionString;
        private readonly IConfiguration _configuration;
        private readonly string _sql944ConnectionString;
        private readonly ApplicationDbContext _dbContext;
        private readonly string _ces941ConnectionString;

        public ListOvertimeController(IConfiguration configuration, ApplicationDbContext context)
            : base(context)
        {
            _dbContext = context;
            _connectionString = context.Database.GetDbConnection().ConnectionString;
            _configuration = configuration;
            _sql944ConnectionString = configuration.GetConnectionString("SQL944");
            _hrIpsConnectionString = configuration.GetConnectionString("HR_IPS");
            _ces941ConnectionString = configuration.GetConnectionString("CES941");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                LoadPermissions("ListOvertime", "Index");
                return View("~/Views/Employee/ListOvertime.cshtml");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ListOvertime action: {ex.Message}");
                return View("~/Views/Employee/ListOvertime.cshtml");
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetOvertimeList(
            string empNo = null,
            int? year = null,
            int? month = null,
            string status = null,
            string empName = null,
            string jgCode = null
        )
        {
            try
            {
                // ตรวจสอบสิทธิ์การแก้ไข
                LoadPermissions("ListOvertime", "Index");
                var permissions = ViewData["Permissions"] as Dictionary<string, bool>;
                var canEdit = permissions?["CanEdit"] ?? false;

                // ถ้าไม่มีสิทธิ์แก้ไข ให้ดูได้แค่ข้อมูลตัวเอง
                if (!canEdit)
                {
                    empNo = HttpContext.Session.GetString("EmployeeID");
                }

                using (var connection = new SqlConnection(_ces941ConnectionString))
                {
                    await connection.OpenAsync();
                    var sql =
                        @"
                        SELECT   
                            t.OTReqNo, t.OTSeqNo, t.SuspReqNo, t.EmpNo, t.BossId, 
                            t.ShiftCode, t.WrkDate, t.DteTmeStr, t.DteTmeEnd, 
                            t.SecCode, t.CostCenter, t.TotalOTHrs, t.ApprStatus, 
                            t.ApprDate, t.Year, t.Month, t.Period, t.Remark,
                            t.OTBfrQty, t.OTBfrRate, t.OTNrmQty, t.OTNrmRate, 
                            t.OTAftQty, t.OTAftRate, t.CreateDate, t.CreateBy,
                            ISNULL(t.OTBfrQty, 0) + ISNULL(t.OTNrmQty, 0) + ISNULL(t.OTAftQty, 0) as TotalHours,
                            CONCAT(COALESCE(m.PrefixName, ''), ' ', COALESCE(m.EmpName, ''), ' ', COALESCE(m.EmpLName, '')) as FullName,
                            c.JGCode                        FROM TOTPlan t
                        LEFT JOIN MEmpBasic m ON t.EmpNo = m.EmpNo
                        LEFT JOIN TEmpComDta c ON t.EmpNo = c.EmpNo
                        LEFT JOIN MTitleJG jg ON c.TitleCode = jg.TitleCode
                        WHERE (@EmpNo IS NULL OR t.EmpNo = @EmpNo)
                        AND (@Year IS NULL OR t.Year = @Year)
                        AND (@Month IS NULL OR t.Month = @Month)
                        AND (@Status IS NULL OR t.ApprStatus = @Status)
                        AND (@EmpName IS NULL OR CONCAT(COALESCE(m.PrefixName, ''), ' ', COALESCE(m.EmpName, ''), ' ', COALESCE(m.EmpLName, '')) LIKE '%' + @EmpName + '%')
                        AND (@JgCode IS NULL OR c.JGCode = @JgCode)
                        AND m.Language = 'EN' 
                        GROUP BY t.OTReqNo, t.OTSeqNo, t.SuspReqNo, t.EmpNo, t.BossId, 
                                t.ShiftCode, t.WrkDate, t.DteTmeStr, t.DteTmeEnd, 
                                t.SecCode, t.CostCenter, t.TotalOTHrs, t.ApprStatus, 
                                t.ApprDate, t.Year, t.Month, t.Period, t.Remark,
                                t.OTBfrQty, t.OTBfrRate, t.OTNrmQty, t.OTNrmRate, 
                                t.OTAftQty, t.OTAftRate, t.CreateDate, t.CreateBy,
                                m.PrefixName, m.EmpName, m.EmpLName, c.JGCode
                        ORDER BY t.CreateDate DESC";

                    var result = await connection.QueryAsync(
                        sql,
                        new
                        {
                            EmpNo = empNo,
                            Year = year,
                            Month = month,
                            Status = status,
                            EmpName = empName,
                            JgCode = jgCode,
                        }
                    );

                    // คำนวณสรุปจำนวน OT
                    var summary = new
                    {
                        Total = result.Count(),
                        Approved = result.Count(r => r.ApprStatus == "Approved"),
                        Pending = result.Count(r => r.ApprStatus == "Pending"),
                        Rejected = result.Count(r => r.ApprStatus == "Rejected"),
                        TotalHours = result.Sum(r => (decimal?)r.TotalHours ?? 0m),
                        ApprovedHours = result
                            .Where(r => r.ApprStatus == "Approved")
                            .Sum(r => (decimal?)r.TotalHours ?? 0m),
                    };

                    return Json(
                        new
                        {
                            success = true,
                            data = result,
                            summary = summary,
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOvertimeById(string otReqNo, decimal otSeqNo)
        {
            try
            {
                using (var connection = new SqlConnection(_ces941ConnectionString))
                {
                    await connection.OpenAsync();
                    var sql =
                        @"
                        SELECT t.*, 
                               CONCAT(COALESCE(m.PrefixName, ''), ' ', COALESCE(m.EmpName, ''), ' ', COALESCE(m.EmpLName, '')) as FullName,
                               c.JGCode,
                               c.TitleCode,
                               c.DepCode
                        FROM TOTPlan t
                        LEFT JOIN MEmpBasic m ON t.EmpNo = m.EmpNo
                        LEFT JOIN TEmpComDta c ON t.EmpNo = c.EmpNo
                        LEFT JOIN MTitleJG jg ON c.TitleCode = jg.TitleCode
                        WHERE t.OTReqNo = @otReqNo 
                        AND t.OTSeqNo = @otSeqNo
                        group by t.OTReqNo, t.OTSeqNo, t.SuspReqNo, t.EmpNo, t.BossId, 
                            t.ShiftCode, t.WrkDate, t.DteTmeStr, t.DteTmeEnd, 
                            t.SecCode, t.CostCenter, t.TotalOTHrs, t.ApprStatus, 
                            t.ApprDate, t.Year, t.Month, t.Period, t.Remark,
                            t.OTBfrQty, t.OTBfrRate, t.OTNrmQty, t.OTNrmRate, 
                            t.OTAftQty, t.OTAftRate, t.CreateDate, t.CreateBy,
                            ISNULL(t.OTBfrQty, 0) + ISNULL(t.OTNrmQty, 0) + ISNULL(t.OTAftQty, 0) as TotalHours, 
                            m.PrefixName, m.EmpName, m.EmpLName, c.JGCode, c.TitleCode, c.DepCode";

                    var result = await connection.QueryFirstOrDefaultAsync(
                        sql,
                        new { otReqNo, otSeqNo }
                    );
                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOvertimeStatus(
            string otReqNo,
            decimal otSeqNo,
            string status,
            string remark = null
        )
        {
            try
            {
                using (var connection = new SqlConnection(_ces941ConnectionString))
                {
                    await connection.OpenAsync();
                    var sql =
                        @"
                        UPDATE TOTPlan 
                        SET ApprStatus = @Status, 
                            ApprDate = GETDATE(),
                            Remark = COALESCE(@Remark, Remark)
                        WHERE OTReqNo = @OtReqNo 
                        AND OTSeqNo = @OtSeqNo";

                    var affected = await connection.ExecuteAsync(
                        sql,
                        new
                        {
                            Status = status,
                            Remark = remark,
                            OtReqNo = otReqNo,
                            OtSeqNo = otSeqNo,
                        }
                    );

                    return Json(
                        new
                        {
                            success = affected > 0,
                            message = affected > 0
                                ? "อัพเดทสถานะสำเร็จ"
                                : "ไม่พบข้อมูลที่ต้องการอัพเดท",
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOvertime(string otReqNo, decimal otSeqNo)
        {
            try
            {
                using (var connection = new SqlConnection(_ces941ConnectionString))
                {
                    await connection.OpenAsync();
                    var sql =
                        @"
                        DELETE FROM TOTPlan 
                        WHERE OTReqNo = @OtReqNo 
                        AND OTSeqNo = @OtSeqNo
                        AND ApprStatus = 'Pending'"; // ลบได้แค่รายการที่ยังไม่ได้อนุมัติ

                    var affected = await connection.ExecuteAsync(
                        sql,
                        new { OtReqNo = otReqNo, OtSeqNo = otSeqNo }
                    );

                    return Json(
                        new
                        {
                            success = affected > 0,
                            message = affected > 0
                                ? "ลบข้อมูลสำเร็จ"
                                : "ไม่สามารถลบข้อมูลได้ หรือข้อมูลถูกอนุมัติแล้ว",
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(
            string empNo = null,
            int? year = null,
            int? month = null,
            string status = null,
            string empName = null,
            string jgCode = null
        )
        {
            try
            {
                // ตรวจสอบสิทธิ์
                LoadPermissions("ListOvertime", "Index");
                var permissions = ViewData["Permissions"] as Dictionary<string, bool>;
                var canEdit = permissions?["CanEdit"] ?? false;

                if (!canEdit)
                {
                    empNo = HttpContext.Session.GetString("EmployeeID");
                }

                using (var connection = new SqlConnection(_ces941ConnectionString))
                {
                    await connection.OpenAsync();
                    var sql =
                        @"
                        SELECT   
                            t.OTReqNo as 'เลขที่ OT',
                            t.EmpNo as 'รหัสพนักงาน',
                            CONCAT(COALESCE(m.PrefixName, ''), ' ', COALESCE(m.EmpName, ''), ' ', COALESCE(m.EmpLName, '')) as 'ชื่อพนักงาน',
                            c.JGCode as 'ระดับ JG',
                            FORMAT(t.WrkDate, 'dd/MM/yyyy') as 'วันที่ทำงาน',
                            FORMAT(t.DteTmeStr, 'HH:mm') + ' - ' + FORMAT(t.DteTmeEnd, 'HH:mm') as 'เวลา',
                            t.TotalOTHrs as 'ชั่วโมง OT',
                            t.SecCode as 'แผนก',
                            CASE t.ApprStatus 
                                WHEN 'Approved' THEN 'อนุมัติแล้ว' 
                                WHEN 'Pending' THEN 'รออนุมัติ' 
                                WHEN 'Rejected' THEN 'ไม่อนุมัติ' 
                                ELSE t.ApprStatus 
                            END as 'สถานะ',
                            t.Remark as 'หมายเหตุ',
                            FORMAT(t.CreateDate, 'dd/MM/yyyy HH:mm') as 'วันที่สร้าง'
                        FROM TOTPlan t
                        LEFT JOIN MEmpBasic m ON t.EmpNo = m.EmpNo
                        LEFT JOIN TEmpComDta c ON t.EmpNo = c.EmpNo
                        LEFT JOIN MTitleJG jg ON c.TitleCode = jg.TitleCode
                        WHERE (@EmpNo IS NULL OR t.EmpNo = @EmpNo)
                        AND (@Year IS NULL OR t.Year = @Year)
                        AND (@Month IS NULL OR t.Month = @Month)
                        AND (@Status IS NULL OR t.ApprStatus = @Status)
                        AND (@EmpName IS NULL OR CONCAT(COALESCE(m.PrefixName, ''), ' ', COALESCE(m.EmpName, ''), ' ', COALESCE(m.EmpLName, '')) LIKE '%' + @EmpName + '%')
                        AND (@JgCode IS NULL OR c.JGCode = @JgCode)
                        ORDER BY t.CreateDate DESC";

                    var result = await connection.QueryAsync(
                        sql,
                        new
                        {
                            EmpNo = empNo,
                            Year = year,
                            Month = month,
                            Status = status,
                            EmpName = empName,
                            JgCode = jgCode,
                        }
                    );

                    return Json(
                        new
                        {
                            success = true,
                            data = result,
                            message = "ส่งออกข้อมูลสำเร็จ",
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOvertimeStatistics(int year, int? month = null)
        {
            try
            {
                using (var connection = new SqlConnection(_ces941ConnectionString))
                {
                    await connection.OpenAsync();

                    string sql;
                    object parameters;

                    if (month.HasValue)
                    {
                        // สstatisticsระดับรายวัน
                        sql =
                            @"
                            SELECT 
                                DAY(WrkDate) as Day,
                                COUNT(*) as Count,
                                SUM(TotalOTHrs) as TotalHours
                            FROM TOTPlan 
                            WHERE YEAR(WrkDate) = @Year AND MONTH(WrkDate) = @Month
                            GROUP BY DAY(WrkDate)
                            ORDER BY DAY(WrkDate)";
                        parameters = new { Year = year, Month = month };
                    }
                    else
                    {
                        // สถิติระดับรายเดือน
                        sql =
                            @"
                            SELECT 
                                MONTH(WrkDate) as Month,
                                COUNT(*) as Count,
                                SUM(TotalOTHrs) as TotalHours
                            FROM TOTPlan 
                            WHERE YEAR(WrkDate) = @Year
                            GROUP BY MONTH(WrkDate)
                            ORDER BY MONTH(WrkDate)";
                        parameters = new { Year = year };
                    }

                    var result = await connection.QueryAsync(sql, parameters);
                    return Json(new { success = true, data = result });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
