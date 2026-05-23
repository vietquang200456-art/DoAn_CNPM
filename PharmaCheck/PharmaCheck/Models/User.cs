using System;
using System.Collections.Generic;

namespace PharmaCheck.Models
{
    public class User
    {
        public int Id { get; set; } // mã người dùng

        public string Username { get; set; } = string.Empty; // tên đăng nhập (unique)

        public string Email { get; set; } = string.Empty; // địa chỉ email (unique)

        public string PasswordHash { get; set; } = string.Empty; // mật khẩu đã được băm (hash)

        public string FullName { get; set; } = string.Empty; // họ và tên đầy đủ của người dùng

        public string Role { get; set; } = "User"; // vai trò của người dùng (ví dụ: "User", "Admin")

        public bool IsActive { get; set; } = true; // trạng thái hoạt động của người dùng (true: hoạt động, false: bị khóa)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // thời điểm tạo tài khoản người dùng    

        public DateTime? UpdatedAt { get; set; } // thời điểm cập nhật thông tin người dùng gần nhất

        public DateTime? LastLoginAt { get; set; } // thời điểm đăng nhập gần nhất của người dùng

        // Navigation
        public ICollection<SearchHistory> SearchHistories { get; set; }
            = new List<SearchHistory>();
    }
}