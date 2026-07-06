-- ============================================================
-- SEED DỮ LIỆU CẤU TRÚC BỆNH VIỆN THỰC TẾ
-- Chạy script này trong SSMS trên database MedicalProcedureManagement
-- Không chứa thông tin cá nhân - chỉ cấu trúc tổ chức
-- ============================================================

USE MedicalProcedureManagement;
GO

-- ============================================================
-- 1. KHOA/PHÒNG (departments)
-- ============================================================
-- Kiểm tra nếu chưa có root department
IF NOT EXISTS (SELECT 1 FROM med.departments WHERE code = N'BV-ROOT')
BEGIN
    INSERT INTO med.departments (code, name, parent_department_id, status)
    VALUES (N'BV-ROOT', N'Bệnh viện Đa khoa Trung ương', NULL, N'active');
END;
GO

DECLARE @rootId UNIQUEIDENTIFIER = (SELECT department_id FROM med.departments WHERE code = N'BV-ROOT');

-- Khối Nội
IF NOT EXISTS (SELECT 1 FROM med.departments WHERE code = N'KHOA-NOI-TQ')
    INSERT INTO med.departments (code, name, parent_department_id) VALUES (N'KHOA-NOI-TQ', N'Khoa Nội tổng quát', @rootId);
IF NOT EXISTS (SELECT 1 FROM med.departments WHERE code = N'KHOA-TIM-MACH')
    INSERT INTO med.departments (code, name, parent_department_id) VALUES (N'KHOA-TIM-MACH', N'Khoa Tim mạch', @rootId);
IF NOT EXISTS (SELECT 1 FROM med.departments WHERE code = N'KHOA-NOI-TIET')
    INSERT INTO med.departments (code, name, parent_department_id) VALUES (N'KHOA-NOI-TIET', N'Khoa Nội tiết', @rootId);
IF NOT EXISTS (SELECT 1 FROM med.departments WHERE code = N'KHOA-THAN-KINH')
    INSERT INTO med.departments (code, name, parent_department_id) VALUES (N'KHOA-THAN-KINH', N'Khoa Thần kinh', @rootId);

-- Khối Ngoại
IF NOT EXISTS (SELECT 1 FROM med.departments WHERE code = N'KHOA-NGOAI-TQ')
    INSERT INTO med.departments (code, name, parent_department_id) VALUES (N'KHOA-NGOAI-TQ', N'Khoa Ngoại tổng quát', @rootId);
IF NOT EXISTS (SELECT 1 FROM med.departments WHERE code = N'KHOA-CHINH-HINH')
    INSERT INTO med.departments (code, name, parent_department_id) VALUES (N'KHOA-CHINH-HINH', N'Khoa Chỉnh hình', @rootId);

-- Khối Sản - Nhi
IF NOT EXISTS (SELECT 1 FROM med.departments WHERE code = N'KHOA-SAN')
    INSERT INTO med.departments (code, name, parent_department_id) VALUES (N'KHOA-SAN', N'Khoa Sản', @rootId);
IF NOT EXISTS (SELECT 1 FROM med.departments WHERE code = N'KHOA-NHI')
    INSERT INTO med.departments (code, name, parent_department_id) VALUES (N'KHOA-NHI', N'Khoa Nhi', @rootId);

-- Khối Cận lâm sàng
IF NOT EXISTS (SELECT 1 FROM med.departments WHERE code = N'KHOA-XET-NGHIEM')
    INSERT INTO med.departments (code, name, parent_department_id) VALUES (N'KHOA-XET-NGHIEM', N'Khoa Xét nghiệm', @rootId);
IF NOT EXISTS (SELECT 1 FROM med.departments WHERE code = N'KHOA-CDHA')
    INSERT INTO med.departments (code, name, parent_department_id) VALUES (N'KHOA-CDHA', N'Khoa Chẩn đoán hình ảnh', @rootId);
IF NOT EXISTS (SELECT 1 FROM med.departments WHERE code = N'KHOA-GIAI-PHAU')
    INSERT INTO med.departments (code, name, parent_department_id) VALUES (N'KHOA-GIAI-PHAU', N'Khoa Giải phẫu bệnh', @rootId);

-- Khối Hỗ trợ
IF NOT EXISTS (SELECT 1 FROM med.departments WHERE code = N'KHOA-DUOC')
    INSERT INTO med.departments (code, name, parent_department_id) VALUES (N'KHOA-DUOC', N'Khoa Dược', @rootId);
IF NOT EXISTS (SELECT 1 FROM med.departments WHERE code = N'KHOA-KIEM-SOAT')
    INSERT INTO med.departments (code, name, parent_department_id) VALUES (N'KHOA-KIEM-SOAT', N'Khoa Kiểm soát nhiễm khuẩn', @rootId);
IF NOT EXISTS (SELECT 1 FROM med.departments WHERE code = N'PHONG-HANH-CHINH')
    INSERT INTO med.departments (code, name, parent_department_id) VALUES (N'PHONG-HANH-CHINH', N'Phòng Hành chính', @rootId);
IF NOT EXISTS (SELECT 1 FROM med.departments WHERE code = N'PHONG-KHTH')
    INSERT INTO med.departments (code, name, parent_department_id) VALUES (N'PHONG-KHTH', N'Phòng Kế hoạch tổng hợp', @rootId);

-- Khối Cấp cứu
IF NOT EXISTS (SELECT 1 FROM med.departments WHERE code = N'KHOA-CAP-CUU')
    INSERT INTO med.departments (code, name, parent_department_id) VALUES (N'KHOA-CAP-CUU', N'Khoa Cấp cứu', @rootId);
IF NOT EXISTS (SELECT 1 FROM med.departments WHERE code = N'KHOA-HSCC')
    INSERT INTO med.departments (code, name, parent_department_id) VALUES (N'KHOA-HSCC', N'Khoa Hồi sức cấp cứu', @rootId);
GO

-- ============================================================
-- 2. GÁN VAI TRÒ CHO ADMIN (nếu chưa có)
-- ============================================================
DECLARE @adminUserId UNIQUEIDENTIFIER = (SELECT user_id FROM med.users WHERE username = N'admin');
DECLARE @sysAdminRoleId UNIQUEIDENTIFIER = (SELECT role_id FROM med.roles WHERE code = N'SYSTEM_ADMIN');

IF @adminUserId IS NOT NULL AND @sysAdminRoleId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM med.user_roles WHERE user_id = @adminUserId AND role_id = @sysAdminRoleId AND effective_to IS NULL)
    BEGIN
        INSERT INTO med.user_roles (user_id, role_id) VALUES (@adminUserId, @sysAdminRoleId);
    END;
END;
GO

PRINT N'Seed dữ liệu cấu trúc bệnh viện hoàn tất.';
GO
