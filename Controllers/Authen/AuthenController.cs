using System.Diagnostics;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
using System.Text;
using IPS_TH.Controllers.Employee; // เพิ่ม namespace ของ EmployeeController
using IPS_TH.Data; // เพิ่มการนำเข้า ApplicationDbContext
using IPS_TH.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity; // เพิ่มการนำเข้า
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore; // สำหรับ EF Core
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json; // เพิ่มการนำเข้าสำหรับ JsonConvert
using Dapper; // เพิ่มเพื่อใช้งาน QueryAsync/ExecuteAsync ของ Dapper

namespace IPS_TH.Controllers
{
    public class AuthenController : Controller
    {
        private readonly ApplicationDbContext _context; // ประกาศตัวแปร _context

        private readonly IConfiguration _configuration;

        private readonly IPasswordHasher<sys_user> _passwordHasher; // เปลี่ยนเป็น sys_user

        private readonly IWebHostEnvironment _environment;

        private readonly string _sql944ConnectionString;

        private readonly EmployeeController _employeeController;

        private readonly string _defaultConnectionString;

        private readonly string _ces941ConnectionString;
        private readonly string _hrIpsConnectionString;

        // Constructor ที่รับ ApplicationDbContext
        public AuthenController(
            ApplicationDbContext context,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            EmployeeController employeeController
        )
        {
            _context = context; // กำหนดค่าให้ _context
            _configuration = configuration;
            _environment = environment;
            _passwordHasher = new PasswordHasher<sys_user>(); // สร้าง instance ของ PasswordHasher
            _sql944ConnectionString =
                _configuration.GetConnectionString("SQL944")
                ?? _configuration["SQL944ConnectionString"];
            _defaultConnectionString = _configuration.GetConnectionString("DefaultConnection");
            _employeeController = employeeController;
                        _ces941ConnectionString = _configuration.GetConnectionString("CES941");
                        _hrIpsConnectionString = _configuration.GetConnectionString("HR_IPS");
        }

        [HttpGet]
        public IActionResult Login()
        {
            // เช็คก่อนว่ามี session login อยู่มั้ย
            var userData = HttpContext.Session.GetString("UserData");
            if (!string.IsNullOrEmpty(userData))
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {
                // ดึง Windows Identity
                var windowsIdentity = HttpContext.User.Identity;
                if (windowsIdentity != null && windowsIdentity.IsAuthenticated)
                {
                    string[] userParts = windowsIdentity.Name.Split('\\');
                    string currentUser = userParts.Length > 1 ? userParts[1] : windowsIdentity.Name;
                    ViewBag.CurrentWindowsUser = currentUser;

                    // เพิ่ม debug information
                    if (_environment.IsDevelopment())
                    {
                        ViewBag.AuthType = windowsIdentity.AuthenticationType;
                        ViewBag.IsAuthenticated = windowsIdentity.IsAuthenticated;
                        ViewBag.FullIdentityName = windowsIdentity.Name;
                    }
                }
                else if (_environment.IsDevelopment())
                {
                    // ถ้าอยู่ในโหมด Development และไม่มี Windows Identity
                    // ลองใช้ Environment.UserName
                    ViewBag.CurrentWindowsUser = Environment.UserName;
                }
            }
            catch (Exception ex)
            {
                if (_environment.IsDevelopment())
                {
                    ViewBag.AuthError = ex.Message;
                }
            }

            ViewBag.IsAdmin = HttpContext.Session.GetString("IsAdmin") == "true";
            return View();
        }

        public IActionResult Registers()
        {
            return View();
        }

        //function get department from CES941
        public async Task<IActionResult> GetDepartmentsFromCES941(string aduser)
        {
            var departments = new List<Dictionary<string, object>>();
            using (var connection = new SqlConnection(_ces941ConnectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("SELECT ADUser, EmpNo, PrefixName, EmpName, EmpLName, TitleShort FROM vEmployee WHERE Language = 'EN' AND ADUser = @aduser", connection))
                {
                    command.Parameters.AddWithValue("@aduser", aduser);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            departments.Add(row);
                        }
                    }
                }
            }
            return Json(departments);
        }

        // ดึง TitleShort จาก CES941 ตาม ADUser (ใช้ภาษา EN)
        private async Task<string> GetTitleShortFromCES941ByAdUser(string aduser)
        {
            if (string.IsNullOrWhiteSpace(aduser)) return string.Empty;
            using (var connection = new SqlConnection(_ces941ConnectionString))
            {
                var query = "SELECT TOP 1 TitleShort,WorkArea FROM vEmployee WHERE Language = 'EN' AND ADUser = @aduser";
                var result = await connection.QueryAsync<dynamic>(query, new { aduser });
                return result.FirstOrDefault()?.TitleShort ?? string.Empty;
            }
        }

        // ดึงทั้ง TitleShort และ WorkArea เพื่อเก็บใน Session
        private async Task<(string TitleShort, string WorkArea)> GetDeptInfoFromCES941ByAdUser(string aduser)
        {
            if (string.IsNullOrWhiteSpace(aduser)) return (string.Empty, string.Empty);
            using (var connection = new SqlConnection(_ces941ConnectionString))
            {
                var query = "SELECT TOP 1 TitleShort, WorkArea FROM vEmployee WHERE Language = 'EN' AND ADUser = @aduser";
                var result = await connection.QueryAsync<dynamic>(query, new { aduser });
                var row = result.FirstOrDefault();
                return (row?.TitleShort?.ToString() ?? string.Empty, row?.WorkArea?.ToString() ?? string.Empty);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            try
            {
                // ตรวจสอบข้อมูลเบื้องต้น
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    return Json(new { success = false, error = "กรุณากรอกชื่อผู้ใช้และรหัสผ่าน" });

                if (!username.StartsWith("LITEON\\", StringComparison.OrdinalIgnoreCase))
                    return Json(
                        new
                        {
                            success = false,
                            error = "กรุณาใช้ Domain Account เท่านั้น (เช่น LITEON\\username)",
                        }
                    );

                // ตรวจสอบการตั้งค่า AD
                string domainUsername = username.Substring(7);
                string domain = _configuration["ADDomain"];

                if (
                    string.IsNullOrEmpty(_configuration["DefaultActiveDirectoryServer"])
                    || string.IsNullOrEmpty(domain)
                )
                    return Json(
                        new { success = false, error = "ไม่พบการตั้งค่า Active Directory" }
                    );

                // ตรวจสอบ AD และดึงข้อมูลผู้ใช้
                using var adContext = new PrincipalContext(
                    ContextType.Domain,
                    domain,
                    null,
                    ContextOptions.SimpleBind,
                    $"{domainUsername}@{domain}",
                    password
                );

                if (!adContext.ValidateCredentials(domainUsername, password))
                    return Json(
                        new { success = false, error = "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง" }
                    );

                var adUser = UserPrincipal.FindByIdentity(
                    adContext,
                    IdentityType.SamAccountName,
                    domainUsername
                );
                if (adUser == null)
                    return Json(
                        new { success = false, error = "ไม่พบข้อมูลผู้ใช้ใน Active Directory" }
                    );

                // ดึงข้อมูลเพิ่มเติมจาก DirectoryEntry
                var directoryEntry = adUser.GetUnderlyingObject() as DirectoryEntry;
                string department = string.Empty;

                if (directoryEntry != null && directoryEntry.Properties.Contains("department"))
                {
                    department = directoryEntry.Properties["department"].Value?.ToString() ?? "";
                }

                // บันทึก Session
                if (!string.IsNullOrEmpty(adUser.EmployeeId))
                    HttpContext.Session.SetString("EmployeeID", adUser.EmployeeId);
                if (!string.IsNullOrEmpty(adUser.DisplayName))
                    HttpContext.Session.SetString("EmployeeName", adUser.DisplayName);

                // ตรวจสอบหรือสร้างผู้ใช้ในระบบ
                var userAccount = await _context.sys_user.FirstOrDefaultAsync(u =>
                    u.Username == username
                );
                if (userAccount == null)
                {
                    userAccount = new sys_user
                    {
                        Username = username,
                        FirstName = adUser.GivenName ?? domainUsername,
                        LastName = adUser.Surname ?? "",
                        Email = adUser.EmailAddress ?? "",
                        Emp_no = adUser.EmployeeId ?? "",
                        IsActive = true,
                        Roles = "5",
                        CreatedDate = DateTime.Now,
                    };
                    _context.sys_user.Add(userAccount);
                    await _context.SaveChangesAsync();
                }

                // ตรวจสอบสถานะการใช้งาน
                if (!userAccount.IsActive.GetValueOrDefault(true))
                    return Json(
                        new
                        {
                            success = false,
                            error = "บัญชีของคุณถูกระงับการใช้งาน กรุณาติดต่อผู้ดูแลระบบ",
                        }
                    );

                var RolesSplit = userAccount.Roles.Split('|');
                // ดึงข้อมูล Role และตั้งค่า Session
                var roleNames = await _context
                    .sys_role.Where(r => RolesSplit.Contains(r.Id.ToString()))
                    .ToListAsync();

                bool isAdmin = roleNames.Any(r => r.RoleName == "Administrators");

                HttpContext.Session.SetString("UserData", JsonConvert.SerializeObject(userAccount));
                HttpContext.Session.SetString(
                    "Roles",
                    JsonConvert.SerializeObject(roleNames.Select(r => r.RoleName))
                );
                HttpContext.Session.SetString("IsAdmin", isAdmin.ToString());
                HttpContext.Session.SetString("Language", userAccount.Language ?? "th");
                var deptInfo = await GetDeptInfoFromCES941ByAdUser(domainUsername);
                HttpContext.Session.SetString("Department", deptInfo.TitleShort ?? "");
                HttpContext.Session.SetString("WorkArea", deptInfo.WorkArea ?? "");
                HttpContext.Session.SetString("UserName", domainUsername ?? "");

                // โหลดข้อมูลภาษาเมื่อ login
                await LoadLanguageData(userAccount.Language ?? "th", userAccount.Language);

                // เพิ่มการตรวจสอบ department เมื่อไม่มีข้อมูลแผนกให้บังคับใส่รหัสพนักงาน
                bool needsProfileUpdate = string.IsNullOrEmpty(userAccount.Department);

                // อัปเดตเวลาเข้าสู่ระบบ
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE sys_user SET LastLogin = GETDATE() WHERE Id = {0}",
                    userAccount.Id
                );

                return Json(
                    new
                    {
                        success = true,
                        needsProfileUpdate = needsProfileUpdate,
                        userId = userAccount.Id,
                        redirectUrl = Url.Action("Index", "Home"),
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(
                    new { success = false, error = "เกิดข้อผิดพลาดในการเข้าสู่ระบบ: " + ex.Message }
                );
            }
        }

        [HttpGet]
        public IActionResult Logout()
        {
            // ล้าง session ทั้งหมด
            HttpContext.Session.Clear();

            // ล้าง authentication cookies (ถ้ามี)
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                // ล้าง authentication cookies
                HttpContext.SignOutAsync();
            }

            // ล้าง cookies ทั้งหมด
            foreach (var cookie in Request.Cookies.Keys)
            {
                // ตั้งค่า cookies ให้หมดอายุทันที
                Response.Cookies.Delete(cookie);

                // เพิ่มการลบ cookies แบบกำหนดค่าเพิ่มเติม
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(-1),
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Path =
                        "/" // เพิ่ม Path เป็น root เพื่อให้ลบ cookies ทั้งหมด
                    ,
                };
                Response.Cookies.Append(cookie, "", cookieOptions);
            }

            // ล้าง cookies ที่มีการตั้งค่าพิเศษ
            var expiredCookieOptions = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(-1),
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/",
            };

            // ล้าง cookies ที่เกี่ยวข้องกับ ASP.NET Core
            Response.Cookies.Delete(".AspNetCore.Session", expiredCookieOptions);
            Response.Cookies.Delete(".AspNetCore.Cookies", expiredCookieOptions);
            Response.Cookies.Delete(".AspNetCore.Antiforgery", expiredCookieOptions);

            // ล้าง cookies ที่อาจใช้ในหน้า CheckIn และ InOut
            Response.Cookies.Delete("filterName", expiredCookieOptions);
            Response.Cookies.Delete("filterDepartment", expiredCookieOptions);
            Response.Cookies.Delete("filterShift", expiredCookieOptions);
            Response.Cookies.Delete("selectedDate", expiredCookieOptions);
            Response.Cookies.Delete("departmentFilter", expiredCookieOptions);
            Response.Cookies.Delete("shiftFilter", expiredCookieOptions);

            // ล้าง TempData
            TempData.Clear();

            // เพิ่ม JavaScript เพื่อล้าง localStorage และ sessionStorage
            TempData["ClearClientStorage"] = true;

            return RedirectToAction("Login", "Authen");
        }

        [HttpPost]
        public IActionResult Register(sys_user model)
        {
            try
            {
                // ตรวจสอบ username ซ้ำ
                var existingUser = _context.sys_user.FirstOrDefault(u =>
                    u.Username == model.Username
                );
                if (existingUser != null)
                {
                    return Json(new { success = false, error = "Username already exists." });
                }

                // ตรวจสอบ ModelState
                if (!ModelState.IsValid)
                {
                    var errorMessages = ModelState.Values.SelectMany(v =>
                        v.Errors.Select(e => e.ErrorMessage)
                    );
                    return Json(new { success = false, error = string.Join(", ", errorMessages) });
                }

                // Hash รหัสผ่านและบันทึกข้อมูล
                model.PasswordHash = _passwordHasher.HashPassword(model, model.Password);
                model.PasswordDefault = model.Password;
                model.Password = null; // ลบรหัสผ่านที่ไม่เข้ารหัส

                _context.sys_user.Add(model);
                _context.SaveChanges();

                return Json(
                    new
                    {
                        success = true,
                        message = "เพิ่มข้อมูลผู้ใช้ใหม่เรียบร้อยแล้ว",
                        redirectUrl = Url.Action("Login", "Authen"),
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(
                    new { success = false, error = "เกิดข้อผิดพลาดในการลงทะเบียน: " + ex.Message }
                );
            }
        }

        public void SetSession(string key, string value)
        {
            // เก็บค่าในเซสชัน
            HttpContext.Session.SetString(key, value); // เก็บข้อมูลในเซสชัน
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
            ).OrderByDescending(u => u.LastLogin).ToList();

            bool isAdmin = userData.Any(u =>
                u.RoleName.Equals("admin", StringComparison.OrdinalIgnoreCase)
            );

            // ส่งข้อูลไปยัง View หรือ JSON
            return Json(new { userData, isAdmin });
        }

        public IActionResult Users()
        {
            ViewBag.Roles = _context.sys_role.ToList();
            return View();
        }

        [HttpGet]
        public IActionResult GetUsers()
        {
            try
            {
                var users = _context.sys_user.ToList();
                var roles = _context.sys_role.ToList();

                var result = users
                    .Select(user => new
                    {
                        id = user.Id,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        username = user.Username,
                        email = user.Email,
                        lastLogin = user.LastLogin,
                        phoneNumber = user.PhoneNumber,
                        isActive = user.IsActive,
                        roles = !string.IsNullOrEmpty(user.Roles)
                            ? string.Join(
                                ", ",
                                user.Roles.Split('|')
                                    .Select(roleId =>
                                    {
                                        var role = roles.Find(r => r.Id == int.Parse(roleId));
                                        return role?.RoleName ?? "Unknown";
                                    })
                            )
                            : "",
                    })
                    .ToList();

                return Json(new { data = result });
            }
            catch (Exception ex)
            {
                return Json(new { error = "เกิดข้อผิดพลาดในการดึงข้อมูล: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetUserRoles(int userId)
        {
            try
            {
                var user = _context.sys_user.FirstOrDefault(u => u.Id == userId);
                if (user == null || string.IsNullOrEmpty(user.Roles))
                {
                    return Json(new List<object>());
                }

                var roleIds = user.Roles.Split('|', StringSplitOptions.RemoveEmptyEntries);
                var roles = new List<object>();

                foreach (var roleId in roleIds)
                {
                    if (int.TryParse(roleId, out int id))
                    {
                        var role = _context.sys_role.Find(id);
                        if (role != null)
                        {
                            roles.Add(new { id = role.Id, name = role.RoleName });
                        }
                    }
                }

                return Json(roles);
            }
            catch (Exception ex)
            {
                return Json(new List<object>());
            }
        }

        [HttpPost]
        public IActionResult GetActionResult(string action, string message)
        {
            return Json(new { action, message });
        }

        [HttpGet]
        public IActionResult GetAvailableRoles(string searchTerm = "", int page = 1)
        {
            try
            {
                var query = _context.sys_role.AsQueryable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(r => r.RoleName.Contains(searchTerm));
                }

                var roles = query.Select(r => new { id = r.Id, name = r.RoleName }).ToList();

                // จัดการการแบ่งหน้าหลังจากดึงข้อมูลมาแล้ว
                int pageSize = 10;
                var pagedRoles = roles.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                return Json(pagedRoles);
            }
            catch (Exception ex)
            {
                return Json(new List<object>());
            }
        }

        [HttpPost]
        public IActionResult UpdateUserRoles([FromBody] UpdateUserRolesModel model)
        {
            try
            {
                var user = _context.sys_user.FirstOrDefault(u => u.Id == model.UserId);
                if (user == null)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลผู้ใช้" });
                }

                // อัพเดท Roles string
                if (model.RoleIds != null && model.RoleIds.Any())
                {
                    user.Roles = string.Join("|", model.RoleIds.OrderBy(r => r));
                }
                else
                {
                    user.Roles = string.Empty;
                }

                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(
                    new
                    {
                        success = false,
                        message = "เกิดข้อผิดพลาดในการอัพเดทสิทธิ์: " + ex.Message,
                    }
                );
            }
        }

        // เพิ่ม model class สำหรับรับข้อมูล
        public class UpdateUserRolesModel
        {
            public int UserId { get; set; }

            public List<int> RoleIds { get; set; }
        }

        [HttpGet]
        public IActionResult GetUserById(int id)
        {
            try
            {
                var user = _context.sys_user.FirstOrDefault(u => u.Id == id);
                if (user == null)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลผู้ใช้" });
                }

                var roles = !string.IsNullOrEmpty(user.Roles)
                    ? user
                        .Roles.Split('|', StringSplitOptions.RemoveEmptyEntries)
                        .Select(r => int.Parse(r))
                        .ToList()
                    : new List<int>();

                var result = new
                {
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    username = user.Username,
                    email = user.Email,
                    phoneNumber = user.PhoneNumber,
                    isActive = user.IsActive,
                    roles = roles,
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(
                    new { success = false, message = "เกิดข้อผิดพลาดในการดึงข้อมูล: " + ex.Message }
                );
            }
        }

        [HttpPost]
        public IActionResult UpdateUser(sys_user user)
        {
            try
            {
                var existingUser = _context.sys_user.FirstOrDefault(u => u.Id == user.Id);
                if (existingUser == null)
                {
                    return Json(new { success = false, message = "ไม่พบข้อมูลผู้ใช้" });
                }

                // Update user properties
                existingUser.FirstName = user.FirstName;
                existingUser.LastName = user.LastName;
                existingUser.Username = user.Username;
                existingUser.Email = user.Email;
                existingUser.PhoneNumber = user.PhoneNumber;
                existingUser.IsActive = user.IsActive;
                existingUser.Roles = user.Roles;

                user = existingUser;
            }
            catch (Exception ex)
            {
                return Json(
                    new
                    {
                        success = false,
                        message = "เกิดข้อผิดพลาดในการอัพเดทขู้ล: " + ex.Message,
                    }
                );
            }
            _context.sys_user.Update(user);
            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserProfile(
            int userId,
            string empNo,
            string department
        )
        {
            try
            {
                // ตรวจสอบข้อมูลที่ได้รับ
                if (string.IsNullOrWhiteSpace(empNo))
                {
                    return Json(new { success = false, error = "กรุณากรอกรหัสพนักงาน" });
                }

                if (string.IsNullOrWhiteSpace(department))
                {
                    return Json(new { success = false, error = "กรุณาเลือกแผนก" });
                }

                var user = await _context.sys_user.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, error = "ไม่พบข้อมูลผู้ใช้" });
                }

                // ตรวจสอบรหัสพนักงานใน SQL944 และเช็คกับชื่อใน AD
                if (!await ValidateEmployeeWithAD(empNo.Trim(), user.Username))
                {
                    return Json(
                        new
                        {
                            success = false,
                            error = "รหัสพนักงานไม่ตรงกับข้อมูลในระบบ หรือไม่พบข้อมูลพนักงาน",
                        }
                    );
                }

                // อัพเดทข้อมูลพนักงานและแผนก
                user.Emp_no = empNo.Trim();
                user.Department = department.Trim();
                await _context.SaveChangesAsync();

                // อัพเดท Session
                var userData = HttpContext.Session.GetString("UserData");
                if (!string.IsNullOrEmpty(userData))
                {
                    var userObj = JsonConvert.DeserializeObject<sys_user>(userData);
                    if (userObj != null)
                    {
                        userObj.Emp_no = empNo.Trim();
                        userObj.Department = department.Trim();
                        HttpContext.Session.SetString(
                            "UserData",
                            JsonConvert.SerializeObject(userObj)
                        );
                        HttpContext.Session.SetString("Department", department.Trim());
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(
                    new
                    {
                        success = false,
                        error = "เกิดข้อผิดพลาดในการอัพเดทข้อมูล: " + ex.Message,
                    }
                );
            }
        }

        private async Task<bool> ValidateEmployeeWithAD(string empNo, string username)
        {
            try
            {
                // ดึงข้อมูลพนักงานจาก SQL944 ตาราง Person
                bool foundInSQL944 = false;
                using (var connection = new SqlConnection(_sql944ConnectionString))
                {
                    connection.Open();
                    // ตรวจสอบว่ามีรหัสพนักงานใน SQL944 หรือไม่ (PersonID อาจจะมี -1, -2 ต่อท้าย)
                    var query =
                        @"SELECT TOP 1 PersonID, Name FROM Person WHERE PersonID LIKE @empNo + '%'";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@empNo", empNo);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                foundInSQL944 = true;
                                System.Diagnostics.Debug.WriteLine(
                                    $"Found employee in SQL944: {reader["PersonID"]}, {reader["Name"]}"
                                );
                            }
                        }
                    }
                }

                if (!foundInSQL944)
                {
                    System.Diagnostics.Debug.WriteLine($"Employee {empNo} not found in SQL944");
                    return false; // ไม่พบรหัสพนักงานใน SQL944
                }

                // ตรวจสอบข้อมูลจาก AD
                string domain = _configuration["ADDomain"];
                string domainUsername = username.Replace("LITEON\\", "");

                using var adContext = new PrincipalContext(ContextType.Domain, domain);
                var adUser = UserPrincipal.FindByIdentity(
                    adContext,
                    IdentityType.SamAccountName,
                    domainUsername
                );

                if (adUser == null)
                {
                    System.Diagnostics.Debug.WriteLine($"AD user not found: {domainUsername}");
                    return false;
                }

                // เช็คว่า Employee ID ใน AD ตรงกับรหัสพนักงานที่ใส่หรือไม่
                bool isValidEmployeeId =
                    !string.IsNullOrEmpty(adUser.EmployeeId) && adUser.EmployeeId == empNo;

                System.Diagnostics.Debug.WriteLine(
                    $"AD EmployeeId: {adUser.EmployeeId}, Input EmpNo: {empNo}, Match: {isValidEmployeeId}"
                );

                return isValidEmployeeId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating employee: {ex.Message}");
                return false;
            }
        }

        [HttpGet]
        public IActionResult GetDepartments()
        {
            try
            {
                var departments = new List<object>();

                var connectionString = !string.IsNullOrEmpty(_sql944ConnectionString)
                    ? _sql944ConnectionString
                    : _defaultConnectionString;

                if (string.IsNullOrEmpty(connectionString))
                {
                    return Json(new { success = false, message = "ไม่พบการตั้งค่าฐานข้อมูล" });
                }

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var query =
                        @"SELECT TOP (200) rowAutoID, code, name, deptLevel 
                                 FROM Dept 
                                 ORDER BY name";

                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                departments.Add(
                                    new
                                    {
                                        id = reader["rowAutoID"].ToString(),
                                        code = reader["code"].ToString(),
                                        name = reader["name"].ToString(),
                                        level = reader["deptLevel"].ToString(),
                                    }
                                );
                            }
                        }
                    }
                }

                return Json(new { success = true, data = departments });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetDepartments: {ex.Message}");
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLanguage(string language, string moduleId)
        {
            try
            {
                var userData = HttpContext.Session.GetString("UserData");
                if (!string.IsNullOrEmpty(userData))
                {
                    var userFromSession = JsonConvert.DeserializeObject<sys_user>(userData);
                    if (userFromSession != null)
                    {
                        // ค้นหาผู้ใช้จากฐานข้อมูลโดยตรงแทนที่จะใช้ข้อมูลจาก Session
                        var user = await _context.sys_user.FirstOrDefaultAsync(u =>
                            u.Id == userFromSession.Id
                        );
                        if (user != null)
                        {
                            // อัพเดทภาษาในฐานข้อมูล
                            user.Language = language;
                            _context.sys_user.Update(user);
                            await _context.SaveChangesAsync();

                            // อัพเดทข้อมูลผู้ใช้ใน Session
                            userFromSession.Language = language;
                            HttpContext.Session.SetString(
                                "UserData",
                                JsonConvert.SerializeObject(userFromSession)
                            );

                            // อัปเดต Session Language
                            HttpContext.Session.SetString("Language", language);

                            // ล้าง Session ModuleMenu เพื่อให้โหลดใหม่เมื่อเปลี่ยนภาษา
                            HttpContext.Session.Remove("ModuleMenu");

                            // โหลดข้อมูลภาษาใหม่
                            await LoadLanguageData(language, moduleId);

                            // ดึงข้อมูลภาษาที่โหลดใหม่
                            var languageDataJson = HttpContext.Session.GetString("LanguageData");
                            var translations = JsonConvert.DeserializeObject<
                                Dictionary<string, string>
                            >(languageDataJson ?? "{}");

                            return Json(
                                new
                                {
                                    success = true,
                                    language = language,
                                    data = translations,
                                    message = "อัพเดทภาษาเรียบร้อยแล้ว",
                                }
                            );
                        }
                        else
                        {
                            return Json(
                                new { success = false, message = "ไม่พบข้อมูลผู้ใช้ในฐานข้อมูล" }
                            );
                        }
                    }
                }
                return Json(new { success = false, message = "ไม่พบข้อมูลผู้ใช้ใน Session" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ฟังก์ชันสำหรับถอดรหัส
        string Decode(string id)
        {
            if (string.IsNullOrEmpty(id))
                return string.Empty;
            var plainTextBytes = Convert.FromBase64String(id);
            return Encoding.UTF8.GetString(plainTextBytes);
        }

        private async Task LoadLanguageData(string language, string moduleId)
        {
            try
            {
                // ตรวจสอบว่าภาษาถูกต้องหรือไม่
                if (string.IsNullOrEmpty(language))
                {
                    language = "th"; // ค่าเริ่มต้นเป็นภาษาไทย
                    System.Diagnostics.Debug.WriteLine("ไม่พบข้อมูลภาษา ใช้ค่าเริ่มต้นเป็นภาษาไทย");
                }

                // ถอดรหัส moduleId
                string decodedModuleId = string.Empty;
                try
                {
                    decodedModuleId = Decode(moduleId);
                    System.Diagnostics.Debug.WriteLine(
                        $"ถอดรหัส moduleId: {moduleId} -> {decodedModuleId}"
                    );
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"ไม่สามารถถอดรหัส moduleId ได้: {ex.Message}"
                    );
                    decodedModuleId = string.Empty;
                }
                moduleId = decodedModuleId;

                // แปลง moduleId เป็น int หากมีค่า และตรวจสอบว่าเป็นตัวเลขหรือไม่
                int? moduleIdInt = null;
                if (
                    !string.IsNullOrEmpty(moduleId)
                    && int.TryParse(moduleId, out int parsedModuleId)
                )
                {
                    moduleIdInt = parsedModuleId;
                    System.Diagnostics.Debug.WriteLine($"แปลง moduleId เป็น int: {moduleIdInt}");
                }

                // ดึงข้อมูลภาษาจากฐานข้อมูล
                var query = _context.sys_language.Where(l => l.RecordStatus == "N");
                System.Diagnostics.Debug.WriteLine("เริ่มดึงข้อมูลภาษาจากฐานข้อมูล");

                // เพิ่มเงื่อนไข moduleId ถ้ามีค่า
                if (moduleIdInt.HasValue)
                {
                    query = query.Where(l => l.ModuleId == moduleIdInt.Value.ToString());
                    System.Diagnostics.Debug.WriteLine(
                        $"กรองข้อมูลภาษาตาม moduleId: {moduleIdInt.Value}"
                    );
                }

                var languageData = await query
                    .Select(l => new
                    {
                        l.Id,
                        l.Keyword,
                        l.Th,
                        l.En,
                        zh = l.Cn,
                    })
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"พบข้อมูลภาษาทั้งหมด {languageData.Count} รายการ"
                );

                // สร้าง Dictionary เพื่อเก็บข้อมูลภาษาตาม keyword
                var translations = new Dictionary<string, string>();

                // เลือกคอลัมน์ภาษาตามที่ผู้ใช้เลือก
                foreach (var item in languageData)
                {
                    string value = language switch
                    {
                        "en" => item.En,
                        "zh" => item.zh,
                        _ =>
                            item.Th // ค่าเริ่มต้นเป็นภาษาไทย
                        ,
                    };

                    // เก็บข้อมูลใน Dictionary โดยใช้ Keyword เป็นคีย์
                    if (!string.IsNullOrEmpty(item.Keyword) && !string.IsNullOrEmpty(value))
                    {
                        translations[item.Keyword] = value;
                    }
                }

                System.Diagnostics.Debug.WriteLine(
                    $"แปลงข้อมูลภาษาเป็น Dictionary สำเร็จ มีข้อมูลทั้งหมด {translations.Count} รายการ"
                );

                // เก็บข้อมูลภาษาลงใน Session
                HttpContext.Session.SetString(
                    "LanguageData",
                    JsonConvert.SerializeObject(translations)
                );

                System.Diagnostics.Debug.WriteLine("บันทึกข้อมูลภาษาลงใน Session สำเร็จ");
            }
            catch (Exception ex)
            {
                // บันทึกข้อผิดพลาด
                System.Diagnostics.Debug.WriteLine($"Error loading language data: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLanguageData(string moduleId)
        {
            try
            {
                var currentLanguage = HttpContext.Session.GetString("Language");

                // ตรวจสอบว่า currentLanguage เป็นค่าว่างหรือไม่
                if (string.IsNullOrEmpty(currentLanguage))
                {
                    currentLanguage = "th"; // ค่าเริ่มต้นเป็นภาษาไทย
                    HttpContext.Session.SetString("Language", currentLanguage);
                }

                // ตรวจสอบว่ามีข้อมูลภาษาใน Session หรือไม่
                var languageDataJson = HttpContext.Session.GetString("LanguageData");

                // ถ้าไม่มี moduleId ให้ใช้ segment จาก path
                if (string.IsNullOrEmpty(moduleId))
                {
                    moduleId = HttpContext.Request.Path.Value.Split('/').Last();
                }

                await LoadLanguageData(currentLanguage, moduleId);

                // ถ้าไม่มีข้อมูลใน Session ให้โหลดใหม่
                if (string.IsNullOrEmpty(languageDataJson))
                {
                    await LoadLanguageData(currentLanguage, moduleId);
                    languageDataJson = HttpContext.Session.GetString("LanguageData");
                }

                // แปลงข้อมูลจาก JSON เป็น Dictionary
                var translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    languageDataJson ?? "{}"
                );

                return Json(
                    new
                    {
                        success = true,
                        data = translations,
                        language = currentLanguage,
                    }
                );
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
