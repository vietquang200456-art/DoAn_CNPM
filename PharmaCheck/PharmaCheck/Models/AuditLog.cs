using System;

namespace PharmaCheck.Models;

public class AuditLog
{
    public int Id { get; set; }
    
    // Nội dung hành động (Ví dụ: "Admin 'vietquang' đã cập nhật thông tin thuốc Paracetamol")
    public string Message { get; set; } = string.Empty;
    
    // Thời gian xảy ra hành động
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Loại hành động để hiển thị Icon/Color tương ứng (Ví dụ: "Edit", "Delete", "Create", "System")
    public string ActionType { get; set; } = "System";
    
    // Người thực hiện hành động (nếu có)
    public string? PerformedBy { get; set; }
}