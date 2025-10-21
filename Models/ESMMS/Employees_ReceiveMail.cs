using System;
using System.ComponentModel.DataAnnotations;

namespace IPS_TH.Models.ESMMS
{
    // ตารางเก็บรายการพนักงานที่อนุญาตให้ส่งเมลสรุปหลังอัปโหลด Excel สำเร็จ (Thai)
    // 允许在上传 Excel 完成后接收汇总邮件的员工白名单表 (简中)
    // คอลัมน์ใช้ชื่ออังกฤษตัวพิมพ์เล็กตาม requirement (Thai)
    // 所有列名使用英文小写 (简中)
    public class esmms_employees_receive_mail
    {
        [Key]
        public int id { get; set; }

        // รหัส/เลขพนักงาน (unique ต่อระบบ) (Thai)
        // 员工编号（系统唯一）(简中)
        [Required]
        [StringLength(50)] 
        public string emp_no { get; set; } = string.Empty;

        // อีเมลสำหรับรับสรุปผล (ควรเป็นรูปแบบอีเมล) (Thai)
        // 用于接收汇总的邮箱（应为邮箱格式）(简中)
        [Required]
        [StringLength(255)]
        public string email { get; set; } = string.Empty;

        // ชื่อ-นามสกุลของพนักงาน (Thai)
        // 员工姓名 (简中)
        [StringLength(255)]
        public string? full_name { get; set; }

        // หมายเหตุ: ใช้เชื่อมโยงเงื่อนไขการส่งสรุปตามคอร์ส (ถ้าต้องการจำกัด) (Thai)
        // 备注：用于按课程限制发送范围（可选）(简中) 
        public int? course_id { get; set; }

        // เปิด/ปิดการใช้งาน (Thai)
        // 启用/禁用 (简中)
        public bool is_active { get; set; } = true;

        // วันที่สร้าง/ปรับปรุง (Thai)
        // 创建/更新日期 (简中)
        public DateTime created_at { get; set; } = DateTime.Now;
        public DateTime updated_at { get; set; } = DateTime.Now;

        // ผู้สร้าง/ผู้แก้ไข (Thai)
        // 创建者/更新者 (简中)
        [StringLength(50)]
        public string? created_by { get; set; }

        [StringLength(50)]
        public string? updated_by { get; set; }
    }
}


