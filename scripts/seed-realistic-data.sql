/* ============================================================
   Seed realistic data for MedicalProcedureManagement
   Run after MedicalProcedureManagement.sql.
   Idempotent by business codes where the schema has natural keys.
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

/* 1. Organization */
MERGE med.departments AS target
USING (VALUES
    (N'BV-ROOT', NULL, N'Bệnh viện Đa khoa Nhóm 3'),
    (N'KHOA-NOI', N'BV-ROOT', N'Khoa Nội tổng hợp'),
    (N'KHOA-NGOAI', N'BV-ROOT', N'Khoa Ngoại tổng hợp'),
    (N'KHOA-SAN', N'BV-ROOT', N'Khoa Sản'),
    (N'KHOA-NHI', N'BV-ROOT', N'Khoa Nhi'),
    (N'KHOA-XN', N'BV-ROOT', N'Khoa Xét nghiệm'),
    (N'KHOA-CDHA', N'BV-ROOT', N'Khoa Chẩn đoán hình ảnh'),
    (N'KHOA-DUOC', N'BV-ROOT', N'Khoa Dược'),
    (N'KHOA-CAP-CUU', N'BV-ROOT', N'Khoa Cấp cứu'),
    (N'PHONG-CNTT', N'BV-ROOT', N'Phòng Công nghệ thông tin'),
    (N'PHONG-HC', N'BV-ROOT', N'Phòng Hành chính'),
    (N'KHOA-TIM-MACH', N'KHOA-NOI', N'Khoa Tim mạch'),
    (N'KHOA-THAN-KINH', N'KHOA-NOI', N'Khoa Thần kinh'),
    (N'KHOA-CTCH', N'KHOA-NGOAI', N'Khoa Chấn thương chỉnh hình'),
    (N'KHOA-PT-TIM', N'KHOA-NGOAI', N'Khoa Phẫu thuật tim'),
    (N'PHONG-KHTH', N'BV-ROOT', N'Phòng Kế hoạch tổng hợp')
) AS src(code, parent_code, name)
ON target.code = src.code
WHEN MATCHED THEN UPDATE SET
    name = src.name,
    status = N'active',
    updated_at = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (code, name, parent_department_id, status)
    VALUES (src.code, src.name, (SELECT department_id FROM med.departments p WHERE p.code = src.parent_code), N'active');

/* 2. Roles, users, groups */
MERGE med.roles AS target
USING (VALUES
    (N'SYSTEM_ADMIN', N'Quản trị hệ thống', N'Toàn quyền hệ thống', 1),
    (N'DEPARTMENT_ADMIN', N'Quản trị khoa phòng', N'Quản lý trong phạm vi khoa/phòng', 0),
    (N'DOCTOR', N'Bác sĩ', N'Thực hiện chuyên môn và chỉ định', 0),
    (N'NURSE', N'Điều dưỡng', N'Chăm sóc và thực hiện y lệnh', 0),
    (N'TECHNICIAN', N'Kỹ thuật viên', N'Thực hiện dịch vụ kỹ thuật', 0),
    (N'PHARMACIST', N'Dược sĩ', N'Quản lý thuốc và vật tư', 0),
    (N'REPORT_VIEWER', N'Xem báo cáo', N'Xem báo cáo thống kê', 0)
) AS src(code, name, description, is_system)
ON target.code = src.code
WHEN MATCHED THEN UPDATE SET name = src.name, description = src.description, is_system = src.is_system, status = N'active'
WHEN NOT MATCHED THEN INSERT (code, name, description, is_system) VALUES (src.code, src.name, src.description, src.is_system);

MERGE med.users AS target
USING (VALUES
    (N'admin', N'admin@benhvien.vn', N'Quản trị hệ thống', N'BV-ROOT', N'EXT-ADMIN'),
    (N'truongkhoa.noi', N'tk.noi@benhvien.vn', N'TS. Nguyễn Minh Khang', N'KHOA-NOI', N'EXT-001'),
    (N'truongkhoa.ngoai', N'tk.ngoai@benhvien.vn', N'TS. Trần Quốc Bảo', N'KHOA-NGOAI', N'EXT-002'),
    (N'truongkhoa.xn', N'tk.xn@benhvien.vn', N'ThS. Phạm Thu Hà', N'KHOA-XN', N'EXT-003'),
    (N'truongkhoa.cdha', N'tk.cdha@benhvien.vn', N'BS. Lê Hoàng Nam', N'KHOA-CDHA', N'EXT-004'),
    (N'bs.noi.01', N'bs.noi01@benhvien.vn', N'BS. Đỗ An Nhiên', N'KHOA-NOI', N'EXT-005'),
    (N'bs.noi.02', N'bs.noi02@benhvien.vn', N'BS. Võ Thanh Sơn', N'KHOA-TIM-MACH', N'EXT-006'),
    (N'bs.ngoai.01', N'bs.ngoai01@benhvien.vn', N'BS. Hồ Mai Anh', N'KHOA-NGOAI', N'EXT-007'),
    (N'bs.san.01', N'bs.san01@benhvien.vn', N'BS. Trương Bích Ngọc', N'KHOA-SAN', N'EXT-008'),
    (N'bs.nhi.01', N'bs.nhi01@benhvien.vn', N'BS. Nguyễn Nhật Linh', N'KHOA-NHI', N'EXT-009'),
    (N'ktv.xn.01', N'ktv.xn01@benhvien.vn', N'KTV. Lê Quang Huy', N'KHOA-XN', N'EXT-010'),
    (N'ktv.xn.02', N'ktv.xn02@benhvien.vn', N'KTV. Phạm Minh Tú', N'KHOA-XN', N'EXT-011'),
    (N'ktv.cdha.01', N'ktv.cdha01@benhvien.vn', N'KTV. Nguyễn Hải Đăng', N'KHOA-CDHA', N'EXT-012'),
    (N'dd.noi.01', N'dd.noi01@benhvien.vn', N'ĐD. Trần Thị Hương', N'KHOA-NOI', N'EXT-013'),
    (N'dd.ngoai.01', N'dd.ngoai01@benhvien.vn', N'ĐD. Lê Thị Thanh', N'KHOA-NGOAI', N'EXT-014'),
    (N'duoc.01', N'duoc01@benhvien.vn', N'DS. Nguyễn Hoài Phương', N'KHOA-DUOC', N'EXT-015'),
    (N'capcuu.01', N'capcuu01@benhvien.vn', N'BS. Vũ Đức Long', N'KHOA-CAP-CUU', N'EXT-016'),
    (N'cntt.01', N'cntt01@benhvien.vn', N'KS. Trần Việt Anh', N'PHONG-CNTT', N'EXT-017'),
    (N'khth.01', N'khth01@benhvien.vn', N'CN. Phạm Khánh Linh', N'PHONG-KHTH', N'EXT-018'),
    (N'baocao.01', N'baocao01@benhvien.vn', N'CV. Nguyễn Phương Mai', N'PHONG-KHTH', N'EXT-019'),
    (N'hc.01', N'hc01@benhvien.vn', N'CV. Lê Đức Hòa', N'PHONG-HC', N'EXT-020')
) AS src(username, email, full_name, dept_code, external_auth_id)
ON target.username = src.username
WHEN MATCHED THEN UPDATE SET
    email = src.email,
    full_name = src.full_name,
    external_auth_id = src.external_auth_id,
    primary_department_id = (SELECT department_id FROM med.departments d WHERE d.code = src.dept_code),
    status = N'active',
    updated_at = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (username, email, full_name, primary_department_id, external_auth_id, status)
    VALUES (src.username, src.email, src.full_name, (SELECT department_id FROM med.departments d WHERE d.code = src.dept_code), src.external_auth_id, N'active');

DECLARE @UserRoles TABLE(username NVARCHAR(100), role_code NVARCHAR(80), dept_code NVARCHAR(50));
INSERT INTO @UserRoles VALUES
(N'admin', N'SYSTEM_ADMIN', NULL),
(N'truongkhoa.noi', N'DEPARTMENT_ADMIN', N'KHOA-NOI'),
(N'truongkhoa.ngoai', N'DEPARTMENT_ADMIN', N'KHOA-NGOAI'),
(N'truongkhoa.xn', N'DEPARTMENT_ADMIN', N'KHOA-XN'),
(N'truongkhoa.cdha', N'DEPARTMENT_ADMIN', N'KHOA-CDHA'),
(N'bs.noi.01', N'DOCTOR', N'KHOA-NOI'),
(N'bs.noi.02', N'DOCTOR', N'KHOA-TIM-MACH'),
(N'bs.ngoai.01', N'DOCTOR', N'KHOA-NGOAI'),
(N'bs.san.01', N'DOCTOR', N'KHOA-SAN'),
(N'bs.nhi.01', N'DOCTOR', N'KHOA-NHI'),
(N'ktv.xn.01', N'TECHNICIAN', N'KHOA-XN'),
(N'ktv.xn.02', N'TECHNICIAN', N'KHOA-XN'),
(N'ktv.cdha.01', N'TECHNICIAN', N'KHOA-CDHA'),
(N'dd.noi.01', N'NURSE', N'KHOA-NOI'),
(N'dd.ngoai.01', N'NURSE', N'KHOA-NGOAI'),
(N'duoc.01', N'PHARMACIST', N'KHOA-DUOC'),
(N'capcuu.01', N'DOCTOR', N'KHOA-CAP-CUU'),
(N'cntt.01', N'SYSTEM_ADMIN', N'PHONG-CNTT'),
(N'khth.01', N'DEPARTMENT_ADMIN', N'PHONG-KHTH'),
(N'baocao.01', N'REPORT_VIEWER', N'PHONG-KHTH');

INSERT INTO med.user_roles (user_id, role_id, department_id)
SELECT u.user_id, r.role_id, d.department_id
FROM @UserRoles ur
JOIN med.users u ON u.username = ur.username
JOIN med.roles r ON r.code = ur.role_code
LEFT JOIN med.departments d ON d.code = ur.dept_code
WHERE NOT EXISTS (
    SELECT 1 FROM med.user_roles x
    WHERE x.user_id = u.user_id AND x.role_id = r.role_id
      AND ISNULL(x.department_id, '00000000-0000-0000-0000-000000000000') = ISNULL(d.department_id, '00000000-0000-0000-0000-000000000000')
      AND x.effective_to IS NULL
);

MERGE med.groups AS target
USING (VALUES
    (N'NHOM-HOI-DONG-QT', N'Hội đồng quản lý quy trình', N'BV-ROOT', N'Duyệt và theo dõi quy trình chuyên môn'),
    (N'NHOM-KTV-XN', N'Kỹ thuật viên xét nghiệm', N'KHOA-XN', N'Thực hiện xét nghiệm và ghi nhận vật tư'),
    (N'NHOM-DIEU-DUONG', N'Điều dưỡng lâm sàng', N'KHOA-NOI', N'Chăm sóc, chuẩn bị và theo dõi người bệnh')
) AS src(code, name, dept_code, description)
ON target.code = src.code
WHEN MATCHED THEN UPDATE SET name = src.name, description = src.description, department_id = (SELECT department_id FROM med.departments d WHERE d.code = src.dept_code)
WHEN NOT MATCHED THEN INSERT (code, name, department_id, description) VALUES (src.code, src.name, (SELECT department_id FROM med.departments d WHERE d.code = src.dept_code), src.description);

/* 3. Screen catalog, permissions and group permission samples */
MERGE med.screen_catalog AS target
USING (VALUES
    (N'SCR_DASHBOARD', N'Tổng quan', N'/admin', N'CORE'),
    (N'SCR_PROCEDURES', N'Quy trình', N'/admin/quy-trinh', N'PROC'),
    (N'SCR_CATALOG', N'Danh mục kỹ thuật', N'/admin/danh-muc', N'CAT'),
    (N'SCR_RESOURCES', N'Tài nguyên', N'/tai-nguyen', N'TECH'),
    (N'SCR_ORDERS', N'Chỉ định kỹ thuật', N'/dieu-phoi', N'TECH'),
    (N'SCR_CLINICAL', N'Lâm sàng', N'/lam-sang', N'CLINICAL'),
    (N'SCR_PROTOCOLS', N'Phác đồ', N'/admin/phac-do', N'CLINICAL'),
    (N'SCR_NOTIFICATIONS', N'Thông báo', N'/thong-bao', N'CORE'),
    (N'SCR_PERMISSIONS', N'Phân quyền', N'/admin/phan-quyen', N'PERM')
) AS src(screen_code, name, route, module_code)
ON target.screen_code = src.screen_code
WHEN MATCHED THEN UPDATE SET name = src.name, route = src.route, module_code = src.module_code, status = N'active'
WHEN NOT MATCHED THEN INSERT (screen_code, name, route, module_code) VALUES (src.screen_code, src.name, src.route, src.module_code);

MERGE med.feature_catalog AS target
USING (VALUES
    (N'SCR_PROCEDURES', N'FEAT_PROC_CREATE', N'Tạo quy trình'),
    (N'SCR_PROCEDURES', N'FEAT_PROC_APPROVE', N'Phê duyệt quy trình'),
    (N'SCR_CATALOG', N'FEAT_SERVICE_NORM', N'Quản lý định mức dịch vụ'),
    (N'SCR_ORDERS', N'FEAT_ORDER_USAGE', N'Ghi nhận sử dụng thực tế'),
    (N'SCR_PROTOCOLS', N'FEAT_PROTOCOL_RULE', N'Quản lý rule áp dụng'),
    (N'SCR_PERMISSIONS', N'FEAT_PERMISSION_OVERRIDE', N'Ghi đè quyền cá nhân')
) AS src(screen_code, feature_code, name)
ON target.screen_id = (SELECT screen_id FROM med.screen_catalog s WHERE s.screen_code = src.screen_code)
   AND target.feature_code = src.feature_code
WHEN MATCHED THEN UPDATE SET name = src.name, status = N'active'
WHEN NOT MATCHED THEN INSERT (screen_id, feature_code, name)
    VALUES ((SELECT screen_id FROM med.screen_catalog s WHERE s.screen_code = src.screen_code), src.feature_code, src.name);

MERGE med.permissions AS target
USING (
    SELECT CONCAT(N'PERM_', REPLACE(s.screen_code, N'SCR_', N''), N'_', a.action_code) AS permission_code,
           s.screen_id,
           a.action_code,
           CONCAT(a.name, N' - ', s.name) AS description
    FROM med.screen_catalog s
    CROSS JOIN (VALUES
        (N'view', N'Xem'),
        (N'create', N'Tạo'),
        (N'update', N'Cập nhật'),
        (N'delete', N'Xóa'),
        (N'approve', N'Phê duyệt')
    ) AS a(action_code, name)
    WHERE s.screen_code IN (N'SCR_PROCEDURES', N'SCR_CATALOG', N'SCR_RESOURCES', N'SCR_ORDERS', N'SCR_CLINICAL', N'SCR_PROTOCOLS', N'SCR_PERMISSIONS')
) AS src(permission_code, screen_id, action_code, description)
ON target.permission_code = src.permission_code
WHEN MATCHED THEN UPDATE SET screen_id = src.screen_id, action_code = src.action_code, description = src.description, status = N'active'
WHEN NOT MATCHED THEN INSERT (permission_code, screen_id, action_code, description) VALUES (src.permission_code, src.screen_id, src.action_code, src.description);

INSERT INTO med.group_permissions (group_id, permission_id, effect_code, department_scope_type, priority, reason, created_by)
SELECT g.group_id, p.permission_id, N'allow', N'global', 200, N'Seed quyền nhóm nghiệp vụ', (SELECT user_id FROM med.users WHERE username = N'admin')
FROM med.groups g
JOIN med.permissions p ON
    (g.code = N'NHOM-HOI-DONG-QT' AND p.permission_code IN (N'PERM_PROCEDURES_APPROVE', N'PERM_PROTOCOLS_APPROVE', N'PERM_PERMISSIONS_VIEW')) OR
    (g.code = N'NHOM-KTV-XN' AND p.permission_code IN (N'PERM_ORDERS_VIEW', N'PERM_ORDERS_UPDATE', N'PERM_RESOURCES_VIEW')) OR
    (g.code = N'NHOM-DIEU-DUONG' AND p.permission_code IN (N'PERM_CLINICAL_VIEW', N'PERM_ORDERS_CREATE'))
WHERE NOT EXISTS (SELECT 1 FROM med.group_permissions gp WHERE gp.group_id = g.group_id AND gp.permission_id = p.permission_id);

INSERT INTO med.role_permissions (role_id, permission_id, effect_code, department_scope_type, priority, reason, created_by)
SELECT r.role_id, p.permission_id, N'allow', N'global', 100, N'Seed quyền vai trò hệ thống', (SELECT user_id FROM med.users WHERE username = N'admin')
FROM med.roles r
JOIN med.permissions p ON
    r.code = N'SYSTEM_ADMIN'
    OR (r.code = N'DEPARTMENT_ADMIN' AND p.action_code IN (N'view', N'create', N'update', N'approve'))
    OR (r.code = N'DOCTOR' AND p.permission_code IN (N'PERM_CLINICAL_VIEW', N'PERM_ORDERS_VIEW', N'PERM_ORDERS_CREATE', N'PERM_PROTOCOLS_VIEW'))
    OR (r.code = N'NURSE' AND p.permission_code IN (N'PERM_CLINICAL_VIEW', N'PERM_ORDERS_VIEW', N'PERM_ORDERS_CREATE'))
    OR (r.code = N'TECHNICIAN' AND p.permission_code IN (N'PERM_ORDERS_VIEW', N'PERM_ORDERS_UPDATE', N'PERM_RESOURCES_VIEW'))
    OR (r.code = N'REPORT_VIEWER' AND p.action_code = N'view')
WHERE NOT EXISTS (SELECT 1 FROM med.role_permissions rp WHERE rp.role_id = r.role_id AND rp.permission_id = p.permission_id);

INSERT INTO med.user_group_members (user_id, group_id)
SELECT u.user_id, g.group_id
FROM med.users u
JOIN med.groups g ON
    (g.code = N'NHOM-HOI-DONG-QT' AND u.username IN (N'admin', N'truongkhoa.noi', N'khth.01')) OR
    (g.code = N'NHOM-KTV-XN' AND u.username IN (N'truongkhoa.xn', N'ktv.xn.01', N'ktv.xn.02')) OR
    (g.code = N'NHOM-DIEU-DUONG' AND u.username IN (N'dd.noi.01', N'dd.ngoai.01'))
WHERE NOT EXISTS (SELECT 1 FROM med.user_group_members m WHERE m.user_id = u.user_id AND m.group_id = g.group_id AND m.effective_to IS NULL);

/* 4. Procedures */
MERGE med.professional_procedures AS target
USING (VALUES
    (N'QT-XN-CTM', N'Xét nghiệm công thức máu', N'technical', N'KHOA-XN'),
    (N'QT-XN-SH', N'Xét nghiệm sinh hóa máu', N'technical', N'KHOA-XN'),
    (N'QT-XQ-NGUC', N'Chụp X-quang ngực thẳng', N'technical', N'KHOA-CDHA'),
    (N'QT-SA-BUNG', N'Siêu âm ổ bụng tổng quát', N'technical', N'KHOA-CDHA'),
    (N'QT-PT-VAN-TIM', N'Phẫu thuật thay van tim', N'surgery', N'KHOA-PT-TIM'),
    (N'QT-CS-HAU-PHAU', N'Chăm sóc hậu phẫu', N'care', N'KHOA-NGOAI'),
    (N'QT-CAP-CUU-SOC', N'Cấp cứu sốc phản vệ', N'procedure', N'KHOA-CAP-CUU'),
    (N'QT-DT-THA', N'Quản lý điều trị tăng huyết áp', N'care', N'KHOA-TIM-MACH'),
    (N'QT-DT-DTD', N'Quản lý điều trị đái tháo đường', N'care', N'KHOA-NOI'),
    (N'QT-NHI-VP', N'Điều trị viêm phổi trẻ em', N'care', N'KHOA-NHI')
) AS src(code, name, type_code, dept_code)
ON target.procedure_code = src.code
WHEN MATCHED THEN UPDATE SET
    name = src.name,
    procedure_type = src.type_code,
    owner_department_id = (SELECT department_id FROM med.departments d WHERE d.code = src.dept_code),
    description = CONCAT(N'Quy trình chuẩn: ', src.name),
    status = N'active'
WHEN NOT MATCHED THEN INSERT (procedure_code, name, procedure_type, owner_department_id, description, created_by)
    VALUES (src.code, src.name, src.type_code, (SELECT department_id FROM med.departments d WHERE d.code = src.dept_code), CONCAT(N'Quy trình chuẩn: ', src.name), (SELECT user_id FROM med.users WHERE username = N'admin'));

INSERT INTO med.procedure_versions (procedure_id, version_no, version_label, status_code, department_id, title, summary, created_by, approved_by, published_by, approved_at, published_at, effective_from)
SELECT p.procedure_id, 1, N'v1.0', N'active', p.owner_department_id, CONCAT(p.name, N' - Phiên bản 1'), N'{"seed":"realistic"}',
       (SELECT user_id FROM med.users WHERE username = N'admin'),
       (SELECT user_id FROM med.users WHERE username = N'admin'),
       (SELECT user_id FROM med.users WHERE username = N'admin'),
       SYSUTCDATETIME(), SYSUTCDATETIME(), DATEADD(DAY, -30, SYSUTCDATETIME())
FROM med.professional_procedures p
WHERE p.procedure_code LIKE N'QT-%'
  AND NOT EXISTS (SELECT 1 FROM med.procedure_versions v WHERE v.procedure_id = p.procedure_id AND v.version_no = 1);

INSERT INTO med.procedure_steps (procedure_version_id, step_no, step_code, name, description, standard_duration_minutes)
SELECT v.procedure_version_id, s.step_no, CONCAT(N'B', FORMAT(s.step_no, '00')), s.name, s.description, s.minutes
FROM med.procedure_versions v
JOIN med.professional_procedures p ON p.procedure_id = v.procedure_id
CROSS JOIN (VALUES
    (1, N'Tiếp nhận và kiểm tra chỉ định', N'Đối chiếu người bệnh, chỉ định và điều kiện thực hiện', 5),
    (2, N'Chuẩn bị nguồn lực', N'Chuẩn bị nhân sự, vật tư, thiết bị theo định mức', 10),
    (3, N'Thực hiện kỹ thuật', N'Thực hiện theo SOP và ghi nhận kết quả', 20),
    (4, N'Hoàn tất và bàn giao', N'Ghi nhận sử dụng thực tế, trả kết quả hoặc bàn giao chăm sóc', 5)
) s(step_no, name, description, minutes)
WHERE p.procedure_code LIKE N'QT-%'
  AND NOT EXISTS (SELECT 1 FROM med.procedure_steps ps WHERE ps.procedure_version_id = v.procedure_version_id AND ps.step_no = s.step_no);

INSERT INTO med.procedure_attachments (procedure_version_id, attachment_type, file_name, file_uri, mime_type, uploaded_by)
SELECT v.procedure_version_id, N'sop', CONCAT(p.procedure_code, N'-sop.pdf'), CONCAT(N'/docs/procedures/', p.procedure_code, N'-sop.pdf'), N'application/pdf',
       (SELECT user_id FROM med.users WHERE username = N'admin')
FROM med.procedure_versions v
JOIN med.professional_procedures p ON p.procedure_id = v.procedure_id
WHERE p.procedure_code LIKE N'QT-%'
  AND NOT EXISTS (SELECT 1 FROM med.procedure_attachments a WHERE a.procedure_version_id = v.procedure_version_id AND a.file_name = CONCAT(p.procedure_code, N'-sop.pdf'));

INSERT INTO med.procedure_screen_mappings (procedure_version_id, screen_id, feature_id, action_code, enforcement_mode, rule_json)
SELECT v.procedure_version_id, s.screen_id, f.feature_id, N'execute', N'warning', N'{"source":"seed"}'
FROM med.procedure_versions v
JOIN med.professional_procedures p ON p.procedure_id = v.procedure_id
JOIN med.screen_catalog s ON s.screen_code = CASE
    WHEN p.procedure_code LIKE N'QT-XN-%' THEN N'SCR_ORDERS'
    WHEN p.procedure_code LIKE N'QT-DT-%' THEN N'SCR_CLINICAL'
    ELSE N'SCR_PROCEDURES' END
LEFT JOIN med.feature_catalog f ON f.screen_id = s.screen_id
WHERE p.procedure_code LIKE N'QT-%'
  AND NOT EXISTS (
      SELECT 1 FROM med.procedure_screen_mappings m
      WHERE m.procedure_version_id = v.procedure_version_id AND m.screen_id = s.screen_id
  );

/* 5. Resources and technical services */
MERGE med.resource_catalog AS target
USING (VALUES
    (N'supply', N'VT-ONG-EDTA', N'Ống nghiệm EDTA', N'ampoule'),
    (N'supply', N'VT-KIM-LAY-MAU', N'Kim lấy máu', N'piece'),
    (N'supply', N'VT-BANG-GAC', N'Băng gạc vô khuẩn', N'piece'),
    (N'supply', N'VT-GANG-TAY', N'Găng tay y tế', N'piece'),
    (N'supply', N'VT-KHAU-PT', N'Chỉ khâu phẫu thuật', N'piece'),
    (N'supply', N'VT-DAY-TRUYEN', N'Dây truyền dịch', N'set'),
    (N'equipment', N'TB-MAY-CTM', N'Máy huyết học tự động', N'piece'),
    (N'equipment', N'TB-MAY-SH', N'Máy sinh hóa tự động', N'piece'),
    (N'equipment', N'TB-XQUANG', N'Máy X-quang kỹ thuật số', N'piece'),
    (N'equipment', N'TB-SIEU-AM', N'Máy siêu âm màu', N'piece'),
    (N'equipment', N'TB-MONITOR', N'Monitor theo dõi', N'piece'),
    (N'equipment', N'TB-MAY-THO', N'Máy thở', N'piece'),
    (N'drug', N'THUOC-ADRENALIN', N'Adrenalin 1mg/ml', N'ampoule'),
    (N'drug', N'THUOC-INSULIN', N'Insulin nhanh', N'ampoule'),
    (N'drug', N'THUOC-HEPARIN', N'Heparin', N'ampoule'),
    (N'drug', N'THUOC-CEFTRIAXONE', N'Ceftriaxone 1g', N'g'),
    (N'drug', N'THUOC-PARACETAMOL', N'Paracetamol 500mg', N'tablet'),
    (N'chemical', N'HC-HUYET-HOC', N'Hóa chất xét nghiệm huyết học', N'ml'),
    (N'chemical', N'HC-SINH-HOA', N'Hóa chất xét nghiệm sinh hóa', N'ml'),
    (N'chemical', N'HC-SAT-KHUAN', N'Dung dịch sát khuẩn', N'ml'),
    (N'supply', N'VT-KHAY-BENH-PHAM', N'Khay bệnh phẩm', N'piece'),
    (N'supply', N'VT-TUI-MAU', N'Túi đựng mẫu', N'piece'),
    (N'supply', N'VT-MASK-OXY', N'Mặt nạ oxy', N'piece'),
    (N'supply', N'VT-SONDE', N'Sonde tiểu', N'piece'),
    (N'equipment', N'TB-DAO-MO', N'Dao mổ điện', N'piece'),
    (N'equipment', N'TB-BOM-TIEM-DIEN', N'Bơm tiêm điện', N'piece'),
    (N'drug', N'THUOC-NORADRENALIN', N'Noradrenalin', N'ampoule'),
    (N'drug', N'THUOC-AMLODIPIN', N'Amlodipin 5mg', N'tablet'),
    (N'drug', N'THUOC-SALBUTAMOL', N'Salbutamol khí dung', N'ampoule'),
    (N'chemical', N'HC-CAN-QUANG', N'Thuốc cản quang', N'ml')
) AS src(resource_type, resource_code, name, unit_code)
ON target.resource_type = src.resource_type AND target.resource_code = src.resource_code
WHEN MATCHED THEN UPDATE SET name = src.name, default_unit_code = src.unit_code, status = N'active'
WHEN NOT MATCHED THEN INSERT (resource_type, resource_code, name, default_unit_code) VALUES (src.resource_type, src.resource_code, src.name, src.unit_code);

MERGE med.technical_services AS target
USING (VALUES
    (N'DV-XN-CTM', N'Xét nghiệm công thức máu', N'lab', N'KHOA-XN', N'QT-XN-CTM'),
    (N'DV-XN-SH-GLU', N'Định lượng Glucose máu', N'lab', N'KHOA-XN', N'QT-XN-SH'),
    (N'DV-XN-SH-MO', N'Bộ mỡ máu', N'lab', N'KHOA-XN', N'QT-XN-SH'),
    (N'DV-XN-CRP', N'Định lượng CRP', N'lab', N'KHOA-XN', N'QT-XN-SH'),
    (N'DV-XQ-NGUC', N'Chụp X-quang ngực', N'imaging', N'KHOA-CDHA', N'QT-XQ-NGUC'),
    (N'DV-XQ-XUONG', N'Chụp X-quang xương khớp', N'imaging', N'KHOA-CDHA', N'QT-XQ-NGUC'),
    (N'DV-SA-BUNG', N'Siêu âm ổ bụng', N'imaging', N'KHOA-CDHA', N'QT-SA-BUNG'),
    (N'DV-SA-TIM', N'Siêu âm tim', N'imaging', N'KHOA-CDHA', N'QT-SA-BUNG'),
    (N'DV-PT-VAN-TIM', N'Phẫu thuật thay van tim', N'surgery', N'KHOA-PT-TIM', N'QT-PT-VAN-TIM'),
    (N'DV-PT-CTCH', N'Phẫu thuật kết hợp xương', N'surgery', N'KHOA-CTCH', N'QT-CS-HAU-PHAU'),
    (N'DV-CS-HAU-PHAU', N'Chăm sóc hậu phẫu chuẩn', N'care', N'KHOA-NGOAI', N'QT-CS-HAU-PHAU'),
    (N'DV-CAPCUU-SOC', N'Xử trí sốc phản vệ', N'procedure', N'KHOA-CAP-CUU', N'QT-CAP-CUU-SOC'),
    (N'DV-DT-THA', N'Theo dõi tăng huyết áp', N'care', N'KHOA-TIM-MACH', N'QT-DT-THA'),
    (N'DV-DT-DTD', N'Theo dõi đái tháo đường', N'care', N'KHOA-NOI', N'QT-DT-DTD'),
    (N'DV-NHI-VP', N'Điều trị viêm phổi trẻ em', N'care', N'KHOA-NHI', N'QT-NHI-VP'),
    (N'DV-KHI-DUNG', N'Khí dung thuốc giãn phế quản', N'procedure', N'KHOA-NHI', N'QT-NHI-VP'),
    (N'DV-THO-MAY', N'Thở máy xâm nhập', N'procedure', N'KHOA-CAP-CUU', N'QT-CAP-CUU-SOC'),
    (N'DV-TIEM-TRUYEN', N'Tiêm truyền tĩnh mạch', N'care', N'KHOA-NOI', N'QT-CS-HAU-PHAU'),
    (N'DV-DUOC-CAP-PHAT', N'Cấp phát thuốc nội trú', N'other', N'KHOA-DUOC', NULL),
    (N'DV-SAT-KHUAN-PT', N'Sát khuẩn vùng mổ', N'care', N'KHOA-NGOAI', N'QT-CS-HAU-PHAU')
) AS src(service_code, name, service_type, dept_code, procedure_code)
ON target.service_code = src.service_code
WHEN MATCHED THEN UPDATE SET
    name = src.name,
    service_type = src.service_type,
    department_id = (SELECT department_id FROM med.departments WHERE code = src.dept_code),
    linked_procedure_id = (SELECT procedure_id FROM med.professional_procedures WHERE procedure_code = src.procedure_code),
    description = CONCAT(N'Dịch vụ kỹ thuật: ', src.name),
    status = N'active'
WHEN NOT MATCHED THEN INSERT (service_code, name, service_type, department_id, linked_procedure_id, description, created_by)
    VALUES (src.service_code, src.name, src.service_type, (SELECT department_id FROM med.departments WHERE code = src.dept_code), (SELECT procedure_id FROM med.professional_procedures WHERE procedure_code = src.procedure_code), CONCAT(N'Dịch vụ kỹ thuật: ', src.name), (SELECT user_id FROM med.users WHERE username = N'admin'));

DECLARE @Norms TABLE(service_code NVARCHAR(100), resource_code NVARCHAR(100), qty DECIMAL(18,4), unit_code NVARCHAR(50), note NVARCHAR(1000));
INSERT INTO @Norms VALUES
(N'DV-XN-CTM', N'VT-ONG-EDTA', 2, N'ampoule', N'Mẫu máu EDTA'),
(N'DV-XN-CTM', N'HC-HUYET-HOC', 10, N'ml', N'Hóa chất chạy máy'),
(N'DV-XN-SH-GLU', N'VT-ONG-EDTA', 1, N'ampoule', N'Mẫu huyết thanh'),
(N'DV-XN-SH-GLU', N'HC-SINH-HOA', 8, N'ml', N'Hóa chất sinh hóa'),
(N'DV-XN-SH-MO', N'HC-SINH-HOA', 12, N'ml', N'Bộ hóa chất mỡ máu'),
(N'DV-XN-CRP', N'HC-SINH-HOA', 6, N'ml', N'CRP reagent'),
(N'DV-XQ-NGUC', N'TB-XQUANG', 1, N'piece', N'Phòng chụp X-quang'),
(N'DV-XQ-XUONG', N'TB-XQUANG', 1, N'piece', N'Phòng chụp X-quang'),
(N'DV-SA-BUNG', N'TB-SIEU-AM', 1, N'piece', N'Máy siêu âm'),
(N'DV-SA-TIM', N'TB-SIEU-AM', 1, N'piece', N'Máy siêu âm tim'),
(N'DV-PT-VAN-TIM', N'VT-KHAU-PT', 6, N'piece', N'Chỉ khâu tim mạch'),
(N'DV-PT-VAN-TIM', N'THUOC-HEPARIN', 2, N'ampoule', N'Chống đông'),
(N'DV-PT-CTCH', N'VT-KHAU-PT', 4, N'piece', N'Chỉ khâu phẫu thuật'),
(N'DV-CS-HAU-PHAU', N'VT-BANG-GAC', 8, N'piece', N'Băng gạc thay băng'),
(N'DV-CAPCUU-SOC', N'THUOC-ADRENALIN', 2, N'ampoule', N'Adrenalin cấp cứu'),
(N'DV-DT-THA', N'THUOC-AMLODIPIN', 1, N'tablet', N'Thuốc điều trị nền'),
(N'DV-DT-DTD', N'THUOC-INSULIN', 1, N'ampoule', N'Insulin nhanh'),
(N'DV-NHI-VP', N'THUOC-CEFTRIAXONE', 1, N'g', N'Kháng sinh'),
(N'DV-KHI-DUNG', N'THUOC-SALBUTAMOL', 1, N'ampoule', N'Khí dung'),
(N'DV-THO-MAY', N'TB-MAY-THO', 1, N'piece', N'Máy thở'),
(N'DV-TIEM-TRUYEN', N'VT-DAY-TRUYEN', 1, N'set', N'Dây truyền'),
(N'DV-DUOC-CAP-PHAT', N'THUOC-PARACETAMOL', 2, N'tablet', N'Đơn thuốc mẫu'),
(N'DV-SAT-KHUAN-PT', N'HC-SAT-KHUAN', 50, N'ml', N'Sát khuẩn vùng mổ');

INSERT INTO med.technical_resource_norms (technical_service_id, resource_id, standard_quantity, unit_code, is_required, note)
SELECT s.technical_service_id, r.resource_id, n.qty, n.unit_code, 1, n.note
FROM @Norms n
JOIN med.technical_services s ON s.service_code = n.service_code
JOIN med.resource_catalog r ON r.resource_code = n.resource_code
WHERE NOT EXISTS (
    SELECT 1 FROM med.technical_resource_norms x
    WHERE x.technical_service_id = s.technical_service_id AND x.resource_id = r.resource_id
);

/* Mirror resource norms to procedure version norms when a service links to a procedure. */
INSERT INTO med.procedure_version_resource_norms (procedure_version_id, resource_id, standard_quantity, unit_code, is_required, note)
SELECT pv.procedure_version_id, trn.resource_id, trn.standard_quantity, trn.unit_code, trn.is_required, CONCAT(N'Mirror từ ', ts.service_code)
FROM med.technical_services ts
JOIN med.procedure_versions pv ON pv.procedure_id = ts.linked_procedure_id AND pv.version_no = 1
JOIN med.technical_resource_norms trn ON trn.technical_service_id = ts.technical_service_id
WHERE ts.linked_procedure_id IS NOT NULL
  AND ts.technical_service_id = (
      SELECT MIN(ts2.technical_service_id)
      FROM med.technical_services ts2
      JOIN med.technical_resource_norms trn2 ON trn2.technical_service_id = ts2.technical_service_id
      WHERE ts2.linked_procedure_id = ts.linked_procedure_id
        AND trn2.resource_id = trn.resource_id
  )
  AND NOT EXISTS (
      SELECT 1 FROM med.procedure_version_resource_norms x
      WHERE x.procedure_version_id = pv.procedure_version_id AND x.resource_id = trn.resource_id
  );

/* 6. Protocols */
MERGE med.clinical_protocols AS target
USING (VALUES
    (N'PD-THA', N'Phác đồ tăng huyết áp', N'treatment_protocol', N'KHOA-TIM-MACH'),
    (N'PD-DTD', N'Phác đồ đái tháo đường type 2', N'treatment_protocol', N'KHOA-NOI'),
    (N'PD-SUY-TIM', N'Phác đồ suy tim', N'treatment_protocol', N'KHOA-TIM-MACH'),
    (N'PD-COPD', N'Phác đồ COPD', N'treatment_protocol', N'KHOA-NOI'),
    (N'PD-VIEM-PHOI', N'Phác đồ viêm phổi cộng đồng', N'treatment_protocol', N'KHOA-NHI')
) AS src(code, name, type_code, dept_code)
ON target.protocol_code = src.code
WHEN MATCHED THEN UPDATE SET
    name = src.name,
    protocol_type = src.type_code,
    owner_department_id = (SELECT department_id FROM med.departments WHERE code = src.dept_code),
    description = CONCAT(N'Phác đồ chuẩn: ', src.name),
    status = N'active'
WHEN NOT MATCHED THEN INSERT (protocol_code, name, protocol_type, owner_department_id, description, created_by)
    VALUES (src.code, src.name, src.type_code, (SELECT department_id FROM med.departments WHERE code = src.dept_code), CONCAT(N'Phác đồ chuẩn: ', src.name), (SELECT user_id FROM med.users WHERE username = N'admin'));

INSERT INTO med.clinical_protocol_versions (clinical_protocol_id, version_no, status_code, title, summary, content_json, effective_from, created_by, approved_by, published_by, approved_at, published_at)
SELECT cp.clinical_protocol_id, 1, N'active', CONCAT(cp.name, N' - Phiên bản 1'), N'Áp dụng theo hướng dẫn nội bộ', N'{"seed":"realistic"}',
       DATEADD(DAY, -20, SYSUTCDATETIME()), (SELECT user_id FROM med.users WHERE username = N'admin'),
       (SELECT user_id FROM med.users WHERE username = N'admin'), (SELECT user_id FROM med.users WHERE username = N'admin'),
       SYSUTCDATETIME(), SYSUTCDATETIME()
FROM med.clinical_protocols cp
WHERE cp.protocol_code LIKE N'PD-%'
  AND NOT EXISTS (SELECT 1 FROM med.clinical_protocol_versions v WHERE v.clinical_protocol_id = cp.clinical_protocol_id AND v.version_no = 1);

DECLARE @ProtocolRules TABLE(protocol_code NVARCHAR(100), rule_type NVARCHAR(50), rule_json NVARCHAR(MAX), priority INT);
INSERT INTO @ProtocolRules VALUES
(N'PD-THA', N'icd', N'{"icd":["I10","I11","I12","I13","I15"]}', 100),
(N'PD-DTD', N'icd', N'{"icd":["E11","E14"]}', 100),
(N'PD-SUY-TIM', N'icd', N'{"icd":["I50"]}', 100),
(N'PD-COPD', N'icd', N'{"icd":["J44"]}', 100),
(N'PD-VIEM-PHOI', N'icd', N'{"icd":["J12","J13","J15","J18"]}', 100);

INSERT INTO med.protocol_applicability_rules (clinical_protocol_version_id, rule_type, rule_json, priority)
SELECT v.clinical_protocol_version_id, r.rule_type, r.rule_json, r.priority
FROM @ProtocolRules r
JOIN med.clinical_protocols cp ON cp.protocol_code = r.protocol_code
JOIN med.clinical_protocol_versions v ON v.clinical_protocol_id = cp.clinical_protocol_id AND v.version_no = 1
WHERE NOT EXISTS (
    SELECT 1 FROM med.protocol_applicability_rules x
    WHERE x.clinical_protocol_version_id = v.clinical_protocol_version_id AND x.rule_type = r.rule_type
);

INSERT INTO med.clinical_protocol_procedures (clinical_protocol_version_id, procedure_version_id, relation_type, sequence_no, note)
SELECT cpv.clinical_protocol_version_id, pv.procedure_version_id, N'requires', 1, N'Seed liên kết phác đồ - quy trình'
FROM med.clinical_protocols cp
JOIN med.clinical_protocol_versions cpv ON cpv.clinical_protocol_id = cp.clinical_protocol_id AND cpv.version_no = 1
JOIN med.professional_procedures pp ON
    (cp.protocol_code = N'PD-THA' AND pp.procedure_code = N'QT-DT-THA') OR
    (cp.protocol_code = N'PD-DTD' AND pp.procedure_code = N'QT-DT-DTD') OR
    (cp.protocol_code = N'PD-SUY-TIM' AND pp.procedure_code = N'QT-PT-VAN-TIM') OR
    (cp.protocol_code = N'PD-COPD' AND pp.procedure_code = N'QT-CAP-CUU-SOC') OR
    (cp.protocol_code = N'PD-VIEM-PHOI' AND pp.procedure_code = N'QT-NHI-VP')
JOIN med.procedure_versions pv ON pv.procedure_id = pp.procedure_id AND pv.version_no = 1
WHERE NOT EXISTS (
    SELECT 1 FROM med.clinical_protocol_procedures x
    WHERE x.clinical_protocol_version_id = cpv.clinical_protocol_version_id AND x.procedure_version_id = pv.procedure_version_id
);

/* 7. Patients, encounters, orders, resource checks */
MERGE med.patient_refs AS target
USING (VALUES
    (N'BN-2026-001', N'BN001', N'Nguyễn Văn Minh', '1975-03-15', N'male'),
    (N'BN-2026-002', N'BN002', N'Trần Thị Hoa', '1988-07-20', N'female'),
    (N'BN-2026-003', N'BN003', N'Lê Quốc Huy', '1969-11-03', N'male'),
    (N'BN-2026-004', N'BN004', N'Phạm Thu Trang', '1992-02-14', N'female'),
    (N'BN-2026-005', N'BN005', N'Hoàng Đức Anh', '2017-05-09', N'male'),
    (N'BN-2026-006', N'BN006', N'Vũ Thị Lan', '1958-12-22', N'female'),
    (N'BN-2026-007', N'BN007', N'Đỗ Minh Quân', '1981-09-30', N'male'),
    (N'BN-2026-008', N'BN008', N'Bùi Ngọc Mai', '2001-01-05', N'female'),
    (N'BN-2026-009', N'BN009', N'Ngô Gia Bảo', '2014-04-18', N'male'),
    (N'BN-2026-010', N'BN010', N'Phan Thảo Vy', '1995-10-10', N'female'),
    (N'BN-2026-011', N'BN011', N'Nguyễn Hải Nam', '1970-08-08', N'male'),
    (N'BN-2026-012', N'BN012', N'Đặng Thanh Tâm', '1983-06-06', N'female')
) AS src(external_patient_id, patient_code, display_name, birth_date, gender_code)
ON target.external_patient_id = src.external_patient_id
WHEN MATCHED THEN UPDATE SET patient_code = src.patient_code, display_name = src.display_name, birth_date = src.birth_date, gender_code = src.gender_code
WHEN NOT MATCHED THEN INSERT (external_patient_id, patient_code, display_name, birth_date, gender_code)
    VALUES (src.external_patient_id, src.patient_code, src.display_name, src.birth_date, src.gender_code);

INSERT INTO med.encounter_refs (patient_ref_id, external_encounter_id, encounter_type, department_id, started_at)
SELECT p.patient_ref_id, CONCAT(N'LK-', p.patient_code, N'-01'), N'outpatient',
       COALESCE((SELECT department_id FROM med.departments WHERE code = N'KHOA-NOI'), (SELECT TOP 1 department_id FROM med.departments)),
       DATEADD(DAY, -ABS(CHECKSUM(p.patient_code)) % 20, SYSUTCDATETIME())
FROM med.patient_refs p
WHERE p.patient_code LIKE N'BN0%'
  AND NOT EXISTS (SELECT 1 FROM med.encounter_refs e WHERE e.external_encounter_id = CONCAT(N'LK-', p.patient_code, N'-01'));

IF (SELECT COUNT(*) FROM med.technical_orders) < 50
BEGIN
    ;WITH n AS (
        SELECT TOP (50) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS rn FROM sys.all_objects
    ),
    services AS (
        SELECT technical_service_id, department_id, linked_procedure_id, ROW_NUMBER() OVER (ORDER BY service_code) AS rn,
               COUNT(*) OVER () AS cnt
        FROM med.technical_services
        WHERE status = N'active'
    ),
    patients AS (
        SELECT p.patient_ref_id, e.encounter_ref_id, ROW_NUMBER() OVER (ORDER BY p.patient_code) AS rn,
               COUNT(*) OVER () AS cnt
        FROM med.patient_refs p
        LEFT JOIN med.encounter_refs e ON e.patient_ref_id = p.patient_ref_id
        WHERE p.patient_code LIKE N'BN0%'
    )
    INSERT INTO med.technical_orders (technical_service_id, procedure_version_id, patient_ref_id, encounter_ref_id, ordering_department_id, ordered_by, order_status, ordered_at, completed_at)
    SELECT s.technical_service_id,
           (SELECT TOP 1 procedure_version_id FROM med.procedure_versions pv WHERE pv.procedure_id = s.linked_procedure_id ORDER BY version_no DESC),
           p.patient_ref_id,
           p.encounter_ref_id,
           s.department_id,
           (SELECT user_id FROM med.users WHERE username = N'bs.noi.01'),
           CASE WHEN n.rn % 7 = 0 THEN N'cancelled' WHEN n.rn % 5 = 0 THEN N'in_progress' WHEN n.rn % 3 = 0 THEN N'scheduled' ELSE N'completed' END,
           DATEADD(HOUR, -n.rn, SYSUTCDATETIME()),
           CASE WHEN n.rn % 7 <> 0 AND n.rn % 5 <> 0 AND n.rn % 3 <> 0 THEN DATEADD(HOUR, -n.rn + 1, SYSUTCDATETIME()) ELSE NULL END
    FROM n
    JOIN services s ON s.rn = ((n.rn - 1) % s.cnt) + 1
    JOIN patients p ON p.rn = ((n.rn - 1) % p.cnt) + 1;
END;

INSERT INTO med.resource_availability_snapshots (technical_order_id, resource_id, required_quantity, available_quantity, unit_code, availability_status, external_payload_json)
SELECT o.technical_order_id, trn.resource_id, trn.standard_quantity, trn.standard_quantity + 20, trn.unit_code, N'available', N'{"source":"seed"}'
FROM med.technical_orders o
JOIN med.technical_resource_norms trn ON trn.technical_service_id = o.technical_service_id
WHERE NOT EXISTS (
    SELECT 1 FROM med.resource_availability_snapshots x
    WHERE x.technical_order_id = o.technical_order_id AND x.resource_id = trn.resource_id
);

INSERT INTO med.actual_resource_usages (technical_order_id, resource_id, actual_quantity, unit_code, variance_reason, revision_no, is_final, captured_by)
SELECT o.technical_order_id, trn.resource_id,
       CASE WHEN ABS(CHECKSUM(o.technical_order_id, trn.resource_id)) % 5 = 0 THEN trn.standard_quantity + 1 ELSE trn.standard_quantity END,
       trn.unit_code,
       CASE WHEN ABS(CHECKSUM(o.technical_order_id, trn.resource_id)) % 5 = 0 THEN N'Tăng do hao hụt thực tế' ELSE NULL END,
       1, 1,
       (SELECT user_id FROM med.users WHERE username = N'ktv.xn.01')
FROM med.technical_orders o
JOIN med.technical_resource_norms trn ON trn.technical_service_id = o.technical_service_id
WHERE o.order_status = N'completed'
  AND NOT EXISTS (
      SELECT 1 FROM med.actual_resource_usages x
      WHERE x.technical_order_id = o.technical_order_id AND x.resource_id = trn.resource_id AND x.revision_no = 1
  );

/* Apply protocols to representative patients. */
;WITH patient_rows AS (
    SELECT TOP (12)
           p.patient_ref_id,
           e.encounter_ref_id,
           ROW_NUMBER() OVER (ORDER BY p.patient_code) AS rn
    FROM med.patient_refs p
    JOIN med.encounter_refs e ON e.patient_ref_id = p.patient_ref_id
    WHERE p.patient_code LIKE N'BN0%'
)
INSERT INTO med.patient_protocol_applications (patient_ref_id, encounter_ref_id, diagnosis_code, clinical_protocol_version_id, application_status, applied_by, applied_at, decision_context_json)
SELECT pr.patient_ref_id,
       pr.encounter_ref_id,
       CASE (pr.rn - 1) % 5 WHEN 0 THEN N'I10' WHEN 1 THEN N'E11' WHEN 2 THEN N'I50' WHEN 3 THEN N'J44' ELSE N'J18' END,
       v.clinical_protocol_version_id,
       N'applied',
       (SELECT user_id FROM med.users WHERE username = N'bs.noi.01'),
       DATEADD(DAY, -pr.rn, SYSUTCDATETIME()),
       N'{"source":"seed"}'
FROM patient_rows pr
JOIN med.clinical_protocols cp ON cp.protocol_code = CASE (pr.rn - 1) % 5
    WHEN 0 THEN N'PD-THA' WHEN 1 THEN N'PD-DTD' WHEN 2 THEN N'PD-SUY-TIM' WHEN 3 THEN N'PD-COPD' ELSE N'PD-VIEM-PHOI' END
JOIN med.clinical_protocol_versions v ON v.clinical_protocol_id = cp.clinical_protocol_id AND v.version_no = 1
WHERE NOT EXISTS (SELECT 1 FROM med.patient_protocol_applications x WHERE x.patient_ref_id = pr.patient_ref_id AND x.clinical_protocol_version_id = v.clinical_protocol_version_id);

/* 8. Notifications and preferences */
INSERT INTO med.notification_preferences (user_id, notification_type, channel_code, is_enabled)
SELECT u.user_id, t.notification_type, c.channel_code, 1
FROM med.users u
CROSS JOIN (VALUES (N'procedure_approval'), (N'order_status'), (N'resource_warning')) t(notification_type)
CROSS JOIN (VALUES (N'in_app'), (N'email')) c(channel_code)
WHERE u.username IN (N'admin', N'truongkhoa.noi', N'ktv.xn.01', N'bs.noi.01')
  AND NOT EXISTS (
      SELECT 1 FROM med.notification_preferences p
      WHERE p.user_id = u.user_id AND p.notification_type = t.notification_type AND p.channel_code = c.channel_code
  );

INSERT INTO med.notifications (recipient_user_id, notification_type, title, body, severity, source_type, source_id, payload_json)
SELECT u.user_id, N'procedure_approval', N'Có quy trình chờ phê duyệt', N'Quy trình mới đã được gửi lên hội đồng phê duyệt.', N'info', N'procedure_version', NULL, N'{"seed":true}'
FROM med.users u
WHERE u.username IN (N'admin', N'truongkhoa.noi')
  AND NOT EXISTS (SELECT 1 FROM med.notifications n WHERE n.recipient_user_id = u.user_id AND n.title = N'Có quy trình chờ phê duyệt');

INSERT INTO med.notifications (recipient_user_id, notification_type, title, body, severity, source_type, source_id, payload_json)
SELECT u.user_id, N'resource_warning', N'Cảnh báo định mức vật tư', N'Một số chỉ định có sử dụng thực tế vượt định mức chuẩn.', N'warning', N'actual_resource_usage', NULL, N'{"seed":true}'
FROM med.users u
WHERE u.username IN (N'truongkhoa.xn', N'ktv.xn.01')
  AND NOT EXISTS (SELECT 1 FROM med.notifications n WHERE n.recipient_user_id = u.user_id AND n.title = N'Cảnh báo định mức vật tư');

INSERT INTO med.notification_delivery_attempts (notification_id, channel_code, delivery_status, error_message)
SELECT n.notification_id, c.channel_code, CASE WHEN c.channel_code = N'email' THEN N'sent' ELSE N'sent' END, NULL
FROM med.notifications n
CROSS JOIN (VALUES (N'in_app'), (N'email')) c(channel_code)
WHERE NOT EXISTS (
    SELECT 1 FROM med.notification_delivery_attempts a
    WHERE a.notification_id = n.notification_id AND a.channel_code = c.channel_code
);

PRINT N'Seed realistic data completed: departments, users, permissions, procedures, services, resources, orders, protocols, notifications.';
