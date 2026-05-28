USE MedicalProcedureManagement;
GO

SET XACT_ABORT ON;

BEGIN TRANSACTION;

MERGE med.lookup_action_codes AS target
USING (VALUES (N'sign', N'Ký xác nhận', 90, 1, N'Ký xác nhận điện tử'))
    AS source(action_code, name, display_order, is_active, description)
ON target.action_code = source.action_code
WHEN MATCHED THEN UPDATE SET name = source.name, display_order = source.display_order, is_active = source.is_active, description = source.description
WHEN NOT MATCHED THEN
    INSERT (action_code, name, display_order, is_active, description)
    VALUES (source.action_code, source.name, source.display_order, source.is_active, source.description);

MERGE med.lookup_protocol_application_statuses AS target
USING (VALUES
    (N'draft', N'Bản nháp', 5, 1, N'Hồ sơ đang soạn'),
    (N'signed', N'Đã ký', 30, 1, N'Hồ sơ đã ký demo'),
    (N'revoked', N'Đã thu hồi', 40, 1, N'Hồ sơ đã thu hồi ký xác nhận')
) AS source(application_status, name, display_order, is_active, description)
ON target.application_status = source.application_status
WHEN MATCHED THEN UPDATE SET name = source.name, display_order = source.display_order, is_active = source.is_active, description = source.description
WHEN NOT MATCHED THEN
    INSERT (application_status, name, display_order, is_active, description)
    VALUES (source.application_status, source.name, source.display_order, source.is_active, source.description);

IF OBJECT_ID(N'med.signature_records', N'U') IS NULL
BEGIN
    CREATE TABLE med.signature_records (
        signature_record_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_signature_records_id DEFAULT NEWID(),
        target_type NVARCHAR(64) NOT NULL,
        target_id UNIQUEIDENTIFIER NOT NULL,
        signer_user_id UNIQUEIDENTIFIER NOT NULL,
        signer_username NVARCHAR(256) NULL,
        provider_code NVARCHAR(32) NOT NULL CONSTRAINT DF_signature_records_provider DEFAULT N'demo',
        is_legally_valid BIT NOT NULL CONSTRAINT DF_signature_records_legal DEFAULT 0,
        signature_hash NVARCHAR(128) NOT NULL,
        signed_at DATETIME2(3) NOT NULL CONSTRAINT DF_signature_records_signed_at DEFAULT SYSUTCDATETIME(),
        certificate_subject NVARCHAR(512) NULL,
        certificate_serial NVARCHAR(256) NULL,
        certificate_expiry DATETIME2(3) NULL,
        metadata_json NVARCHAR(MAX) NULL,
        correlation_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_signature_records_correlation DEFAULT NEWID(),
        CONSTRAINT PK_signature_records PRIMARY KEY (signature_record_id),
        CONSTRAINT FK_signature_records_signer FOREIGN KEY (signer_user_id) REFERENCES med.users(user_id),
        CONSTRAINT FK_signature_records_patient_protocol_application FOREIGN KEY (target_id) REFERENCES med.patient_protocol_applications(patient_protocol_application_id),
        CONSTRAINT UQ_signature_records_target UNIQUE (target_type, target_id),
        CONSTRAINT CK_signature_records_target_type CHECK (target_type = N'patient_protocol_application'),
        CONSTRAINT CK_signature_records_metadata_json CHECK (metadata_json IS NULL OR ISJSON(metadata_json) = 1)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_signature_records_target' AND object_id = OBJECT_ID(N'med.signature_records'))
    CREATE INDEX IX_signature_records_target ON med.signature_records(target_type, target_id);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_signature_records_signer' AND object_id = OBJECT_ID(N'med.signature_records'))
    CREATE INDEX IX_signature_records_signer ON med.signature_records(signer_user_id, signed_at DESC);

EXEC(N'
CREATE OR ALTER TRIGGER med.TR_signature_records_immutable
ON med.signature_records
AFTER UPDATE, DELETE
AS
BEGIN
    RAISERROR(''Signature records are immutable. Use revocation workflow.'', 16, 1);
    ROLLBACK;
END;
');

DECLARE @clinical_screen_id UNIQUEIDENTIFIER =
    (SELECT TOP (1) screen_id FROM med.screen_catalog WHERE screen_code = N'SCR_CLINICAL');
DECLARE @sign_feature_id UNIQUEIDENTIFIER;
DECLARE @sign_permission_id UNIQUEIDENTIFIER;

IF @clinical_screen_id IS NOT NULL
BEGIN
    MERGE med.feature_catalog AS target
    USING (VALUES (@clinical_screen_id, N'FEAT_CLINICAL_SIGN_PROTOCOL_APPLICATION', N'Ký xác nhận hồ sơ phác đồ'))
        AS source(screen_id, feature_code, name)
    ON target.feature_code = source.feature_code
    WHEN MATCHED THEN UPDATE SET screen_id = source.screen_id, name = source.name, status = N'active', updated_at = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (screen_id, feature_code, name)
        VALUES (source.screen_id, source.feature_code, source.name);

    SELECT @sign_feature_id = feature_id
    FROM med.feature_catalog
    WHERE feature_code = N'FEAT_CLINICAL_SIGN_PROTOCOL_APPLICATION';

    MERGE med.permissions AS target
    USING (VALUES (N'SCR_CLINICAL:SIGN_PROTOCOL_APPLICATION', @clinical_screen_id, @sign_feature_id, N'sign', N'Ký xác nhận hồ sơ áp dụng phác đồ'))
        AS source(permission_code, screen_id, feature_id, action_code, description)
    ON target.permission_code = source.permission_code
    WHEN MATCHED THEN UPDATE SET screen_id = source.screen_id, feature_id = source.feature_id, action_code = source.action_code, description = source.description, status = N'active', updated_at = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (permission_code, screen_id, feature_id, action_code, description)
        VALUES (source.permission_code, source.screen_id, source.feature_id, source.action_code, source.description);

    SELECT @sign_permission_id = permission_id
    FROM med.permissions
    WHERE permission_code = N'SCR_CLINICAL:SIGN_PROTOCOL_APPLICATION';

    INSERT INTO med.role_permissions (role_id, permission_id, effect_code, department_scope_type, priority, reason, created_by)
    SELECT r.role_id, @sign_permission_id, N'allow', N'global', 100, N'Seed quyền ký xác nhận demo', admin_user.user_id
    FROM med.roles r
    CROSS APPLY (SELECT TOP (1) user_id FROM med.users WHERE username = N'admin') admin_user
    WHERE @sign_permission_id IS NOT NULL
      AND r.code IN (N'SYSTEM_ADMIN', N'DEPARTMENT_ADMIN', N'CLINICAL_USER', N'DOCTOR')
      AND NOT EXISTS (
          SELECT 1
          FROM med.role_permissions rp
          WHERE rp.role_id = r.role_id
            AND rp.permission_id = @sign_permission_id
            AND rp.effective_to IS NULL
      );
END;

DECLARE @admin_screen_id UNIQUEIDENTIFIER;
DECLARE @manage_feature_id UNIQUEIDENTIFIER;
DECLARE @manage_permission_id UNIQUEIDENTIFIER;

MERGE med.screen_catalog AS target
USING (VALUES (N'SCR_ADMIN', N'Quan tri he thong', N'/admin', N'CORE'))
    AS source(screen_code, name, route, module_code)
ON target.screen_code = source.screen_code
WHEN MATCHED THEN UPDATE SET name = source.name, route = source.route, module_code = source.module_code, status = N'active', updated_at = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (screen_code, name, route, module_code)
    VALUES (source.screen_code, source.name, source.route, source.module_code);

SELECT @admin_screen_id = screen_id
FROM med.screen_catalog
WHERE screen_code = N'SCR_ADMIN';

MERGE med.feature_catalog AS target
USING (VALUES (@admin_screen_id, N'FEAT_ADMIN_MANAGE_SIGNATURES', N'Quản lý thu hồi chữ ký'))
    AS source(screen_id, feature_code, name)
ON target.feature_code = source.feature_code
WHEN MATCHED THEN UPDATE SET screen_id = source.screen_id, name = source.name, status = N'active', updated_at = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (screen_id, feature_code, name)
    VALUES (source.screen_id, source.feature_code, source.name);

SELECT @manage_feature_id = feature_id
FROM med.feature_catalog
WHERE feature_code = N'FEAT_ADMIN_MANAGE_SIGNATURES';

MERGE med.permissions AS target
USING (VALUES (N'SCR_ADMIN:MANAGE_SIGNATURES', @admin_screen_id, @manage_feature_id, N'update', N'Thu hồi chữ ký demo'))
    AS source(permission_code, screen_id, feature_id, action_code, description)
ON target.permission_code = source.permission_code
WHEN MATCHED THEN UPDATE SET screen_id = source.screen_id, feature_id = source.feature_id, action_code = source.action_code, description = source.description, status = N'active', updated_at = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (permission_code, screen_id, feature_id, action_code, description)
    VALUES (source.permission_code, source.screen_id, source.feature_id, source.action_code, source.description);

SELECT @manage_permission_id = permission_id
FROM med.permissions
WHERE permission_code = N'SCR_ADMIN:MANAGE_SIGNATURES';

INSERT INTO med.role_permissions (role_id, permission_id, effect_code, department_scope_type, priority, reason, created_by)
SELECT r.role_id, @manage_permission_id, N'allow', N'global', 100, N'Seed quyền thu hồi chữ ký demo', admin_user.user_id
FROM med.roles r
CROSS APPLY (SELECT TOP (1) user_id FROM med.users WHERE username = N'admin') admin_user
WHERE @manage_permission_id IS NOT NULL
  AND r.code = N'SYSTEM_ADMIN'
  AND NOT EXISTS (
      SELECT 1
      FROM med.role_permissions rp
      WHERE rp.role_id = r.role_id
        AND rp.permission_id = @manage_permission_id
        AND rp.effective_to IS NULL
  );

COMMIT TRANSACTION;
GO
