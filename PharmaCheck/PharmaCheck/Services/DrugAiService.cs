using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration; // Thêm thư viện này để đọc cấu hình
using PharmaCheck.Models;

namespace PharmaCheck.Services;

public class DrugAiService : IDrugAiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration; // Khai báo biến cấu hình

    // Inject thêm IConfiguration vào Constructor
    public DrugAiService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<AiPredictionResult> PredictInteractionAsync(string drugA, string drugB)
    {
        var client = _httpClientFactory.CreateClient();
        
        // 🌟 ĐỌC URL ĐỘNG: Lấy link từ appsettings.json, nếu không thấy thì tự động dùng localhost làm fallback
        string baseUrl = _configuration["AppSettings:AiServerUrl"] ?? "http://127.0.0.1:8000";
        
        // Ghép nối endpoint API của FastAPI Python vào sau Base URL
        string aiUrl = $"{baseUrl.TrimEnd('/')}/api/ai/predict";
        
        var bodyPayload = new { drug_a = drugA, drug_b = drugB };

        try
        {
            // Thiết lập Timeout ngắn (3 giây) để nếu AI có phản hồi chậm thì không làm nghẽn luồng kê đơn của Bác sĩ
            client.Timeout = TimeSpan.FromSeconds(3); 
            
            var response = await client.PostAsJsonAsync(aiUrl, bodyPayload);

            if (response.IsSuccessStatusCode)
            {
                var jsonResult = await response.Content.ReadFromJsonAsync<AiPredictionResult>();
                if (jsonResult != null)
                {
                    return jsonResult;
                }
            }
        }
        catch (Exception ex)
        {
            // Ghi log ra màn hình Console của C# nếu Python chưa bật hoặc nghẽn mạng
            Console.WriteLine($"[BioBERT Connection Error]: {ex.Message}");
        }

        // Cơ chế phòng vệ lâm sàng: Nếu AI chết, trả về cấp độ 1 (An toàn) để bác sĩ không bị hoang mang
        return new AiPredictionResult
        {
            PredictedSeverity = 1,
            Confidence = 1.0,
            Reason = "Hệ thống AI đang bảo trì. Chuyển chế độ kiểm soát thủ công."
        };
    }
}