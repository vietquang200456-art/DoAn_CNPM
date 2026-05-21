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
            catch (Exception)
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
        public async Task<IActionResult> CheckInteractions([FromBody] InteractionCheckRequest request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine("🔍 [CheckInteractions] REQUEST NHẬN ĐƯỢC");
                System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"   Request object: {(request == null ? "NULL" : "OK")}");
                
                if (request != null)
                {
                    System.Diagnostics.Debug.WriteLine($"   DrugIds count: {request.DrugIds?.Count ?? 0}");
                    System.Diagnostics.Debug.WriteLine($"   DrugIds: [{string.Join(", ", request.DrugIds ?? new List<int>())}]");
                    System.Diagnostics.Debug.WriteLine($"   DiseaseIds count: {request.DiseaseIds?.Count ?? 0}");
                    System.Diagnostics.Debug.WriteLine($"   DiseaseIds: [{string.Join(", ", request.DiseaseIds ?? new List<int>())}]");
                }

                // Kiểm tra Model State
                if (!ModelState.IsValid)
                {
                    System.Diagnostics.Debug.WriteLine("❌ ModelState KHÔNG HỢP LỆ!");
                    foreach (var modelState in ModelState.Values)
                    {
                        foreach (var error in modelState.Errors)
                        {
                            System.Diagnostics.Debug.WriteLine($"   Error: {error.ErrorMessage}");
                        }
                    }
                    return BadRequest(new 
                    { 
                        message = "Model State không hợp lệ",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }

                if (request == null || ((request.DrugIds == null || !request.DrugIds.Any()) && 
                                        (request.DiseaseIds == null || !request.DiseaseIds.Any())))
                {
                    System.Diagnostics.Debug.WriteLine("❌ [CheckInteractions] Request null hoặc không có dữ liệu");
                    return BadRequest(new { message = "Dữ liệu yêu cầu không hợp lệ.", error = "EmptyRequest" });
                }

                var drugDrugInteractionsResult = new List<object>();
                var drugDiseaseContraindicationsResult = new List<object>();

                // 1. Lưu lịch sử tìm kiếm vào DB
                await SaveSearchHistoryAsync(request);

                // 2. Kiểm tra tương tác THUỐC - THUỐC
                if (request.DrugIds != null && request.DrugIds.Count >= 2)
                {
                    System.Diagnostics.Debug.WriteLine("🔍 [CheckInteractions] Kiểm tra Drug-Drug Interactions...");
                    
                    var interactions = await _context.Set<DrugInteraction>()
                        .Include(di => di.SourceDrug)
                        .Include(di => di.TargetDrug)
                        .Where(di => request.DrugIds.Contains(di.SourceDrugId) && 
                                     request.DrugIds.Contains(di.TargetDrugId) &&
                                     di.SourceDrugId != di.TargetDrugId)
                        .ToListAsync();

                    System.Diagnostics.Debug.WriteLine($"   📊 Tìm thấy {interactions.Count} interactions");

                    foreach (var item in interactions)
                    {
                        string severityText = item.SeverityLevel == 3 ? "NGUY HIỂM" : 
                                              item.SeverityLevel == 2 ? "THẬN TRỌNG" : "NHẸ";
                        
                        string icon = item.SeverityLevel == 3 ? "fa-exclamation-triangle" : "fa-exclamation-circle";

                        drugDrugInteractionsResult.Add(new
                        {
                            id = item.Id,
                            level = item.SeverityLevel,
                            title = $"{item.SourceDrug?.Name} + {item.TargetDrug?.Name} - {severityText}",
                            description = item.Description,
                            icon = icon,
                            recommendation = item.Recommendation
                        });
                    }
                }

                // 3. Kiểm tra tương tác THUỐC - BỆNH LÝ
                if (request.DrugIds != null && request.DrugIds.Any() && request.DiseaseIds != null && request.DiseaseIds.Any())
                {
                    System.Diagnostics.Debug.WriteLine("🔍 [CheckInteractions] Kiểm tra Drug-Disease Contraindications...");
                    
                    var contraindications = await _context.Set<DrugDiseaseContraindication>()
                        .Include(ddc => ddc.Drug)
                        .Include(ddc => ddc.Disease)
                        .Where(ddc => request.DrugIds.Contains(ddc.DrugId) && 
                                     request.DiseaseIds.Contains(ddc.DiseaseId))
                        .ToListAsync();

                    System.Diagnostics.Debug.WriteLine($"   📊 Tìm thấy {contraindications.Count} contraindications");

                    foreach (var item in contraindications)
                    {
                        string riskText = item.RiskLevel == 3 ? "CHỐNG CHỈ ĐỊNH" : 
                                          item.RiskLevel == 2 ? "THẬN TRỌNG" : "THẤP";
                        
                        string icon = item.RiskLevel == 3 ? "fa-ban" : 
                                      item.RiskLevel == 2 ? "fa-exclamation-circle" : "fa-check-circle";

                        drugDiseaseContraindicationsResult.Add(new
                        {
                            id = item.Id,
                            level = item.RiskLevel,
                            title = $"{item.Drug?.Name} + {item.Disease?.Name} - {riskText}",
                            description = $"{item.Warning}. {item.Risk}",
                            icon = icon,
                            recommendation = item.Recommendation
                        });
                    }
                }

                System.Diagnostics.Debug.WriteLine($"✅ [CheckInteractions] Trả về {drugDrugInteractionsResult.Count} drug-drug, {drugDiseaseContraindicationsResult.Count} drug-disease");
                System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════");

                // Trả về định dạng JSON camelCase đồng bộ hoàn toàn với giao diện Alpine.js
                return Json(new
                {
                    drugDrugInteractions = drugDrugInteractionsResult,
                    drugDiseaseContraindications = drugDiseaseContraindicationsResult
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ [CheckInteractions] Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack Trace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════");
                
                return StatusCode(500, new 
                { 
                    message = "Lỗi xử lý dữ liệu trên máy chủ.",
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// DEBUG ENDPOINT: Kiểm tra dữ liệu trong database
        /// URL: /Home/DiagnosticData
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DiagnosticData()
        {
            try
            {
                var drugCount = await _context.Set<Drug>().CountAsync();
                var diseaseCount = await _context.Set<Disease>().CountAsync();
                var interactionCount = await _context.Set<DrugInteraction>().CountAsync();
                var contraindicationCount = await _context.Set<DrugDiseaseContraindication>().CountAsync();

                var drugs = await _context.Set<Drug>().Take(5).ToListAsync();
                var diseases = await _context.Set<Disease>().Take(5).ToListAsync();
                var interactions = await _context.Set<DrugInteraction>()
                    .Include(di => di.SourceDrug)
                    .Include(di => di.TargetDrug)
                    .Take(3)
                    .ToListAsync();
                var contraindications = await _context.Set<DrugDiseaseContraindication>()
                    .Include(ddc => ddc.Drug)
                    .Include(ddc => ddc.Disease)
                    .Take(3)
                    .ToListAsync();

                return Json(new
                {
                    summary = new
                    {
                        totalDrugs = drugCount,
                        totalDiseases = diseaseCount,
                        totalDrugInteractions = interactionCount,
                        totalContraindications = contraindicationCount
                    },
                    sampleDrugs = drugs.Select(d => new { d.Id, d.Name, d.IsActive }).ToList(),
                    sampleDiseases = diseases.Select(d => new { d.Id, d.Name, d.IsActive }).ToList(),
                    sampleInteractions = interactions.Select(di => new
                    {
                        di.Id,
                        SourceDrugId = di.SourceDrugId,
                        SourceDrugName = di.SourceDrug?.Name,
                        TargetDrugId = di.TargetDrugId,
                        TargetDrugName = di.TargetDrug?.Name,
                        di.SeverityLevel
                    }).ToList(),
                    sampleContraindications = contraindications.Select(ddc => new
                    {
                        ddc.Id,
                        DrugId = ddc.DrugId,
                        DrugName = ddc.Drug?.Name,
                        DiseaseId = ddc.DiseaseId,
                        DiseaseName = ddc.Disease?.Name,
                        ddc.RiskLevel
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Lỗi kiểm tra database",
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
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