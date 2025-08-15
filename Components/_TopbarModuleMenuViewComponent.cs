//// ไฟล์นี้ถูกลบเพื่อแก้ปัญหา ViewComponent ซ้ำกัน
//// เราใช้ ViewComponent ที่อยู่ในโฟลเดอร์ ViewComponents แทน

//using IPS_TH.Data;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Newtonsoft.Json;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace IPS_TH.Components
//{
//    public class _TopbarModuleMenuViewComponent : ViewComponent
//    {
//        private readonly ApplicationDbContext _context;

//        public _TopbarModuleMenuViewComponent(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        public async Task<IViewComponentResult> InvokeAsync()
//        {
//            var userData = HttpContext.Session.GetString("UserData");
//            if (string.IsNullOrEmpty(userData))
//            {
//                return View(new List<dynamic>());
//            }

//            var user = JsonConvert.DeserializeObject<sys_user>(userData);
//            var roles = user.Roles.Split('|');

//            var roleNames = await _context.sys_role
//                .Where(r => roles.Contains(r.Id.ToString()))
//                .Select(r => r.RoleName)
//                .ToArrayAsync();

//            bool isAdmin = roleNames.Contains("Administrators", StringComparer.OrdinalIgnoreCase);
//            var language = HttpContext.Session.GetString("Language");

//            var ModuleMenu = new List<object>();
//            var modules = await _context.sys_module
//                .Where(m => m.IsActive && m.RecordStatus == "N")
//                .ToListAsync();

//            if (isAdmin)
//            {
//                foreach (var module in modules.Where(m => m.ParentId == null))
//                {
//                    ModuleMenu.Add(new {
//                        Name = language == "th" ? module.Name : (language == "en" ? module.NameEn : module.NameChina),
//                        module.Controller,
//                        module.Action,
//                        module.Order,
//                        module.Icon,
//                        module.Id,
//                        module.ParentId
//                    });
//                }
//            }
//            else
//            {
//                var roleDetails = await _context.sys_role_detail
//                    .Where(rd => roles.Contains(rd.RoleId.ToString()) && rd.IsView)
//                    .ToListAsync();

//                var allowedModuleIds = roleDetails.Select(rd => rd.ModuleId).Distinct().ToList();

//                foreach (var moduleId in allowedModuleIds)
//                {
//                    var module = modules.FirstOrDefault(m => m.Id == moduleId && m.ParentId == null);
//                    if (module == null) continue;

//                    ModuleMenu.Add(new {
//                        Name = language == "th" ? module.Name : (language == "en" ? module.NameEn : module.NameChina),
//                        module.Controller,
//                        module.Action,
//                        module.Order,
//                        module.Icon,
//                        module.Id,
//                        module.ParentId
//                    });
//                }
//            }

//            return View(ModuleMenu);
//        }
//    }
//}
