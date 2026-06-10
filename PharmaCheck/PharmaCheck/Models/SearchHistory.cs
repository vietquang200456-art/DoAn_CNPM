using System;

namespace PharmaCheck.Models;

public class SearchHistory
{
    public int Id { get; set; } // Mã lịch sử tìm kiếm

    public int? UserId { get; set; } // Người thực hiện (Null nếu là khách vãng lai)

    /// <summary>
    /// Loại tra cứu: "Drug", "Disease", "Drug-Drug", "Drug-Disease"
    /// Nên dùng một bộ Constant hoặc Enum để đồng bộ (Ví dụ: SearchTypeConstants.DrugDrug)
    /// </summary>
    public string SearchType { get; set; } = string.Empty; 

    /// <summary>
    /// Chuỗi văn bản thô mà người dùng gõ trên thanh tìm kiếm
    /// </summary>
    public string SearchQuery { get; set; } = string.Empty; 

    // =============================================================
    // CÁC TRƯỜNG ĐỊNH DANH ĐỂ THỐNG KÊ (BIẾN LIÊN KẾT)
    // =============================================================

    public int? DrugId { get; set; } // Mã thuốc thứ nhất (Hoặc thuốc đơn lẻ)

    /// <summary>
    /// BỔ SUNG: Mã thuốc thứ hai (Dành cho tra cứu tương tác Thuốc - Thuốc)
    /// </summary>
    public int? TargetDrugId { get; set; } 

    /// <summary>
    /// BỔ SUNG: Mã bệnh lý (Dành cho tra cứu tương tác Thuốc - Bệnh / Chống chỉ định)
    /// </summary>
    public int? DiseaseId { get; set; } 

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Thời điểm tìm kiếm

    // =============================================================
    // NAVIGATION PROPERTIES (LIÊN KẾT CƠ SỞ DỮ LIỆU)
    // =============================================================
    
    public User? User { get; set; }
    
    public Drug? Drug { get; set; }

    /// <summary>
    /// Tham chiếu đến thuốc thứ hai trong cặp tương tác Thuốc - Thuốc
    /// </summary>
    public Drug? TargetDrug { get; set; }

    /// <summary>
    /// Tham chiếu đến bệnh trong cặp tương tác Thuốc - Bệnh
    /// </summary>
    public Disease? Disease { get; set; }
}