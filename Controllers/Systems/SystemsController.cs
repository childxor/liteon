using IPS_TH.Models;
using Microsoft.AspNetCore.Mvc;

namespace IPS_TH.Controllers
{
    public class SystemsController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Registers()
        {
            // ส่งข้อมูลที่ต้องการไปยัง View
            return View();
        }
    }
        
}
