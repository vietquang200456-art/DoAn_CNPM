using System;

namespace PharmaCheck.Models
{
    public class DrugDiseaseContraindication
    {
        public int Id { get; set; } // mã chống chỉ định giữa thuốc và bệnh

        public int DrugId { get; set; } // mã thuốc

        public int DiseaseId { get; set; } //   mã bệnh

        public int RiskLevel { get; set; } // mức độ rủi ro (1-5, 1 là thấp nhất, 5 là cao nhất)

        public string Warning { get; set; } = string.Empty; // cảnh báo chi tiết về chống chỉ định (ví dụ: "Không nên sử dụng thuốc này nếu bạn bị bệnh tim mạch nặng")

        public string Risk { get; set; } = string.Empty; // mô tả chi tiết về rủi ro (ví dụ: "Sử dụng thuốc này có thể làm tăng nguy cơ biến chứng tim mạch nghiêm trọng ở bệnh nhân bị bệnh tim mạch nặng")

        public string Recommendation { get; set; } = string.Empty; // khuyến nghị cụ thể cho bệnh nhân (ví dụ: "Nếu bạn bị bệnh tim mạch nặng, hãy thảo luận với bác sĩ về các lựa chọn điều trị thay thế và theo dõi chặt chẽ các triệu chứng của bạn")

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // thời điểm tạo bản ghi
        public DateTime? UpdatedAt { get; set; } // thời điểm cập nhật bản ghi (nếu có)

        // Navigation
        public Drug? Drug { get; set; } // tham chiếu đến thuốc

        public Disease? Disease { get; set; } // tham chiếu đến bệnh
    }
}