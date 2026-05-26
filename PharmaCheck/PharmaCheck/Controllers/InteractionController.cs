using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCheck.Data;
using PharmaCheck.Models;

namespace PharmaCheck.Controllers;

/// <summary>
/// Controller quản lý tương tác thuốc và chống chỉ định thuốc-bệnh
/// Xử lý tìm kiếm, lọc, phân trang dữ liệu từ database
/// </summary>
public class InteractionController : Controller
{
    private readonly ApplicationDbContext _context;
    private const int PageSize = 10; // Số bản ghi trên mỗi trang

    public InteractionController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Action chính - Hiển thị trang quản lý tương tác với tab tương tác thuốc mặc định
    /// GET: /Interaction
    /// </summary>
    public IActionResult Index()
    {
        // Tạo ViewModel mặc định
        var viewModel = new InteractionManagementViewModel();

        // Lấy dữ liệu mặc định cho tương tác thuốc (trang 1)
        var interactionsData = GetDrugInteractionsData(
            searchTerm: null,
            severityLevel: null,
            pageNumber: 1
        );

        viewModel.DrugInteractions = interactionsData.Items;
        viewModel.InteractionCurrentPage = interactionsData.CurrentPage;
        viewModel.InteractionTotalPages = interactionsData.TotalPages;
        viewModel.InteractionTotalRecords = interactionsData.TotalRecords;

        // Lấy dữ liệu mặc định cho chống chỉ định thuốc-bệnh (trang 1)
        var contraindicationsData = GetDrugDiseaseContraindicationsData(
            searchTerm: null,
            riskLevel: null,
            pageNumber: 1
        );

        viewModel.DrugDiseaseContraindications = contraindicationsData.Items;
        viewModel.ContraindicationCurrentPage = contraindicationsData.CurrentPage;
        viewModel.ContraindicationTotalPages = contraindicationsData.TotalPages;
        viewModel.ContraindicationTotalRecords = contraindicationsData.TotalRecords;

        return View(viewModel);
    }

    /// <summary>
    /// Action xử lý tìm kiếm và lọc tương tác thuốc
    /// POST: /Interaction/SearchAndFilterInteractions
    /// </summary>
    /// <param name="searchTerm">Từ khóa tìm kiếm (tên thuốc hoặc hoạt chất)</param>
    /// <param name="severityLevel">Mức độ nghiêm trọng (1-5), nếu null thì không lọc</param>
    /// <param name="pageNumber">Số trang cần hiển thị (mặc định là 1)</param>
    [HttpPost]
    public IActionResult SearchAndFilterInteractions(string? searchTerm, int? severityLevel, int pageNumber = 1)
    {
        // Lấy dữ liệu theo điều kiện tìm kiếm và lọc
        var interactionsData = GetDrugInteractionsData(searchTerm, severityLevel, pageNumber);

        // Trả về phần HTML của bảng dữ liệu (để cập nhật bảng mà không reload trang)
        return PartialView("_DrugInteractionsTable", interactionsData);
    }

    /// <summary>
    /// Action xử lý tìm kiếm và lọc chống chỉ định thuốc-bệnh
    /// POST: /Interaction/SearchAndFilterContraindications
    /// </summary>
    /// <param name="searchTerm">Từ khóa tìm kiếm (tên thuốc hoặc tên bệnh)</param>
    /// <param name="riskLevel">Mức độ rủi ro (1-5), nếu null thì không lọc</param>
    /// <param name="pageNumber">Số trang cần hiển thị (mặc định là 1)</param>
    [HttpPost]
    public IActionResult SearchAndFilterContraindications(string? searchTerm, int? riskLevel, int pageNumber = 1)
    {
        // Lấy dữ liệu theo điều kiện tìm kiếm và lọc
        var contraindicationsData = GetDrugDiseaseContraindicationsData(searchTerm, riskLevel, pageNumber);

        // Trả về phần HTML của bảng dữ liệu (để cập nhật bảng mà không reload trang)
        return PartialView("_DrugDiseaseContraindicationsTable", contraindicationsData);
    }

    /// <summary>
    /// Hàm private để lấy dữ liệu tương tác thuốc với tìm kiếm, lọc, và phân trang
    /// </summary>
    /// <param name="searchTerm">Từ khóa tìm kiếm (tìm trong tên thuốc nguồn, thuốc đích, hoặc mô tả)</param>
    /// <param name="severityLevel">Mức độ nghiêm trọng (1-5), null = không lọc</param>
    /// <param name="pageNumber">Số trang (bắt đầu từ 1)</param>
    private PaginatedResult<DrugInteractionDisplayDto> GetDrugInteractionsData(
        string? searchTerm, 
        int? severityLevel, 
        int pageNumber)
    {
        // Bước 1: Query cơ bản từ database, Include() tên thuốc liên quan
        var query = _context.DrugInteractions
            .Include(di => di.SourceDrug)  // Lấy thông tin thuốc nguồn
            .Include(di => di.TargetDrug)  // Lấy thông tin thuốc đích
            .AsQueryable();

        // Bước 2: Áp dụng filter tìm kiếm (nếu có)
        // Tìm kiếm trong tên thuốc nguồn, thuốc đích, hoặc mô tả
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

        // Bước 3: Áp dụng filter mức độ nghiêm trọng (nếu có)
        if (severityLevel.HasValue && severityLevel > 0)
        {
            query = query.Where(di => di.SeverityLevel == severityLevel.Value);
        }

        // Bước 4: Tính tổng số bản ghi và số trang
        int totalRecords = query.Count();
        int totalPages = (int)Math.Ceiling((double)totalRecords / PageSize);

        // Bước 5: Phân trang - Skip số hàng của trang trước, Take PageSize bản ghi
        var interactions = query
            .OrderByDescending(di => di.CreatedAt) // Sắp xếp theo thời gian tạo (mới nhất trước)
            .Skip((pageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList(); // Thực thi query tại đây

        // Bước 6: Chuyển đổi sang DTO để hiển thị (bao gồm tên thuốc)
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

        // Bước 7: Trả về kết quả phân trang
        return new PaginatedResult<DrugInteractionDisplayDto>
        {
            Items = dtoList,
            CurrentPage = Math.Max(1, Math.Min(pageNumber, totalPages)), // Đảm bảo trang hợp lệ
            TotalPages = Math.Max(1, totalPages), // Tối thiểu 1 trang
            TotalRecords = totalRecords,
            PageSize = PageSize
        };
    }

    /// <summary>
    /// Hàm private để lấy dữ liệu chống chỉ định thuốc-bệnh với tìm kiếm, lọc, và phân trang
    /// </summary>
    /// <param name="searchTerm">Từ khóa tìm kiếm (tìm trong tên thuốc, tên bệnh, hoặc cảnh báo)</param>
    /// <param name="riskLevel">Mức độ rủi ro (1-5), null = không lọc</param>
    /// <param name="pageNumber">Số trang (bắt đầu từ 1)</param>
    private PaginatedResult<DrugDiseaseContraindicationDisplayDto> GetDrugDiseaseContraindicationsData(
        string? searchTerm,
        int? riskLevel,
        int pageNumber)
    {
        // Bước 1: Query cơ bản từ database, Include() thông tin thuốc và bệnh
        var query = _context.DrugDiseaseContraindications
            .Include(ddc => ddc.Drug)    // Lấy thông tin thuốc
            .Include(ddc => ddc.Disease) // Lấy thông tin bệnh
            .AsQueryable();

        // Bước 2: Áp dụng filter tìm kiếm (nếu có)
        // Tìm kiếm trong tên thuốc, tên bệnh, cảnh báo, hoặc khuyến nghị
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

        // Bước 3: Áp dụng filter mức độ rủi ro (nếu có)
        if (riskLevel.HasValue && riskLevel > 0)
        {
            query = query.Where(ddc => ddc.RiskLevel == riskLevel.Value);
        }

        // Bước 4: Tính tổng số bản ghi và số trang
        int totalRecords = query.Count();
        int totalPages = (int)Math.Ceiling((double)totalRecords / PageSize);

        // Bước 5: Phân trang - Skip số hàng của trang trước, Take PageSize bản ghi
        var contraindications = query
            .OrderByDescending(ddc => ddc.CreatedAt) // Sắp xếp theo thời gian tạo (mới nhất trước)
            .Skip((pageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList(); // Thực thi query tại đây

        // Bước 6: Chuyển đổi sang DTO để hiển thị (bao gồm tên thuốc và tên bệnh)
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

        // Bước 7: Trả về kết quả phân trang
        return new PaginatedResult<DrugDiseaseContraindicationDisplayDto>
        {
            Items = dtoList,
            CurrentPage = Math.Max(1, Math.Min(pageNumber, totalPages)), // Đảm bảo trang hợp lệ
            TotalPages = Math.Max(1, totalPages), // Tối thiểu 1 trang
            TotalRecords = totalRecords,
            PageSize = PageSize
        };
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
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