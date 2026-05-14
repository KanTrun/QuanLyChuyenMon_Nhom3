# Bệnh viện số - Telemedicine Landing Page

## Tổng quan
Dự án hiện có một landing page khám từ xa chạy bằng Blazor Web App trên `.NET 9`. Trang được thiết kế theo hướng editorial/App Store-style soft UI cho hệ thống bệnh viện: tư vấn video, danh bạ bác sĩ chuyên khoa, theo dõi sức khỏe và CTA tải ứng dụng.

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
