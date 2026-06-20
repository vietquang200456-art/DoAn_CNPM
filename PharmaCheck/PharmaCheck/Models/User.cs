using System;
using System.Collections.Generic;

namespace PharmaCheck.Models;

public class User
{
    public int Id { get; set; } 

    public string Username { get; set; } = string.Empty; 

    public string Email { get; set; } = string.Empty; 

    public string PasswordHash { get; set; } = string.Empty; 

    public string FullName { get; set; } = string.Empty; 

    // THAY ĐỔI: Sử dụng Khóa ngoại liên kết tới bảng Role ⭐
    public int RoleId { get; set; } 
    public Role? Role { get; set; } // Navigation property

    public bool IsActive { get; set; } = true; 

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;    

    public DateTime? UpdatedAt { get; set; } 

    public DateTime? LastLoginAt { get; set; } 
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }

    // Navigation
    public ICollection<SearchHistory> SearchHistories { get; set; } = new List<SearchHistory>();
}