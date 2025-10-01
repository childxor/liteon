using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Security.Claims;
using Dapper;
using IPS_TH.Data;
using IPS_TH.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore; // เพิ่มบรรทัดนี้
using Newtonsoft.Json;

namespace IPS_TH.Controllers 
{
    public class BaseController : Controller
    {
        protected readonly ApplicationDbContext _context;
        protected readonly OracleDbContext _oracleContext;
        protected readonly IConfiguration _configuration;
        private const int CACHE_DURATION_MINUTES = 30;

        // Constructor เดิม (สำหรับ backward compatibility)
        public BaseController(ApplicationDbContext context)
        {
            _context = context;
            _oracleContext = null;
            _configuration = null;
        }

        // Constructor ใหม่ (สำหรับใช้งาน Oracle)
        public BaseController(ApplicationDbContext context, OracleDbContext oracleContext, IConfiguration configuration)
        {
            _context = context;
            _oracleContext = oracleContext;
            _configuration = configuration;
        }

        protected Dictionary<string, bool> CurrentPermissions { get; private set; }
        
        // เพิ่ม property สำหรับเก็บข้อมูลแผนกของผู้ใช้
        protected string CurrentUserDepartment { get; private set; }
        
        // เพิ่ม property สำหรับตรวจสอบว่าเป็น admin หรือไม่
        protected bool IsCurrentUserAdmin { get; private set; }

        //public override async Task OnActionExecutionAsync(
        //    ActionExecutingContext context,
        //    ActionExecutionDelegate next
        //)
        //{
        //    try
        //    {
        //        if (!IsUserLoggedIn())
        //        {
        //            context.Result = RedirectToAction("Login", "Authen");
        //            return;
        //        }

        //        var currentController = context.RouteData.Values["controller"]?.ToString();
        //        var currentAction = context.RouteData.Values["action"]?.ToString();

        //        await LoadPermissions(currentController, currentAction);

        //        await next();
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log error here
        //        context.Result = ErrorResponse($"เกิดข้อผิดพลาด: {ex.Message}");
        //    }
        //}

        protected bool IsUserLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserData"));
        }

        protected sys_user GetCurrentUser()
        {
            var userData = HttpContext.Session.GetString("UserData");
            return string.IsNullOrEmpty(userData)
                ? null
                : JsonConvert.DeserializeObject<sys_user>(userData);
        }

        protected async Task LoadPermissions(string controller, string action)
        {
            try
            {
                var user = GetCurrentUser();
                if (user == null)
                {
                    CurrentPermissions = new Dictionary<string, bool>();
                    ViewData["Permissions"] = CurrentPermissions;
                    return;
                }

                // เก็บข้อมูลแผนกของผู้ใช้
                CurrentUserDepartment = HttpContext.Session.GetString("WorkArea");

                // ลองดึงจาก Cache ก่อน
                var cacheKey = $"Permissions_{user.Id}_{controller}_{action}";
                if (HttpContext.Items.TryGetValue(cacheKey, out var cachedPermissions))
                {
                    CurrentPermissions = cachedPermissions as Dictionary<string, bool>;
                    ViewData["Permissions"] = CurrentPermissions;
                    return;
                }

                // ตรวจสอบว่า context ยังใช้งานได้หรือไม่
                if (_context == null)
                {
                    Console.WriteLine("LoadPermissions: Context is null, setting default permissions");
                    SetDefaultPermissions();
                    return;
                }

                try
                {
                    var roles = user.Roles.Split('|');
                    var roleNames = await _context
                        .sys_role.Where(r => roles.Contains(r.Id.ToString()))
                        .Select(r => r.RoleName)
                        .ToArrayAsync();

                    // เช็ค Admin แบบ Case-insensitive
                    IsCurrentUserAdmin = roleNames.Any(r => r.Equals("Administrators", StringComparison.OrdinalIgnoreCase));

                    if (IsCurrentUserAdmin)
                    {
                        CurrentPermissions = new Dictionary<string, bool>
                    {
                        { "CanView", true },
                        { "CanAdd", true },
                        { "CanEdit", true },
                        { "CanDelete", true },
                        { "CanApprove", true },
                        { "CanReport", true },
                        { "IsAdmin", true },

                    };
                    }
                    else
                    {
                        var module = await _context.sys_module.FirstOrDefaultAsync(m =>
                            m.Controller == controller
                            && (string.IsNullOrEmpty(action) || m.Action == action)
                        );

                        if (module != null)
                        {
                            // ดึงข้อมูลสิทธิ์จากทุก role ที่ผู้ใช้มี
                            var roleDetails = await _context
                                .sys_role_detail.Where(rd =>
                                    roles.Contains(rd.RoleId.ToString()) && rd.ModuleId == module.Id
                                )
                                .ToListAsync();

                            // ถ้ามีอย่างน้อยหนึ่ง role ที่มีสิทธิ์ ให้ถือว่ามีสิทธิ์
                            CurrentPermissions = new Dictionary<string, bool>
                        {
                            { "CanView", roleDetails.Any(rd => rd.IsView) },
                            { "CanAdd", roleDetails.Any(rd => rd.IsAdd) },
                            { "CanEdit", roleDetails.Any(rd => rd.IsEdit) },
                            { "CanDelete", roleDetails.Any(rd => rd.IsDelete) },
                            { "CanApprove", roleDetails.Any(rd => rd.IsApprove) },
                            { "CanReport", roleDetails.Any(rd => rd.IsReport) },
                            { "IsAdmin", false },

                        };
                        }
                        else
                        {
                            // กรณีไม่พบ module ให้กำหนดสิทธิ์เป็น false ทั้งหมด
                            SetDefaultPermissions();
                        }
                    }

                    // เก็บลง Cache
                    HttpContext.Items[cacheKey] = CurrentPermissions;
                    ViewData["Permissions"] = CurrentPermissions;
                }
                catch (ObjectDisposedException ex)
                {
                    Console.WriteLine($"LoadPermissions: Context disposed error - {ex.Message}");
                    SetDefaultPermissions();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"LoadPermissions: Unexpected error - {ex.Message}");
                    SetDefaultPermissions();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadPermissions: Critical error - {ex.Message}");
                SetDefaultPermissions();
            }
        }

        private void SetDefaultPermissions()
        {
            CurrentPermissions = new Dictionary<string, bool>
            {
                { "CanView", false },
                { "CanAdd", false },
                { "CanEdit", false },
                { "CanDelete", false },
                { "CanApprove", false },
                { "CanReport", false },
                { "IsAdmin", false },

            };
            ViewData["Permissions"] = CurrentPermissions;
        }

        protected bool CheckPermission(string permissionType)
        {
            if (CurrentPermissions == null)
                return false;

            // ป้องกันกรณี permissionType เป็น null
            if (string.IsNullOrEmpty(permissionType))
                return false;

            return CurrentPermissions.TryGetValue(permissionType, out var hasPermission) && hasPermission;
        }

        // เพิ่มฟังก์ชันสำหรับตรวจสอบสิทธิ์การเข้าถึงตามแผนก
        protected bool CanAccessDepartment(string departmentId)
        {
            // Admin สามารถเข้าถึงได้ทุกแผนก
            if (IsCurrentUserAdmin)
                return true;

            // ถ้าแผนกไม่ตรงกับของผู้ใช้ ให้ปฏิเสธ
            if (!string.IsNullOrEmpty(CurrentUserDepartment) && 
                !string.IsNullOrEmpty(departmentId))
            {
                return CurrentUserDepartment.Equals(departmentId, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }

        // เพิ่มฟังก์ชันสำหรับตรวจสอบสิทธิ์การเข้าถึงข้อมูลพนักงาน
        protected bool CanAccessEmployeeData(string employeeDepartmentId)
        {
            // Admin สามารถเข้าถึงได้ทุกข้อมูล
            if (IsCurrentUserAdmin)
                return true;

            // ถ้าแผนกไม่ตรงกับของผู้ใช้ ให้ปฏิเสธ
            if (!string.IsNullOrEmpty(CurrentUserDepartment) && 
                !string.IsNullOrEmpty(employeeDepartmentId))
            {
                return CurrentUserDepartment.Equals(employeeDepartmentId, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }

        // เพิ่มฟังก์ชันสำหรับตรวจสอบสิทธิ์การแก้ไขข้อมูลพนักงานตามแผนก
        protected bool CanEditEmployeeData(string employeeDepartmentId)
        {
            // Admin สามารถแก้ไขได้ทุกข้อมูล
            if (IsCurrentUserAdmin)
                return true;

            // ตรวจสอบสิทธิ์แก้ไขและแผนก
            if (CheckPermission("CanEdit") && 
                !string.IsNullOrEmpty(CurrentUserDepartment) && 
                !string.IsNullOrEmpty(employeeDepartmentId))
            {
                return CurrentUserDepartment.Equals(employeeDepartmentId, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        // เพิ่มฟังก์ชันสำหรับตรวจสอบสิทธิ์การลบข้อมูลพนักงานตามแผนก
        protected bool CanDeleteEmployeeData(string employeeDepartmentId)
        {
            // Admin สามารถลบได้ทุกข้อมูล
            if (IsCurrentUserAdmin)
                return true;

            // ตรวจสอบสิทธิ์ลบและแผนก
            if (CheckPermission("CanDelete") && 
                !string.IsNullOrEmpty(CurrentUserDepartment) && 
                !string.IsNullOrEmpty(employeeDepartmentId))
            {
                return CurrentUserDepartment.Equals(employeeDepartmentId, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        protected IActionResult ErrorResponse(string message, int statusCode = 400)
        {
            Response.StatusCode = statusCode;
            return Json(new { success = false, message });
        }

        protected IActionResult SuccessResponse(
            object data = null,
            string message = "ดำเนินการสำเร็จ"
        )
        {
            return Json(
                new
                {
                    success = true,
                    data,
                    message,
                }
            );
        }

        // ตัวอย่างเมธอดสำหรับใช้งาน Oracle Database (เฉพาะเมื่อมี OracleDbContext)
        protected async Task<List<T>> GetOracleDataAsync<T>(string sql, object parameters = null)
        {
            if (_oracleContext == null)
                throw new InvalidOperationException("OracleDbContext is not available. Use constructor with OracleDbContext parameter.");

            using var connection = _oracleContext.Database.GetDbConnection();
            await connection.OpenAsync();
            var result = await connection.QueryAsync<T>(sql, parameters);
            return result.ToList();
        }

        // ตัวอย่างเมธอดสำหรับ Execute Oracle Command
        protected async Task<int> ExecuteOracleCommandAsync(string sql, object parameters = null)
        {
            if (_oracleContext == null)
                throw new InvalidOperationException("OracleDbContext is not available. Use constructor with OracleDbContext parameter.");

            using var connection = _oracleContext.Database.GetDbConnection();
            await connection.OpenAsync();
            return await connection.ExecuteAsync(sql, parameters);
        }

        protected string GetCurrentUserId()
        {
            return HttpContext.Session.GetString("UserId") ?? "Unknown";
        }

        protected string GetCurrentUserName()
        {
            return HttpContext.Session.GetString("UserName") ?? "Unknown";
        }
    }
}
