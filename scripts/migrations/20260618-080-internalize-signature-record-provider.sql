USE MedicalProcedureManagement;
GO

IF OBJECT_ID(N'med.signature_records', N'U') IS NOT NULL
BEGIN
    IF OBJECT_ID(N'med.DF_signature_records_provider', N'D') IS NOT NULL
        ALTER TABLE med.signature_records DROP CONSTRAINT DF_signature_records_provider;

    ALTER TABLE med.signature_records
        ADD CONSTRAINT DF_signature_records_provider DEFAULT N'internal' FOR provider_code;
END
GO
