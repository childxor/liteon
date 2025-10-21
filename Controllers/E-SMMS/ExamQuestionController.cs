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
            await LoadPermissions("ExamQuestion", "Index");

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

            // อัปโหลดรูปคำถามถ้ามี
            if (image != null && image.Length > 0)
            {
                input.image_path = await SaveUploadedImage(image, "q");
            }

            // อัปโหลดรูปตัวเลือก A–H ถ้ามี (ชื่อฟิลด์ image_a ... image_h)
            var fA = Request?.Form?.Files?.GetFile("image_a");
            var fB = Request?.Form?.Files?.GetFile("image_b");
            var fC = Request?.Form?.Files?.GetFile("image_c");
            var fD = Request?.Form?.Files?.GetFile("image_d");
            var fE = Request?.Form?.Files?.GetFile("image_e");
            var fF = Request?.Form?.Files?.GetFile("image_f");
            var fG = Request?.Form?.Files?.GetFile("image_g");
            var fH = Request?.Form?.Files?.GetFile("image_h");
            if (fA != null && fA.Length > 0) input.choice_a_image_path = await SaveUploadedImage(fA, "qa");
            if (fB != null && fB.Length > 0) input.choice_b_image_path = await SaveUploadedImage(fB, "qb");
            if (fC != null && fC.Length > 0) input.choice_c_image_path = await SaveUploadedImage(fC, "qc");
            if (fD != null && fD.Length > 0) input.choice_d_image_path = await SaveUploadedImage(fD, "qd");
            if (fE != null && fE.Length > 0) input.choice_e_image_path = await SaveUploadedImage(fE, "qe");
            if (fF != null && fF.Length > 0) input.choice_f_image_path = await SaveUploadedImage(fF, "qf");
            if (fG != null && fG.Length > 0) input.choice_g_image_path = await SaveUploadedImage(fG, "qg");
            if (fH != null && fH.Length > 0) input.choice_h_image_path = await SaveUploadedImage(fH, "qh");

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
                input.image_path = await SaveUploadedImage(image, "q");
            }

            var fA = Request?.Form?.Files?.GetFile("image_a");
            var fB = Request?.Form?.Files?.GetFile("image_b");
            var fC = Request?.Form?.Files?.GetFile("image_c");
            var fD = Request?.Form?.Files?.GetFile("image_d");
            var fE = Request?.Form?.Files?.GetFile("image_e");
            var fF = Request?.Form?.Files?.GetFile("image_f");
            var fG = Request?.Form?.Files?.GetFile("image_g");
            var fH = Request?.Form?.Files?.GetFile("image_h");
            if (fA != null && fA.Length > 0) input.choice_a_image_path = await SaveUploadedImage(fA, "qa");
            if (fB != null && fB.Length > 0) input.choice_b_image_path = await SaveUploadedImage(fB, "qb");
            if (fC != null && fC.Length > 0) input.choice_c_image_path = await SaveUploadedImage(fC, "qc");
            if (fD != null && fD.Length > 0) input.choice_d_image_path = await SaveUploadedImage(fD, "qd");
            if (fE != null && fE.Length > 0) input.choice_e_image_path = await SaveUploadedImage(fE, "qe");
            if (fF != null && fF.Length > 0) input.choice_f_image_path = await SaveUploadedImage(fF, "qf");
            if (fG != null && fG.Length > 0) input.choice_g_image_path = await SaveUploadedImage(fG, "qg");
            if (fH != null && fH.Length > 0) input.choice_h_image_path = await SaveUploadedImage(fH, "qh");

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
            entity.choice_e = input.choice_e;
            entity.choice_f = input.choice_f;
            entity.choice_g = input.choice_g;
            entity.choice_h = input.choice_h;
            entity.correct_answer = input.correct_answer;
            entity.score = input.score;
            entity.is_active = input.is_active;

            if (image != null && image.Length > 0)
            {
                var oldImage = entity.image_path;
                entity.image_path = await SaveUploadedImage(image, "q");
                DeleteImageFileIfExists(oldImage);
            }

            var fA = Request?.Form?.Files?.GetFile("image_a");
            var fB = Request?.Form?.Files?.GetFile("image_b");
            var fC = Request?.Form?.Files?.GetFile("image_c");
            var fD = Request?.Form?.Files?.GetFile("image_d");
            var fE = Request?.Form?.Files?.GetFile("image_e");
            var fF = Request?.Form?.Files?.GetFile("image_f");
            var fG = Request?.Form?.Files?.GetFile("image_g");
            var fH = Request?.Form?.Files?.GetFile("image_h");
            if (fA != null && fA.Length > 0) { var old = entity.choice_a_image_path; entity.choice_a_image_path = await SaveUploadedImage(fA, "qa"); DeleteImageFileIfExists(old); }
            if (fB != null && fB.Length > 0) { var old = entity.choice_b_image_path; entity.choice_b_image_path = await SaveUploadedImage(fB, "qb"); DeleteImageFileIfExists(old); }
            if (fC != null && fC.Length > 0) { var old = entity.choice_c_image_path; entity.choice_c_image_path = await SaveUploadedImage(fC, "qc"); DeleteImageFileIfExists(old); }
            if (fD != null && fD.Length > 0) { var old = entity.choice_d_image_path; entity.choice_d_image_path = await SaveUploadedImage(fD, "qd"); DeleteImageFileIfExists(old); }
            if (fE != null && fE.Length > 0) { var old = entity.choice_e_image_path; entity.choice_e_image_path = await SaveUploadedImage(fE, "qe"); DeleteImageFileIfExists(old); }
            if (fF != null && fF.Length > 0) { var old = entity.choice_f_image_path; entity.choice_f_image_path = await SaveUploadedImage(fF, "qf"); DeleteImageFileIfExists(old); }
            if (fG != null && fG.Length > 0) { var old = entity.choice_g_image_path; entity.choice_g_image_path = await SaveUploadedImage(fG, "qg"); DeleteImageFileIfExists(old); }
            if (fH != null && fH.Length > 0) { var old = entity.choice_h_image_path; entity.choice_h_image_path = await SaveUploadedImage(fH, "qh"); DeleteImageFileIfExists(old); }

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
            DeleteImageFileIfExists(entity.choice_a_image_path);
            DeleteImageFileIfExists(entity.choice_b_image_path);
            DeleteImageFileIfExists(entity.choice_c_image_path);
            DeleteImageFileIfExists(entity.choice_d_image_path);
            DeleteImageFileIfExists(entity.choice_e_image_path);
            DeleteImageFileIfExists(entity.choice_f_image_path);
            DeleteImageFileIfExists(entity.choice_g_image_path);
            DeleteImageFileIfExists(entity.choice_h_image_path);
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
            entity.choice_e = input.choice_e;
            entity.choice_f = input.choice_f;
            entity.choice_g = input.choice_g;
            entity.choice_h = input.choice_h;
            entity.correct_answer = input.correct_answer;
            entity.score = input.score;
            entity.is_active = input.is_active;

            // เปลี่ยนรูปถ้ามีการอัปโหลดใหม่
            if (image != null && image.Length > 0)
            {
                var oldImage = entity.image_path;
                entity.image_path = await SaveUploadedImage(image, "q");
                DeleteImageFileIfExists(oldImage);
            }

            var fA = Request?.Form?.Files?.GetFile("image_a");
            var fB = Request?.Form?.Files?.GetFile("image_b");
            var fC = Request?.Form?.Files?.GetFile("image_c");
            var fD = Request?.Form?.Files?.GetFile("image_d");
            var fE = Request?.Form?.Files?.GetFile("image_e");
            var fF = Request?.Form?.Files?.GetFile("image_f");
            var fG = Request?.Form?.Files?.GetFile("image_g");
            var fH = Request?.Form?.Files?.GetFile("image_h");
            if (fA != null && fA.Length > 0) { var old = entity.choice_a_image_path; entity.choice_a_image_path = await SaveUploadedImage(fA, "qa"); DeleteImageFileIfExists(old); }
            if (fB != null && fB.Length > 0) { var old = entity.choice_b_image_path; entity.choice_b_image_path = await SaveUploadedImage(fB, "qb"); DeleteImageFileIfExists(old); }
            if (fC != null && fC.Length > 0) { var old = entity.choice_c_image_path; entity.choice_c_image_path = await SaveUploadedImage(fC, "qc"); DeleteImageFileIfExists(old); }
            if (fD != null && fD.Length > 0) { var old = entity.choice_d_image_path; entity.choice_d_image_path = await SaveUploadedImage(fD, "qd"); DeleteImageFileIfExists(old); }
            if (fE != null && fE.Length > 0) { var old = entity.choice_e_image_path; entity.choice_e_image_path = await SaveUploadedImage(fE, "qe"); DeleteImageFileIfExists(old); }
            if (fF != null && fF.Length > 0) { var old = entity.choice_f_image_path; entity.choice_f_image_path = await SaveUploadedImage(fF, "qf"); DeleteImageFileIfExists(old); }
            if (fG != null && fG.Length > 0) { var old = entity.choice_g_image_path; entity.choice_g_image_path = await SaveUploadedImage(fG, "qg"); DeleteImageFileIfExists(old); }
            if (fH != null && fH.Length > 0) { var old = entity.choice_h_image_path; entity.choice_h_image_path = await SaveUploadedImage(fH, "qh"); DeleteImageFileIfExists(old); }

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
            DeleteImageFileIfExists(entity.choice_a_image_path);
            DeleteImageFileIfExists(entity.choice_b_image_path);
            DeleteImageFileIfExists(entity.choice_c_image_path);
            DeleteImageFileIfExists(entity.choice_d_image_path);
            DeleteImageFileIfExists(entity.choice_e_image_path);
            DeleteImageFileIfExists(entity.choice_f_image_path);
            DeleteImageFileIfExists(entity.choice_g_image_path);
            DeleteImageFileIfExists(entity.choice_h_image_path);
            _context.esmms_question.Remove(entity);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { course_code = redirectCourse });
        }

        private async Task<string> SaveUploadedImage(IFormFile file, string prefix)
        {
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "exam");
            if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);
            var sanitizedName = Path.GetFileName(file.FileName);
            var fileName = $"{prefix}_{DateTime.Now:yyyyMMddHHmmssfff}_{sanitizedName}";
            var fullPath = Path.Combine(uploadsDir, fileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return $"uploads/exam/{fileName}";
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


