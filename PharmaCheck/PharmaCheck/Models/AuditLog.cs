using System.ComponentModel.DataAnnotations;

namespace PharmaCheck.Models;

public class AuditLog
{
    public int Id { get; set; }
    
    [Required]
    public string Message { get; set; } = string.Empty;
    
    // Lưu trữ chuẩn UTC để tránh sai lệch múi giờ
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public string ActionType { get; set; } = "System"; // "Edit", "Delete", "Create", "System"
    
    public string? PerformedBy { get; set; }

    // Helper giúp hiển thị giờ Việt Nam ở View
    public string GetLocalTime() 
    {
        return CreatedAt.AddHours(7).ToString("dd/MM/yyyy HH:mm:ss");
    }
}