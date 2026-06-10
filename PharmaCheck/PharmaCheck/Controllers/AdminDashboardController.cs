using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCheck.Data; // Đảm bảo namespace này đúng với đường dẫn chứa ApplicationDbContext của bạn

namespace PharmaCheck.Controllers;

[Authorize(Roles = "Admin")] // Bảo mật cấp cao: Chỉ tài khoản có quyền Admin mới được phép truy cập
public class AdminDashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    // Inject ApplicationDbContext để truy vấn bảng AuditLog và Drug thật
    public AdminDashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // =============================================================
        // 1. MOCK DATA: Số liệu thống kê ở các thẻ Metric Cards (GIỮ NGUYÊN)
        // =============================================================
        ViewBag.TotalDrugs = 1248;
        ViewBag.ActiveDrugs = 1120;
        ViewBag.ExpiredWarningDrugs = 14;
        ViewBag.TotalUsers = 520;

        // =============================================================
        // 2. DATA THẬT: Chỉ lấy Nhật ký hoạt động từ Database
        // =============================================================
        var rawLogs = await _context.AuditLogs
            .OrderByDescending(al => al.CreatedAt) // Sắp xếp log mới nhất lên đầu
            .Take(5)                               // Chỉ lấy 5 log gần nhất
            .ToListAsync();

        ViewBag.SystemLogs = rawLogs.Select(log => new {
            Message = log.Message, // Nội dung hành động lưu trong DB
            Time = FormatAuditLogTime(log.CreatedAt), // Tính toán thời gian kiểu "Vừa xong", "5 phút trước"
            
            // Đồng bộ bộ màu CSS Tailwind động dựa vào loại Action trong Db
            Color = log.ActionType?.ToLower() switch
            {
                "create" => "text-green-600 bg-green-50",
                "edit"   => "text-blue-600 bg-blue-50",
                "delete" => "text-red-600 bg-red-50",
                "login"  => "text-indigo-600 bg-indigo-50",
                _        => "text-slate-600 bg-slate-50"
            },
            Icon = log.ActionType?.ToLower() switch
            {
                "create" => "fa-plus-circle",
                "edit"   => "fa-edit",
                "delete" => "fa-trash-alt",
                "login"  => "fa-sign-in-alt",
                _        => "fa-info-circle"
            }
        }).ToList();

        // =============================================================
        // 3. MOCK DATA: Dữ liệu biểu đồ Chart.js (GIỮ NGUYÊN)
        // =============================================================
        ViewBag.ChartLabels = new string[] { "Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4", "Tháng 5", "Tháng 6" };
        ViewBag.ChartData = new int[] { 85, 120, 90, 145, 130, 185 };

        // =============================================================
        // 4. DATA THẬT: Danh sách thuốc mới cập nhật hoặc thêm gần đây từ DB
        // =============================================================
        var dbRecentDrugs = await _context.Drugs
            .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt) // Ưu tiên ngày cập nhật, nếu chưa có thì lấy ngày tạo
            .Take(5)                                            // Lấy ra top 5 thuốc mới nhất
            .ToListAsync();

        ViewBag.RecentDrugs = dbRecentDrugs.Select(d => new {
            Id = d.Id,
            Name = d.Name,
            // Sử dụng toán tử ?? để phòng trường hợp cột dữ liệu trong DB bị NULL thì giao diện vẫn không lỗi
            ActiveIngredient = d.ActiveIngredient ?? "Chưa cập nhật", 
            Manufacturer = d.Manufacturer ?? "Chưa rõ", 
            UpdatedAt = FormatAuditLogTime(d.UpdatedAt ?? d.CreatedAt), // Dùng chung hàm format thời gian thân thiện
            Status = d.IsActive ? "Active" : "Inactive"
        }).ToList();

        // =============================================================
        // 5. MOCK DATA: Danh sách quản lý tài khoản người dùng (GIỮ NGUYÊN)
        // =============================================================
        var mockUsers = new List<dynamic>
        {
            new { Id = 1, Name = "Nguyễn Văn A", Email = "nguyenvana@gmail.com", Role = "User", AvatarInitials = "VA", IsActive = true },
            new { Id = 2, Name = "Trần Thị B", Email = "tranthib@gmail.com", Role = "Moderator", AvatarInitials = "TB", IsActive = false },
            new { Id = 3, Name = "Lê Hoàng C", Email = "lehoangc@gmail.com", Role = "User", AvatarInitials = "HC", IsActive = true },
            new { Id = 4, Name = "Phạm Minh D", Email = "phamminhd@gmail.com", Role = "User", AvatarInitials = "MD", IsActive = true }
        };
        ViewBag.UserList = mockUsers;

        return View();
    }

    // =============================================================
    // KÊNH XEM TẤT CẢ LỊCH SỬ LOG (HỆ THỐNG THẬT)
    // =============================================================
    [HttpGet]
    public async Task<IActionResult> Logs(string search, int page = 1)
    {
        int pageSize = 20; // Mỗi trang hiển thị 20 dòng log
        
        // Khởi tạo câu truy vấn từ DB
        var query = _context.AuditLogs.AsNoTracking();

        // Tìm kiếm nếu admin nhập từ khóa (Tìm theo nội dung log hoặc loại hành động)
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(l => l.Message.Contains(search) || l.ActionType.Contains(search));
        }

        // Sắp xếp log mới nhất lên đầu
        query = query.OrderByDescending(l => l.CreatedAt);

        // Tính toán phân trang
        int totalLogs = await query.CountAsync();
        var rawLogs = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Ép kiểu sang cấu trúc object nặc danh để View dễ đọc (giống trang Dashboard)
        var formattedLogs = rawLogs.Select(log => new {
            Id = log.Id,
            Message = log.Message, // Nội dung log lưu trong DB
            Time = log.CreatedAt.AddHours(7).ToString("dd/MM/yyyy HH:mm:ss"), // Hiển thị ngày giờ chi tiết đầy đủ
            Action = log.ActionType,
            Color = log.ActionType?.ToLower() switch
            {
                "create" => "text-green-600 bg-green-50 border-green-200",
                "edit"   => "text-blue-600 bg-blue-50 border-blue-200",
                "delete" => "text-red-600 bg-red-50 border-red-200",
                "login"  => "text-indigo-600 bg-indigo-50 border-indigo-200",
                _        => "text-slate-600 bg-slate-50 border-slate-200"
            },
            Icon = log.ActionType?.ToLower() switch
            {
                "create" => "fa-plus-circle",
                "edit"   => "fa-edit",
                "delete" => "fa-trash-alt",
                "login"  => "fa-sign-in-alt",
                _        => "fa-info-circle"
            }
        }).ToList<dynamic>();

        // Truyền dữ liệu phân trang và tìm kiếm qua ViewBag
        ViewBag.AllLogs = formattedLogs;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalLogs / pageSize);
        ViewBag.SearchKeyword = search;

        return View();
    }

    /// <summary>
    /// Hàm helper tính mốc thời gian hiển thị thân thiện trên UI
    /// </summary>
    private string FormatAuditLogTime(DateTime utcTime)
    {
        var timeSpan = DateTime.UtcNow - utcTime;

        if (timeSpan.TotalSeconds < 60) return "Vừa xong";
        if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} phút trước";
        if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} giờ trước";
        
        // Nếu quá 24h, hiển thị ngày giờ chuẩn Việt Nam (Múi giờ UTC+7)
        return utcTime.AddHours(7).ToString("dd/MM/yyyy HH:mm");
    }
}