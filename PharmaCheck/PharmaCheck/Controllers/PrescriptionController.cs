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
            .Take(30)
            .Select(d => new { id = d.Id, name = d.Name, ingredient = d.ActiveIngredient })
            .ToListAsync();

        return Json(drugs);
    }

    /// <summary>
    /// API gợi ý tìm kiếm hồ sơ bệnh nhân cũ dựa trên Tên hoặc Số điện thoại (Bổ sung để chạy Autocomplete) 🌟
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPatientsJson(string term = "")
    {
        if (string.IsNullOrEmpty(term) || term.Trim().Length < 2)
        {
            return Json(new List<object>());
        }

        var normalized = term.ToLower().Trim();
        var patients = await _context.Patients
            .Where(p => p.FullName.ToLower().Contains(normalized) || p.PhoneNumber.Contains(normalized))
            .Take(10)
            .Select(p => new
            {
                id = p.Id,
                name = p.FullName,
                phone = p.PhoneNumber ?? "Chưa có",
                gender = p.Gender,
                allergies = p.Allergies ?? "",
                // Tính toán số tuổi dựa trên ngày sinh trong DB để trả về hiển thị
                age = DateTime.UtcNow.Year - p.BirthDate.Year
            })
            .ToListAsync();

        return Json(patients);
    }

    /// <summary>
    /// Xử lý lưu chuỗi dữ liệu phức hợp (Bệnh nhân -> Bệnh án -> Đơn thuốc) gửi từ Client AJAX
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePrescription([FromBody] PrescriptionSubmissionDto dto)
    {
        if (dto == null)
        {
            return Json(new { success = false, message = "Dữ liệu đơn thuốc gửi lên trống." });
        }

        // 1. Kiểm tra tính hợp lệ dữ liệu đầu vào (Validation)
        if (string.IsNullOrEmpty(dto.PatientName?.Trim())) return Json(new { success = false, message = "Vui lòng nhập họ tên bệnh nhân." });
        if (string.IsNullOrEmpty(dto.PhoneNumber?.Trim())) return Json(new { success = false, message = "Vui lòng nhập số điện thoại định danh bệnh nhân." });
        if (string.IsNullOrEmpty(dto.Diagnosis?.Trim())) return Json(new { success = false, message = "Vui lòng nhập chẩn đoán lâm sàng." });
        if (dto.Details == null || !dto.Details.Any()) return Json(new { success = false, message = "Đơn thuốc phải chứa ít nhất một loại biệt dược." });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
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

            // 3. XỬ LÝ LƯU THÔNG TIN BỆNH NHÂN (PATIENT) - ĐÃ ĐƯỢC SỬA LỖI TOÀN DIỆN 🌟
            Patient patient = null;
            var phoneNorm = dto.PhoneNumber.Trim();

            // Bước A: Nếu Frontend gửi kèm PatientId cụ thể (Bác sĩ chọn từ Dropdown)
            if (dto.PatientId.HasValue && dto.PatientId.Value > 0)
            {
                patient = await _context.Patients.FindAsync(dto.PatientId.Value);
            }

            // Bước B: Nếu không có ID hoặc không tìm thấy, quét diện rộng bằng Số điện thoại duy nhất
            if (patient == null)
            {
                patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.PhoneNumber == phoneNorm);
            }

            // Bước C: Tính toán và phân tích ngày sinh chi tiết hoặc tuổi quy đổi
            DateTime calculatedBirthDate;
            if (!string.IsNullOrEmpty(dto.BirthDateStr))
            {
                // Nhập Ngày sinh thật: Phân tích định dạng chuỗi "YYYY-MM-DD" từ Frontend gửi sang
                if (!DateTime.TryParse(dto.BirthDateStr, out calculatedBirthDate))
                {
                    return Json(new { success = false, message = "Định dạng Ngày sinh chi tiết không hợp lệ." });
                }
            }
            else
            {
                // Nhập tuổi nhanh: Giữ nguyên cơ chế tính lùi năm mặc định (01/01/XXXX)
                calculatedBirthDate = new DateTime(DateTime.UtcNow.Year - dto.Age, 1, 1);
            }

            if (patient == null)
            {
                // Trường hợp 3.1: Bệnh nhân mới hoàn toàn -> Thêm mới hồ sơ gốc
                patient = new Patient
                {
                    FullName = dto.PatientName.Trim(),
                    BirthDate = calculatedBirthDate,  // Đã sửa: Lưu chuẩn ngày tháng chi tiết 🌟
                    Gender = dto.Gender ?? "Chưa rõ",
                    PhoneNumber = phoneNorm,          // Đã sửa: Lưu Số điện thoại vào DB 🌟
                    Allergies = dto.Allergies?.Trim(),// Đã sửa: Lưu Tiền sử dị ứng thuốc 🌟
                    CreatedAt = DateTime.UtcNow
                };
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync(); // Đồng bộ ngay để sinh mã Patient.Id tự tăng
            }
            else
            {
                // Trường hợp 3.2: Bệnh nhân đã tồn tại -> Cập nhật thông tin hành chính mới nhất
                patient.FullName = dto.PatientName.Trim();
                patient.BirthDate = calculatedBirthDate;
                patient.Gender = dto.Gender ?? "Chưa rõ";
                patient.PhoneNumber = phoneNorm; 
                
                if (!string.IsNullOrEmpty(dto.Allergies))
                {
                    patient.Allergies = dto.Allergies.Trim();
                }

                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();
            }

            // 4. XỬ LÝ TẠO HỒ SƠ BỆNH ÁN LƯỢT KHÁM (MEDICAL RECORD)
            var medicalRecord = new MedicalRecord
            {
                PatientId = patient.Id, // Đảm bảo lấy chính xác Id dù mới hay cũ
                DoctorId = currentUserId,
                Symptoms = dto.Symptoms ?? "Không ghi nhận triệu chứng đặc biệt",
                Diagnosis = dto.Diagnosis.Trim(),
                ExaminedAt = DateTime.UtcNow
            };
            _context.MedicalRecords.Add(medicalRecord);
            await _context.SaveChangesAsync();

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

            // 6. Hoàn tất chuỗi ghi dữ liệu an toàn vào Database thông qua Transaction
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
            await transaction.RollbackAsync(); // Hủy bỏ toàn bộ thao tác nếu có bất kỳ lỗi phát sinh
            return Json(new { success = false, message = $"Lỗi hệ thống khi đồng bộ bệnh án: {ex.Message}" });
        }
    }
}

/// <summary>
/// Đối tượng vận chuyển dữ liệu (DTO) ánh xạ chính xác với cấu trúc JSON từ Javascript gửi lên
/// </summary>
public class PrescriptionSubmissionDto
{
    public int? PatientId { get; set; } 
    public string PatientName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty; 
    public int Age { get; set; }
    public string? BirthDateStr { get; set; } 
    public string? Gender { get; set; }
    public string? Allergies { get; set; } 
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