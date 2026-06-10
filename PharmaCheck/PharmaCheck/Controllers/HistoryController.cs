using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCheck.Data;
using PharmaCheck.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PharmaCheck.Controllers
{
    [Authorize] // Bắt buộc phải đăng nhập mới được vào quản lý lịch sử
    public class HistoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HistoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Hiển thị lịch sử tra cứu phân quyền theo Role dựa trên cấu trúc table mới
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                // Lấy thông tin ID người dùng hiện tại từ phiên đăng nhập
                string currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int? parsedUserId = int.TryParse(currentUserId, out int tempId) ? tempId : (int?)null;

                // Khởi tạo câu truy vấn nạp sẵn tất cả dữ liệu liên quan (Eager Loading)
                var query = _context.SearchHistories
                    .Include(h => h.Drug)
                    .Include(h => h.TargetDrug)
                    .Include(h => h.Disease)
                    .AsQueryable();

                // =============================================================
                // PHÂN QUYỀN TRUY VẤN DỮ LIỆU
                // =============================================================
                if (User.IsInRole("Admin"))
                {
                    // Nếu là Admin: Lấy toàn bộ lịch sử của tất cả người dùng + Nạp thêm thông tin User
                    query = query.Include(h => h.User);
                }
                else
                {
                    // Nếu là User thường: Chỉ lấy lịch sử do chính mình tra cứu
                    query = query.Where(h => h.UserId == parsedUserId);
                }

                // Sắp xếp lịch sử mới nhất lên đầu
                var rawHistories = await query.OrderByDescending(h => h.CreatedAt).ToListAsync();

                // =============================================================
                // ĐỊNH DẠNG DỮ LIỆU ĐỘNG CHO VIEW (Đồng bộ bộ màu Tailwind CSS)
                // =============================================================
                var formattedHistories = rawHistories.Select(h => new {
                    Id = h.Id,
                    UserEmail = h.User?.Email ?? "Khách vãng lai/Ẩn danh",
                    UserName = h.User?.FullName ?? "N/A",
                    SearchType = h.SearchType,
                    SearchQuery = h.SearchQuery,
                    Time = h.CreatedAt.AddHours(7).ToString("dd/MM/yyyy HH:mm:ss"), // Convert sang múi giờ VN chi tiết
                    
                    // Thẻ tag phân loại dạng tra cứu
                    TypeName = h.SearchType switch
                    {
                        "Drug" => "Tra Cứu Thuốc",
                        "Drug-Drug" => "Tương Tác Thuốc - Thuốc",
                        "Drug-Disease" => "Tương Tác Thuốc - Bệnh",
                        _ => "Tra Cứu Hệ Thống"
                    },
                    // Bộ màu tương ứng với loại tag biệt lập
                    BadgeColor = h.SearchType switch
                    {
                        "Drug" => "text-teal-600 bg-teal-50 border-teal-200",
                        "Drug-Drug" => "text-purple-600 bg-purple-50 border-purple-200",
                        "Drug-Disease" => "text-amber-600 bg-amber-50 border-amber-200",
                        _ => "text-slate-600 bg-slate-50 border-slate-200"
                    },
                    // Icon định dạng trực quan cho UI
                    Icon = h.SearchType switch
                    {
                        "Drug" => "fa-pill",
                        "Drug-Drug" => "fa-capsules",
                        "Drug-Disease" => "fa-heartbeat",
                        _ => "fa-search"
                    },

                    // Giữ lại các ID liên kết gốc phòng trường hợp bạn muốn làm nút "Xem lại kết quả nhanh" trên View
                    DrugId = h.DrugId,
                    TargetDrugId = h.TargetDrugId,
                    DiseaseId = h.DiseaseId
                }).ToList<dynamic>();

                ViewBag.HistoryList = formattedHistories;
                return View();
            }
            catch (Exception)
            {
                ViewBag.ErrorMessage = "Đã xảy ra lỗi nghiêm trọng khi tải lịch sử tra cứu.";
                ViewBag.HistoryList = new List<dynamic>();
                return View();
            }
        }

        /// <summary>
        /// Action Xóa: Bảo mật chặt chẽ - Chỉ cho phép Admin thực hiện xóa bản ghi lịch sử
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> Delete(int id)
        {
            var history = await _context.SearchHistories.FindAsync(id);
            
            if (history != null)
            {
                _context.SearchHistories.Remove(history);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}