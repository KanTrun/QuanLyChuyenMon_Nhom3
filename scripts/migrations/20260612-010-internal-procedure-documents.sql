USE MedicalProcedureManagement;
GO

SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF OBJECT_ID(N'med.signature_transactions', N'U') IS NOT NULL
    DROP TABLE med.signature_transactions;

IF COL_LENGTH(N'med.procedure_versions', N'issue_date') IS NULL
    ALTER TABLE med.procedure_versions ADD issue_date DATETIME2(3) NULL;
IF COL_LENGTH(N'med.procedure_versions', N'issue_number') IS NULL
    ALTER TABLE med.procedure_versions ADD issue_number INT NULL;
IF COL_LENGTH(N'med.procedure_versions', N'source_pdf_file_name') IS NULL
    ALTER TABLE med.procedure_versions ADD source_pdf_file_name NVARCHAR(512) NULL;
IF COL_LENGTH(N'med.procedure_versions', N'source_pdf_checksum_sha256') IS NULL
    ALTER TABLE med.procedure_versions ADD source_pdf_checksum_sha256 NVARCHAR(128) NULL;

IF COL_LENGTH(N'med.procedure_steps', N'responsibility_text') IS NULL
    ALTER TABLE med.procedure_steps ADD responsibility_text NVARCHAR(512) NULL;
IF COL_LENGTH(N'med.procedure_steps', N'flow_shape_code') IS NULL
    ALTER TABLE med.procedure_steps ADD flow_shape_code NVARCHAR(32) NOT NULL CONSTRAINT DF_procedure_steps_flow_shape DEFAULT N'process';
IF COL_LENGTH(N'med.procedure_steps', N'form_reference_text') IS NULL
    ALTER TABLE med.procedure_steps ADD form_reference_text NVARCHAR(512) NULL;
IF COL_LENGTH(N'med.procedure_steps', N'detail_section_number') IS NULL
    ALTER TABLE med.procedure_steps ADD detail_section_number NVARCHAR(32) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM med.lookup_attachment_types WHERE attachment_type = N'source_pdf')
    INSERT INTO med.lookup_attachment_types (attachment_type, name, display_order, is_active, description)
    VALUES (N'source_pdf', N'PDF nguon', -10, 1, N'Ban PDF nguon cua quy trinh');

IF OBJECT_ID(N'med.procedure_document_sections', N'U') IS NULL
BEGIN
    CREATE TABLE med.procedure_document_sections (
        procedure_document_section_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_procedure_document_sections_id DEFAULT NEWID(),
        procedure_version_id UNIQUEIDENTIFIER NOT NULL,
        section_order INT NOT NULL,
        section_number NVARCHAR(32) NOT NULL,
        title NVARCHAR(256) NOT NULL,
        section_kind NVARCHAR(64) NOT NULL CONSTRAINT DF_procedure_document_sections_kind DEFAULT N'body',
        content_text NVARCHAR(MAX) NULL,
        is_required BIT NOT NULL CONSTRAINT DF_procedure_document_sections_required DEFAULT 1,
        created_at DATETIME2(3) NOT NULL CONSTRAINT DF_procedure_document_sections_created DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_procedure_document_sections PRIMARY KEY (procedure_document_section_id),
        CONSTRAINT FK_procedure_document_sections_version FOREIGN KEY (procedure_version_id) REFERENCES med.procedure_versions(procedure_version_id)
    );
END;

IF OBJECT_ID(N'med.procedure_distribution_recipients', N'U') IS NULL
BEGIN
    CREATE TABLE med.procedure_distribution_recipients (
        procedure_distribution_recipient_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_procedure_distribution_recipients_id DEFAULT NEWID(),
        procedure_version_id UNIQUEIDENTIFIER NOT NULL,
        display_order INT NOT NULL,
        recipient_name NVARCHAR(256) NOT NULL,
        is_marked BIT NOT NULL CONSTRAINT DF_procedure_distribution_recipients_marked DEFAULT 1,
        CONSTRAINT PK_procedure_distribution_recipients PRIMARY KEY (procedure_distribution_recipient_id),
        CONSTRAINT FK_procedure_distribution_recipients_version FOREIGN KEY (procedure_version_id) REFERENCES med.procedure_versions(procedure_version_id)
    );
END;

IF OBJECT_ID(N'med.procedure_revision_entries', N'U') IS NULL
BEGIN
    CREATE TABLE med.procedure_revision_entries (
        procedure_revision_entry_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_procedure_revision_entries_id DEFAULT NEWID(),
        procedure_version_id UNIQUEIDENTIFIER NOT NULL,
        display_order INT NOT NULL,
        revision_date DATETIME2(3) NULL,
        page_ref NVARCHAR(128) NULL,
        section_ref NVARCHAR(128) NULL,
        summary NVARCHAR(1024) NOT NULL,
        CONSTRAINT PK_procedure_revision_entries PRIMARY KEY (procedure_revision_entry_id),
        CONSTRAINT FK_procedure_revision_entries_version FOREIGN KEY (procedure_version_id) REFERENCES med.procedure_versions(procedure_version_id)
    );
END;

IF OBJECT_ID(N'med.procedure_signoff_records', N'U') IS NULL
BEGIN
    CREATE TABLE med.procedure_signoff_records (
        procedure_signoff_record_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_procedure_signoff_records_id DEFAULT NEWID(),
        procedure_version_id UNIQUEIDENTIFIER NOT NULL,
        signoff_role NVARCHAR(32) NOT NULL,
        display_order INT NOT NULL CONSTRAINT DF_procedure_signoff_records_order DEFAULT 0,
        signer_user_id UNIQUEIDENTIFIER NULL,
        signer_username NVARCHAR(256) NULL,
        signer_full_name NVARCHAR(256) NULL,
        signed_at DATETIME2(3) NOT NULL CONSTRAINT DF_procedure_signoff_records_signed DEFAULT SYSUTCDATETIME(),
        content_hash_sha256 NVARCHAR(128) NOT NULL,
        signature_image_data_url NVARCHAR(MAX) NULL,
        note NVARCHAR(1024) NULL,
        CONSTRAINT PK_procedure_signoff_records PRIMARY KEY (procedure_signoff_record_id),
        CONSTRAINT FK_procedure_signoff_records_version FOREIGN KEY (procedure_version_id) REFERENCES med.procedure_versions(procedure_version_id),
        CONSTRAINT CK_procedure_signoff_records_role CHECK (signoff_role IN (N'writer', N'checker', N'approver'))
    );
END;
GO

DECLARE @admin UNIQUEIDENTIFIER = (SELECT TOP (1) user_id FROM med.users WHERE username = N'admin');
DECLARE @dept UNIQUEIDENTIFIER = (
    SELECT TOP (1) department_id
    FROM med.departments
    ORDER BY CASE WHEN code IN (N'KSNK', N'K.KSNK') OR name LIKE N'%nhiem khuan%' THEN 0 ELSE 1 END, code);

DECLARE @seed TABLE (
    procedure_id UNIQUEIDENTIFIER,
    version_id UNIQUEIDENTIFIER,
    code NVARCHAR(64),
    title NVARCHAR(256),
    file_name NVARCHAR(512),
    checksum NVARCHAR(128),
    file_size BIGINT
);

INSERT INTO @seed VALUES
('f0000000-0000-0000-0000-000000000009','f1000000-0000-0000-0000-000000000009',N'QT.KSNK.09',N'Quy trinh xu ly dung cu phau thuat',N'2145_QUY TRÌNH XỬ LÝ DỤNG CỤ PHẪU THUẬT.pdf',N'C77CA23EA777CFFE94D28F110AB6A58BB8C630248FF04D1CD22A8A0C718C5C8A',30688980),
('f0000000-0000-0000-0000-000000000012','f1000000-0000-0000-0000-000000000012',N'QT.KSNK.12',N'Quy trinh xu ly dung cu y te',N'2145_QUY TRÌNH XỬ LÝ DỤNG CỤ Y TẾ.pdf',N'A81D27EF2338C86280A6F9A8300D5537A6A68BA4A3B74771BB987B9419166F44',26591000),
('f0000000-0000-0000-0000-000000000016','f1000000-0000-0000-0000-000000000016',N'QT.KSNK.16',N'Quy trinh khu khuan muc do cao dung cu y te',N'2145_QUY TRÌNH KHỬ KHUẨN MỨC ĐỘ CAO DỤNG CỤ Y TẾ.pdf',N'F0E0EE39369E3815FF6634A217555A15F68DD878D040CCBC3A23B23C8631892A',11543000),
('f0000000-0000-0000-0000-000000000017','f1000000-0000-0000-0000-000000000017',N'QT.KSNK.17',N'Quy trinh xu ly tay khoan nha khoa',N'2145_QUY TRÌNH XỬ LÝ TAY KHOAN NHA KHOA.pdf',N'40A3241A42BB0B803A75A599B55EEA95D6EC55917CEBF91FCF26F26D717CC4A5',6530255);

INSERT INTO med.professional_procedures (procedure_id, procedure_code, name, procedure_type, owner_department_id, description, status, created_by)
SELECT procedure_id, code, title, N'technical', @dept, N'Nhap tu PDF scan 2145; dang cho OCR day du tung trang truoc khi ban hanh.', N'active', @admin
FROM @seed s
WHERE NOT EXISTS (SELECT 1 FROM med.professional_procedures p WHERE p.procedure_code = s.code);

INSERT INTO med.procedure_versions (procedure_version_id, procedure_id, version_no, version_label, status_code, department_id, title, summary, change_reason, issue_date, issue_number, source_pdf_file_name, source_pdf_checksum_sha256, created_by)
SELECT version_id, procedure_id, 1, N'lan-02-scan', N'draft', @dept, title, N'OCR_PENDING: PDF scan la nguon su that; chi seed metadata va luu do da spot-check tu anh render.', N'Nhap quy trinh KSNK tu PDF scan', '2026-03-19', 2, file_name, checksum, @admin
FROM @seed s
WHERE NOT EXISTS (SELECT 1 FROM med.procedure_versions v WHERE v.procedure_version_id = s.version_id);

DECLARE @sections TABLE (section_order INT, section_number NVARCHAR(32), title NVARCHAR(256), kind NVARCHAR(64), body NVARCHAR(MAX));
INSERT INTO @sections VALUES
(1,N'I',N'Muc dich',N'purpose',N'OCR_PENDING: trich xuat tu PDF scan truoc khi ban hanh.'),
(2,N'II',N'Pham vi ap dung',N'scope',N'OCR_PENDING: ap dung theo dung pham vi trong PDF scan.'),
(3,N'III',N'Can cu va tai lieu vien dan',N'basis',N'Quyet dinh 3671/QD-BYT, quy dinh KSNK hien hanh va PDF scan nguon.'),
(4,N'IV',N'Thuat ngu va dinh nghia',N'definitions',N'OCR_PENDING: bo sung thuat ngu y te dung theo PDF scan.'),
(5,N'V',N'Trach nhiem',N'responsibilities',N'OCR_PENDING: nguoi viet, nguoi kiem tra, nguoi phe duyet va khoa/phong lien quan.'),
(6,N'VI',N'Noi nhan va phan phoi',N'distribution',N'Xem bang Noi nhan tren bia quy trinh.'),
(7,N'VII',N'Theo doi sua doi',N'revision',N'Xem bang Theo doi sua doi tren bia quy trinh.'),
(8,N'VIII',N'Noi dung quy trinh',N'procedure',N'OCR_PENDING: khong duoc ban hanh khi chua co OCR va spot-check tung trang.'),
(9,N'IX',N'Luu do',N'flowchart',N'Luu do duoc seed theo hinh trong PDF scan; can doi chieu lai khi OCR hoan tat.'),
(10,N'X',N'Ho so bieu mau va phu luc',N'records',N'OCR_PENDING: danh muc bieu mau/phu luc theo PDF scan.'),
(11,N'XI',N'Tep dinh kem',N'appendices',N'PDF scan nguon duoc gan kem voi checksum SHA-256.');

INSERT INTO med.procedure_document_sections (procedure_version_id, section_order, section_number, title, section_kind, content_text)
SELECT s.version_id, sec.section_order, sec.section_number, sec.title, sec.kind, sec.body
FROM @seed s CROSS JOIN @sections sec
WHERE NOT EXISTS (
    SELECT 1 FROM med.procedure_document_sections x
    WHERE x.procedure_version_id = s.version_id AND x.section_order = sec.section_order);

INSERT INTO med.procedure_distribution_recipients (procedure_version_id, display_order, recipient_name)
SELECT s.version_id, r.display_order, r.recipient_name
FROM @seed s
CROSS JOIN (VALUES (1,N'Ban Giam doc'),(2,N'Khoa Kiem soat nhiem khuan'),(3,N'Cac khoa/phong su dung dung cu')) r(display_order, recipient_name)
WHERE NOT EXISTS (
    SELECT 1 FROM med.procedure_distribution_recipients x
    WHERE x.procedure_version_id = s.version_id AND x.display_order = r.display_order);

INSERT INTO med.procedure_revision_entries (procedure_version_id, display_order, revision_date, page_ref, section_ref, summary)
SELECT version_id, 1, '2026-03-19', N'Toan van', N'Lan 02', N'Ban hanh theo PDF scan so 2145; noi dung chi tiet cho OCR/doi chieu.'
FROM @seed s
WHERE NOT EXISTS (SELECT 1 FROM med.procedure_revision_entries x WHERE x.procedure_version_id = s.version_id AND x.display_order = 1);

DECLARE @flow TABLE (code NVARCHAR(64), step_no INT, name NVARCHAR(256));
INSERT INTO @flow VALUES
(N'QT.KSNK.09',1,N'Lam sach dung cu'),(N'QT.KSNK.09',2,N'Giao nhan dung cu sau khi lam sach'),(N'QT.KSNK.09',3,N'Lam sach, khu khuan dung cu'),(N'QT.KSNK.09',4,N'Bao duong - kiem tra dung cu'),(N'QT.KSNK.09',5,N'Dong goi dung cu'),(N'QT.KSNK.09',6,N'Tiet khuan dung cu'),(N'QT.KSNK.09',7,N'Giam sat chat luong tiet khuan dung cu'),(N'QT.KSNK.09',8,N'Luu tru dung cu'),(N'QT.KSNK.09',9,N'Giao nhan dung cu sau khi tiet khuan'),
(N'QT.KSNK.12',1,N'Lam sach, khu khuan dung cu'),(N'QT.KSNK.12',2,N'Giao nhan dung cu sau khi lam sach'),(N'QT.KSNK.12',3,N'Lam sach, khu khuan dung cu'),(N'QT.KSNK.12',4,N'Bao duong - kiem tra dung cu'),(N'QT.KSNK.12',5,N'Dong goi dung cu'),(N'QT.KSNK.12',6,N'Tiet khuan dung cu'),(N'QT.KSNK.12',7,N'Giam sat chat luong tiet khuan dung cu'),(N'QT.KSNK.12',8,N'Luu tru dung cu'),(N'QT.KSNK.12',9,N'Giao nhan dung cu sau khi tiet khuan'),
(N'QT.KSNK.16',1,N'Lam sach dung cu'),(N'QT.KSNK.16',2,N'Giao nhan dung cu sau khi lam sach'),(N'QT.KSNK.16',3,N'Khu khuan muc do cao dung cu'),(N'QT.KSNK.16',4,N'Dong goi dung cu'),(N'QT.KSNK.16',5,N'Luu tru dung cu tai khoa KSNK'),(N'QT.KSNK.16',6,N'Giao nhan dung cu vo khuan'),
(N'QT.KSNK.17',1,N'Chuan bi'),(N'QT.KSNK.17',2,N'Lam sach'),(N'QT.KSNK.17',3,N'Khu khuan'),(N'QT.KSNK.17',4,N'Tra dau boi tron'),(N'QT.KSNK.17',5,N'Giao nhan dung cu sau khi lam sach, khu khuan'),(N'QT.KSNK.17',6,N'Dong goi'),(N'QT.KSNK.17',7,N'Tiet khuan'),(N'QT.KSNK.17',8,N'Luu tru tai khoa KSNK'),(N'QT.KSNK.17',9,N'Giao nhan dung cu sau khi tiet khuan');

INSERT INTO med.procedure_steps (procedure_version_id, step_no, step_code, name, description, responsibility_text, flow_shape_code, detail_section_number, standard_duration_minutes)
SELECT s.version_id, f.step_no, CONCAT(N'B', RIGHT(CONCAT(N'00', f.step_no), 2)), f.name,
       N'OCR_PENDING: dien giai chi tiet can trich xuat va doi chieu tu tung trang PDF scan.',
       CASE WHEN f.step_no = 1 THEN N'Khoa su dung / KSNK' ELSE N'Khoa KSNK' END,
       CASE WHEN f.step_no = 1 OR f.step_no = (SELECT MAX(step_no) FROM @flow z WHERE z.code = f.code) THEN N'terminator' ELSE N'process' END,
       N'VIII',
       10
FROM @seed s
JOIN @flow f ON f.code = s.code
WHERE NOT EXISTS (
    SELECT 1 FROM med.procedure_steps x
    WHERE x.procedure_version_id = s.version_id AND x.step_no = f.step_no);

INSERT INTO med.procedure_attachments (procedure_version_id, attachment_type, file_name, file_uri, mime_type, file_size_bytes, checksum_sha256, uploaded_by)
SELECT version_id, N'source_pdf', file_name, N'imported/' + file_name, N'application/pdf', file_size, checksum, @admin
FROM @seed s
WHERE NOT EXISTS (SELECT 1 FROM med.procedure_attachments x WHERE x.procedure_version_id = s.version_id AND x.file_name = s.file_name);

COMMIT TRANSACTION;
GO
