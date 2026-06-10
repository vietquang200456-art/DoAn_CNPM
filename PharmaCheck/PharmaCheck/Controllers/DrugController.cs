using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCheck.Data;
using PharmaCheck.Models;
using PharmaCheck.Services; // 1. THÊM NAMESPACE CỦA DICH VỤ LOG

namespace PharmaCheck.Controllers;

[Authorize] // Bắt buộc mọi người dùng phải đăng nhập trước khi vào Controller này
public class DrugController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _logService; // 2. KHAI BÁO BIẾN DỊCH VỤ LOG
    private const int PageSize = 10;

    // 3. INJECT IAUDITLOGSERVICE VÀO HÀM KHỞI TẠO
    public DrugController(ApplicationDbContext context, IAuditLogService logService)
    {
        _context = context;
        _logService = logService;
    }

    #region KHÔNG GIAN ĐỌC (CẢ USER VÀ ADMIN ĐỀU TRUY CẬP ĐƯỢC)

    /// <summary>
    /// Hiển thị danh sách thuốc với hỗ trợ tìm kiếm, lọc và phân trang
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(string searchTerm = "", string status = "", int page = 1)
    {
        try
        {
            var query = _context.Drugs.AsQueryable();

            // Tìm kiếm theo tên, hoạt chất hoặc nhà sản xuất
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var normalizedSearch = searchTerm.ToLower().Trim();
                query = query.Where(d => d.Name.ToLower().Contains(normalizedSearch) ||
                                         d.ActiveIngredient.ToLower().Contains(normalizedSearch) ||
                                         d.Manufacturer.ToLower().Contains(normalizedSearch));
            }

            // Lọc theo trạng thái hoạt động
            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "active":
                        query = query.Where(d => d.IsActive);
                        break;
                    case "inactive":
                        query = query.Where(d => !d.IsActive);
                        break;
                }
            }

            int totalRecords = await query.CountAsync();

            var drugs = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var viewModel = new DrugPagedListViewModel
            {
                Drugs = drugs,
                CurrentPage = page,
                TotalRecords = totalRecords,
                PageSize = PageSize,
                SearchTerm = searchTerm,
                StatusFilter = status
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Index: {ex.Message}");
            return View(new DrugPagedListViewModel());
        }
    }

    /// <summary>
    /// API để lấy danh sách thuốc khi tìm kiếm/lọc (AJAX hỗ trợ giao diện)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDrugsPartial(string searchTerm = "", string status = "", int page = 1)
    {
        try
        {
            var query = _context.Drugs.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var normalizedSearch = searchTerm.ToLower().Trim();
                query = query.Where(d => d.Name.ToLower().Contains(normalizedSearch) ||
                                         d.ActiveIngredient.ToLower().Contains(normalizedSearch));
            }

            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "active":
                        query = query.Where(d => d.IsActive);
                        break;
                    case "inactive":
                        query = query.Where(d => !d.IsActive);
                        break;
                }
            }

            int totalRecords = await query.CountAsync();

            var drugs = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return Json(new
            {
                success = true,
                data = drugs,
                totalRecords,
                currentPage = page,
                pageSize = PageSize,
                totalPages = (totalRecords + PageSize - 1) / PageSize
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một thuốc theo ID (Xem chi tiết)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDrugById(int id)
    {
        try
        {
            var drug = await _context.Drugs.FirstOrDefaultAsync(d => d.Id == id);
            
            if (drug == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thuốc tương ứng trong hệ thống." });
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    id = drug.Id,
                    name = drug.Name,
                    activeIngredient = drug.ActiveIngredient,
                    function = drug.Function,
                    dosage = drug.Dosage,
                    sideEffects = drug.SideEffects,
                    contraindications = drug.Contraindications,
                    manufacturer = drug.Manufacturer,
                    description = drug.Description,
                    isActive = drug.IsActive
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region KHÔNG GIAN THAY ĐỔI DỮ LIỆU (CHỈ ADMIN MỚI ĐƯỢC PHÉP TRUY CẬP)

    /// <summary>
    /// Lưu thuốc mới hoặc cập nhật thông tin thuốc hiện hành
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")] // Bảo vệ tầng sâu: Chỉ tài khoản có Role Admin mới thực thi được hành động này
    [ValidateAntiForgeryToken] // Đảm bảo an toàn, chống tấn công giả mạo yêu cầu (CSRF)
    public async Task<IActionResult> SaveDrug([FromBody] Drug model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, message = "Dữ liệu đầu vào không hợp lệ.", errors });
            }

            // Kiểm tra trùng lặp tên thuốc
            if (!string.IsNullOrEmpty(model.Name))
            {
                var existingDrug = await _context.Drugs
                    .FirstOrDefaultAsync(d => d.Name.ToLower() == model.Name.ToLower().Trim() && d.Id != model.Id);
                
                if (existingDrug != null)
                {
                    return Json(new { success = false, message = "Tên thuốc này đã tồn tại trong cơ sở dữ liệu." });
                }
            }

            string username = User.Identity?.Name ?? "Admin";
            string logMessage = "";
            string actionType = "";

            if (model.Id == 0)
            {
                // Hành động: Thêm thuốc mới
                model.CreatedAt = DateTime.UtcNow;
                _context.Drugs.Add(model);
                await _context.SaveChangesAsync(); // Lưu thành công trước

                logMessage = $"Admin '{username}' đã cập nhật thông tin thuốc {model.Name}";
                actionType = "Create";
            }
            else
            {
                // Hành động: Cập nhật thuốc cũ
                var existingDrug = await _context.Drugs.FirstOrDefaultAsync(d => d.Id == model.Id);
                
                if (existingDrug == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thuốc yêu cầu trên hệ thống." });
                }

                existingDrug.Name = model.Name;
                existingDrug.ActiveIngredient = model.ActiveIngredient;
                existingDrug.Function = model.Function;
                existingDrug.Dosage = model.Dosage;
                existingDrug.SideEffects = model.SideEffects;
                existingDrug.Contraindications = model.Contraindications;
                existingDrug.Manufacturer = model.Manufacturer;
                existingDrug.Description = model.Description;
                existingDrug.IsActive = model.IsActive;
                existingDrug.UpdatedAt = DateTime.UtcNow;

                _context.Drugs.Update(existingDrug);
                await _context.SaveChangesAsync(); // Lưu thành công trước

                // Nếu thuốc bị chuyển đổi trạng thái hoạt động (Active -> Inactive)
                if (!existingDrug.IsActive)
                {
                    logMessage = $"Thuốc '{existingDrug.Name}' bị chuyển trạng thái sang Ngừng hoạt động";
                    actionType = "Status";
                }
                else
                {
                    logMessage = $"Admin '{username}' đã cập nhật thông tin thuốc {existingDrug.Name}";
                    actionType = "Edit";
                }
            }

            // 4. THỰC THI GHI LOG VÀO DATABASE
            await _logService.LogAsync(logMessage, actionType, username);

            return Json(new
            {
                success = true,
                message = model.Id == 0 ? "Thêm thuốc mới thành công!" : "Cập nhật thông tin thuốc thành công!",
                data = new { id = model.Id }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Lỗi xử lý hệ thống: {ex.Message}" });
        }
    }

    /// <summary>
    /// Xóa thuốc ra khỏi hệ thống theo ID
    /// </summary>
    [HttpPost] // Thống nhất giữ duy nhất HttpPost để xử lý dữ liệu qua AJAX
    [Authorize(Roles = "Admin")] 
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDrug(int id)
    {
        try
        {
            var drug = await _context.Drugs.FirstOrDefaultAsync(d => d.Id == id);
            
            if (drug == null)
            {
                return Json(new { success = false, message = "Không tìm thấy dữ liệu thuốc cần xóa." });
            }

            string drugName = drug.Name;
            _context.Drugs.Remove(drug);
            await _context.SaveChangesAsync();

            // 5. GỌI HÀM GHI LOG KHI XÓA THUỐC THÀNH CÔNG
            string username = User.Identity?.Name ?? "Admin";
            await _logService.LogAsync(
                message: $"Admin '{username}' đã xóa thuốc '{drugName}' khỏi hệ thống.",
                actionType: "Delete",
                username: username
            );

            return Json(new { success = true, message = "Xóa thông tin thuốc thành công." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Không thể xóa do phát sinh lỗi: {ex.Message}" });
        }
    }

    #endregion

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}