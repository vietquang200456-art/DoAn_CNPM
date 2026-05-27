using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaCheck.Data;
using PharmaCheck.Models;
using PharmaCheck.Models.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace PharmaCheck.Controllers
{
    /// <summary>
    /// Controller xử lý các chức năng xác thực tài khoản (Đăng nhập, Đăng ký, Quên mật khẩu)
    /// </summary>
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================== ĐĂNG NHẬP ====================

        /// <summary>
        /// Hiển thị trang Đăng nhập
        /// </summary>
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// Xử lý đăng nhập (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
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
                    _logger.LogWarning($"Đăng nhập không thành công: Tài khoản '{model.UsernameOrEmail}' không tồn tại");
                    ModelState.AddModelError("", "Tên đăng nhập hoặc email không tồn tại.");
                    return View(model);
                }

                // Kiểm tra mật khẩu sử dụng BCrypt
                if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    _logger.LogWarning($"Đăng nhập không thành công: Mật khẩu sai cho tài khoản '{user.Username}'");
                    ModelState.AddModelError("", "Mật khẩu không chính xác.");
                    return View(model);
                }

                // Cập nhật LastLoginAt
                user.LastLoginAt = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // Tạo Claims cho Cookie Authentication
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("FullName", user.FullName),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // Đăng nhập qua Cookie Authentication
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(1)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    authProperties);

                _logger.LogInformation($"Đăng nhập thành công cho tài khoản: {user.Username}");
                TempData["SuccessMessage"] = $"Xin chào {user.FullName}! Đăng nhập thành công.";

                return RedirectToLocal(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng nhập");
                ModelState.AddModelError("", $"Lỗi hệ thống: {ex.Message}");
                return View(model);
            }
        }
        /// Đăng xuất khỏi hệ thống
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation($"Đăng xuất thành công: {username}");
            TempData["SuccessMessage"] = "Đã đăng xuất thành công.";
            return RedirectToAction("Index", "Home");
        }
        /// Hiển thị trang Đăng ký
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }
        /// Xử lý đăng ký (POST)
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
                // Kiểm tra Username đã tồn tại
                var existingUsername = await Task.Run(() =>
                    _context.Users.Any(u => u.Username.ToLower() == model.Username.ToLower())
                );
                if (existingUsername)
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác.");
                    return View(model);
                }
                // Kiểm tra Email đã tồn tại
                var existingEmail = await Task.Run(() =>
                    _context.Users.Any(u => u.Email.ToLower() == model.Email.ToLower())
                );
                if (existingEmail)
                {
                    ModelState.AddModelError("Email", "Email này đã được đăng ký. Vui lòng sử dụng email khác.");
                    return View(model);
                }
                // Băm mật khẩu sử dụng BCrypt
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

                // Tạo user mới
                var newUser = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    FullName = model.FullName,
                    PasswordHash = passwordHash,
                    Role = "User",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đăng ký tài khoản mới thành công: {model.Username}");
                TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Bây giờ hãy đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng ký tài khoản");
                ModelState.AddModelError("", $"Lỗi hệ thống: {ex.Message}");
                return View(model);
            }
        }
        /// Hiển thị trang Quên mật khẩu
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }
        /// Xử lý yêu cầu quên mật khẩu (POST)
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
                var user = await Task.Run(() =>
                    _context.Users.FirstOrDefault(u => u.Email.ToLower() == model.Email.ToLower())
                );

                if (user == null)
                {
                    // Không tiết lộ email có tồn tại hay không (bảo mật)
                    _logger.LogWarning($"Yêu cầu quên mật khẩu cho email không tồn tại: {model.Email}");
                    TempData["InfoMessage"] = "Nếu email này tồn tại trong hệ thống, bạn sẽ nhận được email hướng dẫn đặt lại mật khẩu.";
                    return RedirectToAction("Login");
                }

                // Tạo token ngẫu nhiên để đặt lại mật khẩu (token có hiệu lực 24 giờ)
                var resetToken = GenerateResetToken();
                var tokenExpiry = DateTime.UtcNow.AddHours(24);

                // Lưu token tạm thời trong session hoặc database
                // Trong thực tế, bạn nên lưu vào database với một bảng riêng
                HttpContext.Session.SetString($"ResetToken_{user.Id}", resetToken);
                HttpContext.Session.SetString($"ResetTokenExpiry_{user.Id}", tokenExpiry.ToString("o"));

                // Mô phỏng gửi email (TODO: Cấu hình SMTP email)
                await SendPasswordResetEmailAsync(user.Email, user.Username, resetToken, user.Id);

                _logger.LogInformation($"Yêu cầu đặt lại mật khẩu được gửi đến: {user.Email}");
                TempData["InfoMessage"] = "Nếu email này tồn tại trong hệ thống, bạn sẽ nhận được email hướng dẫn đặt lại mật khẩu.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý quên mật khẩu");
                ModelState.AddModelError("", $"Lỗi hệ thống: {ex.Message}");
                return View(model);
            }
        }

        /// <summary>
        /// Hiển thị trang Đặt lại mật khẩu
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string? token, string? email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Token hoặc email không hợp lệ.");
                return RedirectToAction("Login");
            }

            var user = await Task.Run(() =>
                _context.Users.FirstOrDefault(u => u.Email.ToLower() == email.ToLower())
            );

            if (user == null)
            {
                ModelState.AddModelError("", "Tài khoản không tồn tại.");
                return RedirectToAction("Login");
            }

            // Kiểm tra token hết hạn
            var storedToken = HttpContext.Session.GetString($"ResetToken_{user.Id}");
            var storedExpiry = HttpContext.Session.GetString($"ResetTokenExpiry_{user.Id}");

            if (storedToken == null || storedExpiry == null || storedToken != token)
            {
                ModelState.AddModelError("", "Token không hợp lệ hoặc đã hết hạn.");
                return RedirectToAction("Login");
            }

            if (!DateTime.TryParse(storedExpiry, out var expiryDateTime) || DateTime.UtcNow > expiryDateTime)
            {
                ModelState.AddModelError("", "Token đã hết hạn. Vui lòng yêu cầu lại.");
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        /// <summary>
        /// Xử lý đặt lại mật khẩu (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await Task.Run(() =>
                    _context.Users.FirstOrDefault(u => u.Email.ToLower() == model.Email.ToLower())
                );

                if (user == null)
                {
                    ModelState.AddModelError("", "Tài khoản không tồn tại.");
                    return View(model);
                }

                // Kiểm tra token
                var storedToken = HttpContext.Session.GetString($"ResetToken_{user.Id}");
                var storedExpiry = HttpContext.Session.GetString($"ResetTokenExpiry_{user.Id}");

                if (storedToken == null || storedExpiry == null || storedToken != model.Token)
                {
                    ModelState.AddModelError("", "Token không hợp lệ.");
                    return View(model);
                }

                if (!DateTime.TryParse(storedExpiry, out var expiryDateTime) || DateTime.UtcNow > expiryDateTime)
                {
                    ModelState.AddModelError("", "Token đã hết hạn. Vui lòng yêu cầu lại.");
                    return View(model);
                }

                // Cập nhật mật khẩu (hash mới)
                var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                user.PasswordHash = newPasswordHash;
                user.UpdatedAt = DateTime.UtcNow;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // Xóa token sau khi sử dụng
                HttpContext.Session.Remove($"ResetToken_{user.Id}");
                HttpContext.Session.Remove($"ResetTokenExpiry_{user.Id}");

                _logger.LogInformation($"Đặt lại mật khẩu thành công cho tài khoản: {user.Username}");
                TempData["SuccessMessage"] = "Mật khẩu đã được đặt lại thành công. Bây giờ bạn có thể đăng nhập với mật khẩu mới.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đặt lại mật khẩu");
                ModelState.AddModelError("", $"Lỗi hệ thống: {ex.Message}");
                return View(model);
            }
        }

        // ==================== HỖ TRỢ ROUTES ====================

        /// <summary>
        /// Trang từ chối truy cập
        /// </summary>
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ==================== HELPER METHODS ====================

        /// <summary>
        /// Tạo token ngẫu nhiên để đặt lại mật khẩu
        /// </summary>
        private string GenerateResetToken()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var token = new string(Enumerable.Range(0, 32)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray());
            return token;
        }

        /// <summary>
        /// Mô phỏng gửi email đặt lại mật khẩu (TODO: Cấu hình SMTP)
        /// </summary>
        private async Task SendPasswordResetEmailAsync(string email, string username, string resetToken, int userId)
        {
            // Hiện tại mô phỏng in log
            var resetLink = Url.Action("ResetPassword", "Account",
                new { token = resetToken, email = email },
                protocol: HttpContext.Request.Scheme);

            _logger.LogInformation($"[MÔ PHỎNG] Gửi email đặt lại mật khẩu");
            _logger.LogInformation($"  Đến: {email}");
            _logger.LogInformation($"  Tên: {username}");
            _logger.LogInformation($"  Link: {resetLink}");
            _logger.LogInformation($"  Token: {resetToken}");
            _logger.LogInformation($"  Hạn: 24 giờ từ bây giờ");

            await Task.CompletedTask;
        }
        /// Chuyển hướng an toàn tới URL được yêu cầu hoặc trang chủ
        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
    /// ViewModel cho trang Đăng nhập
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
    /// ViewModel cho trang Đăng ký
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
    /// ViewModel cho trang Quên mật khẩu
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email của bạn")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }
}
