USE MedicalProcedureManagement;
GO

IF COL_LENGTH(N'med.users', N'active_session_id') IS NULL
BEGIN
    ALTER TABLE med.users
        ADD active_session_id UNIQUEIDENTIFIER NULL;
END
GO
