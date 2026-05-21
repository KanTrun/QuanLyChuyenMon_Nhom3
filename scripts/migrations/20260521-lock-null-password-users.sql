USE MedicalProcedureManagement;
GO

UPDATE med.users
SET
    status = N'inactive',
    updated_at = SYSUTCDATETIME()
WHERE
    status = N'active'
    AND deleted_at IS NULL
    AND (password_hash IS NULL OR LTRIM(RTRIM(password_hash)) = N'');
GO
