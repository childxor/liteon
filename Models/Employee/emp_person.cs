public class emp_person
{
    public int Id { get; set; }
    public string? cardNumber { get; set; }
    public string? personID { get; set; }
    public string? name { get; set; }
    public string? deptID { get; set; }
    public string? deptName { get; set; }
    public DateTime leaveJobDate { get; set; }
    public DateTime enableDate { get; set; }
    public DateTime disableDate { get; set; }
    public string? userLevel { get; set; }
    public string? password { get; set; }
    public string? superPassword { get; set; }
    public DateTime modifyTime { get; set; }
    public string? reserve1 { get; set; }
    public string? reserve2 { get; set; }
    public string? reserve3 { get; set; }
    public string? reserve4 { get; set; }
    public string? reserveChar1 { get; set; }
    public string? cardType { get; set; }
    public string? cardTypeDesc { get; set; }
    public string? subSystem { get; set; }
    public string? useCategory { get; set; }
    public string? useStatus { get; set; }
    public string? status { get; set; }
    public string? cardCategory { get; set; }
    public string? cardStatus { get; set; }
    public DateTime? CREATEDATE { get; set; }
    public int isActive { get; set; } = 1;
    public string? shiftMent { get; set; }
    public string? RecordStatus { get; set; } = "N";
} 