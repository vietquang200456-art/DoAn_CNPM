namespace PharmaCheck.Models;

public class Patient
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; } // Dùng ngày sinh để tự tính ra Tuổi, không hardcode số tuổi
    public string Gender { get; set; } = string.Empty; // "Nam", "Nữ", "Khác"
    public string PhoneNumber { get; set; } = string.Empty; // Số điện thoại liên hệ
    public string? Allergies { get; set; } // Tiền sử dị ứng thuốc - Cực kỳ quan trọng!
    
    public List<MedicalRecord> MedicalRecords { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Ngày tạo hồ sơ bệnh nhân
}