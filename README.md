# Bệnh viện số - Telemedicine Landing Page

## Tổng quan
Dự án hiện có một landing page khám từ xa chạy bằng Blazor Web App trên `.NET 9`. Trang được thiết kế theo hướng editorial/App Store-style soft UI cho hệ thống bệnh viện: tư vấn video, danh bạ bác sĩ chuyên khoa, theo dõi sức khỏe và CTA tải ứng dụng.

Bên cạnh trang công khai, dự án còn có module quản trị nội bộ tại `/admin` (gọi tắt là `QLCM Pro`) gồm sidebar điều hướng, bảng lệnh nhanh và các trang Tổng quan, Quy trình kỹ thuật, Phân quyền, Danh mục, Phác đồ, Báo cáo, Lâm sàng, Cài đặt. Module sử dụng dữ liệu demo trong bộ nhớ và đi kèm trợ lý AI tích hợp Anthropic Claude (mô hình mặc định `claude-sonnet-4-5-20250929`). Khi chưa cấu hình API key, trợ lý sẽ chạy ở chế độ demo và phản hồi bằng tiếng Việt có dấu.

### Cấu hình trợ lý AI
Có thể nạp `ApiKey` qua biến môi trường `QLCM_Chatbot__ApiKey` hoặc `dotnet user-secrets` (không nên đặt trực tiếp vào `appsettings.json`):

```powershell
$env:QLCM_Chatbot__ApiKey = "sk-ant-..."
dotnet user-secrets set "Chatbot:ApiKey" "sk-ant-..." --project .\src\telemedicine-landing-page\telemedicine-landing-page.csproj
```

### Phím tắt
| Phím | Hành động |
|---|---|
| `Ctrl + K` | Mở bảng lệnh nhanh (tìm kiếm điều hướng và hành động) |
| `Alt + 0` đến `Alt + 6` | Chuyển nhanh giữa các module trong sidebar |
| `Ctrl + /` | Bật trợ lý AI |
| `Esc` | Đóng bảng lệnh, panel chatbot, drawer hoặc modal đang mở |

## Stack
| Layer | Công nghệ |
|---|---|
| Web host | ASP.NET Core Blazor Web App |
| Target framework | `net9.0` |
| Test | xUnit |
| UI | Razor Components + CSS tokens |

## Chạy local
```powershell
dotnet restore .\telemedicine-landing-page.sln
dotnet run --project .\src\telemedicine-landing-page\telemedicine-landing-page.csproj
```

## Kiểm tra
```powershell
dotnet build .\telemedicine-landing-page.sln -c Release
dotnet test .\telemedicine-landing-page.sln -c Release
```

## Cấu hình CTA
Các link CTA nằm trong `src/telemedicine-landing-page/appsettings.json` tại section `LandingPageLinks`.

## Ghi chú phạm vi
Landing page không lưu dữ liệu người bệnh, không có DB/CMS/API và không triển khai đặt lịch thật. Đây là lớp giao diện nền để mở rộng khi có yêu cầu tích hợp backend bệnh viện.
