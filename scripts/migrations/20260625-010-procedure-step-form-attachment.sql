USE MedicalProcedureManagement;
GO

/* Link procedure flow steps to version-level attachments (form/guideline per step). */
IF COL_LENGTH('med.procedure_steps', 'form_attachment_id') IS NULL
BEGIN
    ALTER TABLE med.procedure_steps
        ADD form_attachment_id UNIQUEIDENTIFIER NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_procedure_steps_form_attachment'
)
BEGIN
    ALTER TABLE med.procedure_steps
        ADD CONSTRAINT FK_procedure_steps_form_attachment
            FOREIGN KEY (form_attachment_id)
            REFERENCES med.procedure_attachments (procedure_attachment_id)
            ON DELETE SET NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_procedure_steps_form_attachment'
      AND object_id = OBJECT_ID(N'med.procedure_steps')
)
BEGIN
    CREATE INDEX IX_procedure_steps_form_attachment
        ON med.procedure_steps (form_attachment_id)
        WHERE form_attachment_id IS NOT NULL;
END
GO
