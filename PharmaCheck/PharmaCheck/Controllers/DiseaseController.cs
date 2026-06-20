using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCheck.Data;
using PharmaCheck.Models;
using PharmaCheck.Services; // 1. THÊM NAMESPACE NÀY ĐỂ SỬ DỤNG SERVICE

namespace PharmaCheck.Controllers;

[Authorize]
public class DiseaseController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _logService; // 2. KHAI BÁO BIẾN DỊCH VỤ LOG
    private const int PageSize = 10;

    // 3. INJECT IAUDITLOGSERVICE VÀO CONSTRUCTOR
    public DiseaseController(ApplicationDbContext context, IAuditLogService logService)
    {
        _context = context;
        _logService = logService;
    }

    /// <summary>
    /// Hiển thị danh sách bệnh lý với hỗ trợ tìm kiếm, lọc và phân trang
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Pharmacist")] // Chỉ Admin và Pharmacist mới được phép truy cập vào trang quản lý danh sách bệnh lý
    public async Task<IActionResult> Index(string searchTerm = "", string status = "", int page = 1)
    {
        try
        {
            var query = _context.Diseases.AsQueryable();

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(d => d.Name.Contains(searchTerm) ||
                                         d.Symptoms.Contains(searchTerm) ||
                                         d.Causes.Contains(searchTerm));
            }

            // ĐỔI TÊN BIẾN: Lọc theo trạng thái ẩn/hiện (Active/Inactive) từ tham số 'status'
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

            var diseases = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var viewModel = new DiseasePagedListViewModel
            {
                Diseases = diseases,
                CurrentPage = page,
                TotalRecords = totalRecords,
                PageSize = PageSize,
                SearchTerm = searchTerm,
                SeverityFilter = status // ĐỒNG BỘ: Gán tham số status vào thuộc tính SeverityFilter của bạn
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Index: {ex.Message}");
            return View(new DiseasePagedListViewModel());
        }
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một bệnh lý theo ID
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDiseaseById(int id)
    {
        try
        {
            var disease = await _context.Diseases.FirstOrDefaultAsync(d => d.Id == id);
            
            if (disease == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bệnh lý" });
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    id = disease.Id,
                    name = disease.Name,
                    symptoms = disease.Symptoms,
                    causes = disease.Causes,
                    treatmentMethod = disease.TreatmentMethod,
                    description = disease.Description,
                    isActive = disease.IsActive
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
    /// Lưu bệnh lý mới hoặc cập nhật bệnh lý hiện có (CHỈ ADMIN)
    [HttpPost]
    [Authorize(Roles = "Admin,Pharmacist")] // Chỉ Admin và Pharmacist mới được phép thêm/sửa bệnh lý
    public async Task<IActionResult> SaveDisease([FromBody] Disease model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }

            if (!string.IsNullOrEmpty(model.Name))
            {
                var existingDisease = await _context.Diseases
                    .FirstOrDefaultAsync(d => d.Name == model.Name && d.Id != model.Id);
                
                if (existingDisease != null)
                {
                    return Json(new { success = false, message = "Tên bệnh lý này đã tồn tại" });
                }
            }

            string username = User.Identity?.Name ?? "Admin";
            string logMessage = "";
            string actionType = "";

            if (model.Id == 0)
            {
                // Thêm mới dữ liệu bệnh lý
                model.CreatedAt = DateTime.UtcNow;
                _context.Diseases.Add(model);
                await _context.SaveChangesAsync(); // Lưu DB trước để chắc chắn thành công

                // Thiết lập thông tin Log
                logMessage = $"Admin '{username}' đã thêm mới bệnh lý '{model.Name}' vào hệ thống.";
                actionType = "Create";
            }
            else
            {
                // Cập nhật dữ liệu bệnh lý hiện có
                var existingDisease = await _context.Diseases.FirstOrDefaultAsync(d => d.Id == model.Id);
                
                if (existingDisease == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bệnh lý để cập nhật" });
                }

                existingDisease.Name = model.Name;
                existingDisease.Symptoms = model.Symptoms;
                existingDisease.Causes = model.Causes;
                existingDisease.TreatmentMethod = model.TreatmentMethod;
                existingDisease.Description = model.Description;
                existingDisease.IsActive = model.IsActive;
                existingDisease.UpdatedAt = DateTime.UtcNow;

                _context.Diseases.Update(existingDisease);
                await _context.SaveChangesAsync();

                // Thiết lập thông tin Log
                logMessage = $"Admin '{username}' đã cập nhật thông tin bệnh lý '{model.Name}'.";
                actionType = "Edit";
            }

            // 4. GỌI HÀM GHI LOG THỰT KHI LƯU THÀNH CÔNG
            await _logService.LogAsync(logMessage, actionType, username);

            return Json(new
            {
                success = true,
                message = model.Id == 0 ? "Thêm bệnh lý thành công" : "Cập nhật bệnh lý thành công",
                data = new { id = model.Id }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
        }
    }

    /// <summary>
    /// Xóa bệnh lý dựa theo ID (CHỈ ADMIN)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<IActionResult> DeleteDisease(int id)
    {
        try
        {
            var disease = await _context.Diseases.FirstOrDefaultAsync(d => d.Id == id);
            
            if (disease == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bệnh lý để xóa" });
            }

            string diseaseName = disease.Name;
            _context.Diseases.Remove(disease);
            await _context.SaveChangesAsync();

            // 5. GỌI HÀM GHI LOG THỰT KHI XÓA THÀNH CÔNG
            string username = User.Identity?.Name ?? "Admin";
            await _logService.LogAsync(
                message: $"Admin '{username}' đã xóa bệnh lý '{diseaseName}' khỏi hệ thống.",
                actionType: "Delete",
                username: username
            );

            return Json(new { success = true, message = "Xóa bệnh lý thành công" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
        }
    }
    /// API để lấy danh sách bệnh lý khi tìm kiếm/lọc (AJAX) - Đã đồng bộ parameter sang 'status'
    [HttpGet]
    public async Task<IActionResult> GetDiseasesPartial(string searchTerm = "", string status = "", int page = 1)
    {
        try
        {
            var query = _context.Diseases.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(d => d.Name.Contains(searchTerm) ||
                                         d.Symptoms.Contains(searchTerm));
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

            var diseases = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return Json(new
            {
                success = true,
                data = diseases,
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}