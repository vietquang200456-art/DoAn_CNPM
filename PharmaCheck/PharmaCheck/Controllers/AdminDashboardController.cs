using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Đảm bảo có dòng này để dùng .Include()
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
        // 1. MOCK DATA: Thống kê
        ViewBag.TotalDrugs = 1248;
        ViewBag.ActiveDrugs = 1120;
        ViewBag.ExpiredWarningDrugs = 14;
        ViewBag.TotalUsers = await _context.Users.CountAsync(); // Thay thế bằng data thật luôn cho chuyên nghiệp

        // 2. DATA THẬT: Logs
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

        // 3. MOCK DATA: Chart
        ViewBag.ChartLabels = new string[] { "Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4", "Tháng 5", "Tháng 6" };
        ViewBag.ChartData = new int[] { 85, 120, 90, 145, 130, 185 };

        // 4. DATA THẬT: Thuốc mới
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

        // 5. DATA THẬT: Người dùng mới (ĐÃ SỬA ĐỔI ĐỂ TƯƠNG THÍCH BẢNG ROLE MỚI) 🌟
        var dbUsers = await _context.Users
            .Include(u => u.Role) // Nạp kèm bảng dữ liệu Role 
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .ToListAsync();

        ViewBag.UserList = dbUsers.Select(u => new {
            Id = u.Id,
            Name = u.FullName ?? "Người dùng mới",
            Email = u.Email,
            Role = u.Role?.Name ?? "User", // SỬA ĐỔI: Lấy thuộc tính Name từ object Role liên kết 🌟
            AvatarInitials = string.IsNullOrEmpty(u.FullName) 
                ? "NN" 
                : string.Concat(u.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(n => n[0])).ToUpper(),
            IsActive = u.IsActive
        }).ToList();

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ToggleUserStatus(int userId, bool activate)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return Json(new { success = false, message = "Người dùng không tồn tại" });

        try 
        {
            user.IsActive = activate;
            await _context.SaveChangesAsync();

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
                "login"  => "text-indigo-600 bg-indigo-50 border-indigo-200", // Thêm màu cho login đồng bộ với Index
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