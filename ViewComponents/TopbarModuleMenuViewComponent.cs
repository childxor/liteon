using IPS_TH.Data;
using IPS_TH.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // เพิ่ม namespace สำหรับ Session Extensions

namespace IPS_TH.ViewComponents
{
    public class TopbarModuleMenuViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public TopbarModuleMenuViewComponent(ApplicationDbContext context)
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
                    return View(new List<object>());
                }

                // แปลงข้อมูลผู้ใช้จาก JSON
                var user = JsonConvert.DeserializeObject<dynamic>(userData); // เปลี่ยนจาก sys_user เป็น dynamic
                string userRoles = user?.Roles?.ToString();
                if (user == null || string.IsNullOrEmpty(userRoles))
                {
                    return View(new List<object>());
                }
                
                // แยกบทบาทของผู้ใช้
                var roles = userRoles.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (roles.Length == 0)
                {
                    return View(new List<object>());
                }

                // ดึงชื่อบทบาทของผู้ใช้
                var roleNames = await _context.sys_role
                    .Where(r => roles.Contains(r.Id.ToString()))
                    .Select(r => r.RoleName)
                    .ToArrayAsync();

                // ตรวจสอบว่าผู้ใช้เป็นผู้ดูแลระบบหรือไม่
                bool isAdmin = roleNames.Contains("Administrators", StringComparer.OrdinalIgnoreCase);

                // ดึงภาษาที่ต้องการแสดงผล
                var language = HttpContext.Session.GetString("Language") ?? "en"; // ค่าเริ่มต้นเป็นภาษาอังกฤษ

                // ดึงข้อมูลโมดูลที่ active
                var modules = await _context.sys_module
                    .Where(m => m.IsActive && m.RecordStatus == "N")
                    .ToListAsync();

                var ModuleMenu = new List<object>();

                if (isAdmin)
                {
                    // ถ้าเป็น Admin ให้เห็นทุก Module ที่ active
                    BuildAdminMenu(modules, ModuleMenu, language);
                }
                else
                {
                    // กรณีเป็นผู้ใช้ทั่วไป ให้เห็นเฉพาะโมดูลที่มีสิทธิ์
                    await BuildUserMenu(modules, ModuleMenu, roles, language);
                }

                // เรียงลำดับเมนูตาม Order
                ModuleMenu = ModuleMenu.OrderBy(m => {
                    var dynamicMenu = (dynamic)m;
                    return dynamicMenu.Order;
                }).ToList();

                // เก็บข้อมูลเมนูลงใน Session เพื่อใช้ในครั้งต่อไป
                HttpContext.Session.SetString("ModuleMenu", JsonConvert.SerializeObject(ModuleMenu));

                return View(ModuleMenu);
            }
            catch (Exception ex)
            {
                // บันทึกข้อผิดพลาด
                System.Diagnostics.Debug.WriteLine($"Error in TopbarModuleMenuViewComponent: {ex.Message}");
                return View(new List<object>());
            }
        }

        // สร้างเมนูสำหรับผู้ดูแลระบบ
        private void BuildAdminMenu(List<sys_module> modules, List<object> ModuleMenu, string language)
        {
            foreach (var module in modules.Where(m => m.ParentId == null))
            {
                var moduleChild = modules
                    .Where(m => m.ParentId == module.Id)
                    .OrderBy(m => m.Order)
                    .ToList();

                ModuleMenu.Add(
                    new
                    {
                        Name = GetLocalizedName(module, language),
                        module.Controller,
                        module.Action,
                        module.Order,
                        module.Icon,
                        module.Id,
                        module.ParentId,
                        Url = !string.IsNullOrEmpty(module.Controller)
                        && !string.IsNullOrEmpty(module.Action)
                            ? $"/{module.Controller}/{module.Action}"
                            : "#",
                        Children = moduleChild
                            .Select(mc => new
                            {
                                Name = GetLocalizedName(mc, language),
                                mc.Controller,
                                mc.Action,
                                mc.Order,
                                mc.Icon,
                                mc.Id,
                                mc.ParentId,
                                Url = !string.IsNullOrEmpty(mc.Controller)
                                && !string.IsNullOrEmpty(mc.Action)
                                    ? $"/{mc.Controller}/{mc.Action}"
                                    : "#",
                            })
                            .OrderBy(c => c.Order)
                            .ToList(),
                    }
                );
            }
        }

        // สร้างเมนูสำหรับผู้ใช้ทั่วไป
        private async Task BuildUserMenu(List<sys_module> modules, List<object> ModuleMenu, string[] roles, string language)
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
                if (!ModuleMenu.Any(m => ((dynamic)m).Id == module.Id))
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

                    ModuleMenu.Add(
                        new
                        {
                            Name = GetLocalizedName(module, language),
                            module.Controller,
                            module.Action,
                            module.Order,
                            module.Icon,
                            module.Id,
                            module.ParentId,
                            Url = !string.IsNullOrEmpty(module.Controller)
                            && !string.IsNullOrEmpty(module.Action)
                                ? $"/{module.Controller}/{module.Action}"
                                : "#",
                            rd.IsAdd,
                            rd.IsApprove,
                            rd.IsDelete,
                            rd.IsEdit,
                            rd.IsReport,
                            rd.IsView,
                            Children = moduleChild
                                .Select(mc => new
                                {
                                    Name = GetLocalizedName(mc.Module, language),
                                    mc.Module.Controller,
                                    mc.Module.Action,
                                    mc.Module.Order,
                                    mc.Module.Icon,
                                    mc.Module.Id,
                                    mc.Module.ParentId,
                                    Url = !string.IsNullOrEmpty(mc.Module.Controller)
                                    && !string.IsNullOrEmpty(mc.Module.Action)
                                        ? $"/{mc.Module.Controller}/{mc.Module.Action}"
                                        : "#",
                                    mc.RoleDetail.IsAdd,
                                    mc.RoleDetail.IsApprove,
                                    mc.RoleDetail.IsDelete,
                                    mc.RoleDetail.IsEdit,
                                    mc.RoleDetail.IsReport,
                                    mc.RoleDetail.IsView,
                                })
                                .OrderBy(c => c.Order)
                                .ToList(),
                        }
                    );
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
