/**
 * Quản lý logic form kê đơn thuốc động và tìm kiếm thông minh
 * Dự án: PharmaCheck
 */

let rowCounter = 0;
let globalDrugsList = []; // Lưu trữ danh sách thuốc lấy từ DB về để tìm kiếm nhanh tại Client
let isBirthDateMode = false; // Theo dõi đang ở chế độ nhập tuổi hay ngày sinh

document.addEventListener("DOMContentLoaded", function () {
    // 1. Tải trước danh sách thuốc từ server để phục vụ tính năng gõ tìm kiếm
    preloadDrugsData();

    // 2. Ép ràng buộc kiểm tra tuổi trực tiếp khi người dùng nhập liệu (Input Event)
    const ageInput = document.getElementById("patientAge");
    if (ageInput) {
        ageInput.addEventListener("input", function() {
            let val = parseInt(this.value);
            if (isNaN(val) || val <= 0) {
                this.value = ""; 
            } else if (val >= 150) {
                this.value = 149; // Nếu nhập >= 150 thì tự động giới hạn ở 149
            }
        });
    }

    // Tự động mở sẵn dòng thuốc đầu tiên
    addDrugRow();
});

/**
 * =========================================================================
 * LOGIC TỰ ĐỘNG TÌM KIẾM VÀ GỢI Ý BỆNH NHÂN CŨ (AUTOCOMPLETE)
 * =========================================================================
 */

function toggleAgeBirthMode() {
    isBirthDateMode = !isBirthDateMode;
    const ageWrapper = document.getElementById("ageInputWrapper");
    const birthWrapper = document.getElementById("birthDateInputWrapper");
    const btn = document.getElementById("btnToggleMode");
    const label = document.getElementById("ageBirthLabel");

    if (isBirthDateMode) {
        ageWrapper.classList.add("hidden");
        birthWrapper.classList.remove("hidden");
        label.innerHTML = 'Ngày Sinh Cụ Thể <span class="text-red-500">*</span>';
        btn.innerText = "Chuyển Nhập Tuổi Nhanh";
        document.getElementById("patientAge").value = "";
    } else {
        ageWrapper.classList.remove("hidden");
        birthWrapper.classList.add("hidden");
        label.innerHTML = 'Tuổi <span class="text-red-500">*</span>';
        btn.innerText = "Nhập Ngày Sinh Thật";
        document.getElementById("patientBirthDate").value = "";
    }
}

function showPatientDropdown() {
    const menu = document.getElementById("patientDropdownMenu");
    if (menu && menu.children.length > 0) menu.classList.remove("hidden");
}

function hidePatientDropdown() {
    const menu = document.getElementById("patientDropdownMenu");
    if (menu) {
        // Tăng delay lên 250ms để trình duyệt kịp ăn sự kiện click chuột chọn item bệnh nhân
        setTimeout(() => { menu.classList.add("hidden"); }, 250);
    }
}

// Click ra ngoài vùng thì ẩn danh sách gợi ý bệnh nhân
document.addEventListener("click", function(e) {
    const wrapper = document.getElementById("patientDropdownWrapper");
    if (wrapper && !wrapper.contains(e.target)) {
        hidePatientDropdown();
    }
});

let patientSearchTimeout = null;
function searchPatientsLive() {
    const keyword = document.getElementById("patientName").value.trim();
    const menu = document.getElementById("patientDropdownMenu");
    
    // Nếu từ khóa ngắn quá, xóa ID đã chọn và ẩn menu
    if (keyword.length < 2) {
        document.getElementById("selectedPatientId").value = "";
        menu.innerHTML = "";
        menu.classList.add("hidden");
        return;
    }

    clearTimeout(patientSearchTimeout);
    patientSearchTimeout = setTimeout(() => {
        fetch(`/Prescription/GetPatientsJson?term=${encodeURIComponent(keyword)}`)
            .then(response => response.json())
            .then(patients => {
                menu.innerHTML = "";
                if (patients.length === 0) {
                    menu.classList.add("hidden");
                    return;
                }

                patients.forEach(p => {
                    const item = document.createElement("div");
                    item.className = "p-3 text-sm text-slate-700 hover:bg-blue-50 hover:text-blue-700 cursor-pointer transition flex justify-between items-center z-50";
                    item.innerHTML = `
                        <div class="flex flex-col">
                            <span class="font-bold text-slate-800">${p.name}</span>
                            <span class="text-xs text-slate-400">SĐT: ${p.phone}</span>
                        </div>
                        <div class="text-right text-xs text-slate-500 italic">${p.gender} - ${p.age} tuổi</div>
                    `;

                    // Khi click chọn bệnh nhân cũ: Tự động điền dữ liệu (Auto-fill) 🌟
                    item.onclick = function() {
                        document.getElementById("selectedPatientId").value = p.id;
                        document.getElementById("patientName").value = p.name;
                        document.getElementById("patientPhone").value = p.phone;
                        document.getElementById("patientGender").value = p.gender;
                        document.getElementById("allergies").value = p.allergies;
                        
                        // Chuyển về ô nhập tuổi nhanh để hiển thị số tuổi của hồ sơ cũ
                        if (isBirthDateMode) {
                            toggleAgeBirthMode(); 
                        }
                        document.getElementById("patientAge").value = p.age;
                        
                        menu.classList.add("hidden");
                    };
                    menu.appendChild(item);
                });
                menu.classList.remove("hidden");
            })
            .catch(err => console.error("Lỗi quét gợi ý bệnh nhân:", err));
    }, 300);
}

/**
 * =========================================================================
 * LOGIC QUẢN LÝ DANH MỤC THUỐC & ĐƠN THUỐC ĐỘNG
 * =========================================================================
 */

function preloadDrugsData() {
    fetch('/Prescription/GetDrugsJson')
        .then(response => response.json())
        .then(data => {
            globalDrugsList = data;
        })
        .catch(err => console.error("Lỗi đồng bộ danh mục thuốc:", err));
}

function addDrugRow() {
    rowCounter++;
    const currentId = rowCounter;
    
    const tbody = document.getElementById("prescriptionTableBody");
    const emptyState = document.getElementById("emptyState");
    
    if (emptyState) emptyState.classList.add("hidden");

    const tr = document.createElement("tr");
    tr.id = `drugRow_${currentId}`;
    tr.className = "group hover:bg-slate-50/80 transition-all duration-200";
    
    tr.innerHTML = `
        <td class="py-4 pr-3 pl-2 relative">
            <div class="relative" id="dropdownWrapper_${currentId}">
                <input type="text" 
                       id="drugSearch_${currentId}" 
                       placeholder="🔍 Gõ để tìm thuốc..." 
                       autocomplete="off"
                       onfocus="showDropdown(${currentId})"
                       oninput="filterDrugs(${currentId})"
                       class="w-full px-3 py-2.5 border border-slate-300 rounded-xl text-sm bg-white focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition" />
                
                <input type="hidden" id="drugSelect_${currentId}" value="" />
                <div id="dropdownMenu_${currentId}" 
                     class="hidden absolute left-0 right-0 mt-1 max-h-60 overflow-y-auto bg-white border border-slate-200 rounded-xl shadow-xl z-50 divide-y divide-slate-50">
                </div>
            </div>
        </td>
        <td class="py-4 pr-3">
            <input type="text" placeholder="Ví dụ: 20 viên" id="quantity_${currentId}"
                   class="w-full px-3 py-2.5 border border-slate-300 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition" />
        </td>
        <td class="py-4">
            <input type="text" placeholder="Ví dụ: Sáng 1 viên, tối 1 viên sau ăn" id="instruction_${currentId}"
                   class="w-full px-3 py-2.5 border border-slate-300 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition" />
        </td>
        <td class="py-4 text-center">
            <button type="button" onclick="removeDrugRow(${currentId})" 
                    class="text-slate-400 hover:text-red-600 p-2 rounded-lg hover:bg-red-50 transition cursor-pointer">
                <i class="fas fa-trash-alt text-sm"></i>
            </button>
        </td>
    `;
    
    tbody.appendChild(tr);

    document.addEventListener("click", function(e) {
        const wrapper = document.getElementById(`dropdownWrapper_${currentId}`);
        if (wrapper && !wrapper.contains(e.target)) {
            hideDropdown(currentId);
        }
    });
}

function showDropdown(id) {
    const menu = document.getElementById(`dropdownMenu_${id}`);
    if (menu) {
        menu.classList.remove("hidden");
        filterDrugs(id);
    }
}

function hideDropdown(id) {
    const menu = document.getElementById(`dropdownMenu_${id}`);
    if (menu) {
        setTimeout(() => { menu.classList.add("hidden"); }, 200);
    }
}

function filterDrugs(id) {
    const keyword = document.getElementById(`drugSearch_${id}`).value.toLowerCase().trim();
    const menu = document.getElementById(`dropdownMenu_${id}`);
    menu.innerHTML = "";

    const filtered = globalDrugsList.filter(drug => 
        drug.name.toLowerCase().includes(keyword) || 
        (drug.ingredient && drug.ingredient.toLowerCase().includes(keyword))
    );

    if (filtered.length === 0) {
        menu.innerHTML = `<div class="p-3 text-xs text-slate-400 italic">❌ Không tìm thấy thuốc phù hợp</div>`;
        return;
    }

    filtered.forEach(drug => {
        const item = document.createElement("div");
        item.className = "p-3 text-sm text-slate-700 hover:bg-blue-50 hover:text-blue-700 cursor-pointer transition flex flex-col gap-0.5";
        item.innerHTML = `
            <span class="font-bold">${drug.name}</span>
            <span class="text-xs text-slate-400">Hoạt chất: ${drug.ingredient || 'N/A'}</span>
        `;
        
        item.onclick = function() {
            document.getElementById(`drugSearch_${id}`).value = drug.name;
            document.getElementById(`drugSelect_${id}`).value = drug.id;
            menu.classList.add("hidden");
        };
        
        menu.appendChild(item);
    });
}

function removeDrugRow(id) {
    const row = document.getElementById(`drugRow_${id}`);
    if (row) row.remove();
    
    const tbody = document.getElementById("prescriptionTableBody");
    if (tbody && tbody.children.length === 0) {
        const emptyState = document.getElementById("emptyState");
        if (emptyState) emptyState.classList.remove("hidden");
    }
}

/**
 * Thu thập và gửi đơn thuốc về Server - ĐÃ SỬA LỖI ĐỊNH DANH VÀ CHẾ ĐỘ NGÀY SINH THẬT 🌟
 */
function submitPrescription() {
    const patientId = document.getElementById("selectedPatientId").value;
    const patientName = document.getElementById("patientName").value.trim();
    const phoneNumber = document.getElementById("patientPhone").value.trim();
    const gender = document.getElementById("patientGender").value;
    const allergies = document.getElementById("allergies").value.trim();
    const symptoms = document.getElementById("symptoms").value.trim();
    const diagnosis = document.getElementById("diagnosis").value.trim();
    const note = document.getElementById("prescriptionNote").value.trim();
    
    // Kiểm tra các trường dữ liệu bắt buộc
    if (!patientName) { alert("⚠️ Vui lòng điền họ và tên bệnh nhân."); return; }
    if (!phoneNumber) { alert("⚠️ Vui lòng nhập số điện thoại để định danh bệnh nhân."); return; }
    if (!diagnosis) { alert("⚠️ Vui lòng nhập thông tin chẩn đoán lâm sàng."); return; }

    // XỬ LÝ KHẮC PHỤC BIẾN TUỔI VÀ NGÀY SINH THÔNG MINH ĐỘNG 🌟
    let ageValue = 0;
    let birthDateValue = null;

    if (isBirthDateMode) {
        birthDateValue = document.getElementById("patientBirthDate").value;
        if (!birthDateValue) { alert("⚠️ Vui lòng chọn ngày tháng năm sinh cụ thể của bệnh nhân."); return; }
        // Tính nhẩm tuổi tạm thời gửi lên để qua bộ lọc validate (Backend sẽ tính lại chuẩn)
        const birthYear = new Date(birthDateValue).getFullYear();
        ageValue = new Date().getFullYear() - birthYear;
    } else {
        ageValue = parseInt(document.getElementById("patientAge").value);
        if (isNaN(ageValue) || ageValue <= 0 || ageValue >= 150) { 
            alert("⚠️ Tuổi bệnh nhân bắt buộc phải lớn hơn 0 và nhỏ hơn 150."); 
            return; 
        }
    }

    const details = [];
    const rows = document.querySelectorAll("#prescriptionTableBody tr");
    if (rows.length === 0) { alert("⚠️ Đơn thuốc phải chứa ít nhất một loại biệt dược."); return; }

    let isValidDrugs = true;
    rows.forEach(row => {
        const rowId = row.id.split("_")[1];
        const drugId = document.getElementById(`drugSelect_${rowId}`).value;
        const quantity = document.getElementById(`quantity_${rowId}`).value.trim();
        const usageInstruction = document.getElementById(`instruction_${rowId}`).value.trim();
        
        if (!drugId || !quantity || !usageInstruction) {
            isValidDrugs = false;
            row.classList.add("bg-red-50/50");
        } else {
            row.classList.remove("bg-red-50/50");
            details.push({
                DrugId: parseInt(drugId),
                Quantity: quantity,
                UsageInstruction: usageInstruction
            });
        }
    });

    if (!isValidDrugs) {
        alert("⚠️ Một số hàng thuốc chưa chọn biệt dược hoặc thiếu chỉ định số lượng/cách dùng.");
        return;
    }

    // Đóng gói dữ liệu JSON gửi đi khớp 100% với DTO đã nâng cấp ở Backend 🌟
    const submissionData = {
        PatientId: patientId ? parseInt(patientId) : null,
        PatientName: patientName,
        PhoneNumber: phoneNumber,
        Age: ageValue,
        BirthDateStr: birthDateValue,
        Gender: gender,
        Allergies: allergies,
        Symptoms: symptoms,
        Diagnosis: diagnosis,
        Note: note,
        Details: details
    };

    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    const verificationToken = tokenElement ? tokenElement.value : "";

    fetch('/Prescription/SavePrescription', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': verificationToken
        },
        body: JSON.stringify(submissionData)
    })
    .then(response => response.json())
    .then(result => {
        if (result.success) {
            alert("🎉 " + result.message);
            window.location.reload(); 
        } else {
            alert("❌ " + result.message);
        }
    })
    .catch(error => {
        console.error("Lỗi đồng bộ đơn thuốc:", error);
        alert("❌ Khóa kết nối hệ thống trong quá trình xử lý dữ liệu.");
    });
}