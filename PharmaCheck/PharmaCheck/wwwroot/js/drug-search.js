/**
 * PharmaCheck - Xử lý hiển thị chi tiết thuốc qua AJAX Modal
 */
function viewDetailedDrug(id) {
    // Tận dụng trực tiếp hàm GetDrugById có sẵn trong DrugController
    fetch(`/Drug/GetDrugById?id=${id}`)
        .then(res => res.json())
        .then(res => {
            if (!res.success) {
                alert(res.message);
                return;
            }
            const d = res.data;
            
            // Cập nhật tiêu đề Modal
            document.getElementById('modalDrugName').innerText = `💊 ${d.name} (${d.manufacturer})`;
            
            // Đổ dữ liệu y khoa lâm sàng vào Body Modal
            document.getElementById('modalBody').innerHTML = `
                <div class="grid grid-cols-3 gap-1 border-b border-slate-100 pb-2">
                    <span class="text-slate-400 font-medium">Hoạt chất chính:</span>
                    <span class="col-span-2 font-semibold text-slate-900">${d.activeIngredient}</span>
                </div>
                <div class="space-y-1">
                    <span class="text-slate-400 font-medium block">🎯 Tác dụng & Chỉ định điều trị:</span>
                    <p class="text-slate-700 bg-slate-50 p-2.5 rounded-lg border border-slate-100">${d.function || 'Chưa cập nhật dữ liệu.'}</p>
                </div>
                <div class="space-y-1">
                    <span class="text-slate-400 font-medium block">📏 Liều lượng & Phương thức sử dụng:</span>
                    <p class="text-slate-700 bg-slate-50 p-2.5 rounded-lg border border-slate-100">${d.dosage || 'Chưa cập nhật dữ liệu.'}</p>
                </div>
                <div class="space-y-1">
                    <span class="text-slate-400 font-medium block">🤢 Tác dụng phụ có thể xảy ra:</span>
                    <p class="text-slate-700 bg-amber-50 p-2.5 rounded-lg border border-amber-100 text-amber-900">${d.sideEffects || 'Chưa phát hiện tác dụng phụ nguy hiểm.'}</p>
                </div>
                <div class="space-y-1">
                    <span class="text-slate-400 font-medium block">🚫 Chống chỉ định lâm sàng:</span>
                    <p class="text-slate-700 bg-rose-50 p-2.5 rounded-lg border border-rose-100 text-rose-900">${d.contraindications || 'Không có chống chỉ định đặc biệt.'}</p>
                </div>
                <div class="space-y-1">
                    <span class="text-slate-400 font-medium block">📝 Mô tả ghi chú bổ sung:</span>
                    <p class="text-slate-600 text-xs italic">${d.description || 'Không có ghi chú thêm.'}</p>
                </div>
            `;
            
            // Hiển thị modal bằng cách xóa class hidden
            document.getElementById('detailModal').classList.remove('hidden');
            document.body.style.overflow = 'hidden'; // Chặn cuộn trang nền khi đang mở modal
        })
        .catch(err => {
            console.error("Lỗi hệ thống tra cứu:", err);
            alert("Không thể kết nối đến máy chủ dữ liệu thuốc.");
        });
}

function closeModal() {
    document.getElementById('detailModal').classList.add('hidden');
    document.body.style.overflow = ''; // Khôi phục cuộn trang bình thường
}

// Đóng modal tự động khi người dùng click chệch ra vùng ngoài hộp thoại
window.addEventListener('click', function(e) {
    const modal = document.getElementById('detailModal');
    if (e.target === modal) {
        closeModal();
    }
});