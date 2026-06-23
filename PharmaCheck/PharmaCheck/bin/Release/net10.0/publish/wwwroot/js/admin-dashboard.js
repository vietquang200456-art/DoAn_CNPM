/**
 * PharmaCheck - Hệ thống Quản trị Dashboard Lâm sàng
 * Hệ thống xử lý tương tác sự kiện và kết xuất đồ thị
 */

document.addEventListener("DOMContentLoaded", function () {
    // 1. KHỞI TẠO BIỂU ĐỒ XU HƯỚNG CẬP NHẬT THUỐC (CHART.JS)
    const chartCanvas = document.getElementById('drugTrendChart');
    if (chartCanvas) {
        const ctx = chartCanvas.getContext('2d');
        // Đọc dữ liệu thô được gán an toàn từ các biến toàn cục (định nghĩa ở View)
        const labels = window.DashboardChartLabels || [];
        const chartData = window.DashboardChartData || [];

        new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Số lượng thuốc nhập mới',
                    data: chartData,
                    borderColor: '#0284c7', 
                    backgroundColor: 'rgba(2, 132, 199, 0.08)',
                    borderWidth: 3,
                    tension: 0.3,
                    fill: true,
                    pointBackgroundColor: '#0284c7',
                    pointHoverBackgroundColor: '#ffffff',
                    pointHoverBorderColor: '#0284c7',
                    pointHoverBorderWidth: 3,
                    pointRadius: 4,
                    pointHoverRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: { color: '#f1f5f9' },
                        ticks: { color: '#94a3b8', font: { size: 11 } }
                    },
                    x: {
                        grid: { display: false },
                        ticks: { color: '#94a3b8', font: { size: 11 } }
                    }
                }
            }
        });
    }
});

// 2. XỬ LÝ ĐỔI QUYỀN HẠN TÀI KHOẢN (ROLE MANAGEMENT)
function openChangeRoleModal(userId, currentRole) {
    const modal = document.getElementById('changeRoleModal');
    if (!modal) return;

    // Gán thông tin người dùng vào các trường ẩn của Modal
    document.getElementById('modalUserId').value = userId;
    document.getElementById('roleSelect').value = currentRole;

    // Hiển thị Modal bằng cách gỡ bỏ class ẩn danh
    modal.classList.remove('hidden');
    modal.classList.add('flex');
}

function closeChangeRoleModal() {
    const modal = document.getElementById('changeRoleModal');
    if (!modal) return;

    modal.classList.remove('flex');
    modal.classList.add('hidden');
}

function submitChangeRole() {
    const userId = document.getElementById('modalUserId').value;
    const newRole = document.getElementById('roleSelect').value;
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    if (!userId || !newRole) return;

    // Gửi yêu cầu cập nhật quyền hạn về hệ thống Backend
    fetch(`/AdminDashboard/ChangeUserRole`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': token // Đảm bảo an toàn chống tấn công CSRF
        },
        body: `userId=${userId}&newRole=${newRole}`
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            window.location.reload(); // Tải lại trang để cập nhật Badge hiển thị mới
        } else {
            alert("Lỗi: " + data.message);
        }
    })
    .catch(err => {
        console.error("Lỗi kết nối API đổi quyền:", err);
        alert("Không thể kết nối đến máy chủ. Vui lòng thử lại.");
    });
}

// 3. XỬ LÝ KHÓA / MỞ KHÓA TÀI KHOẢN (STATUS MANAGEMENT)
function toggleUserStatus(userId, shouldActivate) {
    const actionText = shouldActivate ? "Mở khóa" : "Khóa";
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    if (confirm(`Bạn có chắc chắn muốn ${actionText.toLowerCase()} tài khoản thành viên này không?`)) {
        fetch(`/AdminDashboard/ToggleUserStatus`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': token
            },
            body: `userId=${userId}&activate=${shouldActivate}`
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                window.location.reload();
            } else {
                alert("Lỗi: " + data.message);
            }
        })
        .catch(err => {
            console.error("Lỗi kết nối API thay đổi trạng thái:", err);
        });
    }
}