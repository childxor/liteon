namespace IPS_TH.Models.ESMMS
{
    public class ExamPrintItem
    {
        public int sort_order { get; set; }
        public string question_text { get; set; } = string.Empty;
        public string? choice_a { get; set; }
        public string? choice_b { get; set; }
        public string? choice_c { get; set; }
        public string? choice_d { get; set; }
        public string? correct_choice { get; set; }
        public string? image_path { get; set; }
    }
}


