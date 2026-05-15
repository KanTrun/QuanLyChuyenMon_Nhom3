# Development Roadmap

## Current Source Status
Source hiện tại là landing page Blazor tại `src/telemedicine-landing-page`. Các phần roadmap về quy trình/RBAC bên dưới là legacy planning track, không phản ánh trạng thái source landing page.

## Telemedicine Landing Page Track
| Item | Status | Output |
|---|---|---|
| Stack scaffold | Complete | `telemedicine-landing-page.sln`, Blazor Web App `net9.0`, xUnit tests |
| Home UI | Complete | Hero, preview tư vấn video, danh bạ chuyên khoa, theo dõi sức khỏe, CTA tải ứng dụng |
| Vietnamese content | Complete | UI copy, aria labels, metadata and seed data use Vietnamese with diacritics |
| Verification | Complete | Release build passed, 4 tests passed |
| Next | Planned | Replace placeholder CTA/store/contact URLs and run browser/Lighthouse QA |

## Legacy Procedure Module Roadmap
Roadmap cho module quản lý quy trình kỹ thuật chuyên môn. Vì workspace chưa có source ứng dụng, trạng thái hiện tại là planning-ready, chưa code-ready.

## Milestones
| Phase | Name | Status | Target Output |
|---|---|---|---|
| 01 | Domain foundation and RBAC | Planned | Schema/domain model, permission model, audit baseline |
| 02 | Procedure versioning and mapping | Planned | CRUD quy trình, version, steps, screen mapping |
| 03 | Permission configuration and change history | Implemented (in-memory demo) | UI/API cấu hình quyền, effectiveAt, notification. Persistence layer pending production rollout. |
| 04 | Technical catalog and inventory checks | Implemented (in-memory demo) | Định mức tài nguyên, adapter kho/dược/thiết bị, báo cáo. Persistence layer pending production rollout. |
| 05 | Clinical protocols and patient application | Implemented (in-memory demo) | Phác đồ, rule ICD, chống chỉ định, lịch sử bệnh nhân. Persistence layer pending production rollout. |
| 06 | Notification, reporting, testing, rollout | Planned | Báo cáo, test, migration, tài liệu vận hành |

## Progress
| Area | Progress |
|---|---|
| Requirement analysis | 100% |
| Architecture blueprint | 100% |
| Implementation plan | 100% |
| Code implementation | 0% for procedure module; landing page complete |
| Test automation | 0% for procedure module; landing page has 4 tests |
| Deployment readiness | 0% |

## Dependencies
| Dependency | Needed For |
|---|---|
| Existing procedure-module source | Code implementation for legacy procedure track |
| Auth/user/department data | RBAC and scope |
| Screen/function catalog | Procedure mapping and permission |
| Inventory/pharmacy/equipment APIs | Resource checks |
| Patient/diagnosis data model | Protocol suggestions |

## Next Milestone
Landing page next step: replace placeholder CTA/store/contact URLs and run browser/Lighthouse QA. Procedure module Phase 01 can start when actual backend/frontend/database stack for that module is confirmed.

## Unresolved Questions
| Question | Impact |
|---|---|
| Repo ứng dụng thực tế nằm ở đâu? | Không thể viết code nếu thiếu source |
| Có cần tạo scaffold app mới không? | Ảnh hưởng phạm vi lớn |
