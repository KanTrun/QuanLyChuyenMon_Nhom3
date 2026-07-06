-- Migration: 20260705-030-add-revision-no
-- Thêm cột revision_no vào med.procedure_versions để hỗ trợ số phiên bản phụ (v01.1, v01.2, v02.0...).
-- revision_no = 0 : bản gốc (v01, v02, ...)
-- revision_no > 0 : bản sửa (v01.1, v01.2 sau mỗi lần hoàn trả và sửa lại)

USE MedicalProcedureManagement;
GO

IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'med'
      AND TABLE_NAME   = 'procedure_versions'
      AND COLUMN_NAME  = 'revision_no'
)
BEGIN
    ALTER TABLE med.procedure_versions
        ADD revision_no INT NOT NULL DEFAULT 0;
    PRINT N'Đã thêm cột revision_no vào med.procedure_versions.';
END
ELSE
BEGIN
    PRINT N'Cột revision_no đã tồn tại — bỏ qua.';
END;
GO
