# QLCM Pro - Quản lý chuyên môn bệnh viện

## Tổng quan
Dự án hiện chạy QLCM Pro bằng Blazor Web App trên `.NET 9`. Landing page telemedicine cũ đã được gỡ khỏi runtime; route `/` là trang giới thiệu QLCM Pro chuyên nghiệp trước khi người dùng chọn đăng nhập/đăng ký, còn luồng chính nằm ở `/admin` và các workspace nghiệp vụ.

QLCM Pro gồm sidebar điều hướng, bảng lệnh nhanh và các trang Tổng quan, Quy trình kỹ thuật, Phân quyền, Danh mục, Tài nguyên, Chỉ định, Phác đồ, Báo cáo, Lâm sàng, Thông báo, Cài đặt. Module dùng SQL-backed data store qua `MedDbContext`/`IMedDataStore` cho các luồng quản trị quy trình chuyên môn, RBAC, định mức tài nguyên, chỉ định kỹ thuật, phác đồ, bệnh nhân và thông báo. Trợ lý AI tích hợp Google Gemini (mô hình mặc định `gemini-2.5-flash` - free tier). Khi chưa cấu hình API key, trợ lý sẽ chạy ở chế độ demo và phản hồi bằng tiếng Việt có dấu.

### Cấu hình trợ lý AI
Mặc định chatbot dùng Google Gemini qua endpoint `https://generativelanguage.googleapis.com/v1beta/models/{model}:streamGenerateContent?alt=sse`. Lấy API key miễn phí tại [Google AI Studio](https://aistudio.google.com/apikey). Mô hình mặc định `gemini-2.5-flash` cho chất lượng tốt nhất ở free tier; có thể đổi sang `gemini-2.5-flash-lite` (quota cao hơn) hoặc `gemini-2.5-pro` (chất lượng cao nhất, quota thấp hơn) trong trang Cài đặt.

API key phải do chủ tài khoản tự tạo và giới hạn cho Gemini API. Không tự động đăng ký, thu thập hoặc ghi key vào source. Theo [Gemini API Terms](https://ai.google.dev/gemini-api/terms#unpaid-services), dữ liệu free tier có thể được dùng để cải thiện sản phẩm; không nhập dữ liệu bệnh nhân, dữ liệu bí mật hoặc yêu cầu tư vấn y khoa vào chatbot. Trợ lý chỉ hỗ trợ vận hành phần mềm không định danh. Privacy guard cục bộ chặn nội dung có dấu hiệu định danh bệnh nhân hoặc yêu cầu tư vấn y khoa trước khi gửi tới API ngoài; đây là lớp bảo vệ bổ sung, không thay thế quy tắc không nhập dữ liệu nhạy cảm.

`gemini-2.5-flash` là model stable. Trước khi triển khai production cần kiểm tra lại [Gemini models](https://ai.google.dev/gemini-api/docs/models) và [deprecations](https://ai.google.dev/gemini-api/docs/deprecations), vì lịch model có thể thay đổi.

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
| Data | Entity Framework Core + SQL Server schema `MedicalProcedureManagement` |

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

Sau khi chạy, mở trình duyệt tại địa chỉ in ra trong terminal (mặc định `http://localhost:5xxx`). Route `/` hiển thị trang giới thiệu QLCM Pro; trang quản trị nội bộ ở `/admin`.

## Chạy full stack bằng Docker
Dự án có thể chạy trọn bộ web app, SQL Server và database seed bằng Docker Compose:

```powershell
docker compose up --build
```

Sau khi container sẵn sàng, mở `http://localhost:8080`. Compose gồm:

Tài khoản bootstrap local sau khi seed/migration:

| Tên đăng nhập | Mật khẩu |
|---|---|
| `admin` | `Admin@2026` |

| Service | Vai trò |
|---|---|
| `web` | ASP.NET Core Blazor app `net9.0` |
| `sqlserver` | SQL Server 2022 Developer, lưu dữ liệu trong named volume |
| `db-init` | Chạy `MedicalProcedureManagement.sql` và seed scripts một lần khi DB chưa khởi tạo |

Có thể đổi cấu hình qua biến môi trường hoặc file `.env` cục bộ:

| Biến | Mặc định | Mô tả |
|---|---|---|
| `APP_HTTP_PORT` | `8080` | Port web trên máy host |
| `DB_PORT` | `14333` | Port SQL Server trên máy host |
| `MSSQL_SA_PASSWORD` | `QlcmDev_ChangeMe_2026!` | Mật khẩu SA cho SQL Server local |
| `CHATBOT_API_KEY` | rỗng | API key Gemini tuỳ chọn |
| `CHATBOT_PROVIDER` | `Gemini` | Provider chatbot (`Gemini` hoặc `Anthropic`) |
| `CHATBOT_MODEL` | `gemini-2.5-flash` | Model tương thích provider |
| `CHATBOT_BASE_URL` | `https://generativelanguage.googleapis.com` | Endpoint provider cho container web |
| `CHATBOT_MAX_TOKENS` | `4096` | Giới hạn output chatbot để giảm trả lời bị cắt giữa chừng |

### Chữ ký VNPT SmartCA sandbox
Docker map `SMARTCA_ENABLED`, `SMARTCA_BASE_URL`, `SMARTCA_API_PREFIX`, `SMARTCA_SP_ID`, `SMARTCA_SP_PASSWORD`, `SMARTCA_DEFAULT_USER_ID`, `SMARTCA_DEFAULT_SERIAL_NUMBER`, `SMARTCA_SIGNER_USER_ID`, `SMARTCA_SIGNER_USERNAME`, `SMARTCA_USER_BINDINGS_JSON`, `SMARTCA_CALLBACK_URL`, và `SMARTCA_REQUEST_TIMEOUT_SECONDS` vào `SmartCa:*`.

Bật `SMARTCA_ENABLED=true`, nhập credential SP sandbox do VNPT cấp, rồi bind thuê bao CA với đúng tài khoản app bằng `SMARTCA_SIGNER_USER_ID` hoặc `SMARTCA_SIGNER_USERNAME`. Nếu nhiều người ký, dùng `SMARTCA_USER_BINDINGS_JSON` dạng `[{"appUsername":"admin","subscriberId":"012345678901","serialNumber":"optional"}]`. QLCM chỉ gửi hash chuẩn hóa của hồ sơ sang SmartCA, chờ người ký xác nhận trên app SmartCA, kiểm tra đúng document id và chứng thư trước khi lưu chữ ký pháp lý. Khi chưa có credential/binding, Docker vẫn chạy và chữ ký demo nội bộ vẫn dùng cho QA.

Muốn tạo lại DB sạch:

```powershell
docker compose down --volumes
docker compose up --build
```

Xem chi tiết trong `docs/deployment-guide.md`.

## Dữ liệu mẫu QLCM Pro
Sau khi có SQL Server database đúng schema, có thể nạp dữ liệu demo/QA:

```powershell
sqlcmd -S <server> -d <database> -i .\scripts\seed-realistic-data.sql
```

Script tạo dữ liệu mẫu cho khoa phòng, người dùng, vai trò, quyền, quy trình, dịch vụ kỹ thuật, tài nguyên, chỉ định, bệnh nhân, phác đồ và thông báo.

## Kiểm tra
```powershell
dotnet build .\telemedicine-landing-page.sln -c Release
dotnet test .\telemedicine-landing-page.sln -c Release
dotnet list .\telemedicine-landing-page.sln package --vulnerable --include-transitive
docker compose config
```

Kết quả xác minh ngày `2026-06-04`: build Release đạt `0 warnings, 0 errors`; toàn solution đạt `226/226`; package vulnerability scan sạch; `docker compose config` hợp lệ; Docker web healthy.

## Ghi chú phạm vi
Các tích hợp kho/dược/trang thiết bị ngoài hệ thống hiện được bọc qua service boundary nội bộ. Khi có API thật từ HIS/EMR/kho/dược, thay adapter tương ứng thay vì đưa logic tích hợp trực tiếp vào Razor page.
