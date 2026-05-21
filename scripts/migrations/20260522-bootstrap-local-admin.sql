USE MedicalProcedureManagement;
GO

DECLARE @AdminPasswordHash NVARCHAR(128) = N'a36aef5a11c4073fbe60314fc9df530a9d5f986533594d1f5190742ff9e0e408';
DECLARE @RootDepartmentId UNIQUEIDENTIFIER = (
    SELECT TOP (1) department_id
    FROM med.departments
    WHERE code = N'BV-ROOT'
);
DECLARE @SystemAdminRoleId UNIQUEIDENTIFIER = (
    SELECT TOP (1) role_id
    FROM med.roles
    WHERE code = N'SYSTEM_ADMIN'
);
DECLARE @AdminUserId UNIQUEIDENTIFIER = (
    SELECT TOP (1) user_id
    FROM med.users
    WHERE username = N'admin'
);

IF @AdminUserId IS NULL
BEGIN
    SET @AdminUserId = NEWID();

    INSERT INTO med.users (
        user_id,
        username,
        email,
        full_name,
        primary_department_id,
        status,
        password_hash
    )
    VALUES (
        @AdminUserId,
        N'admin',
        N'admin@benhvien.vn',
        N'Quan tri vien he thong',
        @RootDepartmentId,
        N'active',
        @AdminPasswordHash
    );
END
ELSE
BEGIN
    UPDATE med.users
    SET
        status = N'active',
        deleted_at = NULL,
        password_hash = CASE
            WHEN password_hash IS NULL OR LTRIM(RTRIM(password_hash)) = N'' THEN @AdminPasswordHash
            ELSE password_hash
        END,
        updated_at = SYSUTCDATETIME()
    WHERE user_id = @AdminUserId;
END

IF @SystemAdminRoleId IS NOT NULL
    AND NOT EXISTS (
        SELECT 1
        FROM med.user_roles
        WHERE user_id = @AdminUserId
          AND role_id = @SystemAdminRoleId
          AND effective_to IS NULL
    )
BEGIN
    INSERT INTO med.user_roles (user_id, role_id)
    VALUES (@AdminUserId, @SystemAdminRoleId);
END
GO
