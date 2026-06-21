namespace PharmaCheck.Models;

public class AiPredictionResult
{
    public uint PredictedSeverity { get; set; }
    public double Confidence { get; set; }
    public string Reason { get; set; } = string.Empty;
}