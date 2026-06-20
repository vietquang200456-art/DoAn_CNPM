/**
 * Quản lý logic form kê đơn thuốc động và tìm kiếm thông minh
 * Dự án: PharmaCheck
 */

let rowCounter = 0;
let globalDrugsList = []; // Lưu trữ danh sách thuốc lấy từ DB về để tìm kiếm nhanh tại Client

document.addEventListener("DOMContentLoaded", function () {
    // 1. Tải trước danh sách thuốc từ server để phục vụ tính năng gõ tìm kiếm
    preloadDrugsData();

    // 2. Ép ràng buộc kiểm tra tuổi trực tiếp khi người dùng nhập liệu (Input Event)
    const ageInput = document.getElementById("patientAge");
    if (ageInput) {
        ageInput.addEventListener("input", function() {
            let val = parseInt(this.value);
            if (val <= 0) {
                this.value = 1; // Nếu nhập <= 0 thì tự động đưa về 1
            } else if (val >= 150) {
                this.value = 149; // Nếu nhập >= 150 thì tự động giới hạn ở 149
            }
        });
    }

    // Tự động mở sẵn dòng thuốc đầu tiên
    addDrugRow();
});

/**
 * Tải danh mục thuốc từ Backend
 */
function preloadDrugsData() {
    fetch('/Prescription/GetDrugsJson')
        .then(response => response.json())
        .then(data => {
            globalDrugsList = data;
        })
        .catch(err => console.error("Lỗi đồng bộ danh mục thuốc:", err));
}

/**
 * Thêm một hàng chọn thuốc mới với bộ tìm kiếm thông minh (Searchable Dropdown)
 */
function addDrugRow() {
    rowCounter++;
    const currentId = rowCounter;
    
    const tbody = document.getElementById("prescriptionTableBody");
    const emptyState = document.getElementById("emptyState");
    
    if (emptyState) emptyState.classList.add("hidden");

    const tr = document.createElement("tr");
    tr.id = `drugRow_${currentId}`;
    tr.className = "group hover:bg-slate-50/80 transition-all duration-200";
    
    // Sử dụng Custom Dropdown: Gồm 1 ô input để gõ và 1 menu ẩn chứa kết quả lọc
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

    // Lắng nghe sự kiện click ra ngoài thì đóng dropdown ẩn đi
    document.addEventListener("click", function(e) {
        const wrapper = document.getElementById(`dropdownWrapper_${currentId}`);
        if (wrapper && !wrapper.contains(e.target)) {
            hideDropdown(currentId);
        }
    });
}

/**
 * Hiển thị menu tìm kiếm và nạp dữ liệu ban đầu
 */
function showDropdown(id) {
    const menu = document.getElementById(`dropdownMenu_${id}`);
    if (menu) {
        menu.classList.remove("hidden");
        filterDrugs(id); // Gọi bộ lọc để hiện danh sách ban đầu
    }
}

/**
 * Ẩn menu tìm kiếm
 */
function hideDropdown(id) {
    const menu = document.getElementById(`dropdownMenu_${id}`);
    if (menu) {
        setTimeout(() => { menu.classList.add("hidden"); }, 200); // Delay nhẹ để kịp nhận sự kiện click chọn thuốc
    }
}

/**
 * Thuật toán lọc tìm kiếm thuốc trực tiếp dựa trên dữ liệu bác sĩ gõ (Real-time Filtering)
 */
function filterDrugs(id) {
    const keyword = document.getElementById(`drugSearch_${id}`).value.toLowerCase().trim();
    const menu = document.getElementById(`dropdownMenu_${id}`);
    menu.innerHTML = ""; // Xóa dữ liệu cũ trong menu

    // Lọc danh sách thuốc khớp với tên hoặc hoạt chất
    const filtered = globalDrugsList.filter(drug => 
        drug.name.toLowerCase().includes(keyword) || 
        (drug.ingredient && drug.ingredient.toLowerCase().includes(keyword))
    );

    if (filtered.length === 0) {
        menu.innerHTML = `<div class="p-3 text-xs text-slate-400 italic">❌ Không tìm thấy thuốc phù hợp</div>`;
        return;
    }

    // Sinh cấu trúc giao diện danh sách kết quả tìm kiếm
    filtered.forEach(drug => {
        const item = document.createElement("div");
        item.className = "p-3 text-sm text-slate-700 hover:bg-blue-50 hover:text-blue-700 cursor-pointer transition flex flex-col gap-0.5";
        item.innerHTML = `
            <span class="font-bold">${drug.name}</span>
            <span class="text-xs text-slate-400 group-hover:text-blue-500">Hoạt chất: ${drug.ingredient || 'N/A'}</span>
        `;
        
        // Sự kiện khi bác sĩ bấm chọn thuốc cụ thể
        item.onclick = function() {
            document.getElementById(`drugSearch_${id}`).value = drug.name; // Hiển thị tên lên ô nhập
            document.getElementById(`drugSelect_${id}`).value = drug.id;     // Gán ID ngầm vào thẻ hidden input
            menu.classList.add("hidden"); // Đóng dropdown
        };
        
        menu.appendChild(item);
    });
}

/**
 * Xóa một dòng thuốc ra khỏi phác đồ
 */
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
 * Thu thập và tiến hành gửi đơn thuốc về Server kèm xác thực chặt chẽ
 *//**
 * Thu thập và tiến hành gửi đơn thuốc về Server theo cấu trúc DTO mới
 */
function submitPrescription() {
    const patientName = document.getElementById("patientName").value.trim();
    const age = parseInt(document.getElementById("patientAge").value);
    const gender = document.getElementById("patientGender").value;
    const symptoms = document.getElementById("symptoms").value.trim();
    const diagnosis = document.getElementById("diagnosis").value.trim();
    const note = document.getElementById("prescriptionNote").value.trim();
    
    // 1. Kiểm tra điều kiện dữ liệu hành chính diện rộng
    if (!patientName) { alert("⚠️ Vui lòng điền họ và tên bệnh nhân."); return; }
    if (isNaN(age) || age <= 0 || age >= 150) { alert("⚠️ Tuổi bệnh nhân bắt buộc phải lớn hơn 0 và nhỏ hơn 150."); return; }
    if (!diagnosis) { alert("⚠️ Vui lòng nhập thông tin chẩn đoán lâm sàng."); return; }

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

    // 2. Đóng gói dữ liệu JSON khớp 100% với PrescriptionSubmissionDto ở Backend 🌟
    const submissionData = {
        PatientName: patientName,
        Age: age,
        Gender: gender,
        Symptoms: symptoms,
        Diagnosis: diagnosis,
        Note: note,
        Details: details
    };

    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    const verificationToken = tokenElement ? tokenElement.value : "";

    // 3. Gửi Request API bằng Fetch
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
            // Có thể chuyển hướng sang trang danh sách đơn thuốc hoặc reload
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