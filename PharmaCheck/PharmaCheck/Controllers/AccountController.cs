using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaCheck.Data;
using PharmaCheck.Models;
using PharmaCheck.Models.ViewModels;
using PharmaCheck.Services; // 1. THÊM NAMESPACE DỊCH VỤ LOG
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
        private readonly IAuditLogService _logService; // 2. KHAI BÁO BIẾN DỊCH VỤ LOG

        // 3. INJECT IAUDITLOGSERVICE VÀO HÀM KHỞI TẠO
        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger, IAuditLogService logService)
        {
            _context = context;
            _logger = logger;
            _logService = logService;
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
                    
                    // 4. LOG HOẠT ĐỘNG: Đăng nhập thất bại (Tài khoản không tồn tại)
                    await _logService.LogAsync(
                        message: $"Cố gắng đăng nhập bất thành với tài khoản không tồn tại hoặc đã bị khóa: '{model.UsernameOrEmail}'",
                        actionType: "Login_Failed",
                        username: model.UsernameOrEmail ?? "Anonymous"
                    );

                    ModelState.AddModelError("", "Tên đăng nhập hoặc email không tồn tại.");
                    return View(model);
                }

                // Kiểm tra mật khẩu sử dụng BCrypt
                if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    _logger.LogWarning($"Đăng nhập không thành công: Mật khẩu sai cho tài khoản '{user.Username}'");
                    
                    // 5. LOG HOẠT ĐỘNG: Đăng nhập thất bại (Sai mật khẩu)
                    await _logService.LogAsync(
                        message: $"Người dùng '{user.Username}' đăng nhập không thành công (Sai mật khẩu).",
                        actionType: "Login_Failed",
                        username: user.Username
                    );

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
                
                // 6. LOG HOẠT ĐỘNG: Đăng nhập thành công
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

        /// <summary>
        /// Đăng xuất khỏi hệ thống
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            string username = User.Identity?.Name ?? "N/A";
            
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation($"Đăng xuất thành công: {username}");
            
            // 7. LOG HOẠT ĐỘNG: Đăng xuất
            await _logService.LogAsync(
                message: $"Người dùng '{username}' đã đăng xuất khỏi hệ thống.",
                actionType: "Logout",
                username: username
            );

            TempData["SuccessMessage"] = "Đã đăng xuất thành công.";
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Hiển thị trang Đăng ký
        /// </summary>
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
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
                
                // 8. LOG HOẠT ĐỘNG: Đăng ký thành công
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

        /// <summary>
        /// Hiển thị trang Quên mật khẩu
        /// </summary>
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
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
                var user = await Task.Run(() =>
                    _context.Users.FirstOrDefault(u => u.Email.ToLower() == model.Email.ToLower())
                );

                if (user == null)
                {
                    _logger.LogWarning($"Yêu cầu quên mật khẩu cho email không tồn tại: {model.Email}");
                    
                    // 9. LOG HOẠT ĐỘNG: Yêu cầu đặt lại mật khẩu với email không có trong hệ thống
                    await _logService.LogAsync(
                        message: $"Yêu cầu đặt lại mật khẩu thất bại do Email không tồn tại: '{model.Email}'",
                        actionType: "ForgotPassword_Failed",
                        username: "Anonymous"
                    );

                    TempData["InfoMessage"] = "Nếu email này tồn tại trong hệ thống, bạn sẽ nhận được email hướng dẫn đặt lại mật khẩu.";
                    return RedirectToAction("Login");
                }

                // Tạo token ngẫu nhiên để đặt lại mật khẩu (token có hiệu lực 24 giờ)
                var resetToken = GenerateResetToken();
                var tokenExpiry = DateTime.UtcNow.AddHours(24);

                HttpContext.Session.SetString($"ResetToken_{user.Id}", resetToken);
                HttpContext.Session.SetString($"ResetTokenExpiry_{user.Id}", tokenExpiry.ToString("o"));

                // Gửi email 
                await SendPasswordResetEmailAsync(user.Email, user.Username, resetToken, user.Id);

                _logger.LogInformation($"Yêu cầu đặt lại mật khẩu được gửi đến: {user.Email}");
                
                // 10. LOG HOẠT ĐỘNG: Yêu cầu đặt lại mật khẩu thành công
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
                
                // 11. LOG HOẠT ĐỘNG: Đổi mật khẩu thành công bằng token email
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

        private string GenerateResetToken()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var token = new string(Enumerable.Range(0, 32)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray());
            return token;
        }

        private async Task SendPasswordResetEmailAsync(string email, string username, string resetToken, int userId)
        {
            var resetLink = Url.Action("ResetPassword", "Account",
                new { token = resetToken, email = email },
                protocol: HttpContext.Request.Scheme);

            _logger.LogInformation($"[MÔ PHỎNG] Gửi email đặt lại mật khẩu");
            _logger.LogInformation($"  Đến: {email}");
            _logger.LogInformation($"  Tên: {username}");
            _logger.LogInformation($"  Link: {resetLink}");

            await Task.CompletedTask;
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