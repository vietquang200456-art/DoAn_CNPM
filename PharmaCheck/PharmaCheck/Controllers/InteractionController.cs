using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Đã map SelectListItem
using Microsoft.EntityFrameworkCore;
using PharmaCheck.Data;
using PharmaCheck.Models;
using PharmaCheck.Services;
using PharmaCheck.Models.ViewModels;

namespace PharmaCheck.Controllers;

[Authorize(Roles = "Admin,Pharmacist")]
public class InteractionController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _logService;
    private const int PageSize = 10;

    public InteractionController(ApplicationDbContext context, IAuditLogService logService)
    {
        _context = context;
        _logService = logService;
    }

    #region KHÔNG GIAN ĐỌC DỮ LIỆU

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var viewModel = new InteractionManagementViewModel();

        // 1. Lấy dữ liệu danh sách hiển thị ban đầu
        var interactionsData = GetDrugInteractionsData(null, null, 1);
        viewModel.DrugInteractions = interactionsData.Items;
        viewModel.InteractionCurrentPage = interactionsData.CurrentPage;
        viewModel.InteractionTotalPages = interactionsData.TotalPages;
        viewModel.InteractionTotalRecords = interactionsData.TotalRecords;

        var contraindicationsData = GetDrugDiseaseContraindicationsData(null, null, 1);
        viewModel.DrugDiseaseContraindications = contraindicationsData.Items;
        viewModel.ContraindicationCurrentPage = contraindicationsData.CurrentPage;
        viewModel.ContraindicationTotalPages = contraindicationsData.TotalPages;
        viewModel.ContraindicationTotalRecords = contraindicationsData.TotalRecords;

        // 2. Nạp danh sách Thuốc và Bệnh vào ViewModel để nuôi Dropdown bộ lọc hoặc modal cũ
        viewModel.AvailableDrugs = await _context.Drugs
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.Name
            }).ToListAsync();

        viewModel.AvailableDiseases = await _context.Diseases
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.Name
            }).ToListAsync();

        return View(viewModel);
    }

    [HttpPost]
    public IActionResult SearchAndFilterInteractions(string? searchTerm, int? severityLevel, int pageNumber = 1)
    {
        var interactionsData = GetDrugInteractionsData(searchTerm, severityLevel, pageNumber);
        return PartialView("_DrugInteractionsTable", interactionsData);
    }

    [HttpPost]
    public IActionResult SearchAndFilterContraindications(string? searchTerm, int? riskLevel, int pageNumber = 1)
    {
        var contraindicationsData = GetDrugDiseaseContraindicationsData(searchTerm, riskLevel, pageNumber);
        return PartialView("_DrugDiseaseContraindicationsTable", contraindicationsData);
    }

    #endregion

    #region KHÔNG GIAN XỬ LÝ ĐƯỜNG DẪN VIEW MỚI (TƯƠNG THÍCH VỚI TRANG CREATE RIÊNG BIỆT)

    /// <summary>
    /// GET: Hiển thị trang thêm mới Tương tác Thuốc - Thuốc
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CreateInteraction()
    {
        var viewModel = new CreateInteractionViewModel
        {
            // Nạp danh sách thuốc đang hoạt động vào Dropdown
            DrugList = await _context.Drugs
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Name
                }).ToListAsync()
        };

        return View(viewModel);
    }

    /// <summary>
    /// POST: Tiếp nhận dữ liệu lưu Form từ trang CreateInteraction.cshtml gửi lên
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInteraction(CreateInteractionViewModel model)
    {
        // 1. Kiểm tra logic nghiệp vụ cơ bản trước khi lưu
        if (model.SourceDrugId == model.TargetDrugId)
        {
            ModelState.AddModelError("", "Một loại thuốc không thể tự tương tác với chính nó!");
        }

        // 2. Kiểm tra trùng lặp bản ghi dưới database
        if (model.SourceDrugId.HasValue && model.TargetDrugId.HasValue)
        {
            bool isExist = await _context.DrugInteractions.AnyAsync(di =>
                (di.SourceDrugId == model.SourceDrugId && di.TargetDrugId == model.TargetDrugId) ||
                (di.SourceDrugId == model.TargetDrugId && di.TargetDrugId == model.SourceDrugId));

            if (isExist)
            {
                ModelState.AddModelError("", "Cặp thuốc này đã được thiết lập tương tác từ trước!");
            }
        }

        // 3. Nếu form hợp lệ thì thực hiện lưu vào database
        if (ModelState.IsValid)
        {
            var newInteraction = new DrugInteraction
            {
                SourceDrugId = model.SourceDrugId!.Value,
                TargetDrugId = model.TargetDrugId!.Value,
                SeverityLevel = model.SeverityLevel!.Value,
                Description = model.Description ?? string.Empty,
                Recommendation = model.Recommendation ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            _context.DrugInteractions.Add(newInteraction);
            await _context.SaveChangesAsync();

            // Ghi Log hệ thống
            string username = User.Identity?.Name ?? "Admin";
            var sourceDrug = await _context.Drugs.FindAsync(model.SourceDrugId);
            var targetDrug = await _context.Drugs.FindAsync(model.TargetDrugId);
            await _logService.LogAsync($"Admin '{username}' đã thêm mới tương tác giữa '{sourceDrug?.Name}' và '{targetDrug?.Name}'", "Create", username);

            // Lưu thành công, chuyển hướng về lại trang danh sách chính (Index)
            return RedirectToAction(nameof(Index));
        }

        // 4. Nếu có lỗi dữ liệu, nạp lại danh sách Dropdown để hiển thị lại Form kèm thông báo lỗi cụ thể
        model.DrugList = await _context.Drugs
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.Name
            }).ToListAsync();

        return View(model);
    }

    /// <summary>
    /// GET: Hiển thị trang thêm mới Chống chỉ định Thuốc - Bệnh
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CreateContraindication()
    {
        var viewModel = new CreateContraindicationViewModel
        {
            // Nạp danh sách thuốc
            DrugList = await _context.Drugs
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Name
                }).ToListAsync(),

            // Nạp danh sách bệnh lý
            DiseaseList = await _context.Diseases
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Name
                }).ToListAsync()
        };

        return View(viewModel);
    }

    /// <summary>
    /// POST: Tiếp nhận dữ liệu lưu Form từ trang CreateContraindication.cshtml gửi lên
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateContraindication(CreateContraindicationViewModel model)
    {
        // 1. Kiểm tra trùng lặp cấu hình
        if (model.DrugId.HasValue && model.DiseaseId.HasValue)
        {
            bool isExist = await _context.DrugDiseaseContraindications.AnyAsync(ddc =>
                ddc.DrugId == model.DrugId && ddc.DiseaseId == model.DiseaseId);

            if (isExist)
            {
                ModelState.AddModelError("", "Chống chỉ định cho thuốc và bệnh này đã được thiết lập trước đó!");
            }
        }

        // 2. Nếu Form thỏa mãn tất cả data annotation, tiến hành lưu
        if (ModelState.IsValid)
        {
            var newContra = new DrugDiseaseContraindication
            {
                DrugId = model.DrugId!.Value,
                DiseaseId = model.DiseaseId!.Value,
                RiskLevel = model.RiskLevel!.Value,
                Warning = model.Warning ?? string.Empty,
                Risk = model.Risk ?? string.Empty,
                Recommendation = string.Empty, // Cung cấp giá trị mặc định tránh lỗi DB nếu thuộc tính này là bắt buộc
                CreatedAt = DateTime.UtcNow
            };

            _context.DrugDiseaseContraindications.Add(newContra);
            await _context.SaveChangesAsync();

            // Ghi Log hệ thống
            string username = User.Identity?.Name ?? "Admin";
            var drug = await _context.Drugs.FindAsync(model.DrugId);
            var disease = await _context.Diseases.FindAsync(model.DiseaseId);
            await _logService.LogAsync($"Admin '{username}' đã thêm mới chống chỉ định cho Thuốc '{drug?.Name}' - Bệnh '{disease?.Name}'", "Create", username);

            return RedirectToAction(nameof(Index));
        }

        // 3. Nếu có lỗi dữ liệu, nạp lại Dropdown cho giao diện hiển thị lại
        model.DrugList = await _context.Drugs
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.Name
            }).ToListAsync();

        model.DiseaseList = await _context.Diseases
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.Name
            }).ToListAsync();

        return View(model);
    }
    // hàm GET để hiển thị trang EditInteraction.cshtml với dữ liệu đã tồn tại của tương tác cần sửa đổi
    [HttpGet]
    public async Task<IActionResult> EditInteraction(int id)
    {
        var interaction = await _context.DrugInteractions
            .Include(x => x.SourceDrug)
            .Include(x => x.TargetDrug)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (interaction == null)
        {
            return NotFound();
        }

        var model = new EditInteractionViewModel
        {
            Id = interaction.Id,

            SourceDrugId = interaction.SourceDrugId,
            TargetDrugId = interaction.TargetDrugId,

            SourceDrugName = interaction.SourceDrug?.Name ?? "",
            TargetDrugName = interaction.TargetDrug?.Name ?? "",

            SeverityLevel = interaction.SeverityLevel,
            Description = interaction.Description,
            Recommendation = interaction.Recommendation
        };

        return View(model);
    }
    #endregion
    #region KHÔNG GIAN CŨ: SỬA ĐỔI / XOÁ TRÊN TRANG INDEX (MẪU MODAL/AJAX GIỮ NGUYÊN)
    // hàm POST để tiếp nhận dữ liệu đã sửa đổi từ form EditInteraction.cshtml gửi lên và lưu thay đổi vào database
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditInteraction(EditInteractionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var interaction = await _context.DrugInteractions
            .Include(x => x.SourceDrug)
            .Include(x => x.TargetDrug)
            .FirstOrDefaultAsync(x => x.Id == model.Id);

        if (interaction == null)
        {
            return NotFound();
        }

        interaction.SeverityLevel = model.SeverityLevel;
        interaction.Description = model.Description;
        interaction.Recommendation = model.Recommendation;
        interaction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        string username = User.Identity?.Name ?? "Admin";

        await _logService.LogAsync(
            $"Admin '{username}' đã cập nhật tương tác giữa '{interaction.SourceDrug?.Name}' và '{interaction.TargetDrug?.Name}'",
            "Edit",
            username);

        TempData["SuccessMessage"] =
            "Cập nhật tương tác thuốc thành công.";

        return RedirectToAction(nameof(Index));
    }
    // hàm get để hiển thị trang EditContraindication.cshtml với dữ liệu đã tồn tại của chống chỉ định cần sửa đổi
    [HttpGet]
    public async Task<IActionResult> EditContraindication(int id)
    {
        var contraindication = await _context
            .DrugDiseaseContraindications
            .Include(x => x.Drug)
            .Include(x => x.Disease)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (contraindication == null)
        {
            return NotFound();
        }

        var model = new EditContraindicationViewModel
        {
            Id = contraindication.Id,

            DrugId = contraindication.DrugId,
            DiseaseId = contraindication.DiseaseId,

            DrugName = contraindication.Drug?.Name ?? "",
            DiseaseName = contraindication.Disease?.Name ?? "",

            RiskLevel = contraindication.RiskLevel,
            Warning = contraindication.Warning,
            Risk = contraindication.Risk
        };

        return View(model);
    }
    // hàm POST để tiếp nhận dữ liệu đã sửa đổi từ form EditContraindication.cshtml gửi lên và lưu thay đổi vào database
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditContraindication(
    EditContraindicationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var contraindication = await _context
            .DrugDiseaseContraindications
            .Include(x => x.Drug)
            .Include(x => x.Disease)
            .FirstOrDefaultAsync(x => x.Id == model.Id);

        if (contraindication == null)
        {
            return NotFound();
        }

        contraindication.RiskLevel = model.RiskLevel;
        contraindication.Warning = model.Warning;
        contraindication.Risk = model.Risk;
        contraindication.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        string username = User.Identity?.Name ?? "Admin";

        await _logService.LogAsync(
            $"Admin '{username}' đã cập nhật chống chỉ định giữa thuốc '{contraindication.Drug?.Name}' và bệnh '{contraindication.Disease?.Name}'",
            "Edit",
            username);

        TempData["SuccessMessage"] =
            "Cập nhật cấu hình chống chỉ định thành công.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteInteraction(int id)
    {
        try
        {
            var interaction = await _context.DrugInteractions.FindAsync(id);
            if (interaction == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu." });

            _context.DrugInteractions.Remove(interaction);
            await _context.SaveChangesAsync();

            string username = User.Identity?.Name ?? "Admin";
            await _logService.LogAsync($"Admin '{username}' đã xóa cấu hình tương tác #{id}", "Delete", username);
            return Json(new { success = true });
        }
        catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteContraindication(int id)
    {
        try
        {
            var contra = await _context.DrugDiseaseContraindications.FindAsync(id);
            if (contra == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu mục tiêu." });

            _context.DrugDiseaseContraindications.Remove(contra);
            await _context.SaveChangesAsync();

            string username = User.Identity?.Name ?? "Admin";
            await _logService.LogAsync($"Admin '{username}' đã xóa cấu hình chống chỉ định #{id}", "Delete", username);
            return Json(new { success = true });
        }
        catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
    }

    #endregion

    #region HÀM TRỢ GIÚP PHÂN TRANG (PRIVATE METHODS)

    private PaginatedResult<DrugInteractionDisplayDto> GetDrugInteractionsData(string? searchTerm, int? severityLevel, int pageNumber)
    {
        var query = _context.DrugInteractions.Include(di => di.SourceDrug).Include(di => di.TargetDrug).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string searchLower = searchTerm.ToLower();
            query = query.Where(di =>
                di.SourceDrug!.Name.ToLower().Contains(searchLower) ||
                di.TargetDrug!.Name.ToLower().Contains(searchLower) ||
                di.SourceDrug!.ActiveIngredient.ToLower().Contains(searchLower) ||
                di.TargetDrug!.ActiveIngredient.ToLower().Contains(searchLower) ||
                di.Description.ToLower().Contains(searchLower)
            );
        }

        if (severityLevel.HasValue && severityLevel > 0) query = query.Where(di => di.SeverityLevel == severityLevel.Value);

        int totalRecords = query.Count();
        int totalPages = (int)Math.Ceiling((double)totalRecords / PageSize);
        int validPage = Math.Max(1, Math.Min(pageNumber, totalPages));

        var interactions = query.OrderByDescending(di => di.CreatedAt).Skip((validPage - 1) * PageSize).Take(PageSize).ToList();

        var dtoList = interactions.Select(di => new DrugInteractionDisplayDto
        {
            Id = di.Id,
            SourceDrugId = di.SourceDrugId,
            TargetDrugId = di.TargetDrugId,
            SourceDrugName = di.SourceDrug?.Name ?? "Không tìm thấy",
            TargetDrugName = di.TargetDrug?.Name ?? "Không tìm thấy",
            SeverityLevel = di.SeverityLevel,
            Description = di.Description,
            Recommendation = di.Recommendation,
            CreatedAt = di.CreatedAt
        }).ToList();

        return new PaginatedResult<DrugInteractionDisplayDto> { Items = dtoList, CurrentPage = validPage, TotalPages = Math.Max(1, totalPages), TotalRecords = totalRecords, PageSize = PageSize };
    }

    private PaginatedResult<DrugDiseaseContraindicationDisplayDto> GetDrugDiseaseContraindicationsData(string? searchTerm, int? riskLevel, int pageNumber)
    {
        var query = _context.DrugDiseaseContraindications.Include(ddc => ddc.Drug).Include(ddc => ddc.Disease).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string searchLower = searchTerm.ToLower();
            query = query.Where(ddc =>
                ddc.Drug!.Name.ToLower().Contains(searchLower) ||
                ddc.Drug!.ActiveIngredient.ToLower().Contains(searchLower) ||
                ddc.Disease!.Name.ToLower().Contains(searchLower) ||
                ddc.Warning.ToLower().Contains(searchLower) ||
                ddc.Risk.ToLower().Contains(searchLower) ||
                ddc.Recommendation.ToLower().Contains(searchLower)
            );
        }

        if (riskLevel.HasValue && riskLevel > 0) query = query.Where(ddc => ddc.RiskLevel == riskLevel.Value);

        int totalRecords = query.Count();
        int totalPages = (int)Math.Ceiling((double)totalRecords / PageSize);
        int validPage = Math.Max(1, Math.Min(pageNumber, totalPages));

        var contraindications = query.OrderByDescending(ddc => ddc.CreatedAt).Skip((validPage - 1) * PageSize).Take(PageSize).ToList();

        var dtoList = contraindications.Select(ddc => new DrugDiseaseContraindicationDisplayDto
        {
            Id = ddc.Id,
            DrugId = ddc.DrugId,
            DiseaseId = ddc.DiseaseId,
            DrugName = ddc.Drug?.Name ?? "Không tìm thấy",
            DiseaseName = ddc.Disease?.Name ?? "Không tìm thấy",
            RiskLevel = ddc.RiskLevel,
            Warning = ddc.Warning,
            Risk = ddc.Risk,
            Recommendation = ddc.Recommendation,
            CreatedAt = ddc.CreatedAt
        }).ToList();

        return new PaginatedResult<DrugDiseaseContraindicationDisplayDto> { Items = dtoList, CurrentPage = validPage, TotalPages = Math.Max(1, totalPages), TotalRecords = totalRecords, PageSize = PageSize };
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    #endregion
}
/// <summary>
/// Generic class để chứa kết quả phân trang
/// </summary>
public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new List<T>();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalRecords { get; set; }
    public int PageSize { get; set; }
}