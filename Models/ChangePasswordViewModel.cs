using System.ComponentModel.DataAnnotations;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "กรุณากรอกรหัสผ่านปัจจุบัน")]
    [Display(Name = "รหัสผ่านปัจจุบัน")]
    public string CurrentPassword { get; set; }

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่านใหม่")]
    [StringLength(100, ErrorMessage = "รหัสผ่านต้องมีความยาวอย่างน้อย {2} ตัวอักษร", MinimumLength = 6)]
    [Display(Name = "รหัสผ่านใหม่")]
    public string NewPassword { get; set; }

    [Compare("NewPassword", ErrorMessage = "รหัสผ่านไม่ตรงกัน")]
    [Display(Name = "ยืนยันรหัสผ่านใหม่")]
    public string ConfirmPassword { get; set; }
} 