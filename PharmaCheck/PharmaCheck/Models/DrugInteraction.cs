using System;

namespace PharmaCheck.Models
{
    public class DrugInteraction
    {
        public int Id { get; set; } // mã tương tác giữa hai thuốc

        public int SourceDrugId { get; set; } // mã thuốc nguồn (thuốc chính)

        public int TargetDrugId { get; set; } // mã thuốc đích (thuốc tương tác)

        public int SeverityLevel { get; set; } // mức độ nghiêm trọng của tương tác (1-5, 1 là nhẹ nhất, 5 là nghiêm trọng nhất)

        public string Description { get; set; } = string.Empty; // mô tả chi tiết về tương tác (ví dụ: "Sử dụng đồng thời thuốc A và thuốc B có thể làm tăng nguy cơ chảy máu nghiêm trọng")

        public string Recommendation { get; set; } = string.Empty; // khuyến nghị cụ thể cho bệnh nhân (ví dụ: "Nếu bạn đang sử dụng thuốc A, hãy thảo luận với bác sĩ về việc thay đổi liều lượng hoặc lựa chọn thuốc thay thế để giảm nguy cơ tương tác")

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // thời điểm tạo bản ghi
        public DateTime? UpdatedAt { get; set; } // thời điểm cập nhật bản ghi (nếu có)

        // Navigation
        public Drug? SourceDrug { get; set; } // tham chiếu đến thuốc nguồn (thuốc chính)

        public Drug? TargetDrug { get; set; } // tham chiếu đến thuốc đích (thuốc tương tác)
    }
}