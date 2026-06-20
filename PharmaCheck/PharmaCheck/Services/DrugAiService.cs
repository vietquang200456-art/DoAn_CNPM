using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ML;
using PharmaCheck.Data;
using PharmaCheck.Models;

namespace PharmaCheck.Services;

public interface IDrugAiService
{
    void TrainModelFromDb();
    DrugAiPrediction PredictInteraction(string sourceDrugName, string targetDrugName);
}

public class DrugAiService : IDrugAiService
{
    private readonly MLContext _mlContext;
    private ITransformer? _trainedModel;
    private PredictionEngine<DrugAiInput, DrugAiPrediction>? _predictionEngine;
    private readonly string _modelPath;
    private readonly IServiceProvider _serviceProvider;

    public DrugAiService(IServiceProvider serviceProvider)
    {
        _mlContext = new MLContext(seed: 42); // Khởi tạo môi trường ML với seed cố định
        _modelPath = Path.Combine(AppContext.BaseDirectory, "pharmacheck_ai_model.zip");
        _serviceProvider = serviceProvider;

        // Tải lại model cũ nếu đã từng được train trước đó để tối ưu thời gian khởi động
        if (File.Exists(_modelPath))
        {
            _trainedModel = _mlContext.Model.Load(_modelPath, out _);
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<DrugAiInput, DrugAiPrediction>(_trainedModel);
        }
    }

    /// <summary>
    /// Hàm đọc Database và Huấn luyện (Train) AI từ đầu
    /// </summary>
    public void TrainModelFromDb()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 1. Đọc dữ liệu thực tế từ Database
        var dbData = context.DrugInteractions
            .Include(di => di.SourceDrug)
            .Include(di => di.TargetDrug)
            .AsNoTracking()
            .ToList();

        var trainingDataList = new List<DrugAiInput>();

        if (dbData.Any())
        {
            trainingDataList = dbData.Select(x => new DrugAiInput
            {
                SourceDrugName = x.SourceDrug?.Name?.Trim().ToLower() ?? string.Empty,
                TargetDrugName = x.TargetDrug?.Name?.Trim().ToLower() ?? string.Empty,
                SeverityLevel = (uint)x.SeverityLevel
            }).Where(input => !string.IsNullOrEmpty(input.SourceDrugName) && !string.IsNullOrEmpty(input.TargetDrugName))
              .ToList();
        }

        // 🛡️ ĐOẠN SỬA ĐỔI: Kiểm tra số lượng nhãn phân biệt (Distinct SeverityLevel) 🌟
        var distinctLabelsCount = trainingDataList.Select(x => x.SeverityLevel).Distinct().Count();

        // Nếu số nhóm phân loại nhỏ hơn 2, thêm dữ liệu mồi khác nhóm vào để AI không bị lỗi numClasses
        if (distinctLabelsCount < 2)
        {
            // Giữ lại dòng dữ liệu hiện tại của bạn nếu có
            // Đồng thời chèn thêm 2 mẫu thử nghiệm với 2 mức độ (SeverityLevel) hoàn toàn khác nhau để làm mồi
            trainingDataList.Add(new DrugAiInput { SourceDrugName = "mock_drug_a", TargetDrugName = "mock_drug_b", SeverityLevel = 1 });
            trainingDataList.Add(new DrugAiInput { SourceDrugName = "mock_drug_c", TargetDrugName = "mock_drug_d", SeverityLevel = 5 });
        }

        // 2. Nạp mảng dữ liệu đã được bảo vệ vào ML.NET
        IDataView trainingDataView = _mlContext.Data.LoadFromEnumerable(trainingDataList);

        // 3. Giữ nguyên toàn bộ phần cấu hình Pipeline và Trainer phía dưới...
        var dataProcessPipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label")
            .Append(_mlContext.Transforms.Text.FeaturizeText("SourceDrugFeatures", nameof(DrugAiInput.SourceDrugName)))
            .Append(_mlContext.Transforms.Text.FeaturizeText("TargetDrugFeatures", nameof(DrugAiInput.TargetDrugName)))
            .Append(_mlContext.Transforms.Concatenate("Features", "SourceDrugFeatures", "TargetDrugFeatures"))
            .AppendCacheCheckpoint(_mlContext);

        var trainer = _mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features")
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        var trainingPipeline = dataProcessPipeline.Append(trainer);

        _trainedModel = trainingPipeline.Fit(trainingDataView);
        _mlContext.Model.Save(_trainedModel, trainingDataView.Schema, _modelPath);
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<DrugAiInput, DrugAiPrediction>(_trainedModel);
    }

    /// <summary>
    /// Hàm dự đoán ngầm khi bác sĩ kê đơn
    /// </summary>
    public DrugAiPrediction PredictInteraction(string sourceDrugName, string targetDrugName)
    {
        // Nếu chưa có mô hình được huấn luyện, trả về mặc định an toàn (Mức độ 1)
        if (_predictionEngine == null)
        {
            return new DrugAiPrediction { PredictedSeverity = 1 };
        }

        var input = new DrugAiInput
        {
            SourceDrugName = sourceDrugName.Trim().ToLower(),
            TargetDrugName = targetDrugName.Trim().ToLower()
        };

        return _predictionEngine.Predict(input);
    }
}