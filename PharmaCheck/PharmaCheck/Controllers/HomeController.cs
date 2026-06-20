using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCheck.Data;
using PharmaCheck.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PharmaCheck.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Hiển thị trang chủ kèm theo danh sách các thuốc nổi bật (Ai cũng có thể xem)
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            try
            {
                var featuredDrugs = await _context.Set<Drug>()
                    .Where(d => d.IsActive)
                    .OrderByDescending(d => d.ViewCount)
                    .Take(6)
                    .ToListAsync();

                return View(featuredDrugs);
            }
            catch (Exception)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi khi tải danh sách thuốc.";
                return View(new List<Drug>());
            }
        }
        /// API endpoint: Tìm kiếm gợi ý thuốc (Chỉ cho phép khi đã login)
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SearchDrugs(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>());

            var normalizedTerm = term.ToLower().Trim();

            var drugs = await _context.Set<Drug>()
                .Where(d => d.IsActive &&
                           (d.Name.ToLower().Contains(normalizedTerm) ||
                            d.ActiveIngredient.ToLower().Contains(normalizedTerm)))
                .OrderBy(d => d.Name)
                .Select(d => new { d.Id, d.Name, d.ActiveIngredient })
                .Take(10)
                .ToListAsync();

            return Json(drugs);
        }

        /// <summary>
        /// API endpoint: Tìm kiếm gợi ý bệnh lý (Chỉ cho phép khi đã login)
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SearchDiseases(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>());

            var normalizedTerm = term.ToLower().Trim();

            var diseases = await _context.Set<Disease>()
                .Where(d => d.IsActive && d.Name.ToLower().Contains(normalizedTerm))
                .OrderBy(d => d.Name)
                .Select(d => new { d.Id, d.Name })
                .Take(10)
                .ToListAsync();

            return Json(diseases);
        }

        /// <summary>
        /// Dẫn tới giao diện Phòng Tra Cứu biệt lập (Chỉ cho phép khi đã login)
        /// </summary>
        [Authorize]
        public IActionResult Check()
        {
            return View();
        }

        /// <summary>
        /// API endpoint: Thực hiện tính toán kiểm tra tương tác thuốc (Chỉ cho phép khi đã login)
        /// </summary>
        [Authorize]
[HttpPost]
public async Task<IActionResult> CheckInteractions([FromBody] InteractionCheckRequest request)
{
    try
    {
        if (!ModelState.IsValid || request == null || 
            ((request.DrugIds == null || !request.DrugIds.Any()) &&
             (request.DiseaseIds == null || !request.DiseaseIds.Any())))
        {
            return BadRequest(new { message = "Dữ liệu yêu cầu không hợp lệ hoặc trống." });
        }

        // 1. Lưu vết lịch sử tìm kiếm
        await SaveSearchHistoryAsync(request);

        // Khởi tạo trước object kết quả để trả về (tránh dùng List<object> chung chung nếu có thể)
        var drugDrugInteractionsResult = new List<object>();
        var drugDiseaseContraindicationsResult = new List<object>();

        // 2. Kiểm tra tương tác THUỐC - THUỐC
        if (request.DrugIds != null && request.DrugIds.Count >= 2)
        {
            // Tối ưu bằng cách Select trực tiếp, bỏ Include để tăng tốc độ truy vấn
            var interactions = await _context.Set<DrugInteraction>()
                .Where(di => request.DrugIds.Contains(di.SourceDrugId) &&
                             request.DrugIds.Contains(di.TargetDrugId) &&
                             di.SourceDrugId != di.TargetDrugId)
                .Select(di => new 
                {
                    di.Id,
                    di.SeverityLevel,
                    SourceDrugName = di.SourceDrug.Name,
                    TargetDrugName = di.TargetDrug.Name,
                    di.Description,
                    di.Recommendation
                })
                .ToListAsync();

            foreach (var item in interactions)
            {
                string severityText = item.SeverityLevel == 3 ? "NGUY HIỂM" :
                                      item.SeverityLevel == 2 ? "THẬN TRỌNG" : "NHẸ";

                drugDrugInteractionsResult.Add(new
                {
                    id = item.Id,
                    level = item.SeverityLevel,
                    title = $"{item.SourceDrugName} + {item.TargetDrugName} - {severityText}",
                    description = item.Description,
                    icon = item.SeverityLevel == 3 ? "fa-exclamation-triangle" : "fa-exclamation-circle",
                    recommendation = item.Recommendation
                });
            }
        }

        // 3. Kiểm tra tương tác THUỐC - BỆNH LÝ
        if (request.DrugIds != null && request.DrugIds.Any() && request.DiseaseIds != null && request.DiseaseIds.Any())
        {
            // Tiếp tục tối ưu bằng Select trực tiếp cho tương tác Thuốc - Bệnh
            var contraindications = await _context.Set<DrugDiseaseContraindication>()
                .Where(ddc => request.DrugIds.Contains(ddc.DrugId) &&
                             request.DiseaseIds.Contains(ddc.DiseaseId))
                .Select(ddc => new 
                {
                    ddc.Id,
                    ddc.RiskLevel,
                    DrugName = ddc.Drug.Name,
                    DiseaseName = ddc.Disease.Name,
                    ddc.Warning,
                    ddc.Risk,
                    ddc.Recommendation
                })
                .ToListAsync();

            foreach (var item in contraindications)
            {
                string riskText = item.RiskLevel == 3 ? "CHỐNG CHỈ ĐỊNH" :
                                  item.RiskLevel == 2 ? "THẬN TRỌNG" : "THẤP";

                drugDiseaseContraindicationsResult.Add(new
                {
                    id = item.Id,
                    level = item.RiskLevel,
                    title = $"{item.DrugName} + {item.DiseaseName} - {riskText}",
                    description = $"{item.Warning}. {item.Risk}",
                    icon = item.RiskLevel == 3 ? "fa-ban" : "fa-exclamation-circle",
                    recommendation = item.Recommendation
                });
            }
        }

        // Trả về Ok (200) đồng bộ với cách viết Web API hiện đại
        return Ok(new
        {
            drugDrugInteractions = drugDrugInteractionsResult,
            drugDiseaseContraindications = drugDiseaseContraindicationsResult
        });
    }
    catch (Exception ex)
    {
        // Bạn nên log lỗi ex ở đây nếu có Serilog hoặc NLog
        return StatusCode(500, new { message = "Lỗi xử lý dữ liệu trên máy chủ.", error = ex.Message });
    }
}

/// <summary>
/// Hàm bổ trợ: Lưu vết lịch sử tìm kiếm vào Database theo cấu trúc tối ưu mới
/// </summary>
private async Task SaveSearchHistoryAsync(InteractionCheckRequest request)
{
    try
    {
        // Lấy ra Id của User từ phiên đăng nhập
        string rawUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? currentUserId = int.TryParse(rawUserId, out int parsedId) ? parsedId : null;

        var histories = new List<SearchHistory>();
        DateTime currentTime = DateTime.UtcNow;

        // An toàn dữ liệu: Đảm bảo danh sách không bị null để tránh lỗi NullReferenceException
        var drugIds = request.DrugIds ?? new List<int>();
        var diseaseIds = request.DiseaseIds ?? new List<int>();

        // ===================================================================
        // KỊCH BẢN 1: TRA CỨU TƯƠNG TÁC THUỐC - THUỐC (Chọn từ 2 thuốc trở lên)
        // ===================================================================
        if (drugIds.Count >= 2)
        {
            // Lấy ra tên của các thuốc để ghi vào trường SearchQuery hỗ trợ hiển thị chuỗi text thô nhanh
            var drugNames = await _context.Set<Drug>()
                .Where(d => drugIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.Name);

            // Duyệt qua toàn bộ các cặp kết hợp (Tổ hợp chập 2 của danh sách thuốc được chọn)
            for (int i = 0; i < drugIds.Count; i++)
            {
                for (int j = i + 1; j < drugIds.Count; j++)
                {
                    int drugId1 = drugIds[i];
                    int drugId2 = drugIds[j];

                    string name1 = drugNames.ContainsKey(drugId1) ? drugNames[drugId1] : $"ID {drugId1}";
                    string name2 = drugNames.ContainsKey(drugId2) ? drugNames[drugId2] : $"ID {drugId2}";

                    histories.Add(new SearchHistory
                    {
                        UserId = currentUserId,
                        SearchType = "Drug-Drug",
                        SearchQuery = $"{name1} + {name2}", // Lưu chuỗi text tường minh trực quan
                        DrugId = drugId1,       // Thuốc gốc thứ nhất
                        TargetDrugId = drugId2, // Thuốc đích thứ hai được so sánh tương tác
                        DiseaseId = null,
                        CreatedAt = currentTime
                    });
                }
            }
        }
        // ===================================================================
        // KỊCH BẢN 2: TRA CỨU TƯƠNG TÁC THUỐC - BỆNH LÝ (CHỐNG CHỈ ĐỊNH)
        // ===================================================================
        else if (drugIds.Any() && diseaseIds.Any())
        {
            // Tải danh sách tên thuốc và tên bệnh lý lên bộ nhớ tạm để build chuỗi text thô
            var drugNames = await _context.Set<Drug>().Where(d => drugIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Name);
            var diseaseNames = await _context.Set<Disease>().Where(d => diseaseIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Name);

            foreach (var drugId in drugIds)
            {
                foreach (var diseaseId in diseaseIds)
                {
                    string drugName = drugNames.ContainsKey(drugId) ? drugNames[drugId] : $"Thuốc ID {drugId}";
                    string diseaseName = diseaseNames.ContainsKey(diseaseId) ? diseaseNames[diseaseId] : $"Bệnh ID {diseaseId}";

                    histories.Add(new SearchHistory
                    {
                        UserId = currentUserId,
                        SearchType = "Drug-Disease",
                        SearchQuery = $"{drugName} trên nền {diseaseName}",
                        DrugId = drugId,
                        TargetDrugId = null,
                        DiseaseId = diseaseId, // Lưu định danh mã bệnh lý để làm thống kê bệnh lý hay gặp
                        CreatedAt = currentTime
                    });
                }
            }
        }
        // ===================================================================
        // KỊCH BẢN 3: TRA CỨU ĐƠN LẺ (Chỉ chọn 1 thuốc duy nhất hoặc không rơi vào 2 cụm trên)
        // ===================================================================
        else if (drugIds.Count == 1 && !diseaseIds.Any())
        {
            int singleDrugId = drugIds.First();
            var drug = await _context.Set<Drug>().FindAsync(singleDrugId);

            histories.Add(new SearchHistory
            {
                UserId = currentUserId,
                SearchType = "Drug",
                SearchQuery = drug?.Name ?? $"Xem chi tiết thuốc ID {singleDrugId}",
                DrugId = singleDrugId,
                TargetDrugId = null,
                DiseaseId = null,
                CreatedAt = currentTime
            });
        }

        // Thực hiện lưu hàng loạt bản ghi lịch sử vào database nếu có dữ liệu sinh ra
        if (histories.Any())
        {
            await _context.Set<SearchHistory>().AddRangeAsync(histories);
            await _context.SaveChangesAsync();
        }
    }
    catch
    {
        // Khối lệnh Fail-safe an toàn tuyệt đối: Không ném ra Exception làm gián đoạn
        // trải nghiệm nhận kết quả tương tác của y bác sĩ / người dùng nếu việc lưu log lỗi.
    }
}

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }
    }

    public class InteractionCheckRequest
    {
        public List<int> DrugIds { get; set; } = new List<int>();
        public List<int> DiseaseIds { get; set; } = new List<int>();
    }
}