using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq; // เพิ่ม Linq namespace
using System.Net;
using System.Net.Mail;
using System.Text;
using Dapper;
using IPS_TH.Data;
using IPS_TH.Models.AssetFactory;
using IPS_TH.Models.Attendance; // ใช้ namespace ที่ถูกต้อง
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json; // เพิ่มการนำเข้าสำหรับ JsonConvert
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace IPS_TH.Controllers.Employee
{
    public class AttendanceController : BaseController
    {
        private readonly string _connectionString;
        private readonly string _hrIpsConnectionString;
        private readonly string _ces941ConnectionString;
        private readonly IConfiguration _configuration;
        private readonly string _sql944ConnectionString;
        private readonly ApplicationDbContext _dbContext;
        private readonly IDbConnection _dbConnection;
        private readonly ILogger<AttendanceController> _logger;

        private readonly ApplicationDbContext _context; // เพิ่มตัวแปร _context

        // เพิ่ม Dictionary สำหรับเก็บข้อมูลการลาออก (Cache)
        private Dictionary<string, dynamic> _resignationCache = new Dictionary<string, dynamic>();
        private DateTime _resignationCacheExpiry = DateTime.MinValue;
        private readonly object _resignationCacheLock = new object();

        public AttendanceController(
            IConfiguration configuration,
            ApplicationDbContext context,
            ILogger<AttendanceController> logger
        )
            : base(context)
        {
            _dbContext = context;
            _connectionString = context.Database.GetDbConnection().ConnectionString;
            _configuration = configuration;
            _sql944ConnectionString = configuration.GetConnectionString("SQL944");
            _hrIpsConnectionString = configuration.GetConnectionString("HR_IPS");
            _ces941ConnectionString = configuration.GetConnectionString("CES941");
            _dbConnection = new SqlConnection(_connectionString);
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Workplan()
        {
            try
            {
                await LoadPermissions("Attendance", "Workplan");
                return View("~/Views/Attendance/Workplan.cshtml");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Attendance action: {ex.Message}");
                return View("~/Views/Attendance/Workplan.cshtml");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWorkplans()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var query =
                        @"SELECT * FROM emp_wrkplan WHERE record_status = 'N' ORDER BY work_date DESC";
                    var workplans = await connection.QueryAsync<emp_wrkplan>(query);
                    return Json(new { data = workplans });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWorkplanById(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var query = "SELECT * FROM emp_wrkplan WHERE id = @Id AND record_status = 'N'";
                    var workplan = await connection.QueryFirstOrDefaultAsync<emp_wrkplan>(
                        query,
                        new { Id = id }
                    );
                    return Json(workplan);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckIn()
        {
            try
            {
                // ดึงข้อมูลแผนก จาก sql944
                await LoadPermissions("Attendance", "CheckIn");
                using (var defaultconnection = new SqlConnection(_connectionString))
                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    var userDept = HttpContext.Session.GetString("Department");
                    var query = @"SELECT * FROM Dept";

                    var dept = await connection.QueryAsync<dynamic>(query);
                    ViewBag.Dept = dept;

                    // เพิ่ม debug log
                    _logger.LogInformation(
                        $"CheckIn: ดึงข้อมูลแผนกได้ {dept?.Count() ?? 0} รายการ"
                    );
                    _logger.LogInformation($"CheckIn: userDept = {userDept}");

                    // เพิ่มข้อมูล Shiftdept
                    var shiftDeptQuery =
                        @"SELECT * FROM emp_shift WHERE record_status = 'N' ORDER BY shift_group";
                    var shiftDept = await defaultconnection.QueryAsync<dynamic>(shiftDeptQuery);
                    ViewBag.Shiftdept = shiftDept;
                }
                ViewBag.UserName = HttpContext.Session.GetString("UserName");

                // เพิ่มการตั้งค่า ViewData สำหรับเปรียบเทียบ session department
                ViewData["CurrentUserDepartment"] = HttpContext.Session.GetString("Department");
                Console.WriteLine(ViewData["CurrentUserDepartment"]);

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CheckIn: {ex.Message}");
                return View("~/Views/Attendance/CheckIn.cshtml");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetShifts()
        {
            try
            {
                var shifts = await _dbContext
                    .emp_shift.Where(s => s.record_status == "N")
                    .OrderBy(s => s.sort)
                    .Select(s => new
                    {
                        s.Id,
                        s.shift_code,
                        s.shift_name,
                        s.shift_group,
                        s.start_time,
                        s.break_start_time,
                        s.break_end_time,
                        s.is_break2,
                        s.break2_start_time,
                        s.break2_end_time,
                        s.end_time,
                        s.ot_start_time,
                        s.ot_end_time,
                        s.sort,
                        s.record_status,
                    })
                    .ToListAsync();

                return Json(new { success = true, data = shifts });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetShifts: {ex.Message}");
                return Json(new { error = ex.Message });
            }
        }

        // เก็บเมธอดเดิมไว้เผื่อมีที่อื่นเรียกใช้
        private async Task<string> GetShiftCode()
        {
            var shiftCode = await _dbContext.emp_shift.FirstOrDefaultAsync();
            return shiftCode?.shift_code ?? "";
        }

        private string DetermineStatus(dynamic timeIn)
        {
            try
            {
                if (timeIn == null)
                    return "ไม่ระบุ";

                var inTime = (DateTime)timeIn;
                if (inTime.TimeOfDay > new TimeSpan(8, 0, 0))
                {
                    var lateMinutes = (inTime.TimeOfDay - new TimeSpan(8, 0, 0)).TotalMinutes;
                    return $"สาย {(int)lateMinutes} นาที";
                }
                return "ปกติ";
            }
            catch
            {
                return "ไม่สามารถระบุสถานะ";
            }
        }

        //โหลดการลา
        [HttpGet]
        public async Task<IActionResult> GetLeaveData(string date)
        {
            try
            {
                using (var connection = new SqlConnection(_ces941ConnectionString))
                {
                    string sql =
                        @"
                        SELECT 
                            TEmpLeave.EmpNo, 
                            TEmpLeave.AttenCode,
                            TEmpLeave.WrkDate,
                            TEmpLeave.DteTmeIn,
                            TEmpLeave.DteTmeOut,
                            TEmpLeave.LeaveQty,
                            TEmpLeave.LeaveType,
                            TEmpLeave.ApprStatus,
                            TEmpLeave.ApprDate,
                            TEmpLeave.LeaveReqNo,
                            TEmpLeave.SecCode,
                            TEmpLeave.CostCenter,
                            TEmpLeave.LeaveReqItem,
                            TEmpLeave.CaseId,
                            TEmpLeave.CreateDate,
                            TEmpLeave.CreateBy,
                            TEmpLeave.NextAppr,
                            MSecCode.SecDsc,
                            MEmpBasic.PrefixName,
                            MEmpBasic.EmpName,
                            MEmpBasic.EmpLName,
                            TEmpLeave.Remark
                        FROM TEmpLeave 
                        INNER JOIN MSecCode ON TEmpLeave.SecCode = MSecCode.SecCode AND MSecCode.Language = 'EN'
                        INNER JOIN MEmpBasic ON TEmpLeave.EmpNo = MEmpBasic.EmpNo AND MSecCode.Language = 'EN'
                        WHERE TEmpLeave.EmpNo = @EmpNo 
                        AND CAST(TEmpLeave.WrkDate AS DATE) = @WrkDate
                        AND TEmpLeave.ApprStatus IN ('Approve', 'On Approve')
                        AND MEmpBasic.Gender IN ('M', 'F')
                        ORDER BY TEmpLeave.WrkDate DESC";

                    var parameters = new
                    {
                        EmpNo = HttpContext.Session.GetString("EmployeeID"),
                        WrkDate = DateTime.Parse(date),
                    };

                    var leaveData = await connection.QueryAsync(sql, parameters);

                    var leaveDict = leaveData
                        .GroupBy(l => l.EmpNo.ToString().Trim())
                        .ToDictionary(g => g.Key, g => g.First());

                    return Json(
                        new
                        {
                            success = true,
                            data = leaveData,
                            message = "ดึงข้อมูลการลาสำเร็จ",
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการดึงข้อมูลการลา");
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        // สำหรับโหลด datatable ข้อมูลจาก HQMS_IPS
        [HttpPost]
        public async Task<IActionResult> GetDataHQMS_IPS(
            string dept = "all",
            string personName = "all",
            string startDate = null,
            string endDate = null,
            string shiftFilter = "all",
            bool showResigned = true,
            bool export = false,
            bool hideResigned = false,
            bool hideAbsent = false,
            bool showIncomplete = false
        )
        {
            try 
            {
                // 1. ตรวจสอบสิทธิ์และรับค่าพารามิเตอร์
                if (!CheckPermission("Edit"))
                {
                    HttpContext.Session.GetString("EmployeeID");
                }

                var parameters = GetRequestParameters(
                    dept,
                    personName,
                    startDate,
                    endDate,
                    shiftFilter
                );

                // 2. ดึงข้อมูลที่จำเป็น
                var workPlans = await GetWorkPlanData();
                var personShiftResult = await GetPersonShiftData(
                    parameters.Dept,
                    parameters.PersonName,
                    parameters.StartDate,
                    parameters.EndDate
                );

                // ดึงข้อมูลตู้เก็บของ
                var cabinetResult = await GetCabinetData();

                // 2.1 ดึงข้อมูลคอมพิวเตอร์ทั้งหมด
                var computersByOwner = await GetComputersByOwnerAsync();

                var personShifts =
                    ((personShiftResult as JsonResult)?.Value as dynamic)?.data
                    ?? new List<dynamic>();

                // 3. สร้างช่วงวันที่ต้องการ
                var dateRange = GetDateRange(parameters.StartDate, parameters.EndDate);
                var result = new List<object>();

                // 3.1 โหลดข้อมูลการลาออกทั้งหมดในครั้งเดียว
                var empNos = new List<string>();
                foreach (var person in personShifts)
                {
                    var personId = person?.personID?.ToString();
                    if (!string.IsNullOrEmpty(personId))
                    {
                        var empNo = personId.Replace("-1", "")?.Trim();
                        if (!string.IsNullOrEmpty(empNo))
                        {
                            empNos.Add(empNo);
                        }
                    }
                }
                await LoadAllResignationInfo(empNos);

                // ดึงข้อมูลการลาออกทั้งหมดครั้งเดียว
                Dictionary<string, dynamic> resignationDict = new Dictionary<string, dynamic>(
                    StringComparer.OrdinalIgnoreCase
                );
                using (var connection = new SqlConnection(_ces941ConnectionString))
                {
                    var resignQuery =
                        @"
                        SELECT EmpNo, EmpResignDate 
                        FROM MEmpBasic 
                        WHERE EmpResignDate IS NOT NULL";

                    var resignInfos = await connection.QueryAsync<dynamic>(resignQuery);

                    // เก็บข้อมูลในรูปแบบ Dictionary เพื่อค้นหาได้ง่าย
                    foreach (var info in resignInfos)
                    {
                        if (info.EmpNo != null)
                        {
                            resignationDict[info.EmpNo.ToString().Trim()] = info;
                        }
                    }
                }

                // 4. ประมวลผลข้อมูลตามช่วงวันที่
                foreach (var date in dateRange)
                {
                    // 4.1 ดึงข้อมูลโอทีทั้งหมดในครั้งเดียว
                    var overtimeDict = await GetAllOvertimeInfoDictionary(date);

                    var currentDate = date.ToString("yyyy-MM-dd");
                    await ProcessDateData(
                        currentDate,
                        personShifts,
                        workPlans,
                        result,
                        parameters.ShiftFilter,
                        resignationDict,
                        overtimeDict,
                        showResigned,
                        cabinetResult,
                        hideResigned,
                        hideAbsent,
                        showIncomplete,
                        computersByOwner
                    );
                }

                // 5. ส่งผลลัพธ์กลับ
                if (export)
                    return Json(result);

                // เปลี่ยนรูปแบบการส่งข้อมูลกลับเป็นแบบ non-serverSide
                return Json(new { data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetDataHQMS_IPS: {ex.Message}");
                return Json(new { data = new List<object>() });
            }
        }

        // GetCabinetData
        private async Task<List<dynamic>> GetCabinetData()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query =
                        @"
                            SELECT 
                                Id, 
                                No, 
                                Owner, 
                                status, 
                                updated_at, 
                                updated_by, 
                                created_at, 
                                created_by, 
                                record_status
                            FROM Cabinet_emp";

                    var result = await connection.QueryAsync<dynamic>(query);

                    _logger.LogInformation(
                        $"ดึงข้อมูลตู้ล็อคเกอร์จำนวน {result.Count()} รายการจากฐานข้อมูล"
                    );

                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"เกิดข้อผิดพลาดในการดึงข้อมูลตู้ล็อคเกอร์: {ex.Message}");
                return new List<dynamic>();
            }
        }

        // GetDoorAccessHistory
        [HttpPost]
        public async Task<IActionResult> GetDoorAccessHistory(
            string employeeId,
            string date,
            string shift
        )
        {
            // แก้ไขปัญหา SQL Error: "Conversion failed when converting date and/or time from character string."
            // ปัญหานี้เกิดจากการใช้ BETWEEN กับ TIME แต่ใส่ค่าเป็น datetime string (dd-MM-yyyy HH:mm:ss) ซึ่งผิด format
            // วิธีแก้: ให้ filter ด้วย eventTime >= startDatetime AND eventTime <= endDatetime (ใช้ datetime ตรงๆ)

            try
            {
                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    var query =
                        @"
                        SELECT rowAutoID,
                                eventTime,
                                SUBSTRING(personID, 1, LEN(personID) - 2) as basePersonID,
                                personID,
                                eventCard,
                                personName, 
                                deptCode,
                                deptName,
                                deptID,
                                CAST(eventTime AS DATE) AS rDate,
                                deviceName
                        FROM PubEvent 
                        WHERE eventType = '1'
                        AND (
                            (eventCode = '0001' AND deviceL1Type IN ('GCU', 'RAC2400', 'HDE200', 'NCU', 'RAC2400N', 'RAC2000EL', 'RAC852PMFV'))
                            OR (eventCode IN ('0000', '0009') AND deviceL1Type IN ('HTA860', 'HTA850', 'HTA852PMF', 'RAC2000P', 'HDP100', 'RAC900', 'RAC940', 'RAC960', 'RAC970', 'RTA650', 'RAC852PMFV'))
                            OR (eventCode = '003D' AND deviceL1Type IN ('RAC940', 'RAC960'))
                            OR (eventCode IN ('0000', '0001', '0009') AND deviceL1Type = 'RAC960PMF')
                            OR (eventCode = '0014' AND deviceL1Type = 'RAC960')
                            OR (eventCode IN ('0007', '7', '1') AND deviceL1Type IN ('NCU', 'RAC2000EL'))
                            OR (eventCode IN ('01', '02', '21', '22') AND deviceL1Type = 'UNI')
                            OR ('HMS' + eventCode IN ('HMS0000', 'HMS0009', 'HMS003D', 'HMS003F'))
                        )
                    ";

                    var today = DateTime.Parse(date).ToString("yyyy-MM-dd");

                    if (shift.Contains("NT1") || shift.Contains("N01"))
                    {
                        // กะดึก: เริ่ม 18:00 ของวันก่อน ถึง 06:00 ของวันปัจจุบัน
                        var startDateTime =
                            DateTime.Parse(date).AddDays(-1).ToString("yyyy-MM-dd") + " 18:00:00";
                        var endDateTime = DateTime.Parse(date).ToString("yyyy-MM-dd") + " 10:00:00";
                        query += " AND eventTime >= @startDateTime AND eventTime <= @endDateTime";

                        query += " AND personID like @personId";
                        var result = await connection.QueryAsync(
                            query,
                            new
                            {
                                startDateTime,
                                endDateTime,
                                personId = "%" + employeeId + "%",
                            }
                        );

                        return Json(new { success = true, data = result });
                    }
                    else
                    {
                        // กะปกติ: เฉพาะวันที่เลือก
                        query += " AND CAST(eventTime AS DATE) = @today";
                        query += " AND personID like @personId";
                        var result = await connection.QueryAsync(
                            query,
                            new { today, personId = "%" + employeeId + "%" }
                        );

                        return Json(new { success = true, data = result });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // แยกฟังก์ชันย่อยสำหรับรับค่าพารามิเตอร์
        private (
            string Dept,
            string PersonName,
            string StartDate,
            string EndDate,
            string ShiftFilter
        ) GetRequestParameters(
            string dept,
            string personName,
            string startDate,
            string endDate,
            string shiftFilter
        )
        {
            // ใช้ค่าที่ส่งมาจาก parameter โดยตรง
            return (
                Dept: dept ?? "all",
                PersonName: personName ?? "all",
                StartDate: startDate ?? DateTime.Now.ToString("yyyy-MM-dd"),
                EndDate: endDate ?? startDate ?? DateTime.Now.ToString("yyyy-MM-dd"),
                ShiftFilter: shiftFilter ?? "all"
            );
        }

        // แยกฟังก์ชันย่อยสำหรับสร้างช่วงวันที่
        private List<DateTime> GetDateRange(string startDate, string endDate)
        {
            var start = DateTime.ParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var end = DateTime
                .ParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                .AddDays(1);
            return Enumerable
                .Range(0, (end - start).Days)
                .Select(offset => start.AddDays(offset))
                .ToList();
        }

        // เมธอดสำหรับ API endpoint (คงไว้เพื่อความเข้ากันได้กับโค้ดเดิม)
        [HttpGet]
        public async Task<IActionResult> GetAllOvertimeInfo(DateTime date)
        {
            try
            {
                var data = await GetAllOvertimeInfoDictionary(date);

                // แปลง Dictionary เป็น List เพื่อส่งกลับเป็น JSON
                var result = data.Values.ToList();

                return Json(new { data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError($"เกิดข้อผิดพลาดในการดึงข้อมูล Overtime: {ex.Message}");
                return Json(new { data = new List<object>() });
            }
        }

        // เมธอดใหม่ที่คืนค่าเป็น Dictionary สำหรับใช้ภายใน
        private async Task<Dictionary<string, dynamic>> GetAllOvertimeInfoDictionary(DateTime date)
        {
            var result = new Dictionary<string, dynamic>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (var connection = new SqlConnection(_ces941ConnectionString))
                {
                    var query =
                        @"
                        WITH RankedOT AS (
                            SELECT 
                                OTReqNo, OTSeqNo, SuspReqNo, EmpNo, BossId, ShiftCode, 
                                WrkDate, DteTmeStr, DteTmeEnd, SecCode, CostCenter,
                                TmpBossId, TmpSecCode, TmpCostCenter, ActionType, 
                                ActionReason, CreateDate, CreateBy, FlagDel, CaseId,
                                TotalOTHrs, ApprStatus, ApprDate, Year, Month, Period,
                                Remark, OTBfrQty, OTBfrRate, OTNrmQty, OTNrmRate,
                                OTAftQty, OTAftRate, NextAppr, CEInd, Abnormalrun,
                                ISNULL(OTBfrQty, 0) + ISNULL(OTNrmQty, 0) + ISNULL(OTAftQty, 0) as TotalHours,
                                ROW_NUMBER() OVER (PARTITION BY EmpNo ORDER BY CreateDate DESC) as rn
                            FROM TOTPlan
                            WHERE CAST(WrkDate AS DATE) = @WrkDate AND (ApprStatus in ('Approved', 'On Approve'))
                        )
                        SELECT 
                            OTReqNo, OTSeqNo, SuspReqNo, EmpNo, BossId, ShiftCode, 
                            WrkDate, DteTmeStr, DteTmeEnd, SecCode, CostCenter,
                            TmpBossId, TmpSecCode, TmpCostCenter, ActionType, 
                            ActionReason, CreateDate, CreateBy, FlagDel, CaseId,
                            TotalOTHrs, ApprStatus, ApprDate, Year, Month, Period,
                            Remark, OTBfrQty, OTBfrRate, OTNrmQty, OTNrmRate,
                            OTAftQty, OTAftRate, NextAppr, CEInd, Abnormalrun,
                            TotalHours
                        FROM RankedOT
                        WHERE rn = 1";

                    var otData = await connection.QueryAsync(
                        query,
                        new { WrkDate = date.ToString("yyyy-MM-dd") }
                    );

                    // จัดกลุ่มข้อมูลตามรหัสพนักงาน
                    foreach (var item in otData)
                    {
                        string empNo = item.EmpNo?.ToString().Trim();
                        if (!string.IsNullOrEmpty(empNo))
                        {
                            result[empNo] = new
                            {
                                hasOvertime = true,
                                otReqNo = item.OTReqNo,
                                totalHours = item.TotalHours,
                                approvalStatus = item.ApprStatus,
                                startTime = item.DteTmeStr,
                                endTime = item.DteTmeEnd,
                                beforeHours = item.OTBfrQty,
                                normalHours = item.OTNrmQty,
                                afterHours = item.OTAftQty,
                                remark = item.Remark,
                                createDate = item.CreateDate,
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAllOvertimeInfoDictionary: {ex.Message}");
            }

            return result;
        }

        // ฟังก์ชันนี้จะดึงข้อมูลคอมพิวเตอร์ทั้งหมดและจัดกลุ่มตาม Owner (รหัสพนักงานเจ้าของ)
        private async Task<ILookup<string, dynamic>> GetComputersByOwnerAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query =
                    @"
                    SELECT Id, ProductSerial, AssetNo, CustomerNo, CustomerName, Department, DepartmentName, Line, 
                        LineName, Owner, OwnerName, Name, Type, Status, Location, LocationDetail, Model, Manufacturer, CountryOfOrigin, 
                        InstallationDate, StartDate, EndDate, Remarks, CreatedDate, CreatedBy, UpdatedDate, UpdatedBy, IsDeleted, DeletedDate, DeletedBy, WindowsVersion
                    FROM asset 
                    WHERE IsDeleted = 'False'
                ";
                var data = await connection.QueryAsync<dynamic>(query);
                foreach (var item in data)
                {
                    item.Type = AssetType.GetDisplayName(item.Type);
                }

                // จัดกลุ่มข้อมูลตาม Owner (รหัสพนักงานเจ้าของ)
                // ถ้า Owner เป็น null ให้ใช้ string ว่าง
                return data.ToLookup(x => (string)(x.Owner ?? "").ToString().Trim(), x => x);
            }
        }

        // แยกฟังก์ชันย่อยสำหรับประมวลผลข้อมูลรายคน
        private async Task ProcessDateData(
            string currentDate,
            IEnumerable<dynamic> personShifts,
            List<emp_wrkplan> workPlans,
            List<object> result,
            string shiftFilter,
            Dictionary<string, dynamic> resignationDict,
            Dictionary<string, dynamic> overtimeDict,
            bool showResigned = true,
            List<dynamic> cabinetResult = null,
            bool hideResigned = false,
            bool hideAbsent = false,
            bool showIncomplete = false,
            ILookup<string, dynamic> computersByOwner = null
        )
        {
            using (var connection = new SqlConnection(_ces941ConnectionString))
            {
                // 1. ดึงข้อมูลการลา
                var leaveDict = await GetLeaveDictionary(connection, currentDate);

                // 2. ดึงข้อมูลการสแกน
                (dynamic dayEvents, dynamic nightEvents) = await GetScanEvents(currentDate);

                // 3. ประมวลผลข้อมูลรายคน
                foreach (var person in personShifts.Where(p => p?.date == currentDate))
                {
                    var personData = await ProcessPersonData(
                        person,
                        workPlans,
                        leaveDict,
                        dayEvents,
                        nightEvents,
                        currentDate,
                        shiftFilter,
                        resignationDict,
                        overtimeDict,
                        showResigned,
                        cabinetResult,
                        hideResigned,
                        hideAbsent,
                        showIncomplete,
                        computersByOwner
                    );

                    if (personData != null)
                    {
                        result.Add(personData);
                    }
                }
            }
        }

        // แยกฟังก์ชันย่อยสำหรับดึงข้อมูลการลา
        private async Task<Dictionary<string, dynamic>> GetLeaveDictionary(
            IDbConnection connection,
            string date
        )
        {
            string leaveSql =
                @"
                SELECT 
                    l.EmpNo, l.AttenCode, l.WrkDate, l.DteTmeIn, l.DteTmeOut, 
                    l.LeaveQty, l.LeaveType, l.ApprStatus, l.ApprDate, l.SecCode, 
                    l.CostCenter, l.BossId,
                    -- เพิ่มข้อมูลชื่อ boss
                    b.PrefixName + ' ' + b.EmpName + ' ' + b.EmpLName as BossName,
                    l.LeaveReqNo, l.LeaveReqItem, l.ChangeDate, l.ChangeBy, 
                    l.FlagDel, l.Remark, l.Attachment, l.TmpSeccode, 
                    l.TmpCostCenter, l.TmpBossId, l.CaseId, l.CreateDate, 
                    l.CreateBy, l.ActionType, l.ActionReason, l.NextAppr,
                    l.LeaveQtyReq, l.LeaveReqUnit, l.CEInd, l.SummaryUpdate, 
                    l.NewLeaveInd
                FROM TEmpLeave l
                LEFT JOIN MEmpBasic b ON l.BossId = b.EmpNo 
                WHERE CAST(l.WrkDate AS DATE) = @WrkDate
                AND l.ApprStatus IN ('Approve', 'On Approve')";

            var leaveData = await connection.QueryAsync<dynamic>(leaveSql, new { WrkDate = date });
            var dictionary = new Dictionary<string, dynamic>(StringComparer.OrdinalIgnoreCase);

            foreach (var group in leaveData.GroupBy(l => l.EmpNo.ToString().Trim()))
            {
                dictionary[group.Key] = group.First();
            }

            return dictionary;
        }

        [HttpPost]
        public async Task<IActionResult> ClearSearch()
        {
            try
            {
                // ส่งค่าเริ่มต้นกลับไป
                var defaultResult = await GetDataHQMS_IPS(
                    dept: "all",
                    personName: "all",
                    startDate: DateTime.Now.ToString("yyyy-MM-dd"),
                    endDate: DateTime.Now.ToString("yyyy-MM-dd"),
                    shiftFilter: "all"
                );

                return defaultResult;
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private double CalculateWorkHours(DateTime? timeIn, DateTime? timeOut)
        {
            try
            {
                if (!timeIn.HasValue || !timeOut.HasValue)
                    return 0;

                // คำนวณระยะเวลาทำงานทั้งหมด
                var duration = timeOut.Value - timeIn.Value;

                // ถ้าเวลาติดลบ (กรณีข้ามวัน) ให้บวกเพิ่ม 24 ชั่วโมง
                if (duration.TotalHours < 0)
                {
                    duration = duration.Add(TimeSpan.FromHours(24));
                }

                // ถ้าทำงานเกิน 16 ชั่วโมง อาจเป็นข้อมูลผิดพลาด
                if (duration.TotalHours > 16)
                {
                    return 0;
                }

                // หักเวลาพัก 1 ชั่วโมงถ้าทำงานมากกว่า 5 ชั่วโมง
                if (duration.TotalHours > 5)
                {
                    duration = duration.Subtract(TimeSpan.FromHours(1));
                }

                return Math.Round(duration.TotalHours, 2);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private async Task<emp_wrkplan> GetDayType(
            List<emp_wrkplan> workPlans,
            string currentDate,
            string shiftMent
        )
        {
            try
            {
                // ถ้า shiftMent เป็น "OFFICE2" ให้เปลี่ยนเป็น OF02
                if (shiftMent == "OFFICE2")
                {
                    shiftMent = "OF02";
                }
                // ถ้า shiftMent เป็น "OFFICE" ให้เปลี่ยนเป็น OF01
                else if (shiftMent == "OFFICE")
                {
                    shiftMent = "OF01";
                }
                // ถ้า shiftMent เป็น "DAY" ให้เปลี่ยนเป็น D01
                else if (shiftMent == "DAY")
                {
                    shiftMent = "D01";
                }
                // ถ้า shiftMent เป็น "NIGHT" ให้เปลี่ยนเป็น N01
                else if (shiftMent == "NIGHT")
                {
                    shiftMent = "N01";
                }

                // แปลง currentDate string เป็น DateTime
                if (DateTime.TryParse(currentDate, out DateTime targetDate))
                {
                    // หา workplan ที่ตรงกับเงื่อนไข
                    var resultDataPlan = workPlans.FirstOrDefault(x =>
                        x.work_date.Date == targetDate.Date
                        && // เปรียบเทียบเฉพาะวันที่
                        x.workplan_id == shiftMent
                    );

                    // ถ้าไม่พบ workplan ที่ตรงกับ workplan_id ให้ลองค้นหาด้วย shift_code แทน
                    if (resultDataPlan == null)
                    {
                        resultDataPlan = workPlans.FirstOrDefault(x =>
                            x.work_date.Date == targetDate.Date
                            && // เปรียบเทียบเฉพาะวันที่
                            x.shift_code == shiftMent
                        );
                    }

                    // ถ้ายังไม่พบอีก ให้ใช้ workplan ใดๆ ที่มีวันที่ตรงกัน
                    if (resultDataPlan == null)
                    {
                        resultDataPlan = workPlans.FirstOrDefault(x =>
                            x.work_date.Date == targetDate.Date
                        );

                        if (resultDataPlan != null)
                        {
                            _logger.LogWarning(
                                $"Using fallback workplan for date {targetDate.Date} with shift {resultDataPlan.shift_code}"
                            );
                        }
                    }

                    // ถ้ายังไม่พบอีก ให้ใช้ workplan ล่าสุด
                    if (resultDataPlan == null)
                    {
                        resultDataPlan = workPlans
                            .OrderByDescending(x => x.work_date)
                            .FirstOrDefault();

                        if (resultDataPlan != null)
                        {
                            _logger.LogWarning(
                                $"Using latest workplan from {resultDataPlan.work_date.Date} with shift {resultDataPlan.shift_code}"
                            );
                        }
                    }

                    return resultDataPlan;
                }

                _logger.LogWarning($"Invalid date format: {currentDate}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetDayType: {ex.Message}");
                return null;
            }
        }

        public async Task<IActionResult> GetEventData(
            string startDate,
            string doorId,
            string inOut,
            string shiftType
        )
        {
            try
            {
                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    var query =
                        @"
                        SELECT rowAutoID,
                                eventTime,
                                SUBSTRING(personID, 1, LEN(personID) - 2) as basePersonID,
                                personID,
                                eventName,
                                eventCard,
                                personName, 
                                deptCode,
                                deptName,
                                deptID,
                                CAST(eventTime AS DATE) AS rDate,
                                deviceName
                        FROM PubEvent 
                        WHERE 1=1";

                    var today = DateTime.Parse(startDate).ToString("yyyy-MM-dd");

                    if (shiftType == "day")
                    {
                        query +=
                            $" AND CAST(eventTime AS DATETIME) BETWEEN '{today} 05:00:00' AND '{today} 23:59:59'";
                    }
                    else if (shiftType == "night")
                    {
                        var yesterdayDate = DateTime
                            .Parse(startDate)
                            .AddDays(-1)
                            .ToString("yyyy-MM-dd");
                        query +=
                            $" AND CAST(eventTime AS DATETIME) BETWEEN '{yesterdayDate} 18:00:00' AND '{today} 11:59:59'";
                    }

                    query += " ORDER BY eventTime ASC";

                    var result = await connection.QueryAsync(query);

                    return Json(new { success = true, data = result });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> GetPersonShiftData(
            string dept,
            string personName,
            string date,
            string dateEnd
        )
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var query =
                        @"
                        SELECT DISTINCT name, shiftMent, personID, deptName, deptID AS deptCode, date
                        FROM emp_person_shift
                        WHERE name IS NOT NULL
                        AND LEN(personID) > 5
                        AND isActive = '1'";

                    if (dept != "all")
                    {
                        query += " AND deptName = @dept";
                    }
                    if (personName != "all")
                    {
                        query += " AND name LIKE @personName";
                    }
                    query += " ORDER BY date ASC";

                    var parameters = new { dept, personName = $"%{personName}%" };

                    var result = await connection.QueryAsync(query, parameters);

                    // แก้ไขการแปลงวันที่
                    var start = DateTime.ParseExact(
                        date,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture
                    );
                    var end = DateTime.ParseExact(
                        dateEnd,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture
                    );
                    end = end.AddDays(1);

                    var daterange = Enumerable
                        .Range(0, (end - start).Days)
                        .Select(i => start.AddDays(i));

                    var data = new List<dynamic>();
                    var personData = new Dictionary<string, Dictionary<string, dynamic>>();

                    // Group data by person
                    foreach (var row in result)
                    {
                        if (!personData.ContainsKey(row.personID))
                        {
                            personData[row.personID] = new Dictionary<string, dynamic>();
                        }
                        personData[row.personID][((DateTime)row.date).ToString("yyyy-MM-dd")] = row;
                    }

                    foreach (var currentDate in daterange)
                    {
                        var formattedDate = currentDate.ToString("yyyy-MM-dd");

                        foreach (var personID in personData.Keys)
                        {
                            var shifts = personData[personID];
                            var dateList = shifts
                                .Keys.Select(d =>
                                    DateTime.ParseExact(
                                        d,
                                        "yyyy-MM-dd",
                                        CultureInfo.InvariantCulture
                                    )
                                )
                                .ToList();

                            var nearestDate = FindNearestDate(formattedDate, dateList);

                            if (
                                nearestDate != null
                                && shifts.ContainsKey(nearestDate?.ToString("yyyy-MM-dd"))
                            )
                            {
                                var shift = shifts[nearestDate?.ToString("yyyy-MM-dd")];
                                data.Add(
                                    new
                                    {
                                        shift.name,
                                        shift.shiftMent,
                                        shift.personID,
                                        shift.deptName,
                                        deptCode = shift.deptCode,
                                        date = formattedDate,
                                    }
                                );
                            }
                        }
                    }

                    return Json(new { success = true, data });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private DateTime? FindNearestDate(string targetDateStr, List<DateTime> dates)
        {
            try
            {
                // แปลง string เป็น DateTime โดยระบุ format ชัดเจน
                var targetDate = DateTime.ParseExact(
                    targetDateStr,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture
                );

                if (!dates.Any())
                    return null;

                // เรียงลำดับวันที่
                dates = dates.OrderBy(d => d).ToList();

                // หาวันที่ใกล้เคียงที่สุด
                DateTime? nearestDate = null;
                foreach (var date in dates)
                {
                    if (date <= targetDate)
                    {
                        nearestDate = date;
                    }
                    else
                    {
                        break;
                    }
                }

                // ถ้าไม่พบวันที่ก่อนหน้า ใช้วันที่แรกในรายการ
                return nearestDate ?? dates.FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<List<emp_wrkplan>> GetWorkPlanData()
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var connection2 = new SqlConnection(_ces941ConnectionString))
            {
                try
                {
                    // 1. ดึงข้อมูลจาก MWorkPlan ใน CES941
                    var workplanQuery =
                        @"
                        SELECT 
                            WrkPlanID as workplan_id,
                            Year,
                            Month, 
                            Period,
                            WrkDate as work_date,
                            DayType as day_type,
                            ShiftCode as shift_code,
                            ChangeDate,
                            ChangeBy,
                            FlagDel,
                            OTFIX
                        FROM MWorkPlan";

                    var workplans = await connection2.QueryAsync<emp_wrkplan>(workplanQuery);

                    // บันทึก log จำนวนข้อมูลที่ได้รับ
                    _logger.LogInformation($"Retrieved {workplans.Count()} workplan records");

                    // 2. ดึงข้อมูลกะจาก emp_shift ใน DB หลัก
                    var shiftQuery =
                        @"
                        SELECT 
                            Id,                 
                            shift_code,
                            shift_name,
                            shift_group,
                            start_time,
                            end_time,
                            break_start_time,
                            break_end_time,
                            is_break2,
                            break2_start_time,
                            break2_end_time,
                            ot_start_time,
                            ot_end_time,
                            sort
                        FROM emp_shift
                        ORDER BY sort ASC";

                    var shifts = await connection.QueryAsync<emp_shift>(shiftQuery);

                    // บันทึก log จำนวนข้อมูลที่ได้รับ
                    _logger.LogInformation($"Retrieved {shifts.Count()} shift records");

                    // สร้าง Dictionary โดยเลือกเฉพาะ record ล่าสุดของแต่ละ shift_code
                    var shiftDict = shifts
                        .GroupBy(s => s.shift_code)
                        .ToDictionary(
                            g => g.Key,
                            g => g.OrderByDescending(s => s.Id).First(),
                            StringComparer.OrdinalIgnoreCase
                        );

                    // 3. รวมข้อมูลทั้ง 2 ส่วนเข้าด้วยกัน
                    var resultList = workplans.ToList();
                    foreach (var plan in resultList)
                    {
                        if (
                            !string.IsNullOrEmpty(plan.shift_code)
                            && shiftDict.TryGetValue(plan.shift_code, out var shift)
                        )
                        {
                            plan.shift_id = shift.Id;
                            plan.shift_name = shift.shift_name;
                            plan.shift_group = shift.shift_group;
                            plan.start_time = shift.start_time;
                            plan.end_time = shift.end_time;
                            plan.break_start_time = shift.break_start_time;
                            plan.break_end_time = shift.break_end_time;
                            plan.is_break2 = shift.is_break2;
                            plan.break2_start_time = shift.break2_start_time;
                            plan.break2_end_time = shift.break2_end_time;
                            plan.ot_start_time = shift.ot_start_time;
                            plan.ot_end_time = shift.ot_end_time;
                            plan.sort = shift.sort;
                        }
                    }

                    // Debug logging
                    if (resultList.Any())
                    {
                        _logger.LogInformation(
                            $"Found {resultList.Count} workplan records after merging"
                        );
                        var firstRecord = resultList.First();
                        _logger.LogInformation(
                            $@"Sample workplan record:
                            Workplan ID: {firstRecord.workplan_id},
                            Work Date: {firstRecord.work_date},
                            Shift Code: {firstRecord.shift_code},
                            Shift Group: {firstRecord.shift_group}"
                        );
                    }
                    else
                    {
                        _logger.LogWarning("No workplan records found after merging with shifts");
                    }

                    return resultList;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in GetWorkPlanData: {ex.Message}");
                    _logger.LogError($"Stack trace: {ex.StackTrace}");
                    throw;
                }
            }
        }

        private string DetermineTimeType(
            DateTime eventTime,
            string startTime,
            string endTime,
            string breakStartTime,
            string breakEndTime
        )
        {
            var eventTimeOnly = eventTime.TimeOfDay;
            var startTimeOnly = TimeSpan.Parse(startTime);
            var endTimeOnly = TimeSpan.Parse(endTime);
            var breakStartTimeOnly = TimeSpan.Parse(breakStartTime);
            var breakEndTimeOnly = TimeSpan.Parse(breakEndTime);

            var midPoint = startTimeOnly.Add(endTimeOnly.Subtract(startTimeOnly).Divide(2));

            if (eventTimeOnly < midPoint)
                return "IN (before midpoint)";
            else if (eventTimeOnly > midPoint)
                return "OUT (after midpoint)";
            else if (eventTimeOnly >= breakStartTimeOnly && eventTimeOnly <= breakEndTimeOnly)
                return "have half day";

            return eventTimeOnly <= midPoint ? "IN" : "OUT";
        }

        private bool CheckPersonIdEndsWith(List<dynamic> events, string suffix)
        {
            if (events == null)
                return false;

            foreach (var evt in events)
            {
                var personId = evt?.personID?.ToString();
                if (personId != null && personId.EndsWith(suffix))
                    return true;
            }
            return false;
        }

        [HttpGet]
        public async Task<IActionResult> getTypeComputer()
        {
            var assets = AssetType.GetAllTypes();
            return Json(assets);
        }

        // แก้ไขฟังก์ชัน FilterPersonEvents ให้รองรับ dynamic type
        private List<dynamic> FilterPersonEvents(List<dynamic> events, string personId)
        {
            if (events == null || string.IsNullOrEmpty(personId))
                return new List<dynamic>();

            var result = new List<dynamic>();
            foreach (var e in events)
            {
                string eventPersonId = e.personID?.ToString()?.Trim();
                string eventBasePersonId = e.basePersonID?.ToString()?.Trim();

                if (eventPersonId == personId || eventBasePersonId == personId)
                {
                    result.Add(e);
                }
            }
            return result;
        }

        private async Task<object> ProcessPersonData(
            dynamic person,
            List<emp_wrkplan> workPlans,
            Dictionary<string, dynamic> leaveDict,
            dynamic dayEvents,
            dynamic nightEvents,
            string currentDate,
            string shiftFilter,
            Dictionary<string, dynamic> resignationDict,
            Dictionary<string, dynamic> overtimeDict,
            bool showResigned = true,
            List<dynamic> cabinetResult = null,
            bool hideResigned = false,
            bool hideAbsent = false,
            bool showIncomplete = false,
            ILookup<string, dynamic> computersByOwner = null
        )
        {
            try
            {
                // ตรวจสอบ parameter ที่จำเป็น
                if (
                    person == null
                    || workPlans == null
                    || leaveDict == null
                    || string.IsNullOrEmpty(currentDate)
                )
                {
                    _logger.LogWarning("Required parameters are null in ProcessPersonData");
                    return null;
                }

                // 1. แปลง personId เป็น empno
                var personId = person?.personID?.ToString();
                if (string.IsNullOrEmpty(personId))
                {
                    _logger.LogWarning($"PersonID is null or empty for person: {person?.name}");
                    return null;
                }

                var empNo = personId.Replace("-1", "")?.Trim();
                string empNoStr = empNo ?? "";
                var computers = computersByOwner[empNoStr] ?? Enumerable.Empty<dynamic>();
                int computerCount = computers.Count();

                // ตรวจสอบสถานะการลาออกก่อน
                bool isResigned = false;
                DateTime? resignDate = null;

                if (!string.IsNullOrEmpty(empNo) && resignationDict.ContainsKey(empNo))
                {
                    dynamic resignInfo = resignationDict[empNo];
                    if (resignInfo != null)
                    {
                        isResigned = true;
                        resignDate = resignInfo.EmpResignDate;
                    }
                }

                // ถ้า hideResigned เป็น true และคนนี้ลาออกแล้ว ให้ข้ามไป
                if (hideResigned && isResigned)
                {
                    return null;
                }

                // 2. ตรวจสอบข้อมูลการลา
                var hasLeave =
                    !string.IsNullOrEmpty(empNo)
                    && leaveDict.Keys.Any(k => k?.ToString()?.Trim() == empNo);
                var matchingKey = hasLeave
                    ? leaveDict.Keys.FirstOrDefault(k => k?.ToString()?.Trim() == empNo)
                    : null;
                var leaveInfo = matchingKey != null ? leaveDict[matchingKey] : null;

                // 3. ดึงข้อมูลแผนการทำงาน
                var resultDataPlan = await GetDayType(
                    workPlans,
                    currentDate,
                    person?.shiftMent?.ToString() ?? ""
                );

                if (resultDataPlan == null)
                {
                    _logger.LogWarning(
                        $"No workplan found for person {personId} on date {currentDate}"
                    );
                    return null;
                }

                // 4. ตรวจสอบฟิลเตอร์กะ
                var shiftCode = resultDataPlan.shift_code ?? "";
                if (!ValidateShiftFilter(shiftCode, shiftFilter))
                {
                    return null;
                }

                // 5. รวบรวมข้อมูลการสแกน
                var scanEvents =
                    CollectScanEvents(shiftCode, dayEvents, nightEvents) ?? new List<dynamic>();
                var personEvents = FilterPersonEvents(scanEvents, personId);

                // 6. หาเวลาเข้า-ออก
                DateTime? firstTime = null;
                DateTime? lastTime = null;

                if (personEvents != null && personEvents.Count > 0)
                {
                    // แปลง List<dynamic> เป็น List<DateTime>
                    var eventTimes = new List<DateTime>();
                    foreach (var evt in personEvents)
                    {
                        if (evt?.eventTime != null)
                        {
                            eventTimes.Add(evt.eventTime);
                        }
                    }

                    if (eventTimes.Any())
                    {
                        var workDate = DateTime.Parse(currentDate);
                        var breakStartTime = TimeSpan.Parse(
                            resultDataPlan?.break_start_time ?? "12:00"
                        );
                        var breakEndTime = TimeSpan.Parse(
                            resultDataPlan?.break_end_time ?? "13:00"
                        );

                        // แยกเวลาเป็น 2 ช่วง โดยคำนึงถึงวันที่และกะการทำงาน
                        List<DateTime> morningEvents = new List<DateTime>();
                        List<DateTime> eveningEvents = new List<DateTime>();

                        if (shiftCode == "NT1" || shiftCode == "N01")
                        {
                            // สำหรับกะดึก: เวลาเข้า >= 18:00 ของวันก่อนหน้า, เวลาออก <= 12:00 ของวันปัจจุบัน
                            var previousDate = workDate.AddDays(-1);

                            morningEvents = eventTimes
                                .Where(t =>
                                {
                                    try
                                    {
                                        // เวลาเข้างาน: เวลา >= 18:00 ของวันก่อนหน้า หรือ เวลาของวันปัจจุบัน <= เวลาพักเช้า
                                        return (t.Date == previousDate.Date && t.Hour >= 18)
                                            || (
                                                t.Date == workDate.Date
                                                && t.TimeOfDay <= breakStartTime
                                            );
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(
                                            $"Error in night shift morningEvents filter: {ex.Message}"
                                        );
                                        return false;
                                    }
                                })
                                .ToList();

                            eveningEvents = eventTimes
                                .Where(t =>
                                {
                                    try
                                    {
                                        // เวลาออกงาน: เวลา <= 12:00 ของวันปัจจุบัน หรือ เวลาหลังพักกลางวัน
                                        return (t.Date == workDate.Date && t.Hour <= 12)
                                            || (
                                                t.Date == workDate.Date
                                                && t.TimeOfDay > breakEndTime
                                            );
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(
                                            $"Error in night shift eveningEvents filter: {ex.Message}"
                                        );
                                        return false;
                                    }
                                })
                                .ToList();
                        }
                        else
                        {
                            // สำหรับกะปกติ: ใช้ logic เดิม
                            morningEvents = eventTimes
                                .Where(t =>
                                {
                                    try
                                    {
                                        return t.Date == workDate.Date
                                            && t.TimeOfDay <= breakStartTime;
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(
                                            $"Error in day shift morningEvents filter: {ex.Message}"
                                        );
                                        return false;
                                    }
                                })
                                .ToList();

                            eveningEvents = eventTimes
                                .Where(t =>
                                {
                                    try
                                    {
                                        return t.Date == workDate.Date
                                            && t.TimeOfDay > breakEndTime;
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(
                                            $"Error in day shift eveningEvents filter: {ex.Message}"
                                        );
                                        return false;
                                    }
                                })
                                .ToList();
                        }

                        // หาเวลาเข้างาน
                        if (morningEvents.Any())
                        {
                            firstTime = morningEvents.Min();
                        }

                        // หาเวลาออกงาน
                        if (eveningEvents.Any())
                        {
                            lastTime = eveningEvents.Max();
                        }

                        // ถ้าไม่มีเวลาเข้า หรือไม่มีเวลาออกให้หาว่ามี event กี่อัน
                        if (firstTime == null || lastTime == null)
                        {
                            if (personEvents.Count >= 1)
                            {
                                // แปลง dynamic เป็น List<DateTime> ก่อนใช้ LINQ
                                var eventTimeList = new List<DateTime>();
                                foreach (var evt in personEvents)
                                {
                                    if (evt?.eventTime != null)
                                    {
                                        eventTimeList.Add(evt.eventTime);
                                    }
                                }

                                if (eventTimeList.Any())
                                {
                                    var minTime = eventTimeList.Min();
                                    var maxTime = eventTimeList.Max();

                                    if (personEvents.Count == 1)
                                    {
                                        // กรณีมี event เดียว - ตัดสินใจว่าเป็น firstTime หรือ lastTime
                                        var singleEventTime = eventTimeList.First();

                                        if (shiftCode == "NT1" || shiftCode == "N01")
                                        {
                                            // สำหรับกะดึก
                                            var shiftStartTime = TimeSpan.Parse(
                                                resultDataPlan?.start_time ?? "20:00"
                                            );
                                            var shiftEndTime = TimeSpan.Parse(
                                                resultDataPlan?.end_time ?? "05:00"
                                            );

                                            // ถ้าเวลา event ใกล้กับเวลาเข้างาน (18:00-22:00) ให้เป็น firstTime
                                            if (
                                                singleEventTime.Hour >= 18
                                                || singleEventTime.Hour <= 2
                                            )
                                            {
                                                firstTime = firstTime ?? singleEventTime;
                                            }
                                            // ถ้าเวลา event ใกล้กับเวลาออกงาน (03:00-07:00) ให้เป็น lastTime
                                            else if (
                                                singleEventTime.Hour >= 3
                                                && singleEventTime.Hour <= 7
                                            )
                                            {
                                                lastTime = lastTime ?? singleEventTime;
                                            }
                                            else
                                            {
                                                // กรณีไม่แน่ใจ ให้เป็น firstTime
                                                firstTime = firstTime ?? singleEventTime;
                                            }
                                        }
                                        else
                                        {
                                            // สำหรับกะปกติ
                                            var shiftStartTime = TimeSpan.Parse(
                                                resultDataPlan?.start_time ?? "08:00"
                                            );
                                            var shiftEndTime = TimeSpan.Parse(
                                                resultDataPlan?.end_time ?? "17:00"
                                            );
                                            var midPoint = shiftStartTime.Add(
                                                shiftEndTime.Subtract(shiftStartTime).Divide(2)
                                            );

                                            // ถ้าเวลา event ก่อนจุดกึ่งกลางให้เป็น firstTime
                                            if (singleEventTime.TimeOfDay <= midPoint)
                                            {
                                                firstTime = firstTime ?? singleEventTime;
                                            }
                                            else
                                            {
                                                lastTime = lastTime ?? singleEventTime;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // กรณีมี event หลายอัน
                                        var timeDiff = maxTime - minTime;
                                        if (timeDiff.TotalMinutes >= 30)
                                        {
                                            firstTime = firstTime ?? minTime;
                                            lastTime = lastTime ?? maxTime;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // 7. กำหนดสถานะ
                var status = DetermineStatus(firstTime, lastTime, hasLeave);

                // ตรวจสอบข้อมูลตู้เก็บของ
                dynamic cabinet = null;
                if (cabinetResult != null)
                {
                    cabinet = cabinetResult.FirstOrDefault(c => c.Owner == personId);
                }

                // ถ้าไม่ต้องการแสดงข้อมูลคนที่ลาออกแล้ว และคนนี้ลาออกแล้ว ให้ข้ามไป
                if (!showResigned && isResigned)
                {
                    return null;
                }

                // 9. ดึงข้อมูลโอทีจาก Dictionary ที่เตรียมไว้
                object overtimeInfo = null;
                if (!string.IsNullOrEmpty(empNo) && overtimeDict.ContainsKey(empNo))
                {
                    overtimeInfo = overtimeDict[empNo];
                }
                else
                {
                    overtimeInfo = new { hasOvertime = false };
                }

                if (hideAbsent && status == "Absent")
                {
                    return null;
                }

                // ถ้า showIncomplete เป็น true ให้แสดงข้อมูลที่สแกนไม่ครบ เช่นมี firsttime แต่ไม่มี lasttime หรือ มี lasttime แต่ไม่มี firsttime
                if (showIncomplete)
                {
                    if (firstTime != null && lastTime != null)
                    {
                        return null;
                    }
                }

                // 10. สร้างและส่งคืนข้อมูล
                return new
                {
                    personID = personId,
                    personName = person?.name ?? "Unknown",
                    deptName = person?.deptName ?? "Unknown",
                    rDate = currentDate,
                    shift_group = resultDataPlan?.workplan_id ?? "Unknown",
                    shift = resultDataPlan?.shift_code ?? "Unknown",
                    dayType = resultDataPlan?.day_type ?? "Unknown",
                    firstTime = firstTime,
                    lastTime = lastTime,
                    status = status,
                    leaveInfo = hasLeave && leaveInfo != null
                        ? new
                        {
                            empNo = leaveInfo?.EmpNo,
                            attenCode = leaveInfo?.AttenCode,
                            wrkDate = leaveInfo?.WrkDate,
                            dteTmeIn = leaveInfo?.DteTmeIn,
                            dteTmeOut = leaveInfo?.DteTmeOut,
                            leaveQty = leaveInfo?.LeaveQty,
                            leaveType = leaveInfo?.LeaveType,
                            approvalStatus = leaveInfo?.ApprStatus,
                            apprDate = leaveInfo?.ApprDate,
                            secCode = leaveInfo?.SecCode,
                            costCenter = leaveInfo?.CostCenter,
                            bossId = leaveInfo?.BossId,
                            leaveReqNo = leaveInfo?.LeaveReqNo,
                            leaveReqItem = leaveInfo?.LeaveReqItem,
                            changeDate = leaveInfo?.ChangeDate,
                            changeBy = leaveInfo?.ChangeBy,
                            flagDel = leaveInfo?.FlagDel,
                            remark = leaveInfo?.Remark,
                            attachment = leaveInfo?.Attachment,
                            tmpSeccode = leaveInfo?.TmpSeccode,
                            tmpCostCenter = leaveInfo?.TmpCostCenter,
                            tmpBossId = leaveInfo?.TmpBossId,
                            bossName = leaveInfo?.BossName,
                            caseId = leaveInfo?.CaseId,
                            createDate = leaveInfo?.CreateDate,
                            createBy = leaveInfo?.CreateBy,
                            actionType = leaveInfo?.ActionType,
                            actionReason = leaveInfo?.ActionReason,
                            nextAppr = leaveInfo?.NextAppr,
                            leaveQtyReq = leaveInfo?.LeaveQtyReq,
                            leaveReqUnit = leaveInfo?.LeaveReqUnit,
                            ceInd = leaveInfo?.CEInd,
                            summaryUpdate = leaveInfo?.SummaryUpdate,
                            newLeaveInd = leaveInfo?.NewLeaveInd,
                        }
                        : null,
                    fingerprintUsed = CheckPersonIdEndsWith(personEvents, "-2"),
                    cardUsed = CheckPersonIdEndsWith(personEvents, "-1"),
                    scanData = personEvents ?? new List<dynamic>(),
                    workHours = hasLeave ? 0 : CalculateWorkHours(firstTime, lastTime),
                    shiftInfo = new
                    {
                        startTime = resultDataPlan?.start_time ?? "",
                        endTime = resultDataPlan?.end_time ?? "",
                        breakStartTime = resultDataPlan?.break_start_time ?? "",
                        breakEndTime = resultDataPlan?.break_end_time ?? "",
                    },
                    isResigned = isResigned,
                    resignDate = resignDate,
                    overtimeInfo = overtimeInfo,
                    cabinetInfo = cabinet,
                    computerCount = computerCount,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ProcessPersonData: {ex.Message}");
                return null;
            }
        }

        private bool ValidateShiftFilter(string shiftCode, string shiftFilter)
        {
            if (shiftFilter == "all")
                return true;

            if (shiftCode == shiftFilter)
                return true;

            return false;
        }

        private List<dynamic> CollectScanEvents(
            string shiftCode,
            dynamic dayEvents,
            dynamic nightEvents
        )
        {
            var scanEvents = new List<dynamic>();
            var isNightShift = shiftCode == "N01" || shiftCode == "NT1";

            if (isNightShift)
            {
                if (nightEvents?.data != null)
                {
                    foreach (var evt in nightEvents.data)
                    {
                        scanEvents.Add(evt);
                    }
                }
            }
            else
            {
                if (dayEvents?.data != null)
                {
                    foreach (var evt in dayEvents.data)
                    {
                        scanEvents.Add(evt);
                    }
                }
            }

            return scanEvents;
        }

        private string DetermineStatus(DateTime? firstTime, DateTime? lastTime, bool hasLeave)
        {
            if (hasLeave)
                return "Leave";
            if (firstTime == null && lastTime == null)
                return "Absent";
            if (firstTime == lastTime)
                return "Incomplete";
            return "Present";
        }

        private async Task<(dynamic dayEvents, dynamic nightEvents)> GetScanEvents(string date)
        {
            try
            {
                // ดึงข้อมูลการสแกนกะกลางวัน
                var dayEventsResult = await GetEventData(date, "HR Attendance", null, "day");
                var dayEvents = ((JsonResult)dayEventsResult).Value;

                // ดึงข้อมูลการสแกนกะกลางคืน
                var nightEventsResult = await GetEventData(date, "HR Attendance", null, "night");
                var nightEvents = ((JsonResult)nightEventsResult).Value;

                return (dayEvents, nightEvents);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting scan events: {ex.Message}");
                return (null, null);
            }
        }

        // เมธอดใหม่สำหรับดึงข้อมูลการลาออกทั้งหมดในครั้งเดียว
        private async Task LoadAllResignationInfo(IEnumerable<string> empNos)
        {
            if (empNos == null || !empNos.Any())
                return;

            // ตรวจสอบว่า cache หมดอายุหรือยัง
            if (_resignationCacheExpiry >= DateTime.Now && _resignationCache.Count > 0)
            {
                return; // ยังไม่หมดอายุ ไม่ต้องโหลดใหม่
            }

            try
            {
                using (var connection = new SqlConnection(_ces941ConnectionString))
                {
                    // ใช้ IN clause แทนการ query ทีละคน
                    var distinctEmpNos = empNos
                        .Where(e => !string.IsNullOrEmpty(e))
                        .Distinct()
                        .ToList();

                    if (!distinctEmpNos.Any())
                        return;

                    // สร้าง parameter สำหรับ IN clause
                    var parameters = new DynamicParameters();

                    // สร้าง query แบบ dynamic
                    var sqlBuilder = new StringBuilder();
                    sqlBuilder.Append(
                        "SELECT EmpNo, EmpResignDate FROM MEmpBasic WHERE EmpResignDate IS NOT NULL AND EmpNo IN ("
                    );

                    for (int i = 0; i < distinctEmpNos.Count; i++)
                    {
                        var paramName = $"@EmpNo{i}";
                        sqlBuilder.Append(i > 0 ? ", " : "").Append(paramName);
                        parameters.Add(paramName, distinctEmpNos[i]);
                    }

                    sqlBuilder.Append(")");

                    var resignInfos = await connection.QueryAsync<dynamic>(
                        sqlBuilder.ToString(),
                        parameters
                    );

                    // เก็บข้อมูลลงใน cache
                    lock (_resignationCacheLock)
                    {
                        _resignationCache.Clear();
                        foreach (var info in resignInfos)
                        {
                            _resignationCache[info.EmpNo] = info;
                        }
                        _resignationCacheExpiry = DateTime.Now.AddHours(1);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading all resignation info: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> inOut()
        {
            await LoadPermissions("Attendance", "inOut");

            try
            {
                // ดึงข้อมูลพนักงาน
                var userData = JsonConvert.DeserializeObject<dynamic>(
                    HttpContext.Session.GetString("UserData")
                );
                ViewBag.UserDepartment = userData?.Department ?? "";
                ViewBag.EmployeeId = HttpContext.Session.GetString("EmployeeID");
                ViewBag.EmployeeName = userData?.Name ?? "";
                ViewBag.CanEdit = CheckPermission("Edit");

                // ดึงรายชื่อแผนกทั้งหมด
                using (var connection = new SqlConnection(_ces941ConnectionString))
                {
                    var deptQuery =
                        "SELECT DISTINCT SecCode, SecDsc FROM MSecCode WHERE Language = 'TH'";
                    var departments = await connection.QueryAsync(deptQuery);
                    ViewBag.Departments = departments;
                }

                return View("~/Views/Attendance/inOut.cshtml");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in inOut: {ex.Message}");
                return View("~/Views/Attendance/inOut.cshtml");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllEmployeeBreakEvents(
            string date,
            string startDate,
            string endDate,
            string department = null
        )
        {
            try
            {
                // ตรวจสอบวันที่
                if (string.IsNullOrEmpty(date))
                {
                    date = DateTime.Now.ToString("yyyy-MM-dd");
                }

                // กำหนดค่าเริ่มต้นสำหรับ startDate และ endDate ถ้าไม่ได้ระบุ
                if (string.IsNullOrEmpty(startDate))
                {
                    startDate = date;
                }
                if (string.IsNullOrEmpty(endDate))
                {
                    endDate = startDate;
                }

                // สร้างช่วงวันที่ต้องการ
                var dateRange = GetDateRange(startDate, endDate);

                // สร้างรายการผลลัพธ์
                var result = new List<object>();

                // ดึงข้อมูลพนักงานและกะการทำงานเพียงครั้งเดียว
                var personShiftResult = await GetPersonShiftData(
                    department ?? "all",
                    "all",
                    startDate,
                    endDate
                );

                // ดึงข้อมูลแผนการทำงานเพียงครั้งเดียว
                var workPlans = await GetWorkPlanData();

                // แปลงข้อมูลพนักงานและกะการทำงาน
                var personShifts =
                    ((personShiftResult as JsonResult)?.Value as dynamic)?.data
                    ?? new List<dynamic>();

                // สร้าง Dictionary เพื่อเก็บข้อมูลการสแกนทั้งหมดในทุกวัน
                var allScanEvents =
                    new Dictionary<string, (dynamic dayEvents, dynamic nightEvents)>();

                // ดึงข้อมูลการสแกนเข้าออกทั้งหมดในครั้งเดียว
                foreach (var currentDate in dateRange)
                {
                    var currentDateStr = currentDate.ToString("yyyy-MM-dd");

                    // ดึงข้อมูลการสแกนกะกลางวัน
                    var dayEventsResult = await GetEventData(
                        currentDateStr,
                        "HR Attendance",
                        null,
                        "day"
                    );
                    var dayEvents = ((JsonResult)dayEventsResult).Value;

                    // ดึงข้อมูลการสแกนกะกลางคืน
                    var nightEventsResult = await GetEventData(
                        currentDateStr,
                        "HR Attendance",
                        null,
                        "night"
                    );
                    var nightEvents = ((JsonResult)nightEventsResult).Value;

                    allScanEvents[currentDateStr] = (dayEvents, nightEvents);
                }

                // สร้าง Dictionary เพื่อเก็บข้อมูลแผนการทำงานตามวันที่และรหัสกะ
                var workPlanCache = new Dictionary<string, emp_wrkplan>();

                // ประมวลผลข้อมูลตามช่วงวันที่
                foreach (var currentDate in dateRange)
                {
                    var currentDateStr = currentDate.ToString("yyyy-MM-dd");

                    // filter personShifts by currentDateStr
                    var filteredPersonShifts = new List<dynamic>();
                    foreach (var p in personShifts)
                    {
                        string personDate = p.date?.ToString();
                        if (personDate == currentDateStr)
                        {
                            filteredPersonShifts.Add(p);
                        }
                    }

                    // ดึงข้อมูลการสแกนจาก cache
                    var scanEvents = allScanEvents[currentDateStr];

                    // ประมวลผลข้อมูลพนักงานแต่ละคนในวันปัจจุบัน
                    foreach (var person in filteredPersonShifts)
                    {
                        // ดึงข้อมูลพนักงาน
                        var personId = person.personID?.ToString();
                        var shiftment = person.shiftMent?.ToString();

                        // แปลงรหัสกะ
                        if (shiftment == "OFFICE2")
                            shiftment = "OF02";
                        else if (shiftment == "OFFICE")
                            shiftment = "OF01";
                        else if (shiftment == "DAY")
                            shiftment = "D01";
                        else if (shiftment == "NIGHT")
                            shiftment = "N01";

                        // ใช้ cache สำหรับแผนการทำงาน
                        string cacheKey = $"{currentDateStr}_{shiftment}";
                        emp_wrkplan resultDataPlan;

                        if (workPlanCache.ContainsKey(cacheKey))
                        {
                            resultDataPlan = workPlanCache[cacheKey];
                        }
                        else
                        {
                            resultDataPlan = await GetDayType(workPlans, currentDateStr, shiftment);
                            if (resultDataPlan != null)
                            {
                                workPlanCache[cacheKey] = resultDataPlan;
                            }
                        }

                        // ตรวจสอบว่ามีข้อมูลแผนการทำงานหรือไม่
                        if (resultDataPlan == null)
                        {
                            continue;
                        }

                        shiftment = resultDataPlan?.shift_code;

                        // กรองข้อมูลการสแกนเข้าออกของพนักงาน
                        List<dynamic> personEvents = null;

                        // ตรวจสอบกะการทำงาน
                        var isNightShift = shiftment == "N01" || shiftment == "NT1";

                        var eventsData = isNightShift
                            ? scanEvents.nightEvents?.data
                            : scanEvents.dayEvents?.data;
                        if (eventsData != null)
                        {
                            personEvents = FilterPersonEvents(eventsData, personId);
                        }

                        // ถ้าไม่มีข้อมูลการสแกน ให้ข้ามไป
                        if (personEvents == null || personEvents.Count == 0)
                        {
                            continue;
                        }

                        // จัดรูปแบบข้อมูลการสแกนเข้าออก
                        var formattedEvents = FormatEventsWithBreaks(personEvents, resultDataPlan);

                        // คำนวณสถิติการพัก
                        var breakStats = CalculateBreakStatsWithScheduled(
                            formattedEvents,
                            resultDataPlan
                        );

                        // สร้างข้อมูลผลลัพธ์
                        var employeeData = new
                        {
                            employeeId = personId,
                            employeeName = person.name?.ToString(),
                            department = person.deptName?.ToString(),
                            shift = shiftment,
                            shiftName = resultDataPlan?.shift_name,
                            date = currentDateStr,
                            firstCheckIn = formattedEvents.Count > 0
                                ? formattedEvents[0].time
                                : null,
                            lastCheckOut = formattedEvents.Count > 0
                                ? formattedEvents[formattedEvents.Count - 1].time
                                : null,
                            currentStatus = formattedEvents.Count > 0
                                ? (formattedEvents.Count % 2 == 0 ? "OUT" : "IN")
                                : null,
                            breakStats,
                            events = formattedEvents,
                        };

                        result.Add(employeeData);
                    }
                }

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAllEmployeeBreakEvents: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // เพิ่มเมธอดสำหรับจัดรูปแบบข้อมูลเวลาพัก
        private List<dynamic> FormatEventsWithBreaks(List<dynamic> events, emp_wrkplan shiftPlan)
        {
            try
            {
                if (events == null || events.Count == 0 || shiftPlan == null)
                    return new List<dynamic>();

                var result = new List<dynamic>();

                // สร้าง List ใหม่เพื่อเก็บข้อมูล events
                var eventList = new List<dynamic>();
                foreach (var e in events)
                {
                    eventList.Add(e);
                }

                // จัดเรียงข้อมูลตามเวลาด้วยการใช้ bubble sort แทนการใช้ .Sort() กับ lambda
                for (int i = 0; i < eventList.Count - 1; i++)
                {
                    for (int j = 0; j < eventList.Count - i - 1; j++)
                    {
                        DateTime timeA = (DateTime)eventList[j].eventTime;
                        DateTime timeB = (DateTime)eventList[j + 1].eventTime;

                        if (timeA > timeB)
                        {
                            // สลับตำแหน่ง
                            var temp = eventList[j];
                            eventList[j] = eventList[j + 1];
                            eventList[j + 1] = temp;
                        }
                    }
                }

                // กรองเหตุการณ์ที่มีเวลาใกล้เคียงกันออก โดยเว้นระยะห่างอย่างน้อย 3 นาที
                eventList = FilterCloseTimeEvents(eventList, 3);

                // เพิ่มข้อมูลเวลาพัก 10 นาที (ช่วงเวลา 10:00-10:10)
                var workDate = ((DateTime)eventList[0].eventTime).Date;
                var morningBreakStart = workDate.Add(TimeSpan.Parse("10:00"));
                var morningBreakEnd = workDate.Add(TimeSpan.Parse("10:10"));

                // เพิ่มข้อมูลเวลาพักกลางวัน (จากกะ)
                TimeSpan? lunchStart = null;
                TimeSpan? lunchEnd = null;

                if (
                    !string.IsNullOrEmpty(shiftPlan.break_start_time)
                    && !string.IsNullOrEmpty(shiftPlan.break_end_time)
                )
                {
                    lunchStart = TimeSpan.Parse(shiftPlan.break_start_time);
                    lunchEnd = TimeSpan.Parse(shiftPlan.break_end_time);
                }

                // แปลงข้อมูล event และเพิ่มข้อมูลเวลาพัก
                foreach (var evt in eventList)
                {
                    string eventType = DetermineEventTypeFromOriginal(
                        evt.originalEventCode?.ToString(),
                        evt.originalDeviceType?.ToString()
                    );

                    result.Add(
                        new
                        {
                            rowAutoID = evt.rowAutoID,
                            time = evt.eventTime,
                            type = eventType,
                            deviceName = evt.deviceName,
                            isMorningBreak = IsTimeInRange(
                                (DateTime)evt.eventTime,
                                morningBreakStart,
                                morningBreakEnd
                            ),
                            isLunchBreak = lunchStart.HasValue
                                && lunchEnd.HasValue
                                && IsTimeInRange(
                                    (DateTime)evt.eventTime,
                                    workDate.Add(lunchStart.Value),
                                    workDate.Add(lunchEnd.Value)
                                ),
                        }
                    );
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in FormatEventsWithBreaks: {ex.Message}");
                return new List<dynamic>();
            }
        }

        // กรองเหตุการณ์ที่มีเวลาใกล้เคียงกันออก โดยเว้นระยะห่างตามที่กำหนด
        private List<dynamic> FilterCloseTimeEvents(
            List<dynamic> events,
            int minimumMinutesBetweenEvents
        )
        {
            if (events == null || events.Count <= 1)
                return events;

            var filteredEvents = new List<dynamic>();
            DateTime lastIncludedTime = DateTime.MinValue;

            foreach (var evt in events)
            {
                DateTime currentTime = (DateTime)evt.eventTime;

                // ถ้าเป็นเหตุการณ์แรก หรือ มีระยะห่างจากเหตุการณ์ก่อนหน้ามากกว่าที่กำหนด
                if (
                    lastIncludedTime == DateTime.MinValue
                    || (currentTime - lastIncludedTime).TotalMinutes >= minimumMinutesBetweenEvents
                )
                {
                    filteredEvents.Add(evt);
                    lastIncludedTime = currentTime;
                }
                // ถ้าไม่เข้าเงื่อนไข จะข้ามเหตุการณ์นี้ไป (ไม่รวมในผลลัพธ์)
            }

            return filteredEvents;
        }

        private string DetermineEventTypeFromOriginal(string eventCode, string deviceType)
        {
            if (string.IsNullOrEmpty(eventCode) || string.IsNullOrEmpty(deviceType))
                return "UNKNOWN";

            // เช็คเงื่อนไขสำหรับ IN
            if (
                (
                    eventCode == "0001"
                    && new[]
                    {
                        "GCU",
                        "RAC2400",
                        "HDE200",
                        "NCU",
                        "RAC2400N",
                        "RAC2000EL",
                        "RAC852PMFV",
                        "RAC960PMF",
                    }.Contains(deviceType)
                )
                || (eventCode == "0014" && deviceType == "RAC960")
                || (
                    (eventCode == "0007" || eventCode == "7" || eventCode == "1")
                    && (deviceType == "NCU" || deviceType == "RAC2000EL")
                )
                || ((eventCode == "01" || eventCode == "21") && deviceType == "UNI")
            )
            {
                return "IN";
            }

            // เช็คเงื่อนไขสำหรับ OUT
            if (
                (
                    eventCode == "0000"
                    && new[]
                    {
                        "GCU",
                        "RAC2400",
                        "HDE200",
                        "NCU",
                        "RAC2400N",
                        "RAC2000EL",
                        "RAC960PMF",
                    }.Contains(deviceType)
                )
                || (
                    eventCode == "0009"
                    && new[]
                    {
                        "GCU",
                        "RAC2400",
                        "HDE200",
                        "RAC851",
                        "RAC2400N",
                        "RAC852PMFV",
                        "HDE200",
                        "RAC960PMF",
                    }.Contains(deviceType)
                )
                || (eventCode == "003D" && (deviceType == "RAC940" || deviceType == "RAC960"))
                || ((eventCode == "02" || eventCode == "22") && deviceType == "UNI")
            )
            {
                return "OUT";
            }

            return "UNKNOWN";
        }

        // เช็คว่าเวลาอยู่ในช่วงที่กำหนดหรือไม่
        private bool IsTimeInRange(DateTime time, string startTimeStr, string endTimeStr)
        {
            try
            {
                if (string.IsNullOrEmpty(startTimeStr) || string.IsNullOrEmpty(endTimeStr))
                    return false;

                // แปลงเวลาเริ่มต้นและสิ้นสุดเป็น DateTime
                var datePart = time.Date;
                var hourStartParts = startTimeStr.Split(':');
                var hourEndParts = endTimeStr.Split(':');

                if (hourStartParts.Length < 2 || hourEndParts.Length < 2)
                    return false;

                int startHour = int.Parse(hourStartParts[0]);
                int startMinute = int.Parse(hourStartParts[1]);
                int endHour = int.Parse(hourEndParts[0]);
                int endMinute = int.Parse(hourEndParts[1]);

                // สร้าง DateTime สำหรับเวลาเริ่มต้นและสิ้นสุด
                var startTime = datePart.AddHours(startHour).AddMinutes(startMinute);
                var endTime = datePart.AddHours(endHour).AddMinutes(endMinute);

                // กรณีข้ามวัน เช่น 22:00 - 06:00
                if (endTime < startTime)
                {
                    endTime = endTime.AddDays(1);
                }

                // เช็คว่าเวลาอยู่ในช่วงหรือไม่
                return time >= startTime && time <= endTime;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in IsTimeInRange: {ex.Message}");
                return false;
            }
        }

        // เช็คว่าเวลาอยู่ในช่วงที่กำหนดหรือไม่ (รับพารามิเตอร์เป็น DateTime)
        private bool IsTimeInRange(DateTime time, DateTime startTime, DateTime endTime)
        {
            try
            {
                // กรณีข้ามวัน เช่น 22:00 - 06:00
                if (endTime < startTime)
                {
                    endTime = endTime.AddDays(1);
                }

                // เช็คว่าเวลาอยู่ในช่วงหรือไม่
                return time >= startTime && time <= endTime;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in IsTimeInRange (DateTime): {ex.Message}");
                return false;
            }
        }

        private object CalculateBreakStatsWithScheduled(List<dynamic> events, emp_wrkplan shiftPlan)
        {
            try
            {
                if (events == null || events.Count == 0 || shiftPlan == null)
                    return new
                    {
                        totalBreakTime = 0,
                        breakCount = 0,
                        longestBreak = 0,
                        averageBreak = 0,
                    };

                var eventList = events.OrderBy(e => e.eventTime).ToList();

                // กรองเหตุการณ์ที่มีเวลาใกล้เคียงกันออก โดยเว้นระยะห่างอย่างน้อย 3 นาที
                eventList = FilterCloseTimeEvents(eventList, 3);

                var breaks = new List<int>();
                DateTime? lastOutTime = null;
                var totalBreakTime = 0;
                var breakCount = 0;

                // ตรวจสอบเวลาพัก 10 นาที (ช่วงเวลา 10:00-10:10)
                var workDate = ((DateTime)eventList[0].eventTime).Date;
                var morningBreakStart = workDate.Add(TimeSpan.Parse("10:00"));
                var morningBreakEnd = workDate.Add(TimeSpan.Parse("10:10"));
                var morningBreakDuration = 10; // นาที

                // ตรวจสอบเวลาพักกลางวัน (จากกะ)
                int lunchBreakDuration = 0;
                if (
                    !string.IsNullOrEmpty(shiftPlan.break_start_time)
                    && !string.IsNullOrEmpty(shiftPlan.break_end_time)
                )
                {
                    var lunchStart = TimeSpan.Parse(shiftPlan.break_start_time);
                    var lunchEnd = TimeSpan.Parse(shiftPlan.break_end_time);
                    lunchBreakDuration = (int)(lunchEnd - lunchStart).TotalMinutes;
                }

                // ตรวจสอบ event type และคำนวณเวลาพัก
                foreach (var evt in eventList)
                {
                    string eventType = DetermineEventTypeFromOriginal(
                        evt.originalEventCode?.ToString(),
                        evt.originalDeviceType?.ToString()
                    );

                    // ตรวจสอบว่าอยู่ในช่วงเวลาพัก 10 นาทีหรือไม่
                    bool isMorningBreak = IsTimeInRange(
                        (DateTime)evt.eventTime,
                        morningBreakStart,
                        morningBreakEnd
                    );

                    if (eventType == "OUT")
                    {
                        if (lastOutTime == null)
                        {
                            lastOutTime = evt.eventTime;
                        }
                    }
                    else if (eventType == "IN" && lastOutTime.HasValue)
                    {
                        var breakTime = (int)
                            ((DateTime)evt.eventTime - lastOutTime.Value).TotalMinutes;
                        if (breakTime > 0 && breakTime < 480) // ไม่นับถ้าเวลาพักเกิน 8 ชั่วโมง
                        {
                            breaks.Add(breakTime);
                            totalBreakTime += breakTime;
                            breakCount++;
                        }
                        lastOutTime = null;
                    }
                }

                return new
                {
                    totalBreakTime = totalBreakTime,
                    scheduledBreaks = new
                    {
                        morningBreak = morningBreakDuration,
                        lunchBreak = lunchBreakDuration,
                    },
                    breakCount = breakCount,
                    longestBreak = breaks.Count > 0 ? breaks.Max() : 0,
                    averageBreak = breaks.Count > 0 ? (int)breaks.Average() : 0,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calculating break stats: {ex.Message}");
                return new
                {
                    totalBreakTime = 0,
                    breakCount = 0,
                    longestBreak = 0,
                    averageBreak = 0,
                };
            }
        }

        // เพิ่ม Action Method สำหรับดึงข้อมูลแผนกทั้งหมด
        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            try
            {
                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    await connection.OpenAsync();

                    // ดึงข้อมูลแผนกทั้งหมดจากฐานข้อมูล
                    var query =
                        @"
                        SELECT DISTINCT
                            deptID,
                            name 
                        FROM Dept
                        WHERE deptID IS NOT NULL AND name IS NOT NULL
                        ORDER BY name";

                    var departments = await connection.QueryAsync(query);

                    return Json(departments);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetDepartments: {ex.Message}");
                return Json(new List<object>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> AdjustBreakTime(string id, string adjustedTime)
        {
            try
            {
                // ดึงข้อมูลพนักงานจากฐานข้อมูล
                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    await connection.OpenAsync();

                    var query =
                        "update pubevent set eventtime = @adjustedTime where rowAutoID = @id";
                    var result = await connection.ExecuteAsync(
                        query,
                        new { adjustedTime = adjustedTime, id = id }
                    );

                    if (result > 0)
                    {
                        return Json(
                            new { success = true, message = "ปรับปรุงเวลาพักเรียบร้อยแล้ว" }
                        );
                    }
                    else
                    {
                        return Json(
                            new { success = false, message = "ไม่พบข้อมูลที่ต้องการปรับปรุง" }
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in AdjustBreakTime: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // เพิ่ม Action Method สำหรับการเพิ่มเวลาเข้า-ออกด้วยตนเอง
        [HttpPost]
        public async Task<IActionResult> AddManualTimeRecord(
            string employeeId,
            string date,
            string timeType,
            string timeValue,
            string reason
        )
        {
            try
            {
                // ตรวจสอบสิทธิ์การใช้งาน - เฉพาะ Adisak S. เท่านั้นที่สามารถเพิ่มเวลาได้
                var employeeName = HttpContext.Session.GetString("EmployeeName");
                if (employeeName != "Adisak S.")
                {
                    return Json(
                        new { success = false, message = "คุณไม่มีสิทธิ์ในการเพิ่มเวลาด้วยตนเอง" }
                    );
                }

                // แปลงข้อมูลวันที่และเวลา
                if (!DateTime.TryParse(date, out DateTime recordDate))
                {
                    return Json(new { success = false, message = "รูปแบบวันที่ไม่ถูกต้อง" });
                }

                // สร้างวันเวลาที่สมบูรณ์
                var timeParts = timeValue.Split(':');
                if (timeParts.Length < 2)
                {
                    return Json(new { success = false, message = "รูปแบบเวลาไม่ถูกต้อง" });
                }

                int hours = int.Parse(timeParts[0]);
                int minutes = int.Parse(timeParts[1]);

                var fullDateTime = recordDate.Date.AddHours(hours).AddMinutes(minutes);

                // ตรวจสอบว่าพนักงานมีอยู่ในระบบหรือไม่
                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    // ค้นหาข้อมูลพนักงาน
                    var checkQuery = "SELECT COUNT(*) FROM Person WHERE personID = @personID";
                    var personExists = await connection.ExecuteScalarAsync<int>(
                        checkQuery,
                        new { personID = employeeId }
                    );

                    if (personExists == 0)
                    {
                        return Json(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });
                    }

                    // สร้างข้อมูลการเข้าออกใหม่
                    var eventType = "1"; // ประเภทเหตุการณ์
                    var eventCode = timeType == "in" ? "0001" : "0000"; // รหัสเหตุการณ์ (เข้า/ออก)
                    var deviceType = "HTA860"; // ประเภทอุปกรณ์
                    var deviceName = "Build1 - HR Attendance 01"; // ชื่ออุปกรณ์

                    // เพิ่มข้อมูลลงในตาราง PubEvent
                    var insertQuery =
                        @"
                        INSERT INTO PubEvent (
                            eventType, eventTime, eventCode, personID, personName,
                            deviceName, deviceL1Type, deptCode, deptName, deptID,
                            remark
                        )
                        SELECT 
                            @eventType, @eventTime, @eventCode, p.personID, p.name,
                            @deviceName, @deviceType, d.deptCode, d.name, d.deptID,
                            @remark
                        FROM Person p
                        LEFT JOIN Dept d ON p.deptID = d.deptID
                        WHERE p.personID = @personID";

                    var insertParams = new
                    {
                        eventType = eventType,
                        eventTime = fullDateTime,
                        eventCode = eventCode,
                        personID = employeeId,
                        deviceName = deviceName,
                        deviceType = deviceType,
                        remark = $"เพิ่มโดย {employeeName}: {reason}",
                    };

                    var result = await connection.ExecuteAsync(insertQuery, insertParams);

                    if (result > 0)
                    {
                        // บันทึกประวัติการเพิ่มเวลา
                        var logQuery =
                            @"
                            INSERT INTO emp_time_log (
                                employee_id, record_date, time_type, time_value, 
                                reason, created_by, created_at
                            ) VALUES (
                                @employeeId, @recordDate, @timeType, @timeValue,
                                @reason, @createdBy, @createdAt
                            )";

                        var logParams = new
                        {
                            employeeId = employeeId,
                            recordDate = recordDate.Date,
                            timeType = timeType,
                            timeValue = timeValue,
                            reason = reason,
                            createdBy = employeeName,
                            createdAt = DateTime.Now,
                        };

                        await _dbContext.Database.ExecuteSqlRawAsync(logQuery, logParams);

                        return Json(new { success = true, message = "เพิ่มเวลาเรียบร้อยแล้ว" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "ไม่สามารถเพิ่มเวลาได้" });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"เกิดข้อผิดพลาดในการเพิ่มเวลา: {ex.Message}");
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        // เพิ่ม Action Method สำหรับการเพิ่มเวลาเข้า-ออกด้วยตนเอง (รองรับหลายรูปแบบเวลา)
        [HttpPost]
        public async Task<IActionResult> AddTime()
        {
            try
            {
                // ตรวจสอบสิทธิ์การใช้งาน
                var employeeName = HttpContext.Session.GetString("EmployeeName");
                if (employeeName != "Adisak S.")
                {
                    return Json(
                        new { status = "error", message = "คุณไม่มีสิทธิ์ในการเพิ่มเวลาด้วยตนเอง" }
                    );
                }

                // รับค่า input
                var personID = Request.Form["personID"];
                var type = Request.Form["type"];
                var customTime = Request.Form["customTime"];

                // ตรวจสอบข้อมูลที่จำเป็น
                if (string.IsNullOrEmpty(personID))
                {
                    return Json(new { status = "error", message = "ไม่พบรหัสพนักงาน" });
                }

                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    await connection.OpenAsync();

                    // ดึงข้อมูลพนักงาน
                    var personQuery = "SELECT TOP 1 * FROM Person WHERE personID LIKE @personID";
                    var person = await connection.QueryFirstOrDefaultAsync(
                        personQuery,
                        new { personID = $"%{personID}%" }
                    );

                    if (person == null)
                    {
                        return Json(new { status = "error", message = "ไม่พบข้อมูลพนักงาน" });
                    }

                    // ดึงข้อมูล event ล่าสุดเพื่อใช้เป็นต้นแบบ
                    var eventQuery =
                        @"
                        SELECT TOP 1 * FROM PubEvent 
                        WHERE personID LIKE @personID AND deviceName LIKE @deviceName 
                        ORDER BY eventTime DESC";
                    var latestEvent = await connection.QueryFirstOrDefaultAsync(
                        eventQuery,
                        new { personID = $"%{personID}%", deviceName = "%HR Attendance%" }
                    );

                    if (latestEvent == null)
                    {
                        return Json(new { status = "error", message = "ไม่พบข้อมูล event ต้นแบบ" });
                    }

                    // กำหนดเวลาตามประเภท
                    string eventTimeStr = "";
                    DateTime eventDateTime;

                    switch (type)
                    {
                        case "in":
                            eventTimeStr = Request.Form["inTime"];
                            break;
                        case "out":
                            eventTimeStr = Request.Form["outTime"];
                            break;
                        case "outOT":
                            eventTimeStr = Request.Form["outOT"];
                            break;
                        case "inDay":
                            eventTimeStr = Request.Form["inDay"];
                            break;
                        case "custom":
                            // แปลงรูปแบบวันที่เวลาจาก input datetime-local
                            if (string.IsNullOrEmpty(customTime))
                            {
                                return Json(
                                    new
                                    {
                                        status = "error",
                                        message = "กรุณาระบุเวลาที่ต้องการบันทึก",
                                    }
                                );
                            }

                            // แปลงรูปแบบวันที่เวลา
                            if (!DateTime.TryParse(customTime, out eventDateTime))
                            {
                                return Json(
                                    new { status = "error", message = "รูปแบบเวลาไม่ถูกต้อง" }
                                );
                            }

                            // กำหนดรูปแบบให้ตรงกับฐานข้อมูล
                            eventTimeStr = eventDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                            break;
                        default:
                            return Json(
                                new { status = "error", message = "ประเภทการบันทึกเวลาไม่ถูกต้อง" }
                            );
                    }

                    if (string.IsNullOrEmpty(eventTimeStr))
                    {
                        return Json(
                            new { status = "error", message = "ไม่พบข้อมูลเวลาที่ต้องการบันทึก" }
                        );
                    }

                    // ตรวจสอบความถูกต้องของวันที่เวลา
                    if (!DateTime.TryParse(eventTimeStr, out eventDateTime))
                    {
                        return Json(new { status = "error", message = "รูปแบบเวลาไม่ถูกต้อง" });
                    }

                    // กำหนดรหัสเหตุการณ์ตามประเภท
                    string eventCode = "0000"; // ค่าเริ่มต้นเป็นการออก
                    if (type == "in" || type == "inDay")
                    {
                        eventCode = "0001"; // รหัสเข้างาน
                    }

                    // สร้างข้อมูลสำหรับบันทึก
                    var data = new
                    {
                        eventType = latestEvent.eventType,
                        eventTime = eventDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        eventName = latestEvent.eventName,
                        eventCode = eventCode, // ใช้รหัสที่กำหนดตามประเภท
                        eventCard = person.cardNumber,
                        personID = personID,
                        personName = person.name,
                        deptID = person.deptID,
                        deptName = person.deptName,
                        deptCode = latestEvent.deptCode,
                        deviceID = latestEvent.deviceID,
                        deviceName = latestEvent.deviceName,
                        deviceType = latestEvent.deviceType,
                        doorName = latestEvent.doorName,
                        deviceL1ID = latestEvent.deviceL1ID,
                        deviceL1Name = latestEvent.deviceL1Name,
                        deviceL1Type = latestEvent.deviceL1Type,
                        deviceL2ID = latestEvent.deviceL2ID,
                        deviceL2Name = latestEvent.deviceL2Name,
                        deviceL2Type = latestEvent.deviceL2Type,
                        deviceL3ID = latestEvent.deviceL3ID,
                        deviceL3Name = latestEvent.deviceL3Name,
                        tag = latestEvent.tag,
                        reserve1 = latestEvent.reserve1,
                        reserve2 = latestEvent.reserve2,
                        reserve3 = latestEvent.reserve3,
                        reserve4 = latestEvent.reserve4,
                        systemType = latestEvent.systemType,
                        sourcePK = latestEvent.sourcePK,
                        systemName = latestEvent.systemName,
                        InOut = latestEvent.InOut,
                        EmailAlarmSend = latestEvent.EmailAlarmSend,
                        cctvUpdate = latestEvent.cctvUpdate,
                        extend1 = latestEvent.extend1,
                        NewEventCode_Id = latestEvent.NewEventCode_Id,
                        NewEventCode_Name = latestEvent.NewEventCode_Name,
                        NewEventCode_Type = latestEvent.NewEventCode_Type,
                        Temperature = latestEvent.Temperature,
                        DeductAmount = latestEvent.DeductAmount,
                        PreviousBalance = latestEvent.PreviousBalance,
                        NowBalance = latestEvent.NowBalance,
                        DeductType = latestEvent.DeductType,
                    };

                    // สร้าง SQL สำหรับบันทึกข้อมูล
                    var insertQuery = await BuildInsertQuery("PubEvent", data);
                    int result = await connection.ExecuteAsync(
                        insertQuery.query,
                        insertQuery.parameters
                    );

                    if (result <= 0)
                    {
                        return Json(new { status = "error", message = "ไม่สามารถบันทึกข้อมูลได้" });
                    }

                    // บันทึกประวัติการเพิ่มเวลา
                    var logQuery =
                        @"
                        INSERT INTO emp_time_log (
                            employee_id, record_date, time_type, time_value, 
                            reason, created_by, created_at
                        ) VALUES (
                            @employeeId, @recordDate, @timeType, @timeValue,
                            @reason, @createdBy, @createdAt
                        )";

                    var logParams = new
                    {
                        employeeId = personID,
                        recordDate = eventDateTime.Date,
                        timeType = type,
                        timeValue = eventDateTime.ToString("HH:mm:ss"),
                        reason = $"เพิ่มเวลาแบบ {type}",
                        createdBy = employeeName,
                        createdAt = DateTime.Now,
                    };

                    await _dbContext.Database.ExecuteSqlRawAsync(logQuery, logParams);

                    // ส่งผลลัพธ์กลับ
                    return Json(
                        new
                        {
                            status = "success",
                            message = "บันทึกข้อมูลสำเร็จ",
                            data = new
                            {
                                eventTime = eventTimeStr,
                                personName = person.name,
                                type = type,
                            },
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in AddTime: {ex.Message}");
                return Json(new { status = "error", message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        // สร้าง SQL สำหรับบันทึกข้อมูล
        private async Task<(string query, object parameters)> BuildInsertQuery(
            string table,
            object data
        )
        {
            var properties = data.GetType().GetProperties();
            var columns = new List<string>();
            var parameterNames = new List<string>();
            var parameters = new DynamicParameters();

            foreach (var prop in properties)
            {
                var value = prop.GetValue(data);
                if (value != null)
                {
                    columns.Add(prop.Name);
                    parameterNames.Add($"@{prop.Name}");
                    parameters.Add($"@{prop.Name}", value);
                }
            }

            var query =
                $"INSERT INTO {table} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", parameterNames)})";
            return (query, parameters);
        }

        [HttpPost]
        public async Task<IActionResult> SaveLeaveRequest(emp_leave_outside request)
        {
            try
            {
                // ตรวจสอบข้อมูลก่อนบันทึก
                if (
                    string.IsNullOrEmpty(request.emp_name_th)
                    || string.IsNullOrEmpty(request.start_time)
                    || string.IsNullOrEmpty(request.end_time)
                    || string.IsNullOrEmpty(request.reason_type)
                    || string.IsNullOrEmpty(request.topic)
                    || string.IsNullOrEmpty(request.location)
                )
                {
                    return Json(new { success = false, message = "กรุณากรอกข้อมูลให้ครบถ้วน" });
                }

                // สร้าง SQL query สำหรับบันทึกข้อมูล โดยไม่รวมคอลัมน์ id
                var properties = request.GetType().GetProperties().Where(p => p.Name != "id"); // ไม่รวมคอลัมน์ id

                var columns = new List<string>();
                var parameterNames = new List<string>();
                var parameters = new DynamicParameters();

                foreach (var prop in properties)
                {
                    var value = prop.GetValue(request);
                    if (value != null)
                    {
                        columns.Add(prop.Name);
                        parameterNames.Add($"@{prop.Name}");
                        parameters.Add($"@{prop.Name}", value);
                    }
                }

                var query =
                    $"INSERT INTO emp_leave_outside ({string.Join(", ", columns)}) VALUES ({string.Join(", ", parameterNames)})";

                // บันทึกข้อมูลลงฐานข้อมูล
                using (
                    var connection = new SqlConnection(
                        _configuration.GetConnectionString("DefaultConnection")
                    )
                )
                {
                    await connection.OpenAsync();
                    await connection.ExecuteAsync(query, parameters);
                }

                return Json(new { success = true, message = "บันทึกข้อมูลสำเร็จ" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SaveLeaveRequest: {ex.Message}");
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeInfo(string empNo)
        {
            try
            {
                // ถ้าไม่มีการส่ง emp_no มา ให้แสดงข้อความแจ้งเตือน
                if (string.IsNullOrEmpty(empNo))
                {
                    return Json(new { success = false, message = "กรุณาระบุรหัสพนักงาน" });
                }

                // ดึงข้อมูลพนักงานจากระบบ ces941 โดยใช้ emp_no
                using (var connection = new SqlConnection(_ces941ConnectionString))
                {
                    await connection.OpenAsync();

                    // ใช้ query ที่ดึงข้อมูลเพิ่มเติมจากตาราง MEmpBasic
                    var query =
                        @"SELECT TOP (1) 
                                Language, EmpNo, PrefixName, EmpName, EmpLName, Gender, 
                                EmpStartDate, EmpResignDate, LastDateAtten, 
                                ProbationDay, ProbationPass, ProbationDate, 
                                ContractNo, ContAdd, ContCity, ContZipCode, ContZipDesc, ContCountry, 
                                RegAdd, RegCity, RegZipCode, RegZipDesc, RegCountry, 
                                HomePhone, MobilePhone, Internet, eMailAdd, 
                                ADUser, R3User, DIHUser, LotusUser, SkyUser, RNewUser, 
                                BirthDate, BirthCity, BirthCountry, Nation, Nationality, Religion, 
                                BankID, BankAccount, PersonalID, PassPort, TaxID, SFID, 
                                Hospital, CarLicense, BikeLicense, Marital, MilitaryPass, 
                                Department, Position, Division
                                FROM MEmpBasic 
                                WHERE EmpNo = @empNo AND FlagDel = 'N'";

                    var employee = await connection.QueryFirstOrDefaultAsync(query, new { empNo });

                    if (employee == null)
                    {
                        return Json(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });
                    }

                    // ดึงข้อมูลแผนกและผู้ตรวจสอบ
                    var approversQuery =
                        @"SELECT TOP (1) Department, ManagerEmpNo, ManagerName, 
                                        DeptHeadEmpNo, DeptHeadName, DirectorEmpNo, DirectorName
                                        FROM MDepartment 
                                        WHERE Department = @department";

                    var approvers = await connection.QueryFirstOrDefaultAsync(
                        approversQuery,
                        new { department = employee.Department }
                    );

                    // สร้างข้อมูลที่จะส่งกลับ โดยรวมข้อมูลที่จำเป็นให้ตรงกับโมเดล emp_leave_outside
                    var today = DateTime.Now;

                    var result = new
                    {
                        success = true,
                        emp_no = employee.EmpNo,
                        emp_name_th = employee.PrefixName
                            + employee.EmpName
                            + " "
                            + employee.EmpLName,
                        emp_name_en = employee.EmpName + " " + employee.EmpLName,
                        emp_type = employee.Position,
                        dept = employee.Department,
                        division = employee.Division,
                        mobile = employee.MobilePhone,
                        email = employee.eMailAdd,
                        year = today.Year,
                        month = today.Month,
                        period = (today.Day <= 15) ? 1 : 2,
                        leave_date = today.ToString("yyyy-MM-dd"),
                        request_date = today.ToString("yyyy-MM-dd"),
                        start_time = "08:00",
                        end_time = "17:00",
                        total_hours = "8.00",
                        requestor = employee.PrefixName
                            + employee.EmpName
                            + " "
                            + employee.EmpLName,
                        approver = approvers?.DeptHeadName,
                        approver2 = approvers?.ManagerName,
                        approver3 = approvers?.DirectorName,
                        created_at = today.ToString("yyyy-MM-dd HH:mm:ss"),
                        created_username = User.Identity?.Name ?? "system",
                        record_status = "N",
                    };

                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการดึงข้อมูลพนักงาน");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetDoorHistory(string empId, string date, string shift)
        {
            try
            {
                using (var ces941 = new SqlConnection(_ces941ConnectionString))
                using (var SQL944 = new SqlConnection(_sql944ConnectionString))
                {
                    var parsedDate = DateTime.Parse(date);
                    var startDate = parsedDate.ToString("yyyy-MM-dd") + " 06:00:00";
                    var endDate = parsedDate.ToString("yyyy-MM-dd") + " 23:59:59";

                    await ces941.OpenAsync();
                    await SQL944.OpenAsync();

                    var baseQuery =
                        @"
                        SELECT * FROM PubEvent
                        WHERE personID LIKE @empId + '%'";

                    if (shift.Contains("NT1") || shift.Contains("N01"))
                    {
                        startDate = parsedDate.AddDays(-1).ToString("yyyy-MM-dd") + " 18:00:00";
                        endDate = parsedDate.ToString("yyyy-MM-dd") + " 10:30:00";
                    }

                    var query =
                        baseQuery
                        + @"
                        AND CAST(eventTime AS DATETIME) BETWEEN @startDate AND @endDate
                        ORDER BY eventTime ASC";

                    var queryDataPerson =
                        @"
                        SELECT * FROM MEmpBasic
                        WHERE EmpNo like @empId + '%' and Language = 'EN'";

                    var result = await SQL944.QueryAsync<dynamic>(
                        query,
                        new
                        {
                            empId,
                            startDate,
                            endDate,
                        }
                    );
                    var resultDataPerson = await ces941.QueryAsync<dynamic>(
                        queryDataPerson,
                        new { empId }
                    );

                    var personIds = result.Select(r => r.personID.ToString()).Distinct().ToList();
                    var finalResults = new List<dynamic>();

                    foreach (var personId in personIds)
                    {
                        var personQuery =
                            @"
                            SELECT * FROM PubEvent
                            WHERE personID = @personId
                            AND CAST(eventTime AS DATETIME) BETWEEN @startDate AND @endDate
                            ORDER BY eventTime ASC";

                        var personResult = await SQL944.QueryAsync<dynamic>(
                            personQuery,
                            new
                            {
                                personId,
                                startDate,
                                endDate,
                            }
                        );
                        finalResults.AddRange(personResult);
                    }

                    return Json(new { success = true, data = finalResults });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการดึงข้อมูลประวัติการเข้าออก");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetShiftHistory(string personId)
        {
            try
            {
                if (string.IsNullOrEmpty(personId))
                {
                    return Json(new { success = false, message = "กรุณาระบุรหัสพนักงาน" });
                }

                // ตัด -1 หรือ -2 ออกจาก personId ถ้ามี
                string basePersonId = personId;
                if (personId.Contains("-"))
                {
                    basePersonId = personId.Split('-')[0];
                }

                using (var connection = new SqlConnection(_connectionString))
                using (var connection944 = new SqlConnection(_sql944ConnectionString))
                {
                    // ดึงข้อมูลแผนก
                    var deptSql =
                        @"SELECT TOP 1 name, deptID, deptName FROM person WHERE personID LIKE @personId + '%'";
                    var employee = await connection944.QueryFirstOrDefaultAsync<dynamic>(
                        deptSql,
                        new { personId = basePersonId }
                    );

                    string deptName = "";
                    string deptCode = "";
                    string name = "";

                    if (employee != null)
                    {
                        deptName = employee.deptName ?? "";
                        deptCode = employee.deptID ?? "";
                        name = employee.name ?? "";
                    }

                    // ดึงประวัติกะการทำงาน
                    var shiftHistorySql =
                        @"
                        SELECT
                            id,
                            personID,
                            date,
                            shiftMent,
                            deptName,
                            deptCode,
                            name,
                            isActive,
                            remark,
                            created_at,
                            created_by,
                            updated_at,
                            updated_by
                        FROM emp_person_shift
                        WHERE personID = @personID
                        ORDER BY date DESC";

                    var shiftHistory = await connection.QueryAsync<dynamic>(
                        shiftHistorySql,
                        new { personID = basePersonId }
                    );

                    // เพิ่มข้อมูลแผนกในกรณีที่ไม่มีในตารางประวัติ
                    foreach (var item in shiftHistory)
                    {
                        if (string.IsNullOrEmpty(item.deptName))
                        {
                            item.deptName = deptName;
                        }
                        if (string.IsNullOrEmpty(item.deptCode))
                        {
                            item.deptCode = deptCode;
                        }
                        if (string.IsNullOrEmpty(item.name))
                        {
                            item.name = name;
                        }
                    }

                    return Json(shiftHistory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetShiftHistory: {ex.Message}");
                return Json(new List<object>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> HandleTimeAction(
            string type,
            string personId,
            string date,
            string shift,
            string time
        )
        {
            try
            {
                using var ces941 = new SqlConnection(_ces941ConnectionString);
                using var SQL944 = new SqlConnection(_sql944ConnectionString);
                await Task.WhenAll(ces941.OpenAsync(), SQL944.OpenAsync());

                // แปลง shift เป็นรูปแบบมาตรฐาน
                shift = NormalizeShift(shift);

                // ตรวจสอบข้อมูลพนักงาน
                var person = await GetPersonData(SQL944, personId);
                if (person == null)
                    return Json(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });

                // ดึงข้อมูล event ต้นแบบ
                var template = await GetEventTemplate(SQL944, personId);
                if (template == null)
                    return Json(new { success = false, message = "ไม่พบข้อมูล event ต้นแบบ" });

                // คำนวณเวลาตามประเภทและกะ
                var eventTime = await CalculateEventTime(type, date, shift, time);
                if (eventTime == null)
                    return Json(new { success = false, message = "ไม่สามารถคำนวณเวลาได้" });

                // สร้างและบันทึกข้อมูล
                var data = CreateEventData(template, person, personId, eventTime.Value);
                await SaveEventData(SQL944, data);

                return Json(
                    new
                    {
                        success = true,
                        message = "บันทึกข้อมูลสำเร็จ",
                        data = new
                        {
                            eventTime = eventTime,
                            personName = person.name,
                            type,
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดในการเพิ่มข้อมูลการสแกน");
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string NormalizeShift(string shift) =>
            shift switch
            {
                "NT1" or "N01" => "night",
                "OF02" => "dayoffice",
                _ => "day",
            };

        private async Task<dynamic> GetPersonData(SqlConnection connection, string personId)
        {
            const string query = "SELECT TOP 1 * FROM Person WHERE personID LIKE @personId + '%'";
            return await connection.QueryFirstOrDefaultAsync<dynamic>(query, new { personId });
        }

        private async Task<dynamic> GetEventTemplate(SqlConnection connection, string personId)
        {
            const string query =
                @"
                SELECT TOP(1) * FROM PubEvent 
                WHERE personID LIKE @personId + '%' 
                ORDER BY rowAutoID DESC";

            return await connection.QueryFirstOrDefaultAsync<dynamic>(query, new { personId });
        }

        private async Task<DateTime?> CalculateEventTime(
            string type,
            string date,
            string shift,
            string time
        )
        {
            if (type.ToLower() == "custom" && string.IsNullOrEmpty(time))
                return null;

            var baseDate = DateTime.Parse(date);
            var timeStr = type.ToLower() switch
            {
                "in" => shift == "night"
                    ? baseDate.AddDays(-1).ToString("yyyy-MM-dd") + " 19:50:00"
                    : date + " 07:30:00",
                "in-day" => shift == "night"
                    ? baseDate.ToString("yyyy-MM-dd") + " 00:30:00"
                    : date + " 12:30:00",
                "out" => shift switch
                {
                    "night" => date + " 05:32:00",
                    "dayoffice" => date + " 18:35:00",
                    _ => date + " 17:15:00",
                },
                "ot-out" => shift switch
                {
                    "night" => date + " 07:50:00",
                    "dayoffice" => date + " 20:35:00",
                    _ => date + " 19:35:00",
                },
                "custom" => time,
                _ => null,
            };

            return timeStr != null ? DateTime.Parse(timeStr) : null;
        }

        private object CreateEventData(
            dynamic template,
            dynamic person,
            string personId,
            DateTime eventTime
        ) =>
            new
            {
                eventType = template.eventType,
                eventTime,
                eventName = template.eventName,
                eventCode = template.eventCode,
                eventCard = person.cardNumber,
                personID = personId,
                personName = person.name,
                deptID = person.deptID,
                deptName = person.deptName,
                deptCode = template.deptCode,
                deviceID = template.deviceID,
                deviceName = template.deviceName,
                deviceType = template.deviceType,
                doorName = template.doorName,
                deviceL1ID = template.deviceL1ID,
                deviceL1Name = template.deviceL1Name,
                deviceL1Type = template.deviceL1Type,
                deviceL2ID = template.deviceL2ID,
                deviceL2Name = template.deviceL2Name,
                deviceL2Type = template.deviceL2Type,
                deviceL3ID = template.deviceL3ID,
                deviceL3Name = template.deviceL3Name,
                tag = template.tag,
                reserve1 = template.reserve1,
                reserve2 = template.reserve2,
                reserve3 = template.reserve3,
                reserve4 = template.reserve4,
                systemType = template.systemType,
                sourcePK = template.sourcePK,
                systemName = template.systemName,
                InOut = template.InOut,
                EmailAlarmSend = template.EmailAlarmSend,
                cctvUpdate = template.cctvUpdate,
                extend1 = template.extend1,
                NewEventCode_Id = template.NewEventCode_Id,
                NewEventCode_Name = template.NewEventCode_Name,
                NewEventCode_Type = template.NewEventCode_Type,
                Temperature = template.Temperature,
                DeductAmount = template.DeductAmount,
                PreviousBalance = template.PreviousBalance,
                NowBalance = template.NowBalance,
                DeductType = template.DeductType,
            };

        private async Task SaveEventData(SqlConnection connection, object data)
        {
            var insertQuery = await BuildInsertQuery("PubEvent", data);
            await connection.ExecuteAsync(insertQuery.query, insertQuery.parameters);
        }

        [HttpPost]
        public async Task<IActionResult> EditShift(
            int id,
            string personId,
            string date,
            string shiftMent,
            string namePerson,
            string remark,
            string department
        )
        {
            try
            {
                if (
                    string.IsNullOrEmpty(personId)
                    || string.IsNullOrEmpty(date)
                    || string.IsNullOrEmpty(shiftMent)
                    || string.IsNullOrEmpty(department)
                )
                {
                    return Json(new { success = false, message = "กรุณาระบุข้อมูลให้ครบถ้วน" });
                }

                // ตัด -1 หรือ -2 ออกจาก personId ถ้ามี
                string basePersonId = personId;
                if (personId.Contains("-"))
                {
                    basePersonId = personId.Split('-')[0];
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    // ตรวจสอบว่ามีข้อมูลที่จะแก้ไขหรือไม่
                    var checkSql = @"SELECT COUNT(1) FROM emp_person_shift WHERE id = @id";
                    var exists = await connection.QueryFirstOrDefaultAsync<int>(
                        checkSql,
                        new { id }
                    );

                    if (exists == 0)
                    {
                        return Json(
                            new { success = false, message = "ไม่พบข้อมูลกะที่ต้องการแก้ไข" }
                        );
                    }

                    // แก้ไขข้อมูลกะ
                    var updateSql =
                        @"
                        UPDATE emp_person_shift 
                        SET shiftMent = @shiftMent, 
                            date = @date,
                            name = @namePerson,
                            remark = @remark,
                            deptID = @department,
                            deptCode = @department
                        WHERE id = @id";

                    await connection.ExecuteAsync(
                        updateSql,
                        new
                        {
                            id,
                            shiftMent,
                            date,
                            namePerson,
                            remark,
                            department,
                        }
                    );

                    return Json(new { success = true, message = "แก้ไขกะการทำงานสำเร็จ" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in EditShift: {ex.Message}");
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteShiftForperson(
            string personId,
            string date,
            string namePerson,
            string shiftMent,
            string remark,
            int id
        )
        {
            try
            {
                if (string.IsNullOrEmpty(personId) || string.IsNullOrEmpty(date))
                {
                    return Json(new { success = false, message = "กรุณาระบุข้อมูลให้ครบถ้วน" });
                }

                // ตัด -1 หรือ -2 ออกจาก personId ถ้ามี
                string basePersonId = personId;
                if (personId.Contains("-"))
                {
                    basePersonId = personId.Split('-')[0];
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    // ตรวจสอบว่ามีข้อมูลที่จะลบหรือไม่
                    var checkSql =
                        @"
                        SELECT COUNT(1) 
                        FROM emp_person_shift 
                        WHERE personID = @personID 
                        AND CAST(date AS DATE) = CAST(@date AS DATE)";

                    var exists = await connection.QueryFirstOrDefaultAsync<int>(
                        checkSql,
                        new { personID = basePersonId, date = date }
                    );

                    if (exists == 0)
                    {
                        return Json(new { success = false, message = "ไม่พบข้อมูลกะที่ต้องการลบ" });
                    }

                    // ลบข้อมูลกะ
                    var deleteSql =
                        @"
                        DELETE FROM emp_person_shift 
                        WHERE personID = @personID 
                        AND CAST(date AS DATE) = CAST(@date AS DATE)";

                    await connection.ExecuteAsync(
                        deleteSql,
                        new { personID = basePersonId, date = date }
                    );

                    return Json(new { success = true, message = "ลบกะการทำงานสำเร็จ" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in DeleteShiftForperson: {ex.Message}");
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetDoorHistory30day(
            string personId,
            string date,
            string shift
        )
        {
            try
            {
                if (string.IsNullOrEmpty(personId))
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลรหัสพนักงาน" });
                }

                using (var sqlConnection = new SqlConnection(_sql944ConnectionString))
                using (var ces941 = new SqlConnection(_ces941ConnectionString))
                {
                    await sqlConnection.OpenAsync();
                    await ces941.OpenAsync();

                    string query =
                        @"
                        SELECT 
                            e.eventTime,
                            e.doorName,
                            e.eventType,    
                            e.deviceName,
                            e.personID,
                            e.personName,
                            e.deptName,
                            e.deptCode
                        FROM 
                            PubEvent e
                        WHERE 
                            e.personID like @personId + '%'
                        ORDER BY 
                            e.eventTime DESC";

                    var doorHistory = await sqlConnection.QueryAsync(query, new { personId });

                    return Json(new { success = true, data = doorHistory });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteScanRecord(
            int rowAutoId,
            string personId,
            string eventTime
        )
        {
            try
            {
                // ตรวจสอบสิทธิ์การใช้งาน - เฉพาะ Adisak S. เท่านั้นที่สามารถลบข้อมูลได้
                var employeeName = HttpContext.Session.GetString("EmployeeName");
                if (employeeName != "Adisak S.")
                {
                    return Json(
                        new { success = false, message = "คุณไม่มีสิทธิ์ในการลบประวัติการสแกน" }
                    );
                }

                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    await connection.OpenAsync();

                    // ตรวจสอบว่าข้อมูลมีอยู่จริงหรือไม่
                    var checkQuery = "SELECT COUNT(*) FROM PubEvent WHERE rowAutoID = @rowAutoId";
                    var exists = await connection.ExecuteScalarAsync<int>(
                        checkQuery,
                        new { rowAutoId }
                    );

                    if (exists == 0)
                    {
                        return Json(
                            new { success = false, message = "ไม่พบข้อมูลการสแกนที่ต้องการลบ" }
                        );
                    }

                    // ดึงข้อมูลการสแกนก่อนลบเพื่อบันทึกประวัติ
                    var getRecordQuery = "SELECT * FROM PubEvent WHERE rowAutoID = @rowAutoId";
                    var record = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        getRecordQuery,
                        new { rowAutoId }
                    );

                    // ลบข้อมูลจากตาราง PubEvent
                    var deleteQuery = "DELETE FROM PubEvent WHERE rowAutoID = @rowAutoId";
                    var result = await connection.ExecuteAsync(deleteQuery, new { rowAutoId });

                    if (result > 0)
                    {
                        // บันทึกประวัติการลบ
                        var logQuery =
                            @"
                            INSERT INTO emp_scan_delete_log (
                                row_auto_id, person_id, person_name, event_time, 
                                device_name, event_code, deleted_by, deleted_at, 
                                original_data
                            ) VALUES (
                                @rowAutoId, @personId, @personName, @eventTime,
                                @deviceName, @eventCode, @deletedBy, @deletedAt,
                                @originalData
                            )";

                        var logParams = new
                        {
                            rowAutoId = rowAutoId,
                            personId = record?.personID,
                            personName = record?.personName,
                            eventTime = record?.eventTime,
                            deviceName = record?.deviceName,
                            eventCode = record?.eventCode,
                            deletedBy = employeeName,
                            deletedAt = DateTime.Now,
                            originalData = JsonConvert.SerializeObject(record),
                        };

                        try
                        {
                            await connection.ExecuteAsync(logQuery, logParams);
                        }
                        catch (Exception logEx)
                        {
                            _logger.LogWarning($"ไม่สามารถบันทึกประวัติการลบได้: {logEx.Message}");
                        }

                        return Json(
                            new { success = true, message = "ลบประวัติการสแกนเรียบร้อยแล้ว" }
                        );
                    }
                    else
                    {
                        return Json(new { success = false, message = "ไม่สามารถลบข้อมูลได้" });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"เกิดข้อผิดพลาดในการลบประวัติการสแกน: {ex.Message}");
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetScanDeleteLog(
            string personId = null,
            string startDate = null,
            string endDate = null
        )
        {
            try
            {
                // ตรวจสอบสิทธิ์การใช้งาน - เฉพาะ Adisak S. เท่านั้นที่สามารถดูประวัติได้
                var employeeName = HttpContext.Session.GetString("EmployeeName");
                if (employeeName != "Adisak S.")
                {
                    return Json(
                        new { success = false, message = "คุณไม่มีสิทธิ์ในการดูประวัติการลบ" }
                    );
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query =
                        @"
                        SELECT 
                            id,
                            row_auto_id,
                            person_id,
                            person_name,
                            event_time,
                            device_name,
                            event_code,
                            deleted_by,
                            deleted_at,
                            original_data
                        FROM emp_scan_delete_log
                        WHERE 1=1";

                    var parameters = new DynamicParameters();

                    if (!string.IsNullOrEmpty(personId))
                    {
                        query += " AND person_id LIKE @personId";
                        parameters.Add("personId", $"%{personId}%");
                    }

                    if (!string.IsNullOrEmpty(startDate))
                    {
                        query += " AND CAST(deleted_at AS DATE) >= @startDate";
                        parameters.Add("startDate", DateTime.Parse(startDate));
                    }

                    if (!string.IsNullOrEmpty(endDate))
                    {
                        query += " AND CAST(deleted_at AS DATE) <= @endDate";
                        parameters.Add("endDate", DateTime.Parse(endDate));
                    }

                    query += " ORDER BY deleted_at DESC";

                    var result = await connection.QueryAsync<dynamic>(query, parameters);

                    return Json(new { success = true, data = result });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"เกิดข้อผิดพลาดในการดึงประวัติการลบ: {ex.Message}");
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddOT(
            string personId,
            string addType,
            string[] workDates,
            string actionType,
            string actionReason,
            string empGroup,
            string shift_group,
            string startDate,
            string startTime,
            string endDate,
            string endTime,
            decimal calculatedHours,
            string remark
        )
        {
            try
            {
                // ตรวจสอบข้อมูลที่จำเป็น
                if (workDates == null || workDates.Length == 0)
                {
                    return Json(new { success = false, message = "กรุณาเลือกวันที่ทำงาน" });
                }

                // ใช้วันที่แรกสำหรับการตรวจสอบ validation
                var firstWorkDate = workDates[0];

                // 1. ตรวจสอบข้อมูลที่จำเป็น
                var validationResult = ValidateOTRequest(
                    personId,
                    firstWorkDate,
                    actionType,
                    actionReason,
                    startDate,
                    startTime,
                    endDate,
                    endTime
                );
                if (!validationResult.IsValid)
                {
                    return Json(new { success = false, message = validationResult.ErrorMessage });
                }

                // 2. ตรวจสอบ connection strings
                var connectionValidation = ValidateConnections();
                if (!connectionValidation.IsValid)
                {
                    return Json(
                        new { success = false, message = connectionValidation.ErrorMessage }
                    );
                }

                // 3. แปลงวันที่และเวลา (ใช้วันที่แรก)
                var dateResult = ParseDates(firstWorkDate, startDate, startTime, endDate, endTime);
                if (!dateResult.IsValid)
                {
                    return Json(new { success = false, message = dateResult.ErrorMessage });
                }

                // 4. เชื่อมต่อฐานข้อมูล
                using (var mainConnection = new SqlConnection(_connectionString))
                using (var ces941Connection = new SqlConnection(_ces941ConnectionString))
                {
                    await mainConnection.OpenAsync();
                    await ces941Connection.OpenAsync();

                    // 5. ดึงข้อมูลพนักงาน
                    var empResult = await GetEmployeeData(ces941Connection, personId);
                    if (!empResult.IsValid)
                    {
                        return Json(new { success = false, message = empResult.ErrorMessage });
                    }

                    // ตรวจสอบข้อมูลพนักงานที่จำเป็น
                    if (empResult.EmpData == null)
                    {
                        return Json(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });
                    }

                    // 6. ดึงข้อมูล WrkPlanID
                    var wrkPlanId = await GetWrkPlanId(
                        ces941Connection,
                        personId,
                        dateResult.WrkDate
                    );

                    // 7. ดึงข้อมูล WorkPlan
                    var workPlanResult = await GetWorkPlanData(
                        ces941Connection,
                        wrkPlanId,
                        dateResult.WrkDate
                    );

                    // 8. ดึงข้อมูลผู้อนุมัติ
                    var bossId = empResult.EmpData.BossId?.ToString();
                    var nextAppr = await GetNextApprover(ces941Connection, bossId);

                    // 9. แปลง Action codes
                    var actionCodes = ConvertActionCodes(actionType, actionReason);

                    // 10. ตรวจสอบและบันทึกข้อมูลโอที
                    var commonOtReqNo = $"IOT{DateTime.Now:yyyyMMddHHmmss}";
                    var savedOtReqNos = new List<string>();
                    var skippedDates = new List<object>();
                    var totalDays = workDates.Length;
                    var processedDays = 0;

                    for (int i = 0; i < workDates.Length; i++)
                    {
                        Console.WriteLine("Processing day " + i + 1);
                        var currentWorkDate = workDates[i];
                        var currentWrkDate = DateTime.Parse(currentWorkDate);

                        // ตรวจสอบว่ามีการขอโอทีในวันที่นี้แล้วหรือยัง
                        var existingOTCheck = await CheckExistingOT(
                            mainConnection,
                            personId,
                            currentWrkDate
                        );

                        if (existingOTCheck.Exists)
                        {
                            // มีการขอโอทีแล้ว ให้เก็บข้อมูลเพื่อแสดงผล
                            skippedDates.Add(
                                new
                                {
                                    date = currentWrkDate.ToString("yyyy-MM-dd"),
                                    otReqNo = existingOTCheck.OtReqNo,
                                    message = $"วันที่ {currentWrkDate:dd/MM/yyyy} มีการขอโอทีแล้ว (OTReqNo: {existingOTCheck.OtReqNo})",
                                }
                            );
                            continue; // ข้ามไปวันถัดไป
                        }

                        // ดึงข้อมูล WorkPlan สำหรับแต่ละวัน
                        var currentWrkPlanId = await GetWrkPlanId(
                            ces941Connection,
                            personId,
                            currentWrkDate
                        );

                        var currentWorkPlanResult = await GetWorkPlanData(
                            ces941Connection,
                            currentWrkPlanId,
                            currentWrkDate
                        );

                        // สร้าง DateTime ใหม่สำหรับแต่ละวันที่
                        var currentStartDateTime = new DateTime(
                            currentWrkDate.Year,
                            currentWrkDate.Month,
                            currentWrkDate.Day,
                            dateResult.StartDateTime.Hour,
                            dateResult.StartDateTime.Minute,
                            dateResult.StartDateTime.Second
                        );

                        var currentEndDateTime = new DateTime(
                            currentWrkDate.Year,
                            currentWrkDate.Month,
                            currentWrkDate.Day,
                            dateResult.EndDateTime.Hour,
                            dateResult.EndDateTime.Minute,
                            dateResult.EndDateTime.Second
                        );

                        // จัดการกรณีที่เวลาสิ้นสุดข้ามวัน (เช่น กะกลางคืน)
                        if (currentEndDateTime < currentStartDateTime)
                        {
                            currentEndDateTime = currentEndDateTime.AddDays(1);
                        }

                        _logger.LogInformation(
                            $"Processing day {i + 1}: {currentWrkDate:yyyy-MM-dd}, Start: {currentStartDateTime:yyyy-MM-dd HH:mm}, End: {currentEndDateTime:yyyy-MM-dd HH:mm}"
                        );

                        // บันทึกข้อมูลโอทีสำหรับวันนี้ โดยใช้ OTReqNo เดียวกัน
                        var saveResult = await SaveOTData(
                            mainConnection,
                            empResult.EmpData,
                            personId,
                            currentWrkDate,
                            currentStartDateTime,
                            currentEndDateTime,
                            actionCodes.ActionTypeCode,
                            actionCodes.ActionReasonCode,
                            remark,
                            calculatedHours,
                            currentWorkPlanResult.ShiftCode,
                            currentWorkPlanResult.Month,
                            currentWorkPlanResult.Period,
                            nextAppr,
                            processedDays + 1, // OTSeqNo sequence
                            commonOtReqNo // ใช้ OTReqNo เดียวกัน
                        );

                        if (!saveResult.Item1) // IsValid
                        {
                            return Json(new { success = false, message = saveResult.Item2 }); // ErrorMessage
                        }

                        savedOtReqNos.Add(saveResult.Item3);
                        processedDays++;
                    }

                    // สร้างข้อความและ response ตามผลลัพธ์
                    string message;
                    bool hasSkipped = skippedDates.Count > 0;
                    bool hasProcessed = processedDays > 0;

                    if (hasProcessed && hasSkipped)
                    {
                        // บันทึกได้บางส่วน
                        message =
                            $"บันทึกข้อมูลโอทีเรียบร้อยแล้ว {processedDays} วัน (ข้าม {skippedDates.Count} วันที่มีการขอโอทีแล้ว)";
                    }
                    else if (hasProcessed && !hasSkipped)
                    {
                        // บันทึกได้ทั้งหมด
                        message =
                            totalDays == 1
                                ? "บันทึกข้อมูลโอทีเรียบร้อยแล้ว"
                                : $"บันทึกข้อมูลโอทีเรียบร้อยแล้ว {totalDays} วัน";
                    }
                    else if (!hasProcessed && hasSkipped)
                    {
                        // ไม่สามารถบันทึกได้เลย
                        message =
                            "ไม่สามารถบันทึกข้อมูลโอทีได้ เนื่องจากมีการขอโอทีในวันที่เลือกแล้วทั้งหมด";
                    }
                    else
                    {
                        // ไม่มีข้อมูล
                        message = "ไม่พบข้อมูลวันที่สำหรับบันทึก";
                    }

                    return Json(
                        new
                        {
                            success = hasProcessed, // success = true เฉพาะเมื่อบันทึกได้อย่างน้อย 1 วัน
                            message = message,
                            data = new
                            {
                                otReqNo = hasProcessed ? commonOtReqNo : null,
                                otReqNos = savedOtReqNos,
                                totalDays = totalDays,
                                processedDays = processedDays,
                                skippedDates = skippedDates,
                                hasSkipped = hasSkipped,
                                hasProcessed = hasProcessed,
                            },
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in AddOT: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        // ==================== เมธอดย่อยสำหรับ AddOT ====================

        /// <summary>
        /// ตรวจสอบข้อมูลที่จำเป็นสำหรับการขอโอที
        /// </summary>
        private (bool IsValid, string ErrorMessage) ValidateOTRequest(
            string personId,
            string workDate,
            string actionType,
            string actionReason,
            string startDate,
            string startTime,
            string endDate,
            string endTime
        )
        {
            _logger.LogInformation($"Validating OT request for PersonId: {personId}");

            if (
                string.IsNullOrEmpty(personId)
                || string.IsNullOrEmpty(workDate)
                || string.IsNullOrEmpty(actionType)
                || string.IsNullOrEmpty(actionReason)
                || string.IsNullOrEmpty(startDate)
                || string.IsNullOrEmpty(startTime)
                || string.IsNullOrEmpty(endDate)
                || string.IsNullOrEmpty(endTime)
            )
            {
                var errorMsg = "ข้อมูลที่จำเป็นไม่ครบถ้วน";
                _logger.LogError($"Validation failed: {errorMsg}");
                return (false, errorMsg);
            }

            return (true, "");
        }

        /// <summary>
        /// ตรวจสอบ connection strings
        /// </summary>
        private (bool IsValid, string ErrorMessage) ValidateConnections()
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                _logger.LogError("Main connection string is null or empty");
                return (false, "การตั้งค่าฐานข้อมูลไม่ถูกต้อง");
            }

            if (string.IsNullOrEmpty(_ces941ConnectionString))
            {
                _logger.LogError("CES941 connection string is null or empty");
                return (false, "การตั้งค่าฐานข้อมูลไม่ถูกต้อง");
            }

            return (true, "");
        }

        /// <summary>
        /// แปลงวันที่และเวลาจาก string เป็น DateTime
        /// </summary>
        private (
            bool IsValid,
            string ErrorMessage,
            DateTime WrkDate,
            DateTime StartDateTime,
            DateTime EndDateTime
        ) ParseDates(
            string workDate,
            string startDate,
            string startTime,
            string endDate,
            string endTime
        )
        {
            try
            {
                var wrkDate = DateTime.Parse(workDate);
                var startDateTime = DateTime.Parse($"{startDate} {startTime}");
                var endDateTime = DateTime.Parse($"{endDate} {endTime}");

                // ถ้าเวลาสิ้นสุดเป็นวันถัดไป
                if (endDateTime < startDateTime)
                {
                    endDateTime = endDateTime.AddDays(1);
                }

                _logger.LogInformation(
                    $"Parsed dates - WrkDate: {wrkDate:yyyy-MM-dd}, Start: {startDateTime:yyyy-MM-dd HH:mm}, End: {endDateTime:yyyy-MM-dd HH:mm}"
                );
                return (true, "", wrkDate, startDateTime, endDateTime);
            }
            catch (FormatException ex)
            {
                _logger.LogError($"Date parsing error: {ex.Message}");
                return (
                    false,
                    "รูปแบบวันที่หรือเวลาไม่ถูกต้อง",
                    DateTime.MinValue,
                    DateTime.MinValue,
                    DateTime.MinValue
                );
            }
        }

        /// <summary>
        /// ดึงข้อมูลพนักงานจากฐานข้อมูล
        /// </summary>
        private async Task<(bool IsValid, string ErrorMessage, dynamic EmpData)> GetEmployeeData(
            SqlConnection ces941Connection,
            string personId
        )
        {
            var empQuery =
                @"
                SELECT EmpNo, BossId, SecCode, CostCenter, WrkPlanID 
                FROM vEmployee 
                WHERE EmpNo = @EmpNo";

            try
            {
                var empData = await ces941Connection.QueryFirstOrDefaultAsync(
                    empQuery,
                    new { EmpNo = personId }
                );

                if (empData == null)
                {
                    _logger.LogError($"Employee not found: {personId}");
                    return (false, "ไม่พบข้อมูลพนักงาน", null);
                }

                _logger.LogInformation(
                    $"Employee data found: EmpNo={empData.EmpNo}, BossId={empData.BossId}, SecCode={empData.SecCode}, CostCenter={empData.CostCenter}"
                );
                return (true, "", empData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to query employee data: {ex.Message}");
                return (false, "ไม่สามารถดึงข้อมูลพนักงานได้", null);
            }
        }

        /// <summary>
        /// ดึงข้อมูล WrkPlanID จาก vEmpMovement ตามวันที่
        /// </summary>
        private async Task<string> GetWrkPlanId(
            SqlConnection ces941Connection,
            string personId,
            DateTime workDate
        )
        {
            var empMovementQuery =
                @"
                SELECT TOP 1 WrkPlanID, ActionType, ActionReason, EffDateFrom, EffDateTo
                FROM vEmpMovement 
                WHERE EmpNo = @EmpNo 
                AND Language = 'en' 
                AND EffAttProcess = 'y'
                AND @WorkDate BETWEEN EffDateFrom AND ISNULL(EffDateTo, '9999-12-31')
                ORDER BY EffDateFrom DESC";

            try
            {
                var empMovementData = await ces941Connection.QueryFirstOrDefaultAsync(
                    empMovementQuery,
                    new { EmpNo = personId, WorkDate = workDate.Date }
                );

                if (empMovementData?.WrkPlanID != null)
                {
                    _logger.LogInformation(
                        $"Found WrkPlanID from EmpMovement: {empMovementData.WrkPlanID} for date {workDate:yyyy-MM-dd}"
                    );
                    return empMovementData.WrkPlanID;
                }
                else
                {
                    // ถ้าไม่พบข้อมูลใน vEmpMovement ให้ใช้จาก vEmployee
                    var empDataResult = await GetEmployeeData(ces941Connection, personId);
                    var wrkPlanId = empDataResult.EmpData?.WrkPlanID?.ToString() ?? "OFFICE";
                    _logger.LogInformation(
                        $"No EmpMovement found, using WrkPlanID from vEmployee: {wrkPlanId}"
                    );
                    return wrkPlanId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to query EmpMovement data: {ex.Message}");
                return "OFFICE"; // Default fallback
            }
        }

        /// <summary>
        /// ดึงข้อมูล Month และ Period จาก MWorkPlan
        /// </summary>
        private async Task<(
            int Month,
            int Period,
            string ShiftCode,
            string DayType
        )> GetWorkPlanData(SqlConnection ces941Connection, string wrkPlanId, DateTime workDate)
        {
            var workPlanQuery =
                @"
                SELECT TOP 1 Month, Period, ShiftCode, DayType 
                FROM MWorkPlan 
                WHERE WrkPlanID = @WrkPlanID 
                AND year(WrkDate) = @Year 
                AND month(WrkDate) = @Month 
                AND day(WrkDate) = @Day
                ORDER BY WrkDate DESC";

            try
            {
                var workPlanData = await ces941Connection.QueryFirstOrDefaultAsync(
                    workPlanQuery,
                    new
                    {
                        WrkPlanID = wrkPlanId,
                        Year = workDate.Year,
                        Month = workDate.Month,
                        Day = workDate.Day,
                    }
                );

                if (workPlanData != null)
                {
                    _logger.LogInformation(
                        $"WorkPlan data found - Month: {workPlanData.Month}, Period: {workPlanData.Period}"
                    );
                    return (
                        Convert.ToInt32(workPlanData.Month),
                        Convert.ToInt32(workPlanData.Period),
                        workPlanData.ShiftCode?.ToString(),
                        workPlanData.DayType?.ToString()
                    );
                }
                else
                {
                    _logger.LogWarning(
                        $"No WorkPlan found for WrkPlanID: {wrkPlanId}, using defaults"
                    );
                    return (workDate.Month, 2, wrkPlanId, "N");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to query WorkPlan data: {ex.Message}");
                return (workDate.Month, 2, wrkPlanId, "N");
            }
        }

        /// <summary>
        /// ดึงข้อมูล NextAppr (ผู้อนุมัติถัดไป)
        /// </summary>
        private async Task<string> GetNextApprover(SqlConnection ces941Connection, string bossId)
        {
            // ลองหาจาก vEmployee ก่อน
            var nextApprQuery =
                @"
                SELECT ADUser 
                FROM vEmployee 
                WHERE EmpNo = @BossId";

            try
            {
                var nextApprData = await ces941Connection.QueryFirstOrDefaultAsync(
                    nextApprQuery,
                    new { BossId = bossId }
                );

                if (nextApprData?.ADUser != null)
                {
                    _logger.LogInformation($"Found NextAppr from vEmployee: {nextApprData.ADUser}");
                    return nextApprData.ADUser;
                }

                // ถ้าไม่พบ ให้หาจาก MEmpBasic
                var mempBasicQuery =
                    @"
                    SELECT ADUser 
                    FROM MEmpBasic 
                    WHERE EmpNo = @BossId";

                var mempBasicData = await ces941Connection.QueryFirstOrDefaultAsync(
                    mempBasicQuery,
                    new { BossId = bossId }
                );

                var nextAppr = mempBasicData?.ADUser ?? "adisaks";
                _logger.LogInformation($"Using NextAppr: {nextAppr} (from MEmpBasic or default)");
                return nextAppr;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to query NextAppr data: {ex.Message}");
                return "adisaks";
            }
        }

        /// <summary>
        /// แปลง ActionType และ ActionReason เป็น code
        /// </summary>
        private (string ActionTypeCode, string ActionReasonCode) ConvertActionCodes(
            string actionType,
            string actionReason
        )
        {
            var actionTypeCode = actionType switch
            {
                "Adjust OT" => "A2",
                "Request OT" => "B1",
                _ => "CPS",
            };

            string actionReasonCode;
            if (actionReason.Contains(':'))
            {
                actionReasonCode = actionReason.Split(':')[0].Trim();
                _logger.LogInformation(
                    $"Extracted ActionReason code: '{actionReasonCode}' from '{actionReason}'"
                );
            }
            else
            {
                actionReasonCode = actionReason.Trim();
                _logger.LogInformation($"Using ActionReason as code: '{actionReasonCode}'");
            }

            if (string.IsNullOrEmpty(actionReasonCode))
            {
                actionReasonCode = "OR1";
                _logger.LogWarning("ActionReason code is empty, using default 'OR1'");
            }

            return (actionTypeCode, actionReasonCode);
        }

        /// <summary>
        /// ดึงอีเมลของ NextAppr จากฐานข้อมูล
        /// </summary>
        private async Task<string> GetNextApproverEmail(
            SqlConnection ces941Connection,
            string nextApprId
        )
        {
            try
            {
                var query =
                    @"
                    SELECT TOP 1 
                        Email
                    FROM EmpMaster 
                    WHERE EmpNo = @EmpNo 
                    AND RecordStatus = 'N'";

                var result = await ces941Connection.QueryFirstOrDefaultAsync<dynamic>(
                    query,
                    new { EmpNo = nextApprId }
                );

                if (result != null && !string.IsNullOrEmpty(result.Email?.ToString()))
                {
                    _logger.LogInformation(
                        $"Found email for next approver {nextApprId}: {result.Email}"
                    );
                    return result.Email.ToString();
                }
                else
                {
                    _logger.LogWarning($"No email found for next approver: {nextApprId}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting next approver email: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ส่งอีเมลแจ้งเตือนการอนุมัติ OT ไปยัง NextAppr
        /// </summary>
        private async Task SendOTApprovalEmail(
            string nextApprEmail,
            string otReqNo,
            string personId,
            string personName,
            DateTime workDate,
            DateTime startDateTime,
            DateTime endDateTime,
            decimal calculatedHours,
            string remark
        )
        {
            try
            {
                if (string.IsNullOrEmpty(nextApprEmail))
                {
                    _logger.LogWarning(
                        "No email address provided for next approver, skipping email send"
                    );
                    return;
                }

                using (var smtpClient = new SmtpClient())
                {
                    // ตั้งค่า SMTP
                    smtpClient.Host = "10.1.15.143";
                    smtpClient.Port = 25;
                    smtpClient.EnableSsl = false; // ปิด SSL สำหรับ port 25
                    smtpClient.UseDefaultCredentials = true;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                    // สร้างข้อความอีเมล
                    var mailMessage = new MailMessage();
                    mailMessage.From = new MailAddress("system@ipsth.com", "ระบบ OT Approval");
                    mailMessage.To.Add(nextApprEmail);
                    mailMessage.Subject = $"คำขออนุมัติ OT - {otReqNo}";
                    mailMessage.IsBodyHtml = true;

                    // สร้างเนื้อหาอีเมล
                    var emailBody =
                        $@"
                        <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <h2 style='color: #2c3e50;'>คำขออนุมัติการทำงานล่วงเวลา (OT)</h2>
                            <hr style='border: 1px solid #ecf0f1;'>
                            
                            <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                                <tr>
                                    <td style='padding: 8px; background-color: #f8f9fa; font-weight: bold; width: 150px;'>เลขที่คำขอ:</td>
                                    <td style='padding: 8px;'>{otReqNo}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; background-color: #f8f9fa; font-weight: bold;'>รหัสพนักงาน:</td>
                                    <td style='padding: 8px;'>{personId}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; background-color: #f8f9fa; font-weight: bold;'>ชื่อพนักงาน:</td>
                                    <td style='padding: 8px;'>{personName}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; background-color: #f8f9fa; font-weight: bold;'>วันที่ทำงาน:</td>
                                    <td style='padding: 8px;'>{workDate:dd/MM/yyyy}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; background-color: #f8f9fa; font-weight: bold;'>เวลาเริ่ม:</td>
                                    <td style='padding: 8px;'>{startDateTime:dd/MM/yyyy HH:mm}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; background-color: #f8f9fa; font-weight: bold;'>เวลาสิ้นสุด:</td>
                                    <td style='padding: 8px;'>{endDateTime:dd/MM/yyyy HH:mm}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; background-color: #f8f9fa; font-weight: bold;'>จำนวนชั่วโมง:</td>
                                    <td style='padding: 8px;'>{calculatedHours:F2} ชั่วโมง</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; background-color: #f8f9fa; font-weight: bold;'>หมายเหตุ:</td>
                                    <td style='padding: 8px;'>{remark ?? "-"}</td>
                                </tr>
                            </table>
                            
                            <p style='color: #7f8c8d; font-size: 14px;'>
                                กรุณาตรวจสอบและอนุมัติคำขอนี้ผ่านระบบ<br>
                                หากมีข้อสงสัยกรุณาติดต่อฝ่ายบุคคล
                            </p>
                            
                            <hr style='border: 1px solid #ecf0f1;'>
                            <p style='color: #95a5a6; font-size: 12px;'>
                                อีเมลนี้ถูกส่งจากระบบอัตโนมัติ กรุณาอย่าตอบกลับ
                            </p>
                        </body>
                        </html>";

                    mailMessage.Body = emailBody;

                    // ส่งอีเมล
                    await smtpClient.SendMailAsync(mailMessage);

                    _logger.LogInformation(
                        $"OT approval email sent successfully to {nextApprEmail} for OT request {otReqNo}"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending OT approval email: {ex.Message}");
                // ไม่ throw exception เพื่อไม่ให้กระทบการบันทึกข้อมูล
            }
        }

        /// <summary>
        /// ตรวจสอบว่ามีการขอโอทีในวันที่นั้นแล้วหรือยัง
        /// </summary>
        private async Task<(bool Exists, string OtReqNo, string ErrorMessage)> CheckExistingOT(
            SqlConnection mainConnection,
            string personId,
            DateTime wrkDate
        )
        {
            try
            {
                var checkQuery =
                    @"
                    SELECT OTReqNo, WrkDate, DteTmeStr, DteTmeEnd, TotalOTHrs, ApprStatus
                    FROM TOTPlan 
                    WHERE EmpNo = @PersonId 
                    AND WrkDate = @WrkDate 
                    AND FlagDel IS NULL
                    ORDER BY CreateDate DESC";

                var existingOT = await mainConnection.QueryFirstOrDefaultAsync<dynamic>(
                    checkQuery,
                    new { PersonId = personId, WrkDate = wrkDate }
                );

                if (existingOT != null)
                {
                    return (true, existingOT.OTReqNo?.ToString() ?? "", "");
                }

                return (false, "", "");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking existing OT: {ex.Message}");
                return (false, "", $"เกิดข้อผิดพลาดในการตรวจสอบข้อมูลโอที: {ex.Message}");
            }
        }

        /// <summary>
        /// บันทึกข้อมูลโอทีลงฐานข้อมูล
        /// </summary>
        private async Task<(bool IsValid, string ErrorMessage, string OtReqNo)> SaveOTData(
            SqlConnection mainConnection,
            dynamic empData,
            string personId,
            DateTime wrkDate,
            DateTime startDateTime,
            DateTime endDateTime,
            string actionTypeCode,
            string actionReasonCode,
            string remark,
            decimal calculatedHours,
            string wrkPlanId,
            int month,
            int period,
            string nextAppr,
            int sequenceNo = 1,
            string providedOtReqNo = null
        )
        {
            // ใช้ OTReqNo ที่ส่งมา หรือสร้างใหม่ถ้าไม่ได้ส่งมา
            var otReqNo =
                providedOtReqNo ?? $"IOT{wrkDate:yyyyMMdd}{DateTime.Now:HHmmss}{sequenceNo:D3}";
            var otSeqNo = 1;
            var caseId = DateTime.Now.ToString("yyyyMMddHHmmss");

            var insertQuery =
                @"
                INSERT INTO TOTPlan (
                    OTReqNo, OTSeqNo, SuspReqNo, WrkDate, DteTmeStr, DteTmeEnd, 
                    EmpNo, BossId, SecCode, CostCenter, ShiftCode, 
                    ActionType, ActionReason, Remark, CaseId, 
                    TotalOTHrs, OTBfrQty, OTBfrRate, OTNrmQty, OTNrmRate, 
                    OTAftQty, OTAftRate, Year, Month, Period, ApprStatus,
                    CreateDate, CreateBy, FlagDel, NextAppr
                ) VALUES (
                    @OTReqNo, @OTSeqNo, @SuspReqNo, @WrkDate, @DteTmeStr, @DteTmeEnd,
                    @EmpNo, @BossId, @SecCode, @CostCenter, @ShiftCode,
                    @ActionType, @ActionReason, @Remark, @CaseId,
                    @TotalOTHrs, @OTBfrQty, @OTBfrRate, @OTNrmQty, @OTNrmRate,
                    @OTAftQty, @OTAftRate, @Year, @Month, @Period, @ApprStatus,
                    @CreateDate, @CreateBy, @FlagDel, @NextAppr
                )";

            // ตรวจสอบและแปลงข้อมูลจาก dynamic object
            var bossId = empData.BossId?.ToString() ?? "10129578";
            var secCode = empData.SecCode?.ToString() ?? "2203";
            var costCenter = empData.CostCenter?.ToString() ?? "7403N";

            _logger.LogInformation(
                $"Converted employee data - BossId: {bossId}, SecCode: {secCode}, CostCenter: {costCenter}"
            );

            var parameters = new
            {
                OTReqNo = otReqNo,
                OTSeqNo = sequenceNo,
                SuspReqNo = (string)null, // ค่าว่าง
                WrkDate = wrkDate,
                DteTmeStr = startDateTime,
                DteTmeEnd = endDateTime,
                EmpNo = personId,
                BossId = bossId,
                SecCode = secCode,
                CostCenter = costCenter,
                ShiftCode = wrkPlanId,
                ActionType = actionTypeCode,
                ActionReason = actionReasonCode,
                Remark = remark,
                CaseId = caseId,
                TotalOTHrs = calculatedHours,
                OTBfrQty = 0m,
                OTBfrRate = 1.5m,
                OTNrmQty = 0m,
                OTNrmRate = 0m,
                OTAftQty = calculatedHours,
                OTAftRate = 1.5m,
                Year = wrkDate.Year,
                Month = month,
                Period = period,
                ApprStatus = "Pending",
                CreateDate = DateTime.Now,
                CreateBy = HttpContext.Session.GetString("UserName") ?? "System",
                FlagDel = (string)null,
                NextAppr = nextAppr,
            };

            try
            {
                var rowsAffected = await mainConnection.ExecuteAsync(insertQuery, parameters);
                _logger.LogInformation(
                    $"OT data inserted successfully - OTReqNo: {otReqNo}, Rows affected: {rowsAffected}"
                );

                // ส่งอีเมลแจ้งเตือนไปยัง NextAppr หลังจากบันทึกข้อมูลสำเร็จ
                try
                {
                    // ดึงอีเมลของ NextAppr
                    var nextApprEmail = await GetNextApproverEmail(mainConnection, nextAppr);

                    // ดึงชื่อพนักงานจาก empData
                    var personName = empData.EmpName?.ToString() ?? personId;

                    // ส่งอีเมลแจ้งเตือน
                    await SendOTApprovalEmail(
                        nextApprEmail,
                        otReqNo,
                        personId,
                        personName,
                        wrkDate,
                        startDateTime,
                        endDateTime,
                        calculatedHours,
                        remark
                    );
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning($"Failed to send OT approval email: {emailEx.Message}");
                    // ไม่ throw exception เพื่อไม่ให้กระทบการบันทึกข้อมูล
                }

                return (true, "", otReqNo);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Database insert error: {ex.Message}");
                return (false, "ไม่สามารถบันทึกข้อมูลโอทีได้", "");
            }
        }

        /// <summary>
        /// แสดงหน้าทดสอบการส่งอีเมล
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> TestEmail()
        {
            try
            {
                await LoadPermissions("Attendance", "TestEmail");
                return View("~/Views/Attendance/TestEmail.cshtml");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TestEmail action: {ex.Message}");
                return View("~/Views/Attendance/TestEmail.cshtml");
            }
        }

        /// <summary>
        /// ทดสอบการส่งอีเมล SMTP
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> TestEmail(string toEmail = "Adisak.S@liteon.com")
        {
            try
            {
                using (var smtpClient = new SmtpClient())
                {
                    // ตั้งค่า SMTP
                    smtpClient.Host = "10.1.15.143";
                    smtpClient.Port = 25;
                    smtpClient.EnableSsl = false;
                    smtpClient.UseDefaultCredentials = true;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                    // สร้างข้อความอีเมลทดสอบ
                    var mailMessage = new MailMessage();
                    mailMessage.From = new MailAddress("Adisak.S@liteon.com", "ระบบทดสอบ");
                    mailMessage.To.Add(toEmail);
                    mailMessage.Subject = "ทดสอบการส่งอีเมล SMTP";
                    mailMessage.IsBodyHtml = true;
                    mailMessage.Body =
                        @"
                        <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <h2 style='color: #2c3e50;'>ทดสอบการส่งอีเมล</h2>
                            <p>นี่คือการทดสอบการส่งอีเมลผ่าน SMTP Server</p>
                            <p>เวลา: "
                        + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
                        + @"</p>
                            <hr>
                            <p style='color: #95a5a6; font-size: 12px;'>
                                อีเมลนี้ถูกส่งจากระบบทดสอบ
                            </p>
                        </body>
                        </html>";

                    // ส่งอีเมล
                    await smtpClient.SendMailAsync(mailMessage);

                    _logger.LogInformation($"Test email sent successfully to {toEmail}");
                    return Json(new { success = true, message = "ส่งอีเมลทดสอบสำเร็จ" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending test email: {ex.Message}");
                return Json(
                    new { success = false, message = $"ไม่สามารถส่งอีเมลได้: {ex.Message}" }
                );
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOTData(
            string personId = null,
            string startDate = null,
            string endDate = null,
            string status = null
        )
        {
            try
            {
                using (var mainConnection = new SqlConnection(_connectionString))
                using (var ces941Connection = new SqlConnection(_ces941ConnectionString))
                {
                    await mainConnection.OpenAsync();
                    await ces941Connection.OpenAsync();

                    var query =
                        @"
                        SELECT t.*, 
                               CONCAT(COALESCE(m.PrefixName, ''), ' ', COALESCE(m.EmpName, ''), ' ', COALESCE(m.EmpLName, '')) as FullName
                        FROM TOTPlan t
                        LEFT JOIN CES941.dbo.vEmployee m ON t.EmpNo = m.EmpNo
                        WHERE 1=1";

                    var parameters = new DynamicParameters();

                    if (!string.IsNullOrEmpty(personId))
                    {
                        query += " AND t.EmpNo = @PersonId";
                        parameters.Add("@PersonId", personId);
                    }

                    if (!string.IsNullOrEmpty(startDate))
                    {
                        query += " AND CAST(t.WrkDate AS DATE) >= @StartDate";
                        parameters.Add("@StartDate", DateTime.Parse(startDate));
                    }

                    if (!string.IsNullOrEmpty(endDate))
                    {
                        query += " AND CAST(t.WrkDate AS DATE) <= @EndDate";
                        parameters.Add("@EndDate", DateTime.Parse(endDate));
                    }

                    if (!string.IsNullOrEmpty(status))
                    {
                        query += " AND t.ApprStatus = @Status";
                        parameters.Add("@Status", status);
                    }

                    query += " ORDER BY t.CreateDate DESC";

                    var result = await mainConnection.QueryAsync(query, parameters);
                    return Json(new { success = true, data = result });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetOTData: {ex.Message}");
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddShiftForPerson(
            string personId,
            string shift,
            string date,
            string namePerson,
            string remark = "",
            string endDate = null,
            string department = ""
        )
        {
            try
            {
                if (
                    string.IsNullOrEmpty(personId)
                    || string.IsNullOrEmpty(shift)
                    || string.IsNullOrEmpty(date)
                    || string.IsNullOrEmpty(namePerson)
                    || string.IsNullOrEmpty(department)
                )
                {
                    return Json(new { success = false, message = "กรุณาระบุข้อมูลให้ครบถ้วน" });
                }

                // ตัด -1 หรือ -2 ออกจาก personId ถ้ามี
                string basePersonId = personId;
                if (personId.Contains("-"))
                {
                    basePersonId = personId.Split('-')[0];
                }

                using (var connection = new SqlConnection(_sql944ConnectionString))
                using (var connection2 = new SqlConnection(_connectionString))
                {
                    // ตรวจสอบว่ามีข้อมูลพนักงานหรือไม่
                    var employeeSql =
                        @"
                        SELECT TOP 1 name, deptID, deptName 
                        FROM person 
                        WHERE personID LIKE @personIdPattern";

                    var employee = await connection.QueryFirstOrDefaultAsync<dynamic>(
                        employeeSql,
                        new { personIdPattern = basePersonId + "%" }
                    );

                    if (employee == null)
                    {
                        return Json(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });
                    }

                    // ตรวจสอบว่ามีกะในวันที่เลือกแล้วหรือไม่
                    var checkSql =
                        @"
                        SELECT COUNT(1) 
                        FROM emp_person_shift 
                        WHERE personID = @personID 
                        AND date = @date
                        AND deptID = @department";

                    var exists = await connection2.QueryFirstOrDefaultAsync<int>(
                        checkSql,
                        new
                        {
                            personID = basePersonId,
                            date = date,
                            department = department,
                        }
                    );

                    if (exists > 0)
                    {
                        return Json(new { success = false, message = "มีกะในวันที่เลือกแล้ว" });
                    }

                    // เพิ่มกะใหม่
                    var insertSql =
                        @"
                        INSERT INTO emp_person_shift (
                            personID, date, shiftMent, 
                            deptID, deptName, deptCode,
                            name, isActive, remark
                        )
                        VALUES (
                            @personID, @date, @shiftMent,
                            @deptID, @deptName, @deptCode, 
                            @name, @isActive, @remark
                        )";

                    await connection2.ExecuteAsync(
                        insertSql,
                        new
                        {
                            personID = basePersonId,
                            date = date,
                            shiftMent = shift,
                            deptID = department,
                            deptName = employee.deptName,
                            deptCode = department,
                            name = namePerson,
                            isActive = 1,
                            remark = remark,
                        }
                    );

                    // ถ้ามีการระบุวันที่สิ้นสุด ให้ทำการเพิ่มกะสำหรับทุกวันในช่วงเวลา
                    if (!string.IsNullOrEmpty(endDate) && endDate != date)
                    {
                        DateTime startDate = DateTime.Parse(date);
                        DateTime end = DateTime.Parse(endDate);

                        // ตรวจสอบว่าวันที่สิ้นสุดต้องมากกว่าวันที่เริ่มต้น
                        if (end <= startDate)
                        {
                            return Json(
                                new
                                {
                                    success = false,
                                    message = "วันที่สิ้นสุดต้องมากกว่าวันที่เริ่มต้น",
                                }
                            );
                        }

                        // เพิ่มกะสำหรับวันถัดไปจนถึงวันสิ้นสุด
                        for (
                            DateTime currentDate = startDate.AddDays(1);
                            currentDate <= end;
                            currentDate = currentDate.AddDays(1)
                        )
                        {
                            string currentDateStr = currentDate.ToString("yyyy-MM-dd");

                            // ตรวจสอบว่ามีกะในวันนั้นแล้วหรือไม่
                            var existsOnDate = await connection2.QueryFirstOrDefaultAsync<int>(
                                checkSql,
                                new
                                {
                                    personID = basePersonId,
                                    date = currentDateStr,
                                    department = department,
                                }
                            );

                            if (existsOnDate == 0)
                            {
                                await connection2.ExecuteAsync(
                                    insertSql,
                                    new
                                    {
                                        personID = basePersonId,
                                        date = currentDateStr,
                                        shiftMent = shift,
                                        deptID = department,
                                        deptName = employee.deptName,
                                        deptCode = department,
                                        name = namePerson,
                                        isActive = 1,
                                        remark = remark,
                                    }
                                );
                            }
                        }
                    }

                    return Json(new { success = true, message = "เพิ่มกะการทำงานสำเร็จ" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in AddShiftForPerson: {ex.Message}");
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }
    }
}
