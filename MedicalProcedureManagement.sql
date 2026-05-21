/*
    SQL Server Full Database Script - v4 (consolidated hardening)
    Module: Quan Ly Quy Trinh Ky Thuat Chuyen Mon

    Changes from v3:
    - All hardening patches folded into earlier sections (no separate patch section).
    - NEWSEQUENTIALID() applied to all UUID PKs from the start.
    - Inline CHECK enums replaced by lookup tables with display_order/is_active/description columns.
    - All FK columns now have supporting non-clustered indexes.
    - Filtered uniqueness for active assignments and source-system mapping.
    - Computed-column unique indexes replace nullable-filtered ones for permissions / mappings / versions.
    - Schema-bound views and inline TVF for permission resolution.
    - Database role med_app_role with least-privilege grants; med_pii_unmask_role for DDM.
    - Department archival procedure med.sp_archive_department.
    - Multi-row guard for parent_department_id moves.
    - Trigger maintains is_final = 1 invariant on actual_resource_usages.
    - protocol_applicability_rules.rule_type migrated to lookup_protocol_rule_types FK.
    - actual_resource_usages "at most one final" enforced by trigger only (filtered index reduced to non-unique to avoid constraint-vs-trigger ordering conflict).
    - Requires SQL Server 2019+ (ProductMajorVersion 15) for UTF-8 collation.
*/

/* ============================================================
   00. CREATE DATABASE
   ============================================================ */

USE master;
GO

IF CAST(SERVERPROPERTY('ProductMajorVersion') AS INT) < 15
BEGIN
    THROW 51030, 'This script requires SQL Server 2019 (major version 15) or later for UTF-8 collation support.', 1;
END;
GO

IF DB_ID(N'MedicalProcedureManagement') IS NULL
BEGIN
    CREATE DATABASE MedicalProcedureManagement
    COLLATE Vietnamese_100_CI_AS_SC_UTF8;
END;
GO

ALTER DATABASE MedicalProcedureManagement SET RECOVERY FULL;
GO
-- Note: FULL recovery requires a full backup before log backups become effective. Run BACKUP DATABASE before scheduling log backups.

ALTER DATABASE MedicalProcedureManagement SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
GO

-- Ensure Vietnamese text is preserved:
-- 1. Keep all display columns as NVARCHAR.
-- 2. Always use Unicode string literals with N'...'.
-- 3. Save this .sql file as UTF-8 with BOM or run it in a Unicode-capable editor such as SSMS/Azure Data Studio.
GO

USE MedicalProcedureManagement;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET XACT_ABORT ON;
GO

/* ============================================================
   01. SCHEMA
   ============================================================ */

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'med')
    EXEC(N'CREATE SCHEMA med');
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'auth')
    EXEC(N'CREATE SCHEMA auth');
GO

/* ============================================================
   02. LOOKUP TABLES
   ============================================================ */

CREATE TABLE med.lookup_record_status (
    code NVARCHAR(30) NOT NULL,
    name NVARCHAR(100) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_record_status_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_record_status_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_record_status PRIMARY KEY (code)
);
GO

INSERT INTO med.lookup_record_status (code, name)
VALUES
(N'active', N'Đang hoạt động'),
(N'inactive', N'Ngừng hoạt động'),
(N'archived', N'Lưu trữ');
GO

CREATE TABLE med.lookup_action_codes (
    action_code NVARCHAR(30) NOT NULL,
    name NVARCHAR(100) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_action_codes_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_action_codes_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_action_codes PRIMARY KEY (action_code)
);
GO

INSERT INTO med.lookup_action_codes (action_code, name)
VALUES
(N'view', N'Xem'),
(N'create', N'Tạo mới'),
(N'update', N'Cập nhật'),
(N'delete', N'Xóa'),
(N'approve', N'Phê duyệt'),
(N'publish', N'Ban hành'),
(N'execute', N'Thực hiện'),
(N'export', N'Xuất dữ liệu'),
(N'configure', N'Cấu hình');
GO

CREATE TABLE med.lookup_department_scope_types (
    scope_type NVARCHAR(30) NOT NULL,
    name NVARCHAR(100) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_department_scope_types_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_department_scope_types_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_department_scope_types PRIMARY KEY (scope_type)
);
GO

INSERT INTO med.lookup_department_scope_types (scope_type, name)
VALUES
(N'global', N'Toàn hệ thống'),
(N'department', N'Một khoa/phòng'),
(N'department_tree', N'Cây khoa/phòng'),
(N'own_department', N'Khoa/phòng của người dùng'),
(N'custom', N'Quy tắc tùy chỉnh');
GO

CREATE TABLE med.lookup_permission_effects (
    effect_code NVARCHAR(10) NOT NULL,
    name NVARCHAR(100) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_permission_effects_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_permission_effects_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_permission_effects PRIMARY KEY (effect_code)
);
GO

INSERT INTO med.lookup_permission_effects (effect_code, name)
VALUES
(N'allow', N'Cho phép'),
(N'deny', N'Từ chối');
GO

CREATE TABLE med.lookup_version_statuses (
    status_code NVARCHAR(30) NOT NULL,
    name NVARCHAR(100) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_version_statuses_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_version_statuses_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_version_statuses PRIMARY KEY (status_code)
);
GO

INSERT INTO med.lookup_version_statuses (status_code, name)
VALUES
(N'draft', N'Bản nháp'),
(N'pending_approval', N'Chờ phê duyệt'),
(N'active', N'Đang hiệu lực'),
(N'superseded', N'Đã được thay thế'),
(N'archived', N'Lưu trữ'),
(N'rejected', N'Bị từ chối');
GO

CREATE TABLE med.lookup_enforcement_modes (
    enforcement_mode NVARCHAR(20) NOT NULL,
    name NVARCHAR(100) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_enforcement_modes_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_enforcement_modes_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_enforcement_modes PRIMARY KEY (enforcement_mode)
);
GO

INSERT INTO med.lookup_enforcement_modes (enforcement_mode, name)
VALUES
(N'off', N'Tắt'),
(N'warning', N'Cảnh báo'),
(N'block', N'Chặn');
GO

CREATE TABLE med.lookup_resource_types (
    resource_type NVARCHAR(30) NOT NULL,
    name NVARCHAR(100) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_resource_types_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_resource_types_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_resource_types PRIMARY KEY (resource_type)
);
GO

INSERT INTO med.lookup_resource_types (resource_type, name)
VALUES
(N'supply', N'Vật tư tiêu hao'),
(N'equipment', N'Thiết bị'),
(N'drug', N'Thuốc'),
(N'chemical', N'Hóa chất');
GO

CREATE TABLE med.lookup_notification_channels (
    channel_code NVARCHAR(30) NOT NULL,
    name NVARCHAR(100) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_notification_channels_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_notification_channels_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_notification_channels PRIMARY KEY (channel_code)
);
GO

INSERT INTO med.lookup_notification_channels (channel_code, name)
VALUES
(N'in_app', N'Trong ứng dụng'),
(N'email', N'Email'),
(N'sms', N'SMS'),
(N'zalo', N'Zalo'),
(N'webhook', N'Webhook');
GO

CREATE TABLE med.lookup_permission_change_statuses (
    change_status NVARCHAR(30) NOT NULL,
    name NVARCHAR(100) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_permission_change_statuses_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_permission_change_statuses_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_permission_change_statuses PRIMARY KEY (change_status)
);
GO

INSERT INTO med.lookup_permission_change_statuses (change_status, name)
VALUES
(N'draft', N'Bản nháp'),
(N'pending_approval', N'Chờ phê duyệt'),
(N'scheduled', N'Đã lên lịch'),
(N'applied', N'Đã áp dụng'),
(N'rejected', N'Bị từ chối'),
(N'failed', N'Thất bại'),
(N'cancelled', N'Đã hủy');
GO

CREATE TABLE med.lookup_permission_change_operations (
    operation_code NVARCHAR(20) NOT NULL,
    name NVARCHAR(100) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_permission_change_operations_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_permission_change_operations_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_permission_change_operations PRIMARY KEY (operation_code)
);
GO

INSERT INTO med.lookup_permission_change_operations (operation_code, name)
VALUES
(N'grant', N'Cấp quyền'),
(N'revoke', N'Thu hồi quyền'),
(N'update', N'Cập nhật quyền');
GO

CREATE TABLE med.unit_catalog (
    unit_code NVARCHAR(50) NOT NULL,
    name NVARCHAR(255) NOT NULL,
    unit_group NVARCHAR(100) NULL,
    status NVARCHAR(30) NOT NULL CONSTRAINT DF_unit_catalog_status DEFAULT N'active',
    display_order INT NOT NULL CONSTRAINT DF_unit_catalog_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_unit_catalog_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_unit_catalog PRIMARY KEY (unit_code),
    CONSTRAINT FK_unit_catalog_status FOREIGN KEY (status) REFERENCES med.lookup_record_status(code)
);
GO

INSERT INTO med.unit_catalog (unit_code, name, unit_group)
VALUES
(N'piece', N'Cái', N'count'),
(N'set', N'Bộ', N'count'),
(N'ml', N'Millilít', N'volume'),
(N'l', N'Lít', N'volume'),
(N'mg', N'Miligam', N'mass'),
(N'g', N'Gam', N'mass'),
(N'tablet', N'Viên', N'count'),
(N'ampoule', N'Ống', N'count');
GO

CREATE TABLE med.lookup_procedure_types (
    procedure_type NVARCHAR(50) NOT NULL,
    name NVARCHAR(255) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_procedure_types_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_procedure_types_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_procedure_types PRIMARY KEY (procedure_type)
);
GO

INSERT INTO med.lookup_procedure_types (procedure_type, name) VALUES
(N'technical', N'Kỹ thuật'),
(N'care', N'Chăm sóc'),
(N'surgery', N'Phẫu thuật'),
(N'procedure', N'Thủ thuật');
GO

CREATE TABLE med.lookup_service_types (
    service_type NVARCHAR(50) NOT NULL,
    name NVARCHAR(255) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_service_types_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_service_types_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_service_types PRIMARY KEY (service_type)
);
GO

INSERT INTO med.lookup_service_types (service_type, name) VALUES
(N'lab', N'Xét nghiệm'),
(N'imaging', N'Chẩn đoán hình ảnh'),
(N'procedure', N'Thủ thuật'),
(N'surgery', N'Phẫu thuật'),
(N'care', N'Chăm sóc'),
(N'other', N'Khác');
GO

CREATE TABLE med.lookup_protocol_types (
    protocol_type NVARCHAR(50) NOT NULL,
    name NVARCHAR(255) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_protocol_types_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_protocol_types_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_protocol_types PRIMARY KEY (protocol_type)
);
GO

INSERT INTO med.lookup_protocol_types (protocol_type, name) VALUES
(N'care', N'Chăm sóc'),
(N'treatment_protocol', N'Phác đồ điều trị'),
(N'surgery', N'Phẫu thuật'),
(N'procedure', N'Thủ thuật');
GO

CREATE TABLE med.lookup_attachment_types (
    attachment_type NVARCHAR(50) NOT NULL,
    name NVARCHAR(255) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_attachment_types_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_attachment_types_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_attachment_types PRIMARY KEY (attachment_type)
);
GO

INSERT INTO med.lookup_attachment_types (attachment_type, name) VALUES
(N'sop', N'SOP'),
(N'guideline', N'Hướng dẫn'),
(N'form', N'Biểu mẫu'),
(N'reference', N'Tham khảo'),
(N'other', N'Khác');
GO

CREATE TABLE med.lookup_order_statuses (
    order_status NVARCHAR(30) NOT NULL,
    name NVARCHAR(255) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_order_statuses_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_order_statuses_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_order_statuses PRIMARY KEY (order_status)
);
GO

INSERT INTO med.lookup_order_statuses (order_status, name) VALUES
(N'ordered', N'Đã chỉ định'),
(N'resource_warning', N'Cảnh báo nguồn lực'),
(N'scheduled', N'Đã lên lịch'),
(N'in_progress', N'Đang thực hiện'),
(N'completed', N'Hoàn thành'),
(N'cancelled', N'Đã hủy');
GO

CREATE TABLE med.lookup_availability_statuses (
    availability_status NVARCHAR(30) NOT NULL,
    name NVARCHAR(255) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_availability_statuses_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_availability_statuses_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_availability_statuses PRIMARY KEY (availability_status)
);
GO

INSERT INTO med.lookup_availability_statuses (availability_status, name) VALUES
(N'available', N'Sẵn sàng'),
(N'insufficient', N'Không đủ'),
(N'unknown', N'Chưa xác định'),
(N'adapter_error', N'Lỗi adapter');
GO

CREATE TABLE med.lookup_permission_change_target_types (
    target_type NVARCHAR(20) NOT NULL,
    name NVARCHAR(255) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_permission_change_target_types_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_permission_change_target_types_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_permission_change_target_types PRIMARY KEY (target_type)
);
GO

INSERT INTO med.lookup_permission_change_target_types (target_type, name) VALUES
(N'role', N'Vai trò'),
(N'group', N'Nhóm'),
(N'user', N'Người dùng');
GO

CREATE TABLE med.lookup_protocol_relation_types (
    relation_type NVARCHAR(50) NOT NULL,
    name NVARCHAR(255) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_protocol_relation_types_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_protocol_relation_types_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_protocol_relation_types PRIMARY KEY (relation_type)
);
GO

INSERT INTO med.lookup_protocol_relation_types (relation_type, name) VALUES
(N'references', N'Tham chiếu'),
(N'requires', N'Bắt buộc'),
(N'optional', N'Tùy chọn');
GO

CREATE TABLE med.lookup_protocol_application_statuses (
    application_status NVARCHAR(30) NOT NULL,
    name NVARCHAR(255) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_protocol_application_statuses_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_protocol_application_statuses_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_protocol_application_statuses PRIMARY KEY (application_status)
);
GO

INSERT INTO med.lookup_protocol_application_statuses (application_status, name) VALUES
(N'suggested', N'Đề xuất'),
(N'applied', N'Đã áp dụng'),
(N'skipped', N'Bỏ qua'),
(N'cancelled', N'Đã hủy');
GO

CREATE TABLE med.lookup_delivery_statuses (
    delivery_status NVARCHAR(30) NOT NULL,
    name NVARCHAR(255) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_delivery_statuses_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_delivery_statuses_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_delivery_statuses PRIMARY KEY (delivery_status)
);
GO

INSERT INTO med.lookup_delivery_statuses (delivery_status, name) VALUES
(N'pending', N'Chờ gửi'),
(N'sent', N'Đã gửi'),
(N'failed', N'Thất bại'),
(N'skipped', N'Bỏ qua');
GO

CREATE TABLE med.lookup_notification_severities (
    severity NVARCHAR(20) NOT NULL,
    name NVARCHAR(255) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_notification_severities_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_notification_severities_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_notification_severities PRIMARY KEY (severity)
);
GO

INSERT INTO med.lookup_notification_severities (severity, name) VALUES
(N'info', N'Thông tin'),
(N'warning', N'Cảnh báo'),
(N'critical', N'Nghiêm trọng');
GO

CREATE TABLE med.lookup_genders (
    gender_code NVARCHAR(20) NOT NULL,
    name NVARCHAR(255) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_genders_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_genders_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_genders PRIMARY KEY (gender_code)
);
GO

INSERT INTO med.lookup_genders (gender_code, name) VALUES
(N'male', N'Nam'),
(N'female', N'Nữ'),
(N'other', N'Khác'),
(N'unknown', N'Không xác định');
GO

CREATE TABLE med.lookup_protocol_rule_types (
    rule_type NVARCHAR(50) NOT NULL,
    name NVARCHAR(255) NOT NULL,
    display_order INT NOT NULL CONSTRAINT DF_lookup_protocol_rule_types_display_order DEFAULT 0,
    is_active BIT NOT NULL CONSTRAINT DF_lookup_protocol_rule_types_is_active DEFAULT 1,
    description NVARCHAR(500) NULL,
    CONSTRAINT PK_lookup_protocol_rule_types PRIMARY KEY (rule_type)
);
GO

INSERT INTO med.lookup_protocol_rule_types (rule_type, name) VALUES
(N'icd', N'Mã ICD'),
(N'patient_group', N'Nhóm bệnh nhân'),
(N'department', N'Khoa/phòng'),
(N'age', N'Độ tuổi'),
(N'gender', N'Giới tính'),
(N'condition', N'Tình trạng'),
(N'contraindication', N'Chống chỉ định');
GO

/* ============================================================
   03. ORGANIZATION AND IDENTITY
   ============================================================ */

CREATE TABLE med.departments (
    department_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_departments_id DEFAULT NEWSEQUENTIALID(),
    parent_department_id UNIQUEIDENTIFIER NULL,
    code NVARCHAR(50) NOT NULL,
    name NVARCHAR(255) NOT NULL,
    status NVARCHAR(30) NOT NULL CONSTRAINT DF_departments_status DEFAULT N'active',
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_departments_created_at DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2(3) NOT NULL CONSTRAINT DF_departments_updated_at DEFAULT SYSUTCDATETIME(),
    row_version ROWVERSION NOT NULL,
    CONSTRAINT PK_departments PRIMARY KEY (department_id),
    CONSTRAINT UQ_departments_code UNIQUE (code),
    CONSTRAINT FK_departments_parent FOREIGN KEY (parent_department_id) REFERENCES med.departments(department_id),
    CONSTRAINT FK_departments_status FOREIGN KEY (status) REFERENCES med.lookup_record_status(code)
);
GO

CREATE TABLE med.department_closure (
    ancestor_department_id UNIQUEIDENTIFIER NOT NULL,
    descendant_department_id UNIQUEIDENTIFIER NOT NULL,
    depth INT NOT NULL,
    CONSTRAINT PK_department_closure PRIMARY KEY (ancestor_department_id, descendant_department_id),
    CONSTRAINT FK_department_closure_ancestor FOREIGN KEY (ancestor_department_id) REFERENCES med.departments(department_id),
    CONSTRAINT FK_department_closure_descendant FOREIGN KEY (descendant_department_id) REFERENCES med.departments(department_id),
    CONSTRAINT CK_department_closure_depth CHECK (depth >= 0)
);
GO

CREATE INDEX IX_department_closure_descendant
ON med.department_closure(descendant_department_id, ancestor_department_id, depth);
GO

-- Admin-only emergency rebuild. Do not run during normal operations.
CREATE OR ALTER PROCEDURE med.sp_rebuild_department_closure_admin
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM med.department_closure;

    ;WITH dept_tree AS (
        SELECT
            d.department_id AS ancestor_department_id,
            d.department_id AS descendant_department_id,
            CAST(0 AS INT) AS depth
        FROM med.departments d

        UNION ALL

        SELECT
            dt.ancestor_department_id,
            child.department_id AS descendant_department_id,
            dt.depth + 1 AS depth
        FROM dept_tree dt
        JOIN med.departments child
            ON child.parent_department_id = dt.descendant_department_id
    )
    INSERT INTO med.department_closure (ancestor_department_id, descendant_department_id, depth)
    SELECT ancestor_department_id, descendant_department_id, depth
    FROM dept_tree
    OPTION (MAXRECURSION 32767);
END;
GO

CREATE OR ALTER TRIGGER med.TR_departments_no_delete
ON med.departments
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51020, 'Physical DELETE on departments is not allowed. Use status = archived.', 1;
END;
GO

CREATE OR ALTER TRIGGER med.TR_departments_insert_closure
ON med.departments
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO med.department_closure (ancestor_department_id, descendant_department_id, depth)
    SELECT i.department_id, i.department_id, 0
    FROM inserted i
    WHERE NOT EXISTS (
        SELECT 1
        FROM med.department_closure dc
        WHERE dc.ancestor_department_id = i.department_id
          AND dc.descendant_department_id = i.department_id
    );

    INSERT INTO med.department_closure (ancestor_department_id, descendant_department_id, depth)
    SELECT parent_paths.ancestor_department_id, i.department_id, parent_paths.depth + 1
    FROM inserted i
    JOIN med.department_closure parent_paths
        ON parent_paths.descendant_department_id = i.parent_department_id
    WHERE i.parent_department_id IS NOT NULL
      AND NOT EXISTS (
          SELECT 1
          FROM med.department_closure existing
          WHERE existing.ancestor_department_id = parent_paths.ancestor_department_id
            AND existing.descendant_department_id = i.department_id
      );
END;
GO

CREATE OR ALTER TRIGGER med.TR_departments_update_parent_closure
ON med.departments
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT UPDATE(parent_department_id) RETURN;

    IF (SELECT COUNT(*) FROM inserted i
         JOIN deleted d ON d.department_id = i.department_id
         WHERE ISNULL(CONVERT(NVARCHAR(36), i.parent_department_id), N'') <> ISNULL(CONVERT(NVARCHAR(36), d.parent_department_id), N'')) > 1
        THROW 51022, 'Multi-row UPDATE of parent_department_id is not supported.', 1;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN med.department_closure dc
            ON dc.ancestor_department_id = i.department_id
           AND dc.descendant_department_id = i.parent_department_id
        WHERE i.parent_department_id IS NOT NULL
    )
    BEGIN
        THROW 51021, 'Cannot move department under its own descendant.', 1;
    END;

    ;WITH moved AS (
        SELECT i.department_id, i.parent_department_id AS new_parent_department_id
        FROM inserted i
        JOIN deleted d ON d.department_id = i.department_id
        WHERE ISNULL(CONVERT(NVARCHAR(36), i.parent_department_id), N'') <> ISNULL(CONVERT(NVARCHAR(36), d.parent_department_id), N'')
    ), subtree AS (
        SELECT m.department_id AS root_department_id,
               dc.descendant_department_id,
               dc.depth AS depth_from_root
        FROM moved m
        JOIN med.department_closure dc
            ON dc.ancestor_department_id = m.department_id
    )
    DELETE dc
    FROM med.department_closure dc
    JOIN subtree s ON s.descendant_department_id = dc.descendant_department_id
    WHERE dc.ancestor_department_id NOT IN (
        SELECT descendant_department_id
        FROM subtree s2
        WHERE s2.root_department_id = s.root_department_id
    );

    ;WITH moved AS (
        SELECT i.department_id, i.parent_department_id AS new_parent_department_id
        FROM inserted i
        JOIN deleted d ON d.department_id = i.department_id
        WHERE ISNULL(CONVERT(NVARCHAR(36), i.parent_department_id), N'') <> ISNULL(CONVERT(NVARCHAR(36), d.parent_department_id), N'')
    ), subtree AS (
        SELECT m.department_id AS root_department_id,
               dc.descendant_department_id,
               dc.depth AS depth_from_root
        FROM moved m
        JOIN med.department_closure dc
            ON dc.ancestor_department_id = m.department_id
    ), new_ancestors AS (
        SELECT m.department_id AS root_department_id,
               p.ancestor_department_id,
               p.depth AS ancestor_depth
        FROM moved m
        JOIN med.department_closure p
            ON p.descendant_department_id = m.new_parent_department_id
        WHERE m.new_parent_department_id IS NOT NULL
    )
    INSERT INTO med.department_closure (ancestor_department_id, descendant_department_id, depth)
    SELECT na.ancestor_department_id,
           s.descendant_department_id,
           na.ancestor_depth + s.depth_from_root + 1
    FROM new_ancestors na
    JOIN subtree s ON s.root_department_id = na.root_department_id
    WHERE NOT EXISTS (
        SELECT 1
        FROM med.department_closure existing
        WHERE existing.ancestor_department_id = na.ancestor_department_id
          AND existing.descendant_department_id = s.descendant_department_id
    );
END;
GO

CREATE TABLE med.users (
    user_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_users_id DEFAULT NEWSEQUENTIALID(),
    external_auth_id NVARCHAR(255) NULL,
    username NVARCHAR(100) NOT NULL,
    email NVARCHAR(320) NULL,
    full_name NVARCHAR(255) NOT NULL,
    primary_department_id UNIQUEIDENTIFIER NULL,
    status NVARCHAR(30) NOT NULL CONSTRAINT DF_users_status DEFAULT N'active',
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_users_created_at DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2(3) NOT NULL CONSTRAINT DF_users_updated_at DEFAULT SYSUTCDATETIME(),
    deleted_at DATETIME2(3) NULL,
    row_version ROWVERSION NOT NULL,
    password_hash NVARCHAR(500) NULL,
    CONSTRAINT PK_users PRIMARY KEY (user_id),
    CONSTRAINT UQ_users_username UNIQUE (username),
    CONSTRAINT FK_users_primary_department FOREIGN KEY (primary_department_id) REFERENCES med.departments(department_id),
    CONSTRAINT FK_users_status FOREIGN KEY (status) REFERENCES med.lookup_record_status(code)
);
GO

CREATE UNIQUE INDEX UX_users_external_auth_id_not_null ON med.users(external_auth_id) WHERE external_auth_id IS NOT NULL;
GO
CREATE UNIQUE INDEX UX_users_email_not_null ON med.users(email) WHERE email IS NOT NULL;
GO

CREATE INDEX IX_users_primary_department ON med.users(primary_department_id);
GO

CREATE TABLE auth.identity_users (
    Id UNIQUEIDENTIFIER NOT NULL,
    MedUserId UNIQUEIDENTIFIER NULL,
    FullName NVARCHAR(255) NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_identity_users_status DEFAULT N'active',
    UserName NVARCHAR(256) NULL,
    NormalizedUserName NVARCHAR(256) NULL,
    Email NVARCHAR(256) NULL,
    NormalizedEmail NVARCHAR(256) NULL,
    EmailConfirmed BIT NOT NULL,
    PasswordHash NVARCHAR(MAX) NULL,
    SecurityStamp NVARCHAR(MAX) NULL,
    ConcurrencyStamp NVARCHAR(MAX) NULL,
    PhoneNumber NVARCHAR(MAX) NULL,
    PhoneNumberConfirmed BIT NOT NULL,
    TwoFactorEnabled BIT NOT NULL,
    LockoutEnd DATETIMEOFFSET NULL,
    LockoutEnabled BIT NOT NULL,
    AccessFailedCount INT NOT NULL,
    CONSTRAINT PK_identity_users PRIMARY KEY (Id),
    CONSTRAINT FK_identity_users_med_users FOREIGN KEY (MedUserId) REFERENCES med.users(user_id),
    CONSTRAINT FK_identity_users_status FOREIGN KEY (Status) REFERENCES med.lookup_record_status(code)
);
GO

CREATE TABLE auth.identity_roles (
    Id UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(256) NULL,
    NormalizedName NVARCHAR(256) NULL,
    ConcurrencyStamp NVARCHAR(MAX) NULL,
    CONSTRAINT PK_identity_roles PRIMARY KEY (Id)
);
GO

CREATE TABLE auth.identity_user_roles (
    UserId UNIQUEIDENTIFIER NOT NULL,
    RoleId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_identity_user_roles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_identity_user_roles_users FOREIGN KEY (UserId) REFERENCES auth.identity_users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_identity_user_roles_roles FOREIGN KEY (RoleId) REFERENCES auth.identity_roles(Id) ON DELETE CASCADE
);
GO

CREATE TABLE auth.identity_user_claims (
    Id INT IDENTITY(1,1) NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    ClaimType NVARCHAR(MAX) NULL,
    ClaimValue NVARCHAR(MAX) NULL,
    CONSTRAINT PK_identity_user_claims PRIMARY KEY (Id),
    CONSTRAINT FK_identity_user_claims_users FOREIGN KEY (UserId) REFERENCES auth.identity_users(Id) ON DELETE CASCADE
);
GO

CREATE TABLE auth.identity_user_logins (
    LoginProvider NVARCHAR(128) NOT NULL,
    ProviderKey NVARCHAR(128) NOT NULL,
    ProviderDisplayName NVARCHAR(MAX) NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_identity_user_logins PRIMARY KEY (LoginProvider, ProviderKey),
    CONSTRAINT FK_identity_user_logins_users FOREIGN KEY (UserId) REFERENCES auth.identity_users(Id) ON DELETE CASCADE
);
GO

CREATE TABLE auth.identity_role_claims (
    Id INT IDENTITY(1,1) NOT NULL,
    RoleId UNIQUEIDENTIFIER NOT NULL,
    ClaimType NVARCHAR(MAX) NULL,
    ClaimValue NVARCHAR(MAX) NULL,
    CONSTRAINT PK_identity_role_claims PRIMARY KEY (Id),
    CONSTRAINT FK_identity_role_claims_roles FOREIGN KEY (RoleId) REFERENCES auth.identity_roles(Id) ON DELETE CASCADE
);
GO

CREATE TABLE auth.identity_user_tokens (
    UserId UNIQUEIDENTIFIER NOT NULL,
    LoginProvider NVARCHAR(128) NOT NULL,
    Name NVARCHAR(128) NOT NULL,
    Value NVARCHAR(MAX) NULL,
    CONSTRAINT PK_identity_user_tokens PRIMARY KEY (UserId, LoginProvider, Name),
    CONSTRAINT FK_identity_user_tokens_users FOREIGN KEY (UserId) REFERENCES auth.identity_users(Id) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX UX_identity_users_med_user ON auth.identity_users(MedUserId) WHERE MedUserId IS NOT NULL;
GO
CREATE UNIQUE INDEX UX_identity_users_normalized_username ON auth.identity_users(NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
GO
CREATE INDEX IX_identity_users_normalized_email ON auth.identity_users(NormalizedEmail);
GO
CREATE UNIQUE INDEX UX_identity_roles_normalized_name ON auth.identity_roles(NormalizedName) WHERE NormalizedName IS NOT NULL;
GO
CREATE INDEX IX_identity_user_roles_role_id ON auth.identity_user_roles(RoleId);
GO
CREATE INDEX IX_identity_user_claims_user_id ON auth.identity_user_claims(UserId);
GO
CREATE INDEX IX_identity_user_logins_user_id ON auth.identity_user_logins(UserId);
GO
CREATE INDEX IX_identity_role_claims_role_id ON auth.identity_role_claims(RoleId);
GO

CREATE TABLE med.roles (
    role_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_roles_id DEFAULT NEWSEQUENTIALID(),
    code NVARCHAR(80) NOT NULL,
    name NVARCHAR(255) NOT NULL,
    description NVARCHAR(1000) NULL,
    is_system BIT NOT NULL CONSTRAINT DF_roles_is_system DEFAULT 0,
    status NVARCHAR(30) NOT NULL CONSTRAINT DF_roles_status DEFAULT N'active',
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_roles_created_at DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2(3) NOT NULL CONSTRAINT DF_roles_updated_at DEFAULT SYSUTCDATETIME(),
    row_version ROWVERSION NOT NULL,
    CONSTRAINT PK_roles PRIMARY KEY (role_id),
    CONSTRAINT UQ_roles_code UNIQUE (code),
    CONSTRAINT FK_roles_status FOREIGN KEY (status) REFERENCES med.lookup_record_status(code)
);
GO

CREATE TABLE med.groups (
    group_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_groups_id DEFAULT NEWSEQUENTIALID(),
    code NVARCHAR(80) NOT NULL,
    name NVARCHAR(255) NOT NULL,
    department_id UNIQUEIDENTIFIER NULL,
    description NVARCHAR(1000) NULL,
    status NVARCHAR(30) NOT NULL CONSTRAINT DF_groups_status DEFAULT N'active',
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_groups_created_at DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2(3) NOT NULL CONSTRAINT DF_groups_updated_at DEFAULT SYSUTCDATETIME(),
    row_version ROWVERSION NOT NULL,
    CONSTRAINT PK_groups PRIMARY KEY (group_id),
    CONSTRAINT UQ_groups_code UNIQUE (code),
    CONSTRAINT FK_groups_department FOREIGN KEY (department_id) REFERENCES med.departments(department_id),
    CONSTRAINT FK_groups_status FOREIGN KEY (status) REFERENCES med.lookup_record_status(code)
);
GO

CREATE TABLE med.user_roles (
    user_role_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_user_roles_id DEFAULT NEWSEQUENTIALID(),
    user_id UNIQUEIDENTIFIER NOT NULL,
    role_id UNIQUEIDENTIFIER NOT NULL,
    department_id UNIQUEIDENTIFIER NULL,
    effective_from DATETIME2(3) NOT NULL CONSTRAINT DF_user_roles_effective_from DEFAULT SYSUTCDATETIME(),
    effective_to DATETIME2(3) NULL,
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_user_roles_created_at DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_user_roles PRIMARY KEY (user_role_id),
    CONSTRAINT FK_user_roles_user FOREIGN KEY (user_id) REFERENCES med.users(user_id),
    CONSTRAINT FK_user_roles_role FOREIGN KEY (role_id) REFERENCES med.roles(role_id),
    CONSTRAINT FK_user_roles_department FOREIGN KEY (department_id) REFERENCES med.departments(department_id),
    CONSTRAINT CK_user_roles_dates CHECK (effective_to IS NULL OR effective_to > effective_from)
);
GO

CREATE INDEX IX_user_roles_user_effective ON med.user_roles(user_id, effective_from, effective_to);
GO
CREATE INDEX IX_user_roles_role_effective ON med.user_roles(role_id, effective_from, effective_to);
GO

CREATE INDEX IX_user_roles_department ON med.user_roles(department_id);
GO

CREATE UNIQUE INDEX UX_user_roles_active_with_dept
  ON med.user_roles(user_id, role_id, department_id)
  WHERE effective_to IS NULL AND department_id IS NOT NULL;
GO

CREATE UNIQUE INDEX UX_user_roles_active_no_dept
  ON med.user_roles(user_id, role_id)
  WHERE effective_to IS NULL AND department_id IS NULL;
GO

CREATE TABLE med.user_group_members (
    user_group_member_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_user_group_members_id DEFAULT NEWSEQUENTIALID(),
    user_id UNIQUEIDENTIFIER NOT NULL,
    group_id UNIQUEIDENTIFIER NOT NULL,
    effective_from DATETIME2(3) NOT NULL CONSTRAINT DF_user_group_members_effective_from DEFAULT SYSUTCDATETIME(),
    effective_to DATETIME2(3) NULL,
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_user_group_members_created_at DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_user_group_members PRIMARY KEY (user_group_member_id),
    CONSTRAINT FK_user_group_members_user FOREIGN KEY (user_id) REFERENCES med.users(user_id),
    CONSTRAINT FK_user_group_members_group FOREIGN KEY (group_id) REFERENCES med.groups(group_id),
    CONSTRAINT CK_user_group_members_dates CHECK (effective_to IS NULL OR effective_to > effective_from)
);
GO

CREATE INDEX IX_user_group_members_user_effective ON med.user_group_members(user_id, effective_from, effective_to);
GO
CREATE INDEX IX_user_group_members_group_effective ON med.user_group_members(group_id, effective_from, effective_to);
GO

CREATE UNIQUE INDEX UX_user_group_members_active
  ON med.user_group_members(user_id, group_id)
  WHERE effective_to IS NULL;
GO

/* ============================================================
   04. SCREEN, FEATURE, PERMISSION CATALOG
   ============================================================ */

CREATE TABLE med.screen_catalog (
    screen_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_screen_catalog_id DEFAULT NEWSEQUENTIALID(),
    screen_code NVARCHAR(100) NOT NULL,
    name NVARCHAR(255) NOT NULL,
    route NVARCHAR(500) NULL,
    module_code NVARCHAR(100) NULL,
    status NVARCHAR(30) NOT NULL CONSTRAINT DF_screen_catalog_status DEFAULT N'active',
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_screen_catalog_created_at DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2(3) NOT NULL CONSTRAINT DF_screen_catalog_updated_at DEFAULT SYSUTCDATETIME(),
    row_version ROWVERSION NOT NULL,
    CONSTRAINT PK_screen_catalog PRIMARY KEY (screen_id),
    CONSTRAINT UQ_screen_catalog_code UNIQUE (screen_code),
    CONSTRAINT FK_screen_catalog_status FOREIGN KEY (status) REFERENCES med.lookup_record_status(code)
);
GO

CREATE TABLE med.feature_catalog (
    feature_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_feature_catalog_id DEFAULT NEWSEQUENTIALID(),
    screen_id UNIQUEIDENTIFIER NOT NULL,
    feature_code NVARCHAR(100) NOT NULL,
    name NVARCHAR(255) NOT NULL,
    description NVARCHAR(1000) NULL,
    status NVARCHAR(30) NOT NULL CONSTRAINT DF_feature_catalog_status DEFAULT N'active',
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_feature_catalog_created_at DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2(3) NOT NULL CONSTRAINT DF_feature_catalog_updated_at DEFAULT SYSUTCDATETIME(),
    row_version ROWVERSION NOT NULL,
    CONSTRAINT PK_feature_catalog PRIMARY KEY (feature_id),
    CONSTRAINT UQ_feature_catalog_code UNIQUE (feature_code),
    CONSTRAINT UQ_feature_catalog_screen_feature UNIQUE (screen_id, feature_code),
    CONSTRAINT FK_feature_catalog_screen FOREIGN KEY (screen_id) REFERENCES med.screen_catalog(screen_id),
    CONSTRAINT FK_feature_catalog_status FOREIGN KEY (status) REFERENCES med.lookup_record_status(code)
);
GO

CREATE TABLE med.permissions (
    permission_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_permissions_id DEFAULT NEWSEQUENTIALID(),
    permission_code NVARCHAR(220) NOT NULL,
    screen_id UNIQUEIDENTIFIER NOT NULL,
    feature_id UNIQUEIDENTIFIER NULL,
    action_code NVARCHAR(30) NOT NULL,
    description NVARCHAR(1000) NULL,
    status NVARCHAR(30) NOT NULL CONSTRAINT DF_permissions_status DEFAULT N'active',
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_permissions_created_at DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2(3) NOT NULL CONSTRAINT DF_permissions_updated_at DEFAULT SYSUTCDATETIME(),
    row_version ROWVERSION NOT NULL,
    CONSTRAINT PK_permissions PRIMARY KEY (permission_id),
    CONSTRAINT UQ_permissions_code UNIQUE (permission_code),
    CONSTRAINT FK_permissions_screen FOREIGN KEY (screen_id) REFERENCES med.screen_catalog(screen_id),
    CONSTRAINT FK_permissions_feature FOREIGN KEY (feature_id) REFERENCES med.feature_catalog(feature_id),
    CONSTRAINT FK_permissions_action FOREIGN KEY (action_code) REFERENCES med.lookup_action_codes(action_code),
    CONSTRAINT FK_permissions_status FOREIGN KEY (status) REFERENCES med.lookup_record_status(code)
);
GO

ALTER TABLE med.permissions ADD feature_id_key AS ISNULL(feature_id, '00000000-0000-0000-0000-000000000000') PERSISTED;
GO
CREATE UNIQUE INDEX UX_permissions_natural ON med.permissions(screen_id, feature_id_key, action_code);
GO

/* ============================================================
   05. STRICT RBAC/ABAC ASSIGNMENTS
   ============================================================ */

CREATE TABLE med.role_permissions (
    role_permission_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_role_permissions_id DEFAULT NEWSEQUENTIALID(),
    role_id UNIQUEIDENTIFIER NOT NULL,
    permission_id UNIQUEIDENTIFIER NOT NULL,
    effect_code NVARCHAR(10) NOT NULL CONSTRAINT DF_role_permissions_effect DEFAULT N'allow',
    department_scope_type NVARCHAR(30) NOT NULL CONSTRAINT DF_role_permissions_scope DEFAULT N'global',
    department_id UNIQUEIDENTIFIER NULL,
    scope_rule_json NVARCHAR(MAX) NULL,
    priority INT NOT NULL CONSTRAINT DF_role_permissions_priority DEFAULT 100,
    reason NVARCHAR(1000) NULL,
    effective_from DATETIME2(3) NOT NULL CONSTRAINT DF_role_permissions_effective_from DEFAULT SYSUTCDATETIME(),
    effective_to DATETIME2(3) NULL,
    created_by UNIQUEIDENTIFIER NULL,
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_role_permissions_created_at DEFAULT SYSUTCDATETIME(),
    row_version ROWVERSION NOT NULL,
    CONSTRAINT PK_role_permissions PRIMARY KEY (role_permission_id),
    CONSTRAINT FK_role_permissions_role FOREIGN KEY (role_id) REFERENCES med.roles(role_id),
    CONSTRAINT FK_role_permissions_permission FOREIGN KEY (permission_id) REFERENCES med.permissions(permission_id),
    CONSTRAINT FK_role_permissions_effect FOREIGN KEY (effect_code) REFERENCES med.lookup_permission_effects(effect_code),
    CONSTRAINT FK_role_permissions_scope FOREIGN KEY (department_scope_type) REFERENCES med.lookup_department_scope_types(scope_type),
    CONSTRAINT FK_role_permissions_department FOREIGN KEY (department_id) REFERENCES med.departments(department_id),
    CONSTRAINT FK_role_permissions_created_by FOREIGN KEY (created_by) REFERENCES med.users(user_id),
    CONSTRAINT CK_role_permissions_dates CHECK (effective_to IS NULL OR effective_to > effective_from),
    CONSTRAINT CK_role_permissions_scope_json CHECK (scope_rule_json IS NULL OR ISJSON(scope_rule_json) = 1)
);
GO

CREATE INDEX IX_role_permissions_lookup ON med.role_permissions(role_id, permission_id, effective_from, effective_to);
GO

CREATE INDEX IX_role_permissions_department ON med.role_permissions(department_id);
GO
CREATE INDEX IX_role_permissions_permission ON med.role_permissions(permission_id);
GO

CREATE UNIQUE INDEX UX_role_permissions_active_with_dept
  ON med.role_permissions(role_id, permission_id, department_scope_type, department_id)
  WHERE effective_to IS NULL AND department_id IS NOT NULL;
GO
CREATE UNIQUE INDEX UX_role_permissions_active_no_dept
  ON med.role_permissions(role_id, permission_id, department_scope_type)
  WHERE effective_to IS NULL AND department_id IS NULL;
GO

CREATE TABLE med.group_permissions (
    group_permission_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_group_permissions_id DEFAULT NEWSEQUENTIALID(),
    group_id UNIQUEIDENTIFIER NOT NULL,
    permission_id UNIQUEIDENTIFIER NOT NULL,
    effect_code NVARCHAR(10) NOT NULL CONSTRAINT DF_group_permissions_effect DEFAULT N'allow',
    department_scope_type NVARCHAR(30) NOT NULL CONSTRAINT DF_group_permissions_scope DEFAULT N'global',
    department_id UNIQUEIDENTIFIER NULL,
    scope_rule_json NVARCHAR(MAX) NULL,
    priority INT NOT NULL CONSTRAINT DF_group_permissions_priority DEFAULT 200,
    reason NVARCHAR(1000) NULL,
    effective_from DATETIME2(3) NOT NULL CONSTRAINT DF_group_permissions_effective_from DEFAULT SYSUTCDATETIME(),
    effective_to DATETIME2(3) NULL,
    created_by UNIQUEIDENTIFIER NULL,
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_group_permissions_created_at DEFAULT SYSUTCDATETIME(),
    row_version ROWVERSION NOT NULL,
    CONSTRAINT PK_group_permissions PRIMARY KEY (group_permission_id),
    CONSTRAINT FK_group_permissions_group FOREIGN KEY (group_id) REFERENCES med.groups(group_id),
    CONSTRAINT FK_group_permissions_permission FOREIGN KEY (permission_id) REFERENCES med.permissions(permission_id),
    CONSTRAINT FK_group_permissions_effect FOREIGN KEY (effect_code) REFERENCES med.lookup_permission_effects(effect_code),
    CONSTRAINT FK_group_permissions_scope FOREIGN KEY (department_scope_type) REFERENCES med.lookup_department_scope_types(scope_type),
    CONSTRAINT FK_group_permissions_department FOREIGN KEY (department_id) REFERENCES med.departments(department_id),
    CONSTRAINT FK_group_permissions_created_by FOREIGN KEY (created_by) REFERENCES med.users(user_id),
    CONSTRAINT CK_group_permissions_dates CHECK (effective_to IS NULL OR effective_to > effective_from),
    CONSTRAINT CK_group_permissions_scope_json CHECK (scope_rule_json IS NULL OR ISJSON(scope_rule_json) = 1)
);
GO

CREATE INDEX IX_group_permissions_lookup ON med.group_permissions(group_id, permission_id, effective_from, effective_to);
GO

CREATE INDEX IX_group_permissions_department ON med.group_permissions(department_id);
GO
CREATE INDEX IX_group_permissions_permission ON med.group_permissions(permission_id);
GO

CREATE UNIQUE INDEX UX_group_permissions_active_with_dept
  ON med.group_permissions(group_id, permission_id, department_scope_type, department_id)
  WHERE effective_to IS NULL AND department_id IS NOT NULL;
GO
CREATE UNIQUE INDEX UX_group_permissions_active_no_dept
  ON med.group_permissions(group_id, permission_id, department_scope_type)
  WHERE effective_to IS NULL AND department_id IS NULL;
GO

CREATE TABLE med.user_permission_overrides (
    user_permission_override_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_user_permission_overrides_id DEFAULT NEWSEQUENTIALID(),
    user_id UNIQUEIDENTIFIER NOT NULL,
    permission_id UNIQUEIDENTIFIER NOT NULL,
    effect_code NVARCHAR(10) NOT NULL,
    department_scope_type NVARCHAR(30) NOT NULL CONSTRAINT DF_user_permission_overrides_scope DEFAULT N'global',
    department_id UNIQUEIDENTIFIER NULL,
    scope_rule_json NVARCHAR(MAX) NULL,
    priority INT NOT NULL CONSTRAINT DF_user_permission_overrides_priority DEFAULT 300,
    reason NVARCHAR(1000) NOT NULL,
    effective_from DATETIME2(3) NOT NULL CONSTRAINT DF_user_permission_overrides_effective_from DEFAULT SYSUTCDATETIME(),
    effective_to DATETIME2(3) NULL,
    created_by UNIQUEIDENTIFIER NULL,
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_user_permission_overrides_created_at DEFAULT SYSUTCDATETIME(),
    row_version ROWVERSION NOT NULL,
    CONSTRAINT PK_user_permission_overrides PRIMARY KEY (user_permission_override_id),
    CONSTRAINT FK_user_permission_overrides_user FOREIGN KEY (user_id) REFERENCES med.users(user_id),
    CONSTRAINT FK_user_permission_overrides_permission FOREIGN KEY (permission_id) REFERENCES med.permissions(permission_id),
    CONSTRAINT FK_user_permission_overrides_effect FOREIGN KEY (effect_code) REFERENCES med.lookup_permission_effects(effect_code),
    CONSTRAINT FK_user_permission_overrides_scope FOREIGN KEY (department_scope_type) REFERENCES med.lookup_department_scope_types(scope_type),
    CONSTRAINT FK_user_permission_overrides_department FOREIGN KEY (department_id) REFERENCES med.departments(department_id),
    CONSTRAINT FK_user_permission_overrides_created_by FOREIGN KEY (created_by) REFERENCES med.users(user_id),
    CONSTRAINT CK_user_permission_overrides_dates CHECK (effective_to IS NULL OR effective_to > effective_from),
    CONSTRAINT CK_user_permission_overrides_scope_json CHECK (scope_rule_json IS NULL OR ISJSON(scope_rule_json) = 1)
);
GO

CREATE INDEX IX_user_permission_overrides_lookup ON med.user_permission_overrides(user_id, permission_id, effective_from, effective_to);
GO

CREATE INDEX IX_user_permission_overrides_department ON med.user_permission_overrides(department_id);
GO
CREATE INDEX IX_user_permission_overrides_permission ON med.user_permission_overrides(permission_id);
GO

CREATE UNIQUE INDEX UX_user_permission_overrides_active_with_dept
  ON med.user_permission_overrides(user_id, permission_id, department_scope_type, department_id)
  WHERE effective_to IS NULL AND department_id IS NOT NULL;
GO
CREATE UNIQUE INDEX UX_user_permission_overrides_active_no_dept
  ON med.user_permission_overrides(user_id, permission_id, department_scope_type)
  WHERE effective_to IS NULL AND department_id IS NULL;
GO

-- Trigger lives here (after user_roles, user_group_members AND user_permission_overrides all exist).
CREATE OR ALTER TRIGGER med.TR_users_expire_security_assignments
ON med.users
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @now DATETIME2(3) = SYSUTCDATETIME();

    UPDATE ur
       SET effective_to = @now
    FROM med.user_roles ur
    JOIN inserted i ON i.user_id = ur.user_id
    LEFT JOIN deleted d ON d.user_id = i.user_id
    WHERE (i.status <> N'active' OR i.deleted_at IS NOT NULL)
      AND (d.status = N'active' OR d.deleted_at IS NULL)
      AND ur.effective_to IS NULL;

    UPDATE ugm
       SET effective_to = @now
    FROM med.user_group_members ugm
    JOIN inserted i ON i.user_id = ugm.user_id
    LEFT JOIN deleted d ON d.user_id = i.user_id
    WHERE (i.status <> N'active' OR i.deleted_at IS NOT NULL)
      AND (d.status = N'active' OR d.deleted_at IS NULL)
      AND ugm.effective_to IS NULL;

    UPDATE upo
       SET effective_to = @now
    FROM med.user_permission_overrides upo
    JOIN inserted i ON i.user_id = upo.user_id
    LEFT JOIN deleted d ON d.user_id = i.user_id
    WHERE (i.status <> N'active' OR i.deleted_at IS NOT NULL)
      AND (d.status = N'active' OR d.deleted_at IS NULL)
      AND upo.effective_to IS NULL;
END;
GO

/* ============================================================
   06. IMMUTABLE AUDIT LOG
   ============================================================ */

CREATE TABLE med.audit_logs (
    audit_log_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_audit_logs_id DEFAULT NEWSEQUENTIALID(),
    audit_log_seq BIGINT IDENTITY(1,1) NOT NULL,
    correlation_id UNIQUEIDENTIFIER NOT NULL,
    actor_user_id UNIQUEIDENTIFIER NULL,
    actor_username NVARCHAR(100) NULL,
    action_code NVARCHAR(100) NOT NULL,
    target_type NVARCHAR(100) NOT NULL,
    target_id NVARCHAR(100) NULL,
    department_id UNIQUEIDENTIFIER NULL,
    before_json NVARCHAR(MAX) NULL,
    after_json NVARCHAR(MAX) NULL,
    metadata_json NVARCHAR(MAX) NULL,
    ip_address NVARCHAR(64) NULL,
    user_agent NVARCHAR(1000) NULL,
    occurred_at DATETIME2(3) NOT NULL CONSTRAINT DF_audit_logs_occurred_at DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_audit_logs PRIMARY KEY NONCLUSTERED (audit_log_id),
    CONSTRAINT FK_audit_logs_actor FOREIGN KEY (actor_user_id) REFERENCES med.users(user_id),
    CONSTRAINT FK_audit_logs_department FOREIGN KEY (department_id) REFERENCES med.departments(department_id),
    CONSTRAINT CK_audit_logs_before_json CHECK (before_json IS NULL OR ISJSON(before_json) = 1),
    CONSTRAINT CK_audit_logs_after_json CHECK (after_json IS NULL OR ISJSON(after_json) = 1),
    CONSTRAINT CK_audit_logs_metadata_json CHECK (metadata_json IS NULL OR ISJSON(metadata_json) = 1),
    CONSTRAINT CK_audit_logs_occurred_at CHECK (occurred_at <= DATEADD(MINUTE, 5, SYSUTCDATETIME()))
);
GO

CREATE UNIQUE CLUSTERED INDEX CX_audit_logs_seq ON med.audit_logs(audit_log_seq);
GO

CREATE INDEX IX_audit_logs_target ON med.audit_logs(target_type, target_id, occurred_at DESC);
GO
CREATE INDEX IX_audit_logs_actor ON med.audit_logs(actor_user_id, occurred_at DESC);
GO
CREATE INDEX IX_audit_logs_correlation ON med.audit_logs(correlation_id);
GO

CREATE INDEX IX_audit_logs_target_time_cover
ON med.audit_logs(target_type, occurred_at DESC)
INCLUDE (target_id, actor_user_id, action_code, department_id);
GO

CREATE TRIGGER med.TR_audit_logs_immutable
ON med.audit_logs
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51000, 'audit_logs is immutable. UPDATE/DELETE is not allowed.', 1;
END;
GO

CREATE INDEX IX_audit_logs_department ON med.audit_logs(department_id);
GO

/*
   Audit log integrity considerations (future work):
   - Hash chain: add prev_row_hash + row_hash columns and a trigger that computes
     row_hash = HASHBYTES('SHA2_256', concat(prev_row_hash, normalized_row_payload)).
   - SQL Server 2022+ Ledger Tables provide cryptographic tamper evidence out of the box.
   For high-volume deployments, place audit_logs on a dedicated FILEGROUP and partition by audit_log_seq.
*/

/* ============================================================
   07. PERMISSION CHANGE WORKFLOW
   ============================================================ */

CREATE TABLE med.permission_change_requests (
    permission_change_request_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_permission_change_requests_id DEFAULT NEWSEQUENTIALID(),
    change_status NVARCHAR(30) NOT NULL CONSTRAINT DF_permission_change_requests_status DEFAULT N'draft',
    target_type NVARCHAR(20) NOT NULL,
    target_role_id UNIQUEIDENTIFIER NULL,
    target_group_id UNIQUEIDENTIFIER NULL,
    target_user_id UNIQUEIDENTIFIER NULL,
    reason NVARCHAR(1000) NOT NULL,
    requested_by UNIQUEIDENTIFIER NOT NULL,
    approved_by UNIQUEIDENTIFIER NULL,
    applied_by UNIQUEIDENTIFIER NULL,
    requested_at DATETIME2(3) NOT NULL CONSTRAINT DF_permission_change_requests_requested_at DEFAULT SYSUTCDATETIME(),
    approved_at DATETIME2(3) NULL,
    effective_at DATETIME2(3) NOT NULL,
    applied_at DATETIME2(3) NULL,
    error_message NVARCHAR(2000) NULL,
    row_version ROWVERSION NOT NULL,
    CONSTRAINT PK_permission_change_requests PRIMARY KEY (permission_change_request_id),
    CONSTRAINT FK_permission_change_requests_target_role FOREIGN KEY (target_role_id) REFERENCES med.roles(role_id),
    CONSTRAINT FK_permission_change_requests_target_group FOREIGN KEY (target_group_id) REFERENCES med.groups(group_id),
    CONSTRAINT FK_permission_change_requests_target_user FOREIGN KEY (target_user_id) REFERENCES med.users(user_id),
    CONSTRAINT FK_permission_change_requests_requested_by FOREIGN KEY (requested_by) REFERENCES med.users(user_id),
    CONSTRAINT FK_permission_change_requests_approved_by FOREIGN KEY (approved_by) REFERENCES med.users(user_id),
    CONSTRAINT FK_permission_change_requests_applied_by FOREIGN KEY (applied_by) REFERENCES med.users(user_id),
    CONSTRAINT FK_permission_change_requests_status FOREIGN KEY (change_status) REFERENCES med.lookup_permission_change_statuses(change_status),
    CONSTRAINT FK_permission_change_requests_target_type FOREIGN KEY (target_type) REFERENCES med.lookup_permission_change_target_types(target_type),
    CONSTRAINT CK_permission_change_requests_target_exactly_one CHECK (
        (target_type = N'role' AND target_role_id IS NOT NULL AND target_group_id IS NULL AND target_user_id IS NULL) OR
        (target_type = N'group' AND target_role_id IS NULL AND target_group_id IS NOT NULL AND target_user_id IS NULL) OR
        (target_type = N'user' AND target_role_id IS NULL AND target_group_id IS NULL AND target_user_id IS NOT NULL)
    ),
    CONSTRAINT CK_permission_change_requests_effective_date CHECK (effective_at >= requested_at),
    CONSTRAINT CK_pcr_applied CHECK ((change_status = N'applied' AND applied_at IS NOT NULL) OR (change_status <> N'applied' AND applied_at IS NULL)),
    CONSTRAINT CK_pcr_approved_dates CHECK (approved_at IS NULL OR approved_at >= requested_at)
);
GO

CREATE INDEX IX_permission_change_requests_due ON med.permission_change_requests(change_status, effective_at);
GO

CREATE INDEX IX_permission_change_requests_target_role ON med.permission_change_requests(target_role_id);
GO
CREATE INDEX IX_permission_change_requests_target_group ON med.permission_change_requests(target_group_id);
GO
CREATE INDEX IX_permission_change_requests_target_user ON med.permission_change_requests(target_user_id);
GO
CREATE INDEX IX_permission_change_requests_requested_by ON med.permission_change_requests(requested_by);
GO
CREATE INDEX IX_permission_change_requests_approved_by ON med.permission_change_requests(approved_by);
GO
CREATE INDEX IX_permission_change_requests_applied_by ON med.permission_change_requests(applied_by);
GO

CREATE TABLE med.permission_change_items (
    permission_change_item_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_permission_change_items_id DEFAULT NEWSEQUENTIALID(),
    permission_change_request_id UNIQUEIDENTIFIER NOT NULL,
    permission_id UNIQUEIDENTIFIER NOT NULL,
    operation_code NVARCHAR(20) NOT NULL,
    effect_code NVARCHAR(10) NOT NULL,
    department_scope_type NVARCHAR(30) NOT NULL,
    department_id UNIQUEIDENTIFIER NULL,
    scope_rule_json NVARCHAR(MAX) NULL,
    before_json NVARCHAR(MAX) NULL,
    after_json NVARCHAR(MAX) NULL,
    effective_from DATETIME2(3) NULL,
    effective_to DATETIME2(3) NULL,
    CONSTRAINT PK_permission_change_items PRIMARY KEY (permission_change_item_id),
    CONSTRAINT FK_permission_change_items_request FOREIGN KEY (permission_change_request_id) REFERENCES med.permission_change_requests(permission_change_request_id),
    CONSTRAINT FK_permission_change_items_permission FOREIGN KEY (permission_id) REFERENCES med.permissions(permission_id),
    CONSTRAINT FK_permission_change_items_operation FOREIGN KEY (operation_code) REFERENCES med.lookup_permission_change_operations(operation_code),
    CONSTRAINT FK_permission_change_items_effect FOREIGN KEY (effect_code) REFERENCES med.lookup_permission_effects(effect_code),
    CONSTRAINT FK_permission_change_items_scope FOREIGN KEY (department_scope_type) REFERENCES med.lookup_department_scope_types(scope_type),
    CONSTRAINT FK_permission_change_items_department FOREIGN KEY (department_id) REFERENCES med.departments(department_id),
    CONSTRAINT CK_permission_change_items_dates CHECK (effective_to IS NULL OR effective_from IS NULL OR effective_to > effective_from),
    CONSTRAINT CK_permission_change_items_scope_json CHECK (scope_rule_json IS NULL OR ISJSON(scope_rule_json) = 1),
    CONSTRAINT CK_permission_change_items_before_json CHECK (before_json IS NULL OR ISJSON(before_json) = 1),
    CONSTRAINT CK_permission_change_items_after_json CHECK (after_json IS NULL OR ISJSON(after_json) = 1)
);
GO

CREATE INDEX IX_permission_change_items_request ON med.permission_change_items(permission_change_request_id);
GO
CREATE INDEX IX_permission_change_items_permission ON med.permission_change_items(permission_id);
GO
CREATE INDEX IX_permission_change_items_department ON med.permission_change_items(department_id);
GO

CREATE UNIQUE INDEX UX_permission_change_items_with_dept
  ON med.permission_change_items(permission_change_request_id, permission_id, operation_code, department_scope_type, department_id)
  WHERE department_id IS NOT NULL;
GO
CREATE UNIQUE INDEX UX_permission_change_items_no_dept
  ON med.permission_change_items(permission_change_request_id, permission_id, operation_code, department_scope_type)
  WHERE department_id IS NULL;
GO

CREATE OR ALTER PROCEDURE med.sp_apply_due_permission_changes
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @due TABLE (
        permission_change_request_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY
    );

    INSERT INTO @due (permission_change_request_id)
    SELECT permission_change_request_id
    FROM med.permission_change_requests WITH (UPDLOCK, READPAST)
    WHERE change_status = N'scheduled'
      AND effective_at <= SYSUTCDATETIME();

    DECLARE @req_id UNIQUEIDENTIFIER;

    DECLARE due_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT permission_change_request_id FROM @due;

    OPEN due_cursor;
    FETCH NEXT FROM due_cursor INTO @req_id;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            BEGIN TRANSACTION;

            DECLARE @now DATETIME2(3) = SYSUTCDATETIME();

            UPDATE rp
               SET effective_to = @now
            FROM med.role_permissions rp
            JOIN med.permission_change_requests d ON d.target_type = N'role' AND d.target_role_id = rp.role_id
            JOIN med.permission_change_items i ON i.permission_change_request_id = d.permission_change_request_id
            WHERE d.permission_change_request_id = @req_id
              AND i.operation_code IN (N'revoke', N'update')
              AND rp.permission_id = i.permission_id
              AND rp.effective_to IS NULL;

            UPDATE gp
               SET effective_to = @now
            FROM med.group_permissions gp
            JOIN med.permission_change_requests d ON d.target_type = N'group' AND d.target_group_id = gp.group_id
            JOIN med.permission_change_items i ON i.permission_change_request_id = d.permission_change_request_id
            WHERE d.permission_change_request_id = @req_id
              AND i.operation_code IN (N'revoke', N'update')
              AND gp.permission_id = i.permission_id
              AND gp.effective_to IS NULL;

            UPDATE upo
               SET effective_to = @now
            FROM med.user_permission_overrides upo
            JOIN med.permission_change_requests d ON d.target_type = N'user' AND d.target_user_id = upo.user_id
            JOIN med.permission_change_items i ON i.permission_change_request_id = d.permission_change_request_id
            WHERE d.permission_change_request_id = @req_id
              AND i.operation_code IN (N'revoke', N'update')
              AND upo.permission_id = i.permission_id
              AND upo.effective_to IS NULL;

            INSERT INTO med.role_permissions (
                role_id, permission_id, effect_code, department_scope_type, department_id,
                scope_rule_json, priority, reason, effective_from, effective_to, created_by
            )
            SELECT
                d.target_role_id, i.permission_id, i.effect_code, i.department_scope_type, i.department_id,
                i.scope_rule_json, 100, d.reason, COALESCE(i.effective_from, d.effective_at), i.effective_to, d.applied_by
            FROM med.permission_change_requests d
            JOIN med.permission_change_items i ON i.permission_change_request_id = d.permission_change_request_id
            WHERE d.permission_change_request_id = @req_id
              AND d.target_type = N'role'
              AND i.operation_code IN (N'grant', N'update');

            INSERT INTO med.group_permissions (
                group_id, permission_id, effect_code, department_scope_type, department_id,
                scope_rule_json, priority, reason, effective_from, effective_to, created_by
            )
            SELECT
                d.target_group_id, i.permission_id, i.effect_code, i.department_scope_type, i.department_id,
                i.scope_rule_json, 200, d.reason, COALESCE(i.effective_from, d.effective_at), i.effective_to, d.applied_by
            FROM med.permission_change_requests d
            JOIN med.permission_change_items i ON i.permission_change_request_id = d.permission_change_request_id
            WHERE d.permission_change_request_id = @req_id
              AND d.target_type = N'group'
              AND i.operation_code IN (N'grant', N'update');

            INSERT INTO med.user_permission_overrides (
                user_id, permission_id, effect_code, department_scope_type, department_id,
                scope_rule_json, priority, reason, effective_from, effective_to, created_by
            )
            SELECT
                d.target_user_id, i.permission_id, i.effect_code, i.department_scope_type, i.department_id,
                i.scope_rule_json, 300, d.reason, COALESCE(i.effective_from, d.effective_at), i.effective_to, d.applied_by
            FROM med.permission_change_requests d
            JOIN med.permission_change_items i ON i.permission_change_request_id = d.permission_change_request_id
            WHERE d.permission_change_request_id = @req_id
              AND d.target_type = N'user'
              AND i.operation_code IN (N'grant', N'update');

            UPDATE med.permission_change_requests
               SET change_status = N'applied', applied_at = @now, error_message = NULL
            WHERE permission_change_request_id = @req_id;

            COMMIT TRANSACTION;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

            UPDATE med.permission_change_requests
               SET change_status = N'failed',
                   error_message = CONCAT(ERROR_NUMBER(), N': ', ERROR_MESSAGE())
            WHERE permission_change_request_id = @req_id;
        END CATCH;

        FETCH NEXT FROM due_cursor INTO @req_id;
    END;

    CLOSE due_cursor;
    DEALLOCATE due_cursor;
END;
GO

/* ============================================================
   08. PROCEDURE VERSIONING
   ============================================================ */

CREATE TABLE med.professional_procedures (
    procedure_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_professional_procedures_id DEFAULT NEWSEQUENTIALID(),
    procedure_code NVARCHAR(100) NOT NULL,
    name NVARCHAR(500) NOT NULL,
    procedure_type NVARCHAR(50) NOT NULL,
    owner_department_id UNIQUEIDENTIFIER NULL,
    description NVARCHAR(MAX) NULL,
    status NVARCHAR(30) NOT NULL CONSTRAINT DF_professional_procedures_status DEFAULT N'active',
    created_by UNIQUEIDENTIFIER NULL,
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_professional_procedures_created_at DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2(3) NOT NULL CONSTRAINT DF_professional_procedures_updated_at DEFAULT SYSUTCDATETIME(),
    row_version ROWVERSION NOT NULL,
    CONSTRAINT PK_professional_procedures PRIMARY KEY (procedure_id),
    CONSTRAINT UQ_professional_procedures_code UNIQUE (procedure_code),
    CONSTRAINT FK_professional_procedures_department FOREIGN KEY (owner_department_id) REFERENCES med.departments(department_id),
    CONSTRAINT FK_professional_procedures_created_by FOREIGN KEY (created_by) REFERENCES med.users(user_id),
    CONSTRAINT FK_professional_procedures_status FOREIGN KEY (status) REFERENCES med.lookup_record_status(code),
    CONSTRAINT FK_professional_procedures_type FOREIGN KEY (procedure_type) REFERENCES med.lookup_procedure_types(procedure_type)
);
GO

CREATE TABLE med.procedure_versions (
    procedure_version_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_procedure_versions_id DEFAULT NEWSEQUENTIALID(),
    procedure_id UNIQUEIDENTIFIER NOT NULL,
    version_no INT NOT NULL,
    version_label NVARCHAR(50) NULL,
    status_code NVARCHAR(30) NOT NULL CONSTRAINT DF_procedure_versions_status DEFAULT N'draft',
    department_id UNIQUEIDENTIFIER NULL,
    title NVARCHAR(500) NOT NULL,
    summary NVARCHAR(MAX) NULL,
    change_reason NVARCHAR(1000) NULL,
    effective_from DATETIME2(3) NULL,
    effective_to DATETIME2(3) NULL,
    created_by UNIQUEIDENTIFIER NULL,
    submitted_by UNIQUEIDENTIFIER NULL,
    approved_by UNIQUEIDENTIFIER NULL,
    published_by UNIQUEIDENTIFIER NULL,
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_procedure_versions_created_at DEFAULT SYSUTCDATETIME(),
    submitted_at DATETIME2(3) NULL,
    approved_at DATETIME2(3) NULL,
    published_at DATETIME2(3) NULL,
    row_version ROWVERSION NOT NULL,
    CONSTRAINT PK_procedure_versions PRIMARY KEY (procedure_version_id),
    CONSTRAINT FK_procedure_versions_procedure FOREIGN KEY (procedure_id) REFERENCES med.professional_procedures(procedure_id),
    CONSTRAINT FK_procedure_versions_status FOREIGN KEY (status_code) REFERENCES med.lookup_version_statuses(status_code),
    CONSTRAINT FK_procedure_versions_department FOREIGN KEY (department_id) REFERENCES med.departments(department_id),
    CONSTRAINT FK_procedure_versions_created_by FOREIGN KEY (created_by) REFERENCES med.users(user_id),
    CONSTRAINT FK_procedure_versions_submitted_by FOREIGN KEY (submitted_by) REFERENCES med.users(user_id),
    CONSTRAINT FK_procedure_versions_approved_by FOREIGN KEY (approved_by) REFERENCES med.users(user_id),
    CONSTRAINT FK_procedure_versions_published_by FOREIGN KEY (published_by) REFERENCES med.users(user_id),
    CONSTRAINT CK_procedure_versions_version_no CHECK (version_no > 0),
    CONSTRAINT CK_procedure_versions_dates CHECK (effective_to IS NULL OR effective_from IS NULL OR effective_to > effective_from)
);
GO

-- Computed-column unique key replaces the nullable-filtered split for version uniqueness.
ALTER TABLE med.procedure_versions ADD department_id_key AS ISNULL(department_id, '00000000-0000-0000-0000-000000000000') PERSISTED;
GO

CREATE UNIQUE INDEX UX_procedure_versions_no
    ON med.procedure_versions(procedure_id, department_id_key, version_no);
GO

CREATE UNIQUE INDEX UX_procedure_versions_one_active_global
ON med.procedure_versions(procedure_id)
WHERE status_code = N'active' AND department_id IS NULL AND effective_to IS NULL;
GO

CREATE UNIQUE INDEX UX_procedure_versions_one_active_department
ON med.procedure_versions(procedure_id, department_id)
WHERE status_code = N'active' AND department_id IS NOT NULL AND effective_to IS NULL;
GO

CREATE INDEX IX_procedure_versions_resolver
ON med.procedure_versions(procedure_id, department_id, status_code, effective_from, effective_to);
GO

CREATE INDEX IX_procedure_versions_created_by ON med.procedure_versions(created_by);
GO

CREATE INDEX IX_procedure_versions_submitted_by ON med.procedure_versions(submitted_by);
GO

CREATE INDEX IX_procedure_versions_approved_by ON med.procedure_versions(approved_by);
GO

CREATE INDEX IX_procedure_versions_published_by ON med.procedure_versions(published_by);
GO

CREATE INDEX IX_procedure_versions_department ON med.procedure_versions(department_id);
GO

CREATE TABLE med.procedure_steps (
    procedure_step_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_procedure_steps_id DEFAULT NEWSEQUENTIALID(),
    procedure_version_id UNIQUEIDENTIFIER NOT NULL,
    step_no INT NOT NULL,
    step_code NVARCHAR(100) NULL,
    name NVARCHAR(500) NOT NULL,
    description NVARCHAR(MAX) NULL,
    actor_role_id UNIQUEIDENTIFIER NULL,
    transition_condition_json NVARCHAR(MAX) NULL,
    standard_duration_minutes INT NULL,
    is_required BIT NOT NULL CONSTRAINT DF_procedure_steps_is_required DEFAULT 1,
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_procedure_steps_created_at DEFAULT SYSUTCDATETIME(),
    row_version ROWVERSION NOT NULL,
    CONSTRAINT PK_procedure_steps PRIMARY KEY (procedure_step_id),
    CONSTRAINT UQ_procedure_steps_no UNIQUE (procedure_version_id, step_no),
    CONSTRAINT FK_procedure_steps_version FOREIGN KEY (procedure_version_id) REFERENCES med.procedure_versions(procedure_version_id),
    CONSTRAINT FK_procedure_steps_actor_role FOREIGN KEY (actor_role_id) REFERENCES med.roles(role_id),
    CONSTRAINT CK_procedure_steps_step_no CHECK (step_no > 0),
    CONSTRAINT CK_procedure_steps_duration CHECK (standard_duration_minutes IS NULL OR standard_duration_minutes >= 0),
    CONSTRAINT CK_procedure_steps_transition_json CHECK (transition_condition_json IS NULL OR ISJSON(transition_condition_json) = 1)
);
GO

CREATE UNIQUE INDEX UX_procedure_steps_code_not_null
ON med.procedure_steps(procedure_version_id, step_code)
WHERE step_code IS NOT NULL;
GO

CREATE INDEX IX_procedure_steps_actor_role ON med.procedure_steps(actor_role_id);
GO

CREATE TABLE med.procedure_attachments (
    procedure_attachment_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_procedure_attachments_id DEFAULT NEWSEQUENTIALID(),
    procedure_version_id UNIQUEIDENTIFIER NOT NULL,
    attachment_type NVARCHAR(50) NOT NULL CONSTRAINT DF_procedure_attachments_type DEFAULT N'sop',
    file_name NVARCHAR(500) NOT NULL,
    file_uri NVARCHAR(2000) NOT NULL,
    mime_type NVARCHAR(255) NULL,
    file_size_bytes BIGINT NULL,
    checksum_sha256 NVARCHAR(128) NULL,
    uploaded_by UNIQUEIDENTIFIER NULL,
    uploaded_at DATETIME2(3) NOT NULL CONSTRAINT DF_procedure_attachments_uploaded_at DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_procedure_attachments PRIMARY KEY (procedure_attachment_id),
    CONSTRAINT FK_procedure_attachments_version FOREIGN KEY (procedure_version_id) REFERENCES med.procedure_versions(procedure_version_id),
    CONSTRAINT FK_procedure_attachments_uploaded_by FOREIGN KEY (uploaded_by) REFERENCES med.users(user_id),
    CONSTRAINT FK_procedure_attachments_type FOREIGN KEY (attachment_type) REFERENCES med.lookup_attachment_types(attachment_type),
    CONSTRAINT CK_procedure_attachments_size CHECK (file_size_bytes IS NULL OR file_size_bytes >= 0)
);
GO

CREATE INDEX IX_procedure_attachments_version ON med.procedure_attachments(procedure_version_id);
GO

CREATE INDEX IX_procedure_attachments_uploaded_by ON med.procedure_attachments(uploaded_by);
GO

CREATE TABLE med.procedure_screen_mappings (
    procedure_screen_mapping_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_procedure_screen_mappings_id DEFAULT NEWSEQUENTIALID(),
    procedure_version_id UNIQUEIDENTIFIER NOT NULL,
    screen_id UNIQUEIDENTIFIER NOT NULL,
    feature_id UNIQUEIDENTIFIER NULL,
    action_code NVARCHAR(30) NULL,
    enforcement_mode NVARCHAR(20) NOT NULL CONSTRAINT DF_procedure_screen_mappings_mode DEFAULT N'warning',
    rule_json NVARCHAR(MAX) NULL,
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_procedure_screen_mappings_created_at DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_procedure_screen_mappings PRIMARY KEY (procedure_screen_mapping_id),
    CONSTRAINT FK_procedure_screen_mappings_version FOREIGN KEY (procedure_version_id) REFERENCES med.procedure_versions(procedure_version_id),
    CONSTRAINT FK_procedure_screen_mappings_screen FOREIGN KEY (screen_id) REFERENCES med.screen_catalog(screen_id),
    CONSTRAINT FK_procedure_screen_mappings_feature FOREIGN KEY (feature_id) REFERENCES med.feature_catalog(feature_id),
    CONSTRAINT FK_procedure_screen_mappings_action FOREIGN KEY (action_code) REFERENCES med.lookup_action_codes(action_code),
    CONSTRAINT FK_procedure_screen_mappings_mode FOREIGN KEY (enforcement_mode) REFERENCES med.lookup_enforcement_modes(enforcement_mode),
    CONSTRAINT CK_procedure_screen_mappings_rule_json CHECK (rule_json IS NULL OR ISJSON(rule_json) = 1)
);
GO

-- Computed-column unique key replaces the four nullable-filtered uniques.
ALTER TABLE med.procedure_screen_mappings ADD
    feature_id_key AS ISNULL(feature_id, '00000000-0000-0000-0000-000000000000') PERSISTED,
    action_code_key AS ISNULL(action_code, N'') PERSISTED;
GO

CREATE UNIQUE INDEX UX_procedure_screen_mappings_unique
    ON med.procedure_screen_mappings(procedure_version_id, screen_id, feature_id_key, action_code_key);
GO

CREATE INDEX IX_procedure_screen_mappings_lookup
ON med.procedure_screen_mappings(screen_id, feature_id, action_code, enforcement_mode);
GO

/* ============================================================
   09. PATIENT / ENCOUNTER REFERENCES
   ============================================================ */

CREATE TABLE med.patient_refs (
    patient_ref_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_patient_refs_id DEFAULT NEWSEQUENTIALID(),
    external_patient_id NVARCHAR(100) NOT NULL,
    source_system_code NVARCHAR(50) NOT NULL CONSTRAINT DF_patient_refs_source DEFAULT N'default',
    patient_code NVARCHAR(100) NULL,
    display_name NVARCHAR(255) NULL,
    birth_date DATE NULL,
    gender_code NVARCHAR(20) NULL,
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_patient_refs_created_at DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_patient_refs PRIMARY KEY (patient_ref_id),
    CONSTRAINT UQ_patient_refs_external UNIQUE (source_system_code, external_patient_id),
    CONSTRAINT FK_patient_refs_gender FOREIGN KEY (gender_code) REFERENCES med.lookup_genders(gender_code)
);
GO

CREATE TABLE med.encounter_refs (
    encounter_ref_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_encounter_refs_id DEFAULT NEWSEQUENTIALID(),
    patient_ref_id UNIQUEIDENTIFIER NOT NULL,
    external_encounter_id NVARCHAR(100) NOT NULL,
    source_system_code NVARCHAR(50) NOT NULL CONSTRAINT DF_encounter_refs_source DEFAULT N'default',
    encounter_type NVARCHAR(50) NULL,
    department_id UNIQUEIDENTIFIER NULL,
    started_at DATETIME2(3) NULL,
    ended_at DATETIME2(3) NULL,
    CONSTRAINT PK_encounter_refs PRIMARY KEY (encounter_ref_id),
    CONSTRAINT UQ_encounter_refs_external UNIQUE (source_system_code, external_encounter_id),
    CONSTRAINT FK_encounter_refs_patient FOREIGN KEY (patient_ref_id) REFERENCES med.patient_refs(patient_ref_id),
    CONSTRAINT FK_encounter_refs_department FOREIGN KEY (department_id) REFERENCES med.departments(department_id),
    CONSTRAINT CK_encounter_refs_dates CHECK (ended_at IS NULL OR started_at IS NULL OR ended_at > started_at)
);
GO

CREATE INDEX IX_encounter_refs_department ON med.encounter_refs(department_id);
GO

/* ============================================================
   10. TECHNICAL CATALOG AND RESOURCE NORMS
   ============================================================ */

CREATE TABLE med.technical_services (
    technical_service_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_technical_services_id DEFAULT NEWSEQUENTIALID(),
    service_code NVARCHAR(100) NOT NULL,
    name NVARCHAR(500) NOT NULL,
    service_type NVARCHAR(50) NOT NULL,
    department_id UNIQUEIDENTIFIER NULL,
    linked_procedure_id UNIQUEIDENTIFIER NULL,
    description NVARCHAR(MAX) NULL,
    status NVARCHAR(30) NOT NULL CONSTRAINT DF_technical_services_status DEFAULT N'active',
    created_by UNIQUEIDENTIFIER NULL,
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_technical_services_created_at DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2(3) NOT NULL CONSTRAINT DF_technical_services_updated_at DEFAULT SYSUTCDATETIME(),
    row_version ROWVERSION NOT NULL,
    CONSTRAINT PK_technical_services PRIMARY KEY (technical_service_id),
    CONSTRAINT UQ_technical_services_code UNIQUE (service_code),
    CONSTRAINT FK_technical_services_department FOREIGN KEY (department_id) REFERENCES med.departments(department_id),
    CONSTRAINT FK_technical_services_procedure FOREIGN KEY (linked_procedure_id) REFERENCES med.professional_procedures(procedure_id),
    CONSTRAINT FK_technical_services_created_by FOREIGN KEY (created_by) REFERENCES med.users(user_id),
    CONSTRAINT FK_technical_services_status FOREIGN KEY (status) REFERENCES med.lookup_record_status(code),
    CONSTRAINT FK_technical_services_type FOREIGN KEY (service_type) REFERENCES med.lookup_service_types(service_type)
);
GO

CREATE INDEX IX_technical_services_department ON med.technical_services(department_id);
GO

CREATE INDEX IX_technical_services_linked_procedure ON med.technical_services(linked_procedure_id);
GO

CREATE INDEX IX_technical_services_created_by ON med.technical_services(created_by);
GO

CREATE TABLE med.resource_catalog (
    resource_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_resource_catalog_id DEFAULT NEWSEQUENTIALID(),
    resource_type NVARCHAR(30) NOT NULL,
    resource_code NVARCHAR(100) NOT NULL,
    name NVARCHAR(500) NOT NULL,
    default_unit_code NVARCHAR(50) NULL,
    external_system_code NVARCHAR(100) NULL,
    external_resource_id NVARCHAR(255) NULL,
    status NVARCHAR(30) NOT NULL CONSTRAINT DF_resource_catalog_status DEFAULT N'active',
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_resource_catalog_created_at DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2(3) NOT NULL CONSTRAINT DF_resource_catalog_updated_at DEFAULT SYSUTCDATETIME(),
    row_version ROWVERSION NOT NULL,
    CONSTRAINT PK_resource_catalog PRIMARY KEY (resource_id),
    CONSTRAINT UQ_resource_catalog_code UNIQUE (resource_type, resource_code),
    CONSTRAINT FK_resource_catalog_type FOREIGN KEY (resource_type) REFERENCES med.lookup_resource_types(resource_type),
    CONSTRAINT FK_resource_catalog_unit FOREIGN KEY (default_unit_code) REFERENCES med.unit_catalog(unit_code),
    CONSTRAINT FK_resource_catalog_status FOREIGN KEY (status) REFERENCES med.lookup_record_status(code)
);
GO

CREATE UNIQUE INDEX UX_resource_catalog_external
  ON med.resource_catalog(external_system_code, external_resource_id)
  WHERE external_system_code IS NOT NULL AND external_resource_id IS NOT NULL;
GO

CREATE TABLE med.technical_resource_norms (
    technical_resource_norm_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_technical_resource_norms_id DEFAULT NEWSEQUENTIALID(),
    technical_service_id UNIQUEIDENTIFIER NOT NULL,
    resource_id UNIQUEIDENTIFIER NOT NULL,
    standard_quantity DECIMAL(18,4) NOT NULL,
    unit_code NVARCHAR(50) NOT NULL,
    is_required BIT NOT NULL CONSTRAINT DF_technical_resource_norms_is_required DEFAULT 1,
    note NVARCHAR(1000) NULL,
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_technical_resource_norms_created_at DEFAULT SYSUTCDATETIME(),
    row_version ROWVERSION NOT NULL,
    CONSTRAINT PK_technical_resource_norms PRIMARY KEY (technical_resource_norm_id),
    CONSTRAINT UQ_technical_resource_norms_resource UNIQUE (technical_service_id, resource_id),
    CONSTRAINT FK_technical_resource_norms_service FOREIGN KEY (technical_service_id) REFERENCES med.technical_services(technical_service_id),
    CONSTRAINT FK_technical_resource_norms_resource FOREIGN KEY (resource_id) REFERENCES med.resource_catalog(resource_id),
    CONSTRAINT FK_technical_resource_norms_unit FOREIGN KEY (unit_code) REFERENCES med.unit_catalog(unit_code),
    CONSTRAINT CK_technical_resource_norms_qty CHECK (standard_quantity >= 0)
);
GO

CREATE INDEX IX_technical_resource_norms_resource ON med.technical_resource_norms(resource_id);
GO

CREATE INDEX IX_technical_resource_norms_unit ON med.technical_resource_norms(unit_code);
GO

-- Versioned norms preserve historical consistency when a procedure version changes.
CREATE TABLE med.procedure_version_resource_norms (
    procedure_version_resource_norm_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_procedure_version_resource_norms_id DEFAULT NEWSEQUENTIALID(),
    procedure_version_id UNIQUEIDENTIFIER NOT NULL,
    resource_id UNIQUEIDENTIFIER NOT NULL,
    standard_quantity DECIMAL(18,4) NOT NULL,
    unit_code NVARCHAR(50) NOT NULL,
    is_required BIT NOT NULL CONSTRAINT DF_procedure_version_resource_norms_is_required DEFAULT 1,
    note NVARCHAR(1000) NULL,
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_procedure_version_resource_norms_created_at DEFAULT SYSUTCDATETIME(),
    row_version ROWVERSION NOT NULL,
    CONSTRAINT PK_procedure_version_resource_norms PRIMARY KEY (procedure_version_resource_norm_id),
    CONSTRAINT UQ_procedure_version_resource_norms_resource UNIQUE (procedure_version_id, resource_id),
    CONSTRAINT FK_procedure_version_resource_norms_version FOREIGN KEY (procedure_version_id) REFERENCES med.procedure_versions(procedure_version_id),
    CONSTRAINT FK_procedure_version_resource_norms_resource FOREIGN KEY (resource_id) REFERENCES med.resource_catalog(resource_id),
    CONSTRAINT FK_procedure_version_resource_norms_unit FOREIGN KEY (unit_code) REFERENCES med.unit_catalog(unit_code),
    CONSTRAINT CK_procedure_version_resource_norms_qty CHECK (standard_quantity >= 0)
);
GO

CREATE INDEX IX_procedure_version_resource_norms_resource ON med.procedure_version_resource_norms(resource_id);
GO

CREATE INDEX IX_procedure_version_resource_norms_unit ON med.procedure_version_resource_norms(unit_code);
GO

CREATE TABLE med.technical_orders (
    technical_order_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_technical_orders_id DEFAULT NEWSEQUENTIALID(),
    technical_service_id UNIQUEIDENTIFIER NOT NULL,
    procedure_version_id UNIQUEIDENTIFIER NULL,
    patient_ref_id UNIQUEIDENTIFIER NULL,
    encounter_ref_id UNIQUEIDENTIFIER NULL,
    ordering_department_id UNIQUEIDENTIFIER NULL,
    ordered_by UNIQUEIDENTIFIER NULL,
    order_status NVARCHAR(30) NOT NULL CONSTRAINT DF_technical_orders_status DEFAULT N'ordered',
    ordered_at DATETIME2(3) NOT NULL CONSTRAINT DF_technical_orders_ordered_at DEFAULT SYSUTCDATETIME(),
    completed_at DATETIME2(3) NULL,
    CONSTRAINT PK_technical_orders PRIMARY KEY (technical_order_id),
    CONSTRAINT FK_technical_orders_service FOREIGN KEY (technical_service_id) REFERENCES med.technical_services(technical_service_id),
    CONSTRAINT FK_technical_orders_procedure_version FOREIGN KEY (procedure_version_id) REFERENCES med.procedure_versions(procedure_version_id),
    CONSTRAINT FK_technical_orders_patient FOREIGN KEY (patient_ref_id) REFERENCES med.patient_refs(patient_ref_id),
    CONSTRAINT FK_technical_orders_encounter FOREIGN KEY (encounter_ref_id) REFERENCES med.encounter_refs(encounter_ref_id),
    CONSTRAINT FK_technical_orders_department FOREIGN KEY (ordering_department_id) REFERENCES med.departments(department_id),
    CONSTRAINT FK_technical_orders_ordered_by FOREIGN KEY (ordered_by) REFERENCES med.users(user_id),
    CONSTRAINT FK_technical_orders_status FOREIGN KEY (order_status) REFERENCES med.lookup_order_statuses(order_status),
    CONSTRAINT CK_technical_orders_completed CHECK (completed_at IS NULL OR completed_at >= ordered_at)
);
GO

CREATE INDEX IX_technical_orders_patient
ON med.technical_orders(patient_ref_id, encounter_ref_id, ordered_at DESC);
GO

CREATE INDEX IX_technical_orders_service_status
ON med.technical_orders(technical_service_id, order_status, ordered_at DESC);
GO

CREATE INDEX IX_technical_orders_procedure_version ON med.technical_orders(procedure_version_id);
GO

CREATE INDEX IX_technical_orders_encounter ON med.technical_orders(encounter_ref_id);
GO

CREATE INDEX IX_technical_orders_ordering_department ON med.technical_orders(ordering_department_id);
GO

CREATE INDEX IX_technical_orders_ordered_by ON med.technical_orders(ordered_by);
GO

CREATE TABLE med.resource_availability_snapshots (
    resource_availability_snapshot_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_resource_availability_snapshots_id DEFAULT NEWSEQUENTIALID(),
    technical_order_id UNIQUEIDENTIFIER NOT NULL,
    resource_id UNIQUEIDENTIFIER NOT NULL,
    required_quantity DECIMAL(18,4) NOT NULL,
    available_quantity DECIMAL(18,4) NULL,
    unit_code NVARCHAR(50) NOT NULL,
    availability_status NVARCHAR(30) NOT NULL,
    external_payload_json NVARCHAR(MAX) NULL,
    checked_at DATETIME2(3) NOT NULL CONSTRAINT DF_resource_availability_snapshots_checked_at DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_resource_availability_snapshots PRIMARY KEY (resource_availability_snapshot_id),
    CONSTRAINT FK_resource_availability_snapshots_order FOREIGN KEY (technical_order_id) REFERENCES med.technical_orders(technical_order_id),
    CONSTRAINT FK_resource_availability_snapshots_resource FOREIGN KEY (resource_id) REFERENCES med.resource_catalog(resource_id),
    CONSTRAINT FK_resource_availability_snapshots_unit FOREIGN KEY (unit_code) REFERENCES med.unit_catalog(unit_code),
    CONSTRAINT FK_resource_availability_snapshots_status FOREIGN KEY (availability_status) REFERENCES med.lookup_availability_statuses(availability_status),
    CONSTRAINT CK_resource_availability_snapshots_qty CHECK (required_quantity >= 0 AND (available_quantity IS NULL OR available_quantity >= 0)),
    CONSTRAINT CK_resource_availability_snapshots_payload CHECK (external_payload_json IS NULL OR ISJSON(external_payload_json) = 1)
);
GO

CREATE INDEX IX_resource_availability_snapshots_order ON med.resource_availability_snapshots(technical_order_id);
GO

CREATE INDEX IX_resource_availability_snapshots_resource ON med.resource_availability_snapshots(resource_id);
GO

CREATE INDEX IX_resource_availability_snapshots_unit ON med.resource_availability_snapshots(unit_code);
GO

CREATE TABLE med.actual_resource_usages (
    actual_resource_usage_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_actual_resource_usages_id DEFAULT NEWSEQUENTIALID(),
    technical_order_id UNIQUEIDENTIFIER NOT NULL,
    resource_id UNIQUEIDENTIFIER NOT NULL,
    actual_quantity DECIMAL(18,4) NOT NULL,
    unit_code NVARCHAR(50) NOT NULL,
    variance_reason NVARCHAR(1000) NULL,
    revision_no INT NOT NULL CONSTRAINT DF_actual_resource_usages_revision_no DEFAULT 1,
    is_final BIT NOT NULL CONSTRAINT DF_actual_resource_usages_is_final DEFAULT 0,
    captured_by UNIQUEIDENTIFIER NULL,
    captured_at DATETIME2(3) NOT NULL CONSTRAINT DF_actual_resource_usages_captured_at DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_actual_resource_usages PRIMARY KEY (actual_resource_usage_id),
    CONSTRAINT FK_actual_resource_usages_order FOREIGN KEY (technical_order_id) REFERENCES med.technical_orders(technical_order_id),
    CONSTRAINT FK_actual_resource_usages_resource FOREIGN KEY (resource_id) REFERENCES med.resource_catalog(resource_id),
    CONSTRAINT FK_actual_resource_usages_unit FOREIGN KEY (unit_code) REFERENCES med.unit_catalog(unit_code),
    CONSTRAINT FK_actual_resource_usages_captured_by FOREIGN KEY (captured_by) REFERENCES med.users(user_id),
    CONSTRAINT CK_actual_resource_usages_qty CHECK (actual_quantity >= 0),
    CONSTRAINT CK_actual_resource_usages_revision CHECK (revision_no > 0),
    CONSTRAINT UQ_actual_resource_usages_revision UNIQUE (technical_order_id, resource_id, revision_no)
);
GO

CREATE INDEX IX_actual_resource_usages_captured_by ON med.actual_resource_usages(captured_by);
GO

CREATE INDEX IX_actual_resource_usages_unit ON med.actual_resource_usages(unit_code);
GO

-- Non-unique filtered index. The "at most one final per (order, resource)"
-- invariant is enforced by TR_actual_resource_usages_set_final, which demotes
-- prior final rows in an AFTER trigger. A unique index here would conflict with
-- the trigger because constraints are evaluated before AFTER triggers fire.
CREATE INDEX IX_actual_resource_usages_final
ON med.actual_resource_usages(technical_order_id, resource_id)
WHERE is_final = 1;
GO

CREATE OR ALTER TRIGGER med.TR_actual_resource_usages_set_final
ON med.actual_resource_usages
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM inserted WHERE is_final = 1) RETURN;

    UPDATE u
       SET is_final = 0
    FROM med.actual_resource_usages u
    JOIN inserted i
        ON i.technical_order_id = u.technical_order_id
       AND i.resource_id = u.resource_id
    WHERE i.is_final = 1
      AND u.is_final = 1
      AND u.actual_resource_usage_id <> i.actual_resource_usage_id;
END;
GO

/* ============================================================
   11. CLINICAL PROTOCOLS
   ============================================================ */

CREATE TABLE med.clinical_protocols (
    clinical_protocol_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_clinical_protocols_id DEFAULT NEWSEQUENTIALID(),
    protocol_code NVARCHAR(100) NOT NULL,
    name NVARCHAR(500) NOT NULL,
    protocol_type NVARCHAR(50) NOT NULL,
    owner_department_id UNIQUEIDENTIFIER NULL,
    description NVARCHAR(MAX) NULL,
    status NVARCHAR(30) NOT NULL CONSTRAINT DF_clinical_protocols_status DEFAULT N'active',
    created_by UNIQUEIDENTIFIER NULL,
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_clinical_protocols_created_at DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2(3) NOT NULL CONSTRAINT DF_clinical_protocols_updated_at DEFAULT SYSUTCDATETIME(),
    row_version ROWVERSION NOT NULL,
    CONSTRAINT PK_clinical_protocols PRIMARY KEY (clinical_protocol_id),
    CONSTRAINT UQ_clinical_protocols_code UNIQUE (protocol_code),
    CONSTRAINT FK_clinical_protocols_department FOREIGN KEY (owner_department_id) REFERENCES med.departments(department_id),
    CONSTRAINT FK_clinical_protocols_created_by FOREIGN KEY (created_by) REFERENCES med.users(user_id),
    CONSTRAINT FK_clinical_protocols_status FOREIGN KEY (status) REFERENCES med.lookup_record_status(code),
    CONSTRAINT FK_clinical_protocols_type FOREIGN KEY (protocol_type) REFERENCES med.lookup_protocol_types(protocol_type)
);
GO

CREATE INDEX IX_clinical_protocols_owner_department ON med.clinical_protocols(owner_department_id);
GO
CREATE INDEX IX_clinical_protocols_created_by ON med.clinical_protocols(created_by);
GO

CREATE TABLE med.clinical_protocol_versions (
    clinical_protocol_version_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_clinical_protocol_versions_id DEFAULT NEWSEQUENTIALID(),
    clinical_protocol_id UNIQUEIDENTIFIER NOT NULL,
    version_no INT NOT NULL,
    status_code NVARCHAR(30) NOT NULL CONSTRAINT DF_clinical_protocol_versions_status DEFAULT N'draft',
    title NVARCHAR(500) NOT NULL,
    summary NVARCHAR(MAX) NULL,
    content_json NVARCHAR(MAX) NULL,
    effective_from DATETIME2(3) NULL,
    effective_to DATETIME2(3) NULL,
    created_by UNIQUEIDENTIFIER NULL,
    approved_by UNIQUEIDENTIFIER NULL,
    published_by UNIQUEIDENTIFIER NULL,
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_clinical_protocol_versions_created_at DEFAULT SYSUTCDATETIME(),
    approved_at DATETIME2(3) NULL,
    published_at DATETIME2(3) NULL,
    row_version ROWVERSION NOT NULL,
    CONSTRAINT PK_clinical_protocol_versions PRIMARY KEY (clinical_protocol_version_id),
    CONSTRAINT UQ_clinical_protocol_versions_no UNIQUE (clinical_protocol_id, version_no),
    CONSTRAINT FK_clinical_protocol_versions_protocol FOREIGN KEY (clinical_protocol_id) REFERENCES med.clinical_protocols(clinical_protocol_id),
    CONSTRAINT FK_clinical_protocol_versions_status FOREIGN KEY (status_code) REFERENCES med.lookup_version_statuses(status_code),
    CONSTRAINT FK_clinical_protocol_versions_created_by FOREIGN KEY (created_by) REFERENCES med.users(user_id),
    CONSTRAINT FK_clinical_protocol_versions_approved_by FOREIGN KEY (approved_by) REFERENCES med.users(user_id),
    CONSTRAINT FK_clinical_protocol_versions_published_by FOREIGN KEY (published_by) REFERENCES med.users(user_id),
    CONSTRAINT CK_clinical_protocol_versions_version_no CHECK (version_no > 0),
    CONSTRAINT CK_clinical_protocol_versions_dates CHECK (effective_to IS NULL OR effective_from IS NULL OR effective_to > effective_from),
    CONSTRAINT CK_clinical_protocol_versions_content_json CHECK (content_json IS NULL OR ISJSON(content_json) = 1)
);
GO

CREATE UNIQUE INDEX UX_clinical_protocol_versions_one_active
ON med.clinical_protocol_versions(clinical_protocol_id)
WHERE status_code = N'active' AND effective_to IS NULL;
GO

CREATE INDEX IX_clinical_protocol_versions_created_by ON med.clinical_protocol_versions(created_by);
GO
CREATE INDEX IX_clinical_protocol_versions_approved_by ON med.clinical_protocol_versions(approved_by);
GO
CREATE INDEX IX_clinical_protocol_versions_published_by ON med.clinical_protocol_versions(published_by);
GO

CREATE TABLE med.clinical_protocol_procedures (
    clinical_protocol_procedure_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_clinical_protocol_procedures_id DEFAULT NEWSEQUENTIALID(),
    clinical_protocol_version_id UNIQUEIDENTIFIER NOT NULL,
    procedure_version_id UNIQUEIDENTIFIER NOT NULL,
    relation_type NVARCHAR(50) NOT NULL CONSTRAINT DF_clinical_protocol_procedures_relation DEFAULT N'references',
    sequence_no INT NULL,
    note NVARCHAR(1000) NULL,
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_clinical_protocol_procedures_created_at DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_clinical_protocol_procedures PRIMARY KEY (clinical_protocol_procedure_id),
    CONSTRAINT UQ_clinical_protocol_procedures UNIQUE (clinical_protocol_version_id, procedure_version_id),
    CONSTRAINT FK_clinical_protocol_procedures_protocol_version FOREIGN KEY (clinical_protocol_version_id) REFERENCES med.clinical_protocol_versions(clinical_protocol_version_id),
    CONSTRAINT FK_clinical_protocol_procedures_procedure_version FOREIGN KEY (procedure_version_id) REFERENCES med.procedure_versions(procedure_version_id),
    CONSTRAINT FK_clinical_protocol_procedures_relation FOREIGN KEY (relation_type) REFERENCES med.lookup_protocol_relation_types(relation_type),
    CONSTRAINT CK_clinical_protocol_procedures_sequence CHECK (sequence_no IS NULL OR sequence_no > 0)
);
GO

CREATE INDEX IX_clinical_protocol_procedures_procedure_version ON med.clinical_protocol_procedures(procedure_version_id);
GO

CREATE TABLE med.protocol_applicability_rules (
    protocol_applicability_rule_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_protocol_applicability_rules_id DEFAULT NEWSEQUENTIALID(),
    clinical_protocol_version_id UNIQUEIDENTIFIER NOT NULL,
    rule_type NVARCHAR(50) NOT NULL,
    rule_json NVARCHAR(MAX) NOT NULL,
    priority INT NOT NULL CONSTRAINT DF_protocol_applicability_rules_priority DEFAULT 100,
    is_active BIT NOT NULL CONSTRAINT DF_protocol_applicability_rules_is_active DEFAULT 1,
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_protocol_applicability_rules_created_at DEFAULT SYSUTCDATETIME(),
    row_version ROWVERSION NOT NULL,
    CONSTRAINT PK_protocol_applicability_rules PRIMARY KEY (protocol_applicability_rule_id),
    CONSTRAINT FK_protocol_applicability_rules_version FOREIGN KEY (clinical_protocol_version_id) REFERENCES med.clinical_protocol_versions(clinical_protocol_version_id),
    CONSTRAINT FK_protocol_applicability_rules_type FOREIGN KEY (rule_type) REFERENCES med.lookup_protocol_rule_types(rule_type),
    CONSTRAINT CK_protocol_applicability_rules_json CHECK (ISJSON(rule_json) = 1)
);
GO

CREATE INDEX IX_protocol_applicability_rules_version ON med.protocol_applicability_rules(clinical_protocol_version_id);
GO

CREATE TABLE med.patient_protocol_applications (
    patient_protocol_application_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_patient_protocol_applications_id DEFAULT NEWSEQUENTIALID(),
    patient_ref_id UNIQUEIDENTIFIER NOT NULL,
    encounter_ref_id UNIQUEIDENTIFIER NULL,
    diagnosis_code NVARCHAR(50) NULL,
    clinical_protocol_version_id UNIQUEIDENTIFIER NOT NULL,
    application_status NVARCHAR(30) NOT NULL,
    applied_by UNIQUEIDENTIFIER NULL,
    applied_at DATETIME2(3) NULL,
    skipped_reason NVARCHAR(1000) NULL,
    decision_context_json NVARCHAR(MAX) NULL,
    CONSTRAINT PK_patient_protocol_applications PRIMARY KEY (patient_protocol_application_id),
    CONSTRAINT FK_patient_protocol_applications_patient FOREIGN KEY (patient_ref_id) REFERENCES med.patient_refs(patient_ref_id),
    CONSTRAINT FK_patient_protocol_applications_encounter FOREIGN KEY (encounter_ref_id) REFERENCES med.encounter_refs(encounter_ref_id),
    CONSTRAINT FK_patient_protocol_applications_version FOREIGN KEY (clinical_protocol_version_id) REFERENCES med.clinical_protocol_versions(clinical_protocol_version_id),
    CONSTRAINT FK_patient_protocol_applications_applied_by FOREIGN KEY (applied_by) REFERENCES med.users(user_id),
    CONSTRAINT FK_patient_protocol_applications_status FOREIGN KEY (application_status) REFERENCES med.lookup_protocol_application_statuses(application_status),
    CONSTRAINT CK_patient_protocol_applications_context_json CHECK (decision_context_json IS NULL OR ISJSON(decision_context_json) = 1)
);
GO

CREATE INDEX IX_patient_protocol_applications_patient
ON med.patient_protocol_applications(patient_ref_id, encounter_ref_id, applied_at DESC);
GO

CREATE INDEX IX_patient_protocol_applications_protocol_version
ON med.patient_protocol_applications(clinical_protocol_version_id, application_status, applied_at DESC);
GO

CREATE INDEX IX_patient_protocol_applications_applied_by ON med.patient_protocol_applications(applied_by);
GO

/* ============================================================
   12. NOTIFICATIONS
   ============================================================ */

CREATE TABLE med.notification_preferences (
    notification_preference_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_notification_preferences_id DEFAULT NEWSEQUENTIALID(),
    user_id UNIQUEIDENTIFIER NOT NULL,
    notification_type NVARCHAR(80) NOT NULL,
    channel_code NVARCHAR(30) NOT NULL,
    is_enabled BIT NOT NULL CONSTRAINT DF_notification_preferences_is_enabled DEFAULT 1,
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_notification_preferences_created_at DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2(3) NOT NULL CONSTRAINT DF_notification_preferences_updated_at DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_notification_preferences PRIMARY KEY (notification_preference_id),
    CONSTRAINT UQ_notification_preferences UNIQUE (user_id, notification_type, channel_code),
    CONSTRAINT FK_notification_preferences_user FOREIGN KEY (user_id) REFERENCES med.users(user_id),
    CONSTRAINT FK_notification_preferences_channel FOREIGN KEY (channel_code) REFERENCES med.lookup_notification_channels(channel_code)
);
GO

CREATE INDEX IX_notification_preferences_channel ON med.notification_preferences(channel_code);
GO

CREATE TABLE med.notifications (
    notification_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_notifications_id DEFAULT NEWSEQUENTIALID(),
    recipient_user_id UNIQUEIDENTIFIER NOT NULL,
    notification_type NVARCHAR(80) NOT NULL,
    title NVARCHAR(500) NOT NULL,
    body NVARCHAR(MAX) NULL,
    severity NVARCHAR(20) NOT NULL CONSTRAINT DF_notifications_severity DEFAULT N'info',
    source_type NVARCHAR(100) NULL,
    source_id NVARCHAR(100) NULL,
    payload_json NVARCHAR(MAX) NULL,
    read_at DATETIME2(3) NULL,
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_notifications_created_at DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_notifications PRIMARY KEY (notification_id),
    CONSTRAINT FK_notifications_recipient FOREIGN KEY (recipient_user_id) REFERENCES med.users(user_id),
    CONSTRAINT FK_notifications_severity FOREIGN KEY (severity) REFERENCES med.lookup_notification_severities(severity),
    CONSTRAINT CK_notifications_payload_json CHECK (payload_json IS NULL OR ISJSON(payload_json) = 1)
);
GO

CREATE INDEX IX_notifications_recipient_unread
ON med.notifications(recipient_user_id, read_at, created_at DESC);
GO

CREATE TABLE med.notification_delivery_attempts (
    notification_delivery_attempt_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_notification_delivery_attempts_id DEFAULT NEWSEQUENTIALID(),
    notification_id UNIQUEIDENTIFIER NOT NULL,
    channel_code NVARCHAR(30) NOT NULL,
    delivery_status NVARCHAR(30) NOT NULL,
    attempted_at DATETIME2(3) NOT NULL CONSTRAINT DF_notification_delivery_attempts_attempted_at DEFAULT SYSUTCDATETIME(),
    error_message NVARCHAR(2000) NULL,
    CONSTRAINT PK_notification_delivery_attempts PRIMARY KEY (notification_delivery_attempt_id),
    CONSTRAINT FK_notification_delivery_attempts_notification FOREIGN KEY (notification_id) REFERENCES med.notifications(notification_id),
    CONSTRAINT FK_notification_delivery_attempts_channel FOREIGN KEY (channel_code) REFERENCES med.lookup_notification_channels(channel_code),
    CONSTRAINT FK_notification_delivery_attempts_status FOREIGN KEY (delivery_status) REFERENCES med.lookup_delivery_statuses(delivery_status)
);
GO

CREATE INDEX IX_notification_delivery_attempts_notification ON med.notification_delivery_attempts(notification_id);
GO
CREATE INDEX IX_notification_delivery_attempts_channel ON med.notification_delivery_attempts(channel_code);
GO

/* ============================================================
   13. REPORTING VIEWS
   ============================================================ */

CREATE VIEW med.vw_effective_user_permissions_source
WITH SCHEMABINDING
AS
SELECT
    ur.user_id,
    rp.permission_id,
    rp.effect_code,
    rp.department_scope_type,
    rp.department_id,
    rp.priority,
    N'role' AS source_type,
    rp.role_permission_id AS source_id,
    rp.effective_from,
    rp.effective_to
FROM med.user_roles ur
JOIN med.role_permissions rp ON rp.role_id = ur.role_id
WHERE (ur.effective_to IS NULL OR ur.effective_to > SYSUTCDATETIME())
  AND ur.effective_from <= SYSUTCDATETIME()
  AND (rp.effective_to IS NULL OR rp.effective_to > SYSUTCDATETIME())
  AND rp.effective_from <= SYSUTCDATETIME()
UNION ALL
SELECT
    ugm.user_id,
    gp.permission_id,
    gp.effect_code,
    gp.department_scope_type,
    gp.department_id,
    gp.priority,
    N'group' AS source_type,
    gp.group_permission_id AS source_id,
    gp.effective_from,
    gp.effective_to
FROM med.user_group_members ugm
JOIN med.group_permissions gp ON gp.group_id = ugm.group_id
WHERE (ugm.effective_to IS NULL OR ugm.effective_to > SYSUTCDATETIME())
  AND ugm.effective_from <= SYSUTCDATETIME()
  AND (gp.effective_to IS NULL OR gp.effective_to > SYSUTCDATETIME())
  AND gp.effective_from <= SYSUTCDATETIME()
UNION ALL
SELECT
    upo.user_id,
    upo.permission_id,
    upo.effect_code,
    upo.department_scope_type,
    upo.department_id,
    upo.priority,
    N'user_override' AS source_type,
    upo.user_permission_override_id AS source_id,
    upo.effective_from,
    upo.effective_to
FROM med.user_permission_overrides upo
WHERE (upo.effective_to IS NULL OR upo.effective_to > SYSUTCDATETIME())
  AND upo.effective_from <= SYSUTCDATETIME();
GO

CREATE OR ALTER FUNCTION med.fn_user_has_permission_itvf (
    @user_id UNIQUEIDENTIFIER,
    @permission_id UNIQUEIDENTIFIER,
    @context_department_id UNIQUEIDENTIFIER = NULL
)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
(
    WITH active_sources AS (
        SELECT rp.effect_code, rp.priority, 1 AS source_rank, rp.effective_from
        FROM med.user_roles ur
        JOIN med.role_permissions rp ON rp.role_id = ur.role_id
        JOIN med.users u ON u.user_id = ur.user_id
        WHERE ur.user_id = @user_id
          AND u.status = N'active'
          AND u.deleted_at IS NULL
          AND rp.permission_id = @permission_id
          AND ur.effective_from <= SYSUTCDATETIME()
          AND (ur.effective_to IS NULL OR ur.effective_to > SYSUTCDATETIME())
          AND rp.effective_from <= SYSUTCDATETIME()
          AND (rp.effective_to IS NULL OR rp.effective_to > SYSUTCDATETIME())
          AND (
                ur.department_id IS NULL
                OR @context_department_id = ur.department_id
                OR EXISTS (
                    SELECT 1 FROM med.department_closure dc
                    WHERE dc.ancestor_department_id = ur.department_id
                      AND dc.descendant_department_id = @context_department_id
                )
          )
          AND (
                rp.department_scope_type = N'global'
                OR (rp.department_scope_type = N'department' AND rp.department_id = @context_department_id)
                OR (rp.department_scope_type = N'department_tree' AND EXISTS (
                    SELECT 1 FROM med.department_closure dc
                    WHERE dc.ancestor_department_id = rp.department_id
                      AND dc.descendant_department_id = @context_department_id
                ))
                OR (rp.department_scope_type = N'own_department' AND u.primary_department_id = @context_department_id)
          )

        UNION ALL

        SELECT gp.effect_code, gp.priority, 2 AS source_rank, gp.effective_from
        FROM med.user_group_members ugm
        JOIN med.groups g ON g.group_id = ugm.group_id
        JOIN med.group_permissions gp ON gp.group_id = ugm.group_id
        JOIN med.users u ON u.user_id = ugm.user_id
        WHERE ugm.user_id = @user_id
          AND u.status = N'active'
          AND u.deleted_at IS NULL
          AND gp.permission_id = @permission_id
          AND ugm.effective_from <= SYSUTCDATETIME()
          AND (ugm.effective_to IS NULL OR ugm.effective_to > SYSUTCDATETIME())
          AND gp.effective_from <= SYSUTCDATETIME()
          AND (gp.effective_to IS NULL OR gp.effective_to > SYSUTCDATETIME())
          AND (
                g.department_id IS NULL
                OR @context_department_id = g.department_id
                OR EXISTS (
                    SELECT 1 FROM med.department_closure dc
                    WHERE dc.ancestor_department_id = g.department_id
                      AND dc.descendant_department_id = @context_department_id
                )
          )
          AND (
                gp.department_scope_type = N'global'
                OR (gp.department_scope_type = N'department' AND gp.department_id = @context_department_id)
                OR (gp.department_scope_type = N'department_tree' AND EXISTS (
                    SELECT 1 FROM med.department_closure dc
                    WHERE dc.ancestor_department_id = gp.department_id
                      AND dc.descendant_department_id = @context_department_id
                ))
                OR (gp.department_scope_type = N'own_department' AND u.primary_department_id = @context_department_id)
          )

        UNION ALL

        SELECT upo.effect_code, upo.priority, 3 AS source_rank, upo.effective_from
        FROM med.user_permission_overrides upo
        JOIN med.users u ON u.user_id = upo.user_id
        WHERE upo.user_id = @user_id
          AND u.status = N'active'
          AND u.deleted_at IS NULL
          AND upo.permission_id = @permission_id
          AND upo.effective_from <= SYSUTCDATETIME()
          AND (upo.effective_to IS NULL OR upo.effective_to > SYSUTCDATETIME())
          AND (
                upo.department_scope_type = N'global'
                OR (upo.department_scope_type = N'department' AND upo.department_id = @context_department_id)
                OR (upo.department_scope_type = N'department_tree' AND EXISTS (
                    SELECT 1 FROM med.department_closure dc
                    WHERE dc.ancestor_department_id = upo.department_id
                      AND dc.descendant_department_id = @context_department_id
                ))
                OR (upo.department_scope_type = N'own_department' AND u.primary_department_id = @context_department_id)
          )
    ), ranked AS (
        SELECT TOP (1) effect_code
        FROM active_sources
        ORDER BY
            priority DESC,
            CASE WHEN effect_code = N'deny' THEN 1 ELSE 0 END DESC,
            source_rank DESC,
            effective_from DESC
    )
    SELECT CAST(CASE WHEN effect_code = N'allow' THEN 1 ELSE 0 END AS BIT) AS has_permission
    FROM ranked
);
GO

CREATE VIEW med.vw_permission_change_report
WITH SCHEMABINDING
AS
SELECT
    pcr.permission_change_request_id,
    pcr.target_type,
    pcr.target_role_id,
    pcr.target_group_id,
    pcr.target_user_id,
    pcr.change_status,
    pcr.reason,
    requester.full_name AS requested_by_name,
    approver.full_name AS approved_by_name,
    applier.full_name AS applied_by_name,
    pcr.requested_at,
    pcr.effective_at,
    pcr.applied_at
FROM med.permission_change_requests pcr
LEFT JOIN med.users requester ON requester.user_id = pcr.requested_by
LEFT JOIN med.users approver ON approver.user_id = pcr.approved_by
LEFT JOIN med.users applier ON applier.user_id = pcr.applied_by;
GO

CREATE VIEW med.vw_resource_consumption_variance
WITH SCHEMABINDING
AS
SELECT
    o.technical_order_id,
    pr.external_patient_id,
    er.external_encounter_id,
    ts.service_code,
    ts.name AS service_name,
    rc.resource_type,
    rc.resource_code,
    rc.name AS resource_name,
    COALESCE(pvrn.standard_quantity, trn.standard_quantity) AS standard_quantity,
    usage.actual_quantity,
    usage.actual_quantity - COALESCE(pvrn.standard_quantity, trn.standard_quantity) AS variance_quantity,
    usage.unit_code,
    usage.revision_no,
    usage.is_final,
    usage.variance_reason,
    usage.captured_at
FROM med.actual_resource_usages usage
JOIN med.technical_orders o ON o.technical_order_id = usage.technical_order_id
JOIN med.technical_services ts ON ts.technical_service_id = o.technical_service_id
JOIN med.resource_catalog rc ON rc.resource_id = usage.resource_id
LEFT JOIN med.procedure_version_resource_norms pvrn
    ON pvrn.procedure_version_id = o.procedure_version_id
   AND pvrn.resource_id = usage.resource_id
LEFT JOIN med.technical_resource_norms trn
    ON trn.technical_service_id = ts.technical_service_id
   AND trn.resource_id = usage.resource_id
LEFT JOIN med.patient_refs pr ON pr.patient_ref_id = o.patient_ref_id
LEFT JOIN med.encounter_refs er ON er.encounter_ref_id = o.encounter_ref_id
WHERE usage.is_final = 1;
GO

/* ============================================================
   14. SEED BASE SCREENS, FEATURES, PERMISSIONS
   ============================================================ */

INSERT INTO med.screen_catalog (screen_code, name, route, module_code)
VALUES
(N'SCR_DASHBOARD', N'Tổng quan', N'/admin', N'CORE'),
(N'SCR_ORG_DEPARTMENTS', N'Khoa/phòng', N'/admin/to-chuc/khoa-phong', N'ORG'),
(N'SCR_ORG_USERS', N'Người dùng', N'/admin/to-chuc/nguoi-dung', N'ORG'),
(N'SCR_ORG_ROLES', N'Vai trò', N'/admin/to-chuc/vai-tro', N'ORG'),
(N'SCR_ORG_GROUPS', N'Nhóm người dùng', N'/admin/to-chuc/nhom', N'ORG'),
(N'SCR_PERMISSIONS', N'Phân quyền', N'/admin/phan-quyen', N'PERM'),
(N'SCR_PERMISSION_APPROVAL', N'Phê duyệt quyền', N'/phe-duyet', N'PERM'),
(N'SCR_AUDIT', N'Nhật ký kiểm toán', N'/admin/nhat-ky', N'PERM'),
(N'SCR_SYSTEM_SCREENS', N'Màn hình và tính năng', N'/admin/he-thong/man-hinh', N'SYSTEM'),
(N'SCR_SETTINGS', N'Cài đặt', N'/admin/cai-dat', N'CORE'),
(N'SCR_PROFILE', N'Hồ sơ cá nhân', N'/admin/ho-so', N'CORE'),
(N'SCR_PROCEDURES', N'Quy trình kỹ thuật', N'/admin/quy-trinh', N'PROC'),
(N'SCR_PROCEDURE_CREATE', N'Tạo quy trình', N'/admin/quy-trinh/tao', N'PROC'),
(N'SCR_PROCEDURE_APPROVAL', N'Phê duyệt quy trình', N'/admin/quy-trinh/phe-duyet', N'PROC'),
(N'SCR_PROCEDURES_WORKSPACE', N'Không gian quy trình chuyên môn', N'/quy-trinh-pro', N'PROC'),
(N'SCR_CATALOG', N'Danh mục kỹ thuật', N'/admin/danh-muc', N'CAT'),
(N'SCR_RESOURCES', N'Tài nguyên', N'/tai-nguyen', N'TECH'),
(N'SCR_ORDERS', N'Chỉ định kỹ thuật', N'/dieu-phoi', N'TECH'),
(N'SCR_PROTOCOLS', N'Phác đồ', N'/admin/phac-do', N'CLINICAL'),
(N'SCR_PROTOCOLS_WORKSPACE', N'Không gian phác đồ', N'/phac-do-pro', N'CLINICAL'),
(N'SCR_CLINICAL', N'Lâm sàng', N'/lam-sang', N'CLINICAL'),
(N'SCR_CLINICAL_ADMIN', N'Quản trị lâm sàng', N'/admin/lam-sang', N'CLINICAL'),
(N'SCR_REPORTS', N'Báo cáo tổng hợp', N'/admin/bao-cao', N'REPORT'),
(N'SCR_REPORT_CONSUMPTION', N'Báo cáo tiêu thụ', N'/admin/bao-cao/tieu-thu', N'REPORT'),
(N'SCR_NOTIFICATIONS', N'Thông báo', N'/thong-bao', N'CORE');
GO

INSERT INTO med.feature_catalog (screen_id, feature_code, name)
SELECT screen_id, CONCAT(screen_code, N'_MAIN'), name
FROM med.screen_catalog;
GO

;WITH screen_actions AS (
    SELECT screen_code, action_code
    FROM (VALUES
        (N'SCR_DASHBOARD', N'view'),
        (N'SCR_ORG_DEPARTMENTS', N'view'), (N'SCR_ORG_DEPARTMENTS', N'create'), (N'SCR_ORG_DEPARTMENTS', N'update'), (N'SCR_ORG_DEPARTMENTS', N'delete'),
        (N'SCR_ORG_USERS', N'view'), (N'SCR_ORG_USERS', N'create'), (N'SCR_ORG_USERS', N'update'), (N'SCR_ORG_USERS', N'delete'),
        (N'SCR_ORG_ROLES', N'view'), (N'SCR_ORG_ROLES', N'create'), (N'SCR_ORG_ROLES', N'update'), (N'SCR_ORG_ROLES', N'delete'), (N'SCR_ORG_ROLES', N'configure'),
        (N'SCR_ORG_GROUPS', N'view'), (N'SCR_ORG_GROUPS', N'create'), (N'SCR_ORG_GROUPS', N'update'), (N'SCR_ORG_GROUPS', N'delete'), (N'SCR_ORG_GROUPS', N'configure'),
        (N'SCR_PERMISSIONS', N'view'), (N'SCR_PERMISSIONS', N'create'), (N'SCR_PERMISSIONS', N'update'), (N'SCR_PERMISSIONS', N'delete'), (N'SCR_PERMISSIONS', N'approve'), (N'SCR_PERMISSIONS', N'configure'),
        (N'SCR_PERMISSION_APPROVAL', N'view'), (N'SCR_PERMISSION_APPROVAL', N'approve'),
        (N'SCR_AUDIT', N'view'), (N'SCR_AUDIT', N'export'),
        (N'SCR_SYSTEM_SCREENS', N'view'), (N'SCR_SYSTEM_SCREENS', N'create'), (N'SCR_SYSTEM_SCREENS', N'update'), (N'SCR_SYSTEM_SCREENS', N'delete'), (N'SCR_SYSTEM_SCREENS', N'configure'),
        (N'SCR_SETTINGS', N'view'), (N'SCR_SETTINGS', N'update'), (N'SCR_SETTINGS', N'configure'),
        (N'SCR_PROFILE', N'view'), (N'SCR_PROFILE', N'update'),
        (N'SCR_PROCEDURES', N'view'), (N'SCR_PROCEDURES', N'create'), (N'SCR_PROCEDURES', N'update'), (N'SCR_PROCEDURES', N'delete'), (N'SCR_PROCEDURES', N'approve'), (N'SCR_PROCEDURES', N'publish'),
        (N'SCR_PROCEDURE_CREATE', N'view'), (N'SCR_PROCEDURE_CREATE', N'create'),
        (N'SCR_PROCEDURE_APPROVAL', N'view'), (N'SCR_PROCEDURE_APPROVAL', N'approve'), (N'SCR_PROCEDURE_APPROVAL', N'publish'),
        (N'SCR_PROCEDURES_WORKSPACE', N'view'), (N'SCR_PROCEDURES_WORKSPACE', N'approve'), (N'SCR_PROCEDURES_WORKSPACE', N'publish'),
        (N'SCR_CATALOG', N'view'), (N'SCR_CATALOG', N'create'), (N'SCR_CATALOG', N'update'), (N'SCR_CATALOG', N'delete'), (N'SCR_CATALOG', N'configure'),
        (N'SCR_RESOURCES', N'view'), (N'SCR_RESOURCES', N'create'), (N'SCR_RESOURCES', N'update'), (N'SCR_RESOURCES', N'delete'), (N'SCR_RESOURCES', N'configure'),
        (N'SCR_ORDERS', N'view'), (N'SCR_ORDERS', N'create'), (N'SCR_ORDERS', N'update'), (N'SCR_ORDERS', N'delete'), (N'SCR_ORDERS', N'approve'), (N'SCR_ORDERS', N'execute'),
        (N'SCR_PROTOCOLS', N'view'), (N'SCR_PROTOCOLS', N'create'), (N'SCR_PROTOCOLS', N'update'), (N'SCR_PROTOCOLS', N'delete'), (N'SCR_PROTOCOLS', N'approve'), (N'SCR_PROTOCOLS', N'publish'),
        (N'SCR_PROTOCOLS_WORKSPACE', N'view'), (N'SCR_PROTOCOLS_WORKSPACE', N'execute'),
        (N'SCR_CLINICAL', N'view'), (N'SCR_CLINICAL', N'create'), (N'SCR_CLINICAL', N'update'), (N'SCR_CLINICAL', N'approve'), (N'SCR_CLINICAL', N'execute'),
        (N'SCR_CLINICAL_ADMIN', N'view'), (N'SCR_CLINICAL_ADMIN', N'create'), (N'SCR_CLINICAL_ADMIN', N'update'), (N'SCR_CLINICAL_ADMIN', N'approve'), (N'SCR_CLINICAL_ADMIN', N'execute'),
        (N'SCR_REPORTS', N'view'), (N'SCR_REPORTS', N'export'),
        (N'SCR_REPORT_CONSUMPTION', N'view'), (N'SCR_REPORT_CONSUMPTION', N'export'),
        (N'SCR_NOTIFICATIONS', N'view'), (N'SCR_NOTIFICATIONS', N'create'), (N'SCR_NOTIFICATIONS', N'update'), (N'SCR_NOTIFICATIONS', N'delete'), (N'SCR_NOTIFICATIONS', N'configure')
    ) v(screen_code, action_code)
)
INSERT INTO med.permissions (permission_code, screen_id, feature_id, action_code, description)
SELECT CONCAT(sa.screen_code, N':', UPPER(sa.action_code)), sc.screen_id, fc.feature_id, sa.action_code, lac.name
FROM screen_actions sa
JOIN med.screen_catalog sc ON sc.screen_code = sa.screen_code
JOIN med.feature_catalog fc ON fc.screen_id = sc.screen_id AND fc.feature_code = CONCAT(sc.screen_code, N'_MAIN')
JOIN med.lookup_action_codes lac ON lac.action_code = sa.action_code;
GO

/* ============================================================
   15. OPTIONAL BASE ROLES
   ============================================================ */

INSERT INTO med.roles (code, name, description, is_system)
VALUES
(N'SYSTEM_ADMIN', N'Quản trị hệ thống', N'Toàn quyền quản trị hệ thống', 1),
(N'DEPARTMENT_ADMIN', N'Quản trị khoa/phòng', N'Quản trị trong phạm vi khoa/phòng', 1),
(N'DOCTOR', N'Bác sĩ', N'Bác sĩ chỉ định, áp dụng phác đồ và theo dõi điều trị', 1),
(N'NURSE', N'Điều dưỡng', N'Điều dưỡng thực hiện và cập nhật hoạt động lâm sàng', 1),
(N'TECHNICIAN', N'Kỹ thuật viên', N'Kỹ thuật viên thực hiện dịch vụ kỹ thuật', 1),
(N'PHARMACIST', N'Dược sĩ', N'Quản lý thuốc, vật tư và định mức liên quan dược', 1),
(N'CLINICAL_USER', N'Người dùng lâm sàng', N'Người dùng lâm sàng', 1),
(N'REPORT_VIEWER', N'Người xem báo cáo', N'Người xem báo cáo', 1);
GO

INSERT INTO med.role_permissions (role_id, permission_id, effect_code, department_scope_type, priority, reason)
SELECT r.role_id, p.permission_id, N'allow', N'global', 900, N'Base system administrator permission'
FROM med.roles r
CROSS JOIN med.permissions p
WHERE r.code = N'SYSTEM_ADMIN';
GO

INSERT INTO med.role_permissions (role_id, permission_id, effect_code, department_scope_type, priority, reason)
SELECT r.role_id, p.permission_id, N'allow', N'own_department', 500, N'Base department administrator permission'
FROM med.roles r
JOIN med.permissions p ON p.permission_code IN (
    N'SCR_DASHBOARD:VIEW',
    N'SCR_ORG_DEPARTMENTS:VIEW', N'SCR_ORG_USERS:VIEW', N'SCR_ORG_ROLES:VIEW', N'SCR_ORG_GROUPS:VIEW',
    N'SCR_PROCEDURES:VIEW', N'SCR_PROCEDURES:CREATE', N'SCR_PROCEDURES:UPDATE', N'SCR_PROCEDURES:APPROVE',
    N'SCR_PROCEDURE_CREATE:VIEW', N'SCR_PROCEDURE_CREATE:CREATE',
    N'SCR_PROCEDURE_APPROVAL:VIEW', N'SCR_PROCEDURE_APPROVAL:APPROVE',
    N'SCR_CATALOG:VIEW', N'SCR_CATALOG:CREATE', N'SCR_CATALOG:UPDATE',
    N'SCR_RESOURCES:VIEW', N'SCR_RESOURCES:CREATE', N'SCR_RESOURCES:UPDATE',
    N'SCR_ORDERS:VIEW', N'SCR_ORDERS:CREATE', N'SCR_ORDERS:UPDATE', N'SCR_ORDERS:APPROVE',
    N'SCR_PROTOCOLS:VIEW', N'SCR_PROTOCOLS:CREATE', N'SCR_PROTOCOLS:UPDATE', N'SCR_PROTOCOLS:APPROVE',
    N'SCR_CLINICAL:VIEW', N'SCR_CLINICAL:CREATE', N'SCR_CLINICAL:UPDATE', N'SCR_CLINICAL:EXECUTE',
    N'SCR_REPORTS:VIEW', N'SCR_REPORT_CONSUMPTION:VIEW', N'SCR_NOTIFICATIONS:VIEW'
)
WHERE r.code = N'DEPARTMENT_ADMIN';
GO

INSERT INTO med.role_permissions (role_id, permission_id, effect_code, department_scope_type, priority, reason)
SELECT r.role_id, p.permission_id, N'allow', N'own_department', 300, N'Base clinical permission'
FROM med.roles r
JOIN med.permissions p ON p.permission_code IN (
    N'SCR_DASHBOARD:VIEW', N'SCR_PROCEDURES_WORKSPACE:VIEW',
    N'SCR_ORDERS:VIEW', N'SCR_ORDERS:CREATE',
    N'SCR_PROTOCOLS_WORKSPACE:VIEW', N'SCR_PROTOCOLS_WORKSPACE:EXECUTE',
    N'SCR_CLINICAL:VIEW', N'SCR_CLINICAL:CREATE', N'SCR_CLINICAL:UPDATE', N'SCR_CLINICAL:EXECUTE',
    N'SCR_NOTIFICATIONS:VIEW', N'SCR_NOTIFICATIONS:UPDATE', N'SCR_PROFILE:VIEW', N'SCR_PROFILE:UPDATE'
)
WHERE r.code IN (N'CLINICAL_USER', N'DOCTOR', N'NURSE');
GO

INSERT INTO med.role_permissions (role_id, permission_id, effect_code, department_scope_type, priority, reason)
SELECT r.role_id, p.permission_id, N'allow', N'own_department', 300, N'Base technical or pharmacy permission'
FROM med.roles r
JOIN med.permissions p ON p.permission_code IN (
    N'SCR_DASHBOARD:VIEW', N'SCR_RESOURCES:VIEW', N'SCR_ORDERS:VIEW', N'SCR_ORDERS:UPDATE', N'SCR_ORDERS:EXECUTE',
    N'SCR_CATALOG:VIEW', N'SCR_NOTIFICATIONS:VIEW', N'SCR_PROFILE:VIEW', N'SCR_PROFILE:UPDATE'
)
WHERE r.code IN (N'TECHNICIAN', N'PHARMACIST');
GO

INSERT INTO med.role_permissions (role_id, permission_id, effect_code, department_scope_type, priority, reason)
SELECT r.role_id, p.permission_id, N'allow', N'global', 300, N'Base report viewer permission'
FROM med.roles r
JOIN med.permissions p ON p.permission_code IN (
    N'SCR_DASHBOARD:VIEW', N'SCR_REPORTS:VIEW', N'SCR_REPORTS:EXPORT',
    N'SCR_REPORT_CONSUMPTION:VIEW', N'SCR_REPORT_CONSUMPTION:EXPORT'
)
WHERE r.code = N'REPORT_VIEWER';
GO

/* ============================================================
   16. COMMIT
   ============================================================ */

-- Optional storage-level privacy controls. Grant UNMASK only to trusted roles.
-- Important: Dynamic Data Masking changes column metadata. If a column has dependent indexes,
-- apply masking before creating those indexes, or drop/recreate the dependent indexes here.
IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'med.users') AND name = N'UX_users_email_not_null')
    DROP INDEX UX_users_email_not_null ON med.users;
GO

ALTER TABLE med.users ALTER COLUMN email ADD MASKED WITH (FUNCTION = 'email()');
GO

CREATE UNIQUE INDEX UX_users_email_not_null ON med.users(email) WHERE email IS NOT NULL;
GO

ALTER TABLE med.patient_refs ALTER COLUMN display_name ADD MASKED WITH (FUNCTION = 'partial(1,"****",1)');
GO
ALTER TABLE med.patient_refs ALTER COLUMN birth_date ADD MASKED WITH (FUNCTION = 'default()');
GO

CREATE OR ALTER PROCEDURE med.sp_archive_department
    @department_id UNIQUEIDENTIFIER,
    @actor_user_id UNIQUEIDENTIFIER = NULL,
    @reason NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        IF NOT EXISTS (SELECT 1 FROM med.departments WHERE department_id = @department_id AND status = N'active')
            THROW 51023, 'Department is not active or does not exist.', 1;
        IF EXISTS (SELECT 1 FROM med.departments WHERE parent_department_id = @department_id AND status = N'active')
            THROW 51024, 'Cannot archive department with active children. Archive children first.', 1;

        UPDATE med.departments
           SET status = N'archived', updated_at = SYSUTCDATETIME()
         WHERE department_id = @department_id;

        DECLARE @meta NVARCHAR(MAX) = (
            SELECT @reason AS reason
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        INSERT INTO med.audit_logs (
            correlation_id, actor_user_id, action_code, target_type, target_id,
            department_id, metadata_json, occurred_at
        )
        VALUES (
            NEWID(), @actor_user_id, N'archive_department', N'med.departments', CAST(@department_id AS NVARCHAR(100)),
            @department_id, @meta, SYSUTCDATETIME()
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

/* ============================================================
   ROLES AND GRANTS
   ============================================================ */

IF DATABASE_PRINCIPAL_ID(N'med_app_role') IS NULL
    EXEC(N'CREATE ROLE med_app_role');
GO
GRANT SELECT, INSERT, UPDATE ON SCHEMA::med TO med_app_role;
GRANT EXECUTE ON SCHEMA::med TO med_app_role;
GO

IF DATABASE_PRINCIPAL_ID(N'med_pii_unmask_role') IS NULL
    EXEC(N'CREATE ROLE med_pii_unmask_role');
GO
GRANT UNMASK TO med_pii_unmask_role;
GO

-- Application accounts must be added separately:
--   ALTER ROLE med_app_role ADD MEMBER [your_app_user];
-- Grant unmask only to clinical staff who need unmasked PII:
--   ALTER ROLE med_pii_unmask_role ADD MEMBER [clinical_user];

/*
   Optional: Initial fragmentation reduction after seed data load.
   Toggle the IF condition to 1 = 1 to enable.
*/
IF 1 = 0
BEGIN
    EXEC sp_MSforeachtable @command1 = N'ALTER INDEX ALL ON ? REBUILD WITH (FILLFACTOR = 90, ONLINE = OFF)';
END;
GO

/*
   Optional: Always Encrypted for highly sensitive PII columns.
   Recommended candidates:
     - med.patient_refs.external_patient_id
     - med.patient_refs.display_name
     - med.users.email
     - med.users.full_name
   Provision a Column Master Key and Column Encryption Key before applying ALTER COLUMN ENCRYPTED WITH.
*/

PRINT N'Database MedicalProcedureManagement schema created successfully. v4 consolidated.';
GO
