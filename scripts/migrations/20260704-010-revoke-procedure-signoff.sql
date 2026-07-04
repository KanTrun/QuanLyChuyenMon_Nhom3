-- Migration: 20260704-010-revoke-procedure-signoff
-- Thêm khả năng thu hồi / hủy chữ ký trên bản ghi ký nội bộ quy trình.
-- Chữ ký bị thu hồi vẫn được lưu trữ để duy trì audit trail đầy đủ;
-- chỉ cột is_revoked = 1 đánh dấu chúng không còn hiệu lực nghiệp vụ.

USE MedicalProcedureManagement;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'med.procedure_signoff_records')
      AND name = N'is_revoked'
)
BEGIN
    ALTER TABLE med.procedure_signoff_records
        ADD is_revoked          BIT           NOT NULL DEFAULT 0,
            revoked_at          DATETIME2     NULL,
            revoked_by_user_id  UNIQUEIDENTIFIER NULL,
            revoke_reason       NVARCHAR(1000) NULL;

    PRINT N'Đã thêm cột is_revoked, revoked_at, revoked_by_user_id, revoke_reason vào med.procedure_signoff_records.';
END
ELSE
BEGIN
    PRINT N'Cột is_revoked đã tồn tại — bỏ qua.';
END;
GO
