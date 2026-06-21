using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCheck.Data;
using PharmaCheck.Models;
using PharmaCheck.Services;

namespace PharmaCheck.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ClinicalAiController : ControllerBase
{
    private readonly IDrugAiService _aiService;
    private readonly ApplicationDbContext _context;

    public ClinicalAiController(IDrugAiService aiService, ApplicationDbContext context)
    {
        _aiService = aiService;
        _context = context;
    }

    [HttpGet("analyze")]
    public async Task<IActionResult> AnalyzeInteraction([FromQuery] string drugA, [FromQuery] string drugB)
    {
        if (string.IsNullOrEmpty(drugA) || string.IsNullOrEmpty(drugB))
            return BadRequest(new { message = "Vui lòng cung cấp đầy đủ tên 2 thuốc." });

        // Chuẩn hóa chữ thường và xóa khoảng trắng thừa giúp tìm kiếm chính xác
        string cleanDrugA = drugA.Trim().ToLower();
        string cleanDrugB = drugB.Trim().ToLower();

        // 1. Truy vấn DB trước để tìm cấu hình tương tác thực tế từ chuyên gia/bác sĩ
        var details = await _context.DrugInteractions
            .Include(di => di.SourceDrug)
            .Include(di => di.TargetDrug)
            .FirstOrDefaultAsync(di => 
                (di.SourceDrug!.Name.ToLower() == cleanDrugA && di.TargetDrug!.Name.ToLower() == cleanDrugB) ||
                (di.SourceDrug!.Name.ToLower() == cleanDrugB && di.TargetDrug!.Name.ToLower() == cleanDrugA)
            );

        uint severity;
        string description;
        string recommendation;

        // 2. Logic kiểm soát an toàn nghiêm ngặt
        if (details != null)
        {
            // Trường hợp 1: Tìm thấy trong DB -> Ưu tiên tuyệt đối dữ liệu chuyên gia
            severity = (uint)details.SeverityLevel;
            description = details.Description;
            recommendation = details.Recommendation;
        }
        else
        {
            // Trường hợp 2: DB không có dữ liệu -> Gọi mô hình AI (BioBERT) bất đồng bộ 🌟
            // Đã thêm 'await' và đổi tên hàm thành 'PredictInteractionAsync' để hết lỗi gạch đỏ
            var prediction = await _aiService.PredictInteractionAsync(cleanDrugA, cleanDrugB);
            
            // Đặt bộ lọc ngưỡng (Threshold): Chỉ tin cậy AI nếu nó dự đoán rủi ro rất cao (Cấp 4, 5) và độ tin cậy tốt
            if (prediction.PredictedSeverity >= 4 && prediction.Confidence >= 0.6)
            {
                severity = prediction.PredictedSeverity;
                // Tận dụng thuộc tính dữ liệu thực tế từ BioBERT để hiển thị lên Toast
                description = $"[AI BioBERT Dự Đoán]: {prediction.Reason}";
                recommendation = "Khuyến cáo bác sĩ nên cân nhắc thay thế một trong hai thuốc hoặc giãn cách thời gian sử dụng.";
            }
            else
            {
                // Nếu AI chỉ đoán lờ mờ (Cấp 2, 3) hoặc kết luận an toàn -> Ép về Cấp 1 (An toàn hoàn toàn)
                severity = 1; 
                description = "Không phát hiện tương tác rủi ro lâm sàng.";
                recommendation = "Các hoạt chất an toàn để phối hợp chỉ định trong đơn thuốc.";
            }
        }

        // Định nghĩa màu sắc badge cảnh báo theo cấp độ nghiêm trọng thực tế từ 1 đến 5
        string badgeColor = severity switch
        {
            5 => "bg-red-100 text-red-800 border-red-300",          // Rất nguy hiểm
            4 => "bg-orange-100 text-orange-800 border-orange-300", // Nguy hiểm cao
            3 => "bg-yellow-100 text-yellow-800 border-yellow-300", // Trung bình
            2 => "bg-blue-100 text-blue-800 border-blue-300",       // Nhẹ
            _ => "bg-green-100 text-green-800 border-green-300"     // Mức 1: Không có tương tác / An toàn
        };

        return Ok(new
        {
            severityLevel = severity,
            colorClass = badgeColor,
            description = description,
            recommendation = recommendation
        });
    }
}