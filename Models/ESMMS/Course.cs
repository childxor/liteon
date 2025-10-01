using System.ComponentModel.DataAnnotations;

namespace IPS_TH.Models.ESMMS
{
    public class esmms_course
    {
        [Key]
        public int id { get; set; }

        [Required]
        [StringLength(20)]
        public string course_code { get; set; } = string.Empty;

        [StringLength(255)]
        public string? description_th { get; set; }

        [StringLength(255)] 
        public string? description_en { get; set; }

        public bool dl { get; set; } = false;

        public bool idl { get; set; } = false;

        public bool wi { get; set; } = false;

        // จำนวนการสุ่มข้อสอบ
        public int random_question_count { get; set; } = 10;

        // ตัวอย่างค่า: "1 year"
        [StringLength(50)]
        public string? expire_period { get; set; }

        [StringLength(100)]
        public string? trainer { get; set; }

        public DateTime created_at { get; set; } = DateTime.Now;

        public DateTime updated_at { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? created_by { get; set; }

        [StringLength(50)]
        public string? updated_by { get; set; }
    }
}


