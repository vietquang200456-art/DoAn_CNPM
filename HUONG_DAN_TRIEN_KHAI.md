# Hướng Dẫn Triển Khai Ứng Dụng Quản Lý Thuốc (PharmaCheck)

## 📋 Tóm Tắt Các Thay Đổi

Tôi đã hoàn thiện toàn bộ chức năng CRUD cho ứng dụng quản lý thuốc:

### ✅ 1. Tạo ViewModel (DrugPagedListViewModel.cs)
- Hỗ trợ phân trang, tìm kiếm, lọc
- Tính toán tổng số trang, kiểm tra trang trước/sau

### ✅ 2. Cập Nhật DrugController.cs
Các API endpoints:
- `Index()` - Hiển thị danh sách thuốc với phân trang, tìm kiếm, lọc
- `GetDrugById(id)` - Lấy chi tiết thuốc (JSON)
- `SaveDrug(model)` - Thêm mới/cập nhật thuốc
- `DeleteDrug(id)` - Xóa thuốc
- `GetDrugsPartial()` - API tìm kiếm/lọc (tương lai nếu cần AJAX reload)

### ✅ 3. Chỉnh Sửa View (Drug/Index.cshtml)
- Thay model từ `IEnumerable` thành `DrugPagedListViewModel`
- Sử dụng `@foreach` để loop danh sách thuốc từ database
- Logic màu sắc động cho Badge trạng thái (IsActive)
- Phân trang động (hiển thị nút trang dựa vào tổng số trang)
- Modal form hoàn thiện với các field đầy đủ

### ✅ 4. JavaScript/AJAX Hoàn Thiện
- `openDrugModal()` - Mở modal thêm mới (reset form)
- `editDrug(id)` - Mở modal chỉnh sửa (fetch data từ API)
- `saveDrug()` - AJAX POST để thêm/cập nhật
- `confirmDeleteDrug(id, name)` - Hiển thị xác nhận xóa
- `deleteDrugConfirmed()` - AJAX DELETE
- Tìm kiếm/lọc với debounce

---

## 🚀 Bước Triển Khai

### Bước 1: Kiểm tra Database
Đảm bảo database đã được tạo và migration đã chạy:

```bash
# Trong Package Manager Console
Add-Migration InitialCreate
Update-Database
```

### Bước 2: Chạy Ứng Dụng
```bash
dotnet run
```

Truy cập: `https://localhost:5001/Drug/Index`

### Bước 3: Test Chức Năng CRUD

#### **Thêm Thuốc Mới:**
1. Click "Thêm Thuốc Mới"
2. Điền form (chỉ cần Tên Thuốc)
3. Click "Lưu"

#### **Sửa Thuốc:**
1. Click biểu tượng ✏️ (Sửa) trên dòng thuốc
2. Form sẽ tự động điền dữ liệu
3. Chỉnh sửa nội dung
4. Click "Cập Nhật"

#### **Xóa Thuốc:**
1. Click biểu tượng 🗑️ (Xóa)
2. Xác nhận xóa
3. Click "Xóa"

#### **Tìm Kiếm/Lọc:**
- Nhập tên thuốc hoặc hoạt chất vào ô tìm kiếm
- Chọn trạng thái từ Select
- Bảng sẽ tự động reload

#### **Phân Trang:**
- Bấm các nút trang số hoặc mũi tên Previous/Next

---

## 📂 Cấu Trúc Thư Mục

```
PharmaCheck/
├── Controllers/
│   └── DrugController.cs         ✅ (Đã cập nhật)
├── Models/
│   ├── Drug.cs                   (Hiện tại)
│   ├── DrugPagedListViewModel.cs ✅ (Tạo mới)
│   └── ... (các model khác)
├── Views/
│   └── Drug/
│       └── Index.cshtml          ✅ (Đã cập nhật)
└── Data/
    └── ApplicationDbContext.cs   (Hiện tại)
```

---

## 🔧 Thông Tin Chi Tiết Về Các Hàm

### DrugController Methods

#### `Index(searchTerm, status, page)`
```csharp
// Trả về: View với DrugPagedListViewModel
// Parameter:
//   - searchTerm: tìm kiếm theo tên, hoạt chất, nhà sản xuất
//   - status: "active" hoặc "inactive"
//   - page: số trang (default = 1)
```

#### `GetDrugById(id)`
```csharp
// Trả về: JSON { success: bool, data: { ...drugData } }
// Dùng cho: Lấy dữ liệu khi click Sửa
```

#### `SaveDrug(model)`
```csharp
// Trả về: JSON { success: bool, message: string }
// Thêm nếu Id = 0, cập nhật nếu Id > 0
```

#### `DeleteDrug(id)`
```csharp
// Trả về: JSON { success: bool, message: string }
// Xóa thuốc khỏi database
```

---

## 🎨 CSS/Tailwind Classes

### Badge Trạng Thái
- **Active (Hoạt Động):** `bg-green-100 text-green-800`
- **Inactive (Ngừng):** `bg-gray-100 text-gray-800`

### Button Actions
- **Edit:** `bg-blue-100 text-blue-700`
- **Delete:** `bg-red-100 text-red-700`
- **View:** `bg-gray-100 text-gray-700`

---

## ⚠️ Lưu Ý Quan Trọng

1. **Database Constraints:**
   - Tên thuốc không được trùng lặp
   - Các trường bắt buộc: Name

2. **Validation:**
   - Form kiểm tra validation client-side trước
   - Server-side kiểm tra lại trước khi lưu

3. **Error Handling:**
   - Các lỗi sẽ hiển thị trong alert hoặc form message
   - Xem browser console nếu cần debug

4. **Performance:**
   - Phân trang mặc định 10 bản ghi/trang
   - Có thể thay đổi `PageSize` trong DrugController

---

## 📌 Tính Năng Mở Rộng (Nếu Cần)

### 1. Export/Import Excel
- Thêm nuget: `EPPlus` hoặc `ClosedXML`
- Thêm action `ExportToExcel()` trong controller

### 2. Upload Hình Ảnh
- Thêm field `ImageUrl` vào Drug model
- Tạo logic upload file
- Hiển thị hình ảnh trong table/modal

### 3. Audit Log
- Ghi lại ai thay đổi/xóa thuốc khi nào
- Thêm `CreatedBy`, `UpdatedBy`, `DeletedBy` fields

### 4. Advanced Search
- Tìm kiếm theo ngày tạo
- Lọc theo nhà sản xuất
- Lọc theo tác dụng phụ

---

## 🐛 Troubleshooting

### "Page không tìm thấy" khi click thêm/sửa
- Kiểm tra route attribute: `/Drug/Index`
- Đảm bảo DrugController public

### Form không lưu được
- Kiểm tra browser console (F12) xem lỗi gì
- Kiểm tra NetworkTab xem response từ server
- Đảm bảo `ModelState.IsValid`

### Danh sách không load
- Kiểm tra database connection
- Verify migrations đã chạy
- Xem Application Insights hoặc logs

---

## 📞 Liên Hệ & Hỗ Trợ

Nếu gặp vấn đề:
1. Kiểm tra console (F12)
2. Xem Application Logs
3. Debug từng bước bằng breakpoint

**Chúc bạn phát triển ứng dụng thành công! 🎉**
