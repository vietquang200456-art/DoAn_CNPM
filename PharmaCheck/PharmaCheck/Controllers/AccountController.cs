using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaCheck.Data;
using PharmaCheck.Models;
using PharmaCheck.Models.ViewModels;
using PharmaCheck.Services; 
using System.Security.Claims;
using System.Security.Cryptography;

namespace PharmaCheck.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;
        private readonly IAuditLogService _logService; 
        private readonly IEmailService _emailService;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger, IAuditLogService logService, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _logService = logService;
            _emailService = emailService;
        }
        /// Hiển thị trang Đăng nhập
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
        /// Xử lý đăng nhập (POST)
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
                var user = await Task.Run(() =>
                    _context.Users.FirstOrDefault(u =>
                        (u.Username == model.UsernameOrEmail || u.Email == model.UsernameOrEmail) &&
                        u.IsActive)
                );

                if (user == null)
                {
                    _logger.LogWarning($"Đăng nhập không thành công: Tài khoản '{model.UsernameOrEmail}' không tồn tại");
                    
                    await _logService.LogAsync(
                        message: $"Cố gắng đăng nhập bất thành với tài khoản không tồn tại hoặc đã bị khóa: '{model.UsernameOrEmail}'",
                        actionType: "Login_Failed",
                        username: model.UsernameOrEmail ?? "Anonymous"
                    );

                    ModelState.AddModelError("", "Tên đăng nhập hoặc email không tồn tại.");
                    return View(model);
                }

                if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    _logger.LogWarning($"Đăng nhập không thành công: Mật khẩu sai cho tài khoản '{user.Username}'");
                    
                    await _logService.LogAsync(
                        message: $"Người dùng '{user.Username}' đăng nhập không thành công (Sai mật khẩu).",
                        actionType: "Login_Failed",
                        username: user.Username
                    );

                    ModelState.AddModelError("", "Mật khẩu không chính xác.");
                    return View(model);
                }

                user.LastLoginAt = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

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
                
                await _logService.LogAsync(
                    message: $"Người dùng '{user.Username}' (Quyền: {user.Role}) đã đăng nhập thành công vào hệ thống.",
                    actionType: "Login",
                    username: user.Username
                );

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
        // Hiển thị trang Hồ sơ cá nhân
        [Authorize]
        [HttpGet]
        public IActionResult Profile()
        {
            // Lấy UserId từ Claims của User đang đăng nhập
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return NotFound();

            // Đổ dữ liệu vào ViewModel
            var model = new ProfileViewModel
            {
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role
            };

            return View(model);
        }
        /// Đăng xuất khỏi hệ thống
       [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            string username = User.Identity?.Name ?? "N/A";
            
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation($"Đăng xuất thành công: {username}");
            
            await _logService.LogAsync(
                message: $"Người dùng '{username}' đã đăng xuất khỏi hệ thống.",
                actionType: "Logout",
                username: username
            );

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
                var existingUsername = await Task.Run(() =>
                    _context.Users.Any(u => u.Username.ToLower() == model.Username.ToLower())
                );
                if (existingUsername)
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác.");
                    return View(model);
                }
                
                var existingEmail = await Task.Run(() =>
                    _context.Users.Any(u => u.Email.ToLower() == model.Email.ToLower())
                );
                if (existingEmail)
                {
                    ModelState.AddModelError("Email", "Email này đã được đăng ký. Vui lòng sử dụng email khác.");
                    return View(model);
                }
                
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

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
                
                await _logService.LogAsync(
                    message: $"Tài khoản mới được tạo thành công: '{model.Username}' (Email: {model.Email}, Họ tên: {model.FullName}).",
                    actionType: "Register",
                    username: model.Username
                );

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
        // Cập nhật thông tin cá nhân (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateProfile(ProfileViewModel model)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId)) return RedirectToAction("Login", "Account");

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return NotFound();

            // Chỉ validate các trường thông tin cơ bản, bỏ qua phần mật khẩu ở action này
            ModelState.Remove(nameof(model.CurrentPassword));
            ModelState.Remove(nameof(model.NewPassword));
            ModelState.Remove(nameof(model.ConfirmPassword));

            if (ModelState.IsValid)
            {
                // Kiểm tra trùng lặp email với user khác
                var emailExists = _context.Users.Any(u => u.Email == model.Email && u.Id != userId);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác.");
                    // Giữ lại dữ liệu cũ để hiển thị lại
                    model.Username = user.Username;
                    model.Role = user.Role;
                    return View("Profile", model);
                }

                user.FullName = model.FullName;
                user.Email = model.Email;
                user.UpdatedAt = DateTime.UtcNow;

                _context.SaveChanges();
                TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";
                return RedirectToAction("Profile");
            }

            model.Username = user.Username;
            model.Role = user.Role;
            return View("Profile", model);
        }
        // Đổi mật khẩu
            // POST: /Account/ChangePassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ProfileViewModel model)
    {
        // Vì đây là form đổi mật khẩu, chúng ta không quan tâm/không bắt buộc validate thông tin cá nhân
        ModelState.Remove(nameof(model.FullName));
        ModelState.Remove(nameof(model.Email));

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId)) return RedirectToAction("Login", "Account");

        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null) return NotFound();

        // Yêu cầu bắt buộc nhập các trường mật khẩu ở action này
        if (string.IsNullOrEmpty(model.CurrentPassword))
            ModelState.AddModelError("CurrentPassword", "Vui lòng nhập mật khẩu hiện tại");
        if (string.IsNullOrEmpty(model.NewPassword))
            ModelState.AddModelError("NewPassword", "Vui lòng nhập mật khẩu mới");

        if (ModelState.IsValid)
        {
            // Kiểm tra mật khẩu cũ bằng BCrypt
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash);

            if (!isPasswordValid)
            {
                _logger.LogWarning($"Đổi mật khẩu thất bại: Mật khẩu cũ nhập sai cho tài khoản '{user.Username}'");
                
                await _logService.LogAsync(
                    message: $"Người dùng '{user.Username}' thay đổi mật khẩu không thành công (Sai mật khẩu hiện tại).",
                    actionType: "ChangePassword_Failed",
                    username: user.Username
                );

                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không chính xác.");
                
                // Khôi phục lại thông tin cơ bản từ DB để hiển thị ra View
                model.Username = user.Username;
                model.Email = user.Email;
                model.FullName = user.FullName;
                model.Role = user.Role;
                 TempData["SuccessMessage"] = "Đổi mật khẩu không thành công!";
                return View("Profile", model);
            }

            // Băm mật khẩu mới bằng BCrypt trước khi cập nhật xuống DB
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword); 
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Đổi mật khẩu thành công cho tài khoản: {user.Username}");

            await _logService.LogAsync(
                message: $"Người dùng '{user.Username}' đã chủ động thay đổi mật khẩu tài khoản thành công.",
                actionType: "ChangePassword_Success",
                username: user.Username
            );

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Profile");
        }

        // Nếu có lỗi nhập liệu (ví dụ mật khẩu mới < 6 ký tự hoặc không khớp xác nhận)
        // Cần điền lại dữ liệu gốc từ DB để hiển thị giao diện bên trái/bên trên cho đẹp, không bị trống
        model.Username = user.Username;
        model.Email = user.Email;
        model.FullName = user.FullName;
        model.Role = user.Role;
        return View("Profile", model);
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
                    _logger.LogWarning($"Yêu cầu quên mật khẩu cho email không tồn tại: {model.Email}");
                    
                    await _logService.LogAsync(
                        message: $"Yêu cầu đặt lại mật khẩu thất bại do Email không tồn tại: '{model.Email}'",
                        actionType: "ForgotPassword_Failed",
                        username: "Anonymous"
                    );

                    // Sử dụng TempData đồng nhất để hiển thị ra cho client như code cũ của bạn
                    TempData["InfoMessage"] = "Nếu email này tồn tại trong hệ thống, bạn sẽ nhận được email hướng dẫn đặt lại mật khẩu.";
                    return RedirectToAction("Login");
                }

                // Sửa đổi phương thức sinh Token bằng Cryptography đảm bảo an toàn bảo mật cao hơn Random thông thường
                var resetToken = GenerateResetToken();
                var tokenExpiry = DateTime.UtcNow.AddMinutes(15); // Đổi thành 15 - 30 phút để tăng tính bảo mật hơn 24 giờ

                HttpContext.Session.SetString($"ResetToken_{user.Id}", resetToken);
                HttpContext.Session.SetString($"ResetTokenExpiry_{user.Id}", tokenExpiry.ToString("o"));

                // 3. THỰC HIỆN GỬI EMAIL THỰC TẾ QUA SERVICE
                await SendPasswordResetEmailAsync(user.Email, user.FullName ?? user.Username, resetToken, user.Id);

                _logger.LogInformation($"Yêu cầu đặt lại mật khẩu được gửi đến: {user.Email}");
                
                await _logService.LogAsync(
                    message: $"Người dùng '{user.Username}' đã yêu cầu một liên kết đặt lại mật khẩu qua email '{user.Email}'.",
                    actionType: "ForgotPassword_Request",
                    username: user.Username
                );

                TempData["InfoMessage"] = "Nếu email này tồn tại trong hệ thống, bạn sẽ nhận được email hướng dẫn đặt lại mật khẩu.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý quên mật khẩu");
                ModelState.AddModelError("", $"Lỗi hệ thống không thể gửi mail: {ex.Message}");
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

                var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                user.PasswordHash = newPasswordHash;
                user.UpdatedAt = DateTime.UtcNow;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                HttpContext.Session.Remove($"ResetToken_{user.Id}");
                HttpContext.Session.Remove($"ResetTokenExpiry_{user.Id}");

                _logger.LogInformation($"Đặt lại mật khẩu thành công cho tài khoản: {user.Username}");
                
                await _logService.LogAsync(
                    message: $"Người dùng '{user.Username}' đã đặt lại mật khẩu mới thành công thông qua liên kết xác thực email.",
                    actionType: "ResetPassword_Success",
                    username: user.Username
                );

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

        // ==================== HELPER METHODS ====================

        // Sử dụng mã hóa Cryptography mạnh mẽ thay thế cho Random cũ của bạn
        private string GenerateResetToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        }
// 4. HIỆN THỰC GỬI EMAIL THỰC TẾ
        private async Task SendPasswordResetEmailAsync(string email, string displayName, string resetToken, int userId)
        {
            var resetLink = Url.Action("ResetPassword", "Account",
                new { token = resetToken, email = email },
                protocol: HttpContext.Request.Scheme);

            // Giao diện Email bằng HTML bắt mắt và có nút bấm rõ ràng
            string emailBody = $@"
                <div style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 500px; margin: 0 auto; padding: 25px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff;'>
                    <div style='text-align: center; margin-bottom: 20px;'>
                        <h2 style='color: #dc2626; margin: 0; font-size: 24px;'>PharmaCheck Security</h2>
                    </div>
                    <hr style='border: none; border-top: 1px solid #f1f5f9; margin-bottom: 20px;' />
                    <p style='font-size: 16px; color: #1e293b;'>Xin chào <strong>{displayName}</strong>,</p>
                    <p style='font-size: 15px; color: #334155; line-height: 1.6;'>Bạn nhận được email này vì hệ thống nhận được yêu cầu lấy lại mật khẩu cho tài khoản của bạn. Vui lòng bấm vào nút bấm bên dưới để khởi tạo mật khẩu mới:</p>
                    
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{resetLink}' style='background-color: #dc2626; color: #ffffff; padding: 12px 30px; text-decoration: none; font-weight: 600; border-radius: 6px; display: inline-block; font-size: 15px; box-shadow: 0 4px 6px -1px rgba(220, 38, 38, 0.2);'>
                            Đặt lại mật khẩu mới
                        </a>
                    </div>
                    
                    <p style='font-size: 13px; color: #ef4444; background-color: #fef2f2; padding: 10px; border-radius: 6px; border-left: 4px solid #ef4444;'>
                        <strong>* Lưu ý:</strong> Đường dẫn này chỉ có hiệu lực sử dụng trong vòng <strong>15 phút</strong>.
                    </p>
                    <hr style='border: none; border-top: 1px solid #f1f5f9; margin: 25px 0 15px 0;' />
                    <p style='font-size: 12px; color: #94a3b8; text-align: center; margin: 0;'>Nếu bạn không yêu cầu hành động này, bạn hoàn toàn có thể bỏ qua email một cách an toàn.</p>
                </div>";

            // Gọi hàm từ EmailService đã cấu hình từ trước để gửi đi thực tế
            await _emailService.SendEmailAsync(email, "Yêu cầu khôi phục mật khẩu hệ thống PharmaCheck", emailBody);
        }

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
}