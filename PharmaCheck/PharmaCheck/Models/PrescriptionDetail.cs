
using System;

namespace PharmaCheck.Models;

public class PrescriptionDetail
{
    public int Id { get; set; }
    
    // Liên kết ngược về đơn thuốc tổng
    public int PrescriptionId { get; set; }
    public Prescription? Prescription { get; set; }
    
    // Liên kết sang danh mục thuốc nền của hệ thống
    public int DrugId { get; set; }
    public Drug? Drug { get; set; } 
    
    public string Quantity { get; set; } = string.Empty; // Số lượng (Ví dụ: "20 viên", "2 lọ")
    public string UsageInstruction { get; set; } = string.Empty; // Cách dùng cụ thể (Ví dụ: "Sáng 1 viên, tối 1 viên sau ăn")
}