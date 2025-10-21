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

    public string? corect_randoms { get; set; }

    // ตัวเลือกคำตอบ
    [StringLength(500)]
    public string? choice_a { get; set; }

    [StringLength(500)]
    public string? choice_b { get; set; }

    [StringLength(500)]
    public string? choice_c { get; set; }

    [StringLength(500)]
    public string? choice_d { get; set; }

    // ลำดับข้อ
    public int sort_order { get; set; }

    public DateTime created_at { get; set; } = DateTime.Now;

    public DateTime updated_at { get; set; } = DateTime.Now;

    [StringLength(50)]
    public string? created_by { get; set; }

    [StringLength(50)]
    public string? updated_by { get; set; }

    [StringLength(500)]
    public string? choice_a_image_path { get; set; }
    [StringLength(500)]
    public string? choice_b_image_path { get; set; }

    [StringLength(500)]
    public string? choice_c_image_path { get; set; }

    [StringLength(500)]
    public string? choice_d_image_path { get; set; }

    [StringLength(500)]
    public string? choice_e_image_path { get; set; }

    [StringLength(500)]
    public string? choice_f_image_path { get; set; }

    [StringLength(500)]
    public string? choice_g_image_path { get; set; }

    [StringLength(500)]
    public string? choice_h_image_path { get; set; }
  }
}


