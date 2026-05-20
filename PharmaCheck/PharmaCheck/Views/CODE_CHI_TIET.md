# 🔍 Giải Thích Code Chi Tiết - PharmaCheck Giao Diện

## 📖 Mục Lục
1. [Layout.cshtml - Giải Thích Chi Tiết](#layoutcshtml)
2. [Home/Index.cshtml - Alpine.js App](#homeindexcshtml)
3. [Drug/Index.cshtml - Table & Modal](#drugindexcshtml)
4. [Interaction/Index.cshtml - Tabs System](#interactionindexcshtml)

---

## 📄 Layout.cshtml - Giải Thích Chi Tiết

### **Phần 1: Tailwind CSS CDN & Custom Colors**

```html
<script src="https://cdn.tailwindcss.com"></script>
<script>
    tailwind.config = {
        theme: {
            extend: {
                colors: {
                    'medical': {
                        50: '#f0f9ff',   /* Rất nhạt - background */
                        100: '#e0f2fe',
                        // ... (các bậc khác)
                        700: '#0369a1',  /* Chủ đạo - buttons, headers */
                        800: '#075985'
                    }
                }
            }
        }
    }
</script>
```

**Giải thích**:
- `tailwind.config`: Mở rộng Tailwind theme
- `extend.colors`: Thêm custom colors
- `medical-700` dùng cho primary buttons, headers
- `medical-50` dùng cho backgrounds nhạt

**Cách sử dụng**:
```html
<button class="bg-medical-700 text-white">  <!-- Xanh medical -->
<div class="bg-medical-50">                  <!-- Nền xanh nhạt -->
<span class="text-medical-700">              <!-- Text xanh -->
```

### **Phần 2: CSS Scrollbar Custom**

```css
*::-webkit-scrollbar {
    width: 8px;
    height: 8px;
}

*::-webkit-scrollbar-track {
    background: rgba(226, 232, 240, 1);  /* Xám nhạt */
}

*::-webkit-scrollbar-thumb {
    background: rgba(3, 105, 161, 0.5);  /* Xanh medical mờ */
    border-radius: 4px;
}
```

**Tác dụng**: Làm scrollbar đẹp hơn, match với color scheme

### **Phần 3: Header Navigation**

```html
<header class="sticky top-0 z-50 bg-white border-b border-slate-200 shadow-sm">
    <nav class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex justify-between items-center h-16">
            <!-- Logo -->
            <a class="flex items-center gap-2 hover:opacity-80 transition-opacity">
                <div class="bg-medical-700 text-white p-2 rounded-lg">
                    <i class="fas fa-pills text-lg"></i>
                </div>
                <span class="text-xl font-bold text-medical-700">PharmaCheck</span>
            </a>

            <!-- Desktop Menu -->
            <div class="hidden md:flex items-center gap-1">
                <!-- Menu items -->
            </div>

            <!-- Mobile Toggle -->
            <button class="md:hidden p-2 rounded-md text-gray-700 hover:bg-medical-50"
                    onclick="document.getElementById('mobileMenu').classList.toggle('hidden')">
                <i class="fas fa-bars text-xl"></i>
            </button>
        </div>
    </nav>
</header>
```

**Phân tích**:
| Phần | Giải thích |
|-----|-----------|
| `sticky top-0 z-50` | Header cố định ở trên, z-index cao |
| `max-w-7xl mx-auto` | Container max width = 80rem, center |
| `hidden md:flex` | Ẩn trên mobile, hiện trên tablet+ |
| `md:hidden` | Ẩn trên tablet+, hiện trên mobile |
| `onclick="..."` | Vanilla JS toggle mobile menu |

### **Phần 4: Navigation Menu Items**

```html
<a asp-area="" asp-controller="Home" asp-action="Index"
   class="px-4 py-2 rounded-md text-gray-700 hover:bg-medical-50 hover:text-medical-700 transition-colors">
    <i class="fas fa-home mr-2"></i>Trang Chủ
</a>
```

**Giải thích**:
- `asp-*`: ASP.NET Core tag helpers (tạo URL)
- `hover:bg-medical-50`: Background xanh nhạt khi hover
- `transition-colors`: Animation smooth cho màu
- `<i class="fas fa-*">`: FontAwesome icon

### **Phần 5: Footer**

```html
<footer class="bg-gray-900 text-gray-100 mt-12">
    <div class="grid md:grid-cols-4 gap-8 mb-8">
        <!-- 4 columns: About, Links, Support, Legal -->
    </div>
    
    <div class="border-t border-gray-800 pt-8">
        <!-- Copyright & Social Icons -->
        <a href="#" class="text-gray-400 hover:text-medical-400 transition-colors">
            <i class="fab fa-facebook text-xl"></i>
        </a>
    </div>
</footer>
```

**Đặc điểm**:
- Nền đen (gray-900)
- 4 cột responsive (md:grid-cols-4)
- Social media icons (fab = Font Awesome Brand)

---

## 🏠 Home/Index.cshtml - Alpine.js App

### **Phần 1: Hero Section**

```html
<section class="bg-gradient-to-r from-medical-700 to-medical-600 text-white py-16">
    <div class="grid md:grid-cols-2 gap-8 items-center">
        <h1 class="text-4xl md:text-5xl font-bold mb-4">
            <i class="fas fa-stethoscope"></i> PharmaCheck
        </h1>
        
        <button onclick="document.getElementById('searchSection').scrollIntoView({behavior: 'smooth'})">
            Bắt Đầu Tra Cứu
        </button>
    </div>
</section>
```

**CSS Classes**:
- `bg-gradient-to-r from-X to-Y`: Gradient từ trái sang phải
- `text-4xl md:text-5xl`: Responsive text size (16px → 48px)
- `mb-4`: Margin bottom 16px
- `scrollIntoView({behavior: 'smooth'})`: Smooth scroll JS

### **Phần 2: Alpine.js App Setup**

```html
<div x-data="searchApp()" class="space-y-6">
    <!-- Search inputs -->
</div>

<script>
function searchApp() {
    return {
        // State
        drugSearch: '',
        diseaseSearch: '',
        selectedDrugs: [],
        selectedDiseases: [],
        
        // Methods
        filterDrugs() {
            // Logic...
        },
        
        selectDrug(drug) {
            if (!this.selectedDrugs.includes(drug)) {
                this.selectedDrugs.push(drug);
            }
        },
        
        checkInteractions() {
            // Mock or API call
            this.showResults = true;
        }
    }
}
</script>
```

**Alpine.js Concepts**:
| Directive | Tác dụng |
|-----------|---------|
| `x-data="app()"` | Khởi tạo component |
| `x-model="variable"` | Two-way binding |
| `x-show="boolean"` | Show/hide element |
| `x-for="item in list"` | Loop items |
| `@input="method()"` | Event listener |
| `@click="method()"` | Click handler |

### **Phần 3: Drug Search Input**

```html
<input type="text" 
       x-model="drugSearch"
       @input="filterDrugs()"
       placeholder="Nhập tên thuốc..."
       class="w-full px-4 py-3 border-2 border-slate-300 rounded-lg focus:outline-none focus:border-medical-600 transition-colors">
<i class="fas fa-pills absolute right-4 top-4 text-medical-600"></i>
```

**Giải thích**:
- `x-model="drugSearch"`: Two-way binding → update khi user type
- `@input="filterDrugs()"`: Gọi hàm filter khi input thay đổi
- Relative positioning: `relative` + `absolute` cho icon
- Focus styles: `focus:outline-none focus:border-medical-600`

### **Phần 4: Dropdown Suggestions**

```html
<div x-show="showDrugDropdown && filteredDrugs.length > 0" 
     class="absolute top-full left-0 right-0 mt-2 bg-white border-2 border-medical-300 rounded-lg shadow-lg z-20 max-h-64 overflow-y-auto">
    <template x-for="drug in filteredDrugs" :key="drug">
        <button type="button"
                @click="selectDrug(drug); showDrugDropdown = false"
                class="w-full text-left px-4 py-3 hover:bg-medical-50 transition-colors">
            <i class="fas fa-pills text-medical-600 mr-2"></i>
            <span x-text="drug"></span>
        </button>
    </template>
</div>
```

**Alpine.js Features**:
- `x-show`: Ẩn/hiện dựa điều kiện boolean
- `x-for="drug in filteredDrugs"`: Loop danh sách
- `x-text="drug"`: Hiển thị text dynamic
- `:key="drug"`: Unique identifier cho items
- `@click="selectDrug(drug)"`: Click handler

### **Phần 5: Selected Tags**

```html
<div x-show="selectedDrugs.length > 0" class="mt-4 flex flex-wrap gap-2">
    <template x-for="drug in selectedDrugs" :key="drug">
        <span class="inline-flex items-center gap-2 bg-medical-100 text-medical-800 px-4 py-2 rounded-full text-sm font-medium">
            <i class="fas fa-check-circle"></i>
            <span x-text="drug"></span>
            <button type="button" @click="removeDrug(drug)" class="hover:text-medical-600">
                <i class="fas fa-times"></i>
            </button>
        </span>
    </template>
</div>
```

**Giải thích**:
- `inline-flex`: Flex items inline
- `rounded-full`: Hoàn toàn tròn (border-radius 9999px)
- `gap-2`: Spacing giữa các elements trong tag
- `hover:text-medical-600`: Màu khi hover X button

### **Phần 6: Results Section**

```html
<section id="resultsSection" class="py-12 bg-slate-50" x-show="showResults">
    <div class="grid md:grid-cols-2 gap-6">
        <!-- Drug-Drug Interactions -->
        <div class="bg-white rounded-xl shadow-lg border border-slate-200 overflow-hidden">
            <div class="bg-medical-700 text-white px-6 py-4">
                <h3>Tương Tác Thuốc - Thuốc</h3>
            </div>
            
            <div class="p-6 space-y-4">
                <template x-for="interaction in drugDrugInteractions">
                    <div :class="'p-4 rounded-lg border-l-4 ' + interaction.severityClass">
                        <h4 class="font-semibold" x-text="interaction.title"></h4>
                        <p class="text-sm text-gray-600" x-text="interaction.description"></p>
                        <div class="mt-3 p-3 bg-slate-50 rounded text-sm">
                            <strong>Khuyến cáo:</strong>
                            <span x-text="interaction.recommendation"></span>
                        </div>
                    </div>
                </template>
            </div>
        </div>
    </div>
    
    <!-- Overall Status -->
    <div :class="overallStatus.class" class="p-6 rounded-xl text-center border-2">
        <i :class="'fas ' + overallStatus.icon"></i>
        <h3 x-text="overallStatus.message"></h3>
    </div>
</section>
```

**Dynamic Classes**:
- `:class="interaction.severityClass"`: Bind CSS class từ object
- Ví dụ: `interaction.severityClass = 'border-red-500 bg-red-50'`

### **Phần 7: checkInteractions() Method**

```javascript
checkInteractions() {
    this.drugDrugInteractions = [];
    this.drugDiseaseContraindications = [];
    
    // Simulate drug-drug interactions
    if (this.selectedDrugs.length >= 2) {
        // Check Aspirin + Warfarin
        if ((this.selectedDrugs.some(d => d.includes('Aspirin')) && 
             this.selectedDrugs.some(d => d.includes('Warfarin')))) {
            this.drugDrugInteractions.push({
                id: 1,
                title: 'Aspirin + Warfarin - NGUY HIỂM',
                description: 'Kết hợp 2 thuốc chống đông máu có nguy cơ chảy máu cao',
                icon: 'fa-exclamation-triangle',
                severityClass: 'border-red-500 bg-red-50',
                recommendation: 'Sử dụng liều thấp hoặc đổi sang thuốc khác...'
            });
        }
    }
    
    // Simulate drug-disease contraindications
    if (this.selectedDrugs.length > 0 && this.selectedDiseases.length > 0) {
        // Check Ibuprofen + N18
        if (this.selectedDrugs.some(d => d.includes('Ibuprofen')) && 
            this.selectedDiseases.some(d => d.includes('N18'))) {
            this.drugDiseaseContraindications.push({
                id: 1,
                title: 'Ibuprofen + Bệnh thận mạn tính - CHỐNG CHỈ ĐỊNH',
                description: 'NSAID có thể làm xấu thêm chức năng thận',
                icon: 'fa-ban',
                severityClass: 'border-red-500 bg-red-50',
                recommendation: 'TUYỆT ĐỐI không dùng. Thay thế bằng Paracetamol...'
            });
        }
    }
    
    this.showResults = true;
}
```

**Logic**:
1. Xóa dữ liệu cũ
2. Kiểm tra nếu ≥2 thuốc → tìm tương tác
3. Kiểm tra nếu có thuốc + bệnh → tìm chống chỉ định
4. Push vào array
5. Hiển thị results

### **Phần 8: overallStatus Getter**

```javascript
get overallStatus() {
    const hasWarnings = this.drugDrugInteractions.length > 0 || 
                       this.drugDiseaseContraindications.length > 0;
    
    if (!hasWarnings) {
        return {
            class: 'border-green-300 bg-green-50 text-green-800',
            icon: 'fa-check-circle',
            message: '✓ An Toàn',
            detail: 'Kết hợp thuốc và bệnh lý được kiểm tra là an toàn...'
        };
    }
    
    const hasDangers = this.drugDrugInteractions.some(i => i.severityClass.includes('red')) ||
                      this.drugDiseaseContraindications.some(c => c.severityClass.includes('red'));
    
    if (hasDangers) {
        return {
            class: 'border-red-300 bg-red-50 text-red-800',
            icon: 'fa-exclamation-triangle',
            message: '⚠ NGUY HIỂM',
            detail: 'Phát hiện tương tác nguy hiểm hoặc chống chỉ định...'
        };
    }
    
    return {
        class: 'border-amber-300 bg-amber-50 text-amber-800',
        icon: 'fa-exclamation-circle',
        message: '! Cần Thận Trọng',
        detail: 'Có tương tác cần theo dõi...'
    };
}
```

**Getter trong Alpine.js**:
- `get overallStatus()`: Tính toán động
- `this.drugDrugInteractions.some()`: Kiểm tra nếu có interaction nguy hiểm
- Trả về object với class, icon, message

---

## 💊 Drug/Index.cshtml - Table & Modal

### **Phần 1: Search & Filter Bar**

```html
<div class="bg-white rounded-xl shadow-lg border border-slate-200 p-6 mb-8">
    <div class="grid md:grid-cols-3 gap-4">
        <!-- Search input -->
        <div class="md:col-span-2">
            <div class="relative">
                <input type="text"
                       id="drugSearch"
                       placeholder="Tìm kiếm thuốc..."
                       class="w-full px-4 py-3 border-2 border-slate-300 rounded-lg focus:outline-none focus:border-medical-600 transition-colors">
                <i class="fas fa-search absolute right-4 top-4 text-medical-600"></i>
            </div>
        </div>
        
        <!-- Filter dropdown -->
        <div>
            <select id="statusFilter" class="w-full px-4 py-3 border-2 border-slate-300 rounded-lg focus:outline-none focus:border-medical-600">
                <option value="">Tất cả Trạng Thái</option>
                <option value="active">Đang Hoạt Động</option>
                <option value="inactive">Ngừng Hoạt Động</option>
                <option value="restricted">Hạn Chế</option>
            </select>
        </div>
    </div>
</div>
```

**Event Listeners**:
```javascript
document.getElementById('drugSearch').addEventListener('keyup', function(e) {
    console.log('Tìm kiếm: ' + e.target.value);
    // Gọi API filter
});

document.getElementById('statusFilter').addEventListener('change', function(e) {
    console.log('Lọc theo: ' + e.target.value);
    // Gọi API filter
});
```

### **Phần 2: Table Header**

```html
<table class="w-full">
    <thead class="bg-medical-700 text-white">
        <tr>
            <th class="px-6 py-4 text-left font-semibold text-sm">
                <i class="fas fa-pills mr-2"></i>Tên Thuốc
            </th>
            <th class="px-6 py-4 text-left font-semibold text-sm">
                <i class="fas fa-flask mr-2"></i>Hoạt Chất
            </th>
            <!-- ... -->
        </tr>
    </thead>
</table>
```

**Classes**:
- `bg-medical-700 text-white`: Header nền xanh, text trắng
- `px-6 py-4`: Padding 24px ngang, 16px dọc
- `text-left`: Text align left
- `font-semibold`: Font weight 600

### **Phần 3: Table Body Row**

```html
<tbody class="divide-y divide-slate-200">
    <tr class="hover:bg-slate-50 transition-colors">
        <td class="px-6 py-4">
            <div class="font-semibold text-gray-900">Amoxicillin 500mg</div>
            <div class="text-sm text-gray-500">Mã: AMX-500</div>
        </td>
        
        <td class="px-6 py-4">
            <span class="inline-block bg-blue-100 text-blue-800 px-3 py-1 rounded-full text-sm font-medium">
                Amoxicillin Trihydrate
            </span>
        </td>
        
        <td class="px-6 py-4">
            <span class="inline-flex items-center gap-2 bg-green-100 text-green-800 px-3 py-1 rounded-full text-sm font-medium">
                <i class="fas fa-check-circle"></i>Đang Hoạt Động
            </span>
        </td>
        
        <td class="px-6 py-4">
            <div class="flex justify-center gap-2">
                <button onclick="editDrug()" class="bg-blue-100 text-blue-700 hover:bg-blue-200 p-2 rounded-lg transition-colors">
                    <i class="fas fa-edit"></i>
                </button>
                <button onclick="viewDrug()" class="bg-gray-100 text-gray-700 hover:bg-gray-200 p-2 rounded-lg">
                    <i class="fas fa-eye"></i>
                </button>
                <button onclick="confirmDeleteDrug('Amoxicillin 500mg')" class="bg-red-100 text-red-700 hover:bg-red-200 p-2 rounded-lg">
                    <i class="fas fa-trash"></i>
                </button>
            </div>
        </td>
    </tr>
</tbody>
```

**Giải thích**:
- `divide-y divide-slate-200`: Đường kẻ giữa rows
- `hover:bg-slate-50`: Highlight row khi hover
- `inline-block` hoặc `inline-flex`: Badge/chip styling
- `px-3 py-1 rounded-full`: Chip/badge appearance

### **Phần 4: Modal - Add/Edit Drug**

```html
<div id="drugModal" class="hidden fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
    <div class="bg-white rounded-xl shadow-2xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
        <!-- Header -->
        <div class="sticky top-0 bg-medical-700 text-white px-6 py-4 flex items-center justify-between">
            <h2 class="text-2xl font-bold flex items-center gap-3">
                <i class="fas fa-pills"></i>
                <span id="modalTitle">Thêm Thuốc Mới</span>
            </h2>
            <button onclick="closeDrugModal()" class="text-white hover:bg-medical-600 p-2 rounded-lg">
                <i class="fas fa-times text-xl"></i>
            </button>
        </div>

        <!-- Body -->
        <div class="p-6 space-y-4">
            <!-- Form fields -->
            <div>
                <label class="block text-sm font-semibold text-gray-900 mb-2">
                    <i class="fas fa-pills text-medical-700 mr-2"></i>Tên Thuốc
                </label>
                <input type="text" placeholder="VD: Amoxicillin 500mg"
                       class="w-full px-4 py-3 border-2 border-slate-300 rounded-lg focus:outline-none focus:border-medical-600 transition-colors">
            </div>

            <div class="grid md:grid-cols-2 gap-4">
                <div>
                    <label class="block text-sm font-semibold text-gray-900 mb-2">
                        <i class="fas fa-flask text-medical-700 mr-2"></i>Hoạt Chất
                    </label>
                    <input type="text" placeholder="VD: Amoxicillin Trihydrate"
                           class="w-full px-4 py-3 border-2 border-slate-300 rounded-lg focus:outline-none focus:border-medical-600">
                </div>
                
                <div>
                    <label class="block text-sm font-semibold text-gray-900 mb-2">
                        <i class="fas fa-capsules text-medical-700 mr-2"></i>Dạng Bào Chế
                    </label>
                    <select class="w-full px-4 py-3 border-2 border-slate-300 rounded-lg focus:outline-none focus:border-medical-600">
                        <option>Chọn dạng bào chế</option>
                        <option>Viên</option>
                        <option>Siro</option>
                    </select>
                </div>
            </div>

            <!-- Textarea for description -->
            <div>
                <label class="block text-sm font-semibold text-gray-900 mb-2">
                    <i class="fas fa-align-left text-medical-700 mr-2"></i>Mô Tả / Công Dụng
                </label>
                <textarea rows="4" placeholder="Nhập mô tả..."
                          class="w-full px-4 py-3 border-2 border-slate-300 rounded-lg resize-none"></textarea>
            </div>
        </div>

        <!-- Footer -->
        <div class="sticky bottom-0 bg-slate-50 border-t border-slate-200 px-6 py-4 flex gap-3 justify-end">
            <button onclick="closeDrugModal()"
                    class="px-6 py-3 border-2 border-slate-300 text-gray-700 rounded-lg hover:bg-slate-50 transition-colors">
                Hủy
            </button>
            <button class="px-6 py-3 bg-medical-700 text-white rounded-lg hover:bg-medical-800 transition-colors flex items-center gap-2">
                <i class="fas fa-save"></i> Lưu
            </button>
        </div>
    </div>
</div>
```

**Modal CSS**:
| Class | Tác dụng |
|-------|---------|
| `hidden` | Display none (ẩn modal) |
| `fixed inset-0` | Full screen overlay |
| `bg-black bg-opacity-50` | Overlay mờ đen 50% |
| `flex items-center justify-center` | Center modal |
| `z-50` | Lớp cao nhất |
| `max-h-[90vh] overflow-y-auto` | Scroll nếu quá cao |
| `sticky top-0` | Header dính trên cùng |

**JavaScript Toggle**:
```javascript
function openDrugModal() {
    document.getElementById('drugModal').classList.remove('hidden');
    document.getElementById('modalTitle').textContent = 'Thêm Thuốc Mới';
}

function closeDrugModal() {
    document.getElementById('drugModal').classList.add('hidden');
}

// Close when click outside
document.addEventListener('click', function(e) {
    const modal = document.getElementById('drugModal');
    if (e.target === modal) {
        modal.classList.add('hidden');
    }
});
```

### **Phần 5: Delete Confirmation Modal**

```html
<div id="deleteModal" class="hidden fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
    <div class="bg-white rounded-xl shadow-2xl max-w-md w-full">
        <div class="p-6">
            <div class="flex items-center justify-center w-12 h-12 mx-auto bg-red-100 rounded-full mb-4">
                <i class="fas fa-exclamation-triangle text-red-600 text-xl"></i>
            </div>
            <h3 class="text-xl font-bold text-center text-gray-900 mb-2">Xác Nhận Xóa</h3>
            <p class="text-center text-gray-600 mb-6">
                Bạn chắc chắn muốn xóa <span id="deleteItemName" class="font-semibold"></span>?
            </p>
        </div>
        <div class="bg-slate-50 border-t border-slate-200 px-6 py-4 flex gap-3 justify-end">
            <button onclick="closeDeleteModal()" class="px-6 py-2 border border-slate-300 rounded-lg">
                Hủy
            </button>
            <button class="px-6 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 flex items-center gap-2">
                <i class="fas fa-trash"></i> Xóa
            </button>
        </div>
    </div>
</div>
```

**Giải thích**:
- Overlay giống add/edit modal
- Icon cảnh báo đỏ
- 2 buttons: Cancel, Delete
- `deleteItemName` được update từ JS

---

## ⚠️ Interaction/Index.cshtml - Tabs System

### **Phần 1: Tab Navigation**

```html
<div class="flex gap-4 mb-8 border-b-2 border-slate-200">
    <button onclick="switchTab('drugdrug')"
            id="tab-drugdrug"
            class="px-6 py-4 text-lg font-semibold border-b-4 border-amber-600 text-amber-700 transition-colors flex items-center gap-2">
        <i class="fas fa-pills"></i> Tương Tác Thuốc-Thuốc
    </button>
    <button onclick="switchTab('drugdisease')"
            id="tab-drugdisease"
            class="px-6 py-4 text-lg font-semibold border-b-4 border-transparent text-gray-600 hover:text-gray-900 transition-colors flex items-center gap-2">
        <i class="fas fa-heartbeat"></i> Chống Chỉ Định Thuốc-Bệnh
    </button>
</div>
```

**Tab Styling**:
- Active tab: `border-b-4 border-amber-600 text-amber-700`
- Inactive tab: `border-b-4 border-transparent text-gray-600`
- Hover: `hover:text-gray-900`

**JavaScript Switching**:
```javascript
function switchTab(tab) {
    const drugdrug = document.getElementById('drugdrug-content');
    const drugdisease = document.getElementById('drugdisease-content');
    const tabDrugDrug = document.getElementById('tab-drugdrug');
    const tabDrugDisease = document.getElementById('tab-drugdisease');

    if (tab === 'drugdrug') {
        drugdrug.classList.remove('hidden');
        drugdisease.classList.add('hidden');
        
        // Update active tab styling
        tabDrugDrug.classList.add('border-amber-600', 'text-amber-700');
        tabDrugDrug.classList.remove('border-transparent', 'text-gray-600');
        
        tabDrugDisease.classList.remove('border-red-600', 'text-red-700');
        tabDrugDisease.classList.add('border-transparent', 'text-gray-600');
    } else {
        // Similar logic for drugdisease tab
    }
}
```

### **Phần 2: Tab Content Divs**

```html
<!-- TAB 1 -->
<div id="drugdrug-content" class="space-y-6">
    <!-- Search & Filter -->
    <!-- Table -->
    <!-- Pagination -->
</div>

<!-- TAB 2 (hidden by default) -->
<div id="drugdisease-content" class="hidden space-y-6">
    <!-- Search & Filter -->
    <!-- Table -->
    <!-- Pagination -->
</div>
```

**Classes**:
- `hidden`: Ẩn tab 2 mặc định
- `space-y-6`: Khoảng cách 24px giữa child elements

### **Phần 3: Table Color Coding**

```html
<!-- Dangerous interaction - Red -->
<span class="inline-flex items-center gap-2 bg-red-100 text-red-800 px-3 py-1 rounded-full text-sm font-bold">
    <i class="fas fa-exclamation-triangle"></i>NGUY HIỂM
</span>

<!-- Warning interaction - Amber -->
<span class="inline-flex items-center gap-2 bg-amber-100 text-amber-800 px-3 py-1 rounded-full text-sm font-bold">
    <i class="fas fa-exclamation-circle"></i>THẬN TRỌNG
</span>

<!-- Safe interaction - Green -->
<span class="inline-flex items-center gap-2 bg-green-100 text-green-800 px-3 py-1 rounded-full text-sm font-bold">
    <i class="fas fa-check-circle"></i>AN TOÀN
</span>
```

**Color Mapping**:
| Trạng Thái | BG | Text | Icon |
|-----------|----|----|------|
| Nguy Hiểm | bg-red-100 | text-red-800 | fa-exclamation-triangle |
| Thận Trọng | bg-amber-100 | text-amber-800 | fa-exclamation-circle |
| An Toàn | bg-green-100 | text-green-800 | fa-check-circle |

### **Phần 4: Interaction Modal**

```html
<div id="interactionModal" class="hidden fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
    <div class="bg-white rounded-xl shadow-2xl max-w-3xl w-full max-h-[90vh] overflow-y-auto">
        <!-- Header -->
        <div class="sticky top-0 bg-amber-600 text-white px-6 py-4 flex items-center justify-between">
            <h2 class="text-2xl font-bold">
                <i class="fas fa-exchange-alt"></i>
                <span id="interactionModalTitle">Thêm Tương Tác Mới</span>
            </h2>
            <button onclick="closeInteractionModal()" class="text-white hover:bg-amber-700 p-2 rounded-lg">
                <i class="fas fa-times text-xl"></i>
            </button>
        </div>

        <!-- Body - Form -->
        <div class="p-6 space-y-4">
            <!-- Type Selection -->
            <div>
                <label class="block text-sm font-semibold text-gray-900 mb-2">
                    <i class="fas fa-list text-amber-600 mr-2"></i>Loại Tương Tác
                </label>
                <select class="w-full px-4 py-3 border-2 border-slate-300 rounded-lg focus:border-amber-600">
                    <option>Tương Tác Thuốc-Thuốc</option>
                    <option>Chống Chỉ Định Thuốc-Bệnh</option>
                </select>
            </div>

            <!-- Drug Selections -->
            <div>
                <label class="block text-sm font-semibold text-gray-900 mb-2">
                    <i class="fas fa-pills text-amber-600 mr-2"></i>Thuốc 1
                </label>
                <input type="text" placeholder="Tìm và chọn thuốc..." class="w-full px-4 py-3 border-2 border-slate-300 rounded-lg">
            </div>

            <!-- Severity Selection -->
            <div class="grid md:grid-cols-2 gap-4">
                <div>
                    <label class="block text-sm font-semibold text-gray-900 mb-2">
                        <i class="fas fa-alert text-amber-600 mr-2"></i>Mức Độ Nguy Hiểm
                    </label>
                    <select class="w-full px-4 py-3 border-2 border-slate-300 rounded-lg">
                        <option style="color: #dc2626;">Nguy Hiểm (Đỏ) - Chống Chỉ Định Tuyệt Đối</option>
                        <option style="color: #f59e0b;">Thận Trọng (Vàng) - Cần Giám Sát</option>
                        <option style="color: #10b981;">An Toàn (Xanh) - Có Thể Dùng</option>
                    </select>
                </div>

                <!-- Status -->
                <div>
                    <label class="block text-sm font-semibold text-gray-900 mb-2">
                        <i class="fas fa-circle text-amber-600 mr-2"></i>Trạng Thái
                    </label>
                    <select class="w-full px-4 py-3 border-2 border-slate-300 rounded-lg">
                        <option>Đang Hoạt Động</option>
                        <option>Vô Hiệu Hóa</option>
                    </select>
                </div>
            </div>

            <!-- Textareas -->
            <div>
                <label class="block text-sm font-semibold text-gray-900 mb-2">
                    <i class="fas fa-align-left text-amber-600 mr-2"></i>Cơ Chế / Lý Do Tương Tác
                </label>
                <textarea rows="4" placeholder="Mô tả chi tiết..." class="w-full px-4 py-3 border-2 border-slate-300 rounded-lg resize-none"></textarea>
            </div>

            <div>
                <label class="block text-sm font-semibold text-gray-900 mb-2">
                    <i class="fas fa-lightbulb text-amber-600 mr-2"></i>Khuyến Cáo / Cách Xử Lý
                </label>
                <textarea rows="4" placeholder="Hướng dẫn bác sĩ..." class="w-full px-4 py-3 border-2 border-slate-300 rounded-lg resize-none"></textarea>
            </div>
        </div>

        <!-- Footer -->
        <div class="sticky bottom-0 bg-slate-50 border-t border-slate-200 px-6 py-4 flex gap-3 justify-end">
            <button onclick="closeInteractionModal()" class="px-6 py-3 border-2 border-slate-300 rounded-lg">
                Hủy
            </button>
            <button class="px-6 py-3 bg-amber-600 text-white rounded-lg hover:bg-amber-700 flex items-center gap-2">
                <i class="fas fa-save"></i> Lưu
            </button>
        </div>
    </div>
</div>
```

---

## 🔗 Liên Kết CSS Classes Thường Dùng

```css
/* Sizing */
w-full          /* width: 100% */
max-w-7xl       /* max-width: 80rem */
h-16            /* height: 64px */
max-h-[90vh]    /* max-height: 90vh */

/* Spacing */
p-4, p-6        /* padding */
px-4, py-3      /* horizontal/vertical padding */
m-4             /* margin */
gap-2, gap-4    /* gap between flex/grid items */

/* Display & Positioning */
flex            /* display: flex */
grid            /* display: grid */
hidden          /* display: none */
sticky          /* position: sticky */
fixed           /* position: fixed */
absolute        /* position: absolute */
relative        /* position: relative */

/* Colors */
bg-medical-700  /* background color */
text-gray-900   /* text color */
border-slate-200/* border color */

/* Borders & Radius */
rounded-lg      /* border-radius: 8px */
rounded-full    /* border-radius: 9999px */
border-b-4      /* border-bottom: 4px */

/* Responsive */
md:col-span-2   /* On medium+ screens */
hidden md:flex  /* Hidden by default, flex on md+ */

/* States */
hover:bg-blue-200       /* Background on hover */
focus:outline-none      /* No outline on focus */
transition-colors       /* Smooth color transition */
```

---

## 🚀 Bước Tiếp Theo: Kết Nối Backend

Để kết nối thực tế:

1. **Tạo Controllers**:
```csharp
public class DrugController : Controller
{
    private readonly ApplicationDbContext _context;
    
    public IActionResult Index()
    {
        var drugs = _context.Drugs.ToList();
        return View(drugs);
    }
}
```

2. **Update Views**:
```csharp
@model IEnumerable<Drug>

@foreach(var drug in Model)
{
    <tr>
        <td>@drug.Name</td>
        <td>@drug.ActiveIngredient</td>
    </tr>
}
```

3. **Create API Endpoints**:
```csharp
[ApiController]
[Route("api/[controller]")]
public class InteractionsController : ControllerBase
{
    [HttpPost("check")]
    public IActionResult CheckInteractions([FromBody] CheckRequest request)
    {
        // Logic to check interactions
        return Ok(result);
    }
}
```

---

**Tài liệu này cung cấp giải thích chi tiết về mỗi component. Happy coding! 🎉**
