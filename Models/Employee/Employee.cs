using System.ComponentModel.DataAnnotations;

namespace IPS_TH.Models.Employee
{
    public class emp_person
    {
        [Key]
        public int Id { get; set; }

        [StringLength(20)]
        public string? cardNumber { get; set; }

        [StringLength(50)]
        public string? personID { get; set; }

        [StringLength(100)]
        public string? name { get; set; }

        [StringLength(50)]
        public string? shiftMent { get; set; }

        [StringLength(50)]
        public string? deptID { get; set; }

        [StringLength(100)]
        public string? deptName { get; set; }

        [StringLength(12)]
        public string? leaveJobDate { get; set; }

        public DateTime? enableDate { get; set; }

        public DateTime? disableDate { get; set; }

        public int? userLevel { get; set; }

        [StringLength(20)]
        public string? password { get; set; }

        [StringLength(20)]
        public string? superPassword { get; set; }

        public DateTime? modifyTime { get; set; }

        public int? reserve1 { get; set; }

        public int? reserve2 { get; set; }

        [StringLength(50)]
        public string? reserve3 { get; set; }

        [StringLength(50)]
        public string? reserve4 { get; set; }

        [StringLength(50)]
        public string? reserveChar1 { get; set; }

        public int? cardType { get; set; }

        [StringLength(50)]
        public string? cardTypeDesc { get; set; }

        [StringLength(20)]
        public string? subSystem { get; set; }

        [StringLength(20)]
        public string? useCategory { get; set; }

        [StringLength(1)]
        public string? useStatus { get; set; }

        [StringLength(20)]
        public string? status { get; set; }

        [StringLength(40)]
        public string? cardCategory { get; set; }

        [StringLength(10)]
        public string? cardStatus { get; set; }

        public DateTime? CREATEDATE { get; set; }

        public string? RecordStatus { get; set; } = "N";

        public int isActive { get; set; }
    }

    public class emp_person_shift
    {
        [Key]
        public int Id { get; set; }

        [StringLength(255)]
        public string? name { get; set; }

        [StringLength(50)]
        public string? shiftMent { get; set; }

        [StringLength(50)]
        public string? personID { get; set; }

        [StringLength(50)]
        public string? deptCode { get; set; }

        [StringLength(255)]
        public string? deptName { get; set; }

        [StringLength(50)]
        public string? deptID { get; set; }

        public DateTime? date { get; set; }

        [StringLength(1)]
        public string? RecordStatus { get; set; } = "N";

        public int isActive { get; set; } = 1;
        
        // เพิ่มฟิลด์เวลาเข้างาน
        public DateTime? timeIn { get; set; }
        
        // เพิ่มฟิลด์เวลาออกงาน
        public DateTime? timeOut { get; set; }
        
        // เพิ่มฟิลด์หมายเหตุ
        [StringLength(255)]
        public string? remark { get; set; }
        
        // เพิ่มฟิลด์วันที่บันทึกข้อมูล
        public DateTime createdAt { get; set; } = DateTime.Now;
        
        // เพิ่มฟิลด์ผู้บันทึกข้อมูล
        [StringLength(50)]
        public string? createdBy { get; set; }
        
        // เพิ่มฟิลด์วันที่แก้ไขข้อมูล
        public DateTime? updatedAt { get; set; }
        
        // เพิ่มฟิลด์ผู้แก้ไขข้อมูล
        [StringLength(50)]
        public string? updatedBy { get; set; }
    }
}
