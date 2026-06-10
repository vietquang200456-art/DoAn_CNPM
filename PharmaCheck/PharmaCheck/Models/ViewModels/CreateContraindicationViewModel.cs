using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

public class CreateContraindicationViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn thuốc cảnh báo.")]
    public int? DrugId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn bệnh lý chống chỉ định.")]
    public int? DiseaseId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn mức độ rủi ro.")]
    [Range(1, 5, ErrorMessage = "Mức độ rủi ro phải từ 1 đến 5.")]
    public int? RiskLevel { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề cảnh báo.")]
    public string Warning { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập chi tiết rủi ro.")]
    public string Risk { get; set; }

    // Các danh sách hiển thị lên Dropdownlist
    public List<SelectListItem>? DrugList { get; set; }
    public List<SelectListItem>? DiseaseList { get; set; }
}