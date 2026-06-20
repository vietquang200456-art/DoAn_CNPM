using System;
using System.Collections.Generic;

namespace PharmaCheck.Models;

public class Role
{
    public int Id { get; set; }

    // Tên vai trò: "Admin", "Doctor", "Pharmacist" (Dược sĩ)
    public string Name { get; set; } = string.Empty;

    // Mô tả vai trò (ví dụ: "Quản trị viên hệ thống", "Bác sĩ khám bệnh")
    public string Description { get; set; } = string.Empty;

    // Mối quan hệ: Một vai trò có thể gán cho nhiều Người dùng
    public ICollection<User> Users { get; set; } = new List<User>();
}