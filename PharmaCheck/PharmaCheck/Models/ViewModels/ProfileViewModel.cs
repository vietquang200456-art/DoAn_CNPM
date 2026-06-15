using System.ComponentModel.DataAnnotations;

namespace PharmaCheck.Models
{
    public class ProfileViewModel
    {
        // --- Phần thông tin cá nhân ---
        [Display(Name = "Tên đăng nhập")]
        public string?Username { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string? FullName { get; set; }

        [Display(Name = "Vai trò hệ thống")]
        public string? Role { get; set; }

        // --- Phần đổi mật khẩu (Ẩn/Hiện tùy nhu cầu user) ---
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string? CurrentPassword { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải từ 6 ký tự trở lên")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string? ConfirmPassword { get; set; }
    }
}