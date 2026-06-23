/**
 * PharmaCheck - Hệ thống quản lý danh mục thuốc (Mã nguồn Client-side)
 */

// ==========================================
// KHAI BÁO BIẾN TOÀN CỤC (GLOBAL VARIABLES)
// ==========================================
let currentDeleteId = 0;      
let currentDeleteName = '';    

// Window.IS_ADMIN được khởi tạo từ Index.cshtml

// ==========================================
// QUẢN LÝ MODAL THÊM/SỬA THUỐC
// ==========================================
function openDrugModal() {
    if (!window.IS_ADMIN) return; 
    resetForm(); 
    document.getElementById('drugModal')?.classList.remove('hidden'); 
    document.getElementById('modalTitle').textContent = 'Thêm Thuốc Mới'; 
    document.getElementById('saveBtnText').textContent = 'Lưu'; 
}

function editDrug(id) {
    if (!window.IS_ADMIN) {
        alert('Bạn không có quyền chỉnh sửa thông tin thuốc.');
        return;
    }
    if (!id || id === 0) return;

    fetch(`/Drug/GetDrugById?id=${id}`)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                fillFormWithData(data.data); 
                document.getElementById('drugModal')?.classList.remove('hidden'); 
                document.getElementById('modalTitle').textContent = 'Chỉnh Sửa Thuốc'; 
                document.getElementById('saveBtnText').textContent = 'Cập Nhật'; 
            } else {
                alert('Lỗi: ' + data.message);
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Lỗi khi lấy dữ liệu thuốc từ hệ thống');
        });
}

/**
 * Xem chi tiết thông tin thuốc (Cả Admin và User đều sử dụng được)
 */
function viewDrug(id) {
    if (!id || id === 0) return;
    
    fetch(`/Drug/GetDrugById?id=${id}`)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                fillFormWithData(data.data);
                
                const modal = document.getElementById('drugModal');
                const saveBtn = document.getElementById('saveDrugBtn');
                
                if (modal) {
                    modal.classList.remove('hidden');
                    document.getElementById('modalTitle').textContent = 'Chi Tiết Thông Tin Thuốc';
                    
                    if (saveBtn) {
                        if (!window.IS_ADMIN) {
                            saveBtn.classList.add('hidden');
                        } else {
                            saveBtn.classList.remove('hidden');
                            document.getElementById('saveBtnText').textContent = 'Cập Nhật';
                        }
                    }
                }
            } else {
                alert('Lỗi: ' + data.message);
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Khởi tạo thông tin thất bại.');
        });
}

function closeDrugModal() {
    const modal = document.getElementById('drugModal');
    if (modal) {
        modal.classList.add('hidden');
        resetForm();
    }
}

// ==========================================
// CHỨC NĂNG IMPORT THUỐC TỪ FILE EXCEL (NEW ⭐)
// ==========================================
function uploadExcelFile() {
    if (!window.IS_ADMIN) return;

    const fileInput = document.getElementById('excelFileInput');
    if (!fileInput || fileInput.files.length === 0) {
        alert('Vui lòng chọn một file Excel (.xlsx) trước khi bấm nạp!');
        return;
    }

    const file = fileInput.files[0];
    const formData = new FormData();
    formData.append('excelFile', file);

    // Lấy token bảo mật Antiforgery được sinh ra ở trang Index
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    const uploadBtn = document.getElementById('uploadExcelBtn');
    const originalBtnHtml = uploadBtn.innerHTML;
    
    // Đổi trạng thái hiển thị loading nút bấm
    uploadBtn.disabled = true;
    uploadBtn.innerHTML = '<i class="fas fa-spinner fa-spin mr-1"></i> Đang xử lý...';

    fetch('/Drug/ImportFromExcel', {
        method: 'POST',
        body: formData,
        headers: {
            'RequestVerificationToken': token || ''
        }
    })
    .then(res => {
        if (!res.ok) throw new Error(`HTTP error! status: ${res.status}`);
        return res.json();
    })
    .then(res => {
        uploadBtn.disabled = false;
        uploadBtn.innerHTML = originalBtnHtml;

        if (res.success) {
            alert(res.message);
            window.location.reload(); // Thao tác thành công, reload để cập nhật Grid
        } else {
            alert('Lỗi: ' + res.message);
        }
    })
    .catch(err => {
        console.error("Error detailed:", err);
        uploadBtn.disabled = false;
        uploadBtn.innerHTML = originalBtnHtml;
        alert('Đã xảy ra lỗi kết nối đường truyền internet hoặc hệ thống Server gặp sự cố.');
    });
}

// ==========================================
// QUẢN LÝ MODAL XÁC NHẬN XÓA
// ==========================================
function confirmDeleteDrug(id, drugName) {
    if (!window.IS_ADMIN) return;
    if (!id || id === 0) return;
    currentDeleteId = id; 
    currentDeleteName = drugName;
    document.getElementById('deleteItemName').textContent = drugName; 
    document.getElementById('deleteModal')?.classList.remove('hidden'); 
}

function closeDeleteModal() {
    if (!window.IS_ADMIN) return;
    document.getElementById('deleteModal')?.classList.add('hidden'); 
    currentDeleteId = 0; 
    currentDeleteName = '';
}

// Sự kiện click đóng Modal khi click ra ngoài vùng trống
document.addEventListener('click', function (e) {
    const drugModal = document.getElementById('drugModal');
    const deleteModal = document.getElementById('deleteModal');

    if (e.target === drugModal) {
        drugModal.classList.add('hidden');
        resetForm();
    }
    if (window.IS_ADMIN && e.target === deleteModal) {
        deleteModal.classList.add('hidden');
    }
});

// ==========================================
// QUẢN LÝ FORM VÀ ĐỒNG BỘ DỮ LIỆU
// ==========================================
function resetForm() {
    const form = document.getElementById('drugForm');
    if (!form) return;
    
    form.reset(); 
    if(document.getElementById('drugId')) document.getElementById('drugId').value = '0'; 
    if(document.getElementById('statusActive')) document.getElementById('statusActive').checked = true; 
    document.getElementById('formMessage')?.classList.add('hidden');
    
    const saveBtn = document.getElementById('saveDrugBtn');
    if (saveBtn && window.IS_ADMIN) saveBtn.classList.remove('hidden');
}

function fillFormWithData(data) {
    if (!document.getElementById('drugForm')) return;

    if(document.getElementById('drugId')) document.getElementById('drugId').value = data.id ?? data.Id ?? 0;
    if(document.getElementById('drugName')) document.getElementById('drugName').value = data.name ?? data.Name ?? '';
    if(document.getElementById('activeIngredient')) document.getElementById('activeIngredient').value = data.activeIngredient ?? data.ActiveIngredient ?? '';
    if(document.getElementById('dosage')) document.getElementById('dosage').value = data.dosage ?? data.Dosage ?? '';
    if(document.getElementById('manufacturer')) document.getElementById('manufacturer').value = data.manufacturer ?? data.Manufacturer ?? '';
    
    const inputFunction = document.getElementById('drugUse') || document.getElementById('function');
    if (inputFunction) inputFunction.value = data.function ?? data.Function ?? '';

    if(document.getElementById('sideEffects')) document.getElementById('sideEffects').value = data.sideEffects ?? data.SideEffects ?? '';
    if(document.getElementById('contraindications')) document.getElementById('contraindications').value = data.contraindications ?? data.Contraindications ?? '';
    if(document.getElementById('description')) document.getElementById('description').value = data.description ?? data.Description ?? '';

    const isActiveVal = (data.isActive !== undefined) ? data.isActive : data.IsActive;
    if (isActiveVal) {
        if(document.getElementById('statusActive')) document.getElementById('statusActive').checked = true;
    } else {
        if(document.getElementById('statusInactive')) document.getElementById('statusInactive').checked = true;
    }
}

async function saveDrug() {
    const saveBtn = document.getElementById('saveDrugBtn');
    const btnText = document.getElementById('saveBtnText');
    const msgDiv = document.getElementById('formMessage');

    const drugData = {
        Id: parseInt(document.getElementById('drugId')?.value) || 0,
        Name: document.getElementById('drugName')?.value?.trim() || "",
        ActiveIngredient: document.getElementById('activeIngredient')?.value?.trim() || "",
        Dosage: document.getElementById('dosage')?.value?.trim() || "",
        Manufacturer: document.getElementById('manufacturer')?.value?.trim() || "",
        Function: (document.getElementById('drugUse') || document.getElementById('function'))?.value?.trim() || "",
        SideEffects: document.getElementById('sideEffects')?.value?.trim() || "",
        Contraindications: document.getElementById('contraindications')?.value?.trim() || "",
        Description: document.getElementById('description')?.value?.trim() || "",
        IsActive: document.getElementById('statusActive')?.checked || false
    };

    if (!drugData.Name || !drugData.ActiveIngredient || !drugData.Dosage || !drugData.Function) {
        showMsg("Vui lòng điền đầy đủ các thông tin thuốc bắt buộc (*)", "bg-amber-100 text-amber-800 border-amber-200");
        return;
    }

    if (saveBtn) saveBtn.disabled = true;
    if (btnText) btnText.innerText = "Đang lưu...";
    if (msgDiv) msgDiv.classList.add('hidden');

    try {
        const response = await fetch('/Drug/SaveDrug', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            },
            body: JSON.stringify(drugData)
        });

        if (response.ok) {
            showMsg("🎉 Cập nhật thông tin thuốc thành công!", "bg-green-100 text-green-800 border-green-200");
            setTimeout(() => { location.reload(); }, 1200);
        } else {
            const errorResult = await response.text();
            console.error("🔴 Chi tiết lỗi ModelState từ Back-end C# trả về:\n", errorResult);
            showMsg("❌ Không thể lưu dữ liệu. Vui lòng kiểm tra lại cấu hình hoặc xem tab Console!", "bg-red-100 text-red-800 border-red-200");
            if (saveBtn) saveBtn.disabled = false;
            if (btnText) btnText.innerText = "Lưu";
        }
    } catch (error) {
        console.error("🔴 Lỗi kết nối mạng:", error);
        showMsg("⚠️ Có lỗi mạng xảy ra khi lưu thông tin!", "bg-red-100 text-red-800 border-red-200");
        if (saveBtn) saveBtn.disabled = false;
        if (btnText) btnText.innerText = "Lưu";
    }
}

function showMsg(message, className) {
    const msgDiv = document.getElementById('formMessage');
    if (msgDiv) {
        msgDiv.className = `px-4 py-3 rounded-lg text-sm font-medium transition-all duration-300 ${className}`;
        msgDiv.innerText = message;
        msgDiv.classList.remove('hidden');
    }
}

function deleteDrugConfirmed() {
    if (!window.IS_ADMIN || currentDeleteId === 0) return;

    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    if (!tokenInput) {
        alert('Lỗi bảo mật: Không tìm thấy Token xác thực (AntiForgeryToken).');
        return;
    }
    const token = tokenInput.value;

    const deleteBtn = document.getElementById('confirmDeleteBtn');
    deleteBtn.disabled = true; 
    const originalText = deleteBtn.innerHTML;
    deleteBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang xóa...';

    fetch('/Drug/DeleteDrug', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': token
        },
        body: new URLSearchParams({ id: currentDeleteId })
    })
    .then(response => {
        if (!response.ok) throw new Error(`Mã trạng thái từ máy chủ: ${response.status}`);
        return response.json();
    })
    .then(data => {
        deleteBtn.disabled = false;
        deleteBtn.innerHTML = originalText;

        if (data.success) {
            alert(data.message);
            closeDeleteModal();
            location.reload(); 
        } else {
            alert('Lỗi từ hệ thống: ' + data.message);
        }
    })
    .catch(error => {
        console.error('Error:', error);
        deleteBtn.disabled = false;
        deleteBtn.innerHTML = originalText;
        alert('Không thể thực hiện kết nối máy chủ để xóa thuốc. Lỗi: ' + error.message);
    });
}

// ==========================================
// LOGIC XỬ LÝ TÌM KIẾM VÀ BỘ LỌC (SEARCH & FILTER)
// ==========================================
function goToPage(page) {
    if (page < 1) return;

    const searchTerm = document.getElementById('drugSearch')?.value || '';
    const status = document.getElementById('statusFilter')?.value || '';

    let url = `/Drug/Index?page=${page}`;
    if (searchTerm) url += `&searchTerm=${encodeURIComponent(searchTerm.trim())}`;
    if (status) url += `&status=${encodeURIComponent(status)}`;

    window.location.href = url;
}

function searchAndFilter() {
    goToPage(1);
}

document.addEventListener("DOMContentLoaded", function () {
    document.getElementById('drugSearch')?.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault(); 
            searchAndFilter();  
        }
    });

    document.getElementById('statusFilter')?.addEventListener('change', function (e) {
        searchAndFilter();
    });
});