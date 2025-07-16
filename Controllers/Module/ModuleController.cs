using IPS_TH.Data;
using IPS_TH.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HR.Controllers;

public class ModuleController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ModuleController> _logger;

    public ModuleController(ApplicationDbContext context, ILogger<ModuleController> logger)
    {
        _context = context;
        _logger = logger;
    }
  
    public IActionResult ManageRole()
    {
        return View();
    }

    public IActionResult ManageModule()
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

    [HttpGet]
    public IActionResult GetModules()
    {
        try
        {
            var modules = _context
                .sys_module.Where(m => m.ParentId == null && m.RecordStatus == "N")
                .OrderBy(m => m.Order)
                .Select(m => new
                {
                    m.Id,
                    m.Order,
                    m.Name,
                    m.Description,
                    m.Icon,
                    m.IsActive,
                    m.UpdatedAt,
                    m.UpdatedBy,
                })
                .ToList();

            _logger.LogInformation($"Retrieved {modules.Count} modules");
            return Json(modules);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in GetModules: {ex.Message}");
            return StatusCode(500, new { success = false, message = "ไม่สามารถโหลดข้อมูลได้" });
        }
    }

    [HttpGet]
    public IActionResult GetSubModules(int parentId)
    {
        try
        {
            var subModules = _context
                .sys_module.Where(m => m.ParentId == parentId && m.RecordStatus == "N")
                .OrderBy(m => m.Order)
                .Select(m => new
                {
                    m.Id,
                    m.Order,
                    m.Name,
                    m.Description,
                    m.Controller,
                    m.Action,
                    m.Icon,
                    m.IsActive,
                    m.UpdatedAt,
                    m.UpdatedBy,
                })
                .ToList();

            _logger.LogInformation(
                $"Retrieved {subModules.Count} sub-modules for parent ID: {parentId}"
            );
            return Json(subModules);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in GetSubModules: {ex.Message}");
            return StatusCode(
                500,
                new { success = false, message = "ไม่สามารถโหลดข้อมูลโมดูลย่อยได้" }
            );
        }
    }

    [HttpGet]
    public IActionResult GetModuleById(int id)
    {
        try
        {
            var module = _context
                .sys_module.Where(m => m.Id == id && m.RecordStatus == "N")
                .Select(m => new
                {
                    m.Id,
                    m.Order,
                    m.Name,
                    m.Description,
                    m.Controller,
                    m.Action,
                    m.Icon,
                    m.IsActive,
                    m.ParentId,
                })
                .FirstOrDefault();

            if (module == null)
            {
                return Json(new { success = false, message = "ไม่พบข้อมูลโมดูล" });
            }

            return Json(new { success = true, data = module });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in GetModuleById: {ex.Message}");
            return Json(new { success = false, message = "ไม่สามารถโหลดข้อมูลโมดูลได้" });
        }
    }

    [HttpPost]
    public IActionResult UpdateModule([FromBody] ModuleUpdateModel model)
    {
        try
        {
            var module = _context.sys_module.Find(model.Id);
            if (module == null || module.RecordStatus != "N")
            {
                return Json(new { success = false, message = "ไม่พบข้อมูลโมดูล" });
            }

            module.Order = model.Order;
            module.Name = model.Name;
            module.Description = model.Description;
            module.Icon = model.Icon;
            module.IsActive = model.IsActive;
            module.UpdatedAt = DateTime.Now;
            module.UpdatedBy = User.Identity?.Name;

            _context.SaveChanges();
            _logger.LogInformation($"Module {model.Id} updated successfully");

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in UpdateModule: {ex.Message}");
            return Json(new { success = false, message = "ไม่สามารถอัปเดตโมดูลได้" });
        }
    }

    [HttpPost]
    public IActionResult UpdateSubModule([FromBody] SubModuleUpdateModel model)
    {
        try
        {
            var module = _context.sys_module.Find(model.Id);
            if (module == null || module.RecordStatus != "N")
            {
                return Json(new { success = false, message = "ไม่พบข้อมูลโมดูลย่อย" });
            }

            module.Order = model.Order;
            module.Name = model.Name;
            module.Description = model.Description;
            module.Controller = model.Controller;
            module.Action = model.Action;
            module.Icon = model.Icon;
            module.IsActive = model.IsActive;
            module.UpdatedAt = DateTime.Now;
            module.UpdatedBy = User.Identity?.Name;
            module.ParentId = model.ParentId;

            _context.SaveChanges();
            _logger.LogInformation($"Sub-module {model.Id} updated successfully");

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in UpdateSubModule: {ex.Message}");
            return Json(new { success = false, message = "ไม่สามารถอัปเดตโมดูลย่อยได้" });
        }
    }

    [HttpPost]
    public IActionResult ToggleModuleStatus(int id, bool isActive)
    {
        try
        {
            var module = _context.sys_module.Find(id);
            if (module == null || module.RecordStatus != "N")
            {
                return Json(new { success = false, message = "ไม่พบข้อมูลโมดูล" });
            }

            module.IsActive = isActive;
            module.UpdatedAt = DateTime.Now;
            module.UpdatedBy = User.Identity?.Name;

            // ถ้าเป็นโมดูลหลักและปิดการใช้งาน ให้ปิดโมดูลย่อยด้วย
            if (!isActive && !module.ParentId.HasValue)
            {
                var subModules = _context
                    .sys_module.Where(m => m.ParentId == id && m.RecordStatus == "N")
                    .ToList();

                foreach (var subModule in subModules)
                {
                    subModule.IsActive = false;
                    subModule.UpdatedAt = DateTime.Now;
                    subModule.UpdatedBy = User.Identity?.Name;
                }
            }

            _context.SaveChanges();
            _logger.LogInformation($"Module {id} status updated to {isActive}");

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in ToggleModuleStatus: {ex.Message}");
            return Json(new { success = false, message = "ไม่สามารถเปลี่ยนสถานะโมดูลได้" });
        }
    }

    [HttpPost]
    public IActionResult AddModule([FromBody] ModuleAddModel model)
    {
        try
        {
            var module = new sys_module
            {
                Name = model.Name,
                Description = model.Description,
                Icon = model.Icon,
                IsActive = model.IsActive,
                Order = model.Order,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = User.Identity?.Name,
                UpdatedBy = User.Identity?.Name,
                RecordStatus = "N",
            };

            _context.sys_module.Add(module);
            _context.SaveChanges();

            _logger.LogInformation($"New module added: {module.Id}");
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in AddModule: {ex.Message}");
            return Json(new { success = false, message = "ไม่สามารถเพิ่มโมดูลได้" });
        }
    }

    [HttpPost]
    public IActionResult AddSubModule([FromBody] SubModuleAddModel model)
    {
        try
        {
            var module = new sys_module
            {
                Name = model.Name,
                Description = model.Description,
                Controller = model.Controller,
                Action = model.Action,
                Icon = model.Icon,
                IsActive = model.IsActive,
                Order = model.Order,
                ParentId = model.ParentId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = User.Identity?.Name,
                UpdatedBy = User.Identity?.Name,
                RecordStatus = "N",
            };

            _context.sys_module.Add(module);
            _context.SaveChanges();

            _logger.LogInformation($"New sub-module added: {module.Id}");
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in AddSubModule: {ex.Message}");
            return Json(new { success = false, message = "ไม่สามารถเพิ่มโมดูลย่อยได้" });
        }
    }

    [HttpPost]
    public IActionResult DeleteModule(int id)
    {
        try
        {
            var module = _context.sys_module.Find(id);
            if (module == null || module.RecordStatus != "N")
            {
                return Json(new { success = false, message = "ไม่พบข้อมูลโมดูล" });
            }

            // ถ้าเป็นโมดูลหลัก ให้ลบโมดูลย่อยด้วย
            if (!module.ParentId.HasValue)
            {
                var subModules = _context.sys_module
                    .Where(m => m.ParentId == id && m.RecordStatus == "N")
                    .ToList();

                foreach (var subModule in subModules)
                {
                    subModule.RecordStatus = "D";
                    subModule.UpdatedAt = DateTime.Now;
                    subModule.UpdatedBy = User.Identity?.Name;
                }
            }

            // ลบโมดูลหลัก
            module.RecordStatus = "D";
            module.UpdatedAt = DateTime.Now;
            module.UpdatedBy = User.Identity?.Name;

            _context.SaveChanges();
            _logger.LogInformation($"Module {id} deleted successfully");

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in DeleteModule: {ex.Message}");
            return Json(new { success = false, message = "ไม่สามารถลบโมดูลได้" });
        }
    }

    public class ModuleUpdateModel
    {
        public int Id { get; set; }
        public int Order { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public bool IsActive { get; set; }
    }

    public class SubModuleUpdateModel : ModuleUpdateModel
    {
        public string Controller { get; set; }
        public string Action { get; set; }
        public int? ParentId { get; set; }
    }

    public class ModuleAddModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public bool IsActive { get; set; }
        public int Order { get; set; }
    }

    public class SubModuleAddModel : ModuleAddModel
    {
        public string Controller { get; set; }
        public string Action { get; set; }
        public int ParentId { get; set; }
    }
}
