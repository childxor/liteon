using Microsoft.AspNetCore.Mvc;

namespace YourProject.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Profile()
        {
            // แสดงและแก้ไขข้อมูลโปรไฟล์
            return View();
        }

        public IActionResult ChangePassword()
        {
            // หน้าเปลี่ยนรหัสผ่าน
            return View();
        }

        public IActionResult Notifications()
        {
            // จัดการการแจ้งเตือน
            return View();
        }

        public IActionResult ActivityLog()
        {
            // แสดงประวัติการใช้งาน
            return View();
        }

        public IActionResult Privacy()
        {
            // ตั้งค่าความเป็นส่วนตัว
            return View();
        }

        public IActionResult Security()
        {
            // ตั้งค่าความปลอดภัย
            return View();
        }
    }
} 