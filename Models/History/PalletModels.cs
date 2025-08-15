using System;

namespace IPS_TH.Models.History
{
    public class PalletInfo
    {
        public string PalletId { get; set; }
        public string WorkOrder { get; set; }
        public decimal PartId { get; set; }
        public string CloseFlag { get; set; }
        public decimal TerminalId { get; set; }
        public decimal CreateEmpId { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? CloseTime { get; set; }
        public decimal? CloseEmpId { get; set; }
        public string QcFlag { get; set; }
        public string MixFlag { get; set; }
        public string PalletVolume { get; set; }
        public string FullFlag { get; set; }
        public string TerminalName { get; set; }
        public int SerialCount { get; set; }
    }

    public class CartonInfo
    {
        public string CartonId { get; set; }
        public string WorkOrder { get; set; }
        public decimal PartId { get; set; }
        public string CloseFlag { get; set; }
        public decimal TerminalId { get; set; }
        public decimal CreateEmpId { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? CloseTime { get; set; }
        public decimal? CloseEmpId { get; set; }
        public string QcFlag { get; set; }
        public string MixFlag { get; set; }
        public string FullFlag { get; set; }
        public string TerminalName { get; set; }
        public int SerialCount { get; set; }
    }

    public class SerialInfo
    {
        public string SerialNumber { get; set; }
        public string WorkOrder { get; set; }
        public decimal PartId { get; set; }
        public string CurrentStatus { get; set; }
        public string WorkFlag { get; set; }
        public string CartonNo { get; set; }
        public DateTime? InProcessTime { get; set; }
        public DateTime? OutProcessTime { get; set; }
    }

    public class CreatePalletDto
    {
        public string PalletNo { get; set; }
        public string WorkOrder { get; set; }
        public decimal PartId { get; set; }
        public decimal TerminalId { get; set; }
        public decimal CreateEmpId { get; set; }
        public string QcFlag { get; set; }
        public string MixFlag { get; set; }
    }

    public class ClosePalletDto
    {
        public string PalletNo { get; set; }
        public decimal CloseEmpId { get; set; }
        public string FullFlag { get; set; }
    }

    public class DeleteCartonDto
    {
        public string CartonNo { get; set; }
    }

    public class CloseCartonDto
    {
        public string CartonNo { get; set; } = string.Empty;
        public decimal CloseEmpId { get; set; }
    }

    public class DeletePalletDto
    {
        public string PalletNo { get; set; }
    }

    public class OpenCartonDto
    {
        public string CartonNo { get; set; } = string.Empty;
        public decimal OpenEmpId { get; set; }
        
        // Constructor เพื่อให้แน่ใจว่าข้อมูลไม่เป็น null
        public OpenCartonDto()
        {
            CartonNo = string.Empty;
            OpenEmpId = 0;
        }
        
        public OpenCartonDto(string cartonNo, decimal openEmpId)
        {
            CartonNo = cartonNo ?? string.Empty;
            OpenEmpId = openEmpId;
        }
    }

    public class OpenPalletDto
    {
        public string PalletNo { get; set; }
        public decimal OpenEmpId { get; set; }
    }

    public class MoveCartonDto
    {
        public string CartonNo { get; set; }
        public string TargetPalletNo { get; set; }
        public decimal MoveEmpId { get; set; }
    }

    public class MoveSerialNumbersDto
    {
        public string SourceCartonNo { get; set; }
        public string TargetCartonNo { get; set; }
        public decimal MoveEmpId { get; set; }
    }
} 