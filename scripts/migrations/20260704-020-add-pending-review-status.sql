-- Migration: 20260704-020-add-pending-review-status
-- Thêm trạng thái "chờ kiểm tra" (pending_review) vào lookup_version_statuses.
-- Luồng mới chuẩn đa cấp:
--   draft → pending_review → pending_approval → active
--   (Tất cả người viết ký xong → chờ kiểm tra → người kiểm tra ký → chờ phê duyệt → người phê duyệt ký & ban hành)

USE MedicalProcedureManagement;
GO

IF NOT EXISTS (
    SELECT 1
    FROM med.lookup_version_statuses
    WHERE status_code = N'pending_review'
)
BEGIN
    INSERT INTO med.lookup_version_statuses (status_code, name, display_order, description)
    VALUES (
        N'pending_review',
        N'Chờ kiểm tra',
        25,
        N'Tất cả người viết đã ký xác nhận, đang chờ người kiểm tra xem xét và ký'
    );
    PRINT N'Đã thêm trạng thái pending_review vào lookup_version_statuses.';
END
ELSE
BEGIN
    PRINT N'Trạng thái pending_review đã tồn tại — bỏ qua.';
END;
GO
