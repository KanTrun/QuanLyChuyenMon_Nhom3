# System Architecture

## Telemedicine Landing Page Track
Ngày 2026-05-14, repo có thêm một ứng dụng Blazor Web App riêng cho trang chủ khám từ xa tại `src/telemedicine-landing-page`.

| Area | Decision |
|---|---|
| Runtime | ASP.NET Core Blazor Web App, `net9.0` |
| Rendering | SSR-first Razor Components, không phụ thuộc JS cho nội dung chính |
| Content source | `LandingPageContentService` seed dữ liệu chuyên khoa, chỉ số, tín hiệu tin cậy |
| CTA config | `LandingPageLinks` trong `appsettings.json` |
| Data persistence | Không lưu dữ liệu người bệnh trong scope landing page |
| UI system | CSS token theo màu y tế dịu, Figtree/Noto Sans, motion nhẹ có `prefers-reduced-motion` |

Landing page hiện chạy cùng Blazor app với module QLCM Pro. Nếu sau này tích hợp đặt lịch, tư vấn thật hoặc hồ sơ người bệnh, cần thêm API/server-side authorization và review tuân thủ dữ liệu y tế trước khi lưu PHI.

## QLCM Pro SQL-Backed Architecture
Ngày 2026-05-19, module quản lý quy trình kỹ thuật chuyên môn đã được triển khai trong `src/telemedicine-landing-page` theo hướng SQL-backed.

| Layer | Decision |
|---|---|
| UI | Razor Components trong `Components/Pages`, tổ chức theo Admin, Procedure, Resource, Order, Clinical, Notification |
| Application services | Các service admin dùng `IMedDataStore`, `IProcedureLifecycleService`, `ICurrentUserContext`, `IToastService` |
| Persistence | Entity Framework Core `MedDbContext` map schema `MedicalProcedureManagement` |
| SQL facade | `IMedDataStore` che chi tiết query/mutation cho identity, permissions, procedures, catalog, patients, orders, protocols, notifications |
| Seed data | `scripts/seed-realistic-data.sql` nạp dữ liệu demo/QA có lookup hợp lệ |
| Version lifecycle | `procedure_versions.status_code` dùng `draft`, `pending_approval`, `active`, `superseded`, `archived` |

## Procedure Module Architecture
Kiến trúc dưới đây là logic đang được hiện thực trong module QLCM Pro của Blazor app.

## Notes
For the procedure/RBAC module only:
Các tích hợp kho/dược/thiết bị và auth production vẫn đi qua boundary service để giữ UI và data store không phụ thuộc trực tiếp hệ thống ngoài.

## Logical Components
| Component | Responsibility |
|---|---|
| Procedure Management | CRUD quy trình, bước, tài liệu, version, ban hành |
| Workflow Runtime Guard | Kiểm tra thao tác có đúng quy trình active hay không |
| RBAC/ABAC Permission Service | Kiểm tra quyền theo user, nhóm, vai trò, khoa/phòng, chức năng |
| Permission Change Service | Điều chỉnh quyền, hiệu lực tức thời/tương lai, log trước/sau |
| Technical Catalog Service | Quản lý danh mục kỹ thuật và định mức tài nguyên |
| Inventory Adapter | Kết nối kho, dược, thiết bị để kiểm tra khả dụng |
| Clinical Protocol Service | Quản lý phác đồ/chăm sóc/phẫu thuật/thủ thuật, gợi ý theo ICD |
| Notification Service | Gửi thông báo thay đổi quyền, quy trình mới, cảnh báo thiếu nguồn lực |
| Audit Service | Lưu log bất biến cho permission, version, workflow, protocol |
| Reporting Service | Báo cáo tiêu thụ thực tế so với định mức, lệch quy trình, thay đổi quyền |

## Data Model
| Table | Purpose |
|---|---|
| departments | Khoa/phòng áp dụng |
| users | Tài khoản người dùng |
| groups | Nhóm tài khoản |
| roles | Vai trò nghiệp vụ |
| user_group_members | Thành viên nhóm |
| permissions | Danh mục quyền theo màn hình/chức năng và thao tác |
| role_permissions | Quyền mặc định theo vai trò |
| group_permissions | Quyền kế thừa theo nhóm người dùng |
| user_permission_overrides | Quyền bổ sung/thu hồi riêng theo user |
| permission_change_logs | Log thay đổi quyền trước/sau |
| professional_procedures | Quy trình gốc |
| procedure_versions | Version của quy trình |
| procedure_steps | Bước thực hiện tuần tự |
| procedure_screen_mappings | Ánh xạ quy trình với màn hình/chức năng |
| procedure_attachments | SOP, hướng dẫn kỹ thuật, file đính kèm |
| workflow_action_logs | Log thao tác theo quy trình và lệch quy trình |
| technical_services | Danh mục kỹ thuật chuyên môn |
| technical_resource_norms | Định mức vật tư, thuốc, hóa chất, thiết bị |
| resource_availability_snapshots | Snapshot kiểm tra tồn kho/thiết bị khi chỉ định |
| actual_resource_usages | Tiêu thụ thực tế khi thực hiện kỹ thuật |
| clinical_protocols | Quy trình chăm sóc, phác đồ, phẫu thuật, thủ thuật |
| clinical_protocol_versions | Version phác đồ/quy trình lâm sàng |
| protocol_applicability_rules | ICD, nhóm bệnh nhân, điều kiện áp dụng/chống chỉ định |
| patient_protocol_applications | Lịch sử áp dụng phác đồ cho bệnh nhân |
| notifications | Thông báo hệ thống |
| audit_logs | Audit bất biến dùng chung |

## Core Relationships
| Relationship | Rule |
|---|---|
| professional_procedures 1-n procedure_versions | Mỗi quy trình có nhiều version |
| procedure_versions 1-n procedure_steps | Version chứa danh sách bước có thứ tự |
| procedure_versions 1-n procedure_screen_mappings | Version active kiểm soát chức năng liên kết |
| roles n-n permissions | Role có nhiều quyền, quyền dùng lại nhiều role |
| users n-n user_groups | User kế thừa quyền từ nhóm |
| technical_services 1-n technical_resource_norms | Mỗi kỹ thuật có nhiều định mức |
| clinical_protocol_versions 1-n protocol_applicability_rules | Version có nhiều rule áp dụng/chống chỉ định |
| patients n-n clinical_protocol_versions | Lưu qua patient_protocol_applications |

## Runtime Permission Flow
1. User gửi request hoặc mở màn hình.
2. UI đọc danh sách quyền để ẩn chức năng không hợp lệ.
3. API nhận request và gọi Permission Service.
4. Permission Service tính quyền từ vai trò, nhóm, override user và scope khoa/phòng.
5. Khi nhiều nguồn cùng cấp quyền, hệ thống chọn theo thứ tự SQL: priority cao hơn, deny thắng khi hòa, source user override > group > role, rồi effective_from mới hơn.
6. Nếu không có quyền, API trả `403 Không có quyền truy cập`.
7. Nếu có quyền, request chuyển sang Workflow Runtime Guard.
8. Guard kiểm tra quy trình active đã ánh xạ với màn hình/chức năng.
9. Guard xác định bước hiện tại, role được phép, điều kiện chuyển bước, SLA.
10. Nếu lệch quy trình, hệ thống cảnh báo hoặc chặn theo `enforcementMode`.
11. Hệ thống ghi workflow_action_logs và audit_logs.

## Audit Flow
1. Mọi mutation qua `MedDbContext.SaveChanges` được quét từ ChangeTracker.
2. Entity nghiệp vụ ở trạng thái Added/Modified/Deleted sinh audit log tự động với `target_type`, `target_id`, `before_json`, `after_json`.
3. Các nghiệp vụ có ý nghĩa riêng như đăng nhập, gửi duyệt, phê duyệt, ban hành, từ chối vẫn ghi thêm action nghiệp vụ chuyên biệt.
4. `audit_logs` là append-only; trigger SQL chặn UPDATE/DELETE.
5. UI `/admin/nhat-ky` hiển thị toàn bộ log và JSON trước/sau, còn tab lịch sử phân quyền lọc các target liên quan quyền.

## Procedure Versioning Flow
1. Tạo version draft từ quy trình mới hoặc copy version active.
2. Người soạn cập nhật nội dung, bước, mapping, tài liệu.
3. Người duyệt kiểm tra và approve.
4. Hệ thống set version mới `active` theo `effectiveFrom`.
5. Version cũ chuyển `superseded` khi cùng scope và còn hiệu lực.
6. Các thao tác sau thời điểm hiệu lực dùng version mới.
7. Lịch sử thao tác cũ vẫn tham chiếu version đã dùng tại thời điểm đó.

## Permission Change Flow
1. Admin tạo change request gồm target, before, after, reason, effectiveAt.
2. Hệ thống validate người thực hiện có quyền quản trị tương ứng.
3. Nếu `effectiveAt <= now`, quyền mới được áp ngay.
4. Nếu `effectiveAt > now`, bản ghi ở trạng thái scheduled.
5. Scheduler áp quyền khi đến hạn.
6. Permission cache của user/nhóm bị ảnh hưởng bị invalidated.
7. Notification gửi tới người bị ảnh hưởng.
8. Audit log lưu immutable event.

## Technical Catalog Integration Flow
1. Khi chỉ định kỹ thuật, Service lấy định mức active theo mã kỹ thuật.
2. Inventory Adapter gọi phân hệ kho/dược/thiết bị.
3. Hệ thống tạo snapshot khả dụng tại thời điểm chỉ định.
4. Nếu thiếu thuốc/vật tư/thiết bị, cảnh báo hiển thị cho người chỉ định.
5. Khi kỹ thuật hoàn thành, hệ thống ghi nhận tiêu thụ thực tế.
6. Reporting Service so sánh thực tế với định mức theo kỳ, khoa, kỹ thuật.

## Clinical Protocol Suggestion Flow
1. Bác sĩ nhập/chọn chẩn đoán ICD.
2. Clinical Protocol Service tìm protocol version active có rule phù hợp.
3. Hệ thống loại trừ protocol có chống chỉ định theo thông tin bệnh nhân có sẵn.
4. UI hiển thị danh sách gợi ý kèm lý do phù hợp.
5. Bác sĩ chọn áp dụng hoặc bỏ qua và nhập lý do nếu cần.
6. Hệ thống lưu patient_protocol_applications với version chính xác.

## API Surface Draft
| Endpoint | Purpose |
|---|---|
| `GET /procedures` | Tra cứu quy trình |
| `POST /procedures` | Tạo quy trình |
| `POST /procedures/{id}/versions` | Tạo version mới |
| `POST /procedure-versions/{id}/submit` | Gửi duyệt |
| `POST /procedure-versions/{id}/approve` | Ban hành |
| `GET /permissions/effective?userId=` | Lấy quyền hiệu lực |
| `POST /permissions/changes` | Tạo thay đổi phân quyền |
| `GET /technical-services` | Tra cứu danh mục kỹ thuật |
| `POST /technical-services/{id}/resource-norms` | Cấu hình định mức |
| `POST /technical-orders/{id}/check-resources` | Kiểm tra tồn kho/thiết bị |
| `GET /clinical-protocols/suggestions?patientId=&icd=` | Gợi ý phác đồ |
| `POST /patients/{id}/protocol-applications` | Lưu áp dụng phác đồ |

## Security Controls
| Control | Requirement |
|---|---|
| Server-side authorization | Không tin UI, mọi API phải check quyền |
| Scoped administration | Admin chỉ điều chỉnh quyền trong phạm vi được giao |
| Immutable audit | Không cho sửa/xóa audit log qua UI |
| Version integrity | Không sửa trực tiếp version active; tạo version mới |
| Attachment safety | Kiểm tra loại file, kích thước, virus scan nếu có hạ tầng |
| Sensitive patient data | Protocol application log phải theo quyền hồ sơ bệnh án |

## Unresolved Questions
None.
