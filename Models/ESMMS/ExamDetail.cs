using System.ComponentModel.DataAnnotations;

namespace IPS_TH.Models.ESMMS
{
    public class esmms_exam_detail
    {
        [Key]
        public int id { get; set; }

        public int exam_id { get; set; }

        public int course_id { get; set; }

        public int question_id { get; set; }

        // ลำดับข้อ
        public int sort_order { get; set; }

        public DateTime created_at { get; set; } = DateTime.Now;

        public DateTime updated_at { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? created_by { get; set; }

        [StringLength(50)]
        public string? updated_by { get; set; }
    }
}


