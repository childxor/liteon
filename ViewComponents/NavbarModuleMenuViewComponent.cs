using IPS_TH.Data;
using IPS_TH.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace IPS_TH.ViewComponents
{
    public class MenuItem 
    {
        public string Name { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public int Order { get; set; }
        public string Icon { get; set; }
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string Url { get; set; }
        public bool IsAdd { get; set; }
        public bool IsApprove { get; set; }
        public bool IsDelete { get; set; }
        public bool IsEdit { get; set; }
        public bool IsReport { get; set; }
        public bool IsView { get; set; }
        public List<MenuItem> Children { get; set; } = new List<MenuItem>();
    }

    public class NavbarModuleMenuViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public NavbarModuleMenuViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                // ตรวจสอบข้อมูลผู้ใช้จาก Session
                var userData = HttpContext.Session.GetString("UserData");
                if (string.IsNullOrEmpty(userData))
                {
                    return View(new List<MenuItem>());
                }

                // แปลงข้อมูลผู้ใช้จาก JSON
                var user = JsonConvert.DeserializeObject<dynamic>(userData);
                string userRoles = user?.Roles?.ToString();
                if (user == null || string.IsNullOrEmpty(userRoles))
                {
                    return View(new List<MenuItem>());
                }
                
                // แยกบทบาทของผู้ใช้
                var roles = userRoles.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (roles.Length == 0)
                {
                    return View(new List<MenuItem>());
                }

                // ดึงชื่อบทบาทของผู้ใช้
                var roleNames = await _context.sys_role
                    .Where(r => roles.Contains(r.Id.ToString()))
                    .Select(r => r.RoleName)
                    .ToArrayAsync();

                // ตรวจสอบว่าผู้ใช้เป็นผู้ดูแลระบบหรือไม่
                bool isAdmin = roleNames.Contains("Administrators", StringComparer.OrdinalIgnoreCase);

                // ดึงภาษาที่ต้องการแสดงผล
                var language = HttpContext.Session.GetString("Language") ?? "th"; // ค่าเริ่มต้นเป็นภาษาไทย

                // ดึงข้อมูลโมดูลที่ active
                var modules = await _context.sys_module
                    .Where(m => m.IsActive && m.RecordStatus == "N")
                    .ToListAsync();

                var moduleMenu = new List<MenuItem>();

                if (isAdmin)
                {
                    // ถ้าเป็น Admin ให้เห็นทุก Module ที่ active
                    BuildAdminMenu(modules, moduleMenu, language);
                }
                else
                {
                    // กรณีเป็นผู้ใช้ทั่วไป ให้เห็นเฉพาะโมดูลที่มีสิทธิ์
                    await BuildUserMenu(modules, moduleMenu, roles, language);
                }

                // เรียงลำดับเมนูตาม Order
                moduleMenu = moduleMenu.OrderBy(m => m.Order).ToList();

                // เก็บข้อมูลเมนูลงใน Session เพื่อใช้ในครั้งต่อไป
                HttpContext.Session.SetString("ModuleMenu", JsonConvert.SerializeObject(moduleMenu));

                return View(moduleMenu);
            }
            catch (Exception ex)
            {
                // บันทึกข้อผิดพลาด
                System.Diagnostics.Debug.WriteLine($"Error in NavbarModuleMenuViewComponent: {ex.Message}");
                return View(new List<MenuItem>());
            }
        }

        // สร้างเมนูสำหรับผู้ดูแลระบบ
        private void BuildAdminMenu(List<sys_module> modules, List<MenuItem> moduleMenu, string language)
        {
            foreach (var module in modules.Where(m => m.ParentId == null))
            {
                var moduleChild = modules
                    .Where(m => m.ParentId == module.Id)
                    .OrderBy(m => m.Order)
                    .ToList();

                var menuItem = new MenuItem
                {
                    Name = GetLocalizedName(module, language),
                    Controller = module.Controller,
                    Action = module.Action,
                    Order = module.Order,
                    Icon = module.Icon,
                    Id = module.Id,
                    ParentId = module.ParentId,
                    Url = !string.IsNullOrEmpty(module.Controller) && !string.IsNullOrEmpty(module.Action)
                        ? $"/{module.Controller}/{module.Action}"
                        : "#",
                    Children = moduleChild.Select(mc => new MenuItem
                    {
                        Name = GetLocalizedName(mc, language),
                        Controller = mc.Controller,
                        Action = mc.Action,
                        Order = mc.Order,
                        Icon = mc.Icon,
                        Id = mc.Id,
                        ParentId = mc.ParentId,
                        Url = !string.IsNullOrEmpty(mc.Controller) && !string.IsNullOrEmpty(mc.Action)
                            ? $"/{mc.Controller}/{mc.Action}"
                            : "#"
                    }).OrderBy(c => c.Order).ToList()
                };

                moduleMenu.Add(menuItem);
            }
        }

        // สร้างเมนูสำหรับผู้ใช้ทั่วไป
        private async Task BuildUserMenu(List<sys_module> modules, List<MenuItem> moduleMenu, string[] roles, string language)
        {
            // ดึงรายละเอียดสิทธิ์ของผู้ใช้
            var roleDetails = await _context.sys_role_detail
                .Where(rd => roles.Contains(rd.RoleId.ToString()) && rd.IsView)
                .ToListAsync();

            if (roleDetails.Count == 0)
            {
                return;
            }

            var allowedModuleIds = roleDetails.Select(rd => rd.ModuleId).Distinct().ToList();

            foreach (var moduleId in allowedModuleIds)
            {
                var module = modules.FirstOrDefault(m =>
                    m.Id == moduleId && m.ParentId == null
                );
                if (module == null)
                    continue;

                // เช็คว่ามีโมดูลนี้ในเมนูหรือยัง
                if (!moduleMenu.Any(m => m.Id == module.Id))
                {
                    var rd = roleDetails.FirstOrDefault(r => r.ModuleId == module.Id);
                    if (rd == null) continue;

                    // ดึงเฉพาะโมดูลย่อยที่มีสิทธิ์ดู
                    var moduleChild = modules
                        .Where(m => m.ParentId == module.Id)
                        .Join(
                            roleDetails.Where(rd => rd.IsView),
                            m => m.Id,
                            rd => rd.ModuleId,
                            (m, rd) => new { Module = m, RoleDetail = rd }
                        )
                        .OrderBy(x => x.Module.Order)
                        .ToList();

                    var menuItem = new MenuItem
                    {
                        Name = GetLocalizedName(module, language),
                        Controller = module.Controller,
                        Action = module.Action,
                        Order = module.Order,
                        Icon = module.Icon,
                        Id = module.Id,
                        ParentId = module.ParentId,
                        Url = !string.IsNullOrEmpty(module.Controller) && !string.IsNullOrEmpty(module.Action)
                            ? $"/{module.Controller}/{module.Action}"
                            : "#",
                        IsAdd = rd.IsAdd,
                        IsApprove = rd.IsApprove,
                        IsDelete = rd.IsDelete,
                        IsEdit = rd.IsEdit,
                        IsReport = rd.IsReport,
                        IsView = rd.IsView,
                        Children = moduleChild.Select(mc => new MenuItem
                        {
                            Name = GetLocalizedName(mc.Module, language),
                            Controller = mc.Module.Controller,
                            Action = mc.Module.Action,
                            Order = mc.Module.Order,
                            Icon = mc.Module.Icon,
                            Id = mc.Module.Id,
                            ParentId = mc.Module.ParentId,
                            Url = !string.IsNullOrEmpty(mc.Module.Controller) && !string.IsNullOrEmpty(mc.Module.Action)
                                ? $"/{mc.Module.Controller}/{mc.Module.Action}"
                                : "#",
                            IsAdd = mc.RoleDetail.IsAdd,
                            IsApprove = mc.RoleDetail.IsApprove,
                            IsDelete = mc.RoleDetail.IsDelete,
                            IsEdit = mc.RoleDetail.IsEdit,
                            IsReport = mc.RoleDetail.IsReport,
                            IsView = mc.RoleDetail.IsView
                        }).OrderBy(c => c.Order).ToList()
                    };

                    moduleMenu.Add(menuItem);
                }
            }
        }

        // ดึงชื่อตามภาษาที่ต้องการ
        private string GetLocalizedName(sys_module module, string language) 
        {
            return language switch
            {
                "en" => module.NameEn ?? module.Name ?? string.Empty,
                "zh" => module.NameChina ?? module.Name ?? string.Empty,
                _ => module.Name ?? string.Empty
            };
        }
    }
} 