# Project Overview PDR

## Overview
Module quản lý quy trình kỹ thuật chuyên môn dùng để chuẩn hóa quy trình nghiệp vụ bệnh viện, quản lý phân quyền theo vai trò, kiểm soát thao tác theo quy trình, version hóa tài liệu, liên kết định mức vật tư/thuốc/thiết bị, và hỗ trợ tra cứu phác đồ lâm sàng.

## Scope
Phạm vi gồm 6 chủ đề chính:

| Mã | Chủ đề | Mục tiêu |
|---|---|---|
| F01 | Quy trình kỹ thuật chuyên môn | Lưu quy trình, bước thực hiện, điều kiện chuyển bước, thời gian chuẩn, ánh xạ màn hình/chức năng |
| F02 | Thiết lập phân quyền | Gán quyền theo tài khoản, nhóm vai trò, khoa/phòng, chức năng, thao tác CRUD |
| F03 | Điều chỉnh phân quyền | Cập nhật quyền theo hiệu lực, ghi lý do, log thay đổi, thông báo người bị ảnh hưởng |
| F04 | Ban hành quy trình | Soạn thảo, phê duyệt, ban hành, thay thế phiên bản cũ, thông báo nhân viên |
| F05 | Danh mục kỹ thuật và định mức | Quản lý kỹ thuật, thiết bị, vật tư, thuốc/hóa chất, kiểm tra tồn kho, báo cáo tiêu thụ |
| F06 | Quy trình chăm sóc/phác đồ/thủ thuật | Quản lý phác đồ lâm sàng, chống chỉ định, gợi ý theo ICD, lưu lịch sử áp dụng bệnh nhân |

## Actors
| Actor | Trách nhiệm |
|---|---|
| Quản trị viên hệ thống | Nhập quy trình, phân quyền, điều chỉnh quyền, cấu hình ánh xạ |
| Lãnh đạo/Kiểm duyệt | Duyệt và ban hành quy trình/phác đồ |
| Bác sĩ | Tra cứu, áp dụng phác đồ, thực hiện bước chuyên môn được phân quyền |
| Điều dưỡng | Thực hiện quy trình chăm sóc, xác nhận bước theo vai trò |
| Dược sĩ | Kiểm tra thuốc/hóa chất liên quan kỹ thuật/phác đồ |
| Kỹ thuật viên | Thực hiện kỹ thuật, cập nhật vật tư/thiết bị dùng thực tế |
| Kiểm toán/QA | Truy vết phân quyền, phiên bản, thao tác lệch quy trình |

## Functional Requirements
| ID | Requirement | Acceptance |
|---|---|---|
| FR-01 | Admin tạo quy trình dạng nhập trực tiếp hoặc tải file | Lưu được bản nháp, metadata, file đính kèm |
| FR-02 | Quy trình có bước tuần tự, người thực hiện, điều kiện chuyển bước, thời gian chuẩn | Bước có thứ tự, role phụ trách, SLA, điều kiện rõ |
| FR-03 | Quy trình ánh xạ với màn hình/chức năng phần mềm | Khi vào chức năng, hệ thống biết quy trình kiểm soát |
| FR-04 | Hệ thống cảnh báo thao tác lệch quy trình | Chặn hoặc cảnh báo tùy cấu hình mức độ |
| FR-05 | Quy trình version hóa và lưu lịch sử thay đổi | Không mất phiên bản cũ, xem diff được |
| FR-06 | Admin thiết lập quyền theo tài khoản/nhóm | User chỉ thấy chức năng được cấp |
| FR-07 | Hệ thống trả thông báo không có quyền truy cập | Trả lỗi thống nhất cho UI/API |
| FR-08 | Log đầy đủ thay đổi phân quyền | Ghi ai, làm gì, trước/sau, lý do, hiệu lực |
| FR-09 | Thay đổi quyền có hiệu lực ngay hoặc theo thời điểm | Scheduler áp quyền đúng thời điểm |
| FR-10 | Người dùng bị ảnh hưởng nhận thông báo | Notification ghi trạng thái đã gửi/đã đọc |
| FR-11 | Ban hành quy trình mới thay thế quy trình cũ | Version active duy nhất theo khoa/phòng và hiệu lực |
| FR-12 | Danh mục kỹ thuật có định mức vật tư/thuốc/thiết bị | Chỉ định kỹ thuật kiểm tra tồn kho tự động |
| FR-13 | Báo cáo tiêu thụ thực tế so với định mức | Có chênh lệch, tỷ lệ vượt/thiếu, lọc theo kỳ |
| FR-14 | Phác đồ liên kết ICD/nhóm bệnh nhân | Gợi ý phác đồ phù hợp khi có chẩn đoán |
| FR-15 | Lưu lịch sử áp dụng phác đồ từng bệnh nhân | Truy vết được phác đồ, version, người áp dụng |

## Key Workflows
### F01. Nhập và ánh xạ quy trình kỹ thuật
1. Admin chọn tạo quy trình.
2. Admin nhập tên, mã, khoa/phòng áp dụng, màn hình/chức năng liên kết.
3. Admin khai báo danh sách bước theo thứ tự.
4. Admin gán người/nhóm vai trò thực hiện từng bước.
5. Admin khai báo điều kiện chuyển bước và thời gian chuẩn.
6. Hệ thống validate mã quy trình, thứ tự bước, quyền role, điều kiện chuyển bước.
7. Hệ thống lưu bản nháp hoặc gửi duyệt.
8. Khi được ban hành, runtime guard dùng quy trình để kiểm soát thao tác.

### F02. Thiết lập phân quyền
1. Admin chọn tài khoản hoặc nhóm tài khoản.
2. Admin gán khoa/phòng, vai trò, phạm vi chức năng.
3. Admin chọn quyền xem/thêm/sửa/xóa/thực hiện/phê duyệt.
4. Hệ thống kiểm tra xung đột quyền và quyền tối thiểu.
5. Hệ thống lưu cấu hình quyền.
6. UI/API chỉ hiển thị và cho thao tác trong phạm vi quyền.
7. Mọi truy cập ngoài phạm vi trả thông báo `Không có quyền truy cập`.

### F03. Điều chỉnh phân quyền
1. Admin chọn tài khoản/nhóm cần điều chỉnh.
2. Hệ thống hiển thị quyền hiện tại.
3. Admin nhập quyền mới, lý do, thời điểm áp dụng.
4. Hệ thống lưu bản ghi thay đổi ở trạng thái pending hoặc applied.
5. Nếu hiệu lực ngay, hệ thống áp quyền và vô hiệu cache phiên làm việc liên quan.
6. Nếu hiệu lực tương lai, scheduler áp quyền đúng thời điểm.
7. Hệ thống gửi thông báo cho người bị ảnh hưởng.
8. Audit log lưu đầy đủ trước/sau.

### F04. Soạn thảo, duyệt và ban hành quy trình
1. Quản trị/Lãnh đạo soạn thảo quy trình hoặc cập nhật version mới.
2. Hệ thống lưu metadata, nội dung chi tiết, tài liệu SOP, thời hạn hiệu lực.
3. Người có thẩm quyền phê duyệt.
4. Hệ thống chuyển version mới thành active theo ngày ban hành/hiệu lực.
5. Version cũ chuyển archived/superseded, vẫn truy xuất được.
6. Nhân viên liên quan nhận thông báo và xem trực tiếp trên hệ thống.

### F05. Danh mục kỹ thuật và định mức
1. Admin nhập mã/tên/loại kỹ thuật.
2. Admin khai báo thiết bị, vật tư, thuốc/hóa chất, đơn vị tính, số lượng chuẩn.
3. Hệ thống liên kết kho, dược, trang thiết bị.
4. Khi bác sĩ chỉ định kỹ thuật, hệ thống kiểm tra tồn kho và trạng thái thiết bị.
5. Nếu thiếu, hệ thống cảnh báo và ghi log.
6. Khi thực hiện, kỹ thuật viên ghi nhận tiêu thụ thực tế.
7. Báo cáo so sánh định mức và tiêu thụ thực tế được tạo theo kỳ.

### F06. Quy trình chăm sóc, phác đồ, phẫu thuật, thủ thuật
1. Admin/Lãnh đạo tạo quy trình theo loại.
2. Khai báo đối tượng áp dụng: ICD, nhóm tuổi, giới, khoa, trạng thái bệnh nhân.
3. Khai báo bước tuần tự, thuốc/vật tư chuẩn bị, nhân sự, chống chỉ định.
4. Hệ thống version hóa và liên kết khám bệnh, nội trú, phẫu thuật.
5. Khi có chẩn đoán ICD, hệ thống gợi ý phác đồ phù hợp.
6. Bác sĩ/điều dưỡng chọn áp dụng, hệ thống lưu version áp dụng cho bệnh nhân.
7. Các bước thực hiện được kiểm soát bằng quyền và quy trình active.

## Data Objects
| Object | Main Fields |
|---|---|
| ProfessionalProcedure | code, name, type, status, departmentScope, effectiveFrom, effectiveTo |
| ProcedureVersion | procedureId, version, issuedDate, issuedBy, status, changeReason |
| ProcedureStep | versionId, sequence, name, actorRole, transitionCondition, standardDuration |
| ProcedureScreenMapping | versionId, screenCode, featureCode, enforcementMode |
| RolePermission | roleId, featureCode, canView, canCreate, canUpdate, canDelete, canApprove |
| PermissionChangeLog | targetUserOrGroup, beforeJson, afterJson, reason, changedBy, effectiveAt |
| TechnicalServiceCatalog | code, name, type, departmentScope, status |
| ResourceNorm | technicalServiceId, resourceType, resourceCode, unit, standardQuantity |
| ClinicalProtocol | code, name, protocolType, icdScope, contraindications, status |
| PatientProtocolApplication | patientId, protocolVersionId, appliedBy, appliedAt, outcome |

## Non-Functional Requirements
| Category | Requirement |
|---|---|
| Security | RBAC enforced at UI, API, service, and data scope |
| Audit | Immutable logs for permission, version, workflow deviation, protocol application |
| Performance | Permission check p95 under 100 ms with cache; audit write async-safe |
| Availability | Runtime guard must fail closed for dangerous actions and fail visible for read-only views |
| Compliance | Store issuer, effective dates, SOP attachments, historical versions |
| Data integrity | Only one active version per procedure scope and effective period |

## Out Of Scope For First Release
| Item | Reason |
|---|---|
| AI tự sinh phác đồ | Cần hội đồng chuyên môn và kiểm định y khoa |
| Tự động trừ kho thực tế | Phụ thuộc phân hệ kho/dược hiện hữu |
| Ký số pháp lý | Cần hạ tầng CA/chữ ký số |
| Tích hợp HIS/EMR cụ thể | Chưa có thông tin stack/API hiện tại |

## Acceptance Summary
Module đạt yêu cầu khi admin cấu hình được quy trình và quyền, quy trình được ban hành theo version, người dùng bị giới hạn đúng vai trò, thao tác lệch quy trình bị cảnh báo/chặn, tồn kho được kiểm tra khi chỉ định kỹ thuật, phác đồ được gợi ý theo ICD, và mọi thay đổi quan trọng có audit log.

## Unresolved Questions
| Question | Impact |
|---|---|
| Stack backend/frontend/database hiện tại là gì? | Cần để chuyển plan sang code thực tế |
| Danh sách màn hình/chức năng hiện hữu gồm những mã nào? | Cần để ánh xạ quy trình và quyền |
| Quy trình duyệt có 1 cấp hay nhiều cấp? | Ảnh hưởng workflow ban hành |
| Khi lệch quy trình sẽ chặn cứng hay chỉ cảnh báo? | Ảnh hưởng runtime guard |
| Tồn kho/dược/thiết bị đã có API nào? | Ảnh hưởng tích hợp F05 |
