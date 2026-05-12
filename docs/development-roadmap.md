# Development Roadmap

## Overview
Roadmap cho module quản lý quy trình kỹ thuật chuyên môn. Vì workspace chưa có source ứng dụng, trạng thái hiện tại là planning-ready, chưa code-ready.

## Milestones
| Phase | Name | Status | Target Output |
|---|---|---|---|
| 01 | Domain foundation and RBAC | Planned | Schema/domain model, permission model, audit baseline |
| 02 | Procedure versioning and mapping | Planned | CRUD quy trình, version, steps, screen mapping |
| 03 | Permission configuration and change history | Planned | UI/API cấu hình quyền, effectiveAt, notification |
| 04 | Technical catalog and inventory checks | Planned | Định mức tài nguyên, adapter kho/dược/thiết bị, báo cáo |
| 05 | Clinical protocols and patient application | Planned | Phác đồ, rule ICD, chống chỉ định, lịch sử bệnh nhân |
| 06 | Notification, reporting, testing, rollout | Planned | Báo cáo, test, migration, tài liệu vận hành |

## Progress
| Area | Progress |
|---|---|
| Requirement analysis | 100% |
| Architecture blueprint | 100% |
| Implementation plan | 100% |
| Code implementation | 0% |
| Test automation | 0% |
| Deployment readiness | 0% |

## Dependencies
| Dependency | Needed For |
|---|---|
| Existing application source | Code implementation |
| Auth/user/department data | RBAC and scope |
| Screen/function catalog | Procedure mapping and permission |
| Inventory/pharmacy/equipment APIs | Resource checks |
| Patient/diagnosis data model | Protocol suggestions |

## Next Milestone
Phase 01 can start when actual backend/frontend/database stack is available.

## Unresolved Questions
| Question | Impact |
|---|---|
| Repo ứng dụng thực tế nằm ở đâu? | Không thể viết code nếu thiếu source |
| Có cần tạo scaffold app mới không? | Ảnh hưởng phạm vi lớn |
