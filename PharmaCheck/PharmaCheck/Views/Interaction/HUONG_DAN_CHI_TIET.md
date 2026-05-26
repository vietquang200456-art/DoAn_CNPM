# Hướng Dẫn Quản Lý Tương Tác & Chống Chỉ Định Thuốc

## 📋 Tổng Quan

Hệ thống này cho phép quản lý:
1. **Tương tác Thuốc-Thuốc**: Khi 2 thuốc tương tác với nhau
2. **Chống chỉ định Thuốc-Bệnh**: Khi thuốc không nên dùng với bệnh nhất định

---

## 🗂️ Cấu Trúc File

### Backend (C# .NET Core)

#### 1. **Model - InteractionManagementViewModel.cs**
- Chứa 3 class chính:
  - `InteractionManagementViewModel`: ViewModel chính, truyền dữ liệu từ Controller → View
  - `DrugInteractionDisplayDto`: DTO hiển thị thông tin tương tác thuốc
  - `DrugDiseaseContraindicationDisplayDto`: DTO hiển thị thông tin chống chỉ định

**Tính năng:**
- Lưu trữ danh sách tương tác và chống chỉ định
- Lưu trữ tham số tìm kiếm (để giữ trạng thái form)
- Lưu trữ thông tin phân trang (trang hiện tại, tổng trang, tổng bản ghi)
- Phương thức `SeverityLevelName` và `SeverityBadgeClass` - tự động tạo tên và màu sắc badge dựa vào mức độ

#### 2. **Controller - InteractionController.cs**
Gồm 4 hàm action chính:

**a) `Index()` - GET**
```csharp
public IActionResult Index()
```
- Hiển thị trang chính
- Gọi `GetDrugInteractionsData()` và `GetDrugDiseaseContraindicationsData()` để lấy dữ liệu mặc định (trang 1)
- Trả về View với ViewModel đầy đủ

**b) `SearchAndFilterInteractions(string? searchTerm, int? severityLevel, int pageNumber = 1)` - POST**
```csharp
[HttpPost]
public IActionResult SearchAndFilterInteractions(string? searchTerm, int? severityLevel, int pageNumber = 1)
```
- Xử lý tìm kiếm tương tác thuốc
- Nhận tham số: `searchTerm` (từ khóa), `severityLevel` (mức độ 1-5), `pageNumber` (số trang)
- Trả về Partial View (_DrugInteractionsTable.cshtml)
- Cho phép JavaScript fetch dữ liệu mà không reload trang

**c) `SearchAndFilterContraindications(string? searchTerm, int? riskLevel, int pageNumber = 1)` - POST**
```csharp
[HttpPost]
public IActionResult SearchAndFilterContraindications(string? searchTerm, int? riskLevel, int pageNumber = 1)
```
- Xử lý tìm kiếm chống chỉ định thuốc-bệnh
- Tương tự như action b, nhưng cho dữ liệu chống chỉ định

**d) `GetDrugInteractionsData()` - Private Helper**
```csharp
private PaginatedResult<DrugInteractionDisplayDto> GetDrugInteractionsData(
    string? searchTerm, 
    int? severityLevel, 
    int pageNumber)
```

**Chi tiết logic:**
1. Query từ database: `_context.DrugInteractions.Include(di => di.SourceDrug).Include(di => di.TargetDrug)`
2. Filter tìm kiếm (nếu có):
   - Tìm trong tên thuốc nguồn, thuốc đích
   - Tìm trong hoạt chất
   - Tìm trong mô tả
3. Filter mức độ (nếu có):
   - So sánh `SeverityLevel` với giá trị được chọn
4. Phân trang:
   - Tính tổng bản ghi: `query.Count()`
   - Tính tổng trang: `Math.Ceiling((double)totalRecords / PageSize)` (PageSize = 10)
   - Skip: `(pageNumber - 1) * PageSize` bản ghi
   - Take: `PageSize` (10) bản ghi
5. Chuyển đổi sang DTO với tên thuốc và badge classes
6. Trả về `PaginatedResult<T>` chứa Items, CurrentPage, TotalPages, TotalRecords

**e) `GetDrugDiseaseContraindicationsData()` - Private Helper**
- Tương tự như `GetDrugInteractionsData()` nhưng cho bảng chống chỉ định
- Include `Drug` và `Disease` navigation properties

---

### Frontend (Razor View + JavaScript)

#### 1. **View - Index.cshtml**
- Giao diện Tailwind CSS với 2 tab: Tương tác Thuốc & Chống Chỉ Định Bệnh
- Gradient màu xanh y tế: `from-blue-700 via-blue-600 to-indigo-800`

**Cấu trúc HTML:**
1. **Banner tiêu đề**: Gradient xanh, icon, tiêu đề
2. **Tab navigation**: 2 nút tab có active state
3. **Tab 1 - Tương Tác Thuốc**:
   - Form tìm kiếm với 2 field: searchTerm, severityLevel
   - Thống kê: tổng bản ghi, trang hiện tại
   - Bảng dữ liệu (được load từ partial view)
   - Phân trang

4. **Tab 2 - Chống Chỉ Định**:
   - Tương tự tab 1 nhưng với màu đỏ

**JavaScript Functions:**
1. `switchTab(tabName)` - Chuyển đổi tab
2. `handleInteractionSearch(event)` - Submit form tìm kiếm
3. `handleContraindicationSearch(event)` - Submit form tìm kiếm
4. `goToInteractionPage(pageNumber)` - Chuyển trang
5. `goToContraindicationPage(pageNumber)` - Chuyển trang
6. `resetInteractionFilters()` - Xóa bộ lọc
7. `resetContraindicationFilters()` - Xóa bộ lọc

**Cơ chế fetch:**
```javascript
fetch('@Url.Action("SearchAndFilterInteractions", "Interaction")', {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
        searchTerm: ...,
        severityLevel: ...,
        pageNumber: 1
    })
})
.then(response => response.text())
.then(html => {
    document.getElementById('interactions-table-container').innerHTML = html;
})
```

#### 2. **Partial Views**
- `_DrugInteractionsTable.cshtml`: Bảng tương tác thuốc
- `_DrugDiseaseContraindicationsTable.cshtml`: Bảng chống chỉ định

**Lợi ích của Partial Views:**
- Tái sử dụng HTML bảng
- Cập nhật bảng qua AJAX mà không reload
- Quản lý phân trang dễ dàng

---

## 🔍 Luồng Dữ Liệu

### Luồng 1: Tải Trang Lần Đầu
1. User truy cập `/Interaction`
2. `Index()` gọi `GetDrugInteractionsData(null, null, 1)`
3. Query lấy tất cả dữ liệu (trang 1, không lọc)
4. Trả về View với ViewModel chứa 20 bản ghi (10 tương tác + 10 chống chỉ định)

### Luồng 2: Tìm Kiếm
1. User nhập "Aspirin" vào ô tìm kiếm
2. User click nút "Tìm Kiếm & Lọc"
3. Form submit → `handleInteractionSearch(event)`
4. JavaScript gửi POST tới `SearchAndFilterInteractions`
5. Controller gọi `GetDrugInteractionsData("aspirin", null, 1)`
6. Query filter và trả về dữ liệu
7. Trả về HTML của bảng (partial view)
8. JavaScript cập nhật `#interactions-table-container` innerHTML

### Luồng 3: Phân Trang
1. User click trang 2
2. `goToInteractionPage(2)` gọi `SearchAndFilterInteractions` với `pageNumber=2`
3. Cùng filter cũ, nhưng lấy bản ghi từ 11-20
4. Cập nhật bảng

---

## 🎨 Thiết Kế UI/UX

### Màu Sắc

**Tab Tương Tác Thuốc** (Xanh):
- Badge Nhẹ: `bg-green-100 text-green-800`
- Badge Vừa: `bg-blue-100 text-blue-800`
- Badge Trung bình: `bg-yellow-100 text-yellow-800`
- Badge Nặng: `bg-orange-100 text-orange-800`
- Badge Rất nặng: `bg-red-100 text-red-800`

**Tab Chống Chỉ Định** (Đỏ):
- Cùng tương tự nhưng header là `from-red-600 to-pink-600`

### Bố Cục
- **Header**: Gradient, icon, tiêu đề
- **Form**: Gradient nhẹ, border màu tương ứng
- **Bảng**: Kẻ ngang, hover background, truncate text dài
- **Badge Mức Độ**: Màu theo mức (1-5)
- **Phân Trang**: Nút chuyển trang, page hiện tại highlight

---

## 🔧 Cách Sử Dụng

### 1. Tìm Kiếm Tương Tác Thuốc
1. Truy cập `/Interaction`
2. Mặc định hiển thị 10 tương tác mới nhất
3. Nhập tên thuốc vào ô tìm kiếm
4. Chọn mức độ nghiêm trọng (tùy chọn)
5. Click "Tìm Kiếm & Lọc"
6. Kết quả cập nhật ngay

### 2. Xem Tương Tác Khác
- Có tối đa 5 mức độ (1-5)
- Có thể tìm kiếm không có lọc mức độ
- Có thể lọc mức độ không có tìm kiếm

### 3. Chuyển Đến Tab Chống Chỉ Định
- Click tab "Chống Chỉ Định Thuốc - Bệnh"
- Cùng logic tìm kiếm/lọc/phân trang

---

## 📝 Database Relationships

### DrugInteraction
```
DrugInteraction
├── SourceDrugId → Drug (tham chiếu)
├── TargetDrugId → Drug (tham chiếu)
├── SeverityLevel (1-5)
├── Description
├── Recommendation
└── CreatedAt
```

### DrugDiseaseContraindication
```
DrugDiseaseContraindication
├── DrugId → Drug (tham chiếu)
├── DiseaseId → Disease (tham chiếu)
├── RiskLevel (1-5)
├── Warning
├── Risk
├── Recommendation
└── CreatedAt
```

---

## 🚀 Tối Ưu Hóa

1. **Pagination**: Mỗi trang chỉ load 10 bản ghi
2. **.Include()**: Sử dụng eager loading tránh N+1 queries
3. **Partial Views**: Chỉ load HTML bảng, không full page
4. **Filter trên Server**: Toàn bộ tìm kiếm/lọc xảy ra trên server
5. **Truncate Text**: Bảng hiển thị text đầy đủ qua title attribute

---

## ✅ Kiểm Tra Lỗi

1. Nếu không hiển thị dữ liệu:
   - Kiểm tra database có dữ liệu không
   - Kiểm tra Include() navigation properties
   - Xem browser console có lỗi JavaScript không

2. Nếu tìm kiếm không hoạt động:
   - Kiểm tra POST action có nhận dữ liệu không
   - Kiểm tra LINQ filter logic

3. Nếu phân trang không work:
   - Kiểm tra Math.Ceiling calculation
   - Kiểm tra Skip/Take parameters

---

## 📚 Phụ Lục: Query LINQ Chi Tiết

### Query Tương Tác Thuốc với Tìm Kiếm
```csharp
// Bước 1: Include navigation properties
var query = _context.DrugInteractions
    .Include(di => di.SourceDrug)
    .Include(di => di.TargetDrug)
    .AsQueryable();

// Bước 2: Filter tìm kiếm
query = query.Where(di =>
    di.SourceDrug!.Name.ToLower().Contains("aspirin") ||
    di.TargetDrug!.Name.ToLower().Contains("aspirin") ||
    di.SourceDrug!.ActiveIngredient.ToLower().Contains("aspirin") ||
    di.TargetDrug!.ActiveIngredient.ToLower().Contains("aspirin") ||
    di.Description.ToLower().Contains("aspirin")
);

// Bước 3: Filter mức độ
query = query.Where(di => di.SeverityLevel == 5);

// Bước 4: Phân trang
query = query
    .OrderByDescending(di => di.CreatedAt)
    .Skip((1 - 1) * 10)  // page 1, page size 10
    .Take(10);

// Bước 5: Thực thi
var results = query.ToList();
```

---

## 🎯 Kết Luận

Hệ thống này:
✅ Cho phép tìm kiếm tương tác thuốc từ database thực  
✅ Hỗ trợ lọc theo mức độ (1-5)  
✅ Cải thiện UX với phân trang (10 bản ghi/trang)  
✅ Giao diện sạch sẽ với Tailwind CSS (gradient xanh y tế)  
✅ Tự động lưu trạng thái form (search term, level) khi search  
✅ Sử dụng Partial Views để cập nhật bảng qua AJAX  

