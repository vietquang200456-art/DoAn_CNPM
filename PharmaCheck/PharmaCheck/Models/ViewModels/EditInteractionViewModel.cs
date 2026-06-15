using System.ComponentModel.DataAnnotations;

namespace PharmaCheck.Models.ViewModels
{
    public class EditInteractionViewModel
    {
        public int Id { get; set; }

        public int SourceDrugId { get; set; }

        public int TargetDrugId { get; set; }

        public string SourceDrugName { get; set; } = string.Empty;

        public string TargetDrugName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn mức độ nghiêm trọng.")]
        [Range(1, 5, ErrorMessage = "Mức độ nghiêm trọng phải từ 1 đến 5.")]
        public int SeverityLevel { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mô tả hoặc cơ chế tương tác.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập khuyến nghị lâm sàng.")]
        public string Recommendation { get; set; } = string.Empty;
    }
}