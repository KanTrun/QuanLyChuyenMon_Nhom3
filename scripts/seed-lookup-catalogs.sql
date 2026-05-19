/* ============================================================
   Complete lookup catalogs for MedicalProcedureManagement.
   Run after MedicalProcedureManagement.sql and before demo data.
   ============================================================ */

USE MedicalProcedureManagement;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;

MERGE med.lookup_record_status AS target
USING (VALUES
    (N'active', N'Đang hoạt động', 10, N'Bản ghi đang được sử dụng'),
    (N'inactive', N'Ngừng hoạt động', 20, N'Tạm ngừng sử dụng nhưng còn giữ lịch sử'),
    (N'archived', N'Lưu trữ', 30, N'Không còn dùng trong tác nghiệp thường ngày')
) AS src(code, name, display_order, description)
ON target.code = src.code
WHEN MATCHED THEN UPDATE SET name = src.name, display_order = src.display_order, description = src.description, is_active = 1
WHEN NOT MATCHED THEN INSERT (code, name, display_order, description) VALUES (src.code, src.name, src.display_order, src.description);

MERGE med.lookup_action_codes AS target
USING (VALUES
    (N'view', N'Xem', 10, N'Xem dữ liệu'),
    (N'create', N'Tạo mới', 20, N'Tạo bản ghi mới'),
    (N'update', N'Cập nhật', 30, N'Sửa bản ghi hiện có'),
    (N'delete', N'Xóa', 40, N'Xóa hoặc lưu trữ bản ghi'),
    (N'approve', N'Phê duyệt', 50, N'Duyệt nghiệp vụ'),
    (N'publish', N'Ban hành', 60, N'Ban hành phiên bản'),
    (N'execute', N'Thực hiện', 70, N'Thực hiện tác vụ chuyên môn'),
    (N'export', N'Xuất dữ liệu', 80, N'Xuất báo cáo hoặc danh sách'),
    (N'configure', N'Cấu hình', 90, N'Cấu hình hệ thống')
) AS src(action_code, name, display_order, description)
ON target.action_code = src.action_code
WHEN MATCHED THEN UPDATE SET name = src.name, display_order = src.display_order, description = src.description, is_active = 1
WHEN NOT MATCHED THEN INSERT (action_code, name, display_order, description) VALUES (src.action_code, src.name, src.display_order, src.description);

MERGE med.lookup_genders AS target
USING (VALUES
    (N'male', N'Nam', 10, N'Giới tính nam'),
    (N'female', N'Nữ', 20, N'Giới tính nữ'),
    (N'other', N'Khác', 30, N'Giới tính khác hoặc không thuộc nhóm nhị phân'),
    (N'unknown', N'Không xác định', 40, N'Chưa có thông tin giới tính')
) AS src(gender_code, name, display_order, description)
ON target.gender_code = src.gender_code
WHEN MATCHED THEN UPDATE SET name = src.name, display_order = src.display_order, description = src.description, is_active = 1
WHEN NOT MATCHED THEN INSERT (gender_code, name, display_order, description) VALUES (src.gender_code, src.name, src.display_order, src.description);

MERGE med.lookup_version_statuses AS target
USING (VALUES
    (N'draft', N'Bản nháp', 10, N'Đang soạn thảo'),
    (N'pending_approval', N'Chờ phê duyệt', 20, N'Đã gửi lên hội đồng hoặc người duyệt'),
    (N'active', N'Đang hiệu lực', 30, N'Phiên bản đang áp dụng'),
    (N'superseded', N'Đã được thay thế', 40, N'Đã có phiên bản mới hơn thay thế'),
    (N'archived', N'Lưu trữ', 50, N'Không còn áp dụng'),
    (N'rejected', N'Bị từ chối', 60, N'Không được duyệt')
) AS src(status_code, name, display_order, description)
ON target.status_code = src.status_code
WHEN MATCHED THEN UPDATE SET name = src.name, display_order = src.display_order, description = src.description, is_active = 1
WHEN NOT MATCHED THEN INSERT (status_code, name, display_order, description) VALUES (src.status_code, src.name, src.display_order, src.description);

MERGE med.lookup_order_statuses AS target
USING (VALUES
    (N'ordered', N'Đã chỉ định', 10, N'Bác sĩ đã tạo chỉ định'),
    (N'resource_warning', N'Cảnh báo nguồn lực', 20, N'Có thiếu hụt hoặc cảnh báo vật tư/thiết bị'),
    (N'scheduled', N'Đã lên lịch', 30, N'Đã có lịch thực hiện'),
    (N'in_progress', N'Đang thực hiện', 40, N'Kỹ thuật đang được thực hiện'),
    (N'completed', N'Hoàn thành', 50, N'Đã hoàn tất và ghi nhận tiêu hao'),
    (N'cancelled', N'Đã hủy', 60, N'Chỉ định bị hủy')
) AS src(order_status, name, display_order, description)
ON target.order_status = src.order_status
WHEN MATCHED THEN UPDATE SET name = src.name, display_order = src.display_order, description = src.description, is_active = 1
WHEN NOT MATCHED THEN INSERT (order_status, name, display_order, description) VALUES (src.order_status, src.name, src.display_order, src.description);

MERGE med.lookup_resource_types AS target
USING (VALUES
    (N'supply', N'Vật tư tiêu hao', 10, N'Vật tư dùng một lần hoặc tiêu hao theo ca'),
    (N'equipment', N'Thiết bị', 20, N'Thiết bị, máy móc, phòng chức năng'),
    (N'drug', N'Thuốc', 30, N'Thuốc dùng trong điều trị hoặc kỹ thuật'),
    (N'chemical', N'Hóa chất', 40, N'Hóa chất xét nghiệm, sát khuẩn hoặc xử lý mẫu')
) AS src(resource_type, name, display_order, description)
ON target.resource_type = src.resource_type
WHEN MATCHED THEN UPDATE SET name = src.name, display_order = src.display_order, description = src.description, is_active = 1
WHEN NOT MATCHED THEN INSERT (resource_type, name, display_order, description) VALUES (src.resource_type, src.name, src.display_order, src.description);

MERGE med.lookup_service_types AS target
USING (VALUES
    (N'lab', N'Xét nghiệm', 10, N'Dịch vụ xét nghiệm'),
    (N'imaging', N'Chẩn đoán hình ảnh', 20, N'X-quang, siêu âm, CT, MRI và hình ảnh khác'),
    (N'procedure', N'Thủ thuật', 30, N'Thủ thuật chuyên môn'),
    (N'surgery', N'Phẫu thuật', 40, N'Dịch vụ phẫu thuật'),
    (N'care', N'Chăm sóc', 50, N'Dịch vụ chăm sóc điều dưỡng'),
    (N'other', N'Khác', 90, N'Dịch vụ khác')
) AS src(service_type, name, display_order, description)
ON target.service_type = src.service_type
WHEN MATCHED THEN UPDATE SET name = src.name, display_order = src.display_order, description = src.description, is_active = 1
WHEN NOT MATCHED THEN INSERT (service_type, name, display_order, description) VALUES (src.service_type, src.name, src.display_order, src.description);

MERGE med.lookup_procedure_types AS target
USING (VALUES
    (N'technical', N'Kỹ thuật', 10, N'Quy trình kỹ thuật chuyên môn'),
    (N'care', N'Chăm sóc', 20, N'Quy trình chăm sóc người bệnh'),
    (N'surgery', N'Phẫu thuật', 30, N'Quy trình phẫu thuật'),
    (N'procedure', N'Thủ thuật', 40, N'Quy trình thủ thuật')
) AS src(procedure_type, name, display_order, description)
ON target.procedure_type = src.procedure_type
WHEN MATCHED THEN UPDATE SET name = src.name, display_order = src.display_order, description = src.description, is_active = 1
WHEN NOT MATCHED THEN INSERT (procedure_type, name, display_order, description) VALUES (src.procedure_type, src.name, src.display_order, src.description);

MERGE med.lookup_protocol_types AS target
USING (VALUES
    (N'treatment_protocol', N'Phác đồ điều trị', 10, N'Phác đồ điều trị bệnh hoặc hội chứng'),
    (N'care', N'Chăm sóc', 20, N'Phác đồ chăm sóc'),
    (N'surgery', N'Phẫu thuật', 30, N'Phác đồ phẫu thuật'),
    (N'procedure', N'Thủ thuật', 40, N'Phác đồ thủ thuật')
) AS src(protocol_type, name, display_order, description)
ON target.protocol_type = src.protocol_type
WHEN MATCHED THEN UPDATE SET name = src.name, display_order = src.display_order, description = src.description, is_active = 1
WHEN NOT MATCHED THEN INSERT (protocol_type, name, display_order, description) VALUES (src.protocol_type, src.name, src.display_order, src.description);

MERGE med.lookup_attachment_types AS target
USING (VALUES
    (N'sop', N'SOP', 10, N'Quy trình thao tác chuẩn'),
    (N'guideline', N'Hướng dẫn', 20, N'Tài liệu hướng dẫn chuyên môn'),
    (N'form', N'Biểu mẫu', 30, N'Biểu mẫu sử dụng trong quy trình'),
    (N'reference', N'Tham khảo', 40, N'Tài liệu tham khảo'),
    (N'other', N'Khác', 90, N'Tệp khác')
) AS src(attachment_type, name, display_order, description)
ON target.attachment_type = src.attachment_type
WHEN MATCHED THEN UPDATE SET name = src.name, display_order = src.display_order, description = src.description, is_active = 1
WHEN NOT MATCHED THEN INSERT (attachment_type, name, display_order, description) VALUES (src.attachment_type, src.name, src.display_order, src.description);

MERGE med.unit_catalog AS target
USING (VALUES
    (N'piece', N'Cái', N'count', 10, N'Đơn vị đếm chung'),
    (N'set', N'Bộ', N'count', 20, N'Trọn bộ vật tư'),
    (N'pair', N'Đôi', N'count', 30, N'Một cặp vật tư'),
    (N'box', N'Hộp', N'count', 40, N'Hộp đóng gói'),
    (N'pack', N'Gói', N'count', 50, N'Gói đóng lẻ'),
    (N'roll', N'Cuộn', N'count', 60, N'Cuộn băng, phim hoặc vật tư'),
    (N'bag', N'Túi', N'count', 70, N'Túi dịch hoặc túi vật tư'),
    (N'bottle', N'Chai', N'count', 80, N'Chai thuốc hoặc hóa chất'),
    (N'vial', N'Lọ', N'count', 90, N'Lọ thuốc hoặc hóa chất'),
    (N'ampoule', N'Ống', N'count', 100, N'Ống thuốc'),
    (N'tube', N'Ống nghiệm', N'count', 110, N'Ống lấy mẫu'),
    (N'syringe', N'Bơm tiêm', N'count', 120, N'Bơm tiêm'),
    (N'tablet', N'Viên', N'count', 130, N'Viên thuốc'),
    (N'capsule', N'Viên nang', N'count', 140, N'Viên nang'),
    (N'dose', N'Liều', N'count', 150, N'Liều dùng'),
    (N'test', N'Lần xét nghiệm', N'count', 160, N'Một lần xét nghiệm'),
    (N'kit', N'Bộ kit', N'count', 170, N'Bộ kit xét nghiệm hoặc thủ thuật'),
    (N'strip', N'Que thử', N'count', 180, N'Que thử'),
    (N'drop', N'Giọt', N'volume', 190, N'Giọt thuốc'),
    (N'ml', N'Millilít', N'volume', 200, N'Đơn vị thể tích ml'),
    (N'l', N'Lít', N'volume', 210, N'Đơn vị thể tích lít'),
    (N'mcg', N'Microgam', N'mass', 220, N'Đơn vị khối lượng microgam'),
    (N'mg', N'Miligam', N'mass', 230, N'Đơn vị khối lượng miligam'),
    (N'g', N'Gam', N'mass', 240, N'Đơn vị khối lượng gam'),
    (N'kg', N'Kilôgam', N'mass', 250, N'Đơn vị khối lượng kilôgam'),
    (N'iu', N'Đơn vị quốc tế', N'activity', 260, N'International Unit'),
    (N'minute', N'Phút', N'time', 270, N'Đơn vị thời gian phút'),
    (N'hour', N'Giờ', N'time', 280, N'Đơn vị thời gian giờ'),
    (N'day', N'Ngày', N'time', 290, N'Đơn vị thời gian ngày')
) AS src(unit_code, name, unit_group, display_order, description)
ON target.unit_code = src.unit_code
WHEN MATCHED THEN UPDATE SET name = src.name, unit_group = src.unit_group, display_order = src.display_order, description = src.description, status = N'active', is_active = 1
WHEN NOT MATCHED THEN INSERT (unit_code, name, unit_group, display_order, description, status) VALUES (src.unit_code, src.name, src.unit_group, src.display_order, src.description, N'active');

PRINT N'Lookup catalogs seeded and normalized.';
