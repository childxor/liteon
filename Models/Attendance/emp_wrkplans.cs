public class emp_wrkplans
{
    public int id { get; set; }
    public string workplan_id { get; set; }
    public string workplan_desc { get; set; }
    public int year { get; set; }
    public int month { get; set; }
    public int period { get; set; }
    public DateTime work_date { get; set; }
    public string day_type { get; set; }
    public string shift_code { get; set; }
    public int cycle_day { get; set; }
    public string record_status { get; set; }
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
    public string emp_no { get; set; }
    public string emp_name { get; set; }
    public string dep_code { get; set; }
    public string sec_code { get; set; }
    public DateTime? eff_date_from { get; set; }
    public DateTime? eff_date_to { get; set; }
}
