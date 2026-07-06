USE MedicalProcedureManagement;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @procedures TABLE (
    procedure_id UNIQUEIDENTIFIER,
    code NVARCHAR(64),
    title NVARCHAR(256),
    file_name NVARCHAR(512)
);

INSERT INTO @procedures VALUES
('f0000000-0000-0000-0000-000000000009',N'QT.KSNK.09',N'Quy trình xử lý dụng cụ phẫu thuật',N'2145_QUY TRÌNH XỬ LÝ DỤNG CỤ PHẪU THUẬT.pdf'),
('f0000000-0000-0000-0000-000000000012',N'QT.KSNK.12',N'Quy trình xử lý dụng cụ y tế',N'2145_QUY TRÌNH XỬ LÝ DỤNG CỤ Y TẾ.pdf'),
('f0000000-0000-0000-0000-000000000016',N'QT.KSNK.16',N'Quy trình khử khuẩn mức độ cao dụng cụ y tế',N'2145_QUY TRÌNH KHỬ KHUẨN MỨC ĐỘ CAO DỤNG CỤ Y TẾ.pdf'),
('f0000000-0000-0000-0000-000000000017',N'QT.KSNK.17',N'Quy trình xử lý tay khoan nha khoa',N'2145_QUY TRÌNH XỬ LÝ TAY KHOAN NHA KHOA.pdf');

UPDATE v
SET v.title = CASE
        WHEN v.version_no = 1 THEN p.title
        ELSE p.title + N' - ' + v.version_label
    END,
    v.summary = N'{"ocrStatus":"OCR_PENDING","note":"PDF scan là nguồn sự thật; chỉ seed metadata và lưu đồ đã spot-check từ ảnh render."}',
    v.change_reason = CASE
        WHEN v.version_no = 1 THEN N'Nhập quy trình KSNK từ PDF scan'
        ELSE N'Cập nhật từ phiên bản trước'
    END,
    v.source_pdf_file_name = p.file_name
FROM med.procedure_versions v
JOIN @procedures p ON p.procedure_id = v.procedure_id
WHERE v.title COLLATE Latin1_General_100_CI_AI LIKE N'Quy trinh%';

DECLARE @sections TABLE (section_order INT, title NVARCHAR(256), body NVARCHAR(MAX));
INSERT INTO @sections VALUES
(1,N'Mục đích',N'OCR_PENDING: trích xuất từ PDF scan trước khi ban hành.'),
(2,N'Phạm vi áp dụng',N'OCR_PENDING: áp dụng theo đúng phạm vi trong PDF scan.'),
(3,N'Căn cứ và tài liệu viện dẫn',N'Quyết định 3671/QĐ-BYT, quy định KSNK hiện hành và PDF scan nguồn.'),
(4,N'Thuật ngữ và định nghĩa',N'OCR_PENDING: bổ sung thuật ngữ y tế đúng theo PDF scan.'),
(5,N'Trách nhiệm',N'OCR_PENDING: người viết, người kiểm tra, người phê duyệt và khoa/phòng liên quan.'),
(6,N'Nơi nhận và phân phối',N'Xem bảng Nơi nhận trên bìa quy trình.'),
(7,N'Theo dõi sửa đổi',N'Xem bảng Theo dõi sửa đổi trên bìa quy trình.'),
(8,N'Nội dung quy trình',N'OCR_PENDING: không được ban hành khi chưa có OCR và spot-check từng trang.'),
(9,N'Lưu đồ',N'Lưu đồ được seed theo hình trong PDF scan; cần đối chiếu lại khi OCR hoàn tất.'),
(10,N'Hồ sơ, biểu mẫu và phụ lục',N'OCR_PENDING: danh mục biểu mẫu/phụ lục theo PDF scan.'),
(11,N'Tệp đính kèm',N'PDF scan nguồn được gắn kèm với checksum SHA-256.');

UPDATE d
SET d.title = s.title,
    d.content_text = s.body
FROM med.procedure_document_sections d
JOIN med.procedure_versions v ON v.procedure_version_id = d.procedure_version_id
JOIN @procedures p ON p.procedure_id = v.procedure_id
JOIN @sections s ON s.section_order = d.section_order
WHERE d.content_text LIKE N'OCR_PENDING:%'
   OR d.content_text COLLATE Latin1_General_100_CI_AI IN (
        N'Quyet dinh 3671/QD-BYT, quy dinh KSNK hien hanh va PDF scan nguon.',
        N'Xem bang Noi nhan tren bia quy trinh.',
        N'Xem bang Theo doi sua doi tren bia quy trinh.',
        N'Luu do duoc seed theo hinh trong PDF scan; can doi chieu lai khi OCR hoan tat.',
        N'PDF scan nguon duoc gan kem voi checksum SHA-256.'
   );

UPDATE r
SET r.recipient_name = CASE r.display_order
    WHEN 1 THEN N'Ban Giám đốc'
    WHEN 2 THEN N'Khoa Kiểm soát nhiễm khuẩn'
    WHEN 3 THEN N'Các khoa/phòng sử dụng dụng cụ'
    ELSE r.recipient_name END
FROM med.procedure_distribution_recipients r
JOIN med.procedure_versions v ON v.procedure_version_id = r.procedure_version_id
JOIN @procedures p ON p.procedure_id = v.procedure_id
WHERE r.display_order BETWEEN 1 AND 3;

UPDATE r
SET r.page_ref = N'Toàn văn',
    r.section_ref = N'Lần 02',
    r.summary = N'Ban hành theo PDF scan số 2145; nội dung chi tiết chờ OCR và đối chiếu.'
FROM med.procedure_revision_entries r
JOIN med.procedure_versions v ON v.procedure_version_id = r.procedure_version_id
JOIN @procedures p ON p.procedure_id = v.procedure_id
WHERE r.display_order = 1
  AND r.summary LIKE N'%OCR%';

DECLARE @flow TABLE (code NVARCHAR(64), step_no INT, name NVARCHAR(256));
INSERT INTO @flow VALUES
(N'QT.KSNK.09',1,N'Làm sạch dụng cụ'),(N'QT.KSNK.09',2,N'Giao nhận dụng cụ sau khi làm sạch'),(N'QT.KSNK.09',3,N'Làm sạch, khử khuẩn dụng cụ'),(N'QT.KSNK.09',4,N'Bảo dưỡng - kiểm tra dụng cụ'),(N'QT.KSNK.09',5,N'Đóng gói dụng cụ'),(N'QT.KSNK.09',6,N'Tiệt khuẩn dụng cụ'),(N'QT.KSNK.09',7,N'Giám sát chất lượng tiệt khuẩn dụng cụ'),(N'QT.KSNK.09',8,N'Lưu trữ dụng cụ'),(N'QT.KSNK.09',9,N'Giao nhận dụng cụ sau khi tiệt khuẩn'),
(N'QT.KSNK.12',1,N'Làm sạch, khử khuẩn dụng cụ'),(N'QT.KSNK.12',2,N'Giao nhận dụng cụ sau khi làm sạch'),(N'QT.KSNK.12',3,N'Làm sạch, khử khuẩn dụng cụ'),(N'QT.KSNK.12',4,N'Bảo dưỡng - kiểm tra dụng cụ'),(N'QT.KSNK.12',5,N'Đóng gói dụng cụ'),(N'QT.KSNK.12',6,N'Tiệt khuẩn dụng cụ'),(N'QT.KSNK.12',7,N'Giám sát chất lượng tiệt khuẩn dụng cụ'),(N'QT.KSNK.12',8,N'Lưu trữ dụng cụ'),(N'QT.KSNK.12',9,N'Giao nhận dụng cụ sau khi tiệt khuẩn'),
(N'QT.KSNK.16',1,N'Làm sạch dụng cụ'),(N'QT.KSNK.16',2,N'Giao nhận dụng cụ sau khi làm sạch'),(N'QT.KSNK.16',3,N'Khử khuẩn mức độ cao dụng cụ'),(N'QT.KSNK.16',4,N'Đóng gói dụng cụ'),(N'QT.KSNK.16',5,N'Lưu trữ dụng cụ tại khoa KSNK'),(N'QT.KSNK.16',6,N'Giao nhận dụng cụ vô khuẩn'),
(N'QT.KSNK.17',1,N'Chuẩn bị'),(N'QT.KSNK.17',2,N'Làm sạch'),(N'QT.KSNK.17',3,N'Khử khuẩn'),(N'QT.KSNK.17',4,N'Tra dầu bôi trơn'),(N'QT.KSNK.17',5,N'Giao nhận dụng cụ sau khi làm sạch, khử khuẩn'),(N'QT.KSNK.17',6,N'Đóng gói'),(N'QT.KSNK.17',7,N'Tiệt khuẩn'),(N'QT.KSNK.17',8,N'Lưu trữ tại khoa KSNK'),(N'QT.KSNK.17',9,N'Giao nhận dụng cụ sau khi tiệt khuẩn');

UPDATE s
SET s.name = f.name,
    s.description = N'OCR_PENDING: diễn giải chi tiết cần trích xuất và đối chiếu từ từng trang PDF scan.',
    s.responsibility_text = CASE
        WHEN s.step_no = 1 THEN N'Khoa sử dụng / Khoa KSNK'
        ELSE N'Khoa KSNK'
    END
FROM med.procedure_steps s
JOIN med.procedure_versions v ON v.procedure_version_id = s.procedure_version_id
JOIN @procedures p ON p.procedure_id = v.procedure_id
JOIN @flow f ON f.code = p.code AND f.step_no = s.step_no
WHERE s.description LIKE N'OCR_PENDING:%';

UPDATE a
SET a.file_name = p.file_name,
    a.file_uri = N'imported/' + p.file_name
FROM med.procedure_attachments a
JOIN med.procedure_versions v ON v.procedure_version_id = a.procedure_version_id
JOIN @procedures p ON p.procedure_id = v.procedure_id
WHERE a.attachment_type = N'source_pdf';

COMMIT TRANSACTION;
GO
