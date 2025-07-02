using System;
using System.ComponentModel.DataAnnotations;
    public class Asset
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "กรุณาระบุรหัสทรัพย์สิน")]
        [Display(Name = "รหัสทรัพย์สิน")]
        public string Code { get; set; }

        [Required(ErrorMessage = "กรุณาระบุชื่อทรัพย์สิน")]
        [Display(Name = "ชื่อทรัพย์สิน")]
        public string Name { get; set; }

        [Required(ErrorMessage = "กรุณาเลือกประเภททรัพย์สิน")]
        [Display(Name = "ประเภททรัพย์สิน")]
        public string Type { get; set; }

        [Required(ErrorMessage = "กรุณาเลือกสถานะ")]
        [Display(Name = "สถานะ")]
        public string Status { get; set; }

        [Display(Name = "ผู้รับผิดชอบ")]
        public string? OwnerId { get; set; }

        [Display(Name = "ชื่อผู้รับผิดชอบ")]
        public string? OwnerName { get; set; }

        [Display(Name = "แผนก")]
        public string? Department { get; set; }

        [Display(Name = "ชื่อแผนก")]
        public string? DepartmentName { get; set; }

        [Required(ErrorMessage = "กรุณาเลือกสถานที่จัดเก็บ")]
        [Display(Name = "สถานที่จัดเก็บ")]
        public string Location { get; set; }

        [Display(Name = "ไลน์ผลิต")]
        public string? ProductionLine { get; set; }

        [Display(Name = "วันที่รับ")]
        [DataType(DataType.Date)]
        public DateTime? PurchaseDate { get; set; }

        [Display(Name = "วันหมดประกัน")]
        [DataType(DataType.Date)]
        public DateTime? Warranty { get; set; }

        [Display(Name = "รายละเอียดเพิ่มเติม")]
        public string? Description { get; set; }

        [Display(Name = "วันที่สร้าง")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "ผู้สร้าง")]
        public string? CreatedBy { get; set; }

        [Display(Name = "วันที่แก้ไข")]
        public DateTime? UpdatedDate { get; set; }

        [Display(Name = "ผู้แก้ไข")]
        public string? UpdatedBy { get; set; }
    }
