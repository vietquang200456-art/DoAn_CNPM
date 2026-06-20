using Microsoft.ML.Data;
using System.ComponentModel.DataAnnotations;

namespace PharmaCheck.Models
{
    // Class đại diện cho dữ liệu đầu vào cho mô hình AI dự đoán mức độ nghiêm trọng của tương tác thuốc
    public class DrugAiInput
{
    [LoadColumn(0)]
    public string SourceDrugName { get; set; } = string.Empty;

    [LoadColumn(1)]
    public string TargetDrugName { get; set; } = string.Empty;

    // Nhãn kết quả để AI học (Mức độ 1-5). ML.NET Multiclass yêu cầu kiểu dữ liệu dạng số nguyên hoặc chuỗi
    [LoadColumn(2), ColumnName("Label")]
    public uint SeverityLevel { get; set; } 
}
    // Class đại diện cho kết quả dự đoán từ mô hình AI
    public class DrugAiPrediction
{
    // Cấp độ tương tác dự đoán được (1 -> 5)
    [ColumnName("PredictedLabel")]
    public uint PredictedSeverity { get; set; }

    // Xác suất phân bổ điểm số của các nhãn (dùng để kiểm tra độ tự tin của AI)
    public float[]? Score { get; set; }
}
}