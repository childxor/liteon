using System.ComponentModel.DataAnnotations;

namespace IPS_TH.Models.ESMMS
{
    public class esmms_exam
    {
        [Key]
        public int id { get; set; }

        [Required]
        [StringLength(200)]
        public string title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? description { get; set; }

        // จำนวนข้อที่จะสุ่มจากธนาคารข้อสอบ
        public int random_question_count { get; set; }

        // ระยะเวลาทำข้อสอบ (นาที) หากต้องการจำกัดเวลา
        public int? duration_minutes { get; set; }

        public DateTime? start_at { get; set; }

        public DateTime? end_at { get; set; }

        // จำนวนครั้งที่อนุญาตให้ทำได้
        public int? attempts_limit { get; set; }

        // กลุ่มพนักงานเป้าหมาย (เช่น รหัสกลุ่ม/แผนก หรือ CSV)
        [StringLength(200)]
        public string? target_group { get; set; }

        // ลิงก์ Microsoft Forms ที่ใช้ทำข้อสอบ
        [StringLength(500)]
        public string? forms_url { get; set; }

        // foreign key ไปยังคอร์สที่เกี่ยวข้อง
        public int? course_id { get; set; }

        public bool is_active { get; set; } = true;

        public DateTime created_at { get; set; } = DateTime.Now;

        public DateTime updated_at { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? created_by { get; set; }

        [StringLength(50)]
        public string? updated_by { get; set; }
        
        //พาธเก็บ excel file .xlsx
        [StringLength(500)]
        public string? excel_path { get; set; }
    }
}


