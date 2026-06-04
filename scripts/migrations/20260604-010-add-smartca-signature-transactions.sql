USE MedicalProcedureManagement;
GO

SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF OBJECT_ID(N'med.signature_transactions', N'U') IS NULL
BEGIN
    CREATE TABLE med.signature_transactions (
        signature_transaction_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_signature_transactions_id DEFAULT NEWID(),
        target_type NVARCHAR(64) NOT NULL,
        target_id UNIQUEIDENTIFIER NOT NULL,
        signer_user_id UNIQUEIDENTIFIER NOT NULL,
        signer_username NVARCHAR(256) NULL,
        provider_code NVARCHAR(32) NOT NULL,
        environment_code NVARCHAR(32) NOT NULL CONSTRAINT DF_signature_transactions_environment DEFAULT N'sandbox',
        external_transaction_id NVARCHAR(128) NULL,
        external_transaction_code NVARCHAR(128) NULL,
        document_id NVARCHAR(128) NOT NULL,
        document_hash NVARCHAR(128) NOT NULL,
        ca_subscriber_id NVARCHAR(128) NULL,
        requested_certificate_serial NVARCHAR(256) NULL,
        transaction_status NVARCHAR(64) NOT NULL CONSTRAINT DF_signature_transactions_status DEFAULT N'created',
        status_message NVARCHAR(512) NULL,
        requested_at DATETIME2(3) NOT NULL CONSTRAINT DF_signature_transactions_requested_at DEFAULT SYSUTCDATETIME(),
        updated_at DATETIME2(3) NOT NULL CONSTRAINT DF_signature_transactions_updated_at DEFAULT SYSUTCDATETIME(),
        completed_at DATETIME2(3) NULL,
        certificate_subject NVARCHAR(512) NULL,
        certificate_serial NVARCHAR(256) NULL,
        certificate_expiry DATETIME2(3) NULL,
        metadata_json NVARCHAR(MAX) NULL,
        correlation_id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_signature_transactions_correlation DEFAULT NEWID(),
        CONSTRAINT PK_signature_transactions PRIMARY KEY (signature_transaction_id),
        CONSTRAINT FK_signature_transactions_signer FOREIGN KEY (signer_user_id) REFERENCES med.users(user_id),
        CONSTRAINT FK_signature_transactions_patient_protocol_application FOREIGN KEY (target_id) REFERENCES med.patient_protocol_applications(patient_protocol_application_id),
        CONSTRAINT CK_signature_transactions_target_type CHECK (target_type = N'patient_protocol_application'),
        CONSTRAINT CK_signature_transactions_metadata_json CHECK (metadata_json IS NULL OR ISJSON(metadata_json) = 1),
        CONSTRAINT CK_signature_transactions_status CHECK (transaction_status IN (N'created', N'waiting', N'signed', N'rejected', N'expired', N'failed', N'unknown'))
    );
END;

IF COL_LENGTH(N'med.signature_transactions', N'ca_subscriber_id') IS NULL
    ALTER TABLE med.signature_transactions ADD ca_subscriber_id NVARCHAR(128) NULL;

IF COL_LENGTH(N'med.signature_transactions', N'requested_certificate_serial') IS NULL
    ALTER TABLE med.signature_transactions ADD requested_certificate_serial NVARCHAR(256) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_signature_transactions_target' AND object_id = OBJECT_ID(N'med.signature_transactions'))
    CREATE INDEX IX_signature_transactions_target ON med.signature_transactions(target_type, target_id, requested_at DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_signature_transactions_external_code' AND object_id = OBJECT_ID(N'med.signature_transactions'))
    CREATE INDEX IX_signature_transactions_external_code ON med.signature_transactions(external_transaction_code);

COMMIT TRANSACTION;
GO
