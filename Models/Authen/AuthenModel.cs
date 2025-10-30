using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPS_TH.Models
{
    public class sys_user
    {
        [Key]
        public int Id { get; set; }

        public string? Username { get; set; }

        public string? PasswordDefault { get; set; }

        public string? Password { get; set; }

        public string? PasswordHash { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        public string? Roles { get; set; } // อาจจะต้องมีการกำหนดค่าเริ่มต้น

        public string? CreatedBy { get; set; } // อาจจะต้องมีการกำหนดค่าเริ่มต้น

        public string? UpdatedBy { get; set; } // อาจจะต้องมีการกำหนดค่าเริ่มต้น

        public string? ProfilePictureUrl { get; set; } // อาจจะต้องมีการกำหนดค่าเริ่มต้น

        public DateTime? CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; } = DateTime.Now;

        public bool? IsActive { get; set; } = true;

        public bool? IsEmailVerified { get; set; } = false;

        public DateTime? LastLogin { get; set; } = DateTime.Now;

        public string? Language { get; set; } = "th";

        public string? RecordStatus { get; set; } = "N";

        public string? Department { get; set; }

        public string? Emp_no { get; set; }

        public string? WorkArea { get; set; }

        // เพิ่มการเชื่อมโยงกับ sys_user_role
        public virtual ICollection<sys_user_role> UserRoles { get; set; } // เพิ่มการเชื่อมโยง
    }

    public class sys_user_role
    {
        [Key]
        public int UserId { get; set; }

        public int RoleId { get; set; }

        public string? CreatedBy { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; } = DateTime.Now;

        public string? RecordStatus { get; set; } = "N";
    }

    public class sys_role
    {
        [Key]
        public int Id { get; set; }

        public string? RoleName { get; set; }

        public string? RoleDescription { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; } = DateTime.Now;

        public string? CreatedBy { get; set; }

        public string? UpdatedBy { get; set; }

        public string? RecordStatus { get; set; } = "N";
    }

    public class sys_role_detail
    {
        [Key]
        public int Id { get; set; }
        public int RoleId { get; set; }

        public int ModuleId { get; set; }

        public bool IsAdd { get; set; } = false;

        public bool IsApprove { get; set; } = false;

        public bool IsDelete { get; set; } = false;

        public bool IsEdit { get; set; } = false;

        public bool IsReport { get; set; } = false;

        public bool IsView { get; set; } = false;
    }

    public class sys_user_feedback
    {
        public int id { get; set; } // รหัสความคิดเห็น (pk)
        public string? username { get; set; } // ชื่อผู้แสดงความคิดเห็น (null ได้ถ้า anonymous)
        public string? department { get; set; } // แผนก (ถ้ามี)
        public string type { get; set; } // ประเภทความคิดเห็น (feature, bug, improvement, other)
        public string title { get; set; } // หัวข้อ
        public string content { get; set; } // รายละเอียด
        public bool isanonymous { get; set; } // ส่งแบบไม่ระบุตัวตนหรือไม่
        public DateTime createdat { get; set; } // วันที่/เวลาที่แสดงความคิดเห็น
        public int likecount { get; set; } // จำนวนไลค์
        public int commentcount { get; set; } // จำนวนคอมเมนต์ (ถ้ามีระบบตอบกลับ)
    } 

    public class sys_user_feedback_comment
    {
        public int id { get; set; } // รหัสความคิดเห็น (pk)
        public int feedbackid { get; set; } // รหัสความคิดเห็น (fk)
        public string? username { get; set; } // ชื่อผู้แสดงความคิดเห็น (null ได้ถ้า anonymous)
        public string? content { get; set; } // รายละเอียด
        public DateTime createdat { get; set; } // วันที่/เวลาที่แสดงความคิดเห็น
    }

    public class sys_user_feedback_like
    {
        public int id { get; set; } // รหัสความคิดเห็น (pk)
        public int feedbackid { get; set; } // รหัสความคิดเห็น (fk)
        public string? status { get; set; } // สถานะ (like, dislike)
        public string? username { get; set; } // ชื่อผู้แสดงความคิดเห็น (null ได้ถ้า anonymous)
    }
}
