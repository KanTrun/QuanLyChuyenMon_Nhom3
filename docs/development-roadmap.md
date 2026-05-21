# Development Roadmap

## Current Source Status
Source hiện tại là Blazor Web App `net9.0` tại `src/telemedicine-landing-page`, gồm landing page và module QLCM Pro. Module QLCM Pro đã chuyển từ demo/in-memory sang luồng SQL-backed qua `MedDbContext`, `IMedDataStore` và schema `MedicalProcedureManagement`.

## QLCM Pro SQL Track
| Item | Status | Output |
|---|---|---|
| SQL data access | Complete | `IMedDataStore` có đủ create/update/remove cho identity, permissions, procedures, patients, technical catalog, orders, protocols và notifications |
| Procedure lifecycle | Complete | Version dùng lookup SQL `draft`, `pending_approval`, `active`, `superseded`, `archived` |
| Admin workflows | Complete | Quy trình, tổ chức/khoa/phòng, người dùng, vai trò, nhóm, phân quyền, danh mục, tài nguyên, phác đồ, thông báo |
| Clinical workflows | Complete | Bệnh nhân, lượt khám, phác đồ áp dụng, chỉ định kỹ thuật, snapshot nguồn lực và tiêu hao thực tế |
| Seed data | Complete | `scripts/seed-realistic-data.sql` nạp dữ liệu mẫu thực tế cho demo/QA |
| Verification | Complete | Release build/test passed, 109 tests passed; Docker web build and `/login` HTTP check passed |

## Telemedicine Landing Page Track
| Item | Status | Output |
|---|---|---|
| Stack scaffold | Complete | `telemedicine-landing-page.sln`, Blazor Web App `net9.0`, xUnit tests |
| Home UI | Complete | Hero, preview tư vấn video, danh bạ chuyên khoa, theo dõi sức khỏe, CTA tải ứng dụng |
| Vietnamese content | Complete | UI copy, aria labels, metadata and seed data use Vietnamese with diacritics |
| Verification | Complete | Release build passed, 4 tests passed |
| Next | Planned | Replace placeholder CTA/store/contact URLs and run browser/Lighthouse QA |

## Procedure Module Roadmap
Roadmap cho module quản lý quy trình kỹ thuật chuyên môn. Track này đã có source triển khai trong Blazor app và đã được kiểm tra bằng build/test Release.

## Milestones
| Phase | Name | Status | Target Output |
|---|---|---|---|
| 01 | Domain foundation and RBAC | Complete | Schema/domain model, permission model, SQL data-store operations |
| 02 | Procedure versioning and mapping | Complete | CRUD quy trình, version, steps, screen mapping, attachment, lifecycle |
| 03 | Permission configuration and change history | Complete | Role/group permission, user override, effective rights UI, change-log base |
| 04 | Technical catalog and inventory checks | Complete | Danh mục kỹ thuật, tài nguyên, định mức, snapshot khả dụng, tiêu hao thực tế |
| 05 | Clinical protocols and patient application | Complete | Phác đồ, version, rule ICD, liên kết quy trình, lịch sử áp dụng cho bệnh nhân |
| 06 | Notification, reporting, testing, rollout | Complete | Thông báo, preference, delivery attempts, report data paths, build/test Release |

## Progress
| Area | Progress |
|---|---|
| Requirement analysis | 100% |
| Architecture blueprint | 100% |
| Implementation plan | 100% |
| Code implementation | 100% for implementation-plan scope |
| Test automation | Release build/test passed, 109 tests |
| Deployment readiness | Docker Compose web/sql/db-init verified locally; target DB execution/config remains deployment task |

## Dependencies
| Dependency | Needed For |
|---|---|
| Existing procedure-module source | Code implementation for legacy procedure track |
| Auth/user/department data | RBAC and scope |
| Screen/function catalog | Procedure mapping and permission |
| Inventory/pharmacy/equipment APIs | Resource checks |
| Patient/diagnosis data model | Protocol suggestions |

## Next Milestone
Run browser QA by role on target SQL Server: `SYSTEM_ADMIN`, `DEPARTMENT_ADMIN`, clinical user, technician/pharmacist and report viewer.

## Unresolved Questions
None.
