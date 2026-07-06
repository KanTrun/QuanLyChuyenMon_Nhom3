USE MedicalProcedureManagement;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @procedures TABLE (
    procedure_id UNIQUEIDENTIFIER,
    code NVARCHAR(64)
);

INSERT INTO @procedures VALUES
('f0000000-0000-0000-0000-000000000009',N'QT.KSNK.09'),
('f0000000-0000-0000-0000-000000000012',N'QT.KSNK.12'),
('f0000000-0000-0000-0000-000000000016',N'QT.KSNK.16'),
('f0000000-0000-0000-0000-000000000017',N'QT.KSNK.17');

UPDATE p
SET p.description = N'Nhập từ PDF scan 2145; đang chờ trích xuất và đối chiếu đầy đủ từng trang trước khi ban hành.',
    p.updated_at = SYSUTCDATETIME()
FROM med.professional_procedures p
JOIN @procedures seed ON seed.procedure_id = p.procedure_id;

UPDATE v
SET v.summary = N'{"ocrStatus":"OCR_PENDING","note":"PDF scan là nguồn sự thật; nội dung in không hiển thị mã kỹ thuật và chỉ ban hành sau khi OCR/spot-check đủ từng trang."}'
FROM med.procedure_versions v
JOIN @procedures seed ON seed.procedure_id = v.procedure_id;

DECLARE @sections TABLE (section_order INT, body NVARCHAR(MAX));
INSERT INTO @sections VALUES
(1,N'Quy định thống nhất việc tiếp nhận, làm sạch, khử khuẩn/tiệt khuẩn, đóng gói, lưu trữ và giao nhận dụng cụ theo PDF scan nguồn, bảo đảm an toàn người bệnh và kiểm soát nhiễm khuẩn.'),
(2,N'Áp dụng cho khoa Kiểm soát nhiễm khuẩn và các khoa/phòng sử dụng dụng cụ thuộc phạm vi quy trình đã ban hành kèm PDF scan nguồn.'),
(3,N'Quyết định 3671/QĐ-BYT, quy định KSNK hiện hành và PDF scan nguồn.'),
(4,N'Thuật ngữ, phân loại dụng cụ, biểu mẫu và phụ lục chuyên môn thực hiện theo bản PDF scan nguồn.'),
(5,N'Người viết, người kiểm tra, người phê duyệt và các khoa/phòng liên quan chịu trách nhiệm theo bảng ký duyệt, bảng phân phối và từng bước trong lưu đồ.'),
(6,N'Xem bảng Nơi nhận trên bìa quy trình.'),
(7,N'Xem bảng Theo dõi sửa đổi trên bìa quy trình.'),
(8,N'Thực hiện theo trình tự các bước tại lưu đồ và diễn giải tương ứng; chỉ ban hành chính thức sau khi trích xuất, đối chiếu trực quan từng trang PDF scan nguồn.'),
(9,N'Lưu đồ được trình bày theo ba cột Trách nhiệm, Các bước thực hiện và Mô tả/Các biểu mẫu; tên bước nằm trực tiếp trong ký hiệu lưu đồ.'),
(10,N'Biểu mẫu, phụ lục và hồ sơ kiểm soát được liệt kê tại cột Mô tả/Các biểu mẫu của lưu đồ và tệp PDF nguồn đính kèm.'),
(11,N'PDF scan nguồn được gắn kèm với checksum SHA-256.');

UPDATE d
SET d.content_text = s.body
FROM med.procedure_document_sections d
JOIN med.procedure_versions v ON v.procedure_version_id = d.procedure_version_id
JOIN @procedures seed ON seed.procedure_id = v.procedure_id
JOIN @sections s ON s.section_order = d.section_order;

UPDATE r
SET r.summary = N'Ban hành theo PDF scan số 2145; nội dung chi tiết chờ trích xuất và đối chiếu.'
FROM med.procedure_revision_entries r
JOIN med.procedure_versions v ON v.procedure_version_id = r.procedure_version_id
JOIN @procedures seed ON seed.procedure_id = v.procedure_id
WHERE r.summary LIKE N'%OCR%';

DECLARE @flow TABLE (
    code NVARCHAR(64),
    step_no INT,
    responsibility NVARCHAR(MAX),
    detail_ref NVARCHAR(32),
    description NVARCHAR(MAX),
    form_ref NVARCHAR(MAX),
    shape_code NVARCHAR(32)
);

INSERT INTO @flow VALUES
(N'QT.KSNK.09',1,N'DD dụng cụ - khoa GMHS',N'5.2.1',N'',N'BM.KSNK.09.01
BM.KSNK.09.02',N'terminator'),
(N'QT.KSNK.09',2,N'- DD dụng cụ - khoa GMHS
- NV khu vực làm sạch, khử khuẩn dụng cụ - khoa KSNK',N'5.2.2',N'',N'BM.KSNK.09.09
Phụ lục I
Phụ lục II',N'process'),
(N'QT.KSNK.09',3,N'NV khu vực làm sạch, khử khuẩn dụng cụ - khoa KSNK',N'5.2.3',N'',N'BM.KSNK.09.03
BM.KSNK.09.04',N'process'),
(N'QT.KSNK.09',4,N'NV khu vực đóng gói dụng cụ - khoa KSNK',N'5.2.4',N'',N'Phụ lục III
Phụ lục IV
Phụ lục VI',N'process'),
(N'QT.KSNK.09',5,N'NV khu vực đóng gói dụng cụ - khoa KSNK',N'5.2.5',N'',N'BM.KSNK.09.05
BM.KSNK.09.06
BM.KSNK.09.07',N'process'),
(N'QT.KSNK.09',6,N'NV vận hành máy hấp - khoa KSNK',N'',N'Vận hành máy hấp phù hợp với loại dụng cụ cần tiệt khuẩn:
- Dụng cụ chịu nhiệt: Máy hấp nhiệt độ cao
- Dụng cụ không chịu nhiệt: Máy hấp nhiệt độ thấp',N'',N'process'),
(N'QT.KSNK.09',7,N'NV vận hành máy hấp - khoa KSNK',N'5.2.6',N'',N'BM.KSNK.09.08
Phụ lục VII
Phụ lục VIII',N'process'),
(N'QT.KSNK.09',8,N'NV kho vô khuẩn - khoa KSNK',N'5.2.7',N'',N'BM.KSNK.09.11',N'process'),
(N'QT.KSNK.09',9,N'- NV khu vực cấp phát dụng cụ - khoa KSNK
- DD dụng cụ - khoa GMHS',N'5.2.8',N'',N'BM.KSNK.09.10
Phụ lục IX
Phụ lục X',N'terminator');

INSERT INTO @flow
SELECT p.code, s.step_no,
       CASE
           WHEN s.step_no = 1 THEN N'Khoa sử dụng / Khoa KSNK'
           WHEN s.step_no = max_step.max_step THEN N'Khoa KSNK / Khoa sử dụng'
           WHEN s.name LIKE N'%Tiệt khuẩn%' OR s.name LIKE N'%Giám sát%' THEN N'NV vận hành máy hấp - khoa KSNK'
           WHEN s.name LIKE N'%Lưu trữ%' THEN N'NV kho vô khuẩn - khoa KSNK'
           WHEN s.name LIKE N'%Đóng gói%' OR s.name LIKE N'%Bảo dưỡng%' THEN N'NV khu vực đóng gói dụng cụ - khoa KSNK'
           ELSE N'NV khu vực làm sạch, khử khuẩn dụng cụ - khoa KSNK'
       END,
       N'5.2.' + CONVERT(NVARCHAR(8), s.step_no),
       CONVERT(NVARCHAR(MAX), CONVERT(NVARCHAR(8), s.step_no) + N'. ' + s.name + N': thực hiện theo diễn giải chi tiết trong PDF scan nguồn; ghi nhận hồ sơ và biểu mẫu tương ứng trước khi chuyển bước tiếp theo.'),
       N'Biểu mẫu/phụ lục: đối chiếu theo PDF scan nguồn.',
       CASE WHEN s.step_no = 1 OR s.step_no = max_step.max_step THEN N'terminator' ELSE N'process' END
FROM med.procedure_steps s
JOIN med.procedure_versions v ON v.procedure_version_id = s.procedure_version_id
JOIN @procedures p ON p.procedure_id = v.procedure_id
CROSS APPLY (
    SELECT MAX(step_no) AS max_step
    FROM med.procedure_steps x
    WHERE x.procedure_version_id = s.procedure_version_id
) max_step
WHERE p.code <> N'QT.KSNK.09'
  AND v.version_no = (
      SELECT MIN(v2.version_no)
      FROM med.procedure_versions v2
      WHERE v2.procedure_id = v.procedure_id
  );

UPDATE s
SET s.responsibility_text = f.responsibility,
    s.detail_section_number = f.detail_ref,
    s.description = f.description,
    s.form_reference_text = f.form_ref,
    s.flow_shape_code = f.shape_code
FROM med.procedure_steps s
JOIN med.procedure_versions v ON v.procedure_version_id = s.procedure_version_id
JOIN @procedures p ON p.procedure_id = v.procedure_id
JOIN @flow f ON f.code = p.code AND f.step_no = s.step_no;

COMMIT TRANSACTION;
GO
