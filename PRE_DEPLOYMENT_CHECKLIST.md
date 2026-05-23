# 🔍 Danh Sách Kiểm Tra Trước Khi Deploy (Pre-Deployment Checklist)

## ✅ Yêu Cầu Hệ Thống
- [ ] .NET 8.0+ hoặc version bạn đang dùng
- [ ] SQL Server (hoặc database được cấu hình trong appsettings.json)
- [ ] Visual Studio 2022 / VS Code + .NET CLI
- [ ] Node.js (nếu có Tailwind CSS build process)

---

## 📋 Code Review

### DrugController.cs
- [ ] Tất cả method có comment rõ ràng
- [ ] Exception handling đúng cách
- [ ] Không có hardcoded connection strings
- [ ] PageSize là constant hoặc config

### Drug/Index.cshtml
- [ ] Model binding chính xác: `@model DrugPagedListViewModel`
- [ ] Tất cả foreach loop kiểm tra null
- [ ] JavaScript không có syntax errors
- [ ] Form ID và field ID khớp với JavaScript

### JavaScript
- [ ] Kiểm tra console.log() debug code, xóa nếu cần
- [ ] AJAX error handling đầy đủ
- [ ] Debounce tìm kiếm hoạt động
- [ ] Modal close/open logic không bị stuck

---

## 🗄️ Database

### Migrations
- [ ] Migrations đã apply lên database
```bash
dotnet ef migrations add [MigrationName]
dotnet ef database update
```

### Test Data
- [ ] Có ít nhất 5 bản ghi test
- [ ] Hoặc chạy: `TEST_DATA_INSERT.sql`

### Indexes (Optional)
Nếu cần performance tốt hơn:
```sql
CREATE INDEX idx_drug_name ON Drugs(Name);
CREATE INDEX idx_drug_status ON Drugs(IsActive);
```

---

## 🌐 Configuration

### appsettings.json
- [ ] Connection string chính xác
- [ ] Không bao giờ commit password/secrets
- [ ] Dùng User Secrets hoặc Environment Variables

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=YOUR_DB;User Id=YOUR_USER;Password=YOUR_PASSWORD;"
  }
}
```

### Program.cs
- [ ] DbContext đã register
- [ ] CORS cấu hình (nếu có frontend riêng)
- [ ] Authentication/Authorization setup

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

---

## 🧪 Testing

### Unit Testing (Local)

#### Test Thêm Mới
```csharp
[Test]
public async Task SaveDrug_WithValidData_ShouldAddNewDrug()
{
    var drug = new Drug { Id = 0, Name = "Test Drug", IsActive = true };
    var result = await controller.SaveDrug(drug) as JsonResult;
    Assert.IsTrue(result.Value.success);
}
```

#### Test Tìm Kiếm
```csharp
[Test]
public async Task Index_WithSearchTerm_ShouldReturnFilteredDrugs()
{
    var result = await controller.Index(searchTerm: "Amoxicillin", status: "", page: 1);
    Assert.IsNotNull(result);
}
```

#### Test Xóa
```csharp
[Test]
public async Task DeleteDrug_WithValidId_ShouldRemove()
{
    var result = await controller.DeleteDrug(1) as JsonResult;
    Assert.IsTrue(result.Value.success);
}
```

### Manual Testing (Browser)

#### Quy Trình
1. **Add New Drug**
   - [ ] Click "Thêm Thuốc Mới"
   - [ ] Điền form
   - [ ] Kiểm tra DB

2. **Edit Drug**
   - [ ] Click sửa
   - [ ] Data load chính xác?
   - [ ] Sửa xong kiểm tra DB

3. **Delete Drug**
   - [ ] Click xóa
   - [ ] Xác nhận
   - [ ] Kiểm tra DB

4. **Search**
   - [ ] Tìm kiếm theo tên
   - [ ] Tìm kiếm theo hoạt chất
   - [ ] Lọc theo trạng thái

5. **Pagination**
   - [ ] Nhấp qua các trang
   - [ ] Kiểm tra số lượng bản ghi

---

## 📊 Performance Checklist

- [ ] Query không N+1
- [ ] Đã sử dụng `.Skip().Take()` cho phân trang
- [ ] Assets (CSS, JS) tối ưu hóa
- [ ] Tailwind CSS production build
- [ ] Images/Media compressed

---

## 🔒 Security Checklist

- [ ] CSRF protection bật
- [ ] Input validation (client + server)
- [ ] SQL Injection: sử dụng Parameterized Queries
- [ ] XSS Prevention: HTML encoding trong View
- [ ] No hardcoded credentials
- [ ] API rate limiting (optional)

---

## 🚀 Production Deployment

### Bước 1: Build
```bash
dotnet build -c Release
```

### Bước 2: Publish
```bash
dotnet publish -c Release -o ./publish
```

### Bước 3: Deploy
- Thay tệp ứng dụng trên server
- Update appsettings.production.json
- Restart application service

### Bước 4: Health Check
```bash
# Test API endpoints
curl https://yourserver.com/Drug/Index
```

---

## 🆘 Troubleshooting

### "DbContext not registered"
**Fix:** Thêm vào Program.cs
```csharp
builder.Services.AddDbContext<ApplicationDbContext>();
```

### "Connection timeout"
**Fix:** Kiểm tra connection string, firewall, SQL Server status

### "Model validation failed"
**Fix:** Kiểm tra ModelState.Errors trong controller

### "CORS issue"
**Fix:** Thêm CORS middleware
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});
```

### "JavaScript not working"
**Fix:** Kiểm trap console.log (F12), xem Network tab

---

## 📝 Logging & Monitoring

### Cơ bản
```csharp
_logger.LogInformation("Drug added: {DrugId}", drug.Id);
_logger.LogError(ex, "Error deleting drug: {DrugId}", id);
```

### Production
Cân nhắc sử dụng:
- Application Insights
- Serilog
- ELK Stack

---

## ✨ Tính Năng Mở Rộng (Lần Sau)

- [ ] Authentication (Login/Role-based access)
- [ ] Audit trail (Ghi lại ai làm gì lúc mấy giờ)
- [ ] Export/Import Excel
- [ ] Batch operations
- [ ] Advanced filtering
- [ ] API versioning
- [ ] Documentation (Swagger)

---

## 📞 Liên Hệ Support

Nếu gặp vấn đề:

1. **Kiểm tra:**
   - Event log
   - Application console
   - Browser console (F12)

2. **Debug:**
   - Thêm breakpoint
   - Trace execution
   - Kiểm tra database directly

3. **Report:**
   - Ghi lại exact error
   - Các steps để reproduce
   - Environment details

---

**Created: 2026-05-24**
**Last Updated: 2026-05-24**
