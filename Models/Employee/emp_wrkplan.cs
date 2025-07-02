namespace IPS_TH.Models.Employee
{
    public class emp_wrkplan
    {
        // Primary Key
        public int id { get; set; }

        // Properties จาก emp_wrkplan
        public string workplan_id { get; set; }
        public string dept { get; set; }
        public int year { get; set; }
        public int month { get; set; }
        public string period { get; set; }
        public DateTime? work_date { get; set; }
        public string day_type { get; set; }
        public string shift_code { get; set; }
        public int cycle_day { get; set; }
        public string record_status { get; set; }

        // Properties จาก emp_shift
        public int shift_id { get; set; }
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
