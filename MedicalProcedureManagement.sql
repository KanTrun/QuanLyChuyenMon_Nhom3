/*
    SQL Server Full Database Script
    Module: Quan Ly Quy Trinh Ky Thuat Chuyen Mon
*/

/* ============================================================
   00. CREATE DATABASE
   ============================================================ */

USE master;
GO

IF DB_ID(N'MedicalProcedureManagement') IS NULL
BEGIN
    CREATE DATABASE MedicalProcedureManagement
    COLLATE Vietnamese_100_CI_AS_SC_UTF8;
END;
GO

ALTER DATABASE MedicalProcedureManagement SET RECOVERY FULL;
GO

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

/* ============================================================
   02. LOOKUP TABLES
   ============================================================ */

CREATE TABLE med.lookup_record_status (
    code NVARCHAR(30) NOT NULL,
    name NVARCHAR(100) NOT NULL,
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

/* ============================================================
   03. ORGANIZATION AND IDENTITY
   ============================================================ */

CREATE TABLE med.departments (
    department_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_departments_id DEFAULT NEWID(),
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

CREATE OR ALTER PROCEDURE med.sp_rebuild_department_closure
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

CREATE OR ALTER TRIGGER med.TR_departments_rebuild_closure
ON med.departments
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    EXEC med.sp_rebuild_department_closure;
END;
GO

CREATE TABLE med.users (
    user_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_users_id DEFAULT NEWID(),
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

CREATE TABLE med.roles (
    role_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_roles_id DEFAULT NEWID(),
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
    group_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_groups_id DEFAULT NEWID(),
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
    user_role_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_user_roles_id DEFAULT NEWID(),
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

CREATE TABLE med.user_group_members (
    user_group_member_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_user_group_members_id DEFAULT NEWID(),
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

/* ============================================================
   04. SCREEN, FEATURE, PERMISSION CATALOG
   ============================================================ */

CREATE TABLE med.screen_catalog (
    screen_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_screen_catalog_id DEFAULT NEWID(),
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
    feature_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_feature_catalog_id DEFAULT NEWID(),
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
    permission_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_permissions_id DEFAULT NEWID(),
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

-- SQL Server treats NULL specially in unique constraints. Split nullable feature_id cases.
CREATE UNIQUE INDEX UX_permissions_natural_with_feature
ON med.permissions(screen_id, feature_id, action_code)
WHERE feature_id IS NOT NULL;
GO

CREATE UNIQUE INDEX UX_permissions_natural_without_feature
ON med.permissions(screen_id, action_code)
WHERE feature_id IS NULL;
GO

/* ============================================================
   05. STRICT RBAC/ABAC ASSIGNMENTS
   ============================================================ */

CREATE TABLE med.role_permissions (
    role_permission_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_role_permissions_id DEFAULT NEWID(),
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

CREATE TABLE med.group_permissions (
    group_permission_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_group_permissions_id DEFAULT NEWID(),
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

CREATE TABLE med.user_permission_overrides (
    user_permission_override_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_user_permission_overrides_id DEFAULT NEWID(),
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

/* ============================================================
   06. IMMUTABLE AUDIT LOG
   ============================================================ */

CREATE TABLE med.audit_logs (
    audit_log_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_audit_logs_id DEFAULT NEWID(),
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
    CONSTRAINT PK_audit_logs PRIMARY KEY (audit_log_id),
    CONSTRAINT FK_audit_logs_actor FOREIGN KEY (actor_user_id) REFERENCES med.users(user_id),
    CONSTRAINT FK_audit_logs_department FOREIGN KEY (department_id) REFERENCES med.departments(department_id),
    CONSTRAINT CK_audit_logs_before_json CHECK (before_json IS NULL OR ISJSON(before_json) = 1),
    CONSTRAINT CK_audit_logs_after_json CHECK (after_json IS NULL OR ISJSON(after_json) = 1),
    CONSTRAINT CK_audit_logs_metadata_json CHECK (metadata_json IS NULL OR ISJSON(metadata_json) = 1)
);
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

/* ============================================================
   07. PERMISSION CHANGE WORKFLOW
   ============================================================ */

CREATE TABLE med.permission_change_requests (
    permission_change_request_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_permission_change_requests_id DEFAULT NEWID(),
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
    CONSTRAINT CK_permission_change_requests_target_type CHECK (target_type IN (N'role',N'group',N'user')),
    CONSTRAINT CK_permission_change_requests_target_exactly_one CHECK (
        (target_type = N'role' AND target_role_id IS NOT NULL AND target_group_id IS NULL AND target_user_id IS NULL) OR
        (target_type = N'group' AND target_role_id IS NULL AND target_group_id IS NOT NULL AND target_user_id IS NULL) OR
        (target_type = N'user' AND target_role_id IS NULL AND target_group_id IS NULL AND target_user_id IS NOT NULL)
    ),
    CONSTRAINT CK_permission_change_requests_effective_date CHECK (effective_at >= requested_at)
);
GO

CREATE INDEX IX_permission_change_requests_due ON med.permission_change_requests(change_status, effective_at);
GO

CREATE TABLE med.permission_change_items (
    permission_change_item_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_permission_change_items_id DEFAULT NEWID(),
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

CREATE OR ALTER PROCEDURE med.sp_apply_due_permission_changes
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @now DATETIME2(3) = SYSUTCDATETIME();

    BEGIN TRANSACTION;

    ;WITH due AS (
        SELECT *
        FROM med.permission_change_requests WITH (UPDLOCK, READPAST)
        WHERE change_status = N'scheduled'
          AND effective_at <= @now
    )
    UPDATE rp
       SET effective_to = @now
    FROM med.role_permissions rp
    JOIN due d ON d.target_type = N'role' AND d.target_role_id = rp.role_id
    JOIN med.permission_change_items i ON i.permission_change_request_id = d.permission_change_request_id
    WHERE i.operation_code IN (N'revoke', N'update')
      AND rp.permission_id = i.permission_id
      AND rp.effective_to IS NULL;

    ;WITH due AS (
        SELECT *
        FROM med.permission_change_requests WITH (UPDLOCK, READPAST)
        WHERE change_status = N'scheduled'
          AND effective_at <= @now
    )
    UPDATE gp
       SET effective_to = @now
    FROM med.group_permissions gp
    JOIN due d ON d.target_type = N'group' AND d.target_group_id = gp.group_id
    JOIN med.permission_change_items i ON i.permission_change_request_id = d.permission_change_request_id
    WHERE i.operation_code IN (N'revoke', N'update')
      AND gp.permission_id = i.permission_id
      AND gp.effective_to IS NULL;

    ;WITH due AS (
        SELECT *
        FROM med.permission_change_requests WITH (UPDLOCK, READPAST)
        WHERE change_status = N'scheduled'
          AND effective_at <= @now
    )
    UPDATE upo
       SET effective_to = @now
    FROM med.user_permission_overrides upo
    JOIN due d ON d.target_type = N'user' AND d.target_user_id = upo.user_id
    JOIN med.permission_change_items i ON i.permission_change_request_id = d.permission_change_request_id
    WHERE i.operation_code IN (N'revoke', N'update')
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
    WHERE d.change_status = N'scheduled'
      AND d.effective_at <= @now
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
    WHERE d.change_status = N'scheduled'
      AND d.effective_at <= @now
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
    WHERE d.change_status = N'scheduled'
      AND d.effective_at <= @now
      AND d.target_type = N'user'
      AND i.operation_code IN (N'grant', N'update');

    UPDATE med.permission_change_requests
       SET change_status = N'applied', applied_at = @now
    WHERE change_status = N'scheduled'
      AND effective_at <= @now;

    COMMIT TRANSACTION;
END;
GO

/* ============================================================
   08. PROCEDURE VERSIONING
   ============================================================ */

CREATE TABLE med.professional_procedures (
    procedure_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_professional_procedures_id DEFAULT NEWID(),
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
    CONSTRAINT CK_professional_procedures_type CHECK (procedure_type IN (N'technical',N'care',N'clinical_protocol',N'surgery',N'procedure'))
);
GO

CREATE TABLE med.procedure_versions (
    procedure_version_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_procedure_versions_id DEFAULT NEWID(),
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

-- Split nullable department_id version uniqueness into global and department-scoped indexes.
CREATE UNIQUE INDEX UX_procedure_versions_no_global
ON med.procedure_versions(procedure_id, version_no)
WHERE department_id IS NULL;
GO

CREATE UNIQUE INDEX UX_procedure_versions_no_department
ON med.procedure_versions(procedure_id, department_id, version_no)
WHERE department_id IS NOT NULL;
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

CREATE TABLE med.procedure_steps (
    procedure_step_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_procedure_steps_id DEFAULT NEWID(),
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

CREATE TABLE med.procedure_attachments (
    procedure_attachment_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_procedure_attachments_id DEFAULT NEWID(),
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
    CONSTRAINT CK_procedure_attachments_type CHECK (attachment_type IN (N'sop',N'guideline',N'form',N'reference',N'other')),
    CONSTRAINT CK_procedure_attachments_size CHECK (file_size_bytes IS NULL OR file_size_bytes >= 0)
);
GO

CREATE TABLE med.procedure_screen_mappings (
    procedure_screen_mapping_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_procedure_screen_mappings_id DEFAULT NEWID(),
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

-- Split nullable uniqueness cases for feature_id/action_code.
CREATE UNIQUE INDEX UX_procedure_screen_mappings_screen_only
ON med.procedure_screen_mappings(procedure_version_id, screen_id)
WHERE feature_id IS NULL AND action_code IS NULL;
GO

CREATE UNIQUE INDEX UX_procedure_screen_mappings_feature_only
ON med.procedure_screen_mappings(procedure_version_id, screen_id, feature_id)
WHERE feature_id IS NOT NULL AND action_code IS NULL;
GO

CREATE UNIQUE INDEX UX_procedure_screen_mappings_action_only
ON med.procedure_screen_mappings(procedure_version_id, screen_id, action_code)
WHERE feature_id IS NULL AND action_code IS NOT NULL;
GO

CREATE UNIQUE INDEX UX_procedure_screen_mappings_feature_action
ON med.procedure_screen_mappings(procedure_version_id, screen_id, feature_id, action_code)
WHERE feature_id IS NOT NULL AND action_code IS NOT NULL;
GO

CREATE INDEX IX_procedure_screen_mappings_lookup
ON med.procedure_screen_mappings(screen_id, feature_id, action_code, enforcement_mode);
GO

/* ============================================================
   09. PATIENT / ENCOUNTER REFERENCES
   ============================================================ */

CREATE TABLE med.patient_refs (
    patient_ref_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_patient_refs_id DEFAULT NEWID(),
    external_patient_id NVARCHAR(100) NOT NULL,
    patient_code NVARCHAR(100) NULL,
    display_name NVARCHAR(255) NULL,
    birth_date DATE NULL,
    gender_code NVARCHAR(20) NULL,
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_patient_refs_created_at DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_patient_refs PRIMARY KEY (patient_ref_id),
    CONSTRAINT UQ_patient_refs_external UNIQUE (external_patient_id),
    CONSTRAINT CK_patient_refs_gender CHECK (gender_code IS NULL OR gender_code IN (N'male',N'female',N'other',N'unknown'))
);
GO

CREATE TABLE med.encounter_refs (
    encounter_ref_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_encounter_refs_id DEFAULT NEWID(),
    patient_ref_id UNIQUEIDENTIFIER NOT NULL,
    external_encounter_id NVARCHAR(100) NOT NULL,
    encounter_type NVARCHAR(50) NULL,
    department_id UNIQUEIDENTIFIER NULL,
    started_at DATETIME2(3) NULL,
    ended_at DATETIME2(3) NULL,
    CONSTRAINT PK_encounter_refs PRIMARY KEY (encounter_ref_id),
    CONSTRAINT UQ_encounter_refs_external UNIQUE (external_encounter_id),
    CONSTRAINT FK_encounter_refs_patient FOREIGN KEY (patient_ref_id) REFERENCES med.patient_refs(patient_ref_id),
    CONSTRAINT FK_encounter_refs_department FOREIGN KEY (department_id) REFERENCES med.departments(department_id),
    CONSTRAINT CK_encounter_refs_dates CHECK (ended_at IS NULL OR started_at IS NULL OR ended_at > started_at)
);
GO

/* ============================================================
   10. TECHNICAL CATALOG AND RESOURCE NORMS
   ============================================================ */

CREATE TABLE med.technical_services (
    technical_service_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_technical_services_id DEFAULT NEWID(),
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
    CONSTRAINT CK_technical_services_type CHECK (service_type IN (N'lab',N'imaging',N'procedure',N'surgery',N'care',N'other'))
);
GO

CREATE TABLE med.resource_catalog (
    resource_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_resource_catalog_id DEFAULT NEWID(),
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

CREATE TABLE med.technical_resource_norms (
    technical_resource_norm_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_technical_resource_norms_id DEFAULT NEWID(),
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

-- Versioned norms preserve historical consistency when a procedure version changes.
CREATE TABLE med.procedure_version_resource_norms (
    procedure_version_resource_norm_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_procedure_version_resource_norms_id DEFAULT NEWID(),
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

CREATE TABLE med.technical_orders (
    technical_order_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_technical_orders_id DEFAULT NEWID(),
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
    CONSTRAINT CK_technical_orders_status CHECK (order_status IN (N'ordered',N'resource_warning',N'scheduled',N'in_progress',N'completed',N'cancelled')),
    CONSTRAINT CK_technical_orders_completed CHECK (completed_at IS NULL OR completed_at >= ordered_at)
);
GO

CREATE INDEX IX_technical_orders_patient
ON med.technical_orders(patient_ref_id, encounter_ref_id, ordered_at DESC);
GO

CREATE INDEX IX_technical_orders_service_status
ON med.technical_orders(technical_service_id, order_status, ordered_at DESC);
GO

CREATE TABLE med.resource_availability_snapshots (
    resource_availability_snapshot_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_resource_availability_snapshots_id DEFAULT NEWID(),
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
    CONSTRAINT CK_resource_availability_snapshots_status CHECK (availability_status IN (N'available',N'insufficient',N'unknown',N'adapter_error')),
    CONSTRAINT CK_resource_availability_snapshots_qty CHECK (required_quantity >= 0 AND (available_quantity IS NULL OR available_quantity >= 0)),
    CONSTRAINT CK_resource_availability_snapshots_payload CHECK (external_payload_json IS NULL OR ISJSON(external_payload_json) = 1)
);
GO

CREATE TABLE med.actual_resource_usages (
    actual_resource_usage_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_actual_resource_usages_id DEFAULT NEWID(),
    technical_order_id UNIQUEIDENTIFIER NOT NULL,
    resource_id UNIQUEIDENTIFIER NOT NULL,
    actual_quantity DECIMAL(18,4) NOT NULL,
    unit_code NVARCHAR(50) NOT NULL,
    variance_reason NVARCHAR(1000) NULL,
    revision_no INT NOT NULL CONSTRAINT DF_actual_resource_usages_revision_no DEFAULT 1,
    is_final BIT NOT NULL CONSTRAINT DF_actual_resource_usages_is_final DEFAULT 1,
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

/* ============================================================
   11. CLINICAL PROTOCOLS
   ============================================================ */

CREATE TABLE med.clinical_protocols (
    clinical_protocol_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_clinical_protocols_id DEFAULT NEWID(),
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
    CONSTRAINT CK_clinical_protocols_type CHECK (protocol_type IN (N'care',N'treatment_protocol',N'surgery',N'procedure'))
);
GO

CREATE TABLE med.clinical_protocol_versions (
    clinical_protocol_version_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_clinical_protocol_versions_id DEFAULT NEWID(),
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

CREATE TABLE med.clinical_protocol_procedures (
    clinical_protocol_procedure_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_clinical_protocol_procedures_id DEFAULT NEWID(),
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
    CONSTRAINT CK_clinical_protocol_procedures_relation CHECK (relation_type IN (N'references', N'requires', N'optional')),
    CONSTRAINT CK_clinical_protocol_procedures_sequence CHECK (sequence_no IS NULL OR sequence_no > 0)
);
GO

CREATE TABLE med.protocol_applicability_rules (
    protocol_applicability_rule_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_protocol_applicability_rules_id DEFAULT NEWID(),
    clinical_protocol_version_id UNIQUEIDENTIFIER NOT NULL,
    rule_type NVARCHAR(50) NOT NULL,
    rule_json NVARCHAR(MAX) NOT NULL,
    priority INT NOT NULL CONSTRAINT DF_protocol_applicability_rules_priority DEFAULT 100,
    is_active BIT NOT NULL CONSTRAINT DF_protocol_applicability_rules_is_active DEFAULT 1,
    created_at DATETIME2(3) NOT NULL CONSTRAINT DF_protocol_applicability_rules_created_at DEFAULT SYSUTCDATETIME(),
    row_version ROWVERSION NOT NULL,
    CONSTRAINT PK_protocol_applicability_rules PRIMARY KEY (protocol_applicability_rule_id),
    CONSTRAINT FK_protocol_applicability_rules_version FOREIGN KEY (clinical_protocol_version_id) REFERENCES med.clinical_protocol_versions(clinical_protocol_version_id),
    CONSTRAINT CK_protocol_applicability_rules_type CHECK (rule_type IN (N'icd',N'patient_group',N'department',N'age',N'gender',N'condition',N'contraindication')),
    CONSTRAINT CK_protocol_applicability_rules_json CHECK (ISJSON(rule_json) = 1)
);
GO

CREATE TABLE med.patient_protocol_applications (
    patient_protocol_application_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_patient_protocol_applications_id DEFAULT NEWID(),
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
    CONSTRAINT CK_patient_protocol_applications_status CHECK (application_status IN (N'suggested',N'applied',N'skipped',N'cancelled')),
    CONSTRAINT CK_patient_protocol_applications_context_json CHECK (decision_context_json IS NULL OR ISJSON(decision_context_json) = 1)
);
GO

CREATE INDEX IX_patient_protocol_applications_patient
ON med.patient_protocol_applications(patient_ref_id, encounter_ref_id, applied_at DESC);
GO

CREATE INDEX IX_patient_protocol_applications_protocol_version
ON med.patient_protocol_applications(clinical_protocol_version_id, application_status, applied_at DESC);
GO

/* ============================================================
   12. NOTIFICATIONS
   ============================================================ */

CREATE TABLE med.notification_preferences (
    notification_preference_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_notification_preferences_id DEFAULT NEWID(),
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

CREATE TABLE med.notifications (
    notification_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_notifications_id DEFAULT NEWID(),
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
    CONSTRAINT CK_notifications_severity CHECK (severity IN (N'info',N'warning',N'critical')),
    CONSTRAINT CK_notifications_payload_json CHECK (payload_json IS NULL OR ISJSON(payload_json) = 1)
);
GO

CREATE INDEX IX_notifications_recipient_unread
ON med.notifications(recipient_user_id, read_at, created_at DESC);
GO

CREATE TABLE med.notification_delivery_attempts (
    notification_delivery_attempt_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_notification_delivery_attempts_id DEFAULT NEWID(),
    notification_id UNIQUEIDENTIFIER NOT NULL,
    channel_code NVARCHAR(30) NOT NULL,
    delivery_status NVARCHAR(30) NOT NULL,
    attempted_at DATETIME2(3) NOT NULL CONSTRAINT DF_notification_delivery_attempts_attempted_at DEFAULT SYSUTCDATETIME(),
    error_message NVARCHAR(2000) NULL,
    CONSTRAINT PK_notification_delivery_attempts PRIMARY KEY (notification_delivery_attempt_id),
    CONSTRAINT FK_notification_delivery_attempts_notification FOREIGN KEY (notification_id) REFERENCES med.notifications(notification_id),
    CONSTRAINT FK_notification_delivery_attempts_channel FOREIGN KEY (channel_code) REFERENCES med.lookup_notification_channels(channel_code),
    CONSTRAINT CK_notification_delivery_attempts_status CHECK (delivery_status IN (N'pending',N'sent',N'failed',N'skipped'))
);
GO

/* ============================================================
   13. REPORTING VIEWS
   ============================================================ */

CREATE VIEW med.vw_effective_user_permissions_source AS
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

CREATE OR ALTER FUNCTION med.fn_user_has_permission (
    @user_id UNIQUEIDENTIFIER,
    @permission_id UNIQUEIDENTIFIER,
    @context_department_id UNIQUEIDENTIFIER = NULL
)
RETURNS BIT
AS
BEGIN
    DECLARE @result BIT = 0;
    DECLARE @now DATETIME2(3) = SYSUTCDATETIME();

    ;WITH active_sources AS (
        SELECT rp.effect_code, rp.priority, 1 AS source_rank, rp.effective_from
        FROM med.user_roles ur
        JOIN med.role_permissions rp ON rp.role_id = ur.role_id
        JOIN med.users u ON u.user_id = ur.user_id
        WHERE ur.user_id = @user_id
          AND u.status = N'active'
          AND u.deleted_at IS NULL
          AND rp.permission_id = @permission_id
          AND ur.effective_from <= @now
          AND (ur.effective_to IS NULL OR ur.effective_to > @now)
          AND rp.effective_from <= @now
          AND (rp.effective_to IS NULL OR rp.effective_to > @now)
          AND (
                ur.department_id IS NULL OR @context_department_id = ur.department_id
                OR EXISTS (SELECT 1 FROM med.department_closure dc WHERE dc.ancestor_department_id = ur.department_id AND dc.descendant_department_id = @context_department_id)
          )
          AND (
                rp.department_scope_type = N'global'
                OR (rp.department_scope_type = N'department' AND rp.department_id = @context_department_id)
                OR (rp.department_scope_type = N'department_tree' AND EXISTS (SELECT 1 FROM med.department_closure dc WHERE dc.ancestor_department_id = rp.department_id AND dc.descendant_department_id = @context_department_id))
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
          AND ugm.effective_from <= @now
          AND (ugm.effective_to IS NULL OR ugm.effective_to > @now)
          AND gp.effective_from <= @now
          AND (gp.effective_to IS NULL OR gp.effective_to > @now)
          AND (
                g.department_id IS NULL OR @context_department_id = g.department_id
                OR EXISTS (SELECT 1 FROM med.department_closure dc WHERE dc.ancestor_department_id = g.department_id AND dc.descendant_department_id = @context_department_id)
          )
          AND (
                gp.department_scope_type = N'global'
                OR (gp.department_scope_type = N'department' AND gp.department_id = @context_department_id)
                OR (gp.department_scope_type = N'department_tree' AND EXISTS (SELECT 1 FROM med.department_closure dc WHERE dc.ancestor_department_id = gp.department_id AND dc.descendant_department_id = @context_department_id))
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
          AND upo.effective_from <= @now
          AND (upo.effective_to IS NULL OR upo.effective_to > @now)
          AND (
                upo.department_scope_type = N'global'
                OR (upo.department_scope_type = N'department' AND upo.department_id = @context_department_id)
                OR (upo.department_scope_type = N'department_tree' AND EXISTS (SELECT 1 FROM med.department_closure dc WHERE dc.ancestor_department_id = upo.department_id AND dc.descendant_department_id = @context_department_id))
                OR (upo.department_scope_type = N'own_department' AND u.primary_department_id = @context_department_id)
          )
    ), winner AS (
        SELECT TOP (1) effect_code
        FROM active_sources
        ORDER BY priority DESC,
                 CASE WHEN effect_code = N'deny' THEN 1 ELSE 0 END DESC,
                 source_rank DESC,
                 effective_from DESC
    )
    SELECT @result = CASE WHEN effect_code = N'allow' THEN 1 ELSE 0 END
    FROM winner;

    RETURN @result;
END;
GO

CREATE VIEW med.vw_permission_change_report AS
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

CREATE VIEW med.vw_resource_consumption_variance AS
SELECT
    o.technical_order_id,
    pr.external_patient_id,
    er.external_encounter_id,
    ts.service_code,
    ts.name AS service_name,
    rc.resource_type,
    rc.resource_code,
    rc.name AS resource_name,
    norm.standard_quantity,
    usage.actual_quantity,
    usage.actual_quantity - norm.standard_quantity AS variance_quantity,
    usage.unit_code,
    usage.variance_reason,
    usage.captured_at
FROM med.actual_resource_usages usage
JOIN med.technical_orders o ON o.technical_order_id = usage.technical_order_id
JOIN med.technical_services ts ON ts.technical_service_id = o.technical_service_id
JOIN med.resource_catalog rc ON rc.resource_id = usage.resource_id
LEFT JOIN med.technical_resource_norms norm
    ON norm.technical_service_id = ts.technical_service_id
   AND norm.resource_id = usage.resource_id
LEFT JOIN med.patient_refs pr ON pr.patient_ref_id = o.patient_ref_id
LEFT JOIN med.encounter_refs er ON er.encounter_ref_id = o.encounter_ref_id;
GO

/* ============================================================
   14. SEED BASE SCREENS, FEATURES, PERMISSIONS
   ============================================================ */

INSERT INTO med.screen_catalog (screen_code, name, route, module_code)
VALUES
(N'ADMIN_PERMISSIONS', N'Quản trị phân quyền', N'/admin/permissions', N'ADMIN'),
(N'PROCEDURE_MANAGEMENT', N'Quản lý quy trình', N'/procedures', N'PROCEDURE'),
(N'TECHNICAL_CATALOG', N'Danh mục kỹ thuật', N'/technical-catalog', N'TECHNICAL'),
(N'CLINICAL_PROTOCOLS', N'Phác đồ lâm sàng', N'/clinical-protocols', N'CLINICAL'),
(N'REPORTS', N'Báo cáo', N'/reports', N'REPORT');
GO

INSERT INTO med.feature_catalog (screen_id, feature_code, name)
SELECT screen_id, CONCAT(screen_code, N'_MAIN'), name
FROM med.screen_catalog;
GO

INSERT INTO med.permissions (permission_code, screen_id, feature_id, action_code, description)
SELECT CONCAT(sc.screen_code, N':VIEW'), sc.screen_id, fc.feature_id, N'view', N'Xem màn hình'
FROM med.screen_catalog sc
JOIN med.feature_catalog fc ON fc.screen_id = sc.screen_id AND fc.feature_code = CONCAT(sc.screen_code, N'_MAIN');
GO

INSERT INTO med.permissions (permission_code, screen_id, feature_id, action_code, description)
SELECT CONCAT(sc.screen_code, N':CONFIGURE'), sc.screen_id, fc.feature_id, N'configure', N'Cấu hình dữ liệu'
FROM med.screen_catalog sc
JOIN med.feature_catalog fc ON fc.screen_id = sc.screen_id AND fc.feature_code = CONCAT(sc.screen_code, N'_MAIN')
WHERE sc.screen_code IN (N'ADMIN_PERMISSIONS', N'PROCEDURE_MANAGEMENT', N'TECHNICAL_CATALOG', N'CLINICAL_PROTOCOLS');
GO

/* ============================================================
   15. OPTIONAL BASE ROLES
   ============================================================ */

INSERT INTO med.roles (code, name, description, is_system)
VALUES
(N'SYSTEM_ADMIN', N'Quản trị hệ thống', N'Toàn quyền quản trị hệ thống', 1),
(N'DEPARTMENT_ADMIN', N'Quản trị khoa/phòng', N'Quản trị trong phạm vi khoa/phòng', 1),
(N'CLINICAL_USER', N'Người dùng lâm sàng', N'Người dùng lâm sàng', 1),
(N'REPORT_VIEWER', N'Người xem báo cáo', N'Người xem báo cáo', 1);
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

/* ============================================================
   17. V3 HARDENING PATCH
   ============================================================ */

/*
   17.1 GUID default hardening
   NEWID() creates random inserts and page splits on clustered GUID PKs.
   For new rows, use NEWSEQUENTIALID() for most entity tables.
   For very high-write audit_logs, add a sequential clustered key.
*/

ALTER TABLE med.departments DROP CONSTRAINT DF_departments_id;
ALTER TABLE med.departments ADD CONSTRAINT DF_departments_id DEFAULT NEWSEQUENTIALID() FOR department_id;
GO
ALTER TABLE med.users DROP CONSTRAINT DF_users_id;
ALTER TABLE med.users ADD CONSTRAINT DF_users_id DEFAULT NEWSEQUENTIALID() FOR user_id;
GO
ALTER TABLE med.roles DROP CONSTRAINT DF_roles_id;
ALTER TABLE med.roles ADD CONSTRAINT DF_roles_id DEFAULT NEWSEQUENTIALID() FOR role_id;
GO
ALTER TABLE med.groups DROP CONSTRAINT DF_groups_id;
ALTER TABLE med.groups ADD CONSTRAINT DF_groups_id DEFAULT NEWSEQUENTIALID() FOR group_id;
GO
ALTER TABLE med.user_roles DROP CONSTRAINT DF_user_roles_id;
ALTER TABLE med.user_roles ADD CONSTRAINT DF_user_roles_id DEFAULT NEWSEQUENTIALID() FOR user_role_id;
GO
ALTER TABLE med.user_group_members DROP CONSTRAINT DF_user_group_members_id;
ALTER TABLE med.user_group_members ADD CONSTRAINT DF_user_group_members_id DEFAULT NEWSEQUENTIALID() FOR user_group_member_id;
GO
ALTER TABLE med.permissions DROP CONSTRAINT DF_permissions_id;
ALTER TABLE med.permissions ADD CONSTRAINT DF_permissions_id DEFAULT NEWSEQUENTIALID() FOR permission_id;
GO
ALTER TABLE med.role_permissions DROP CONSTRAINT DF_role_permissions_id;
ALTER TABLE med.role_permissions ADD CONSTRAINT DF_role_permissions_id DEFAULT NEWSEQUENTIALID() FOR role_permission_id;
GO
ALTER TABLE med.group_permissions DROP CONSTRAINT DF_group_permissions_id;
ALTER TABLE med.group_permissions ADD CONSTRAINT DF_group_permissions_id DEFAULT NEWSEQUENTIALID() FOR group_permission_id;
GO
ALTER TABLE med.user_permission_overrides DROP CONSTRAINT DF_user_permission_overrides_id;
ALTER TABLE med.user_permission_overrides ADD CONSTRAINT DF_user_permission_overrides_id DEFAULT NEWSEQUENTIALID() FOR user_permission_override_id;
GO
ALTER TABLE med.audit_logs DROP CONSTRAINT DF_audit_logs_id;
ALTER TABLE med.audit_logs ADD CONSTRAINT DF_audit_logs_id DEFAULT NEWSEQUENTIALID() FOR audit_log_id;
GO

IF COL_LENGTH('med.audit_logs', 'audit_log_seq') IS NULL
BEGIN
    ALTER TABLE med.audit_logs ADD audit_log_seq BIGINT IDENTITY(1,1) NOT NULL;
END;
GO

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'PK_audit_logs' AND parent_object_id = OBJECT_ID(N'med.audit_logs'))
BEGIN
    ALTER TABLE med.audit_logs DROP CONSTRAINT PK_audit_logs;
END;
GO

ALTER TABLE med.audit_logs ADD CONSTRAINT PK_audit_logs PRIMARY KEY NONCLUSTERED (audit_log_id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'CX_audit_logs_seq' AND object_id = OBJECT_ID(N'med.audit_logs'))
BEGIN
    CREATE UNIQUE CLUSTERED INDEX CX_audit_logs_seq ON med.audit_logs(audit_log_seq);
END;
GO

/*
   17.2 Department closure incremental maintenance
   - No full rebuild on every department name/status update.
   - INSERT: add self path and parent ancestor paths only.
   - UPDATE parent_department_id: rebuild only affected subtree paths.
   - DELETE is intentionally not supported for departments; use status='archived'.
*/

IF OBJECT_ID(N'med.TR_departments_rebuild_closure', N'TR') IS NOT NULL
    DROP TRIGGER med.TR_departments_rebuild_closure;
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

    IF NOT UPDATE(parent_department_id)
        RETURN;

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

/*
   17.3 Replace scalar permission resolver with inline table-valued function.
   Usage:
       SELECT has_permission
       FROM med.fn_user_has_permission_itvf(@user_id, @permission_id, @department_id);
*/

IF OBJECT_ID(N'med.fn_user_has_permission', N'FN') IS NOT NULL
    DROP FUNCTION med.fn_user_has_permission;
GO

CREATE OR ALTER FUNCTION med.fn_user_has_permission_itvf (
    @user_id UNIQUEIDENTIFIER,
    @permission_id UNIQUEIDENTIFIER,
    @context_department_id UNIQUEIDENTIFIER = NULL
)
RETURNS TABLE
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

/*
   17.4 Batch scheduled permission workflow.
   Each request is applied in its own transaction. Failed requests do not block others.
*/

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

/*
   17.5 Final usage guard and variance view fix.
*/

CREATE UNIQUE INDEX UX_actual_resource_usages_final
ON med.actual_resource_usages(technical_order_id, resource_id)
WHERE is_final = 1;
GO

CREATE OR ALTER VIEW med.vw_resource_consumption_variance AS
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

/*
   17.6 Remove ambiguous professional_procedures type at data-entry level.
   Existing rows are not deleted. This tightens future data.
*/

ALTER TABLE med.professional_procedures DROP CONSTRAINT CK_professional_procedures_type;
GO
ALTER TABLE med.professional_procedures ADD CONSTRAINT CK_professional_procedures_type
CHECK (procedure_type IN (N'technical', N'care', N'surgery', N'procedure'));
GO

/*
   17.7 Recreate user soft-delete trigger after all dependent tables exist.
*/

IF OBJECT_ID(N'med.TR_users_expire_security_assignments', N'TR') IS NOT NULL
    DROP TRIGGER med.TR_users_expire_security_assignments;
GO

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

/*
   17.8 Production note for audit partitioning.
   For high-volume deployments, put audit_logs on a monthly partition scheme.
   The exact partition boundary list should be generated by deployment automation.
*/

PRINT N'Database MedicalProcedureManagement schema created successfully. V3 hardening patch included.';
GO

