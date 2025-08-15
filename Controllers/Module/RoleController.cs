using System.Text.Json;
using IPS_TH.Data;
using IPS_TH.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HR.Controllers;

public class RoleController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RoleController> _logger;

    public RoleController(ApplicationDbContext context, ILogger<RoleController> logger)
    {
        _context = context;
        _logger = logger;
    } 
 
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult GetRoles()
    {
        try
        {
            var roles = _context
                .sys_role.Where(r => r.RecordStatus == "N")
                .Select(r => new
                {
                    r.Id,
                    r.RoleName,
                    r.RoleDescription,
                    r.IsActive,
                    r.CreatedDate,
                    r.UpdatedDate,
                    r.CreatedBy,
                    r.UpdatedBy,
                    r.RecordStatus,
                })
                .ToList();

            _logger.LogInformation($"Retrieved {roles.Count} roles");
            return Json(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in GetRoles: {ex.Message}");
            return StatusCode(500, new { error = "ไม่สามารถโหลดข้อมูลได้" });
        }
    }

    [HttpPost]
    public IActionResult GetMainModule([FromBody] int roleId)
    {
        try
        {
            var query =
                @"
                SELECT 
                    m.Id as Id,
                    m.Name as Name,
                    m.Description as Description,
                    m.Controller as Controller,
                    m.Action as Action,
                    m.ParentId as ParentId,
                    m.IsActive as IsActive,
                    m.[Order] as [Order],
                    m.Icon as Icon,
                    ISNULL(rd.IsView, 0) as IsView,
                    ISNULL(rd.IsAdd, 0) as IsAdd,
                    ISNULL(rd.IsEdit, 0) as IsEdit,
                    ISNULL(rd.IsDelete, 0) as IsDelete,
                    ISNULL(rd.IsApprove, 0) as IsApprove,
                    ISNULL(rd.IsReport, 0) as IsReport,
                    CASE 
                        WHEN EXISTS (SELECT 1 FROM sys_module sm WHERE sm.ParentId = m.Id) THEN 1 
                        ELSE 0 
                    END as HasChildren
                FROM sys_module m
                LEFT JOIN sys_role_detail rd ON m.Id = rd.ModuleId AND rd.RoleId = @roleId
                WHERE m.ParentId IS NULL 
                AND m.IsActive = 1 
                AND m.RecordStatus = 'N'
                ORDER BY m.[Order] ASC";

            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = query;
                command.Parameters.Add(
                    new Microsoft.Data.SqlClient.SqlParameter("@roleId", roleId)
                );

                _context.Database.OpenConnection();

                using (var result = command.ExecuteReader())
                {
                    var modules = new List<dynamic>();
                    while (result.Read())
                    {
                        var module = new
                        {
                            Id = result.GetInt32(0),
                            Name = result.GetString(1),
                            Description = result.IsDBNull(2) ? null : result.GetString(2),
                            Controller = result.IsDBNull(3) ? null : result.GetString(3),
                            Action = result.IsDBNull(4) ? null : result.GetString(4),
                            ParentId = result.IsDBNull(5) ? (int?)null : result.GetInt32(5),
                            IsActive = result.GetBoolean(6),
                            Order = result.GetInt32(7),
                            Icon = result.IsDBNull(8) ? null : result.GetString(8),
                            Permissions = new
                            {
                                IsView = result.GetBoolean(9),
                                IsAdd = result.GetBoolean(10),
                                IsEdit = result.GetBoolean(11),
                                IsDelete = result.GetBoolean(12),
                                IsApprove = result.GetBoolean(13),
                                IsReport = result.GetBoolean(14),
                            },
                            HasChildren = result.GetInt32(15) == 1,
                        };
                        modules.Add(module);
                    }

                    _logger.LogInformation($"Found {modules.Count} main modules");
                    return Json(modules);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in GetMainModule: {ex.Message}");
            return Json(new { error = "ไม่สามารถโหลดข้อมูลได้" });
        }
    }

    [HttpGet]
    public IActionResult GetModuleChild(int roleId, int moduleId)
    {
        try
        {
            var modules = _context
                .Database.SqlQuery<dynamic>(
                    $@"
                SELECT 
                    m.Id as id,
                    m.Name as name,
                    m.Description as description,
                    m.Controller as controller,
                    m.Action as action,
                    m.ParentId as parentId,
                    m.IsActive as isActive,
                    m.[Order] as [order],
                    m.Icon as icon,
                    m.CreatedAt as createdAt,
                    m.UpdatedAt as updatedAt,
                    m.CreatedBy as createdBy,
                    m.UpdatedBy as updatedBy,
                    m.RecordStatus as recordStatus,
                    m.NameChina as nameChina,
                    m.NameEn as nameEn,
                    ISNULL(rd.IsView, 0) as isView,
                    ISNULL(rd.IsAdd, 0) as isAdd,
                    ISNULL(rd.IsEdit, 0) as isEdit,
                    ISNULL(rd.IsDelete, 0) as isDelete,
                    ISNULL(rd.IsApprove, 0) as isApprove,
                    ISNULL(rd.IsReport, 0) as isReport,
                    CASE 
                        WHEN EXISTS (SELECT 1 FROM sys_module sm WHERE sm.ParentId = m.Id) THEN 1 
                        ELSE 0 
                    END as hasChildren
                FROM sys_module m 
                LEFT JOIN sys_role_detail rd ON m.Id = rd.ModuleId AND rd.RoleId = {roleId}
                WHERE m.ParentId = {moduleId}
                AND m.IsActive = 1 
                AND m.RecordStatus = 'N'
                ORDER BY m.[Order] ASC"
                )
                .ToList();

            _logger.LogInformation($"Found {modules.Count} child modules");
            return Json(modules);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in GetModuleChild: {ex.Message}");
            return Json(new { error = "ไม่สามารถโหลดข้อมูลได้" });
        }
    }

    [HttpGet]
    public IActionResult GetRoleDetails(int roleId, int moduleId)
    {
        try
        {
            // Log เพื่อ debug
            _logger.LogInformation(
                $"Getting role details for roleId: {roleId}, moduleId: {moduleId}"
            );

            var roleDetail = _context.sys_role_detail.FirstOrDefault(rd =>
                rd.RoleId == roleId && rd.ModuleId == moduleId
            );

            var result = new
            {
                isView = roleDetail?.IsView ?? false,
                isAdd = roleDetail?.IsAdd ?? false,
                isEdit = roleDetail?.IsEdit ?? false,
                isDelete = roleDetail?.IsDelete ?? false,
                isApprove = roleDetail?.IsApprove ?? false,
                isReport = roleDetail?.IsReport ?? false,
            };

            // Log ค่าที่จะส่งกลับ
            _logger.LogInformation($"Returning role details: {JsonSerializer.Serialize(result)}");

            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in GetRoleDetails: {ex.Message}");
            return Json(new { error = ex.Message });
        }
    }

    [HttpPost]
    public JsonResult UpdateRolePermission(
        int moduleId,
        string permissionType,
        bool isGranted,
        int roleId
    )
    {
        try
        {
            var roleDetail = _context.sys_role_detail.FirstOrDefault(rd =>
                rd.ModuleId == moduleId && rd.RoleId == roleId
            );

            if (roleDetail == null)
            {
                // สร้าง role detail ใหม่
                roleDetail = new sys_role_detail
                {
                    ModuleId = moduleId,
                    RoleId = roleId,
                    IsAdd = false,
                    IsApprove = false,
                    IsDelete = false,
                    IsEdit = false,
                    IsReport = false,
                    IsView = false,
                };
                _context.sys_role_detail.Add(roleDetail);
            }

            // อัพเดทสิทธิ์
            switch (permissionType.ToLower())
            {
                case "isview":
                    roleDetail.IsView = isGranted;
                    break;
                case "isadd":
                    roleDetail.IsAdd = isGranted;
                    break;
                case "isedit":
                    roleDetail.IsEdit = isGranted;
                    break;
                case "isdelete":
                    roleDetail.IsDelete = isGranted;
                    break;
                case "isapprove":
                    roleDetail.IsApprove = isGranted;
                    break;
                case "isreport":
                    roleDetail.IsReport = isGranted;
                    break;
            }

            _context.SaveChanges();
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public JsonResult ToggleRoleStatus(int id, bool status)
    {
        try
        {
            var role = _context.sys_role.Find(id);
            if (role != null)
            {
                role.IsActive = status;
                role.UpdatedDate = DateTime.Now;
                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "ไม่พบข้อมูลสิทธิ์" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public JsonResult AddRole([FromBody] sys_role role)
    {
        try
        {
            role.RecordStatus = "N";
            role.CreatedDate = DateTime.Now;
            role.UpdatedDate = DateTime.Now;
            _context.sys_role.Add(role);
            _context.SaveChanges();
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public JsonResult UpdateRole([FromBody] sys_role role)
    {
        try
        {
            var existingRole = _context.sys_role.Find(role.Id);
            if (existingRole == null)
            {
                return Json(new { success = false, message = "ไม่พบข้อมูลสิทธิ์" });
            }

            existingRole.RoleName = role.RoleName;
            existingRole.RoleDescription = role.RoleDescription;
            existingRole.IsActive = role.IsActive;
            existingRole.UpdatedDate = DateTime.Now;

            _context.SaveChanges();
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public JsonResult DeleteRole(int id)
    {
        try
        {
            var role = _context.sys_role.Find(id);
            if (role != null)
            {
                role.RecordStatus = "D";
                role.UpdatedDate = DateTime.Now;
                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "ไม่พบข้อมูลสิทธิ์" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public IActionResult GetSubModules(int roleId, int moduleId)
    {
        try
        {
            var subModules = _context
                .sys_module.Where(m => m.ParentId == moduleId && m.RecordStatus == "N")
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.Description,
                    m.Controller,
                    m.Action,
                    m.IsActive,
                    m.RecordStatus,
                    m.Order,
                    m.Icon,
                    hasChildren = _context.sys_module.Any(sm => sm.ParentId == m.Id && sm.RecordStatus == "N"),
                    Permissions = _context
                        .sys_role_detail.Where(rd => rd.RoleId == roleId && rd.ModuleId == m.Id)
                        .Select(rd => new
                        {
                            IsView = rd.IsView,
                            IsAdd = rd.IsAdd,
                            IsEdit = rd.IsEdit,
                            IsDelete = rd.IsDelete,
                            IsApprove = rd.IsApprove,
                            IsReport = rd.IsReport,
                        })
                        .FirstOrDefault()
                        ?? new
                        {
                            IsView = false,
                            IsAdd = false,
                            IsEdit = false,
                            IsDelete = false,
                            IsApprove = false,
                            IsReport = false,
                        },
                })
                .OrderBy(m => m.Order)
                .ToList();

            _logger.LogInformation(
                $"Found {subModules.Count} sub modules for moduleId: {moduleId}"
            );
            return Json(new { 
                success = true,
                data = subModules,
                count = subModules.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in GetSubModules: {ex.Message}");
            return Json(new { success = false, error = "ไม่สามารถโหลดข้อมูลโมดูลย่อยได้" });
        }
    }
}
