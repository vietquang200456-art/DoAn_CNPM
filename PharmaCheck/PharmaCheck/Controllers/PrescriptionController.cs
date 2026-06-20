using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCheck.Data;
using PharmaCheck.Models;
using PharmaCheck.Services;

namespace PharmaCheck.Controllers;

[Authorize]
[Authorize(Roles = "Admin,Doctor")] // Chỉ Admin và Bác sĩ mới có quyền kê đơn thuốc
public class PrescriptionController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _logService;

    public PrescriptionController(ApplicationDbContext context, IAuditLogService logService)
    {
        _context = context;
        _logService = logService;
    }

    /// <summary>
    /// Hiển thị giao diện tạo đơn thuốc và hồ sơ bệnh án mới
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// API trả về danh sách thuốc hỗ trợ gõ tìm kiếm động trực tiếp từ Client
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDrugsJson(string term = "")
    {
        var query = _context.Drugs.Where(d => d.IsActive).AsQueryable();

        if (!string.IsNullOrEmpty(term))
        {
            var normalized = term.ToLower().Trim();
            query = query.Where(d => d.Name.ToLower().Contains(normalized) || 
                                     d.ActiveIngredient.ToLower().Contains(normalized));
        }

        var drugs = await query
            .OrderBy(d => d.Name)
            .Take(30) // Tăng giới hạn lên một chút để tìm kiếm thoải mái hơn
            .Select(d => new { id = d.Id, name = d.Name, ingredient = d.ActiveIngredient })
            .ToListAsync();

        return Json(drugs);
    }

    /// <summary>
    /// Xử lý lưu chuỗi dữ liệu phức hợp (Bệnh nhân -> Bệnh án -> Đơn thuốc) gửi từ Client AJAX
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePrescription([FromBody] PrescriptionSubmissionDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (dto == null)
            {
                return Json(new { success = false, message = "Dữ liệu đơn thuốc gửi lên trống." });
            }

            // 1. Kiểm tra tính hợp lệ dữ liệu đầu vào (Validation)
            if (string.IsNullOrEmpty(dto.PatientName?.Trim())) return Json(new { success = false, message = "Vui lòng nhập họ tên bệnh nhân." });
            if (dto.Age <= 0 || dto.Age >= 150) return Json(new { success = false, message = "Tuổi bệnh nhân không hợp lệ (Phải từ 1 đến 149)." });
            if (string.IsNullOrEmpty(dto.Diagnosis?.Trim())) return Json(new { success = false, message = "Vui lòng nhập chẩn đoán lâm sàng." });
            if (dto.Details == null || !dto.Details.Any()) return Json(new { success = false, message = "Đơn thuốc phải chứa ít nhất một loại biệt dược." });

            // 2. Xác định ID Bác sĩ đang đăng nhập thao tác hệ thống
            int currentUserId;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int parsedId))
            {
                currentUserId = parsedId;
            }
            else
            {
                var defaultUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
                if (defaultUser != null) currentUserId = defaultUser.Id;
                else return Json(new { success = false, message = "Không thể định danh tài khoản bác sĩ đang thao tác." });
            }

            // 3. XỬ LÝ LƯU THÔNG TIN BỆNH NHÂN (PATIENT)
            // Tìm kiếm xem bệnh nhân đã từng khám ở hệ thống chưa để tránh nhân bản dữ liệu (Trùng Tên & Số điện thoại nếu có)
            var patientNameNorm = dto.PatientName.Trim();
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.FullName.ToLower() == patientNameNorm.ToLower());

            if (patient == null)
            {
                // Nếu chưa có, tiến hành tạo mới hồ sơ gốc cho bệnh nhân này
                // Tính toán năm sinh tượng trưng dựa trên số tuổi hiện tại nhập vào
                var calculatedBirthDate = new DateTime(DateTime.UtcNow.Year - dto.Age, 1, 1);
                
                patient = new Patient
                {
                    FullName = patientNameNorm,
                    BirthDate = calculatedBirthDate,
                    Gender = dto.Gender ?? "Chưa rõ",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync(); // Lưu để lấy PatientId tự tăng
            }

            // 4. XỬ LÝ TẠO HỒ SƠ BỆNH ÁN LƯỢT KHÁM (MEDICAL RECORD)
            var medicalRecord = new MedicalRecord
            {
                PatientId = patient.Id,
                DoctorId = currentUserId,
                Symptoms = dto.Symptoms ?? "Không ghi nhận triệu chứng đặc biệt",
                Diagnosis = dto.Diagnosis.Trim(),
                ExaminedAt = DateTime.UtcNow
            };
            _context.MedicalRecords.Add(medicalRecord);
            await _context.SaveChangesAsync(); // Lưu để lấy MedicalRecordId tự tăng

            // 5. XỬ LÝ RA ĐƠN THUỐC VÀ CHI TIẾT ĐƠN (PRESCRIPTION)
            var prescription = new Prescription
            {
                MedicalRecordId = medicalRecord.Id,
                UserId = currentUserId,
                CreatedAt = DateTime.UtcNow,
                Note = dto.Note,
                Details = dto.Details.Select(d => new PrescriptionDetail
                {
                    DrugId = d.DrugId,
                    Quantity = d.Quantity,
                    UsageInstruction = d.UsageInstruction
                }).ToList()
            };
            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            // 6. Hoàn tất chuỗi ghi dữ liệu an toàn vào Database
            await transaction.CommitAsync();

            // 7. Ghi nhận nhật ký hệ thống (Audit Log)
            string username = User.Identity?.Name ?? "Bác sĩ";
            await _logService.LogAsync(
                message: $"Bác sĩ '{username}' đã lập bệnh án và kê đơn thuốc thành công cho BN: {patient.FullName} (Mã BA: #{medicalRecord.Id})",
                actionType: "Create",
                username: username
            );

            return Json(new { 
                success = true, 
                message = "Khởi tạo hồ sơ bệnh án và đơn thuốc lâm sàng thành công!", 
                prescriptionId = prescription.Id,
                medicalRecordId = medicalRecord.Id
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(); // Thu hồi toàn bộ lệnh thêm sửa nếu dính lỗi giữa chừng
            return Json(new { success = false, message = $"Lỗi hệ thống khi đồng bộ bệnh án: {ex.Message}" });
        }
    }
}

/// <summary>
/// Đối tượng vận chuyển dữ liệu (DTO) ánh xạ chính xác với cấu trúc JSON từ Javascript gửi lên
/// </summary>
public class PrescriptionSubmissionDto
{
    public string PatientName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string? Gender { get; set; }
    public string? Symptoms { get; set; }
    public string Diagnosis { get; set; } = string.Empty;
    public string? Note { get; set; }
    public List<PrescriptionDetailDto> Details { get; set; } = new();
}

public class PrescriptionDetailDto
{
    public int DrugId { get; set; }
    public string Quantity { get; set; } = string.Empty;
    public string UsageInstruction { get; set; } = string.Empty;
}