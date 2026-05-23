using System;

namespace PharmaCheck.Models
{
    public class SearchHistory
    {
        public int Id { get; set; } // mã lịch sử tìm kiếm

        public int? UserId { get; set; } // mã người dùng thực hiện tìm kiếm (có thể null nếu người dùng không đăng nhập)

        public int? DrugId { get; set; } // mã thuốc được tìm kiếm (có thể null nếu tìm kiếm không liên quan đến thuốc cụ thể)

        public string SearchType { get; set; } = string.Empty; // loại tìm kiếm (ví dụ: "Drug", "Disease", "Interaction", "Contraindication")

        public string SearchQuery { get; set; } = string.Empty; // nội dung tìm kiếm (ví dụ: tên thuốc, tên bệnh, từ khóa liên quan đến tương tác hoặc chống chỉ định)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // thời điểm thực hiện tìm kiếm

        // Navigation
        public User? User { get; set; } // tham chiếu đến người dùng thực hiện tìm kiếm (nếu có)

        public Drug? Drug { get; set; } // tham chiếu đến thuốc được tìm kiếm (nếu có)
    }
}