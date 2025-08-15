namespace IPS_TH.Models.Attendance
{
    public class emp_shift
    {
        public int Id { get; set; } // เปลี่ยนจาก shift_id เป็น Id
        public string shift_code { get; set; }
        public string shift_name { get; set; }
        public string shift_group { get; set; }
        public string start_time { get; set; }
        public string end_time { get; set; }
        public string break_start_time { get; set; }
        public string break_end_time { get; set; }
        public bool is_break2 { get; set; }
        public string break2_start_time { get; set; }
        public string break2_end_time { get; set; }
        public string ot_start_time { get; set; }
        public string ot_end_time { get; set; }
        public int sort { get; set; }
    }
}
