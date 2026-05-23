# 🚀 Quick Start Guide - Disease Management (Quản Lý Bệnh Lý)

## 📋 Files Đã Tạo

| File | Mục Đích | Dòng Code |
|------|---------|----------|
| `Models/DiseasePagedListViewModel.cs` | ViewModel phân trang | ~25 |
| `Controllers/DiseaseController.cs` | Backend CRUD API | ~210 |
| `Views/Disease/Index.cshtml` | Giao diện Razor | ~350+ |
| `DISEASE_MANAGEMENT_GUIDE.md` | Tài liệu chi tiết | ~ |
| `TEST_DATA_DISEASE_INSERT.sql` | Test data (10 bệnh) | ~80 |

---

## ⚡ 5 Bước Setup Nhanh

### 1️⃣ Tạo/Update Database
```bash
# Nếu Database chưa có Diseases table
cd d:\DoAn_CNPM\PharmaCheck\PharmaCheck

# Add migration (nếu cần)
dotnet ef migrations add AddDiseaseManagement

# Update database
dotnet ef database update
```

### 2️⃣ Thêm Test Data (Optional)
```sql
-- Chạy script này trong SQL Server Management Studio
-- File: TEST_DATA_DISEASE_INSERT.sql
```

### 3️⃣ Chạy Ứng Dụng
```bash
dotnet run
```

### 4️⃣ Truy Cập Trang
```
http://localhost:5000/Disease/Index
```

### 5️⃣ Test Chức Năng
- ✅ Click "Thêm Bệnh Lý Mới" để thêm
- ✅ Click ✏️ để sửa
- ✅ Click 🗑️ để xóa
- ✅ Gõ vào ô tìm kiếm để tìm
- ✅ Chọn filter để lọc

---

## 📊 Model Structure

```csharp
public class Disease
{
    public int Id { get; set; }                          // PK
    public string Name { get; set; }                     // Tên (bắt buộc)
    public string Symptoms { get; set; }                 // Triệu chứng
    public string Causes { get; set; }                   // Nguyên nhân
    public string TreatmentMethod { get; set; }          // Điều trị
    public string Description { get; set; }              // Mô tả
    public bool IsActive { get; set; }                   // Trạng thái
    public DateTime CreatedAt { get; set; }              // Ngày tạo
    public DateTime? UpdatedAt { get; set; }             // Ngày sửa
}
```

---

## 🎯 API Endpoints

```
GET    /Disease/Index                           → Danh sách
GET    /Disease/GetDiseaseById?id=1             → Chi tiết (JSON)
POST   /Disease/SaveDisease                     → Thêm/Sửa
POST   /Disease/DeleteDisease?id=1              → Xóa
GET    /Disease/GetDiseasesPartial              → AJAX data
```

---

## 🎨 UI Features

### Layout
- ✅ Header tiêu đề + nút "Thêm Bệnh Lý Mới"
- ✅ Search bar + Filter status
- ✅ Table hiển thị danh sách (STT, Tên, Triệu Chứng, Nguyên Nhân, Điều Trị, Status, Action)
- ✅ Pagination (Previous, Page numbers, Next)
- ✅ Modal Thêm/Sửa (form)
- ✅ Modal Xác nhận xóa

### Styling
- Tailwind CSS responsive
- Color: medical-700 (primary), green/red (status)
- FontAwesome icons
- Smooth transitions & hover effects

---

## ✨ Features Implemented

| Feature | Status | Ghi Chú |
|---------|--------|--------|
| CREATE (Thêm) | ✅ | Modal form, AJAX POST |
| READ (Xem) | ✅ | Danh sách phân trang, chi tiết |
| UPDATE (Sửa) | ✅ | Modal form fill data, AJAX POST |
| DELETE (Xóa) | ✅ | Confirm modal, AJAX DELETE |
| SEARCH | ✅ | Tìm tên, triệu chứng, nguyên nhân |
| FILTER | ✅ | Lọc Active/Inactive |
| PAGINATION | ✅ | Dynamic, responsive |
| VALIDATION | ✅ | Client + Server side |
| ERROR HANDLING | ✅ | User-friendly messages |

---

## 🔄 CRUD Workflow

```
┌─────────────────────────────────────────────────────┐
│                    THÊM BỆNH MỚI                    │
├─────────────────────────────────────────────────────┤
│  1. Click "Thêm Bệnh Lý Mới"                       │
│  2. Modal form mở (Tên, Triệu chứng, ...)          │
│  3. Điền dữ liệu                                   │
│  4. Click "Lưu"                                    │
│  5. AJAX POST → /Disease/SaveDisease               │
│  6. Database insert                                │
│  7. Auto reload trang                              │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│                    SỬA BỆNH LÝ                      │
├─────────────────────────────────────────────────────┤
│  1. Click icon ✏️ (Sửa) trên dòng                  │
│  2. AJAX GET → /Disease/GetDiseaseById             │
│  3. Modal form mở, fill dữ liệu cũ                 │
│  4. Chỉnh sửa                                      │
│  5. Click "Cập Nhật"                               │
│  6. AJAX POST → /Disease/SaveDisease               │
│  7. Database update                                │
│  8. Auto reload trang                              │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│                  XÓA BỆNH LÝ                        │
├─────────────────────────────────────────────────────┤
│  1. Click icon 🗑️ (Xóa)                           │
│  2. Modal confirm hiện lên                         │
│  3. Click "Xóa" để confirm                         │
│  4. AJAX DELETE → /Disease/DeleteDisease           │
│  5. Database delete                                │
│  6. Auto reload trang                              │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│                TÌM KIẾM / LỌC                       │
├─────────────────────────────────────────────────────┤
│  1. Gõ từ khóa vào Search input                    │
│  2. Debounce 500ms (tránh quá nhiều request)       │
│  3. Auto load kết quả (reload URL + params)        │
│  4. Hoặc chọn Status filter để lọc                 │
│  5. Kết quả hiển thị động                          │
└─────────────────────────────────────────────────────┘
```

---

## 📱 Responsive Design

### Desktop
- Table full width, 7 columns visible
- Search & filter lado by lado (grid 3 col)

### Tablet/Mobile
- Table scrollable horizontally
- Search & filter stack vertically
- Buttons responsive

---

## 🛡️ Validation & Security

### Client-Side
```javascript
// HTML5 form validation
// Required field: Tên Bệnh Lý
// Form.checkValidity() trước POST
```

### Server-Side
```csharp
// ModelState.IsValid check
// Duplicate name check
// Exception handling
// Parameterized queries (EF Core)
```

---

## 📝 Form Fields

```
┌─────────────────────────────────────────┐
│     Modal Thêm/Sửa Bệnh Lý             │
├─────────────────────────────────────────┤
│ Tên Bệnh Lý*              [Input]       │
│ Triệu Chứng               [Textarea]    │
│ Nguyên Nhân               [Textarea]    │
│ Phương Pháp Điều Trị      [Textarea]    │
│ Mô Tả                     [Textarea]    │
│ Trạng Thái  ○ Active ○ Inactive        │
│                                        │
│ [Hủy]  [Lưu/Cập Nhật]                 │
└─────────────────────────────────────────┘
* = Required
```

---

## 🧪 Test Scenarios

### ✅ Test 1: Thêm Bệnh Mới
```
Input: 
  - Name: "Viêm Phổi"
  - Symptoms: "Ho, sốt"
  - Causes: "Vi khuẩn"
  - Treatment: "Kháng sinh"
  - Status: Active

Expected:
  - Bản ghi thêm vào DB
  - Danh sách reload
  - Bệnh mới xuất hiện
```

### ✅ Test 2: Sửa Bệnh
```
Input: Click sửa bệnh lý ID=2
Expected:
  - Form fill data cũ
  - Thay đổi "Tên" → "Cảm Cúm (Flu)"
  - Click "Cập Nhật"
  - Database update
```

### ✅ Test 3: Xóa Bệnh
```
Input: Click xóa ID=1
Expected:
  - Modal confirm hiện
  - Click "Xóa"
  - Database delete
  - Danh sách reload
```

### ✅ Test 4: Tìm Kiếm
```
Input: Search "Tiểu Đường"
Expected:
  - Kết quả lọc theo tên
  - URL change: ?searchTerm=Tiểu%20Đường
  - Chỉ hiển thị kết quả matching
```

### ✅ Test 5: Lọc Trạng Thái
```
Input: Select "Đang Hoạt Động"
Expected:
  - Kết quả lọc IsActive=true
  - URL change: ?severity=active
  - Chỉ hiển thị bệnh hoạt động
```

### ✅ Test 6: Phân Trang
```
Input: Click trang 2
Expected:
  - URL change: ?page=2
  - Hiển thị bản ghi 11-20 (nếu có)
  - Nút "Previous" enable, "Next" tùy theo
```

---

## 🔧 Debugging Tips

### Console Log (Browser F12)
```javascript
// JavaScript errors
// Network requests (POST /Disease/SaveDisease)
// Response payloads
```

### Network Tab
```
1. POST /Disease/SaveDisease
   - Status: 200 OK
   - Response: {"success": true, ...}

2. GET /Disease/GetDiseaseById?id=1
   - Status: 200 OK
   - Response: {"success": true, "data": {...}}

3. POST /Disease/DeleteDisease?id=1
   - Status: 200 OK
   - Response: {"success": true, ...}
```

### Application Logs
```bash
# Xem lỗi trong console .NET
# Exception messages
# SQL queries (nếu enable logging)
```

---

## ✅ Pre-Launch Checklist

- [ ] Database migrated
- [ ] DbSet<Disease> exists in DbContext ✓ (có sẵn)
- [ ] DiseaseController tồn tại ✓
- [ ] Disease/Index.cshtml tồn tại ✓
- [ ] Test data đã thêm (optional)
- [ ] Chạy `dotnet run` thành công
- [ ] Truy cập /Disease/Index không lỗi
- [ ] Thêm, sửa, xóa hoạt động
- [ ] Tìm kiếm hoạt động
- [ ] Lọc hoạt động
- [ ] Phân trang hoạt động

---

## 🚨 Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| "DbContext not registered" | Kiểm tra Program.cs |
| "Table does not exist" | Chạy `dotnet ef database update` |
| "Modal không open" | Kiểm tra JavaScript errors (F12) |
| "AJAX request failed" | Kiểm tra Network tab, API URL |
| "Database connection error" | Kiểm tra connection string |
| "Form validation failed" | Kiểm tra Required fields |
| "Search không hoạt động" | Kiểm tra parameter names |
| "Pagination error" | Kiểm tra page number >= 1 |

---

## 📞 Support Documentation

Xem file: `DISEASE_MANAGEMENT_GUIDE.md` để biết chi tiết:
- API endpoints đầy đủ
- Database schema
- Curl examples
- Advanced features
- Security notes

---

**Status:** ✅ **READY TO USE**

**Version:** 1.0

**Last Updated:** 2026-05-24

---

🎉 **Congratulations!** Tính năng Disease Management đã sẵn sàng sử dụng!

Hãy bắt đầu với bước 1 trong "5 Bước Setup Nhanh" ở trên.
