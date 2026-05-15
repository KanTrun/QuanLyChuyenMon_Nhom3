# Bệnh viện số - Telemedicine Landing Page

## Tổng quan
Dự án hiện có một landing page khám từ xa chạy bằng Blazor Web App trên `.NET 9`. Trang được thiết kế theo hướng editorial/App Store-style soft UI cho hệ thống bệnh viện: tư vấn video, danh bạ bác sĩ chuyên khoa, theo dõi sức khỏe và CTA tải ứng dụng.

Bên cạnh trang công khai, dự án còn có module quản trị nội bộ tại `/admin` (gọi tắt là `QLCM Pro`) gồm sidebar điều hướng, bảng lệnh nhanh và các trang Tổng quan, Quy trình kỹ thuật, Phân quyền, Danh mục, Phác đồ, Báo cáo, Lâm sàng, Cài đặt. Module sử dụng dữ liệu demo trong bộ nhớ và đi kèm trợ lý AI tích hợp Google Gemini (mô hình mặc định `gemini-2.5-flash` - free tier). Khi chưa cấu hình API key, trợ lý sẽ chạy ở chế độ demo và phản hồi bằng tiếng Việt có dấu.

### Cấu hình trợ lý AI
Mặc định chatbot dùng Google Gemini qua endpoint `https://generativelanguage.googleapis.com/v1beta/models/{model}:streamGenerateContent?alt=sse`. Lấy API key miễn phí tại [Google AI Studio](https://aistudio.google.com/apikey). Mô hình mặc định `gemini-2.5-flash` cho chất lượng tốt nhất ở free tier; có thể đổi sang `gemini-2.5-flash-lite` (quota cao hơn) hoặc `gemini-2.5-pro` (chất lượng cao nhất, quota thấp hơn) trong trang Cài đặt.

Nạp `ApiKey` qua biến môi trường hoặc `dotnet user-secrets` (không đặt trực tiếp vào `appsettings.json`):

```powershell
# Windows PowerShell
$env:Chatbot__ApiKey = "AIza..."
# hoặc dùng user-secrets (khuyến nghị)
dotnet user-secrets set "Chatbot:ApiKey" "AIza..." --project .\src\telemedicine-landing-page\telemedicine-landing-page.csproj
```

```bash
# macOS/Linux
export Chatbot__ApiKey="AIza..."
```

Vẫn có thể chuyển sang Anthropic Claude bằng cách đặt `Chatbot:Provider=Anthropic`, `Chatbot:Model=claude-sonnet-4-5-20250929`, `Chatbot:BaseUrl=https://api.anthropic.com` và dùng API key Anthropic tương ứng.

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

## Pull code mới nhất về VS Code
```powershell
# Lần đầu clone
git clone https://github.com/<owner>/QuanLyChuyenMon_Nhom3.git
cd QuanLyChuyenMon_Nhom3
code .

# Khi đã có repo - cập nhật nhánh đang làm việc
git fetch --all --prune
git checkout main          # hoặc nhánh đang dùng (ví dụ: feat/qlcm-pro-admin-shell)
git pull --ff-only
```

Trong VS Code có thể bấm `Ctrl + Shift + G` để mở panel Source Control, rồi chọn `... > Pull` để cập nhật từ remote.

## Chạy local
```powershell
dotnet restore .\telemedicine-landing-page.sln
dotnet run --project .\src\telemedicine-landing-page\telemedicine-landing-page.csproj
```

Sau khi chạy, mở trình duyệt tại địa chỉ in ra trong terminal (mặc định `http://localhost:5xxx`), trang quản trị nội bộ ở `/admin`.

## Kiểm tra
```powershell
dotnet build .\telemedicine-landing-page.sln -c Release
dotnet test .\telemedicine-landing-page.sln -c Release
```

## Cấu hình CTA
Các link CTA nằm trong `src/telemedicine-landing-page/appsettings.json` tại section `LandingPageLinks`.

## Ghi chú phạm vi
Landing page không lưu dữ liệu người bệnh, không có DB/CMS/API và không triển khai đặt lịch thật. Đây là lớp giao diện nền để mở rộng khi có yêu cầu tích hợp backend bệnh viện.
