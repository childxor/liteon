using System.ComponentModel.DataAnnotations;

namespace IPS_TH.Models
{

    public class sys_language
    {
        [Key]
        public int Id { get; set; }
        public string Keyword { get; set; }
        public string ModuleId { get; set; }
        public string Th { get; set; }
        public string En { get; set; }
        public string Jp { get; set; }
        public string Cn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string RecordStatus { get; set; }
    }
   
}