USE MedicalProcedureManagement;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

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
(N'QT.KSNK.09',1,N'ĐD dụng cụ - khoa GMHS',N'5.2.1',N'',N'BM.KSNK.09.01
BM.KSNK.09.02',N'terminator'),
(N'QT.KSNK.09',2,N'- ĐD dụng cụ - khoa GMHS
- NV khu vực làm sạch, khử khuẩn dụng cụ - khoa KSNK',N'5.2.2',N'',N'BM.KSNK.09.09
Phụ lục I
Phụ lục II',N'process'),
(N'QT.KSNK.09',3,N'NV khu vực làm sạch, khử khuẩn dụng cụ - khoa KSNK',N'5.2.3',N'',N'BM.KSNK.09.03
BM.KSNK.09.04',N'process'),
(N'QT.KSNK.09',4,N'NV khu vực đóng gói dụng cụ - khoa KSNK',N'5.2.4',N'',N'Phụ lục III
Phụ lục IV
Phụ lục V
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
- ĐD dụng cụ - khoa GMHS',N'5.2.8',N'',N'BM.KSNK.09.10
Phụ lục IX
Phụ lục X',N'terminator'),
(N'QT.KSNK.12',1,N'NV khoa sử dụng',N'5.2.1',N'',N'BM.KSNK.12.01
BM.KSNK.12.02',N'terminator'),
(N'QT.KSNK.12',2,N'- NV khoa sử dụng
- NV khu vực làm sạch, khử khuẩn dụng cụ - khoa KSNK',N'5.2.2',N'',N'BM.KSNK.12.09
Phụ lục I',N'process'),
(N'QT.KSNK.12',3,N'NV khu vực làm sạch, khử khuẩn dụng cụ - khoa KSNK',N'5.2.3',N'',N'BM.KSNK.12.03
BM.KSNK.12.04',N'process'),
(N'QT.KSNK.12',4,N'NV khu vực đóng gói dụng cụ - khoa KSNK',N'5.2.4',N'',N'Phụ lục II
Phụ lục III',N'process'),
(N'QT.KSNK.12',5,N'NV khu vực đóng gói dụng cụ - khoa KSNK',N'5.2.5',N'',N'BM.KSNK.12.05
BM.KSNK.12.06
BM.KSNK.12.07',N'process'),
(N'QT.KSNK.12',6,N'NV vận hành máy hấp - khoa KSNK',N'',N'Vận hành máy hấp phù hợp với loại dụng cụ cần tiệt khuẩn theo khuyến cáo của nhà sản xuất:
- Dụng cụ chịu nhiệt: Máy hấp nhiệt độ cao
- Dụng cụ không chịu nhiệt: Máy hấp nhiệt độ thấp',N'',N'process'),
(N'QT.KSNK.12',7,N'NV vận hành máy hấp - khoa KSNK',N'5.2.6',N'',N'BM.KSNK.12.08
Phụ lục IV
Phụ lục V',N'process'),
(N'QT.KSNK.12',8,N'NV kho vô khuẩn - khoa KSNK',N'5.2.7',N'',N'BM.KSNK.12.11',N'process'),
(N'QT.KSNK.12',9,N'- NV khu vực cấp phát dụng cụ - khoa KSNK
- NV khoa sử dụng',N'5.2.8',N'',N'BM.KSNK.12.10
Phụ lục I',N'terminator'),
(N'QT.KSNK.16',1,N'NV khoa sử dụng',N'5.2.1',N'',N'BM.KSNK.16.01',N'terminator'),
(N'QT.KSNK.16',2,N'- NV khoa sử dụng
- NV khu vực làm sạch, khử khuẩn dụng cụ - khoa KSNK',N'5.2.2',N'',N'BM.KSNK.16.02
Phụ lục I',N'process'),
(N'QT.KSNK.16',3,N'NV khu vực làm sạch, khử khuẩn dụng cụ - khoa KSNK',N'5.2.3',N'',N'BM.KSNK.16.03
Phụ lục II
Phụ lục III',N'process'),
(N'QT.KSNK.16',4,N'NV khu vực đóng gói dụng cụ - khoa KSNK',N'',N'- Mang phương tiện PHCN: nón, khẩu trang
- Vệ sinh tay
- Trải khăn vô khuẩn lên bàn đóng gói dụng cụ KKMĐC
- Mang áo choàng vô khuẩn, găng vô khuẩn
- Lấy dụng cụ ra từ tủ sấy và kiểm tra độ khô; để dụng cụ lên bàn đã trải khăn vô khuẩn
- Đóng gói dụng cụ bằng bao túi ép chuyên dụng đã được hàn một đầu và hấp tiệt khuẩn
- Đóng dấu hoặc dán nhãn thông tin: ngày đóng gói, nhân viên đóng gói, hạn sử dụng 14 ngày
- Chuyển dụng cụ qua kho vô khuẩn bằng hộp trung chuyển (Passbox)',N'BM.KSNK.16.04',N'process'),
(N'QT.KSNK.16',5,N'NV kho vô khuẩn - khoa KSNK',N'',N'Lưu trữ dụng cụ sau khi xử lý tại khoa KSNK để duy trì độ vô khuẩn đến khi bàn giao cho khoa sử dụng.',N'',N'process'),
(N'QT.KSNK.16',6,N'- NV khu vực cấp phát dụng cụ - khoa KSNK
- NV khoa sử dụng',N'5.2.4',N'',N'BM.KSNK.16.05',N'terminator'),
(N'QT.KSNK.17',1,N'NV khoa sử dụng',N'5.2.2',N'',N'',N'terminator'),
(N'QT.KSNK.17',2,N'NV khoa sử dụng',N'5.2.3',N'',N'',N'process'),
(N'QT.KSNK.17',3,N'NV khoa sử dụng',N'5.2.4',N'',N'',N'process'),
(N'QT.KSNK.17',4,N'NV khoa sử dụng',N'',N'Tra dầu bôi trơn theo hướng dẫn của nhà sản xuất và cho chạy nhẹ trong 10 - 15 giây với dầu bôi trơn.',N'',N'process'),
(N'QT.KSNK.17',5,N'- NV khoa sử dụng
- NV khoa KSNK',N'5.2.5',N'',N'',N'process'),
(N'QT.KSNK.17',6,N'NV khoa KSNK',N'5.2.6',N'',N'',N'process'),
(N'QT.KSNK.17',7,N'NV khoa KSNK',N'',N'Tiệt khuẩn dụng cụ theo hướng dẫn của nhà sản xuất.',N'',N'process'),
(N'QT.KSNK.17',8,N'NV khoa KSNK',N'',N'Dụng cụ sau khi tiệt khuẩn được lưu trữ tại kho vô khuẩn theo quy định.',N'',N'process'),
(N'QT.KSNK.17',9,N'- NV khoa KSNK
- NV khoa sử dụng',N'5.2.7',N'',N'',N'terminator');

UPDATE s
SET s.responsibility_text = f.responsibility,
    s.detail_section_number = NULLIF(f.detail_ref, N''),
    s.description = NULLIF(f.description, N''),
    s.form_reference_text = NULLIF(f.form_ref, N''),
    s.flow_shape_code = f.shape_code
FROM med.procedure_steps s
JOIN med.procedure_versions v ON v.procedure_version_id = s.procedure_version_id
JOIN med.professional_procedures p ON p.procedure_id = v.procedure_id
JOIN @flow f ON f.code = p.procedure_code AND f.step_no = s.step_no;

COMMIT TRANSACTION;
GO
