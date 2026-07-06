# Project Changelog

## 2026-06-18
### Added
| Item | Description |
|---|---|
| Drawn internal procedure signatures | Added a reusable procedure signature modal so writer, checker and approver must draw a direct internal signature before the signoff record is saved |
| Procedure signoff evidence validation | Procedure signoffs now reject missing, malformed, unsupported or oversized signature images before storing the content-hash-bound signoff |

### Changed
| Item | Description |
|---|---|
| Compact A4 section flow | Short Roman sections now continue on the same A4 page; long sections split into continuation pages only when needed |
| A4 overflow protection | Long flow descriptions, recipient lists, revision history, attachment lists and signoff logs now create continuation pages instead of being clipped by fixed A4 pages |

### Verification
| Check | Result |
|---|---|
| Build/test | Release build clean with `0 warnings, 0 errors`; `242/242` tests passed |
| Printable PDF | QT.KSNK.09 signed preview exported as 6 A4 pages; Chromium audit found `0` overflowing pages |
| Visual smoke | Cover shows hospital logo and writer/checker/approver signoff blocks; flowchart renders all nine source rows with content inside the shapes |

## 2026-06-15
### Changed
| Item | Description |
|---|---|
| Source-faithful KSNK flowchart | Replaced the detached flow cards with a three-column table matching the scan: `Trách nhiệm`, `Các bước thực hiện`, `Mô tả / Các biểu mẫu`; each step name is rendered inside its semantic flow shape |
| Readable procedure content | Removed visible `OCR_PENDING` markers from seeded sections and steps; the OCR publication gate remains in version metadata and readiness validation |
| QT.KSNK.09 references | Added the page-5 source responsibilities, `5.2.1` through `5.2.8`, `BM.KSNK.09.*` references and appendices to all existing v01/v02 history |

### Fixed
| Item | Description |
|---|---|
| Flowchart A4 pagination | Compacted only the flowchart page so all nine rows and the page footer remain on one A4 sheet without a footer-only spill page |
| Internal signoff labels | Localized writer/checker/approver actions, confirmations and validation errors; user-facing workflow no longer exposes unsigned Vietnamese text or raw role codes |

### Verification
| Check | Result |
|---|---|
| Build/test | Release build clean with `0 warnings, 0 errors`; `230/230` tests passed |
| SQL migration | Docker db-init applied the flow-content migration to 42 procedure steps and 55 document sections |
| Printable PDF | QT.KSNK.09 v02 exported as exactly 15 A4 pages; page 14 contains the complete nine-row flowchart and `Trang 14 / 15` footer |
| Runtime | Docker web healthy on `localhost:8080`; browser preview contains no `OCR_PENDING` or raw `writer` role code |

## 2026-06-14
### Added
| Item | Description |
|---|---|
| Visible procedure print action | Added `In/PDF` to each procedure row and version detail, opening a self-contained A4 preview with hospital branding, document control, Roman sections, flowcharts, attachments and internal signoff evidence |

### Changed
| Item | Description |
|---|---|
| Hospital logo | Replaced the web-wide logo and favicon with `assets/logo_hos.jpg`, including login, registration, landing page, sidebars and printable clinical exports |
| KSNK Vietnamese content | Localized all four seeded KSNK procedures and existing version history, including titles, recipients, revision entries, section headings, flow steps, responsibilities and source PDF names |

### Fixed
| Item | Description |
|---|---|
| A4 cover pagination | Compressed cover spacing so the signature table and footer stay on page 1 without generating a footer-only page |
| Approval readiness labels | Replaced internal role codes with `Người viết`, `Người kiểm tra` and `Người phê duyệt` in printable readiness warnings |

### Verification
| Check | Result |
|---|---|
| Build/test | Release build clean; `229/229` tests passed |
| Printable PDF | Initial paginated QT.KSNK.09 v02 export verified cover, 11 Roman sections, flowchart pages and attachment/signoff trace before the 15-page source-faithful table refinement |
| Version history | Browser smoke confirmed latest v02 remains editable while v01 is preserved in the procedure history |

## 2026-06-13
### Added
| Item | Description |
|---|---|
| Professional procedure authoring | Added issue metadata, 11 Roman sections, distribution, revision tracking, semantic flowchart shapes, attachments and A4 print export |
| Internal signoffs | Added writer, checker and approver confirmation bound to the current procedure content hash |
| Immutable version updates | Added `Cập nhật` workflow that creates `v01`, `v02`, `v03`... while preserving all earlier versions and files |
| KSNK imports | Added four source-scan drafts with authenticated PDF attachments and an OCR gate before publication |

### Changed
| Item | Description |
|---|---|
| Procedure update UI | Replaced the metadata-only save modal with the full professional authoring form prefilled from the latest version |
| Version inheritance | New versions inherit sections, recipients, revision history, flow steps, attachments, resource norms and screen mappings; signoffs are intentionally not inherited |
| Internal-only signing | Removed external provider routes, configuration, scripts and documentation |

### Verification
| Check | Result |
|---|---|
| Build/test | Release build clean; `226/226` tests passed |
| Docker | Rebuilt latest web image; web healthy, `/health` and `/` returned 200, db-init exited 0, browser smoke passed for create `v01` and update `v02` |

## 2026-06-03
### Added
| Item | Description |
|---|---|
| Browser session token | Added Data Protection-backed browser session token restore and SignalR user-group validation |
| Realtime data refresh bridge | Added in-process data-change bus so SQL mutations refresh other active Blazor circuits on one web instance |
| Professional clinical dossier | Added selected-patient A4 export with hospital logo/name, ordered clinical sections, signature evidence and revoked-state handling |
| Docker chatbot config | Added Compose `.env` support for `CHATBOT_BASE_URL` and `CHATBOT_MAX_TOKENS` |

### Changed
| Item | Description |
|---|---|
| Sidebar responsiveness | Collapsed sidebar groups now expose clickable flyouts and avoid delayed navigation feedback |
| Archive lifecycle | Procedure/service/resource/department/group archive filters now show archived records consistently; archived groups become read-only |
| Signature integrity | Drawn PNG metadata is validated and bound into signature verification while keeping legacy verification compatible |
| Chatbot streaming UX | Long answers preserve manual scroll position, show new-content affordance and surface Gemini truncation/safety notices |

### Fixed
| Item | Description |
|---|---|
| Dark signature visibility | Signature pad ink follows dark theme so signatures remain visible |
| Reload access flash | Session restore runs through token validation before admin/persona route denial, avoiding AccessDenied flash on F5 |
| Archived group mutation | Archived groups cannot receive new members or group permissions through UI, SQL store, in-memory store or permission-change apply path |
| Clinical status safety | Signed/revoked clinical application states are protected behind signature workflow guards |
| Docker chatbot availability | Teammates can use chatbot from Docker localhost by configuring Compose environment instead of editing app files |
| Clinical PDF signature fallback | Clinical PDF now renders a visible electronic signature stamp when legacy/demo signature records have no PNG evidence image |
| Clinical PDF drawn signature | Clinical PDF now prints the saved drawn PNG signature whenever signature metadata contains valid image evidence |
| Printable signature ink | Clinical PDF and new signature captures normalize drawn signatures to dark ink so dark-mode signatures remain visible on white paper |
| Professional signature block | Clinical PDF signature evidence now uses a dedicated confirmation block with metadata, signing purpose and a non-overlapping signature area |

### Verification
| Check | Result |
|---|---|
| `dotnet build .\telemedicine-landing-page.sln -c Release --no-restore` | Passed, 0 warnings, 0 errors |
| `dotnet test .\telemedicine-landing-page.sln -c Release --no-build --filter "ArchiveGroup\|Approve_GroupPermissionForArchivedGroup"` | Passed, 3/3 tests |
| `dotnet test .\telemedicine-landing-page.sln -c Release --no-build` | Passed, 219/219 tests |
| `dotnet list .\src\telemedicine-landing-page\telemedicine-landing-page.csproj package --vulnerable --include-transitive` | Clean, no vulnerable packages |
| `docker compose config --quiet` | Passed, configuration valid |
| `docker compose up --build -d web` | Passed, web image rebuilt and healthy on `localhost:8080` |
| Browser Docker smoke | Passed, login/reload, dark signature token, collapsed sidebar child click, archive filters and chatbot grounded reply |
| Code review | High archived-group mutation finding fixed and retested |

## 2026-06-02
### Added
| Item | Description |
|---|---|
| Grounded chatbot catalog | Added curated QLCM workflow knowledge with accent-insensitive topic retrieval for live and demo assistance |
| Prompt context builder | Added mandatory core rules, permission-filtered routes and aggregate-only operational snapshot |
| Local chatbot privacy guard | Blocks likely patient identifiers and medical-advice prompts before any external API transport |
| Drawn clinical signature | Added Signature Pad powered canvas capture inside the clinical signing confirmation modal |
| Clinical workspace export | Added self-contained HTML export for `/lam-sang` covering patients, encounters, applied protocols and related technical orders |

### Changed
| Item | Description |
|---|---|
| Gemini API auth | Sends key through `x-goog-api-key` header instead of URL query |
| AI settings isolation | Uses scoped per-circuit preferences and chatbot clients; user customization supplements but cannot replace core grounding |
| Provider-safe models | Removes unsupported OpenAI picker option and clamps model choice to configured provider catalog |
| Safe chat actions | Checks `NavGate.CanAccess()` and rewrites workspace route before navigation |
| External API guardrails | Documents manual user-owned Gemini keys, free-tier no-patient-data/no-medical-advice boundary and stable model re-check before production |
| Admin interaction timing | Shortened shared motion tokens so admin modals, drawers and button feedback feel immediate |
| Department archive filter | Renamed the Khoa/Phong status selector to an explicit archive filter and shows active/archive counts |
| Signature realtime refresh | Added data-store refresh after sign/revoke mutations so clinical status/buttons update without page reload |
| Admin navigation permissions | Cached effective SQL permissions per current user so sidebar filtering and route checks stop re-querying the database on every click |

### Fixed
| Item | Description |
|---|---|
| Clinical sign confirm responsiveness | Removed the heavy PNG data-url roundtrip from the sign confirmation click path so the modal no longer feels frozen while validating the drawn signature |
| Procedure filters | Procedure list now displays the version matching selected status/department filters and searches visible version, department and status text |
| Session restore | Protected `/qlcm` aliases and persona workspace routes now receive pending restore auth state so page reload can restore `sessionStorage` before redirecting |
| Docker login bootstrap | Local admin bootstrap migration now resets the documented Docker password hash for existing volumes |
| Chatbot DI startup | Typed Gemini/Anthropic chatbot clients now use an explicit DI constructor so admin login no longer crashes when an API key is configured |
| Admin reload guard | Admin route access moved from server folder authorization to layout/NavGate checks so browser session restore can run before redirect decisions |
| Dark-mode signature ink | Signature canvas ink now follows the active theme so the drawn signature remains visible in dark mode |

### Verification
| Check | Result |
|---|---|
| `dotnet build .\telemedicine-landing-page.sln -c Release` | Passed, 0 warnings, 0 errors |
| `dotnet test .\telemedicine-landing-page.sln -c Release --filter "FullyQualifiedName~ProcedureVersionDisplaySelectorTests\|FullyQualifiedName~CurrentUserAuthenticationStateProviderTests"` | Passed, 12/12 tests |
| `dotnet test .\telemedicine-landing-page.sln -c Release --filter "FullyQualifiedName~GeminiChatbotClientTests\|FullyQualifiedName~CurrentUserAuthenticationStateProviderTests"` | Passed, 11/11 tests |
| `dotnet test .\telemedicine-landing-page.sln -c Release --no-build --filter "FullyQualifiedName~Chatbot"` | Passed, 42/42 tests |
| `dotnet test .\telemedicine-landing-page.sln -c Release --no-build` | Passed, 194/194 tests |
| `docker compose up --build -d web` | Passed, web image rebuilt and container healthy on `localhost:8080` |
| `Invoke-WebRequest http://localhost:8080/` | Passed, HTTP 200 |
| Playwright Docker login smoke | Passed, `admin` login reaches `/admin`; reload stays on `/admin`; no circuit exception in logs |
| `dotnet list .\telemedicine-landing-page.sln package --vulnerable --include-transitive` | Clean, no vulnerable packages |
| `docker compose config` | Passed, configuration valid |

## 2026-05-28
### Added
| Item | Description |
|---|---|
| Account onboarding | Added dedicated `lookup_user_onboarding_status` and `med.users.onboarding_status`; registration remains insert-only while admins can approve, reject, or resubmit accounts |
| Demo e-signature | Added immutable `med.signature_records`, SHA-256 demo hash integrity, one-signature-per-PPA guard, signing/revoke workflow, and `SCR_CLINICAL:SIGN_PROTOCOL_APPLICATION` permission |
| Safe chat actions | Added chat quick actions with route whitelist and nonce-only `sessionStorage` draft handoff; chat actions never mutate SQL data |
| Hospital logo | Added tracked `wwwroot/brand/logo-hos.jpg` and applied it to intro, auth pages, admin/persona sidebars, and favicon |

### Changed
| Item | Description |
|---|---|
| Clinical workflow | New protocol applications start as `draft`; signing moves `application_status` from `applied` to `signed`, revocation moves to `revoked` with reason |
| Clinical signature UX | Removed the white logo frame, restored Vietnamese accents in signature labels/messages, and aligned signature permission aliases with the UI guard |
| Clinical signature stability | Isolated signing/revoke database work with a DbContext factory so Blazor rerenders no longer race the page data store context |
| Login UX | Rejected onboarding users now receive a rejected-specific login result/message instead of the generic inactive account message |

### Verification
| Command | Result |
|---|---|
| `dotnet build .\telemedicine-landing-page.sln -c Release` | Passed, 0 warnings, 0 errors |
| `dotnet test .\telemedicine-landing-page.sln -c Release` | Passed, 161 tests |

## 2026-05-26
### Improved
| Item | Description |
|---|---|
| Ocean pre-auth entry | Reworked `/` into a clearer ocean-blue entry portal with login/register choices, product console preview and restrained card elevation |
| Ocean auth theme | Shifted `/login` and `/register` to a cohesive ocean-blue palette with reduced glow, softer shadows and cleaner form panels |
| Premium GSAP auth experience | Upgraded `/login` and `/register` with layered clinical glass panels, subtle 3D floating depth, stronger shadows, responsive polish and password visibility controls |
| Auth motion reliability | Added visible-target filtering and final reveal cleanup so GSAP intro motion respects mobile breakpoints and never leaves form controls hidden after hydration |
| Vietnam time timeline | Standardized system timeline rendering, date filters, report windows and chat/admin timestamps on Vietnam time while keeping persisted timestamps UTC |

### Fixed
| Item | Description |
|---|---|
| Docker/server timezone drift | Replaced direct `DateTime.Now`, `DateTime.Today` and `.ToLocalTime()` usage in runtime paths so dashboard activity and reports no longer depend on container host timezone |

### Verification
| Command | Result |
|---|---|
| `dotnet build .\telemedicine-landing-page.sln -c Release` | Passed, 0 warnings, 0 errors |
| `dotnet test .\telemedicine-landing-page.sln -c Release --no-build` | Passed, 149 tests |
| `node --check .\src\telemedicine-landing-page\wwwroot\js\animations.js` | Passed |
| `docker compose up --build -d web` | Passed, image `quanlychuyenmon_nhom3-web:latest` rebuilt and web container healthy on `localhost:8080` |
| Playwright screenshot smoke check | Passed, `/`, `/login` and `/register` render ocean-blue pre-auth/auth UI with restrained shadows |

## 2026-05-25
### Improved
| Item | Description |
|---|---|
| QLCM intro route | Restored `/` as a professional QLCM Pro introduction page with clear login/register actions, without bringing back obsolete telemedicine landing content |
| Auth entry pages | Aligned login/register intro panels, password visibility controls and registration feedback styling with the QLCM Pro administration experience |
| Dashboard trend panel | Replaced the unclear blank-looking area under monthly bars with an explicit procedure-version status distribution strip and stopped the trend panel from stretching to activity height |
| Dashboard operations chart | Added an operational chart for technical-order status, resource readiness and over-norm usage so the main dashboard uses the empty area for QLCM workflow monitoring |

### Changed
| Item | Description |
|---|---|
| Landing runtime cleanup | Removed obsolete public telemedicine landing content/services/styles/tests from the active Blazor runtime; `/` now serves the QLCM Pro intro while authentication stays on `/login` and `/register` |
| Procedure action guard | `AdminActionGuard` now runs `ProcedureRuntimeGuard` after permission checks so mapped active procedures can warn/block actions by first-step role and enforcement mode |
| Scheduled permission job | Added Hangfire recurring job to apply due scheduled permission changes and audit/notify the result |
| Inventory snapshots | Order resource checks now use `InventoryAvailabilityService`, prefer procedure-version norms, create availability snapshots and warn on missing/inactive resources |
| Clinical protocol suggestions | Clinical page can suggest active protocol versions by ICD/rule score, auto-select the best match and save decision context on patient protocol applications |
| Persona route access | Persona layout filters sidebar links and blocks direct route access through `NavGate` |
| Auth motion runtime | Auth entry animations now load GSAP 3.15.0 on demand and use GSAP timelines, matchMedia and quickTo motion helpers |

### Verification
| Command | Result |
|---|---|
| `dotnet build .\telemedicine-landing-page.sln -c Release` | Passed, 0 warnings, 0 errors |
| `dotnet test .\telemedicine-landing-page.sln -c Release` | Passed, 145 tests |
| `dotnet list .\telemedicine-landing-page.sln package --vulnerable --include-transitive` | No vulnerable packages |
| `docker compose up --build -d web` | Passed, web rebuilt and healthy on `localhost:8080` |
| Chrome headless smoke check | Passed, `/`, `/login`, `/register` and `/admin` render the QLCM intro/auth shell plus dashboard status and operations charts |
| Docker auth asset check | Passed, fingerprinted `animations.js` contains GSAP 3.15.0 loader and password visibility controls render on `/login` |

## 2026-05-24
### Improved
| Item | Description |
|---|---|
| Permission picker | Replaced long role-permission dropdown with searchable grouped picker, duplicate-scope badges, scope-aware department selection and concise permission labels |
| Permission request timing | Immediate role-permission requests now normalize effective time against requested time so SQL constraints do not crash the Blazor circuit |
| Workspace URLs | Added `/qlcm` route aliases for professional workflows so non-admin users no longer remain on `/admin` URLs for business pages |
| Login landing | Successful login now opens the first route allowed by the user's effective permissions instead of always going to `/admin` |

### Fixed
| Item | Description |
|---|---|
| Organization mutations | Fixed SQL audit tracking so department create, role create, group create and user restore no longer fail after an admin save error |
| Department dropdowns | Department selectors now disambiguate duplicate names by showing the code when names collide |

### Verification
| Command | Result |
|---|---|
| `dotnet build .\telemedicine-landing-page.sln -c Release` | Passed, 0 warnings, 0 errors |
| `dotnet test .\telemedicine-landing-page.sln -c Release --no-build` | Passed, 142 tests |

## 2026-05-22
### QLCM Pro Business UI Remediation
| Item | Description |
|---|---|
| Business labels | Added shared admin display helper for actions, statuses, targets, modules, permissions, notifications, units and compact JSON summaries |
| Permission workflow | Role permissions and user overrides now submit approval requests; approval applies real role/group/user permission records and sends notifications |
| Organization guard | SQL-backed department create/update/archive now validates duplicate codes, parent links and active children with Vietnamese errors |
| Procedure/protocol drafts | Editing active procedures/protocols creates a new draft version with copied steps, resource norms, mappings, links and rules |
| Resource consistency | Service norms and actual order usage now sync/filter units by resource unit group and block mismatched units |
| Admin UI wording | Dashboard, approval inbox, notifications, audit log, screen catalog, procedure mapping and protocol rules hide raw codes/JSON from primary views |

### Verification
| Command | Result |
|---|---|
| `dotnet build .\telemedicine-landing-page.sln -c Release` | Passed, 0 warnings, 0 errors |
| `dotnet test .\telemedicine-landing-page.sln -c Release` | Passed, 132 tests |

### Fixed
| Item | Description |
|---|---|
| Protocol workspace navigation | `/phac-do-pro` now stays inside the admin shell and keeps the Phác đồ sidebar group open when selected |

## 2026-05-21
### Fixed
| Item | Description |
|---|---|
| Docker admin login | Added bootstrap migration for local `admin` so older Docker volumes locked by null-password migration are reactivated with a password |

### Realtime Notifications
| Item | Description |
|---|---|
| SignalR hub | Added `/hubs/notification` with user/group subscription methods and record presence hooks |
| Notification service | Added persisted user, group and broadcast notification service over existing `med.notifications` |
| Admin bell realtime | Admin top bar now subscribes to SignalR, joins the current user group and shows toast updates without refresh |
| Registration alerts | Public registration now sends persisted realtime notifications to administrators with user-management permission |
| Tests | Added notification service tests for direct user send, active-user broadcast and active group membership targeting |

### Verification
| Command | Result |
|---|---|
| `dotnet build .\telemedicine-landing-page.sln -c Release` | Passed, 0 warnings, 0 errors |
| `dotnet test .\telemedicine-landing-page.sln -c Release` | Passed, 124 tests |

### Workflow and Jobs Foundation
| Item | Description |
|---|---|
| Workflow guard | Added generic workflow guard/definition abstractions plus procedure-version and technical-order transition tables |
| Procedure lifecycle | Procedure version submit/publish/reject/archive/restore/withdraw now run through workflow transition guard and audit side effects |
| Order workflow service | Technical order status transitions moved from `OrderPage` into `TechnicalOrderWorkflowService` |
| Hangfire | Added Hangfire SQL Server storage, worker queues, `/hangfire` dashboard authorization and `IJobService` wrapper |
| Dependency security | Added direct `Newtonsoft.Json` 13.0.4 override to remove Hangfire transitive vulnerability |

### Verification
| Command | Result |
|---|---|
| `dotnet test .\telemedicine-landing-page.sln -c Release` | Passed, 121 tests |
| `dotnet list .\telemedicine-landing-page.sln package --vulnerable --include-transitive` | No vulnerable packages |

### Security Validation and Admin Guard
| Item | Description |
|---|---|
| Password policy | Added shared password strength service, common-password list, FluentValidation validators and Identity password validator |
| Null-password lock | Added Identity sign-in guard and SQL migration that locks active users without password hash |
| Password UI | Register and profile password forms now show realtime strength meter and use server-side validators |
| Admin route guard | Added current-user `AuthenticationStateProvider`, admin folder `AdminAccess` policy import and `AuthorizedComponentBase` helper |

### Verification
| Command | Result |
|---|---|
| `dotnet test .\telemedicine-landing-page.sln -c Release` | Passed, 116 tests |

### Production Architecture Foundation
| Item | Description |
|---|---|
| Identity foundation | Added ASP.NET Core Identity entities/tables under `auth` schema while preserving `med.users` and dynamic RBAC as source of truth |
| Permission claims | Added custom `permission` claims transformation, permission service, authorization requirement/handler and starter policies |
| Password login guard | Active accounts without password hash no longer sign in through the manual SQL context |
| Database resilience | Configured SQL retry, command timeout, pool defaults and `/health` JSON endpoint |
| Observability | Added Serilog structured logging with console, daily JSON file and optional Seq sink; enabled request logging |
| Docker migrations | `db-init` now runs SQL scripts from `scripts/migrations` even when the database already exists |

### Verification
| Command | Result |
|---|---|
| `dotnet build .\telemedicine-landing-page.sln -c Release --no-restore` | Passed, 0 warnings, 0 errors |
| `dotnet test .\telemedicine-landing-page.sln -c Release --no-build` | Passed, 112 tests |

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

| SQL Server alignment | Đồng bộ file SQL chính với database thật `MedicalProcedureManagement`: bổ sung `med.users.password_hash` và đổi map điều hướng sang permission code đang có trong `med.permissions` |
| Full SQL permission catalog | Mở rộng `MedicalProcedureManagement.sql` để seed đầy đủ screen/feature/permission cho 25 route nghiệp vụ, thêm base roles và role-permission cho SYSTEM_ADMIN, quản trị khoa, lâm sàng, kỹ thuật/dược và báo cáo |
| SQL-backed reports | Chuyển `IReportService` sang `SqlReportService` để báo cáo tiêu thụ, KPI và activity feed đọc từ bảng SQL thật thay vì service demo in-memory |
| SQL-backed admin routes | Gắn route admin chính của Quy trình và Lâm sàng vào các page đọc `IMedDataStore`, chuyển page demo cũ sang route `-legacy` |

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
