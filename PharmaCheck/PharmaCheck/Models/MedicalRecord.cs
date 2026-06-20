namespace PharmaCheck.Models;
public class MedicalRecord
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient? Patient { get; set; } // Khóa ngoại liên kết tới Bệnh nhân
    
    public string Diagnosis { get; set; } = string.Empty; // Chẩn đoán bệnh
    public string Symptoms { get; set; } = string.Empty;  // Triệu chứng lâm sàng
    public DateTime ExaminedAt { get; set; } = DateTime.UtcNow;
    
    public int DoctorId { get; set; } // Khóa ngoại liên kết tới bảng User (Bác sĩ)
    
    // Một hồ sơ bệnh án có thể gồm đơn thuốc, và sau này có thể mở rộng thêm kết quả xét nghiệm...
    public List<Prescription> Prescriptions { get; set; } = new();
}