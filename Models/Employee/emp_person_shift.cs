public class emp_person_shift
{
    public int Id { get; set; }
    public string? name { get; set; }
    public string? shiftMent { get; set; }
    public string? personID { get; set; }
    public string? deptCode { get; set; }
    public string? deptName { get; set; }
    public string? deptID { get; set; }
    public DateTime? date { get; set; }
    public int isActive { get; set; } = 1;
    
    // เพิ่มฟิลด์ใหม่
    public string? remark { get; set; }
    public DateTime? createdAt { get; set; }
    public string? createdBy { get; set; }
    public DateTime? updatedAt { get; set; }
    public string? updatedBy { get; set; }
} 