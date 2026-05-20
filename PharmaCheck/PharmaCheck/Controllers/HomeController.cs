using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCheck.Data;
using PharmaCheck.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Hiển thị trang chủ kèm theo danh sách các thuốc nổi bật (được xem nhiều nhất)
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                // Lấy ra danh sách thuốc đang hoạt động, sắp xếp theo lượt xem giảm dần (Top 6 thuốc)
                var featuredDrugs = await _context.Set<Drug>()
                    .Where(d => d.IsActive)
                    .OrderByDescending(d => d.ViewCount)
                    .Take(6)
                    .ToListAsync();

                return View(featuredDrugs);
            }
            catch (Exception ex)
            {
                // Thực tế bạn có thể dùng ILogger để ghi log: _logger.LogError(ex, "Lỗi tải danh sách thuốc");
                ViewBag.ErrorMessage = "Đã xảy ra lỗi khi tải danh sách thuốc.";
                return View(new List<Drug>());
            }
        }

        /// <summary>
        /// API endpoint: Tìm kiếm gợi ý thuốc (Autocomplete) phục vụ cho Alpine.js
        /// URL: /Home/SearchDrugs?term=...
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchDrugs(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>());

            var normalizedTerm = term.ToLower().Trim();

            // Đã bổ sung OrderBy để khắc phục cảnh báo EF Core Query 10102
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
        /// API endpoint: Tìm kiếm gợi ý bệnh lý (Autocomplete) phục vụ cho Alpine.js
        /// URL: /Home/SearchDiseases?term=...
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchDiseases(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>());

            var normalizedTerm = term.ToLower().Trim();

            // Đã bổ sung OrderBy để khắc phục cảnh báo EF Core Query 10102
            var diseases = await _context.Set<Disease>()
                .Where(d => d.IsActive && d.Name.ToLower().Contains(normalizedTerm))
                .OrderBy(d => d.Name)
                .Select(d => new { d.Id, d.Name })
                .Take(10)
                .ToListAsync();

            return Json(diseases);
        }

        /// <summary>
        /// API endpoint: Kiểm tra tương tác thuốc-thuốc và thuốc-bệnh lý bằng dữ liệu thực tế từ DB
        /// URL: /Home/CheckInteractions (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken] // Bảo mật CSRF token từ Fetch API gửi lên
        public async Task<IActionResult> CheckInteractions([FromBody] InteractionCheckRequest request)
        {
            if (request == null || ((request.DrugIds == null || !request.DrugIds.Any()) && 
                                    (request.DiseaseIds == null || !request.DiseaseIds.Any())))
            {
                return BadRequest(new { message = "Dữ liệu yêu cầu không hợp lệ." });
            }

            var drugDrugInteractionsResult = new List<object>();
            var drugDiseaseContraindicationsResult = new List<object>();

            // 1. Lưu lịch sử tìm kiếm vào DB
            await SaveSearchHistoryAsync(request);

            // 2. Kiểm tra tương tác THUỐC - THUỐC
            if (request.DrugIds != null && request.DrugIds.Count >= 2)
            {
                var interactions = await _context.Set<DrugInteraction>()
                    .Include(di => di.SourceDrug)
                    .Include(di => di.TargetDrug)
                    .Where(di => request.DrugIds.Contains(di.SourceDrugId) && 
                                 request.DrugIds.Contains(di.TargetDrugId) &&
                                 di.SourceDrugId != di.TargetDrugId)
                    .ToListAsync();

                foreach (var item in interactions)
                {
                    string severityText = item.SeverityLevel == 3 ? "NGUY HIỂM" : 
                                          item.SeverityLevel == 2 ? "THẬN TRỌNG" : "NHẸ";
                    
                    string severityClass = item.SeverityLevel == 3 ? "border-red-500 bg-red-50" : 
                                           item.SeverityLevel == 2 ? "border-amber-500 bg-amber-50" : "border-blue-500 bg-blue-50";
                    
                    string icon = item.SeverityLevel == 3 ? "fa-exclamation-triangle" : "fa-exclamation-circle";

                    drugDrugInteractionsResult.Add(new
                    {
                        id = item.Id,
                        level = item.SeverityLevel, // Bổ sung trường số phục vụ Alpine.js :class
                        title = $"{item.SourceDrug?.Name} + {item.TargetDrug?.Name} - {severityText}",
                        description = item.Description,
                        icon = icon,
                        severityClass = severityClass, // Giữ lại để back-up nếu cần dùng
                        recommendation = item.Recommendation
                    });
                }
            }

            // 3. Kiểm tra tương tác THUỐC - BỆNH LÝ
            if (request.DrugIds != null && request.DrugIds.Any() && request.DiseaseIds != null && request.DiseaseIds.Any())
            {
                var contraindications = await _context.Set<DrugDiseaseContraindication>()
                    .Include(ddc => ddc.Drug)
                    .Include(ddc => ddc.Disease)
                    .Where(ddc => request.DrugIds.Contains(ddc.DrugId) && 
                                 request.DiseaseIds.Contains(ddc.DiseaseId))
                    .ToListAsync();

                foreach (var item in contraindications)
                {
                    string riskText = item.RiskLevel == 3 ? "CHỐNG CHỈ ĐỊNH" : 
                                      item.RiskLevel == 2 ? "THẬN TRỌNG" : "THẤP";
                    
                    string severityClass = item.RiskLevel == 3 ? "border-red-500 bg-red-50" : 
                                           item.RiskLevel == 2 ? "border-amber-500 bg-amber-50" : "border-green-500 bg-green-50";
                    
                    string icon = item.RiskLevel == 3 ? "fa-ban" : 
                                  item.RiskLevel == 2 ? "fa-exclamation-circle" : "fa-check-circle";

                    drugDiseaseContraindicationsResult.Add(new
                    {
                        id = item.Id,
                        level = item.RiskLevel, // Bổ sung trường số phục vụ Alpine.js :class
                        title = $"{item.Drug?.Name} + {item.Disease?.Name} - {riskText}",
                        description = $"{item.Warning}. {item.Risk}",
                        icon = icon,
                        severityClass = severityClass, // Giữ lại để back-up nếu cần dùng
                        recommendation = item.Recommendation
                    });
                }
            }

            // Trả về định dạng JSON camelCase đồng bộ hoàn toàn với giao diện Alpine.js
            return Json(new
            {
                drugDrugInteractions = drugDrugInteractionsResult,
                drugDiseaseContraindications = drugDiseaseContraindicationsResult
            });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Hàm bổ trợ: Lưu vết lịch sử tìm kiếm vào Database
        /// </summary>
        private async Task SaveSearchHistoryAsync(InteractionCheckRequest request)
        {
            try
            {
                var histories = new List<SearchHistory>();

                if (request.DrugIds != null)
                {
                    foreach (var drugId in request.DrugIds)
                    {
                        histories.Add(new SearchHistory
                        {
                            DrugId = drugId,
                            SearchType = "InteractionCheck",
                            SearchQuery = $"Kiểm tra tương tác thuốc ID: {drugId}",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                if (histories.Any())
                {
                    _context.Set<SearchHistory>().AddRange(histories);
                    await _context.SaveChangesAsync();
                }
            }
            catch
            {
                // Thực hiện Fail-safe: Lịch sử lỗi không được làm ngắt quãng luồng chính trả thông tin tương tác
            }
        }
    }

    /// <summary>
    /// DTO nhận dữ liệu từ Client gửi lên (JSON body)
    /// </summary>
    public class InteractionCheckRequest
    {
        public List<int> DrugIds { get; set; } = new List<int>();
        public List<int> DiseaseIds { get; set; } = new List<int>();
    }
}