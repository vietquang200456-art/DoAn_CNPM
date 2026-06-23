/**
 * Quản lý cấu hình tập trung (URLs và quyền hạn)
 * Lấy giá trị được cấu hình từ thẻ script hoặc data-attributes ở View
 */
const InteractionConfig = {
    getUrls: () => ({
        searchInteractions: window.InteractionApp?.urls?.searchInteractions || '/Interaction/SearchAndFilterInteractions',
        searchContraindications: window.InteractionApp?.urls?.searchContraindications || '/Interaction/SearchAndFilterContraindications',
        deleteInteraction: window.InteractionApp?.urls?.deleteInteraction || '/Interaction/DeleteInteraction',
        deleteContraindication: window.InteractionApp?.urls?.deleteContraindication || '/Interaction/DeleteContraindication'
    }),
    isAdmin: () => !!window.InteractionApp?.isAdmin
};

/**
 * Hàm chuyển đổi giữa các tab
 */
function switchTab(tabName) {
    document.querySelectorAll('.tab-content').forEach(tab => {
        tab.classList.add('hidden');
    });
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.classList.remove('active', 'border-b-2', 'border-blue-700', 'text-blue-700', 'border-red-600', 'text-red-700');
        btn.classList.add('border-transparent', 'text-gray-600');
    });

    if (tabName === 'interactions') {
        document.getElementById('interactions-tab').classList.remove('hidden');
        const btn = document.getElementById('tab-interactions');
        if (btn) {
            btn.classList.add('active', 'border-b-2', 'border-blue-700', 'text-blue-700');
            btn.classList.remove('border-transparent', 'text-gray-600');
        }
    } else if (tabName === 'contraindications') {
        document.getElementById('contraindications-tab').classList.remove('hidden');
        const btn = document.getElementById('tab-contraindications');
        if (btn) {
            btn.classList.add('border-b-2', 'border-red-600', 'text-red-700');
            btn.classList.remove('border-transparent', 'text-gray-600');
        }
    }
}

/**
 * Hàm tìm kiếm tương tác thuốc
 */
function handleInteractionSearch(event) {
    if (event) event.preventDefault();
    const searchTerm = document.getElementById('interaction-search-term').value.trim();
    const severityLevel = document.getElementById('interaction-severity-level').value;
    const url = InteractionConfig.getUrls().searchInteractions;

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: new URLSearchParams({
            searchTerm: searchTerm,
            severityLevel: severityLevel,
            pageNumber: 1
        })
    })
    .then(response => {
        if (!response.ok) throw new Error('Network response was not ok');
        return response.text();
    })
    .then(html => { 
        document.getElementById('interactions-table-container').innerHTML = html; 
    })
    .catch(error => { console.error('Lỗi tìm kiếm tương tác:', error); });
}

/**
 * Hàm tìm kiếm chống chỉ định
 */
function handleContraindicationSearch(event) {
    if (event) event.preventDefault();
    const searchTerm = document.getElementById('contraindication-search-term').value.trim();
    const riskLevel = document.getElementById('contraindication-risk-level').value;
    const url = InteractionConfig.getUrls().searchContraindications;

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: new URLSearchParams({
            searchTerm: searchTerm,
            riskLevel: riskLevel,
            pageNumber: 1
        })
    })
    .then(response => {
        if (!response.ok) throw new Error('Network response was not ok');
        return response.text();
    })
    .then(html => { 
        document.getElementById('contraindications-table-container').innerHTML = html; 
    })
    .catch(error => { console.error('Lỗi tìm kiếm chống chỉ định:', error); });
}

/**
 * Hàm chuyển trang tương tác thuốc
 */
function goToInteractionPage(pageNumber) {
    const searchTerm = document.getElementById('interaction-search-term').value.trim();
    const severityLevel = document.getElementById('interaction-severity-level').value;
    const url = InteractionConfig.getUrls().searchInteractions;

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: new URLSearchParams({
            searchTerm: searchTerm,
            severityLevel: severityLevel,
            pageNumber: pageNumber
        })
    })
    .then(response => {
        if (!response.ok) throw new Error('Network response was not ok');
        return response.text();
    })
    .then(html => {
        document.getElementById('interactions-table-container').innerHTML = html;
        window.scrollTo({ top: 0, behavior: 'smooth' });
    })
    .catch(error => console.error('Lỗi phân trang tương tác:', error));
}

/**
 * Hàm chuyển trang chống chỉ định
 */
function goToContraindicationPage(pageNumber) {
    const searchTerm = document.getElementById('contraindication-search-term').value.trim();
    const riskLevel = document.getElementById('contraindication-risk-level').value;
    const url = InteractionConfig.getUrls().searchContraindications;

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: new URLSearchParams({
            searchTerm: searchTerm,
            riskLevel: riskLevel,
            pageNumber: pageNumber
        })
    })
    .then(response => {
        if (!response.ok) throw new Error('Network response was not ok');
        return response.text();
    })
    .then(html => {
        document.getElementById('contraindications-table-container').innerHTML = html;
        window.scrollTo({ top: 0, behavior: 'smooth' });
    })
    .catch(error => console.error('Lỗi phân trang chống chỉ định:', error));
}

/**
 * Hàm ĐẶT LẠI bộ lọc
 */
function resetInteractionFilters() {
    document.getElementById('interaction-search-term').value = '';
    document.getElementById('interaction-severity-level').value = '';
    handleInteractionSearch(null);
}

// Sửa lỗi logic: Gọi hàm search thay vì dispatch event lặp vô hạn
function resetContraindicationFilters() {
    document.getElementById('contraindication-search-term').value = '';
    document.getElementById('contraindication-risk-level').value = '';
    handleContraindicationSearch(null);
}

/* =======================================================
   HÀM XỬ LÝ XOÁ AJAX DÀNH CHO ADMIN
======================================================= */
function deleteInteraction(id, sourceDrug, targetDrug) {
    if (!InteractionConfig.isAdmin()) return;

    if (confirm(`Bạn có chắc chắn muốn xoá tương tác giữa "${sourceDrug}" và "${targetDrug}" không?`)) {
        const baseUrl = InteractionConfig.getUrls().deleteInteraction;
        
        fetch(`${baseUrl}/${id}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' }
        })
        .then(response => {
            if (!response.ok) throw new Error('Lỗi hệ thống');
            return response.json();
        })
        .then(data => {
            if (data.success) {
                alert('Xoá tương tác thuốc thành công!');
                handleInteractionSearch(null); // Tải lại danh sách sạch hơn
            } else {
                alert(data.message || 'Xoá thất bại!');
            }
        })
        .catch(error => console.error('Lỗi khi xoá tương tác:', error));
    }
}

function deleteContraindication(id, drug, disease) {
    if (!InteractionConfig.isAdmin()) return;

    if (confirm(`Bạn có chắc chắn muốn xoá chống chỉ định giữa thuốc "${drug}" và bệnh "${disease}" không?`)) {
        const baseUrl = InteractionConfig.getUrls().deleteContraindication;

        fetch(`${baseUrl}/${id}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' }
        })
        .then(response => {
            if (!response.ok) throw new Error('Lỗi hệ thống');
            return response.json();
        })
        .then(data => {
            if (data.success) {
                alert('Xoá chống chỉ định thành công!');
                handleContraindicationSearch(null); // Tải lại danh sách
            } else {
                alert(data.message || 'Xoá thất bại!');
            }
        })
        .catch(error => console.error('Lỗi khi xoá chống chỉ định:', error));
    }
}