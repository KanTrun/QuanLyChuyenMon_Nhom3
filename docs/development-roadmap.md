# Development Roadmap

## Current Source Status
Source hiện tại là Blazor Web App `net9.0` tại `src/telemedicine-landing-page`, chạy trọng tâm QLCM Pro. Landing page telemedicine cũ đã được gỡ khỏi runtime; `/` là trang giới thiệu QLCM Pro trước đăng nhập/đăng ký, còn module nghiệp vụ dùng SQL-backed qua `MedDbContext`, `IMedDataStore` và schema `MedicalProcedureManagement`.

## QLCM Pro SQL Track
| Item | Status | Output |
|---|---|---|
| SQL data access | Complete | `IMedDataStore` có đủ create/update/remove cho identity, permissions, procedures, patients, technical catalog, orders, protocols và notifications |
| Procedure lifecycle | Complete | Version dùng lookup SQL `draft`, `pending_approval`, `active`, `superseded`, `archived` |
| Admin workflows | Complete | Quy trình, tổ chức/khoa/phòng, người dùng, vai trò, nhóm, phân quyền, danh mục, tài nguyên, phác đồ, thông báo |
| Clinical workflows | Complete | Bệnh nhân, lượt khám, phác đồ áp dụng, chỉ định kỹ thuật, export HTML hồ sơ, snapshot nguồn lực và tiêu hao thực tế |
| Audit remediation | Complete | Action guard, session restore, protocol draft/submit/publish, confirm/reject reason, audit history, dashboard/report/audit UX fixes |
| Production architecture foundation | Complete | Identity-compatible account tables, custom permission claims/policies, SQL retry/health checks, Serilog structured logging, Docker migration runner |
| Security validation guard | Complete | FluentValidation password commands, common-password checks, null-password lock migration, admin layout/NavGate route guard |
| Workflow and jobs foundation | Complete | Workflow guards for procedure versions/orders, order status service extraction, Hangfire SQL dashboard and `IJobService` wrapper |
| Realtime notifications | Complete | SignalR notification hub, persisted user/group/broadcast notification service, admin bell live toast updates and presence hooks |
| Business UI remediation | Complete | Admin pages use hospital-facing labels, approval-backed permission changes, active-version drafts, resource-unit guards and compact technical details |
| Business workflow completion | Complete | Runtime procedure guard, scheduled permission apply job, inventory snapshot service, ICD protocol suggestions and persona route gating |
| Grounded chatbot safety | Complete | Core QLCM knowledge catalog, permission-scoped aggregate context, local privacy guard, header-based Gemini auth, per-circuit AI settings and manual user-owned key policy |
| System-wide remediation | Complete | Dark signature visibility, archive filters/lifecycle, collapsed sidebar responsiveness, token-bound realtime session, professional clinical dossier export, Docker chatbot config |
| Internal procedure documents | Complete | Professional authoring, three-column source-faithful flowcharts, compact paginated A4 export, drawn internal writer/checker/approver signoffs and immutable version updates `v01`, `v02`... |
| Seed data | Complete | `scripts/seed-realistic-data.sql` nạp dữ liệu mẫu thực tế cho demo/QA |
| Verification | Complete | Release build `0 warnings, 0 errors`; full solution `242/242`; signed procedure PDF preview and Docker runtime verified |

## Legacy Landing Cleanup Track
| Item | Status | Output |
|---|---|---|
| Runtime removal | Complete | Removed landing sections/content service/link options/CSS from active app |
| Entry route | Complete | `/` shows a QLCM Pro intro with login/register CTAs so users see the professional product entry first |
| Verification | Complete | Covered by current Release verification: full solution `192/192`; Docker Compose config valid |

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
| Test automation | Release build `0 warnings, 0 errors`; full solution `242/242` |
| Deployment readiness | Docker Compose config valid; web/sql/db-init verified locally; drawn internal procedure signing, immutable versioning and compact A4 export enabled |

## Dependencies
| Dependency | Needed For |
|---|---|
| Existing procedure-module source | Code implementation for legacy procedure track |
| Auth/user/department data | RBAC and scope |
| Screen/function catalog | Procedure mapping and permission |
| Inventory/pharmacy/equipment APIs | Resource checks |
| Patient/diagnosis data model | Protocol suggestions |

## Next Milestone
Complete OCR and page-by-page visual verification for all imported KSNK scans, then run browser QA by role on target SQL Server.

## Unresolved Questions
None.
