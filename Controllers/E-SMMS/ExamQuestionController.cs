using System;
using System.IO;
using System.Linq;
using Dapper;
using System.Threading.Tasks;
using IPS_TH.Data;
using IPS_TH.Models.ESMMS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace IPS_TH.Controllers.ESMMS
{
    public class ExamQuestionController : BaseController
    {
        public ExamQuestionController(ApplicationDbContext context) : base(context) { }

        [HttpGet]
        public async Task<IActionResult> Index(string course_code)
        {
            await LoadPermissions("Exam", "ExamQuestion");

            var courses = await _context.esmms_course
                .OrderBy(x => x.course_code)
                .Select(x => new esmms_course { id = x.id, course_code = x.course_code, description_en = x.description_en })
                .ToListAsync();
            ViewBag.Courses = courses;
            ViewBag.SelectedCourse = course_code;

            var items = await _context.esmms_question
                .Where(q => string.IsNullOrEmpty(course_code) || q.course_code == course_code)
                .OrderByDescending(q => q.id)
                .ToListAsync();

            return View("~/Views/ESMMS/exam_questions.cshtml", items);
        }

        [HttpGet]
        public async Task<IActionResult> GetQuestions(string course_code)
        {
            var items = await _context.esmms_question
                .Where(q => string.IsNullOrEmpty(course_code) || q.course_code == course_code)
                .OrderByDescending(q => q.id)
                .ToListAsync();
            return Json(new { data = items });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(MultipartBodyLengthLimit = 104857600)] // 100 MB
        [RequestSizeLimit(104857600)]
        public async Task<IActionResult> CreateQuestion(esmms_question input, IFormFile? image)
        {
            if (!ModelState.IsValid)
                return ErrorResponse("กรุณากรอกข้อมูลให้ครบถ้วน");

            // เติม course_id จาก course_code ถ้ามี
            if (!string.IsNullOrEmpty(input.course_code))
            {
                var course = await _context.esmms_course.FirstOrDefaultAsync(c => c.course_code == input.course_code);
                input.course_id = course?.id;
            }

            // อัปโหลดรูปถ้ามี
            if (image != null && image.Length > 0)
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "exam");
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);
                var fileName = $"q_{DateTime.Now:yyyyMMddHHmmssfff}_{Path.GetFileName(image.FileName)}";
                var fullPath = Path.Combine(uploadsDir, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }
                input.image_path = $"uploads/exam/{fileName}";
            }

            input.created_at = System.DateTime.Now;
            input.updated_at = System.DateTime.Now;
            input.created_by = GetCurrentUserName();
            input.updated_by = GetCurrentUserName();

            _context.esmms_question.Add(input);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { course_code = input.course_code });
        }

        // กันกรณีเรียกแบบ GET /ExamQuestion/CreateQuestion และ /ExamQuestion/Create ให้ redirect แทน 404
        [HttpGet]
        public IActionResult CreateQuestion()
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [ActionName("Create")]
        public IActionResult CreateGet()
        {
            return RedirectToAction(nameof(Index));
        }

        // ========== AJAX endpoints ==========
        [HttpPost]
        public async Task<IActionResult> CreateAjax(esmms_question input, IFormFile? image)
        {
            if (!ModelState.IsValid) return ErrorResponse("กรุณากรอกข้อมูลให้ครบถ้วน");

            if (!string.IsNullOrEmpty(input.course_code))
            {
                var course = await _context.esmms_course.FirstOrDefaultAsync(c => c.course_code == input.course_code);
                input.course_id = course?.id;
            }

            if (image != null && image.Length > 0)
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "exam");
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);
                var fileName = $"q_{DateTime.Now:yyyyMMddHHmmssfff}_{Path.GetFileName(image.FileName)}";
                using var stream = new FileStream(Path.Combine(uploadsDir, fileName), FileMode.Create);
                await image.CopyToAsync(stream);
                input.image_path = $"uploads/exam/{fileName}";
            }

            input.created_at = DateTime.Now;
            input.updated_at = DateTime.Now;
            input.created_by = GetCurrentUserName();
            input.updated_by = GetCurrentUserName();
            _context.esmms_question.Add(input);
            await _context.SaveChangesAsync();
            return SuccessResponse(new { id = input.id });
        }

        [HttpPost]
        public async Task<IActionResult> EditAjax(esmms_question input, IFormFile? image)
        {
            if (input.id <= 0) return ErrorResponse("รหัสไม่ถูกต้อง");
            if (!ModelState.IsValid) return ErrorResponse("กรุณากรอกข้อมูลให้ครบถ้วน");

            var entity = await _context.esmms_question.FirstOrDefaultAsync(q => q.id == input.id);
            if (entity == null) return ErrorResponse("ไม่พบข้อมูล");

            entity.course_code = input.course_code;
            if (!string.IsNullOrEmpty(input.course_code))
            {
                var course = await _context.esmms_course.FirstOrDefaultAsync(c => c.course_code == input.course_code);
                entity.course_id = course?.id;
            }
            entity.question_text = input.question_text;
            entity.choice_a = input.choice_a;
            entity.choice_b = input.choice_b;
            entity.choice_c = input.choice_c;
            entity.choice_d = input.choice_d;
            entity.correct_choice = input.correct_choice;
            entity.score = input.score;
            entity.is_active = input.is_active;

            if (image != null && image.Length > 0)
            {
                var oldImage = entity.image_path;
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "exam");
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);
                var fileName = $"q_{DateTime.Now:yyyyMMddHHmmssfff}_{Path.GetFileName(image.FileName)}";
                using var stream = new FileStream(Path.Combine(uploadsDir, fileName), FileMode.Create);
                await image.CopyToAsync(stream);
                entity.image_path = $"uploads/exam/{fileName}";
                DeleteImageFileIfExists(oldImage);
            }

            entity.updated_at = DateTime.Now;
            entity.updated_by = GetCurrentUserName();
            await _context.SaveChangesAsync();
            return SuccessResponse();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            var entity = await _context.esmms_question.FirstOrDefaultAsync(q => q.id == id);
            if (entity == null) return ErrorResponse("ไม่พบข้อมูล");
            DeleteImageFileIfExists(entity.image_path);
            _context.esmms_question.Remove(entity);
            await _context.SaveChangesAsync();
            return SuccessResponse();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Edit")]
        public async Task<IActionResult> EditQuestion(esmms_question input, IFormFile? image)
        {
            if (!ModelState.IsValid)
                return ErrorResponse("กรุณากรอกข้อมูลให้ครบถ้วน");

            var entity = await _context.esmms_question.FirstOrDefaultAsync(q => q.id == input.id);
            if (entity == null)
                return ErrorResponse("ไม่พบข้อมูลที่ต้องการแก้ไข");

            // อัปเดตฟิลด์หลัก
            entity.course_code = input.course_code;
            if (!string.IsNullOrEmpty(input.course_code))
            {
                var course = await _context.esmms_course.FirstOrDefaultAsync(c => c.course_code == input.course_code);
                entity.course_id = course?.id;
            }
            entity.question_text = input.question_text;
            entity.choice_a = input.choice_a;
            entity.choice_b = input.choice_b;
            entity.choice_c = input.choice_c;
            entity.choice_d = input.choice_d;
            entity.correct_choice = input.correct_choice;
            entity.score = input.score;
            entity.is_active = input.is_active;

            // เปลี่ยนรูปถ้ามีการอัปโหลดใหม่
            if (image != null && image.Length > 0)
            {
                var oldImage = entity.image_path;
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "exam");
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);
                var fileName = $"q_{DateTime.Now:yyyyMMddHHmmssfff}_{Path.GetFileName(image.FileName)}";
                var fullPath = Path.Combine(uploadsDir, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }
                entity.image_path = $"uploads/exam/{fileName}";
                DeleteImageFileIfExists(oldImage);
            }

            entity.updated_at = DateTime.Now;
            entity.updated_by = GetCurrentUserName();

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { course_code = entity.course_code });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? course_code)
        {
            var entity = await _context.esmms_question.FirstOrDefaultAsync(q => q.id == id);
            if (entity == null)
                return RedirectToAction(nameof(Index), new { course_code });

            var redirectCourse = entity.course_code ?? course_code;
            DeleteImageFileIfExists(entity.image_path);
            _context.esmms_question.Remove(entity);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { course_code = redirectCourse });
        }

        private void DeleteImageFileIfExists(string? relativeOrAbsolutePath)
        {
            if (string.IsNullOrWhiteSpace(relativeOrAbsolutePath)) return;
            var trimmed = relativeOrAbsolutePath.TrimStart('/', '\\');
            string fullPath;
            if (Path.IsPathRooted(trimmed))
            {
                fullPath = trimmed;
            }
            else
            {
                var wwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                fullPath = Path.Combine(wwwroot, trimmed.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));
            }
            try
            {
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
            }
            catch { /* ignore IO exceptions */ }
        }
    }
}


