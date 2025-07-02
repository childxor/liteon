using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Text.Encodings.Web; 
using System.Threading.Tasks;
using Dapper;
using IPS_TH.Data;
using IPS_TH.Extensions;
using IPS_TH.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace IPS_TH.Controllers.Home
{
    public class HomeController : BaseController
    { 
        private readonly string _connectionString;
        private readonly string _hrIpsConnectionString;
        private readonly IConfiguration _configuration;
        private readonly string _sql944ConnectionString;
        private readonly string _ces941ConnectionString;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IConfiguration configuration,
            ApplicationDbContext context,
            ILogger<HomeController> logger
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
        public IActionResult Index()
        {
            var userData = GetUserData();
            var segment = Request.Path.Value.Split('/').Last();
            ViewBag.ModuleId = segment;

            // เริ่มต้นภาษาถ้ายังไม่ได้ตั้งค่า
            HttpContext.Session.InitializeLanguage();
            ViewBag.username = Microsoft.AspNetCore.Http.SessionExtensions.GetString(
                HttpContext.Session,
                "EmployeeName"
            );

            // เช็คสิทธิ์ admin
            var isAdmin = Microsoft.AspNetCore.Http.SessionExtensions.GetString(
                HttpContext.Session,
                "IsAdmin"
            );
            ViewBag.IsAdmin = isAdmin?.ToLower() == "true" ? "true" : "false";

            return View(userData);
        }

        [HttpGet]
        public IActionResult ChangeLanguage(string lang)
        {
            // ตรวจสอบว่ารหัสภาษาที่ได้รับเป็นภาษาที่รองรับหรือไม่
            var supportedLanguages = new[] { "th", "en", "zh" };
            if (string.IsNullOrEmpty(lang) || !supportedLanguages.Contains(lang.ToLower()))
            {
                lang = "th"; // ใช้ภาษาไทยเป็นค่าเริ่มต้น
            }

            try
            {
                // เปลี่ยนภาษาใน Session
                HttpContext.Session.SetLanguage(lang.ToLower());

                // สร้างข้อความแจ้งเตือนตามภาษาที่เลือก
                string successMessage = lang.ToLower() switch
                {
                    "en" => "Language changed to English successfully!",
                    "zh" => "语言已成功更改为中文！",
                    _ => "เปลี่ยนภาษาเป็นภาษาไทยเรียบร้อยแล้ว!",
                };

                TempData["SuccessMessage"] = successMessage;
                ViewBag.username = Microsoft.AspNetCore.Http.SessionExtensions.GetString(
                    HttpContext.Session,
                    "EmployeeName"
                );
            }
            catch (Exception ex)
            {
                // หากเกิดข้อผิดพลาด
                _logger.LogError($"Error changing language to {lang}: {ex.Message}");
                TempData["ErrorMessage"] =
                    "เกิดข้อผิดพลาดในการเปลี่ยนภาษา / Error changing language";
            }

            // Redirect กลับไปหน้าที่เรียกมา หรือไปหน้าหลักถ้าไม่ทราบที่มา
            string returnUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetUserData()
        {
            var userData = (
                from user in _context.sys_user
                join userRole in _context.sys_user_role on user.Id equals userRole.UserId
                join role in _context.sys_role on userRole.RoleId equals role.Id
                select new
                {
                    user.Id,
                    user.Username,
                    user.PasswordDefault,
                    user.Password,
                    user.PasswordHash,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.PhoneNumber,
                    user.Address,
                    user.Roles,
                    user.CreatedBy,
                    user.UpdatedBy,
                    user.ProfilePictureUrl,
                    user.CreatedDate,
                    user.UpdatedDate,
                    user.IsActive,
                    user.IsEmailVerified,
                    user.LastLogin,
                    RoleName = role.RoleName,
                }
            ).Take(200).ToList();

            bool isAdmin = userData.Any(u =>
                u.RoleName.Equals("admin", StringComparison.OrdinalIgnoreCase)
            );

            return Json(new { userData, isAdmin });
        }

        [HttpGet]
        public async Task<IActionResult> GetModuleMenu()
        {
            try
            {
                string html = await RenderViewComponentToStringAsync("TopbarModuleMenu");
                return Json(new { success = true, html = html });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        private async Task<string> RenderViewComponentToStringAsync(
            string componentName,
            object parameters = null
        )
        {
            using (var sw = new StringWriter())
            {
                var actionContext = new ActionContext(
                    HttpContext,
                    RouteData,
                    ControllerContext.ActionDescriptor
                );
                var viewContext = new ViewContext(
                    actionContext,
                    new EmptyView(),
                    ViewData,
                    TempData,
                    sw,
                    new HtmlHelperOptions()
                );

                var viewComponentHelper =
                    HttpContext.RequestServices.GetRequiredService<IViewComponentHelper>();
                (viewComponentHelper as IViewContextAware)?.Contextualize(viewContext);

                IHtmlContent result =
                    parameters == null
                        ? await viewComponentHelper.InvokeAsync(componentName)
                        : await viewComponentHelper.InvokeAsync(componentName, parameters);

                result.WriteTo(sw, HtmlEncoder.Default);
                return sw.ToString();
            }
        }

        private async Task<List<dynamic>> GetEmployeesFromCES941()
        {
            try
            {
                using (IDbConnection sql944Connection = new SqlConnection(_sql944ConnectionString))
                using (IDbConnection db = new SqlConnection(_ces941ConnectionString))
                {
                    string personCES941 =
                        @"
                        SELECT 
                            Language, EmpNo, PrefixName, EmpName, EmpLName, Gender, 
                            EmpStartDate, EmpResignDate, LastDateAtten, AcceptRehired, 
                            ProbationDay, ProbationPass, ProbationDate, ContractNo, 
                            ContAdd, ContCity, ContZipCode, ContZipDesc, ContCountry, 
                            RegAdd, RegCity, RegZipCode, RegZipDesc, RegCountry, 
                            HomePhone, MobilePhone, Internet, eMailAdd, ADUser, 
                            R3User, DIHUser, LotusUser, SkyUser, RNewUser, BirthDate, 
                            BirthCity, BirthCountry, Nation, Nationality, Religion, 
                            BankID, BankAccount, PersonalID, PassPort, TaxID, SFID, 
                            Hospital, CarLicense, BikeLicense, Marital, MilitaryPass, 
                            EmpPicture, DocLink, PreEmpNo, ChangeDate, ChangeBy, 
                            FlagDel, AdminAction, ResignAlertDate, ResignKeyDate, Ext, ADPath
                        FROM MEmpBasic
                        WHERE Language = 'EN'";

                    string personSQL944 =
                        @"
                        SELECT DISTINCT 
                            personID, 
                            deptID, 
                            deptName, 
                            cardNumber,
                            CASE 
                                WHEN CHARINDEX('-', personID) > 0 
                                THEN LEFT(personID, CHARINDEX('-', personID) - 1)
                                ELSE personID 
                            END as basePersonID
                        FROM Person
                        WHERE deptID IS NOT NULL 
                        AND deptName IS NOT NULL 
                        AND cardNumber IS NOT NULL
                        AND LEN(TRIM(deptName)) > 0";

                    var employees = await db.QueryAsync<dynamic>(personCES941);
                    var departments = await sql944Connection.QueryAsync<dynamic>(personSQL944);

                    if (employees == null || departments == null)
                    {
                        _logger.LogWarning("ไม่พบข้อมูลพนักงานหรือแผนก");
                        return new List<dynamic>();
                    }

                    var deptByCardNumber = new Dictionary<string, string>();
                    var deptByPersonID = new Dictionary<string, string>();
                    var deptByBasePersonID = new Dictionary<string, string>();

                    foreach (var dept in departments)
                    {
                        try
                        {
                            if (dept == null)
                                continue;

                            string cardNumber = dept.cardNumber?.ToString()?.Trim();
                            string personID = dept.personID?.ToString()?.Trim();
                            string basePersonID = dept.basePersonID?.ToString()?.Trim();
                            string deptName = dept.deptName?.ToString()?.Trim();

                            if (!string.IsNullOrEmpty(deptName))
                            {
                                if (!string.IsNullOrEmpty(cardNumber))
                                    deptByCardNumber[cardNumber] = deptName;

                                if (!string.IsNullOrEmpty(personID))
                                    deptByPersonID[personID] = deptName;

                                if (!string.IsNullOrEmpty(basePersonID))
                                    deptByBasePersonID[basePersonID] = deptName;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                $"เกิดข้อผิดพลาดในการประมวลผลข้อมูลแผนก: {ex.Message}"
                            );
                            continue;
                        }
                    }

                    var employeeList = new List<dynamic>();
                    if (employees != null)
                    {
                        foreach (var employee in employees)
                        {
                            try
                            {
                                if (employee == null)
                                    continue;

                                // แปลง dynamic object เป็น dictionary เพื่อความปลอดภัย
                                var employeeProps = employee as IDictionary<string, object>;
                                if (employeeProps == null)
                                {
                                    // ถ้าไม่สามารถแปลงเป็น dictionary ได้ ให้ข้าม
                                    continue;
                                }

                                // ตรวจสอบ EmpNo อย่างปลอดภัย
                                object empNoObj = null;
                                if (
                                    !employeeProps.TryGetValue("EmpNo", out empNoObj)
                                    || empNoObj == null
                                )
                                {
                                    continue;
                                }

                                string empNo = empNoObj.ToString()?.Trim();
                                if (string.IsNullOrEmpty(empNo))
                                    continue;

                                var expandoEmployee = new System.Dynamic.ExpandoObject();
                                var employeeDict = (IDictionary<string, object>)expandoEmployee;

                                // คัดลอกข้อมูลทั้งหมดจาก employeeProps
                                foreach (var kvp in employeeProps)
                                {
                                    employeeDict[kvp.Key] = kvp.Value ?? DBNull.Value;
                                }

                                // หาแผนก
                                string deptName = null;
                                if (
                                    deptByCardNumber.TryGetValue(empNo, out deptName)
                                    || deptByBasePersonID.TryGetValue(empNo, out deptName)
                                    || deptByPersonID.TryGetValue(empNo, out deptName)
                                    || deptByPersonID.TryGetValue($"{empNo}-2", out deptName)
                                )
                                {
                                    employeeDict["deptName"] = deptName;
                                }
                                else
                                {
                                    employeeDict["deptName"] = "ไม่ระบุแผนก";
                                }

                                employeeList.Add(expandoEmployee);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(
                                    $"เกิดข้อผิดพลาดในการประมวลผลข้อมูลพนักงาน: {ex.Message}"
                                );
                                continue;
                            }
                        }
                    }

                    return employeeList;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching employees from CES941: {ex.Message}");
                return new List<dynamic>();
            }
        }

        // Helper method เพื่อดึงข้อมูลแผนกจากพนักงาน
        private string GetEmployeeDepartment(dynamic employee)
        {
            try
            {
                if (employee == null)
                    return "ไม่ระบุแผนก";

                var employeeDict = employee as IDictionary<string, object>;
                if (employeeDict != null && employeeDict.ContainsKey("deptName"))
                {
                    var deptValue = employeeDict["deptName"];
                    return deptValue?.ToString() ?? "ไม่ระบุแผนก";
                }
                return "ไม่ระบุแผนก";
            }
            catch (Exception ex)
            {
                _logger.LogError($"เกิดข้อผิดพลาดในการดึงข้อมูลแผนก: {ex.Message}");
                return "ไม่ระบุแผนก";
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeStats()
        {
            try
            {
                var ces941Employees = await GetEmployeesFromCES941();

                var today = DateTime.Today;
                var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

                int totalEmployees = ces941Employees.Count;

                int activeEmployees = ces941Employees.Count(e =>
                {
                    var eDict = e as IDictionary<string, object>;
                    if (eDict == null)
                        return false;
                    eDict.TryGetValue("EmpResignDate", out object resignDateObj);
                    return resignDateObj == null || resignDateObj == DBNull.Value;
                });

                int newEmployees = ces941Employees.Count(e =>
                {
                    var eDict = e as IDictionary<string, object>;
                    if (eDict == null)
                        return false;

                    eDict.TryGetValue("EmpStartDate", out object startDateObj);
                    if (startDateObj == null || startDateObj == DBNull.Value)
                        return false;

                    if (DateTime.TryParse(startDateObj.ToString(), out DateTime startDate))
                        return startDate >= firstDayOfMonth;
                    return false;
                });

                int resignedEmployees = ces941Employees.Count(e =>
                {
                    var eDict = e as IDictionary<string, object>;
                    if (eDict == null)
                        return false;

                    eDict.TryGetValue("EmpResignDate", out object resignDateObj);
                    if (resignDateObj == null || resignDateObj == DBNull.Value)
                        return false;

                    if (DateTime.TryParse(resignDateObj.ToString(), out DateTime resignDate))
                        return resignDate >= firstDayOfMonth;
                    return false;
                });

                var birthdayEmployees = ces941Employees
                    .Where(e =>
                    {
                        var eDict = e as IDictionary<string, object>;
                        if (eDict == null)
                            return false;

                        eDict.TryGetValue("BirthDate", out object birthDateObj);
                        eDict.TryGetValue("EmpResignDate", out object resignDateObj);

                        if (
                            birthDateObj == null
                            || birthDateObj == DBNull.Value
                            || (resignDateObj != null && resignDateObj != DBNull.Value)
                        )
                            return false;

                        if (!DateTime.TryParse(birthDateObj.ToString(), out DateTime birthDate))
                            return false;

                        var birthDateThisYear = new DateTime(
                            today.Year,
                            birthDate.Month,
                            birthDate.Day
                        );

                        if (birthDateThisYear < today)
                            birthDateThisYear = birthDateThisYear.AddYears(1);

                        var daysUntilBirthday = (int)(birthDateThisYear - today).TotalDays;
                        return daysUntilBirthday >= -15 && daysUntilBirthday <= 15;
                    })
                    .Select(e =>
                    {
                        var eDict = e as IDictionary<string, object>;
                        eDict.TryGetValue("BirthDate", out object birthDateObj);

                        if (!DateTime.TryParse(birthDateObj.ToString(), out DateTime birthDate))
                            return null;

                        var age = today.Year - birthDate.Year;

                        if (
                            today.Month < birthDate.Month
                            || (today.Month == birthDate.Month && today.Day < birthDate.Day)
                        )
                            age--;

                        var birthDateThisYear = new DateTime(
                            today.Year,
                            birthDate.Month,
                            birthDate.Day
                        );

                        if (birthDateThisYear < today)
                            birthDateThisYear = birthDateThisYear.AddYears(1);

                        var daysUntilBirthday = (int)(birthDateThisYear - today).TotalDays;

                        string department = GetEmployeeDepartment(e);

                        eDict.TryGetValue("PrefixName", out object prefixNameObj);
                        eDict.TryGetValue("EmpName", out object empNameObj);
                        eDict.TryGetValue("EmpLName", out object empLNameObj);

                        var prefixName = prefixNameObj?.ToString() ?? "";
                        var empName = empNameObj?.ToString() ?? "";
                        var empLName = empLNameObj?.ToString() ?? "";

                        return new
                        {
                            name = $"{prefixName} {empName} {empLName}".Trim(),
                            department = department,
                            age = age,
                            birthDate = birthDate.ToString("dd MMM"),
                            daysUntilBirthday = daysUntilBirthday,
                            isPast = daysUntilBirthday < 0,
                        };
                    })
                    .Where(e => e != null)
                    .OrderBy(e => e.daysUntilBirthday)
                    .ToList();

                var startTodayEmployees = ces941Employees
                    .Where(e =>
                    {
                        var eDict = e as IDictionary<string, object>;
                        if (eDict == null)
                            return false;

                        eDict.TryGetValue("EmpStartDate", out object startDateObj);
                        if (startDateObj == null || startDateObj == DBNull.Value)
                            return false;

                        if (DateTime.TryParse(startDateObj.ToString(), out DateTime startDate))
                            return startDate.Date == today.Date;
                        return false;
                    })
                    .Select(e =>
                    {
                        var eDict = e as IDictionary<string, object>;
                        string department = GetEmployeeDepartment(e);

                        eDict.TryGetValue("PrefixName", out object prefixNameObj);
                        eDict.TryGetValue("EmpName", out object empNameObj);
                        eDict.TryGetValue("EmpLName", out object empLNameObj);

                        var prefixName = prefixNameObj?.ToString() ?? "";
                        var empName = empNameObj?.ToString() ?? "";
                        var empLName = empLNameObj?.ToString() ?? "";

                        return new
                        {
                            name = $"{prefixName} {empName} {empLName}".Trim(),
                            department = department,
                        };
                    })
                    .ToList();

                // สร้างข้อมูลสถิติแผนก - ใช้ helper method
                var departmentStats = ces941Employees
                    .GroupBy(e => GetEmployeeDepartment(e))
                    .Select(g => new
                    {
                        department = g.Key,
                        totalCount = g.Count(),
                        maleCount = g.Count(e =>
                        {
                            var eDict = e as IDictionary<string, object>;
                            if (eDict != null && eDict.TryGetValue("Gender", out object genderObj))
                                return genderObj?.ToString() == "M";
                            return false;
                        }),
                        femaleCount = g.Count(e =>
                        {
                            var eDict = e as IDictionary<string, object>;
                            if (eDict != null && eDict.TryGetValue("Gender", out object genderObj))
                                return genderObj?.ToString() == "F";
                            return false;
                        }),
                    })
                    .OrderByDescending(d => d.totalCount)
                    .ToList();

                // สร้างข้อมูลสถิติรายเดือน
                var monthlyStats = new List<object>();
                for (int i = 12; i >= 0; i--)
                {
                    var month = today.AddMonths(-i);
                    var firstDay = new DateTime(month.Year, month.Month, 1);
                    var lastDay = firstDay.AddMonths(1).AddDays(-1);

                    var newCount = ces941Employees.Count(e =>
                    {
                        var eDict = e as IDictionary<string, object>;
                        if (eDict == null)
                            return false;

                        eDict.TryGetValue("EmpStartDate", out object startDateObj);
                        if (startDateObj == null || startDateObj == DBNull.Value)
                            return false;

                        if (DateTime.TryParse(startDateObj.ToString(), out DateTime startDate))
                            return startDate >= firstDay && startDate <= lastDay;
                        return false;
                    });

                    var resignedCount = ces941Employees.Count(e =>
                    {
                        var eDict = e as IDictionary<string, object>;
                        if (eDict == null)
                            return false;

                        eDict.TryGetValue("EmpResignDate", out object resignDateObj);
                        if (resignDateObj == null || resignDateObj == DBNull.Value)
                            return false;

                        if (DateTime.TryParse(resignDateObj.ToString(), out DateTime resignDate))
                            return resignDate >= firstDay && resignDate <= lastDay;
                        return false;
                    });

                    monthlyStats.Add(
                        new
                        {
                            monthName = month.ToString("MMM yyyy"),
                            newEmployees = newCount,
                            resignedEmployees = resignedCount,
                        }
                    );
                }

                // เพิ่มการดึงข้อมูลพนักงานที่ลาออก 100 คนล่าสุด
                var recentResignedEmployees = ces941Employees
                    .Where(e =>
                    {
                        var eDict = e as IDictionary<string, object>;
                        if (eDict == null)
                            return false;

                        eDict.TryGetValue("EmpResignDate", out object resignDateObj);
                        if (resignDateObj == null || resignDateObj == DBNull.Value)
                            return false;

                        if (DateTime.TryParse(resignDateObj.ToString(), out DateTime resignDate))
                            return resignDate.Date <= today.Date;
                        return false;
                    })
                    .Select(e =>
                    {
                        var eDict = e as IDictionary<string, object>;
                        string department = GetEmployeeDepartment(e);

                        eDict.TryGetValue("EmpStartDate", out object startDateObj);
                        eDict.TryGetValue("EmpResignDate", out object resignDateObj);

                        if (
                            !DateTime.TryParse(startDateObj?.ToString(), out DateTime startDate)
                            || !DateTime.TryParse(
                                resignDateObj?.ToString(),
                                out DateTime resignDate
                            )
                        )
                            return null;

                        // คำนวณระยะเวลาการทำงาน
                        var workDuration = resignDate - startDate;

                        // แปลงเป็นปีและเดือน
                        int years = (int)(workDuration.TotalDays / 365.25);
                        int months = (int)((workDuration.TotalDays % 365.25) / 30.44);
                        string workPeriod =
                            years > 0
                                ? $"{years} {HttpContext.Session.GetTranslation("years")} {months} {HttpContext.Session.GetTranslation("months")}"
                                : $"{months} {HttpContext.Session.GetTranslation("months")}";

                        eDict.TryGetValue("PrefixName", out object prefixNameObj);
                        eDict.TryGetValue("EmpName", out object empNameObj);
                        eDict.TryGetValue("EmpLName", out object empLNameObj);

                        var prefixName = prefixNameObj?.ToString() ?? "";
                        var empName = empNameObj?.ToString() ?? "";
                        var empLName = empLNameObj?.ToString() ?? "";

                        return new
                        {
                            name = $"{prefixName} {empName} {empLName}".Trim(),
                            department = department,
                            startDate = startDate.ToString("dd MMM yyyy"),
                            resignDate = resignDate.ToString("dd MMM yyyy"),
                            workPeriod = workPeriod,
                            daysAgo = (int)(today - resignDate).TotalDays,
                            resignDateValue = resignDate, // เพิ่มข้อมูลนี้สำหรับการเรียงลำดับ
                        };
                    })
                    .Where(e => e != null)
                    .OrderByDescending(e => e.resignDateValue) // เรียงลำดับตามวันที่ลาออกล่าสุด
                    .Take(100) // แสดงเฉพาะ 100 คนล่าสุด
                    .ToList();

                // เพิ่ม recentResignedEmployees ในการส่งข้อมูลกลับไปยัง View
                return Json(
                    new
                    {
                        success = true,
                        totalEmployees,
                        activeEmployees,
                        newEmployees,
                        resignedEmployees,
                        birthdayEmployees,
                        startTodayEmployees,
                        recentResignedEmployees,
                        departmentStats,
                        monthlyStats,
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(
                    new
                    {
                        success = false,
                        message = ex.Message,
                        stackTrace = ex.StackTrace,
                    }
                );
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUpcomingHolidays()
        {
            try
            {
                var today = DateTime.Today;
                var thirtyDaysLater = today.AddDays(100);

                // สร้าง HttpClient สำหรับเรียก API
                using (var httpClient = new HttpClient())
                {
                    // ตั้งค่า User-Agent และ Accept header
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                    httpClient.DefaultRequestHeaders.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(
                            "application/json"
                        )
                    );

                    // เรียกข้อมูลวันหยุดจาก myhora.com
                    var response = await httpClient.GetStringAsync(
                        "https://www.myhora.com/calendar/ical/holiday.aspx?latest.json"
                    );
                    var calendarData = JsonConvert.DeserializeObject<dynamic>(response);
                    var holidays = new List<dynamic>();

                    // ตรวจสอบว่ามีข้อมูล VCALENDAR หรือไม่
                    if (calendarData?.VCALENDAR != null)
                    {
                        foreach (var calendar in calendarData.VCALENDAR)
                        {
                            // ตรวจสอบว่ามีข้อมูล VEVENT หรือไม่
                            if (calendar?.VEVENT != null)
                            {
                                foreach (var event_ in calendar.VEVENT)
                                {
                                    try
                                    {
                                        // ตรวจสอบว่ามีข้อมูล DTSTART หรือไม่
                                        var dtstartStr =
                                            event_["DTSTART;VALUE=DATE"] != null
                                                ? event_["DTSTART;VALUE=DATE"].ToString()
                                                : null;
                                        if (!string.IsNullOrEmpty(dtstartStr))
                                        {
                                            // แยกข้อมูลวันที่จาก DTSTART
                                            var dateStr = dtstartStr;
                                            if (dtstartStr.Contains(";VALUE=DATE"))
                                            {
                                                var parts = dtstartStr.Split(';');
                                                foreach (var part in parts)
                                                {
                                                    if (part.StartsWith("VALUE=DATE"))
                                                    {
                                                        dateStr = part.Split('=')[1].Trim('"');
                                                        break;
                                                    }
                                                }
                                            }

                                            if (
                                                DateTime.TryParseExact(
                                                    dateStr,
                                                    "yyyyMMdd",
                                                    null,
                                                    System.Globalization.DateTimeStyles.None,
                                                    out DateTime holidayDate
                                                )
                                            )
                                            {
                                                // ตรวจสอบว่าอยู่ในช่วง 100 วันข้างหน้าหรือไม่
                                                if (
                                                    holidayDate >= today
                                                    && holidayDate <= thirtyDaysLater
                                                )
                                                {
                                                    // ดึงข้อมูลเพิ่มเติม
                                                    var summary =
                                                        event_["SUMMARY"] != null
                                                            ? event_["SUMMARY"].ToString()
                                                            : "วันหยุด";
                                                    var description =
                                                        event_["DESCRIPTION"] != null
                                                            ? event_["DESCRIPTION"].ToString()
                                                            : "วันหยุดราชการ";
                                                    var location =
                                                        event_["LOCATION"] != null
                                                            ? event_["LOCATION"].ToString()
                                                            : "";
                                                    var status =
                                                        event_["STATUS"] != null
                                                            ? event_["STATUS"].ToString()
                                                            : "CONFIRMED";

                                                    holidays.Add(
                                                        new
                                                        {
                                                            holidayDate = holidayDate.ToString(
                                                                "dd MMMM yyyy"
                                                            ),
                                                            daysUntil = (int)
                                                                (holidayDate - today).TotalDays,
                                                            holidayName = summary,
                                                            description = description,
                                                            location = location,
                                                            status = status,
                                                            lunarDate = description.Contains(
                                                                "เดือน"
                                                            )
                                                                ? description
                                                                : "",
                                                        }
                                                    );
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(
                                            ex,
                                            "Error processing holiday event: {Message}",
                                            ex.Message
                                        );
                                        continue; // ข้ามไปยังวันหยุดถัดไป
                                    }
                                }
                            }
                        }
                    }

                    // เรียงลำดับวันหยุดตามจำนวนวันที่จะมาถึง
                    holidays = holidays.OrderBy(h => h.daysUntil).ToList();

                    return Json(new { success = true, holidays = holidays });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming holidays");
                return Json(new { success = false, message = "ไม่สามารถดึงข้อมูลวันหยุดได้" });
            }
        }

        public class EmptyView : IView
        {
            public string Path => string.Empty;

            public Task RenderAsync(ViewContext context)
            {
                return Task.CompletedTask;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFeedbackList()
        {
            var feedbacks = await _context
                .sys_user_feedback.OrderByDescending(f => f.createdat)
                .ToListAsync();

            var feedbackIds = feedbacks.Select(f => f.id).ToList();

            var comments = await _context
                .sys_user_feedback_comment.Where(c => feedbackIds.Contains(c.feedbackid))
                .OrderByDescending(c => c.id)
                .ToListAsync();

            var likes = await _context
                .sys_user_feedback_like.Where(l => feedbackIds.Contains(l.feedbackid))
                .OrderByDescending(l => l.id)
                .ToListAsync();

            // หาจำนวนจาก id ของ feedback
            foreach (var feedback in feedbacks)
            {
                feedback.commentcount = comments.Count(c => c.feedbackid == feedback.id);
                feedback.likecount = likes.Count(l =>
                    l.feedbackid == feedback.id && l.status == "like"
                );
            }

            return Json(
                new
                {
                    success = true,
                    data = feedbacks,
                    comments,
                    likes,
                }
            );
        }

        [HttpPost]
        public async Task<IActionResult> AddFeedback([FromBody] sys_user_feedback model)
        {
            model.createdat = DateTime.Now;
            _context.sys_user_feedback.Add(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> LikeFeedback(
            int feedbackid,
            string username,
            string status
        )
        {
            var like = await _context.sys_user_feedback_like.FirstOrDefaultAsync(l =>
                l.feedbackid == feedbackid && l.username == username
            );

            if (like == null)
            {
                like = new sys_user_feedback_like
                {
                    feedbackid = feedbackid,
                    username = username,
                    status = status,
                };
                _context.sys_user_feedback_like.Add(like);
            }
            else
            {
                like.status = status;
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> AddFeedbackComment(
            [FromBody] sys_user_feedback_comment model
        )
        {
            _context.sys_user_feedback_comment.Add(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
