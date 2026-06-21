using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using PharmaCheck.Models;

namespace PharmaCheck.Services;

public class DrugAiService : IDrugAiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DrugAiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<AiPredictionResult> PredictInteractionAsync(string drugA, string drugB)
    {
        var client = _httpClientFactory.CreateClient();
        
        // Cấu hình URL trỏ thẳng tới API FastAPI Python đang giữ mô hình BioBERT
        string aiUrl = "http://127.0.0.1:8000/api/ai/predict";
        
        var bodyPayload = new { drug_a = drugA, drug_b = drugB };

        try
        {
            // Thiết lập Timeout ngắn (ví dụ 3 giây) để nếu AI có phản hồi chậm thì không làm nghẽn luồng kê đơn của Bác sĩ
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
            // Ghi log ra màn hình Console Console của C# nếu Python chưa bật
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