using Microsoft.AspNetCore.Mvc;
using IPS_TH.Extensions;

namespace IPS_TH.ViewComponents
{
    public class AdminToolbarViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string moduleId = "MQ==")
        {
            // ส่งข้อมูล ModuleId ไปยัง View
            ViewBag.ModuleId = moduleId;

            return View();
        }
    }
}
