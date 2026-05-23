-- ================================
-- SQL Script: Seed Test Data for PharmaCheck
-- ================================
-- Chạy script này nếu bạn muốn thêm dữ liệu test vào database
-- Thay đổi server/database name nếu cần


-- ===== DATA INSERTION =====

INSERT INTO Drugs (Name, ActiveIngredient, Function, Dosage, SideEffects, Contraindications, Manufacturer, Description, IsActive, CreatedAt, UpdatedAt, ViewCount)
VALUES
(
    'Amoxicillin 500mg',
    'Amoxicillin Trihydrate',
    'Kháng sinh, chống viêm',
    '500mg',
    'Dị ứng, tiêu chảy, buồn nôn',
    'Dị ứng với Penicillin hoặc Cephalosporin',
    'Công ty Dược phẩm ABC',
    'Thuốc kháng sinh phổ rộng, dùng để điều trị nhiễm khuẩn đường hô hấp, tiêu hóa, niệu',
    1,
    GETUTCDATE(),
    NULL,
    0
),
(
    'Paracetamol 500mg',
    'Paracetamol (Acetaminophen)',
    'Hạ sốt, giảm đau',
    '500mg',
    'Buồn nôn, chóng mặt, phát ban',
    'Bệnh gan, bệnh thận nặng, dị ứng paracetamol',
    'Công ty Dược phẩm XYZ',
    'Thuốc hạ sốt và giảm đau nhất dụng, an toàn cho trẻ em',
    1,
    GETUTCDATE(),
    NULL,
    0
),
(
    'Ibuprofen 400mg',
    'Ibuprofen',
    'Chống viêm, giảm đau',
    '400mg',
    'Buồn nôn, viêm dạ dày, tiêu chảy',
    'Viêm dạ dày mạn tính, bệnh tim, dị ứng NSAID',
    'Công ty Dược phẩm 123',
    'Thuốc chống viêm không steroids, dùng cho đau đầu, đau cơ',
    1,
    GETUTCDATE(),
    NULL,
    0
),
(
    'Metformin 500mg',
    'Metformin HCl',
    'Kiểm soát đường huyết',
    '500mg',
    'Buồn nôn, rối loạn tiêu hóa, vị kim loại trong miệng',
    'Suy thận, bệnh gan, acidosis',
    'Công ty Dược phẩm DEF',
    'Thuốc chính cho bệnh tiểu đường loại 2, giúp giảm đường huyết',
    1,
    GETUTCDATE(),
    NULL,
    0
),
(
    'Lisinopril 10mg',
    'Lisinopril',
    'Hạ huyết áp',
    '10mg',
    'Ho khô, chóng mặt, tê lạnh',
    'Bệnh thận nặng, thai kỳ, dị ứng ACE inhibitor',
    'Công ty Dược phẩm GHI',
    'Thuốc hạ huyết áp, dùng cho bệnh nhân cao huyết áp và suy tim',
    1,
    GETUTCDATE(),
    NULL,
    0
),
(
    'Cetirizine 10mg',
    'Cetirizine HCl',
    'Chống dị ứng',
    '10mg',
    'Buồn ngủ, khô miệng, đau đầu',
    'Dị ứng cetirizine, cho con dưới 2 tuổi',
    'Công ty Dược phẩm JKL',
    'Thuốc kháng organistin H1, dùng điều trị viêm mũi dị ứng',
    1,
    GETUTCDATE(),
    NULL,
    0
),
(
    'Aspirin 325mg',
    'Acetylsalicylic Acid',
    'Giảm đau, chống viêm, chống đông máu',
    '325mg',
    'Chảy máu, viêm dạ dày, phát ban',
    'Viêm dạ dày hoạt động, chảy máu, dị ứng aspirin',
    'Công ty Dược phẩm MNO',
    'Thuốc giảm đau, hạ sốt, chống viêm, dùng phòng chống cảm cúm',
    1,
    GETUTCDATE(),
    NULL,
    0
),
(
    'Omeprazole 20mg',
    'Omeprazole',
    'Giảm acid dạ dày',
    '20mg',
    'Đau đầu, tiêu chảy, chóng mặt',
    'Dị ứng omeprazole, bệnh gan nặng',
    'Công ty Dược phẩm PQR',
    'Thuốc bệnh trào ngược axit, viêm loét dạ dày',
    1,
    GETUTCDATE(),
    NULL,
    0
),
(
    'Vitamin C 1000mg',
    'Ascorbic Acid',
    'Bổ sung vitamin, tăng miễn dịch',
    '1000mg',
    'Tiêu chảy, buồn nôn',
    'Đá thận, bệnh máu',
    'Công ty Dược phẩm STU',
    'Vitamin bổ sung, hỗ trợ hệ miễn dịch, chống oxy hóa',
    1,
    GETUTCDATE(),
    NULL,
    0
),
(
    'Dexamethasone 0.5mg',
    'Dexamethasone',
    'Chống viêm, ức chế miễn dịch',
    '0.5mg',
    'Tăng cân, mất ngủ, tăng đường huyết',
    'Bệnh nhiễm khuẩn nặng, viêm dạ dày hoạt động',
    'Công ty Dược phẩm VWX',
    'Thuốc corticosteroid mạnh, dùng cho viêm nặng, sốc phản vệ',
    0,
    GETUTCDATE(),
    NULL,
    0
);

-- ===== VERIFY DATA =====
SELECT COUNT(*) AS TotalDrugs FROM Drugs;
SELECT * FROM Drugs ORDER BY CreatedAt DESC;

-- ===== NOTES =====
/*
- Script trên thêm 10 loại thuốc test vào database
- Chỉnh sửa các giá trị nếu cần
- GETUTCDATE() lấy thời gian hiện tại UTC
- Nếu bạn muốn sửa dữ liệu sau, sử dụng UPDATE:

UPDATE Drugs 
SET Description = 'Mô tả mới'
WHERE Id = 1;

- Nếu muốn xóa test data:

DELETE FROM Drugs WHERE Name LIKE '%mg%';  -- Xóa tất cả
DELETE FROM Drugs WHERE Id = 1;             -- Xóa theo ID

- Cần giữ tính toàn vẹn khóa ngoài nếu có dữ liệu liên kết
*/
