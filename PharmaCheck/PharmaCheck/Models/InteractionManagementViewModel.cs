using System;
using System.Collections.Generic;
// Bắt buộc phải có thư viện này để sử dụng được SelectListItem cho thẻ dropdown select
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PharmaCheck.Models
{
    /// <summary>
    /// ViewModel để hiển thị dữ liệu tương tác thuốc và chống chỉ định thuốc-bệnh
    /// Được truyền từ Controller sang View để render giao diện
    /// </summary>
    public class InteractionManagementViewModel
    {
        /// <summary>
        /// Danh sách các tương tác giữa hai thuốc hiện có
        /// </summary>
        public List<DrugInteractionDisplayDto> DrugInteractions { get; set; } = new List<DrugInteractionDisplayDto>();

        /// <summary>
        /// Danh sách các chống chỉ định giữa thuốc và bệnh hiện có
        /// </summary>
        public List<DrugDiseaseContraindicationDisplayDto> DrugDiseaseContraindications { get; set; } 
            = new List<DrugDiseaseContraindicationDisplayDto>();

        // ===================================================================
        // ===== DANH SÁCH DỮ LIỆU BỔ SUNG ĐỂ ĐỔ VÀO DROPDOWN TRÊN MODAL =====
        // ===================================================================

        /// <summary>
        /// Danh sách toàn bộ Thuốc trong hệ thống dùng cho việc lựa chọn ở Modal Thêm/Sửa
        /// </summary>
        public IEnumerable<SelectListItem> AvailableDrugs { get; set; } = new List<SelectListItem>();

        /// <summary>
        /// Danh sách toàn bộ Bệnh/Hội chứng dùng cho việc lựa chọn ở Modal Thêm/Sửa
        /// </summary>
        public IEnumerable<SelectListItem> AvailableDiseases { get; set; } = new List<SelectListItem>();

        // ===================================================================
        // ===== Tham số tìm kiếm, lọc và phân trang cho Tương tác Thuốc =====
        // ===================================================================
        public string? InteractionSearchTerm { get; set; }
        public int? InteractionSeverityLevel { get; set; }
        public int InteractionCurrentPage { get; set; } = 1;
        public int InteractionTotalPages { get; set; } = 1;
        public int InteractionTotalRecords { get; set; } = 0;

        // ===================================================================
        // ===== Tham số tìm kiếm, lọc và phân trang cho Chống chỉ định =====
        // ===================================================================
        public string? ContraindicationSearchTerm { get; set; }
        public int? ContraindicationRiskLevel { get; set; }
        public int ContraindicationCurrentPage { get; set; } = 1;
        public int ContraindicationTotalPages { get; set; } = 1;
        public int ContraindicationTotalRecords { get; set; } = 0;
    }

    /// <summary>
    /// DTO để hiển thị thông tin chi tiết tương tác thuốc
    /// Bao gồm tên của thuốc nguồn và thuốc đích
    /// </summary>
    public class DrugInteractionDisplayDto
    {
        public int Id { get; set; }
        public int SourceDrugId { get; set; }
        public int TargetDrugId { get; set; }
        public string SourceDrugName { get; set; } = string.Empty;
        public string TargetDrugName { get; set; } = string.Empty;
        public int SeverityLevel { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Lấy tên mức độ nghiêm trọng dễ đọc
        /// </summary>
        public string SeverityLevelName => SeverityLevel switch
        {
            1 => "Nhẹ",
            2 => "Vừa",
            3 => "Trung bình",
            4 => "Nặng",
            5 => "Rất nặng",
            _ => "Không xác định"
        };

        /// <summary>
        /// Lấy class CSS cho badge mức độ nghiêm trọng (Tailwind CSS)
        /// </summary>
        public string SeverityBadgeClass => SeverityLevel switch
        {
            1 => "bg-green-100 text-green-800",
            2 => "bg-blue-100 text-blue-800",
            3 => "bg-yellow-100 text-yellow-800",
            4 => "bg-orange-100 text-orange-800",
            5 => "bg-red-100 text-red-800",
            _ => "bg-gray-100 text-gray-800"
        };
    }

    /// <summary>
    /// DTO để hiển thị thông tin chi tiết chống chỉ định thuốc-bệnh
    /// Bao gồm tên thuốc và tên bệnh
    /// </summary>
    public class DrugDiseaseContraindicationDisplayDto
    {
        public int Id { get; set; }
        public int DrugId { get; set; }
        public int DiseaseId { get; set; }
        public string DrugName { get; set; } = string.Empty;
        public string DiseaseName { get; set; } = string.Empty;
        public int RiskLevel { get; set; }
        public string Warning { get; set; } = string.Empty;
        public string Risk { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Lấy tên mức độ rủi ro dễ đọc
        /// </summary>
        public string RiskLevelName => RiskLevel switch
        {
            1 => "Thấp",
            2 => "Vừa",
            3 => "Trung bình",
            4 => "Cao",
            5 => "Rất cao",
            _ => "Không xác định"
        };

        /// <summary>
        /// Lấy class CSS cho badge mức độ rủi ro (Tailwind CSS)
        /// </summary>
        public string RiskBadgeClass => RiskLevel switch
        {
            1 => "bg-green-100 text-green-800",
            2 => "bg-blue-100 text-blue-800",
            3 => "bg-yellow-100 text-yellow-800",
            4 => "bg-orange-100 text-orange-800",
            5 => "bg-red-100 text-red-800",
            _ => "bg-gray-100 text-gray-800"
        };
    }
}