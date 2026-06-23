/**
 * Quản lý logic form kê đơn thuốc động và tìm kiếm thông minh
 * Dự án: PharmaCheck
 * Tích hợp bộ não AI cảnh báo rủi ro tương tác thuốc ngầm lâm sàng 🌟
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
        ageInput.addEventListener("input", function () {
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
document.addEventListener("click", function (e) {
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
                    item.onclick = function () {
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
 * LOGIC QUẢN LÝ DANH MỤC THUỐC & ĐƠN THUỐC ĐỘNG (ĐÃ TÍCH HỢP AI KIỂM TRA NGẦM) 🌟
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
                       class="drug-name-input w-full px-3 py-2.5 border border-slate-300 rounded-xl text-sm bg-white focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition" />
                
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

    document.addEventListener("click", function (e) {
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

        // SỰ KIỆN CLICK CHỌN THUỐC: Thực hiện đồng bộ lập tức thông qua Trigger AI
        item.onclick = function () {
            const searchInput = document.getElementById(`drugSearch_${id}`);
            searchInput.value = drug.name;
            document.getElementById(`drugSelect_${id}`).value = drug.id;
            menu.classList.add("hidden");

            searchInput.dispatchEvent(new Event('input'));
            searchInput.dispatchEvent(new Event('change'));

            // Thay vì truyền tham số lắt léo gây sai dòng, hãy gọi bộ điều phối trung tâm quét toàn đơn 🌟
            triggerClinicalAiCheck();
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

    // Sau khi xóa một dòng thuốc, quét lại AI một lượt để xóa các cảnh báo liên quan của thuốc đó
    clearOldAiAlerts();
    reEvaluateAllDrugsByAi();
}

/**
 * =========================================================================
 * BỘ NÃO AI ENGINE XỬ LÝ KIỂM TRA NGẦM SỰ CỐ TƯƠNG TÁC LÂM SÀNG 🧠🌟
 * =========================================================================
 */

function triggerClinicalAiCheck() {
    // Mỗi khi kích hoạt, xóa toàn bộ Toast cũ để render lại từ đầu, tránh bị lặp đè Toast cũ
    clearOldAiAlerts();

    // 1. Lấy ra danh sách tất cả các ID thuốc đang có trên giao diện (bỏ các ô trống)
    const drugIds = Array.from(document.querySelectorAll("input[id^='drugSelect_']"))
        .map(input => parseInt(input.value))
        .filter(id => !isNaN(id) && id > 0);

    // Nếu có ít hơn 2 thuốc thì không thể có tương tác, khôi phục trạng thái nút lưu an toàn rồi thoát
    if (drugIds.length < 2) {
        togglePrescriptionSubmitButton(false);
        return;
    }

    // 2. Gọi API Backend (Controller C#) để xử lý một mẻ duy nhất
    // Endpoint: /Prescription/CheckInteractions
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    const verificationToken = tokenElement ? tokenElement.value : "";

    fetch('/Prescription/CheckInteractions', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': verificationToken
        },
        body: JSON.stringify(drugIds)
    })
    .then(res => res.json())
    .then(data => {
        let hasCriticalLevel5 = false;

        // data sẽ là mảng các InteractionAlertResponse
        if (Array.isArray(data) && data.length > 0) {
            data.forEach(alert => {
                displayAiAlertToast(alert);
                
                // Quét tìm xem có bản ghi nào chứa mức độ nguy kịch cấp 5 không
                if (parseInt(alert.severityLevel) === 5 || parseInt(alert.SeverityLevel) === 5) {
                    hasCriticalLevel5 = true;
                }
            });
        }

        // Thực thi kiểm soát liên khóa dựa trên trạng thái quét nguy kịch
        togglePrescriptionSubmitButton(hasCriticalLevel5);
    })
    .catch(err => console.error("Lỗi phân tích tương tác lâm sàng (Hybrid Mode):", err));
}

// Giờ đây hàm đánh giá lại đơn thuốc chỉ cần gọi duy nhất một lệnh điều phối trung tâm
function reEvaluateAllDrugsByAi() {
    triggerClinicalAiCheck();
}

/**
 * Điều phối trạng thái khóa/mở nút bấm lưu đơn thuốc và hiển thị thông báo khẩn cấp 🔒🔓
 */
function togglePrescriptionSubmitButton(shouldBlock) {
    const submitBtn = document.querySelector('button[onclick="submitPrescription()"]');
    if (!submitBtn) return;

    if (shouldBlock) {
        // 1. Kích hoạt thuộc tính chặn cứng tương tác DOM
        submitBtn.disabled = true;

        // 2. Tạo hình giao diện nút Bị Cấm trực quan bằng hệ màu cảnh báo Tailwind CSS
        submitBtn.classList.remove('bg-emerald-600', 'hover:bg-emerald-700', 'hover:shadow-emerald-600/10', 'active:scale-95');
        submitBtn.classList.add('bg-slate-400', 'cursor-not-allowed', 'opacity-60');
        submitBtn.innerHTML = `<i class="fas fa-ban"></i> Đơn Thuốc Bị Khóa Chặn (Có Cấp 5)`;

        // 3. Đẩy thêm biểu ngữ thông báo khẩn cấp dạng Banner ghim trực tiếp cố định phía trên các toast nếu chưa có
        injectCriticalBlockBanner();
    } else {
        // Trả lại nguyên trạng nút bấm kê đơn an toàn
        submitBtn.disabled = false;
        submitBtn.classList.remove('bg-slate-400', 'cursor-not-allowed', 'opacity-60');
        submitBtn.classList.add('bg-emerald-600', 'hover:bg-emerald-700', 'hover:shadow-emerald-600/10', 'active:scale-95');
        submitBtn.innerHTML = `<i class="fas fa-check-circle"></i> Hoàn Thành & Lưu Đơn`;

        // Gỡ bỏ biểu ngữ khẩn cấp ra khỏi DOM
        const staticBanner = document.getElementById("criticalClinicalBlockBanner");
        if (staticBanner) staticBanner.remove();
    }
}

/**
 * Bơm cấu trúc biểu ngữ đỏ khẩn cấp trực tiếp vào vùng an toàn của Container
 */
function injectCriticalBlockBanner() {
    if (document.getElementById("criticalClinicalBlockBanner")) return;

    const container = document.getElementById("aiAlertContainer");
    if (!container) return;

    const banner = document.createElement("div");
    banner.id = "criticalClinicalBlockBanner";
    banner.className = "p-4 bg-red-50 border-2 border-red-500 rounded-2xl shadow-xl flex gap-3 animate-bounce bg-white";
    banner.innerHTML = `
        <div class="w-9 h-9 rounded-xl flex items-center justify-center shrink-0 bg-red-100 text-red-600">
            <i class="fas fa-exclamation-triangle text-lg"></i>
        </div>
        <div class="flex-1">
            <h4 class="text-xs font-extrabold uppercase tracking-wider text-red-700">Yêu cầu thay đổi phác đồ thuốc!</h4>
            <p class="text-[11px] text-red-600 leading-relaxed mt-1 font-semibold">Phát hiện tương tác thuốc đặc biệt nguy hiểm (Cấp 5/5). Hệ thống khóa tính năng lưu đơn thuốc cho đến khi hoạt chất xung đột được gỡ bỏ.</p>
        </div>
    `;
    // Đẩy lên vị trí trên cùng của vùng chứa thông báo
    container.prepend(banner);
}

/**
 * Hiển thị Banner cảnh báo nổi dạng Toast bằng Tailwind CSS cực đẹp 🎨
 */
function displayAiAlertToast(data) {
    // Tạo vùng chứa Toast Container ở góc màn hình nếu chưa có sẵn
    let container = document.getElementById("aiAlertContainer");
    if (!container) {
        container = document.createElement("div");
        container.id = "aiAlertContainer";
        container.className = "fixed bottom-5 right-5 space-y-3 z-50 max-w-md w-full px-4 sm:px-0 pointer-events-none [&>*]:pointer-events-auto";
        document.body.appendChild(container);
    }

    // Tạo một thẻ Toast Alert độc lập
    const toast = document.createElement("div");

    // Động hóa hoàn toàn CSS màu sắc viền dựa vào màu sắc lớp Class động trả về từ Controller
    toast.className = `p-4 rounded-2xl border shadow-lg flex gap-3 animate-in fade-in slide-in-from-bottom-5 duration-300 bg-white ${data.colorClass.split(' ')[2] || 'border-blue-200'}`;

    // Phân cấp Icon động và màu sắc tiêu đề Toast theo cấp độ nghiêm trọng thực tế
    let badgeText = "";
    let iconClass = "fa-circle-info";
    let iconBgColor = "bg-blue-50 text-blue-600";
    let titleTextColor = "text-blue-700";

    switch (parseInt(data.severityLevel)) {
        case 5:
            badgeText = `Cảnh báo nguy kịch (Cấp 5/5)`;
            iconClass = "fa-skull-crossbones";
            iconBgColor = "bg-red-100 text-red-600";
            titleTextColor = "text-red-700";
            break;
        case 4:
            badgeText = `Cảnh báo nghiêm trọng (Cấp 4/5)`;
            iconClass = "fa-triangle-exclamation";
            iconBgColor = "bg-orange-100 text-orange-600";
            titleTextColor = "text-orange-700";
            break;
        case 3:
            badgeText = `Cảnh báo trung bình (Cấp 3/5)`;
            iconClass = "fa-circle-exclamation";
            iconBgColor = "bg-yellow-100 text-yellow-600";
            titleTextColor = "text-yellow-700";
            break;
        case 2:
            badgeText = `Lưu ý tương tác (Cấp 2/5)`;
            iconClass = "fa-circle-info";
            iconBgColor = "bg-blue-100 text-blue-600";
            titleTextColor = "text-blue-700";
            break;
        default:
            badgeText = `Tương tác nhẹ (Cấp 1/5)`;
            iconClass = "fa-circle-check";
            iconBgColor = "bg-green-100 text-green-600";
            titleTextColor = "text-green-700";
    }

    // Label Nguồn dữ liệu (Cấu hình hệ thống vs AI dự đoán)
    const sourceLabel = data.source === "AI" 
        ? `<span class="bg-indigo-100 text-indigo-700 px-2 py-0.5 rounded text-[10px] font-bold tracking-wide shadow-sm"><i class="fas fa-robot mr-1"></i>🤖 AI Dự đoán</span>` 
        : `<span class="bg-emerald-100 text-emerald-700 px-2 py-0.5 rounded text-[10px] font-bold tracking-wide shadow-sm"><i class="fas fa-clipboard-list mr-1"></i>📋 Cấu hình Hệ thống</span>`;

    toast.innerHTML = `
        <div class="w-9 h-9 rounded-xl flex items-center justify-center shrink-0 ${iconBgColor}">
            <i class="fas ${iconClass} text-lg"></i>
        </div>
        <div class="flex-1 space-y-1">
            <div class="flex items-center justify-between">
                <span class="text-xs font-extrabold uppercase tracking-wider ${titleTextColor}">${badgeText}</span>
                <button onclick="this.parentElement.parentElement.parentElement.remove()" class="text-slate-400 hover:text-slate-600 text-xs">
                    <i class="fas fa-times"></i>
                </button>
            </div>
            <div class="mt-1 mb-1">
                ${sourceLabel}
            </div>
            <p class="text-xs font-bold text-slate-800 leading-tight">Xung đột giữa: ${data.drugA_Name} và ${data.drugB_Name}</p>
            <p class="text-[11px] text-slate-600 leading-relaxed mt-1">${data.description}</p>
            <div class="bg-slate-50 p-2 rounded-lg border border-slate-100 mt-2 text-[10px] text-slate-500 italic leading-snug">
                <strong>Khuyến cáo:</strong> ${data.recommendation}
            </div>
        </div>
    `;

    container.appendChild(toast);

    // Chỉ tự động ẩn sau 15 giây đối với các cấp thấp hơn 5. Riêng Cấp 5 bắt buộc giữ lại để ép bác sĩ nhìn thấy.
    if (parseInt(data.severityLevel) !== 5) {
        setTimeout(() => { if (toast && toast.parentElement) toast.remove(); }, 15000);
    }
}

function clearOldAiAlerts() {
    const container = document.getElementById("aiAlertContainer");
    if (container) container.innerHTML = "";
}

/**
 * =========================================================================
 * THU THẬP VÀ ĐỒNG BỘ DỮ LIỆU ĐƠN THUỐC VỀ SERVER
 * =========================================================================
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

    // XỬ LÝ KHẮC PHỤC BIẾN TUỔI VÀ NGÀY SINH THÔNG MINH ĐỘNG
    let ageValue = 0;
    let birthDateValue = null;

    if (isBirthDateMode) {
        birthDateValue = document.getElementById("patientBirthDate").value;
        if (!birthDateValue) { alert("⚠️ Vui lòng chọn ngày tháng năm sinh cụ thể của bệnh nhân."); return; }
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

    // Đóng gói dữ liệu JSON gửi đi khớp 100% với DTO
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