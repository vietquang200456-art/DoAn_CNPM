using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

public class CreateInteractionViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn thuốc nguồn.")]
    public int? SourceDrugId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thuốc tương tác.")]
    public int? TargetDrugId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn mức độ nghiêm trọng.")]
    [Range(1, 5, ErrorMessage = "Mức độ nghiêm trọng phải từ 1 đến 5.")]
    public int? SeverityLevel { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mô tả hoặc cơ chế tương tác.")]
    public string Description { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập khuyến nghị lâm sàng.")]
    public string Recommendation { get; set; }

    // Danh sách hiển thị lên Dropdownlist ở giao diện
    public List<SelectListItem>? DrugList { get; set; } 
}