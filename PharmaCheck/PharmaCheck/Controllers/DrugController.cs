using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCheck.Data;
using PharmaCheck.Models;

namespace PharmaCheck.Controllers;

public class DrugController : Controller
{
    private readonly ApplicationDbContext _context;
    private const int PageSize = 10;

    public DrugController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Hiển thị danh sách thuốc với hỗ trợ tìm kiếm, lọc và phân trang
    /// </summary>
    public async Task<IActionResult> Index(string searchTerm = "", string status = "", int page = 1)
    {
        try
        {
            // Lấy tất cả dữ liệu từ cơ sở dữ liệu
            var query = _context.Drugs.AsQueryable();

            // Tìm kiếm theo tên, hoạt chất hoặc mã thuốc
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(d => d.Name.Contains(searchTerm) ||
                                        d.ActiveIngredient.Contains(searchTerm) ||
                                        d.Manufacturer.Contains(searchTerm));
            }

            // Lọc theo trạng thái
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

            // Tính toán số bản ghi tổng cộng
            int totalRecords = await query.CountAsync();

            // Phân trang
            var drugs = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Tạo ViewModel
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
    /// Lấy thông tin chi tiết của một thuốc theo ID (trả về JSON)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDrugById(int id)
    {
        try
        {
            var drug = await _context.Drugs.FirstOrDefaultAsync(d => d.Id == id);
            
            if (drug == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thuốc" });
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

    /// <summary>
    /// Lưu thuốc mới hoặc cập nhật thuốc hiện có
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SaveDrug([FromBody] Drug model)
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

            // Kiểm tra xem tên thuốc đã tồn tại chưa (ngoài trừ bản ghi hiện tại)
            if (!string.IsNullOrEmpty(model.Name))
            {
                var existingDrug = await _context.Drugs
                    .FirstOrDefaultAsync(d => d.Name == model.Name && d.Id != model.Id);
                
                if (existingDrug != null)
                {
                    return Json(new { success = false, message = "Tên thuốc này đã tồn tại" });
                }
            }

            if (model.Id == 0)
            {
                // Thêm mới
                model.CreatedAt = DateTime.UtcNow;
                _context.Drugs.Add(model);
            }
            else
            {
                // Cập nhật
                var existingDrug = await _context.Drugs.FirstOrDefaultAsync(d => d.Id == model.Id);
                
                if (existingDrug == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thuốc để cập nhật" });
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
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = model.Id == 0 ? "Thêm thuốc thành công" : "Cập nhật thuốc thành công",
                data = new { id = model.Id }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
        }
    }

    /// <summary>
    /// Xóa thuốc theo ID
    /// </summary>
    [HttpDelete]
    [HttpPost]
    public async Task<IActionResult> DeleteDrug(int id)
    {
        try
        {
            var drug = await _context.Drugs.FirstOrDefaultAsync(d => d.Id == id);
            
            if (drug == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thuốc để xóa" });
            }

            _context.Drugs.Remove(drug);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa thuốc thành công" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
        }
    }

    /// <summary>
    /// API để lấy danh sách thuốc khi tìm kiếm/lọc (AJAX)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDrugsPartial(string searchTerm = "", string status = "", int page = 1)
    {
        try
        {
            var query = _context.Drugs.AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(d => d.Name.Contains(searchTerm) ||
                                        d.ActiveIngredient.Contains(searchTerm));
            }

            // Lọc theo trạng thái
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
