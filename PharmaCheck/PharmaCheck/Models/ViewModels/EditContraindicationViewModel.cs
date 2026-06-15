using System.ComponentModel.DataAnnotations;

namespace PharmaCheck.Models.ViewModels
{
    public class EditContraindicationViewModel
    {
        public int Id { get; set; }

        public int DrugId { get; set; }

        public int DiseaseId { get; set; }

        public string DrugName { get; set; } = string.Empty;

        public string DiseaseName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn mức độ rủi ro.")]
        [Range(1, 5, ErrorMessage = "Mức độ rủi ro phải từ 1 đến 5.")]
        public int RiskLevel { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung cảnh báo.")]
        public string Warning { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập thông tin rủi ro.")]
        public string Risk { get; set; } = string.Empty;
    }
}