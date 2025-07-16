using System;
using System.Collections.Generic; // Added for ICollection
using System.ComponentModel.DataAnnotations;

namespace IPS_TH.Models.AssetFactory
{
    public class Asset
    {
        public int Id { get; set; }

        [Display(Name = "รหัสทรัพย์สิน")]
        [StringLength(50, ErrorMessage = "รหัสทรัพย์สินต้องไม่เกิน 50 ตัวอักษร")]
        public string? AssetNo { get; set; }

        [Display(Name = "Serial Number")]
        [StringLength(100, ErrorMessage = "Serial Number ต้องไม่เกิน 100 ตัวอักษร")]
        public string? ProductSerial { get; set; }

        [Display(Name = "ชื่อทรัพย์สิน")]
        [StringLength(200, ErrorMessage = "ชื่อทรัพย์สินต้องไม่เกิน 200 ตัวอักษร")]
        public string? Name { get; set; }

        [Display(Name = "ประเภททรัพย์สิน")]
        public string? Type { get; set; }

        [Display(Name = "สถานะ")]
        public string? Status { get; set; }

        [Display(Name = "รหัสลูกค้า")]
        [StringLength(50, ErrorMessage = "รหัสลูกค้าต้องไม่เกิน 50 ตัวอักษร")]
        public string? CustomerNo { get; set; }

        [Display(Name = "ชื่อลูกค้า")]
        [StringLength(200, ErrorMessage = "ชื่อลูกค้าต้องไม่เกิน 200 ตัวอักษร")]
        public string? CustomerName { get; set; }

        [Display(Name = "รหัสแผนก")]
        [StringLength(20, ErrorMessage = "รหัสแผนกต้องไม่เกิน 20 ตัวอักษร")]
        public string? Department { get; set; }

        [Display(Name = "ชื่อแผนก")]
        [StringLength(100, ErrorMessage = "ชื่อแผนกต้องไม่เกิน 100 ตัวอักษร")]
        public string? DepartmentName { get; set; }

        [Display(Name = "รหัสไลน์")]
        [StringLength(20, ErrorMessage = "รหัสไลน์ต้องไม่เกิน 20 ตัวอักษร")]
        public string? Line { get; set; }

        [Display(Name = "ชื่อไลน์")]
        [StringLength(100, ErrorMessage = "ชื่อไลน์ต้องไม่เกิน 100 ตัวอักษร")]
        public string? LineName { get; set; }

        [Display(Name = "รหัสผู้รับผิดชอบ")]
        [StringLength(20, ErrorMessage = "รหัสผู้รับผิดชอบต้องไม่เกิน 20 ตัวอักษร")]
        public string? Owner { get; set; }

        [Display(Name = "ชื่อผู้รับผิดชอบ")]
        [StringLength(200, ErrorMessage = "ชื่อผู้รับผิดชอบต้องไม่เกิน 200 ตัวอักษร")]
        public string? OwnerName { get; set; }

        [Display(Name = "สถานที่จัดเก็บ")]
        [StringLength(50, ErrorMessage = "สถานที่จัดเก็บต้องไม่เกิน 50 ตัวอักษร")]
        public string? Location { get; set; }

        [Display(Name = "รายละเอียดสถานที่")]
        [StringLength(500, ErrorMessage = "รายละเอียดสถานที่ต้องไม่เกิน 500 ตัวอักษร")]
        public string? LocationDetail { get; set; }

        [Display(Name = "รุ่น/Model")]
        [StringLength(100, ErrorMessage = "รุ่น/Model ต้องไม่เกิน 100 ตัวอักษร")]
        public string? Model { get; set; }

        [Display(Name = "ผู้ผลิต")]
        [StringLength(100, ErrorMessage = "ผู้ผลิตต้องไม่เกิน 100 ตัวอักษร")]
        public string? Manufacturer { get; set; }

        [Display(Name = "ประเทศผู้ผลิต")]
        [StringLength(50, ErrorMessage = "ประเทศผู้ผลิตต้องไม่เกิน 50 ตัวอักษร")]
        public string? CountryOfOrigin { get; set; }

        [Display(Name = "เวอร์ชั่น Windows")]
        [StringLength(50, ErrorMessage = "เวอร์ชั่น Windows ต้องไม่เกิน 50 ตัวอักษร")]
        public string? WindowsVersion { get; set; }

        [Display(Name = "วันที่ติดตั้ง")]
        [DataType(DataType.Date)]
        public DateTime? InstallationDate { get; set; }

        [Display(Name = "วันที่เริ่มใช้งาน")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "วันที่สิ้นสุดการใช้งาน")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "หมายเหตุ")]
        [StringLength(1000, ErrorMessage = "หมายเหตุต้องไม่เกิน 1000 ตัวอักษร")]
        public string? Remarks { get; set; }

        [Display(Name = "วันที่สร้าง")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "ผู้สร้าง")]
        [StringLength(50, ErrorMessage = "ผู้สร้างต้องไม่เกิน 50 ตัวอักษร")]
        public string? CreatedBy { get; set; }

        [Display(Name = "วันที่แก้ไข")]
        public DateTime? UpdatedDate { get; set; }

        [Display(Name = "ผู้แก้ไข")]
        [StringLength(50, ErrorMessage = "ผู้แก้ไขต้องไม่เกิน 50 ตัวอักษร")]
        public string? UpdatedBy { get; set; }

        [Display(Name = "สถานะการลบ")]
        public bool IsDeleted { get; set; } = false;

        [Display(Name = "วันที่ลบ")]
        public DateTime? DeletedDate { get; set; }

        [Display(Name = "ผู้ลบ")]
        [StringLength(50, ErrorMessage = "ผู้ลบต้องไม่เกิน 50 ตัวอักษร")]
        public string? DeletedBy { get; set; }

        // Navigation Properties
        public virtual ICollection<AssetMacAddress> MacAddresses { get; set; }
        public virtual ICollection<AssetHistory> Histories { get; set; }
    }

    // ตารางเก็บ Mac Address
    public class AssetMacAddress
    {
        public int Id { get; set; }

        [Required]
        public int AssetId { get; set; }

        [Required]
        [Display(Name = "Mac Address")]
        [StringLength(17, ErrorMessage = "Mac Address ต้องไม่เกิน 17 ตัวอักษร")]
        public string MacAddress { get; set; }

        [Display(Name = "ประเภท Network")]
        [StringLength(20, ErrorMessage = "ประเภท Network ต้องไม่เกิน 20 ตัวอักษร")]
        public string NetworkType { get; set; } // เช่น Ethernet, WiFi, Bluetooth

        [Display(Name = "หมายเหตุ")]
        [StringLength(200, ErrorMessage = "หมายเหตุต้องไม่เกิน 200 ตัวอักษร")]
        public string? Remarks { get; set; }

        [Display(Name = "วันที่สร้าง")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "ผู้สร้าง")]
        [StringLength(50, ErrorMessage = "ผู้สร้างต้องไม่เกิน 50 ตัวอักษร")]
        public string CreatedBy { get; set; }

        // Navigation Property
        public virtual Asset Asset { get; set; }
    }

    // ตารางเก็บประวัติการเปลี่ยนแปลง
    public class AssetHistory
    {
        public int Id { get; set; }

        [Required]
        public int AssetId { get; set; }

        [Required]
        [Display(Name = "ประเภทการเปลี่ยนแปลง")]
        [StringLength(20, ErrorMessage = "ประเภทการเปลี่ยนแปลงต้องไม่เกิน 20 ตัวอักษร")]
        public string ChangeType { get; set; } // Create, Update, Delete, Assign, Transfer

        [Display(Name = "ฟิลด์ที่เปลี่ยนแปลง")]
        [StringLength(50, ErrorMessage = "ฟิลด์ที่เปลี่ยนแปลงต้องไม่เกิน 50 ตัวอักษร")]
        public string FieldName { get; set; }

        [Display(Name = "ค่าเดิม")]
        public string? OldValue { get; set; }

        [Display(Name = "ค่าใหม่")]
        public string? NewValue { get; set; }

        [Display(Name = "รายละเอียด")]
        [StringLength(500, ErrorMessage = "รายละเอียดต้องไม่เกิน 500 ตัวอักษร")]
        public string? Description { get; set; }

        [Display(Name = "วันที่เปลี่ยนแปลง")]
        public DateTime ChangeDate { get; set; } = DateTime.Now;

        [Display(Name = "ผู้เปลี่ยนแปลง")]
        [StringLength(50, ErrorMessage = "ผู้เปลี่ยนแปลงต้องไม่เกิน 50 ตัวอักษร")]
        public string ChangedBy { get; set; }

        [Display(Name = "IP Address")]
        [StringLength(15, ErrorMessage = "IP Address ต้องไม่เกิน 15 ตัวอักษร")]
        public string IpAddress { get; set; }

        // Navigation Property
        public virtual Asset Asset { get; set; }
    }

    // Enum สำหรับประเภททรัพย์สิน
    public static class AssetType
    {
        public const string Computer = "1";
        public const string Laptop = "2";
        public const string Tablet = "3";
        public const string Phone = "4";
        public const string Printer = "5";
        public const string NetworkDevice = "6";
        public const string Machine = "7";
        public const string Tool = "8";
        public const string Equipment = "9";
        public const string Other = "10";

        public static string GetDisplayName(string type)
        {
            return type switch
            {
                Computer => "คอมพิวเตอร์",
                Laptop => "โน๊ตบุ๊ค",
                Tablet => "แท็บเล็ต",
                Phone => "โทรศัพท์",
                Printer => "เครื่องพิมพ์",
                NetworkDevice => "อุปกรณ์เครือข่าย",
                Machine => "เครื่องจักร",
                Tool => "เครื่องมือ",
                Equipment => "อุปกรณ์",
                Other => "อื่นๆ",
                _ => "ไม่ระบุ",
            };
        }

        // เมธอดสำหรับดึงประเภททั้งหมด
        public static Dictionary<string, string> GetAllTypes()
        {
            return new Dictionary<string, string>
            {
                { Computer, GetDisplayName(Computer) },
                { Laptop, GetDisplayName(Laptop) },
                { Tablet, GetDisplayName(Tablet) },
                { Phone, GetDisplayName(Phone) },
                { Printer, GetDisplayName(Printer) },
                { NetworkDevice, GetDisplayName(NetworkDevice) },
                { Machine, GetDisplayName(Machine) },
                { Tool, GetDisplayName(Tool) },
                { Equipment, GetDisplayName(Equipment) },
                { Other, GetDisplayName(Other) }
            };
        }

        // เมธอดสำหรับดึงเฉพาะ value ทั้งหมด
        public static string[] GetAllValues()
        {
            return new[] { Computer, Laptop, Tablet, Phone, Printer, NetworkDevice, Machine, Tool, Equipment, Other };
        }
    }

    // Enum สำหรับสถานะทรัพย์สิน
    public static class AssetStatus
    {
        public const string Active = "1";
        public const string Repair = "2";
        public const string Damaged = "3";
        public const string Disposed = "4";
        public const string Inactive = "5";
        public const string Spare = "6";

        public static string GetDisplayName(string status)
        {
            return status switch
            {
                Active => "ใช้งานได้",
                Repair => "กำลังซ่อม",
                Damaged => "ชำรุด",
                Disposed => "จำหน่าย",
                Inactive => "ไม่ใช้งาน",
                Spare => "อะไหล่",
                _ => "ไม่ระบุ",
            };
        }
    }

    // Enum สำหรับสถานที่จัดเก็บ
    public static class AssetLocation
    {
        public const string ProductionArea = "PD";
        public const string RawMaterialWH = "RMW";
        public const string ElectronicWH = "EW";
        public const string SemiFinishedWH = "RSFPW";
        public const string DQELab = "DQE";
        public const string IQCRoom = "IQC";
        public const string StoreArea = "FA";
        public const string SparePartsRoom = "JIG";
        public const string ServerRoom = "DCC";
        public const string Office = "POR";
        public const string BossRoom = "MR";
        public const string MeetingRoom = "BTR";
        public const string TrainingRoom = "TC";
        public const string ConferenceRoom = "CF";
        public const string ProductionOffice = "OFPD";
        public const string PrintingRoom = "PR";
        public const string ColorMixingRoom = "OM";
        public const string ScreenRoom = "SC";
        public const string SecurityRoom = "PTR";
        public const string StaffDormitory = "SLSC";
        public const string ServerRoom2 = "MICR";
        public const string ChangingRoom = "CR";

        public static string GetDisplayName(string location)
        {
            return location switch
            {
                ProductionArea => "พื้นที่ไลน์ผลิต",
                RawMaterialWH => "Raw material WH",
                ElectronicWH => "Electronic WH",
                SemiFinishedWH => "Raw materials & semi Finished WH",
                DQELab => "DQE LAB",
                IQCRoom => "IQC room",
                StoreArea => "พื้นที่รับของสโตร์",
                SparePartsRoom => "ห้องเก็บอะไหล่",
                ServerRoom => "ห้องเก็บของเล็กข้างห้องเซิฟเวอร์",
                Office => "ออฟฟิศ",
                BossRoom => "ห้องบอส",
                MeetingRoom => "ห้องประชุม",
                TrainingRoom => "ห้องเทรนงานชั้น 2",
                ConferenceRoom => "ห้องประชุมชั้น 2",
                ProductionOffice => "ออฟฟิศไลน์ผลิต",
                PrintingRoom => "ห้องปลิ้นติ้ง",
                ColorMixingRoom => "ห้องผสมสี",
                ScreenRoom => "ห้องแผ่นสกีนรูม",
                SecurityRoom => "ห้องยาม",
                StaffDormitory => "ที่พักพนักงาน",
                ServerRoom2 => "ห้องเซิฟเวอร์",
                ChangingRoom => "ห้องเปลี่ยนรองเท้า",
                _ => "ไม่ระบุ",
            };
        }
    }

    // Enum สำหรับ Windows Version
    public static class WindowsVersion
    {
        public const string Windows10 = "Windows 10";
        public const string Windows11 = "Windows 11";
        public const string WindowsServer2019 = "Windows Server 2019";
        public const string WindowsServer2022 = "Windows Server 2022";
        public const string Other = "อื่นๆ";

        public static string[] GetVersions()
        {
            return new[] { Windows10, Windows11, WindowsServer2019, WindowsServer2022, Other };
        }
    }

    // Model สำหรับการเปลี่ยนสถานะ
    public class ChangeStatusModel
    {
        public int Id { get; set; }
        public string Status { get; set; }
    }
}
