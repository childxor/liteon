using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IPS_TH.Models
{ 
    public class sys_module
    {
        [Key]
        public int Id { get; set; } // รหัสโมดูล (Auto Increment)

        public string? Name { get; set; } // ชื่อโมดูล (เช่น "Employee", "Leave", "Report")

        public string? Description { get; set; } // คำอธิบายเกี่ยวกับโมดูล

        public string? Controller { get; set; } // ชื่อ Controller ที่เกี่ยวข้อง

        public string? Action { get; set; } // ชื่อ Action ที่เกี่ยวข้อง

        public int? ParentId { get; set; } // รหัสโมดูลหลัก (สำหรับโมดูลที่มีซับโมดูล)

        public string? NameEn { get; set; } // ชื่อโมดูล (เช่น "Employee", "Leave", "Report")

        public string? NameChina { get; set; } // ชื่อโมดูล (เช่น "Employee", "Leave", "Report")

        public bool IsActive { get; set; } // สถานะการใช้งานของโมดูล

        public int Order { get; set; } // ลำดับของโมดูล

        public DateTime CreatedAt { get; set; } = DateTime.Now; // วันที่สร้าง

        public DateTime UpdatedAt { get; set; } = DateTime.Now; // วันที่ปรับปรุง

        public string? CreatedBy { get; set; } // ผู้สร้าง

        public string? UpdatedBy { get; set; } // ผู้ปรับปรุง

        public string? RecordStatus { get; set; } = "N";

        // ฟิลด์ใหม่สำหรับเก็บข้อมูลไอคอน
        public string? Icon { get; set; } // ชื่อหรือ URL ของไอคอน

        public List<sys_module_permission> Permissions { get; set; } = new List<sys_module_permission>(); // รายการสิทธิ์ที่เกี่ยวข้องกับโมดูล
    }

    public class sys_module_permission
    {
        [Key] // เพิ่ม Primary Key
        public int Id { get; set; } // รหัสสิทธิ์ (Auto Increment)
        
        public int RoleId { get; set; } // รหัสบทบาท
        public bool CanView { get; set; } // สิทธิ์ในการดู
        public bool CanCreate { get; set; } // สิทธิ์ในการสร้าง
        public bool CanEdit { get; set; } // สิทธิ์ในการแก้ไข
        public bool CanDelete { get; set; } // สิทธิ์ในการลบ
        public bool CanPrint { get; set; } // สิทธิ์ในการพิมพ์
        public bool CanExport { get; set; } // สิทธิ์ในการส่งออก
        public bool CanImport { get; set; } // สิทธิ์ในการนำเข้า
        public bool CanApprove { get; set; } // สิทธิ์ในการอนุมัติ
        public bool CanReject { get; set; } // สิทธิ์ในการปฏิเสธ
        public bool CanCancel { get; set; } // สิทธิ์ในการยกเลิก
        public bool CanUpload { get; set; } // สิทธิ์ในการอัปโหลด
        public bool CanDownload { get; set; } // สิทธิ์ในการดาวน์โหลด
    }
}
