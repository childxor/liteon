using System.ComponentModel.DataAnnotations;

namespace IPS_TH.Models.ESMMS
{
    public class esmms_exam_employee
    {
        [Key]
        public int id { get; set; }

        // รหัสพนักงาน/เลขพนักงานที่เข้าทดสอบ
        [Required]
        [StringLength(50)]
        public string emp_no { get; set; } = string.Empty;

        // อ้างอิงคอร์สที่ผูกกับการทดสอบ (เพื่อคำนวณวันหมดอายุอิงจาก course.expire_period)
        public int? course_id { get; set; }

        // อ้างอิงไลน์ผลิต
        public string? pd_line { get; set; }

        // อ้างอิงชุดข้อสอบ (ถ้ามี)
        public int? exam_id { get; set; }

        // เก็บสแน็ปช็อตช่วงอายุจากคอร์ส ณ วันที่กำหนด (เช่น "1 year")
        [StringLength(50)]
        public string? expire_period_snapshot { get; set; }

        // วันหมดอายุสิทธิ์ทำ/ผ่านการทดสอบ ตาม period ที่กำหนด
        public DateTime? expire_at { get; set; }

        public bool is_active { get; set; } = true;

        // คะแนนที่ผ่าน
        public int? score { get; set; }

        public DateTime assigned_at { get; set; } = DateTime.Now;

        public DateTime created_at { get; set; } = DateTime.Now;

        public DateTime updated_at { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? created_by { get; set; }

        [StringLength(50)]
        public string? updated_by { get; set; }

        public bool? is_passed { get; set; }
        public int? pass_score { get; set; }

    }
}


