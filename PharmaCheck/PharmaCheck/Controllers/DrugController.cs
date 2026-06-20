using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCheck.Data;
using PharmaCheck.Models;
using PharmaCheck.Services;
using OfficeOpenXml;

namespace PharmaCheck.Controllers;

[Authorize] // Bắt buộc mọi người dùng (Admin, Pharmacist, Doctor) phải đăng nhập
public class DrugController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _logService;
    private const int PageSize = 10;

    public DrugController(ApplicationDbContext context, IAuditLogService logService)
    {
        _context = context;
        _logService = logService;
    }

    #region 🔍 KHÔNG GIAN TRA CỨU & ĐỌC DỮ LIỆU (ĐỒNG BỘ: ADMIN, PHARMACIST, DOCTOR)

    /// <summary>
    /// Trang quản lý danh sách thuốc chính dành cho Admin và Dược sĩ
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Pharmacist")] 
    public async Task<IActionResult> Index(string searchTerm = "", string status = "", int page = 1)
    {
        try
        {
            var query = _context.Drugs.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var normalizedSearch = searchTerm.ToLower().Trim();
                query = query.Where(d => d.Name.ToLower().Contains(normalizedSearch) ||
                                         d.ActiveIngredient.ToLower().Contains(normalizedSearch) ||
                                         d.Manufacturer.ToLower().Contains(normalizedSearch));
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
    /// Giao diện tra cứu thuốc biệt dược độc lập dành riêng cho Bác sĩ (Doctor) và Điều dưỡng
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Pharmacist,Doctor")] // Cho phép cả 3 quyền vào giao diện tra cứu lâm sàng
    public async Task<IActionResult> Search(string searchTerm = "", int page = 1)
    {
        try
        {
            // Chỉ lấy các thuốc đang ở trạng thái hoạt động (IsActive == true) để đảm bảo an toàn kê đơn
            var query = _context.Drugs.Where(d => d.IsActive).AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var normalizedSearch = searchTerm.ToLower().Trim();
                query = query.Where(d => d.Name.ToLower().Contains(normalizedSearch) ||
                                         d.ActiveIngredient.ToLower().Contains(normalizedSearch) ||
                                         d.Manufacturer.ToLower().Contains(normalizedSearch));
            }

            int totalRecords = await query.CountAsync();

            var drugs = await query
                .OrderBy(d => d.Name) // Sắp xếp từ A-Z giúp bác sĩ dễ tìm kiếm theo bảng chữ cái
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
                StatusFilter = "active"
            };

            return View(viewModel); 
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Drug Search: {ex.Message}");
            return View(new DrugPagedListViewModel());
        }
    }

    /// <summary>
    /// API trả về Partial dữ liệu (Dùng chung cho các bộ lọc AJAX)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,PharmaCheck,Pharmacist,Doctor")]
    public async Task<IActionResult> GetDrugsPartial(string searchTerm = "", string status = "", int page = 1)
    {
        try
        {
            var query = _context.Drugs.AsQueryable();

            // Nếu là Bác sĩ (Doctor), ép buộc hệ thống chỉ lọc những thuốc đang Active
            if (User.IsInRole("Doctor"))
            {
                query = query.Where(d => d.IsActive);
            }
            else if (!string.IsNullOrEmpty(status)) // Admin/Pharmacist thì lọc theo trạng thái chọn lọc
            {
                switch (status.ToLower())
                {
                    case "active": query = query.Where(d => d.IsActive); break;
                    case "inactive": query = query.Where(d => !d.IsActive); break;
                }
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var normalizedSearch = searchTerm.ToLower().Trim();
                query = query.Where(d => d.Name.ToLower().Contains(normalizedSearch) ||
                                         d.ActiveIngredient.ToLower().Contains(normalizedSearch));
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
    /// Lấy thông tin chi tiết của một biệt dược (Xem nhanh qua Modal hoặc Popover)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Pharmacist,Doctor")] // Bác sĩ cần xem chi tiết Tác dụng phụ / Chống chỉ định để kê đơn
    public async Task<IActionResult> GetDrugById(int id)
    {
        try
        {
            var drug = await _context.Drugs.FirstOrDefaultAsync(d => d.Id == id);
            
            if (drug == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thuốc tương ứng trong hệ thống." });
            }

            // Bảo vệ lâm sàng: Bác sĩ không được xem thông tin chi tiết của thuốc đã bị Khóa (Ngừng hoạt động)
            if (User.IsInRole("Doctor") && !drug.IsActive)
            {
                return Json(new { success = false, message = "Thuốc này hiện đã ngừng hoạt động trên hệ thống, không thể truy cập." });
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

    #region 💾 KHÔNG GIAN THAY ĐỔI DỮ LIỆU (CHỈ ADMIN VÀ PHARMACIST ĐƯỢC PHÉP)

    /// <summary>
    /// Thêm thuốc mới hoặc cập nhật thông tin thuốc hiện hành
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Pharmacist")] // Từ chối Doctor can thiệp chỉnh sửa dữ liệu gốc
    [ValidateAntiForgeryToken]
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

            if (!string.IsNullOrEmpty(model.Name))
            {
                var existingDrug = await _context.Drugs
                    .FirstOrDefaultAsync(d => d.Name.ToLower() == model.Name.ToLower().Trim() && d.Id != model.Id);
                
                if (existingDrug != null)
                {
                    return Json(new { success = false, message = "Tên thuốc này đã tồn tại trong cơ sở dữ liệu." });
                }
            }

            // Lấy chính xác danh tính và vai trò người thực hiện (Admin hoặc Pharmacist)
            string username = User.Identity?.Name ?? "Hệ thống";
            string currentRole = User.IsInRole("Admin") ? "Admin" : "Dược sĩ";
            string logMessage = "";
            string actionType = "";

            if (model.Id == 0)
            {
                model.CreatedAt = DateTime.UtcNow;
                _context.Drugs.Add(model);
                await _context.SaveChangesAsync();

                logMessage = $"{currentRole} '{username}' đã thêm mới thành công biệt dược: {model.Name}";
                actionType = "Create";
            }
            else
            {
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
                await _context.SaveChangesAsync();

                if (!existingDrug.IsActive)
                {
                    logMessage = $"Biệt dược '{existingDrug.Name}' đã bị {currentRole.ToLower()} '{username}' chuyển sang trạng thái: Ngừng hoạt động";
                    actionType = "Status";
                }
                else
                {
                    logMessage = $"{currentRole} '{username}' đã cập nhật lại thông tin biệt dược: {existingDrug.Name}";
                    actionType = "Edit";
                }
            }

            // Thực thi ghi Log lịch sử thao tác hệ thống
            await _logService.LogAsync(logMessage, actionType, username);

            return Json(new
            {
                success = true,
                message = model.Id == 0 ? "Thêm mới biệt dược thành công!" : "Cập nhật dữ liệu biệt dược thành công!",
                data = new { id = model.Id }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Lỗi xử lý hệ thống: {ex.Message}" });
        }
    }

    /// <summary>
    /// Xóa hoàn toàn bản ghi biệt dược ra khỏi cơ sở dữ liệu
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Pharmacist")]
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

            string username = User.Identity?.Name ?? "Hệ thống";
            string currentRole = User.IsInRole("Admin") ? "Admin" : "Dược sĩ";

            await _logService.LogAsync(
                message: $"{currentRole} '{username}' đã xóa vĩnh viễn thuốc '{drugName}' khỏi hệ thống.",
                actionType: "Delete",
                username: username
            );

            return Json(new { success = true, message = "Xóa thông tin thuốc khỏi kho dữ liệu thành công." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Không thể thực hiện lệnh xóa do ràng buộc dữ liệu: {ex.Message}" });
        }
    }

    /// <summary>
    /// Nhập kho dữ liệu hàng loạt từ file Excel (.xlsx) dành cho Admin và Dược Sĩ
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Pharmacist")] 
    [ValidateAntiForgeryToken] 
    public async Task<IActionResult> ImportFromExcel(IFormFile excelFile)
    {
        try
        {   
            // Thiết lập bản quyền sử dụng thư viện EPPlus 8+ không thương mại
            ExcelPackage.License.SetNonCommercialPersonal("Viet Quang");

            if (excelFile == null || excelFile.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn một tệp Excel hợp lệ." });
            }

            var extension = Path.GetExtension(excelFile.FileName).ToLower();
            if (extension != ".xlsx")
            {
                return Json(new { success = false, message = "Hệ thống chỉ hỗ trợ định dạng file Excel chuẩn OpenXML (.xlsx)." });
            }

            var importedDrugs = new List<Drug>();
            int successCount = 0;
            int duplicateCount = 0;

            using (var stream = new MemoryStream())
            {  
                await excelFile.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension?.Rows ?? 0;

                    if (rowCount < 3) 
                    {
                        return Json(new { success = false, message = "Tệp Excel trống hoặc không chứa dữ liệu hàng hóa từ dòng thứ 3." });
                    }

                    for (int row = 3; row <= rowCount; row++)
                    {
                        string name = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                        string activeIngredient = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                        string manufacturer = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                        string function = worksheet.Cells[row, 4].Value?.ToString()?.Trim();
                        string dosage = worksheet.Cells[row, 5].Value?.ToString()?.Trim();
                        string sideEffects = worksheet.Cells[row, 6].Value?.ToString()?.Trim();
                        string contraindications = worksheet.Cells[row, 7].Value?.ToString()?.Trim();
                        string description = worksheet.Cells[row, 8].Value?.ToString()?.Trim();

                        if (string.IsNullOrEmpty(name)) continue;

                        bool isExist = await _context.Drugs.AnyAsync(d => d.Name.ToLower() == name.ToLower());
                        if (isExist)
                        {
                            duplicateCount++;
                            continue; 
                        }

                        var drug = new Drug
                        {
                            Name = name,
                            ActiveIngredient = activeIngredient ?? "",
                            Manufacturer = manufacturer ?? "Chưa rõ",
                            Function = function ?? "",
                            Dosage = dosage ?? "",
                            SideEffects = sideEffects ?? "",
                            Contraindications = contraindications ?? "",
                            Description = description ?? "",
                            IsActive = true, 
                            CreatedAt = DateTime.UtcNow
                        };

                        importedDrugs.Add(drug);
                        successCount++;
                    }
                }
            }

            if (importedDrugs.Any())
            {
                _context.Drugs.AddRange(importedDrugs);
                await _context.SaveChangesAsync();

                string username = User.Identity?.Name ?? "Hệ thống";
                string currentRole = User.IsInRole("Admin") ? "Admin" : "Dược sĩ";

                await _logService.LogAsync(
                    message: $"{currentRole} '{username}' đã thực hiện import thành công {successCount} thuốc bằng Excel.",
                    actionType: "Import",
                    username: username
                );
            }

            return Json(new { 
                success = true, 
                message = $"Nhập dữ liệu hoàn tất! Đã thêm mới: {successCount} thuốc, phát hiện trùng lặp và bỏ qua: {duplicateCount}." 
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Đã xảy ra lỗi hệ thống khi phân tích cấu trúc file: {ex.Message}" });
        }
    }

    #endregion

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}