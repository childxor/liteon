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

            // ตรวจสอบว่าเป็น AJAX request หรือไม่
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, language = lang.ToLower() });
            }

            // ถ้าไม่ใช่ AJAX request ให้ redirect กลับไปหน้าที่เรียกมา
            string returnUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // ถ้าไม่มี Referer ให้ redirect กลับไปหน้าเดิมที่เรียกมา
            // หรือถ้าเป็น AJAX request ให้ return JSON
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, language = lang.ToLower() });
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
                    .Take(10) // แสดงเฉพาะ 10 คนล่าสุด
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
            model.createdat = DateTime.Now;
            _context.sys_user_feedback_comment.Add(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetAccessErrorStats()
        {
            try 
            {
                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    await connection.OpenAsync();

                    // โหลดข้อมูล Person ทั้งหมดก่อน (ลดจำนวนลงเพื่อทดสอบ) 
                    var personQuery =
                        @"
                        SELECT 
                            rowAutoID, cardNumber, serialNumber, personID, name, englishName, sex, birthday, 
                            identityType, identityID, deptID, deptName, inID, inName, jobLevelID, jobPositionID, 
                            category, educational, nation, place, people, specialty, inaugurationDate, leaveJobDate, 
                            enableDate, disableDate, HealthStatus, interest, introducer, salaryCategory, email, 
                            address, phone, zip, registerAddress, registerPhone, registerZip, school, department, 
                            urgentContact, urgentPhone, marriage, spouse, spousePhone, userID, userLevel, password, 
                            superPassword, modifyTime, operator, reserve1, reserve2, reserve3, reserve4, reserveChar1, 
                            reserveChar2, reserveChar3, reserveChar4, reserveChar5, reserveChar6, reserveChar7, 
                            reserveChar8, reserveInt1, reserveInt2, reserveInt3, reserveInt4, reserveInt5, reserveInt6, 
                            reserveInt7, reserveInt8, pinyin, cardType, cardTypeDesc, subSystem, useCategory, useStatus, 
                            note, groupID, timeGroup, status, cardCategory, cardStatus, eatStatus, freeNumber, ATT_Free, 
                            cardNumberSP1, cardNumberSP2, cardNumberSP3, cardNumberSP4, cardNumberSP5, cardNumberSP6, 
                            FaceUserID, CREATEDATE
                        FROM Person 
                        WHERE personID LIKE '%-2%'
                        ORDER BY name";

                    // test hotreload 

                    _logger.LogInformation("Executing Person query...");
                    var persons = await connection.QueryAsync(personQuery);
                    _logger.LogInformation(
                        "Person query completed. Found {Count} persons",
                        persons.Count()
                    );

                    var personDict = persons.ToDictionary(p => p.cardNumber?.ToString(), p => p);
                    _logger.LogInformation(
                        "Person dictionary created with {Count} entries",
                        personDict.Count
                    );

                    // โหลดข้อมูล PubEvent ที่มี personName เป็น NULL
                    var eventQuery =
                        @"
                        SELECT TOP 100
                            rowAutoID, eventType, eventTime, eventName, eventCode, eventCard, 
                            personID, personName, deptID, deptName, deptCode, deviceID, deviceName, 
                            deviceType, doorName, deviceL1ID, deviceL1Name, deviceL1Type, 
                            deviceL2ID, deviceL2Name, deviceL2Type, deviceL3ID, deviceL3Name, 
                            tag, reserve1, reserve2, reserve3, reserve4, systemType, sourcePK, 
                            systemName, InOut, EmailAlarmSend, cctvUpdate, extend1, NewEventCode_Id, 
                            NewEventCode_Name, NewEventCode_Type, Temperature, DeductAmount, 
                            PreviousBalance, NowBalance, DeductType
                        FROM PubEvent
                        WHERE personName IS NULL
                        ORDER BY eventTime DESC";

                    _logger.LogInformation("Executing PubEvent query...");
                    var events = await connection.QueryAsync(eventQuery);
                    _logger.LogInformation(
                        "PubEvent query completed. Found {Count} events",
                        events.Count()
                    );

                    // รวมข้อมูล Person กับ Event
                    _logger.LogInformation("Starting to enrich events with person data...");
                    var enrichedEvents = new List<dynamic>();
                    foreach (var evt in events)
                    {
                        var eventCard = evt.eventCard?.ToString();
                        var person = personDict.ContainsKey(eventCard)
                            ? personDict[eventCard]
                            : null;

                        var enrichedEvent = new
                        {
                            // ข้อมูลจาก PubEvent
                            rowAutoID = evt.rowAutoID,
                            eventType = evt.eventType,
                            eventTime = evt.eventTime,
                            eventName = evt.eventName,
                            eventCode = evt.eventCode,
                            eventCard = evt.eventCard,
                            personID = evt.personID,
                            personName = evt.personName,
                            deptID = evt.deptID,
                            deptName = evt.deptName,
                            deptCode = evt.deptCode,
                            deviceID = evt.deviceID,
                            deviceName = evt.deviceName,
                            deviceType = evt.deviceType,
                            doorName = evt.doorName,
                            deviceL1ID = evt.deviceL1ID,
                            deviceL1Name = evt.deviceL1Name,
                            deviceL1Type = evt.deviceL1Type,
                            deviceL2ID = evt.deviceL2ID,
                            deviceL2Name = evt.deviceL2Name,
                            deviceL2Type = evt.deviceL2Type,
                            deviceL3ID = evt.deviceL3ID,
                            deviceL3Name = evt.deviceL3Name,
                            tag = evt.tag,
                            reserve1 = evt.reserve1,
                            reserve2 = evt.reserve2,
                            reserve3 = evt.reserve3,
                            reserve4 = evt.reserve4,
                            systemType = evt.systemType,
                            sourcePK = evt.sourcePK,
                            systemName = evt.systemName,
                            InOut = evt.InOut,
                            EmailAlarmSend = evt.EmailAlarmSend,
                            cctvUpdate = evt.cctvUpdate,
                            extend1 = evt.extend1,
                            NewEventCode_Id = evt.NewEventCode_Id,
                            NewEventCode_Name = evt.NewEventCode_Name,
                            NewEventCode_Type = evt.NewEventCode_Type,
                            Temperature = evt.Temperature,
                            DeductAmount = evt.DeductAmount,
                            PreviousBalance = evt.PreviousBalance,
                            NowBalance = evt.NowBalance,
                            DeductType = evt.DeductType,

                            // ข้อมูลจาก Person (ถ้ามี)
                            personNameFromCard = person?.name,
                            personEnglishName = person?.englishName,
                            personDeptName = person?.deptName,
                            personPhone = person?.phone,
                            personEmail = person?.email,
                            personStatus = person?.status,
                            personCardStatus = person?.cardStatus,
                            personEnableDate = person?.enableDate,
                            personDisableDate = person?.disableDate,
                        };

                        enrichedEvents.Add(enrichedEvent);
                    }

                    _logger.LogInformation(
                        "Enrichment completed. Returning {Count} enriched events",
                        enrichedEvents.Count
                    );
                    return Json(
                        new
                        {
                            success = true,
                            data = enrichedEvents,
                            totalCount = enrichedEvents.Count,
                            personCount = persons.Count(),
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting access error stats from SQL944. Connection string: {ConnectionString}",
                    _sql944ConnectionString?.Substring(
                        0,
                        Math.Min(50, _sql944ConnectionString?.Length ?? 0)
                    )
                );
                return Json(
                    new
                    {
                        success = false,
                        message = "เกิดข้อผิดพลาดในการดึงข้อมูลสถานะผิดพลาด",
                        error = ex.Message,
                        details = ex.ToString(),
                    }
                );
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestSQL944Connection()
        {
            try
            {
                _logger.LogInformation("Testing SQL944 connection...");

                if (string.IsNullOrEmpty(_sql944ConnectionString))
                {
                    return Json(
                        new
                        {
                            success = false,
                            message = "SQL944 connection string is null or empty",
                        }
                    );
                }

                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    await connection.OpenAsync();
                    _logger.LogInformation("Successfully connected to SQL944");

                    // ทดสอบ query ง่ายๆ
                    var testQuery = "SELECT COUNT(*) as Count FROM Person";
                    var result = await connection.QueryFirstAsync<int>(testQuery);

                    return Json(
                        new
                        {
                            success = true,
                            message = "เชื่อมต่อสำเร็จ",
                            personCount = result,
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing SQL944 connection");
                return Json(
                    new
                    {
                        success = false,
                        message = "เกิดข้อผิดพลาดในการเชื่อมต่อ",
                        error = ex.Message,
                    }
                );
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPhoneList()
        {
            try
            {
                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    await connection.OpenAsync();

                    var sql =
                        @"
                        SELECT TOP (200)
                            personID,
                            name,
                            deptName,
                            phone,
                            cardNumber
                        FROM Person
                        WHERE personID LIKE '%-2%'
                          AND phone IS NOT NULL
                          AND LTRIM(RTRIM(phone)) <> ''
                        ORDER BY name";

                    var data = await connection.QueryAsync(sql);
                    var count = data?.Count() ?? 0;
                    return Json(
                        new
                        {
                            success = true,
                            count,
                            data,
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching phone list from SQL944");
                return Json(
                    new
                    {
                        success = false,
                        message = "เกิดข้อผิดพลาดในการดึงข้อมูลเบอร์โทรศัพท์",
                        error = ex.Message,
                    }
                );
            }
        }

        [HttpGet]
        public async Task<IActionResult> FindPersonForPhone(string personId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(personId))
                {
                    return Json(new { success = false, message = "กรุณาระบุ PersonID" });
                }

                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    await connection.OpenAsync();

                    var sql =
                        @"
                        SELECT TOP 1 personID, name, deptName, phone, cardNumber
                        FROM Person
                        WHERE personID = @pid and personid not like '%-1%'";

                    var data = await connection.QueryFirstOrDefaultAsync(
                        sql,
                        new { pid = personId }
                    );

                    if (data == null)
                    {
                        var altId = personId.Contains('-') ? personId : personId + "-2";
                        data = await connection.QueryFirstOrDefaultAsync(sql, new { pid = altId });
                    }

                    if (data == null)
                    {
                        return Json(new { success = false, message = "ไม่พบชื่อ" });
                    }

                    return Json(new { success = true, data });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding person for phone");
                return Json(
                    new
                    {
                        success = false,
                        message = "เกิดข้อผิดพลาดในการค้นหา",
                        error = ex.Message,
                    }
                );
            }
        }

        public class UpdatePhoneRequest
        {
            public string personId { get; set; } = string.Empty;
            public string phone { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePhone([FromBody] UpdatePhoneRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.personId))
                {
                    return Json(new { success = false, message = "ข้อมูลไม่ถูกต้อง" });
                }

                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    await connection.OpenAsync();

                    var sql =
                        @"
                        UPDATE Person SET phone = @phone
                        WHERE personID = @personID;
                        SELECT personID, name, deptName, phone, cardNumber
                        FROM Person WHERE personID = @personID";

                    var data = await connection.QueryFirstOrDefaultAsync(
                        sql,
                        new { personID = req.personId, phone = (object?)req.phone ?? DBNull.Value }
                    );

                    if (data == null)
                    {
                        // ลองด้วย personID + '-2' กรณีไม่พบ
                        var altId = req.personId.Contains('-') ? req.personId : req.personId + "-2";
                        data = await connection.QueryFirstOrDefaultAsync(
                            sql,
                            new { personID = altId, phone = (object?)req.phone ?? DBNull.Value }
                        );
                    }

                    if (data == null)
                    {
                        return Json(new { success = false, message = "ไม่พบชื่อ" });
                    }

                    return Json(new { success = true, data });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating phone");
                return Json(
                    new
                    {
                        success = false,
                        message = "เกิดข้อผิดพลาดในการบันทึก",
                        error = ex.Message,
                    }
                );
            }
        }

        public class ClearPhoneRequest
        {
            public string personId { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> ClearPhone([FromBody] ClearPhoneRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.personId))
                {
                    return Json(new { success = false, message = "ข้อมูลไม่ถูกต้อง" });
                }

                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    await connection.OpenAsync();

                    var sql =
                        @"
                        UPDATE Person SET phone = NULL
                        WHERE personID = @personID;
                        SELECT personID, name, deptName, phone, cardNumber
                        FROM Person WHERE personID = @personID";

                    var data = await connection.QueryFirstOrDefaultAsync(
                        sql,
                        new { personID = req.personId }
                    );
                    if (data == null)
                    {
                        var altId = req.personId.Contains('-') ? req.personId : req.personId + "-2";
                        data = await connection.QueryFirstOrDefaultAsync(
                            sql,
                            new { personID = altId }
                        );
                    }

                    if (data == null)
                    {
                        return Json(new { success = false, message = "ไม่พบชื่อ" });
                    }

                    return Json(new { success = true, data });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing phone");
                return Json(
                    new
                    {
                        success = false,
                        message = "เกิดข้อผิดพลาดในการลบข้อมูล",
                        error = ex.Message,
                    }
                );
            }
        }
    }
}
