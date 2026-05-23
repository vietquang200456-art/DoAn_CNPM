-- ================================
-- SQL Script: Seed Test Data for Disease (Bệnh Lý)
-- ================================
-- Chạy script này để thêm dữ liệu test cho trang quản lý bệnh lý

-- ===== DATA INSERTION =====

INSERT INTO Diseases (Name, Symptoms, Causes, TreatmentMethod, Description, IsActive, CreatedAt, UpdatedAt)
VALUES
(
    'Tiểu Đường Loại 2',
    'Khát nhiều, mệt mỏi, mờ mắt, tương tự hay buồn tiểu',
    'Yếu tố di truyền, lịch sử gia đình, béo phì, lối sống không lành mạnh',
    'Chế độ ăn uống khoa học, tập luyện thể dục, thuốc điều trị, kiểm soát đường huyết định kỳ',
    'Tiểu đường loại 2 là bệnh mãn tính phổ biến nhất. Tuyến tụy không sản xuất đủ insulin hoặc cơ thể không sử dụng insulin hiệu quả.',
    1,
    GETUTCDATE(),
    NULL
),
(
    'Huyết Áp Cao (Tăng Huyết Áp)',
    'Đau đầu, chóng mặt, hoa mắt, nóng mặt, mất ngủ',
    'Căng thẳng, lượng muối dư thừa, uống rượu, béo phì, di truyền',
    'Giảm stress, hạn chế muối, tập luyện đều đặn, uống thuốc hạ huyết áp, kiểm tra định kỳ',
    'Tăng huyết áp là tình trạng áp lực máu trong động mạch cao, làm tăng nguy hiểm bệnh tim, đột quỵ.',
    1,
    GETUTCDATE(),
    NULL
),
(
    'Cảm Cúm',
    'Ho, sốt, đau họng, chảy nước mũi, mệt mỏi, đau cơ',
    'Virus cúm, tiếp xúc với người bệnh, không rửa tay sạch sẽ',
    'Nghỉ ngơi, uống nước ấm, tăng cường sức đề kháng, sử dụng thuốc hạ sốt, kháng virus',
    'Cảm cúm là bệnh nhiễm trùng đường hô hấp cấp do virus gây ra, lây truyền qua giọt nước bọt.',
    1,
    GETUTCDATE(),
    NULL
),
(
    'Bệnh Tim Mạch',
    'Đau ngực, khó thở, mệt mỏi, chóng mặt, hồi hộp',
    'Tăng huyết áp, cao cholesterol, hút thuốc, béo phì, stress, di truyền',
    'Thay đổi lối sống, tập luyện, kiểm soát chế độ ăn, thuốc tim mạch, phẫu thuật nếu cần',
    'Bệnh tim mạch bao gồm các vấn đề về tim và mạch máu như nhồi máu cơ tim, đột quỵ.',
    1,
    GETUTCDATE(),
    NULL
),
(
    'Hen Suyễn',
    'Thở có tiếng, khó thở, ho kéo dài, tightness trong ngực',
    'Dị ứng, ô nhiễm không khí, thay đổi thời tiết, stress, do di truyền',
    'Tránh trigger, sử dụng inhaler, thuốc chống viêm, theo dõi các triệu chứng',
    'Hen suyễn là bệnh viêm đường hô hấp mãn tính gây hẹp đường thở, khó thở đặc biệt vào ban đêm.',
    1,
    GETUTCDATE(),
    NULL
),
(
    'Viêm Khớp',
    'Đau khớp, sưng, cứng, giảm vận động, mệt mỏi',
    'Tuổi tác, chấn thương, tăng cân, di truyền, vi khuẩn hoặc virus',
    'Vật lý trị liệu, thuốc chống viêm, tiêm corticosteroid, giảm cân, tập luyện nhẹ nhàng',
    'Viêm khớp là bệnh gây thoái hóa sụn khớp, gây đau, sưng và cứng khớp, phổ biến ở người cao tuổi.',
    1,
    GETUTCDATE(),
    NULL
),
(
    'Loãng Xương',
    'Không có triệu chứng rõ ràng, dễ gãy xương, cong vẹo cột sống',
    'Tuổi tác, nữ (sau mãn kinh), lồng não, thiếu vitamin D, yếu tố di truyền',
    'Bổ sung calcium & vitamin D, tập luyện, điều chỉnh chế độ ăn, thuốc loãng xương',
    'Loãng xương là tình trạng mật độ xương giảm, dễ gây gãy xương, đặc biệt ở phụ nữ sau mãn kinh.',
    1,
    GETUTCDATE(),
    NULL
),
(
    'Viêm Dạ Dày',
    'Đau bụng, buồn nôn, nôn, chán ăn, cảm giác no sớm',
    'Nhiễm H. pylori, aspirin, corticosteroid, căng thẳng, ăn cay nóng',
    'Kiêng ăn cay, dùng thuốc giảm acid, kháng sinh (nếu H. pylori), giảm stress',
    'Viêm dạ dày là bệnh viêm niêm mạc dạ dày, có thể gây loét nếu không điều trị kịp thời.',
    1,
    GETUTCDATE(),
    NULL
),
(
    'Bệnh Gan Mạn Tính',
    'Mệt mỏi, đau bụng trên, vàng da, phù nước',
    'Virus viêm gan, rượu, béo phì, tự miễn, chất độc hại',
    'Kiêng rượu, kiểm soát chế độ ăn, thuốc chống viêm, tiêm vắc xin phòng ngừa',
    'Bệnh gan mãn tính là tình trạng suy thoái gan lâu dài, có thể dẫn đến xơ gan nếu không điều trị.',
    1,
    GETUTCDATE(),
    NULL
),
(
    'Trầm Cảm',
    'Buồn, mất hứng thú, mất ngủ, mệt mỏi, tự làm hại',
    'Stress, chấn thương tâm lý, mất mất người thân, yếu tố di truyền, không cân bằng hóa chất não',
    'Liệu pháp tâm lý, thuốc chống trầm cảm, tập luyện, thay đổi lối sống, hỗ trợ xã hội',
    'Trầm cảm là bệnh tâm thần phổ biến gây ảnh hưởng lớn đến chất lượng sống nếu không điều trị.',
    1,
    GETUTCDATE(),
    NULL
);

-- ===== VERIFY DATA =====
SELECT COUNT(*) AS TotalDiseases FROM Diseases;
SELECT * FROM Diseases ORDER BY CreatedAt DESC;

-- ===== NOTES =====
/*
Ghi chú:
- Script trên thêm 10 bệnh lý phổ biến vào database
- Giá trị [Column] tương ứng:
  + Name: Tên bệnh lý
  + Symptoms: Triệu chứng
  + Causes: Nguyên nhân
  + TreatmentMethod: Phương pháp điều trị
  + Description: Mô tả chi tiết
  + IsActive: 1 = Active, 0 = Inactive
  + CreatedAt: Thời gian tạo (GETUTCDATE() = hiện tại UTC)
  + UpdatedAt: Thời gian cập nhật (NULL = chưa cập nhật)

- Để sửa dữ liệu:
  UPDATE Diseases 
  SET Description = 'Mô tả mới'
  WHERE Id = 1;

- Để xóa test data:
  DELETE FROM Diseases WHERE Id IN (1, 2, 3, ...);  -- Xóa theo ID
  DELETE FROM Diseases;                              -- Xóa tất cả

- Lưu ý: Nếu có dữ liệu liên kết (DrugDiseaseContraindication), 
  cần xóa dữ liệu con trước khi xóa Disease.
*/
