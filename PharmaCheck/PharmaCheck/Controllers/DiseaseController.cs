using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCheck.Data;
using PharmaCheck.Models;

namespace PharmaCheck.Controllers;

public class DiseaseController : Controller
{
    private readonly ApplicationDbContext _context;
    private const int PageSize = 10;

    public DiseaseController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Hiển thị danh sách bệnh lý với hỗ trợ tìm kiếm, lọc và phân trang
    /// </summary>
    public async Task<IActionResult> Index(string searchTerm = "", string severity = "", int page = 1)
    {
        try
        {
            // Lấy tất cả dữ liệu từ cơ sở dữ liệu
            var query = _context.Diseases.AsQueryable();

            // Tìm kiếm theo tên, triệu chứng hoặc nguyên nhân
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(d => d.Name.Contains(searchTerm) ||
                                        d.Symptoms.Contains(searchTerm) ||
                                        d.Causes.Contains(searchTerm));
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(severity))
            {
                switch (severity.ToLower())
                {
                    case "active":
                        query = query.Where(d => d.IsActive);
                        break;
                    case "inactive":
                        query = query.Where(d => !d.IsActive);
                        break;
                }
            }

            // Tính toán số bản ghi tổng cộng
            int totalRecords = await query.CountAsync();

            // Phân trang
            var diseases = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Tạo ViewModel
            var viewModel = new DiseasePagedListViewModel
            {
                Diseases = diseases,
                CurrentPage = page,
                TotalRecords = totalRecords,
                PageSize = PageSize,
                SearchTerm = searchTerm,
                SeverityFilter = severity
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
    /// Lấy thông tin chi tiết của một bệnh lý theo ID (trả về JSON)
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

    /// <summary>
    /// Lưu bệnh lý mới hoặc cập nhật bệnh lý hiện có
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SaveDisease([FromBody] Disease model)
    {
        try
        {
            // Kiểm tra ModelState
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }

            // Kiểm tra xem tên bệnh lý đã tồn tại chưa (ngoài trừ bản ghi hiện tại)
            if (!string.IsNullOrEmpty(model.Name))
            {
                var existingDisease = await _context.Diseases
                    .FirstOrDefaultAsync(d => d.Name == model.Name && d.Id != model.Id);
                
                if (existingDisease != null)
                {
                    return Json(new { success = false, message = "Tên bệnh lý này đã tồn tại" });
                }
            }

            if (model.Id == 0)
            {
                // Thêm mới
                model.CreatedAt = DateTime.UtcNow;
                _context.Diseases.Add(model);
            }
            else
            {
                // Cập nhật
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
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = model.Id == 0 ? "Thêm bệnh lý thành công" : "Cập nhật bệnh lý thành công",
                data = new { id = model.Id }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
        }
    }

    /// <summary>
    /// Xóa bệnh lý theo ID
    /// </summary>
    [HttpDelete]
    [HttpPost]
    public async Task<IActionResult> DeleteDisease(int id)
    {
        try
        {
            var disease = await _context.Diseases.FirstOrDefaultAsync(d => d.Id == id);
            
            if (disease == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bệnh lý để xóa" });
            }

            _context.Diseases.Remove(disease);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa bệnh lý thành công" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
        }
    }

    /// <summary>
    /// API để lấy danh sách bệnh lý khi tìm kiếm/lọc (AJAX)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDiseasesPartial(string searchTerm = "", string severity = "", int page = 1)
    {
        try
        {
            var query = _context.Diseases.AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(d => d.Name.Contains(searchTerm) ||
                                        d.Symptoms.Contains(searchTerm));
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(severity))
            {
                switch (severity.ToLower())
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
