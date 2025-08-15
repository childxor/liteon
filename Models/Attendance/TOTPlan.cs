using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IPS_TH.Models.Attendance
{
    [Table("TOTPlan")]
    public class TOTPlan
    {
        [Key]
        public string OTReqNo { get; set; } = string.Empty;
        
        public decimal OTSeqNo { get; set; }
        
        public DateTime WrkDate { get; set; }
        
        public DateTime DteTmeStr { get; set; }
        
        public DateTime DteTmeEnd { get; set; }
        
        public string EmpNo { get; set; } = string.Empty;
        
        public string BossId { get; set; } = string.Empty;
        
        public string SecCode { get; set; } = string.Empty;
        
        public string CostCenter { get; set; } = string.Empty;
        
        public string ShiftCode { get; set; } = string.Empty;
        
        public string ActionType { get; set; } = string.Empty;
        
        public string ActionReason { get; set; } = string.Empty;
        
        public string? Remark { get; set; }
        
        public string CaseId { get; set; } = string.Empty;
        
        public decimal TotalOTHrs { get; set; }
        
        public decimal OTBfrQty { get; set; }
        
        public decimal OTBfrRate { get; set; }
        
        public decimal OTNrmQty { get; set; }
        
        public decimal OTNrmRate { get; set; }
        
        public decimal OTAftQty { get; set; }
        
        public decimal OTAftRate { get; set; }
        
        public int Year { get; set; }
        
        public int Month { get; set; }
        
        public int Period { get; set; }
        
        public string ApprStatus { get; set; } = "Pending";
        
        // ฟิลด์เพิ่มเติมที่อาจมีในตาราง
        public string? SuspReqNo { get; set; }
        
        public string? TmpBossId { get; set; }
        
        public string? TmpSecCode { get; set; }
        
        public string? TmpCostCenter { get; set; }
        
        public DateTime? CreateDate { get; set; } = DateTime.Now;
        
        public string? CreateBy { get; set; }
        
        public string? FlagDel { get; set; } = "N";
        
        public DateTime? ApprDate { get; set; }
        
        public string? NextAppr { get; set; }
        
        public string? CEInd { get; set; }
        
        public string? Abnormalrun { get; set; }
    }
} 