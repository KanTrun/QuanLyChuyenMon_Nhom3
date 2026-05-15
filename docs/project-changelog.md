# Project Changelog

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
