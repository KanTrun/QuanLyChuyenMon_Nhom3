USE MedicalProcedureManagement;
GO

SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF OBJECT_ID(N'med.lookup_user_onboarding_status', N'U') IS NULL
BEGIN
    CREATE TABLE med.lookup_user_onboarding_status (
        code NVARCHAR(30) NOT NULL CONSTRAINT PK_lookup_user_onboarding_status PRIMARY KEY,
        name NVARCHAR(100) NOT NULL
    );
END;

MERGE med.lookup_user_onboarding_status AS target
USING (VALUES
    (N'submitted', N'Chờ duyệt'),
    (N'active', N'Đang hoạt động'),
    (N'rejected', N'Bị từ chối'),
    (N'inactive', N'Ngừng hoạt động')
) AS source(code, name)
ON target.code = source.code
WHEN MATCHED THEN UPDATE SET name = source.name
WHEN NOT MATCHED THEN INSERT (code, name) VALUES (source.code, source.name);

IF COL_LENGTH(N'med.users', N'onboarding_status') IS NULL
BEGIN
    ALTER TABLE med.users
        ADD onboarding_status NVARCHAR(30) NOT NULL
            CONSTRAINT DF_users_onboarding_status DEFAULT N'inactive';
END;

EXEC(N'
UPDATE med.users
SET onboarding_status =
    CASE
        WHEN status = N''active'' THEN N''active''
        WHEN deleted_at IS NOT NULL THEN N''inactive''
        WHEN onboarding_status IN (N''submitted'', N''rejected'') THEN onboarding_status
        WHEN status = N''inactive'' THEN N''inactive''
        ELSE N''inactive''
    END
WHERE onboarding_status IS NULL
   OR onboarding_status NOT IN (N''submitted'', N''active'', N''rejected'', N''inactive'')
   OR (status = N''active'' AND onboarding_status <> N''active'')
   OR (deleted_at IS NOT NULL AND onboarding_status <> N''inactive'')
   OR (status = N''inactive'' AND onboarding_status NOT IN (N''submitted'', N''rejected'', N''inactive''));
');

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_users_onboarding_status'
      AND parent_object_id = OBJECT_ID(N'med.users')
)
BEGIN
    EXEC(N'
    ALTER TABLE med.users WITH CHECK
        ADD CONSTRAINT FK_users_onboarding_status
        FOREIGN KEY (onboarding_status)
        REFERENCES med.lookup_user_onboarding_status(code);
    ');
END;

COMMIT TRANSACTION;
GO
