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

        [StringLength(500)]
        public string? choice_e { get; set; }

        [StringLength(500)]
        public string? choice_f { get; set; }

        [StringLength(500)]
        public string? choice_g { get; set; } 

        [StringLength(500)]
        public string? choice_h { get; set; }

        // รูปภาพประกอบของตัวเลือก (path ใต้ wwwroot)
        [StringLength(255)]
        public string? choice_a_image_path { get; set; }

        [StringLength(255)]
        public string? choice_b_image_path { get; set; }

        [StringLength(255)]
        public string? choice_c_image_path { get; set; }

        [StringLength(255)]
        public string? choice_d_image_path { get; set; }

        [StringLength(255)]
        public string? choice_e_image_path { get; set; }

        [StringLength(255)]
        public string? choice_f_image_path { get; set; }

        [StringLength(255)]
        public string? choice_g_image_path { get; set; }

        [StringLength(255)]
        public string? choice_h_image_path { get; set; }

        // คำตอบที่ถูกต้อง (เก็บเป็นข้อความจริง)
        [Required]
        [StringLength(1)]
        public string correct_answer { get; set; } = string.Empty;

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


