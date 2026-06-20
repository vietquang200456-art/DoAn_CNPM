using System;
using System.Collections.Generic;

namespace PharmaCheck.Models;

public class Prescription
{
    public int Id { get; set; }
    
    // THAY THẾ TOÀN BỘ THÔNG TIN BỆNH NHÂN BẰNG KHÓA NGOẠI HỒ SƠ BỆNH ÁN ⭐
    // Đơn thuốc này thuộc về lượt khám/bệnh án nào?
    public int MedicalRecordId { get; set; }
    public MedicalRecord? MedicalRecord { get; set; }

    // Người thực hiện cấp phát / tạo đơn thuốc này (Bác sĩ hoặc Dược sĩ duyệt đơn)
    public int UserId { get; set; } 
    public User? User { get; set; } 

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Ngày ra đơn thuốc
    
    public string? Note { get; set; } // Lời dặn dò chung của đơn thuốc (nếu có)

    // Quan hệ: Một đơn chứa nhiều loại thuốc chi tiết
    public List<PrescriptionDetail> Details { get; set; } = new();
}