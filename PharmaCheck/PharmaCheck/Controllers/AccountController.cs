using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using PharmaCheck.Data;
using PharmaCheck.Models;

namespace PharmaCheck.Controllers
{
    /// <summary>
    /// Controller xử lý các chức năng xác thực tài khoản (Đăng nhập, Đăng ký, Quên mật khẩu)
    /// </summary>
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ========== ĐĂNG NHẬP ==========

        /// <summary>
        /// Hiển thị trang Đăng nhập
        /// </summary>
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// Xử lý đăng nhập (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Tìm user theo Username hoặc Email
                var user = await Task.Run(() =>
                    _context.Users.FirstOrDefault(u =>
                        (u.Username == model.UsernameOrEmail || u.Email == model.UsernameOrEmail) &&
                        u.IsActive)
                );

                if (user == null)
                {
                    ModelState.AddModelError("", "Tên đăng nhập hoặc email không tồn tại.");
                    return View(model);
                }

                // Kiểm tra mật khẩu (Dummy check - trong production sử dụng BCrypt/hashing)
                // TODO: Thay thế bằng proper password hashing (BCrypt, Argon2)
                if (!VerifyPassword(model.Password, user.PasswordHash))
                {
                    ModelState.AddModelError("", "Mật khẩu không chính xác.");
                    return View(model);
                }

                // Cập nhật LastLoginAt
                user.LastLoginAt = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // Thiết lập session/cookie (TODO: Sử dụng ASP.NET Core Identity hoặc JWT)
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Email", user.Email);
                HttpContext.Session.SetString("Role", user.Role);

                TempData["SuccessMessage"] = $"Xin chào {user.FullName}! Đăng nhập thành công.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi hệ thống: {ex.Message}");
                return View(model);
            }
        }

        // ========== ĐĂNG KÝ ==========

        /// <summary>
        /// Hiển thị trang Đăng ký
        /// </summary>
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// Xử lý đăng ký (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Kiểm tra Password và ConfirmPassword
                if (model.Password != model.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "Mật khẩu xác nhận không khớp.");
                    return View(model);
                }

                // Kiểm tra độ mạnh mật khẩu (ít nhất 8 ký tự, có chữ hoa, chữ thường, số)
                if (!IsPasswordStrong(model.Password))
                {
                    ModelState.AddModelError("Password", "Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số.");
                    return View(model);
                }

                // Kiểm tra Username đã tồn tại
                var existingUsername = await Task.Run(() =>
                    _context.Users.Any(u => u.Username == model.Username)
                );
                if (existingUsername)
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác.");
                    return View(model);
                }

                // Kiểm tra Email đã tồn tại
                var existingEmail = await Task.Run(() =>
                    _context.Users.Any(u => u.Email == model.Email)
                );
                if (existingEmail)
                {
                    ModelState.AddModelError("Email", "Email này đã được đăng ký. Vui lòng sử dụng email khác.");
                    return View(model);
                }

                // Tạo user mới
                var newUser = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    FullName = model.FullName,
                    PasswordHash = HashPassword(model.Password),
                    Role = "User",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập để tiếp tục.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi hệ thống: {ex.Message}");
                return View(model);
            }
        }

        // ========== QUÊN MẬT KHẨU ==========

        /// <summary>
        /// Hiển thị trang Quên mật khẩu
        /// </summary>
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        /// <summary>
        /// Xử lý yêu cầu quên mật khẩu (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Tìm user theo email
                var user = await Task.Run(() =>
                    _context.Users.FirstOrDefault(u => u.Email == model.Email)
                );

                if (user == null)
                {
                    // Vì lý do bảo mật, không tiết lộ email có tồn tại hay không
                    TempData["InfoMessage"] = "Nếu email này tồn tại trong hệ thống, bạn sẽ nhận được email hướng dẫn đặt lại mật khẩu.";
                    return RedirectToAction("ForgotPassword");
                }

                // TODO: Tạo reset token và gửi email
                // 1. Tạo token (có thể lưu vào database với thời gian hết hạn)
                // 2. Tạo reset link: /Account/ResetPassword?token=...
                // 3. Gửi email với link reset
                // 4. Sử dụng SendGrid hoặc SMTP service

                // Giả lập: Cập nhật UpdatedAt để đánh dấu yêu cầu reset
                user.UpdatedAt = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                TempData["InfoMessage"] = "Nếu email này tồn tại trong hệ thống, bạn sẽ nhận được email hướng dẫn đặt lại mật khẩu. Vui lòng kiểm tra hộp thư đến (hoặc thư rác).";
                return RedirectToAction("ForgotPassword");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi hệ thống: {ex.Message}");
                return View(model);
            }
        }

        // ========== ĐĂNG XUẤT ==========

        /// <summary>
        /// Xử lý đăng xuất
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Đăng xuất thành công.";
            return RedirectToAction("Index", "Home");
        }

        // ========== HELPER METHODS ==========

        /// <summary>
        /// Kiểm tra tính hợp lệ của mật khẩu (dummy check, trong production dùng hashing)
        /// </summary>
        private bool VerifyPassword(string password, string hash)
        {
            // TODO: Sử dụng BCrypt.Net-Next hoặc System.Security.Cryptography
            // Ví dụ: return BCrypt.Net.BCrypt.Verify(password, hash);
            
            // Dummy implementation (KHÔNG DÙNG TRONG PRODUCTION)
            return HashPassword(password) == hash;
        }

        /// <summary>
        /// Hash mật khẩu (dummy implementation)
        /// </summary>
        private string HashPassword(string password)
        {
            // TODO: Sử dụng BCrypt hoặc PBKDF2
            // Ví dụ: return BCrypt.Net.BCrypt.HashPassword(password);
            
            // Dummy implementation (KHÔNG DÙNG TRONG PRODUCTION - chỉ dùng để demo)
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        /// <summary>
        /// Kiểm tra mật khẩu có đủ mạnh (ít nhất 8 ký tự, có chữ hoa, chữ thường, số)
        /// </summary>
        private bool IsPasswordStrong(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return false;

            bool hasUpperCase = Regex.IsMatch(password, "[A-Z]");
            bool hasLowerCase = Regex.IsMatch(password, "[a-z]");
            bool hasNumber = Regex.IsMatch(password, "[0-9]");
            bool hasSpecialChar = Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\|,.<>\/?]");

            // Yêu cầu: Ít nhất chữ hoa + chữ thường + số
            return hasUpperCase && hasLowerCase && hasNumber;
        }

        /// <summary>
        /// Kiểm tra email có hợp lệ
        /// </summary>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }

    // ========== VIEW MODELS ==========

    /// <summary>
    /// ViewModel cho trang Đăng nhập
    /// </summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập hoặc email")]
        [Display(Name = "Tên đăng nhập hoặc Email")]
        public string UsernameOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }

    /// <summary>
    /// ViewModel cho trang Đăng ký
    /// </summary>
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [StringLength(50, MinimumLength = 4, ErrorMessage = "Tên đăng nhập phải từ 4 đến 50 ký tự")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Họ tên phải từ 5 đến 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chấp nhận Điều khoản sử dụng")]
        [Display(Name = "Tôi đồng ý với Điều khoản sử dụng và Chính sách bảo mật")]
        public bool AgreeToTerms { get; set; }
    }

    /// <summary>
    /// ViewModel cho trang Quên mật khẩu
    /// </summary>
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email của bạn")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }
}
