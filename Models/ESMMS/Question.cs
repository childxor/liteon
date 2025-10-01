using System.ComponentModel.DataAnnotations;

namespace IPS_TH.Models.ESMMS
{
    public class esmms_question
    {
        [Key]
        public int id { get; set; }

        // อ้างอิงคอร์ส
        public int? course_id { get; set; }

        [Required]
        [StringLength(20)]
        public string course_code { get; set; } = string.Empty;

        // เนื้อหาคำถาม
        [Required]
        [StringLength(1000)]
        public string question_text { get; set; } = string.Empty;

        // รูปภาพประกอบคำถาม (path ใต้ wwwroot)
        [StringLength(255)]
        public string? image_path { get; set; }

        // ตัวเลือกคำตอบ
        [StringLength(500)]
        public string? choice_a { get; set; }

        [StringLength(500)]
        public string? choice_b { get; set; }
 
        [StringLength(500)]
        public string? choice_c { get; set; }

        [StringLength(500)]
        public string? choice_d { get; set; }

        // ตัวเลือกที่ถูกต้อง: A/B/C/D
        [Required]
        [StringLength(1)]
        public string correct_choice { get; set; } = "A";

        public int score { get; set; } = 1;

        public bool is_active { get; set; } = true;

        public DateTime created_at { get; set; } = DateTime.Now;

        public DateTime updated_at { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? created_by { get; set; }

        [StringLength(50)]
        public string? updated_by { get; set; }
    }
}


