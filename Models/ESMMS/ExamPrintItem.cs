namespace IPS_TH.Models.ESMMS
{
    public class ExamPrintItem
    {
        public int sort_order { get; set; }
        public string question_text { get; set; } = string.Empty;
        public string? choice_a { get; set; } //1
        public string? choice_b { get; set; } //2
        public string? choice_c { get; set; } //3
        public string? choice_d { get; set; } //4
        public string? choice_e { get; set; } //5
        public string? choice_f { get; set; } //6 
        public string? choice_g { get; set; } //7
        public string? choice_h { get; set; } //8
        public string? correct_answer { get; set; }
        public string? image_path { get; set; }
        public string? choice_a_image_path { get; set; }
        public string? choice_b_image_path { get; set; }
        public string? choice_c_image_path { get; set; }
        public string? choice_d_image_path { get; set; }
    }
}


