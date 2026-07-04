# Triển khai QLCM Pro trên mạng LAN (đa máy, không cần tên miền)

Hướng dẫn này mô tả cách chạy **một máy chủ** Docker (web + SQL Server) và cho phép **nhiều máy tính khác** trong cùng mạng LAN truy cập, đăng nhập cùng tài khoản và thao tác trên **cùng dữ liệu** mà không cần tên miền.

## Mô hình

| Vai trò | Cách truy cập |
|---|---|
| Máy chủ (chạy Docker) | `http://localhost:8080` |
| Máy client khác trong LAN | `http://<IP-máy-chủ>:8080` (ví dụ `http://192.168.1.10:8080`) |

Tất cả máy client phải trỏ vào **cùng một máy chủ**. Không chạy Docker riêng trên từng PC — mỗi instance Docker tạo database riêng và **không đồng bộ** với nhau.

## Đồng bộ dữ liệu

Khi mọi máy dùng chung server:

- Dữ liệu nghiệp vụ lưu trong SQL Server (volume `qlcm-sqlserver-data`)
- Tài khoản đăng nhập dùng chung bảng `med.users`
- Thay đổi trên máy A hiển thị trên máy B qua `MedDataChangeBus` và SignalR (thông báo, presence)
- File đính kèm quy trình lưu tại `procedure-uploads/` trên máy chủ

## Bước 1 — Cài đặt trên máy chủ

1. Cài [Docker Desktop](https://www.docker.com/products/docker-desktop/) trên Windows.
2. Mở PowerShell tại thư mục dự án:

```powershell
cd QuanLyChuyenMon_Nhom3
docker compose up --build -d
```

3. Đợi container `web` healthy:

```powershell
docker compose ps
```

4. Kiểm tra health:

```powershell
curl http://localhost:8080/health
```

## Bước 2 — Lấy địa chỉ IP LAN của máy chủ

```powershell
ipconfig
```

Tìm **IPv4 Address** của adapter đang kết nối Wi‑Fi/Ethernet (ví dụ `192.168.1.10`).

Hoặc chạy script tiện ích:

```powershell
.\scripts\show-lan-url.ps1
```

## Bước 3 — Mở firewall Windows (máy chủ)

Cho phép máy khác trong LAN kết nối tới port web (mặc định `8080`):

```powershell
# Chạy PowerShell với quyền Administrator
.\scripts\open-lan-firewall.ps1
```

Đổi port nếu đã cấu hình `APP_HTTP_PORT` trong `.env`:

```powershell
.\scripts\open-lan-firewall.ps1 -Port 9090
```

## Bước 4 — Truy cập từ máy client

Trên máy tính khác (cùng mạng LAN), mở trình duyệt:

```text
http://192.168.1.10:8080
```

Thay `192.168.1.10` bằng IP thực tế của máy chủ.

Đăng nhập bằng tài khoản đã có trên hệ thống (sau seed Docker):

| Tên đăng nhập | Mật khẩu |
|---|---|
| `admin` | `Admin@2026` |

Cùng một tài khoản có thể đăng nhập đồng thời trên nhiều máy; mỗi trình duyệt có session riêng.

## Bước 5 — Kiểm tra đồng bộ

1. Máy A: đăng nhập, tạo hoặc sửa một quy trình.
2. Máy B: mở cùng trang — dữ liệu cập nhật tự động hoặc sau thông báo SignalR.
3. Gửi thông báo từ admin — chuông thông báo trên máy B hiển thị toast không cần F5.

## Cấu hình tùy chọn

Sao chép `.env.example` thành `.env` và chỉnh nếu cần:

| Biến | Mặc định | Ghi chú |
|---|---|---|
| `APP_HTTP_PORT` | `8080` | Port mà máy client dùng trong URL |
| `MSSQL_SA_PASSWORD` | (xem `.env.example`) | Đổi trước khi triển khai thật |

Volume bổ sung cho session ổn định khi restart container:

- `./app-data/dpkeys` → khóa Data Protection (token đăng nhập không hết hạn sau `docker compose up` lại)

## Dev không dùng Docker (tùy chọn)

Để các máy LAN truy cập khi chạy `dotnet run` trực tiếp:

```powershell
dotnet run --project .\src\telemedicine-landing-page\telemedicine-landing-page.csproj --launch-profile lan
```

Máy client truy cập `http://<IP-máy-chủ>:5049`. Cần SQL Server reachable từ máy dev và mở firewall port `5049`.

## Xử lý sự cố

| Triệu chứng | Nguyên nhân thường gặp | Cách xử lý |
|---|---|---|
| Máy client không mở được trang | Firewall chặn port hoặc Wi‑Fi ở profile **Public** | Chạy `open-lan-firewall.ps1` **với quyền Administrator**; script đặt mạng Wi‑Fi sang Private và mở port 8080 |
| Trang load nhưng realtime không hoạt động | WebSocket bị chặn | Cho phép kết nối WebSocket tới cùng host:port; không dùng proxy HTTP cắt WS |
| Đăng nhập bị đăng xuất sau restart Docker | Volume `app-data/dpkeys` thiếu | Đảm bảo bind mount trong `docker-compose.yml` tồn tại |
| Hai máy thấy dữ liệu khác nhau | Mỗi máy chạy Docker riêng | Chỉ một máy chủ; client chỉ mở trình duyệt tới IP đó |

## Backup

Trước khi `docker compose down --volumes`:

- Volume SQL: `qlcm-sqlserver-data`
- Thư mục upload: `procedure-uploads/`
- Khóa session: `app-data/dpkeys/`

### Reset database và seed lại (tất cả máy dùng chung dữ liệu mới)

Chỉ chạy trên **máy chủ Docker** — các máy client chỉ cần F5 hoặc đăng nhập lại:

```powershell
.\scripts\reset-docker-seed.ps1
```

Script xóa volume SQL, xóa session keys, seed lại từ đầu. Tài khoản mặc định: `admin` / `Admin@2026`.

## Tài liệu liên quan

- [deployment-guide.md](deployment-guide.md) — Docker Compose chi tiết
- [README.md](../README.md) — tổng quan dự án
