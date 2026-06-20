using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCheck.Data;
using PharmaCheck.Models;

namespace PharmaCheck.Controllers;

[Authorize(Roles = "Admin")]
public class AdminDashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminDashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // 1. DATA THỐNG KÊ (Mock + Real Count)
        ViewBag.TotalDrugs = 1248;
        ViewBag.ActiveDrugs = 1120;
        ViewBag.ExpiredWarningDrugs = 14;
        ViewBag.TotalUsers = await _context.Users.CountAsync(); 

        // 2. DATA THẬT: Tải nhật ký hệ thống (Audit Logs)
        var rawLogs = await _context.AuditLogs
            .OrderByDescending(al => al.CreatedAt)
            .Take(5)
            .ToListAsync();

        ViewBag.SystemLogs = rawLogs.Select(log => new {
            Message = log.Message,
            Time = FormatAuditLogTime(log.CreatedAt),
            Color = log.ActionType?.ToLower() switch {
                "create" => "text-green-600 bg-green-50",
                "edit"   => "text-blue-600 bg-blue-50",
                "delete" => "text-red-600 bg-red-50",
                "login"  => "text-indigo-600 bg-indigo-50",
                _        => "text-slate-600 bg-slate-50"
            },
            Icon = log.ActionType?.ToLower() switch {
                "create" => "fa-plus-circle",
                "edit"   => "fa-edit",
                "delete" => "fa-trash-alt",
                "login"  => "fa-sign-in-alt",
                _        => "fa-info-circle"
            }
        }).ToList();

        // 3. MOCK DATA: Biểu đồ xu hướng
        ViewBag.ChartLabels = new string[] { "Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4", "Tháng 5", "Tháng 6" };
        ViewBag.ChartData = new int[] { 85, 120, 90, 145, 130, 185 };

        // 4. DATA THẬT: Top thuốc mới cập nhật
        var dbRecentDrugs = await _context.Drugs
            .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
            .Take(5)
            .ToListAsync();

        ViewBag.RecentDrugs = dbRecentDrugs.Select(d => new {
            Id = d.Id,
            Name = d.Name,
            Manufacturer = d.Manufacturer ?? "Chưa rõ",
            Status = d.IsActive ? "Active" : "Inactive"
        }).ToList();

        // 5. DATA THẬT: Danh sách tài khoản nhân sự lâm sàng (KHÔNG HIỂN THỊ ADMIN) 🌟
        var dbUsers = await _context.Users
            .Include(u => u.Role) 
            .Where(u => u.Role == null || u.Role.Name != "Admin") // Loại bỏ tuyệt đối Admin ngay từ truy vấn gốc
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .ToListAsync();

        ViewBag.UserList = dbUsers.Select(u => new {
            Id = u.Id,
            Name = u.FullName ?? "Người dùng mới",
            Email = u.Email,
            Role = u.Role?.Name ?? "Doctor", // Gán mặc định nếu chưa map Role
            AvatarInitials = string.IsNullOrEmpty(u.FullName) 
                ? "NN" 
                : string.Concat(u.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(n => n[0])).ToUpper(),
            IsActive = u.IsActive
        }).ToList();

        return View();
    }

    /// <summary>
    /// API Thay đổi phân quyền thành viên lâm sàng (Doctor / Pharmacist)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeUserRole(int userId, string newRole)
    {
        // Kiểm tra chặn: Hệ thống hiện tại chỉ có 2 quyền lâm sàng được phép chỉ định
        var allowedRoles = new List<string> { "Doctor", "Pharmacist" };
        if (!allowedRoles.Contains(newRole))
        {
            return Json(new { success = false, message = "Vai trò phân quyền không hợp lệ hoặc bị cấm." });
        }

        try
        {
            // Tải thông tin người dùng kèm theo quyền hiện tại
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) 
                return Json(new { success = false, message = "Tài khoản nhân sự không tồn tại." });

            // Bảo vệ tầng sâu: Ngăn chặn can thiệp hạ quyền nếu tài khoản đích thực chất là Admin
            if (user.Role?.Name == "Admin")
            {
                return Json(new { success = false, message = "Không thể sửa đổi thông tin của Quản trị viên cấp cao." });
            }

            // Tìm thực thể Role tương ứng trong cơ sở dữ liệu
            var targetRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == newRole);
            if (targetRole == null)
            {
                return Json(new { success = false, message = $"Vai trò '{newRole}' chưa được khởi tạo trong Database." });
            }

            string oldRoleName = user.Role?.Name ?? "Chưa phân quyền";
            
            // Tiến hành cập nhật mối quan hệ khóa ngoại
            user.Role = targetRole; 
            _context.Users.Update(user);

            // Ghi nhận hành động vào nhật ký hệ thống (Audit Log)
            _context.AuditLogs.Add(new AuditLog {
                Message = $"Admin đã đổi quyền của {user.FullName ?? user.Email} từ [{oldRoleName}] sang [{newRole}].",
                ActionType = "Edit",
                CreatedAt = DateTime.UtcNow,
                PerformedBy = User.Identity?.Name ?? "Admin"
            });

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi xử lý Database: " + ex.Message });
        }
    }

    /// <summary>
    /// API Khóa / Mở khóa tài khoản nhân sự
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(int userId, bool activate)
    {
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Json(new { success = false, message = "Người dùng không tồn tại" });

        // Chặn không cho khóa tài khoản Admin từ giao diện này
        if (user.Role?.Name == "Admin")
        {
            return Json(new { success = false, message = "Không thể thay đổi trạng thái của tài khoản Quản trị viên." });
        }

        try 
        {
            user.IsActive = activate;
            _context.Users.Update(user);

            var action = activate ? "Mở khóa" : "Khóa";
            _context.AuditLogs.Add(new AuditLog {
                Message = $"Admin đã {action} tài khoản: {user.FullName ?? user.Email}",
                ActionType = "Edit",
                CreatedAt = DateTime.UtcNow,
                PerformedBy = User.Identity?.Name ?? "Admin"
            });
            
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Logs(string search, int page = 1)
    {
        int pageSize = 20;
        var query = _context.AuditLogs.AsNoTracking();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(l => l.Message.Contains(search) || l.ActionType.Contains(search));
        }

        int totalLogs = await query.CountAsync();
        var rawLogs = await query.OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.AllLogs = rawLogs.Select(log => new {
            Id = log.Id,
            Message = log.Message,
            Time = log.CreatedAt.AddHours(7).ToString("dd/MM/yyyy HH:mm:ss"),
            Action = log.ActionType,
            Color = log.ActionType?.ToLower() switch {
                "create" => "text-green-600 bg-green-50 border-green-200",
                "edit"   => "text-blue-600 bg-blue-50 border-blue-200",
                "delete" => "text-red-600 bg-red-50 border-red-200",
                "login"  => "text-indigo-600 bg-indigo-50 border-indigo-200",
                _        => "text-slate-600 bg-slate-50 border-slate-200"
            },
            Icon = log.ActionType?.ToLower() switch {
                "create" => "fa-plus-circle",
                "edit"   => "fa-edit",
                "delete" => "fa-trash-alt",
                "login"  => "fa-sign-in-alt",
                _        => "fa-info-circle"
            }
        }).ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalLogs / pageSize);
        ViewBag.SearchKeyword = search;

        return View();
    }   

    private string FormatAuditLogTime(DateTime utcTime)
    {
        var timeSpan = DateTime.UtcNow - utcTime;
        if (timeSpan.TotalSeconds < 60) return "Vừa xong";
        if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} phút trước";
        if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} giờ trước";
        return utcTime.AddHours(7).ToString("dd/MM/yyyy HH:mm");
    }
}