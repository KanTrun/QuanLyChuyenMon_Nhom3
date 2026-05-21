# Project Changelog

## 2026-05-21
### Audit Remediation
| Item | Description |
|---|---|
| Action-level permission guard | Added centralized `AdminActionGuard` and applied mutation checks across procedures, protocols, catalog, resources, orders, clinical, organization and permission approval pages |
| Session persistence | Login now persists the current user in browser session storage and admin/persona layouts restore the user before route denial after refresh |
| Approval workflows | Clinical protocols now start as draft and require submit then approve/publish; procedure/protocol archive/restore and order actions write explicit audit events |
| Confirmation and rejection UX | Added confirmation for destructive/logout/status operations and replaced hardcoded permission rejection reason with reviewer-entered reason |
| Admin data correctness | Dashboard/report/audit pages now use real active-version counts, resolved target labels, friendly protocol rule summaries and no placeholder chart |
| Account administration | Added reset-password action, registration notification to admins and basic login lockout for repeated failed attempts |

### Fixed
| Item | Description |
|---|---|
| Login circuit crash | Removed duplicate `/admin/lam-sang` route ownership from the clinical workspace page so Blazor Router no longer terminates the circuit on `/login`; added a duplicate-route regression test |
| Registration activation UX | Login now distinguishes correctly saved-but-inactive registrations from invalid credentials, and user management defaults to showing all accounts so new registrations are visible to admins |

### Changed
| Item | Description |
|---|---|
| Admin route ownership | `/admin/quy-trinh`, `/admin/quy-trinh/phe-duyet`, `/admin/phac-do` and `/admin/lam-sang` now render SQL-backed admin pages; `/quy-trinh-pro` and `/phac-do-pro` remain workspace routes |
| Preferences sync | Theme and motion preferences now use explicit `ThemeBus` events and shell JS state so header/settings stay synchronized |
| Admin CRUD filters | Added soft-archive aware filters and CRUD flows for procedures, approvals, protocols, orders, users, reports and clinical applications |
| Audit log UX | Audit page now shows Vietnamese business descriptions by default, with raw JSON hidden inside technical details |
| Time display | Added shared Vietnam-time formatter for admin timestamps |

### Added
| Item | Description |
|---|---|
| Routing and preference tests | Added route ownership, unknown admin route denial and ThemeBus set-theme/motion tests |

### Verification
| Command | Result |
|---|---|
| `dotnet build .\telemedicine-landing-page.sln -c Release` | Passed, 0 warnings, 0 errors |
| `dotnet test .\telemedicine-landing-page.sln -c Release` | Passed, 109 tests |
| `docker compose build web` | Passed |
| `docker compose up -d web` + `/admin` check | Passed, container healthy and HTTP 200 |
| `dotnet build .\telemedicine-landing-page.sln -c Release --no-restore` | Passed after audit remediation, 0 warnings, 0 errors |
| `dotnet test .\telemedicine-landing-page.sln -c Release --no-build` | Passed after audit remediation, 109 tests |

### Unresolved Questions
None.

## 2026-05-20
### Changed
| Item | Description |
|---|---|
| User activation recovery | Trang Người dùng có nút kích hoạt lại tài khoản đã vô hiệu hóa và chặn tự vô hiệu hóa tài khoản đang đăng nhập |
| Settings SQL sync | Trang Cài đặt đọc/ghi hồ sơ từ user SQL hiện tại, dùng khoa/phòng thật và lưu kênh thông báo vào `notification_preferences` |
| Unified guarded navigation | Added SQL permission route guard for admin/persona routes, command palette and hotkeys; expanded nav to resources, orders, notifications, approval, screens and profile |
| SQL notification shell | Top-bar badge/previews now read `med.notifications` for current user only; legacy in-memory notification service removed from production DI |
| Legacy route cleanup | Removed routable legacy mock routes `/admin/lam-sang-legacy`, `/admin/quy-trinh-legacy`, `/admin/quy-trinh/phe-duyet-legacy`; removed production DI for legacy in-memory procedure/clinic/catalog/permission/protocol services |
| Consumption report filter | `SqlReportService` now filters by SQL `department_id` and department closure tree, using `actual_resource_usages` plus resource norms |
| No fake availability | Order resource snapshots no longer invent available stock; status is `unknown` until real inventory source exists |
| Notification and protocol UI | Notification detail resolves business source names instead of raw source ID; protocol type uses lookup names |
| Register hardening | Public register creates inactive users that require admin activation |

### Added
| Item | Description |
|---|---|
| Guard/report tests | Added tests for direct route denial, case-insensitive SQL permission, nav filtering, notification ownership, department report filter and no legacy routes |

### Verification
| Command | Result |
|---|---|
| `dotnet build .\telemedicine-landing-page.sln -c Release --no-restore` | Passed, 0 warnings, 0 errors |
| `dotnet test .\telemedicine-landing-page.sln -c Release --no-restore` | Passed, 97 tests |

### Unresolved Questions
None.

## 2026-05-19
### Added
| Item | Description |
|---|---|
| QLCM Pro SQL-backed workflows | Hoàn thiện các màn hình quản trị/lâm sàng dùng `IMedDataStore` thật: quy trình, danh mục kỹ thuật, tài nguyên, chỉ định kỹ thuật, bệnh nhân, thông báo, nhóm người dùng, phác đồ và ghi đè phân quyền cá nhân |
| Mutable data-store operations | Bổ sung các hàm create/update/remove còn thiếu cho role/group/permission, procedure mapping/attachment, patient/encounter, technical service/norm/order, protocol/rule, notification preference và read state |
| Realistic seed data | Thêm `scripts/seed-realistic-data.sql` để nạp dữ liệu mẫu thực tế cho khoa phòng, người dùng, vai trò, quyền, quy trình, dịch vụ kỹ thuật, định mức, bệnh nhân, chỉ định, phác đồ và thông báo |
| Lifecycle lookup alignment | Đồng bộ trạng thái version theo lookup SQL: `draft`, `pending_approval`, `active`, `superseded`, `archived` |
| Test seed compatibility | Khôi phục seed in-memory legacy cho các test hiện hữu trong khi UI chính dùng SQL-backed data store |
| Organization navigation | Thêm nhánh `Tổ chức` trong sidebar cho Khoa/Phòng, Người dùng, Vai trò và Nhóm |
| Automatic audit trail | `MedDbContext.SaveChanges` tự ghi audit cho mọi bản ghi nghiệp vụ được tạo/sửa/xóa, kể cả khi UI gọi trực tiếp `Db.SaveChanges()` |

### Changed
| Item | Description |
|---|---|
| Admin procedure creation | Trang tạo mới quy trình lưu đầy đủ procedure, version, steps, resource norms, screen mappings và attachments |
| Permission administration | Trang phân quyền có thêm tab ghi đè cá nhân, hỗ trợ thêm/sửa/xóa override theo user |
| RBAC scope logic | `EffectivePermissionResolver` khớp hàm SQL `fn_user_has_permission_itvf`: lọc theo khoa/phòng, role/group scope, priority, deny và source rank |
| Department and group UX | Trang phân quyền hiển thị tab Khoa/Phòng và Nhóm; trang Khoa/Phòng hỗ trợ xem cây, sửa và lưu trữ đơn vị |
| Role management CRUD | Trang Vai trò hỗ trợ thêm, sửa và lưu trữ vai trò tùy chỉnh; khi lưu trữ vai trò sẽ hết hạn các gán vai trò đang hoạt động |
| Audit log UX | Thêm mục Nhật ký vào sidebar, mở rộng lọc action động và xem JSON trước/sau cho bản ghi audit |
| Clinical operations | Trang lâm sàng hiển thị bệnh nhân, lượt khám, phác đồ áp dụng và chỉ định kỹ thuật theo dữ liệu SQL |
| Notification center | Trung tâm thông báo hỗ trợ lọc mức độ, đánh dấu đã đọc, xem lần gửi và cấu hình preference |
| Profile and lookup hardening | Sửa lỗi lưu hồ sơ với bảng SQL có trigger, làm lại UI hồ sơ theo admin style, thêm seed chuẩn hóa lookup và mở rộng unit catalog |

| SQL Server alignment | Dong bo file SQL chinh voi database that `MedicalProcedureManagement`: bo sung `med.users.password_hash` va doi map dieu huong sang permission code dang co trong `med.permissions` |
| Full SQL permission catalog | Mo rong `MedicalProcedureManagement.sql` de seed day du screen/feature/permission cho 25 route nghiep vu, them base roles va role-permission cho SYSTEM_ADMIN, quan tri khoa, lam sang, ky thuat/duoc va bao cao |
| SQL-backed reports | Chuyen `IReportService` sang `SqlReportService` de bao cao tieu thu, KPI va activity feed doc tu bang SQL that thay vi service demo in-memory |
| SQL-backed admin routes | Gan route admin chinh cua Quy trinh va Lam sang vao cac page doc `IMedDataStore`, chuyen page demo cu sang route `-legacy` |

### Verification
| Command | Result |
|---|---|
| `dotnet build .\telemedicine-landing-page.sln -c Release` | Passed, 0 warnings, 0 errors |
| `dotnet test .\telemedicine-landing-page.sln -c Release --no-build` | Passed, 91 tests |

### Unresolved Questions
None.

## 2026-05-15
### Added
| Item | Description |
|---|---|
| QLCM Pro admin shell | Bố trí lại Blazor app `net9.0` với sidebar điều hướng, top bar, breadcrumb, lệnh nhanh và dashboard riêng cho `/admin` (không che sidebar khi mở palette) |
| Soft-blue theme refresh | Cập nhật `design-tokens.css` sang dải xanh dịu (`#5B9BD5`), giữ nguyên cảm giác editorial của trang công khai và bổ sung `meta theme-color` |
| Modules đầy đủ | Hoàn thiện các trang `Tổng quan`, `Quy trình kỹ thuật`, `Phân quyền`, `Danh mục`, `Phác đồ`, `Báo cáo`, `Lâm sàng`, `Cài đặt` (in-memory + seed data có dấu) |
| AI chatbot | Tích hợp Anthropic Claude (`claude-sonnet-4-5-20250929`) qua `/v1/messages` SSE streaming, có chế độ demo khi chưa cấu hình `ApiKey` |
| Phím tắt | Hỗ trợ `Ctrl + K` mở bảng lệnh, `Alt + 0..6` chuyển nhanh module, `Ctrl + /` bật trợ lý AI, `Esc` đóng modal |
| Accessibility & motion | Tôn trọng `prefers-reduced-motion`, đường viền focus 2px theo `--color-primary`, aria-label tiếng Việt cho mọi nút biểu tượng |
| Vietnamese diacritics audit | Rà soát toàn bộ chuỗi hiển thị, mọi văn bản người dùng nhìn thấy đều có dấu đầy đủ (chỉ giữ chuỗi không dấu trong từ khóa khớp đầu vào của demo client) |
| Docs | Cập nhật `README.md`, `docs/development-roadmap.md` và changelog phản ánh trạng thái mới |

### Verification
| Command | Result |
|---|---|
| `dotnet build .\telemedicine-landing-page.sln -c Release` | Passed, 0 warnings, 0 errors |
| `dotnet test .\telemedicine-landing-page.sln -c Release --no-build` | Passed, 45 tests |

## 2026-05-14
### Added
| Item | Description |
|---|---|
| Telemedicine landing page | Tạo Blazor Web App `net9.0` cho trang chủ khám từ xa của hệ thống bệnh viện |
| Soft healthcare UI | Thêm hero, preview tư vấn video, danh bạ chuyên khoa, theo dõi sức khỏe, CTA tải ứng dụng |
| Vietnamese content | Toàn bộ copy hiển thị chính dùng tiếng Việt có dấu |
| Tests | Thêm xUnit tests cho content service và cấu hình CTA |
| README | Thêm hướng dẫn chạy/build/test dự án |

### Verification
| Command | Result |
|---|---|
| `dotnet build .\telemedicine-landing-page.sln -c Release` | Passed, 0 warnings, 0 errors |
| `dotnet test .\telemedicine-landing-page.sln -c Release --no-build` | Passed, 4 tests |

## 2026-05-11
### Added
| Item | Description |
|---|---|
| PDR | Tạo yêu cầu sản phẩm cho module quản lý quy trình kỹ thuật chuyên môn |
| Architecture | Tạo blueprint logic gồm RBAC, workflow guard, versioning, audit, notification, catalog, protocol |
| Roadmap | Tạo roadmap 6 phase để triển khai tuần tự |
| Implementation plan | Tạo plan chi tiết trong `plans/260511-1815-quan-ly-quy-trinh-ky-thuat-chuyen-mon/` |

### Notes
Workspace chưa có source ứng dụng, chưa có `README.md`, chưa có `.git`, chưa có `docs` trước đó. Thay đổi hiện tại là tài liệu và kế hoạch triển khai.

## Unresolved Questions
| Question | Impact |
|---|---|
| Stack và repo ứng dụng thực tế là gì? | Cần để tiếp tục triển khai code |
