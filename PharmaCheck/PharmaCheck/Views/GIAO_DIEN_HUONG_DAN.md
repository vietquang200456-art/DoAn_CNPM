# 📱 Hướng Dẫn Giao Diện PharmaCheck - Sử Dụng Tailwind CSS & Alpine.js

## 🎨 Tổng Quan Thiết Kế

### 1. **Công nghệ sử dụng**
- **CSS Framework**: Tailwind CSS 3.x (nhúng qua CDN từ cdn.tailwindcss.com)
- **Icons**: FontAwesome 6.5.1 (CDN)
- **Interactivity**: Alpine.js 3.x (CDN) - JavaScript framework nhẹ
- **Template Engine**: ASP.NET Core Razor (.cshtml)
- **Responsive Design**: Mobile-first, hoạt động trên tất cả thiết bị

### 2. **Màu Sắc Y Tế**
```css
medical-700: #0369a1  /* Xanh Medical - Màu chính */
medical-600: #0284c7
medical-50: #f0f9ff   /* Xanh rất nhạt - Background */

danger: #dc2626       /* Đỏ - Nguy Hiểm/Chống Chỉ Định */
warning: #f59e0b      /* Vàng/Cam - Thận Trọng/Cảnh Báo */
success: #10b981      /* Xanh lá - An Toàn */

Xám nhạt: #f8fafc (slate-50)
Xám trung bình: #e2e8f0 (slate-200)
```

---

## 📁 Cấu Trúc File Được Tạo

### **1. Views/Shared/_Layout.cshtml** ⭐
**Mục đích**: Layout chung cho toàn bộ ứng dụng

**Thành phần chính**:
- ✅ Tailwind CSS CDN + Custom medical theme colors
- ✅ FontAwesome 6.5.1 CDN
- ✅ Alpine.js CDN
- ✅ **Header/Navigation** với:
  - Logo + Brand name
  - Menu items (Trang Chủ, Quản Lý Thuốc, Cấu Hình Tương Tác, Bảo Mật)
  - Mobile responsive menu (hamburger)
  - Hover effects & transitions
- ✅ **Main container** - Chứa @RenderBody()
- ✅ **Footer** với:
  - 4 cột: About, Quick Links, Support, Legal
  - Social media icons
  - Copyright info

**Code Highlights**:
```html
<!-- Tailwind CDN -->
<script src="https://cdn.tailwindcss.com"></script>

<!-- Custom medical colors config -->
<script>
tailwind.config = {
    theme: {
        extend: {
            colors: {
                'medical': {
                    700: '#0369a1',
                    600: '#0284c7',
                    ...
                }
            }
        }
    }
}
</script>

<!-- Alpine.js -->
<script defer src="https://cdn.jsdelivr.net/npm/alpinejs@3.x.x/dist/cdn.min.js"></script>
```

**Responsive Features**:
- Mobile menu toggle (hidden trên md+)
- Flexible navigation
- Sticky header

---

### **2. Views/Home/Index.cshtml** 🏠
**Mục đích**: Trang chủ & tìm kiếm tương tác thuốc-bệnh

**Thành phần chính**:

#### A. **Hero Section** (xanh medical gradient)
- Title + Subtitle
- 2 CTA buttons (smooth scroll)
- Icon illustration

#### B. **Search Section** (Tìm kiếm thông minh)
Sử dụng **Alpine.js** để xử lý:
```javascript
// Mock data
mockDrugs: ['Amoxicillin', 'Paracetamol', ...]
mockDiseases: ['I10 - Tăng huyết áp', ...]

// Features:
- Tìm kiếm thuốc với dropdown suggestions
- Tìm kiếm bệnh với dropdown suggestions
- Selected items hiển thị dưới dạng tags
- Nút "Kiểm Tra Tương Tác" & "Xóa Tất Cả"
```

**Dropdown behavior**:
- Tự động hiện khi user nhập text
- Lọc theo tên hoặc hoạt chất
- Không cho phép duplicate selections

#### C. **Results Section** (Hiển thị kết quả)
Sau khi nhấn "Kiểm Tra", hiển thị 2 phần:

**1) Drug-Drug Interactions**
- Ví dụ: Aspirin + Warfarin → NGUY HIỂM (đỏ)
- Alert box với:
  - Icon (fa-exclamation-triangle)
  - Title: "Aspirin + Warfarin - NGUY HIỂM"
  - Description: "Kết hợp 2 thuốc chống đông..."
  - Recommendation: "Sử dụng liều thấp hoặc đổi..."

**2) Drug-Disease Contraindications**
- Ví dụ: Ibuprofen + Bệnh thận → CHỐNG CHỈ ĐỊNH (đỏ)
- Cấu trúc tương tự

**Overall Safety Status**:
- 🟢 An Toàn (xanh)
- 🟡 Cần Thận Trọng (vàng)
- 🔴 NGUY HIỂM (đỏ)

#### D. **Statistics Section**
- 3 thẻ thống kê:
  - 1,245 Thuốc
  - 3,856 Bệnh (ICD-10)
  - 24,567 Lượt tra cứu

#### E. **Features Section**
- 6 tính năng chính với icons
- Card hover effect

#### F. **CTA Section**
- Gradient background (medical colors)
- Call-to-action button

**Mock Data Examples**:
```javascript
// Drug-Drug: Aspirin + Warfarin
{
    id: 1,
    title: 'Aspirin + Warfarin - NGUY HIỂM',
    description: 'Kết hợp 2 thuốc chống đông máu có nguy cơ chảy máu cao',
    icon: 'fa-exclamation-triangle',
    severityClass: 'border-red-500 bg-red-50',
    recommendation: 'Sử dụng liều thấp...'
}

// Drug-Disease: Ibuprofen + Bệnh thận
{
    id: 1,
    title: 'Ibuprofen + Bệnh thận mạn tính - CHỐNG CHỈ ĐỊNH',
    description: 'NSAID có thể làm xấu thêm chức năng thận',
    ...
}
```

---

### **3. Views/Drug/Index.cshtml** 💊
**Mục đích**: Quản lý thuốc (CRUD)

**Thành phần chính**:

#### A. **Header**
- Title: "Quản Lý Thuốc"
- Button: "Thêm Thuốc Mới" (mở modal)

#### B. **Search & Filter Bar**
```html
- Search input (tìm kiếm theo tên, hoạt chất, mã)
- Status filter dropdown (Đang Hoạt Động, Ngừng, Hạn Chế)
```

#### C. **Table** (Bảng thuốc)
| Cột | Nội Dung |
|-----|----------|
| Tên Thuốc | Amoxicillin 500mg (+ mã: AMX-500) |
| Hoạt Chất | Badge: Amoxicillin Trihydrate |
| Dạng Bào Chế | Viên, Siro |
| Liều Dùng | 500mg x 3/ngày |
| Trạng Thái | Badge (xanh: Đang Hoạt Động, vàng: Hạn Chế) |
| Hành Động | Edit, View, Delete buttons |

**Sample data** (5 thuốc):
1. Amoxicillin 500mg - Đang Hoạt Động
2. Paracetamol 500mg - Đang Hoạt Động
3. Ibuprofen 400mg - Hạn Chế
4. Metformin 500mg - Đang Hoạt Động
5. Lisinopril 10mg - Đang Hoạt Động

#### D. **Pagination**
- "Hiển thị 1-5 của 1,245 thuốc"
- Nút Previous/Next
- Page numbers

#### E. **Modal: Add/Edit Drug**
Fields:
- Tên Thuốc (required)
- Hoạt Chất
- Dạng Bào Chế (dropdown)
- Liều Lượng
- Hướng Dẫn Dùng
- Mô Tả / Công Dụng (textarea)
- Trạng Thái (dropdown)
- Mã Thuốc

#### F. **Delete Confirmation Modal**
- Icon cảnh báo
- Tên thuốc cần xóa
- Confirm/Cancel buttons

**JavaScript Functions**:
```javascript
openDrugModal()        // Mở modal thêm
editDrug()            // Mở modal sửa
closeDrugModal()      // Đóng modal
confirmDeleteDrug()   // Xác nhận xóa
```

---

### **4. Views/Interaction/Index.cshtml** ⚠️
**Mục đích**: Cấu hình tương tác (2 tabs)

**Thành phần chính**:

#### A. **Tab Navigation** (2 tabs)
- **Tab 1**: Tương Tác Thuốc-Thuốc (màu amber - vàng)
- **Tab 2**: Chống Chỉ Định Thuốc-Bệnh (màu red - đỏ)

Tab switching bằng Alpine.js hoặc vanilla JS

#### B. **Tab 1: Drug-Drug Interactions**

**Search & Filter**:
- Search input (tìm tương tác)
- Filter by severity (Nguy Hiểm, Thận Trọng, An Toàn)
- Filter by status (Hoạt Động, Vô Hiệu Hóa)

**Table columns**:
| Cột | Ví Dụ |
|-----|--------|
| Thuốc 1 | Aspirin (100-500mg) |
| Thuốc 2 | Warfarin (2-10mg) |
| Mức Độ | 🔴 NGUY HIỂM (red badge) |
| Cơ Chế | "Cả hai đều chống đông máu..." |
| Trạng Thái | Hoạt Động (green) |
| Hành Động | Edit, View, Delete |

**Sample data** (4 interactions):
1. Aspirin + Warfarin - NGUY HIỂM
2. Ibuprofen + Lisinopril - THẬN TRỌNG
3. Paracetamol + Ibuprofen - THẬN TRỌNG
4. Ciprofloxacin + Atorvastatin - THẬN TRỌNG

#### C. **Tab 2: Drug-Disease Contraindications**

**Search & Filter**: (tương tự tab 1, nhưng lọc theo mức độ chống chỉ định)

**Table columns**:
| Cột | Ví Dụ |
|-----|--------|
| Thuốc | Ibuprofen (NSAID) |
| Bệnh (ICD-10) | N18 - Bệnh Thận Mạn Tính (eGFR < 30) |
| Mức Độ | 🔴 CHỐNG CHỈ ĐỊNH TUYỆT ĐỐI |
| Lý Do | "NSAID làm xấu thêm chức năng thận" |
| Trạng Thái | Hoạt Động |
| Hành Động | Edit, View, Delete |

**Sample data** (4 contraindications):
1. Ibuprofen + N18 (Bệnh Thận) - CHỐNG CHỈ ĐỊNH TUYỆT ĐỐI
2. Metformin + N18 (Bệnh Thận) - THẬN TRỌNG
3. Ciprofloxacin + J44 (COPD) - AN TOÀN
4. ACE Inhibitors + Mang Thai - CHỐNG CHỈ ĐỊNH TUYỆT ĐỐI

#### D. **Modal: Add/Edit Interaction**
Fields:
- Loại Tương Tác (dropdown: Drug-Drug, Drug-Disease)
- Thuốc 1 (text input với auto-complete)
- Thuốc 2 / Bệnh (text input)
- Mức Độ Nguy Hiểm (dropdown)
- Trạng Thái (dropdown)
- Cơ Chế / Lý Do Tương Tác (textarea)
- Khuyến Cáo / Cách Xử Lý (textarea)
- Nguồn Tham Khảo (text input)

#### E. **Pagination** (tương tự Drug page)

---

## 🎯 Cách Sử Dụng Code

### **1. Kết Nối Với Backend (Entity Framework)**

Hiện tại, các file sử dụng **Mock Data** (dữ liệu giả). Để kết nối với database thực:

#### Home/Index.cshtml (Search page):
```csharp
// Bổ sung trong Controller
public IActionResult Index()
{
    var drugs = _context.Drugs.ToList();
    var diseases = _context.Diseases.ToList();
    
    // Truyền dữ liệu thực thay vì mock data
    ViewBag.Drugs = drugs;
    ViewBag.Diseases = diseases;
    
    return View();
}
```

Sau đó trong `.cshtml`, cập nhật Alpine.js data:
```javascript
mockDrugs: @Html.Raw(Json.Serialize(Model.Drugs.Select(d => d.Name))),
mockDiseases: @Html.Raw(Json.Serialize(Model.Diseases.Select(d => d.ICD10Code + " - " + d.Name)))
```

#### Home/Index.cshtml (Check interactions):
```javascript
// Gọi API endpoint thay vì mock data
checkInteractions() {
    fetch('/api/interactions/check', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            drugs: this.selectedDrugs,
            diseases: this.selectedDiseases
        })
    })
    .then(r => r.json())
    .then(data => {
        this.drugDrugInteractions = data.drugDrugInteractions;
        this.drugDiseaseContraindications = data.contraindications;
        this.showResults = true;
    });
}
```

#### Drug/Index.cshtml:
```csharp
@model IEnumerable<Drug>

// Hiện dữ liệu từ database
@foreach(var drug in Model)
{
    <tr>
        <td>@drug.Name</td>
        <td>@drug.ActiveIngredient</td>
        ...
    </tr>
}
```

#### Interaction/Index.cshtml:
```csharp
@model dynamic

// Similar pattern - iterate through real data
@foreach(var interaction in ViewBag.DrugDrugInteractions)
{
    <tr>
        <td>@interaction.Drug1.Name</td>
        <td>@interaction.Drug2.Name</td>
        ...
    </tr>
}
```

### **2. Modal Form Submission**

Ví dụ cho Drug Add/Edit modal:
```javascript
// Trong modal footer
<button class="px-6 py-3 bg-medical-700 text-white..." 
        onclick="saveDrug()">
    <i class="fas fa-save"></i> Lưu
</button>

<script>
function saveDrug() {
    const formData = {
        name: document.querySelector('input[name="drugName"]').value,
        activeIngredient: document.querySelector('input[name="activeIngredient"]').value,
        form: document.querySelector('select[name="form"]').value,
        dosage: document.querySelector('input[name="dosage"]').value,
        instructions: document.querySelector('input[name="instructions"]').value,
        description: document.querySelector('textarea[name="description"]').value,
        status: document.querySelector('select[name="status"]').value,
        code: document.querySelector('input[name="code"]').value
    };
    
    fetch('/Drug/Save', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(formData)
    })
    .then(r => r.json())
    .then(data => {
        if(data.success) {
            closeDrugModal();
            location.reload(); // Refresh table
        }
    });
}
</script>
```

---

## 🎨 Tailwind CSS Tips

### **Responsive Classes**
```css
/* Mobile first */
col-span-1              /* Default */
md:col-span-2           /* Tablet+ */
lg:col-span-3           /* Desktop+ */

/* Breakpoints */
sm:  640px
md:  768px
lg:  1024px
xl:  1280px
2xl: 1536px
```

### **Custom Medical Colors Usage**
```html
<!-- Background -->
<div class="bg-medical-50">Light medical background</div>
<div class="bg-medical-700">Dark medical button</div>

<!-- Text -->
<span class="text-medical-700">Medical text</span>

<!-- Border -->
<div class="border-l-4 border-medical-700">Left border accent</div>

<!-- Danger alerts -->
<div class="bg-red-50 border border-red-200 text-red-800">
    <i class="fas fa-exclamation-triangle text-red-600"></i>
</div>

<!-- Warning alerts -->
<div class="bg-amber-50 border border-amber-200 text-amber-800">
    <i class="fas fa-exclamation-circle text-amber-600"></i>
</div>
```

### **Common Patterns Used**
```html
<!-- Card with shadow -->
<div class="bg-white rounded-xl shadow-lg border border-slate-200">

<!-- Button with hover -->
<button class="bg-medical-700 text-white hover:bg-medical-800 transition-colors">

<!-- Badge/Tag -->
<span class="inline-flex items-center gap-2 bg-green-100 text-green-800 px-3 py-1 rounded-full text-sm font-medium">
    <i class="fas fa-check-circle"></i>
    Status
</span>

<!-- Flex utilities -->
<div class="flex items-center justify-between gap-4">

<!-- Grid -->
<div class="grid md:grid-cols-3 gap-6">

<!-- Gradient -->
<div class="bg-gradient-to-r from-medical-700 to-medical-600">
```

---

## 🔧 Cách Chạy Project

### **1. Chuẩn bị**
```bash
# Trong thư mục project
dotnet restore
dotnet build
```

### **2. Chạy development**
```bash
dotnet run
# Hoặc
dotnet watch run
```

### **3. Truy cập**
```
https://localhost:5001
http://localhost:5000
```

### **4. Kiểm tra Pages**
- Home: `https://localhost:5001/`
- Drug Management: `https://localhost:5001/Drug/Index`
- Interaction Config: `https://localhost:5001/Interaction/Index`

---

## 🚀 Next Steps (Nếu cần tiếp tục)

1. **Tạo Controllers** (DrugController, InteractionController)
2. **Kết nối Database** (Entity Framework)
3. **API Endpoints** (kiểm tra tương tác, CRUD thuốc/bệnh)
4. **Authentication** (Login/Logout)
5. **Import Data** (Từ FDA, WHO databases)
6. **Testing** (Unit tests, Integration tests)
7. **Deployment** (Azure, Docker)

---

## 📚 Tài Liệu Tham Khảo

- **Tailwind CSS**: https://tailwindcss.com/docs
- **Alpine.js**: https://alpinejs.dev/
- **FontAwesome**: https://fontawesome.com/icons
- **ASP.NET Core**: https://docs.microsoft.com/en-us/aspnet/core/
- **Entity Framework Core**: https://docs.microsoft.com/en-us/ef/core/

---

## 💡 Mẹo Thiết Kế

1. **Màu sắc**: Luôn sử dụng medical palette cho consistency
2. **Icons**: FontAwesome có >7000 icons, chọn đúng context
3. **Spacing**: Tailwind margin/padding (8px units): p-4, p-6, gap-4
4. **Typography**: Sử dụng font-semibold cho headings, text-sm cho labels
5. **Responsiveness**: Test trên mobile, tablet, desktop
6. **Accessibility**: Alt text cho images, proper label cho forms
7. **Performance**: CDN CSS/JS đã load nhanh, Alpine.js lightweight

---

**Ghi chú**: Tất cả file được viết theo chuẩn ASP.NET Core MVC với Razor template.
Có thể dễ dàng thêm validation, error handling, và server-side logic sau.

Happy Coding! 🎉
