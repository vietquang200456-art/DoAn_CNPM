# API Endpoints & Model Reference

## 📚 Model Structures

### Drug.cs
```csharp
public class Drug
{
    public int Id { get; set; }                          // Mã thuốc (PK)
    public string Name { get; set; }                     // Tên thuốc (bắt buộc, unique)
    public string ActiveIngredient { get; set; }         // Hoạt chất
    public string Function { get; set; }                 // Công dụng
    public string Dosage { get; set; }                   // Liều lượng
    public string SideEffects { get; set; }              // Tác dụng phụ
    public string Contraindications { get; set; }        // Chống chỉ định
    public string Manufacturer { get; set; }             // Nhà sản xuất
    public string Description { get; set; }              // Mô tả thêm
    public bool IsActive { get; set; }                   // Trạng thái (default: true)
    public DateTime CreatedAt { get; set; }              // Ngày tạo
    public DateTime? UpdatedAt { get; set; }             // Ngày cập nhật
    public int ViewCount { get; set; }                   // Lượt xem
    
    // Navigation properties
    public ICollection<DrugInteraction> DrugInteractionsAsSourceDrug { get; set; }
    public ICollection<DrugInteraction> DrugInteractionsAsTargetDrug { get; set; }
    public ICollection<DrugDiseaseContraindication> DrugDiseaseContraindications { get; set; }
    public ICollection<SearchHistory> SearchHistories { get; set; }
}
```

### DrugPagedListViewModel.cs
```csharp
public class DrugPagedListViewModel
{
    public IEnumerable<Drug> Drugs { get; set; }        // Danh sách thuốc trang hiện tại
    public int CurrentPage { get; set; }                // Trang hiện tại
    public int TotalRecords { get; set; }               // Tổng số bản ghi
    public int PageSize { get; set; }                   // Số bản ghi/trang (mặc định 10)
    public int TotalPages { get; set; }                 // Tổng số trang (tính toán)
    public bool HasPreviousPage { get; set; }           // Có trang trước
    public bool HasNextPage { get; set; }               // Có trang sau
    public string SearchTerm { get; set; }              // Từ khóa tìm kiếm
    public string StatusFilter { get; set; }            // Lọc trạng thái
}
```

---

## 🔌 API Endpoints

### 1. GET /Drug/Index
**Hiển thị danh sách thuốc**

**Parameters (Query String):**
```
GET /Drug/Index?searchTerm=amoxicillin&status=active&page=1
```
- `searchTerm` (string, optional): Tìm theo tên, hoạt chất, nhà sản xuất
- `status` (string, optional): "active" hoặc "inactive"
- `page` (int, optional): Số trang (default = 1)

**Response:** 
- HTML View (DrugPagedListViewModel)
- Status: 200 OK

**Example:**
```
GET /Drug/Index?searchTerm=paracetamol&status=active&page=1
```

---

### 2. GET /Drug/GetDrugById
**Lấy chi tiết thuốc (JSON)**

**Parameters:**
```
GET /Drug/GetDrugById?id=5
```
- `id` (int, required): ID của thuốc

**Response:**
```json
{
  "success": true,
  "data": {
    "id": 5,
    "name": "Paracetamol 500mg",
    "activeIngredient": "Paracetamol",
    "function": "Hạ sốt, giảm đau",
    "dosage": "500mg",
    "sideEffects": "Buồn nôn, chóng mặt",
    "contraindications": "Dị ứng paracetamol",
    "manufacturer": "Công ty ABC",
    "description": "Thuốc hạ sốt, giảm đau nhất dụng",
    "isActive": true
  }
}
```

**Status:** 
- 200 OK (nếu thành công)
- 200 OK với `success: false` (nếu không tìm thấy)

---

### 3. POST /Drug/SaveDrug
**Thêm mới hoặc cập nhật thuốc**

**Content-Type:** `application/json`

**Request Body (Add New):**
```json
{
  "id": 0,
  "name": "Amoxicillin 500mg",
  "activeIngredient": "Amoxicillin Trihydrate",
  "function": "Kháng sinh",
  "dosage": "500mg",
  "sideEffects": "Dị ứng, tiêu chảy",
  "contraindications": "Dị ứng với Penicillin",
  "manufacturer": "Công ty XYZ",
  "description": "Thuốc kháng sinh phổ rộng",
  "isActive": true
}
```

**Request Body (Update Existing):**
```json
{
  "id": 5,
  "name": "Amoxicillin 500mg - Updated",
  "activeIngredient": "...",
  ...
}
```

**Response (Success):**
```json
{
  "success": true,
  "message": "Thêm thuốc thành công",
  "data": {
    "id": 10
  }
}
```

**Response (Error):**
```json
{
  "success": false,
  "message": "Tên thuốc này đã tồn tại",
  "errors": []
}
```

**Status:** 
- 200 OK

---

### 4. POST/DELETE /Drug/DeleteDrug
**Xóa thuốc**

**Parameters:**
```
DELETE /Drug/DeleteDrug?id=5
```
- `id` (int, required): ID thuốc cần xóa

**Response (Success):**
```json
{
  "success": true,
  "message": "Xóa thuốc thành công"
}
```

**Response (Error):**
```json
{
  "success": false,
  "message": "Không tìm thấy thuốc để xóa"
}
```

**Status:** 
- 200 OK

---

### 5. GET /Drug/GetDrugsPartial
**Lấy danh sách thuốc (JSON, dành cho AJAX)**

**Parameters:**
```
GET /Drug/GetDrugsPartial?searchTerm=amoxicillin&status=active&page=1
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "Amoxicillin 500mg",
      "activeIngredient": "Amoxicillin Trihydrate",
      ...
    }
  ],
  "totalRecords": 145,
  "currentPage": 1,
  "pageSize": 10,
  "totalPages": 15
}
```

**Status:** 
- 200 OK

---

## 🧪 Curl Examples

### Thêm thuốc mới
```bash
curl -X POST http://localhost:5000/Drug/SaveDrug \
  -H "Content-Type: application/json" \
  -d '{
    "id": 0,
    "name": "Ibuprofen 400mg",
    "activeIngredient": "Ibuprofen",
    "dosage": "400mg",
    "isActive": true
  }'
```

### Lấy chi tiết thuốc
```bash
curl http://localhost:5000/Drug/GetDrugById?id=1
```

### Xóa thuốc
```bash
curl -X POST http://localhost:5000/Drug/DeleteDrug?id=1
```

### Tìm kiếm thuốc
```bash
curl "http://localhost:5000/Drug/Index?searchTerm=amoxicillin&page=1"
```

---

## 📊 HTTP Status Codes

| Code | Description |
|------|-------------|
| 200  | OK - Yêu cầu thành công |
| 400  | Bad Request - Dữ liệu không hợp lệ |
| 404  | Not Found - Thuốc không tìm thấy |
| 500  | Internal Server Error - Lỗi server |

---

## ✔️ Validation Rules

| Field | Validation |
|-------|-----------|
| Name | Bắt buộc, không được để trống, tối đa 255 ký tự, không trùng lặp |
| Id | Integer >= 0 |
| IsActive | Boolean (true/false) |
| All others | Optional |

---

## 🔐 Security Notes

1. **CSRF Protection:**
   - Form POST tự động thêm CSRF token
   - API endpoints chuyên biệt không cần CSRF (nếu tính toán rồi)

2. **Input Validation:**
   - Client-side: HTML5 validation
   - Server-side: ModelState validation

3. **SQL Injection Prevention:**
   - Sử dụng EF Core Parameterized Queries

---

## 📝 Logging & Debugging

Để debug, thêm vào DrugController:
```csharp
public class DrugController : Controller
{
    private readonly ILogger<DrugController> _logger;
    
    public DrugController(ApplicationDbContext context, ILogger<DrugController> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<IActionResult> Index(string searchTerm = "", string status = "", int page = 1)
    {
        _logger.LogInformation($"Index called: searchTerm={searchTerm}, status={status}, page={page}");
        // ...
    }
}
```

---

## 🚀 Performance Tips

1. **Phân trang:** Luôn sử dụng `Skip().Take()` để tránh load toàn bộ dữ liệu
2. **Indexing:** Tạo index trên Name và CreatedAt
3. **Caching:** Cache danh sách trạng thái (Active/Inactive)

---

**Tài liệu được cập nhật lần cuối: 2026-05-24**
