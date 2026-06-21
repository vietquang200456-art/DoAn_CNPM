using System.Threading.Tasks;
using PharmaCheck.Models;

namespace PharmaCheck.Services;

public interface IDrugAiService
{
    Task<AiPredictionResult> PredictInteractionAsync(string drugA, string drugB);
}