# Project Changelog

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
