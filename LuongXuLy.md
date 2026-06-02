# Hướng Dẫn Toàn Bộ Luồng Quy Trình Hệ Thống QLCM Pro

Tài liệu này được viết để giúp bạn hiểu hệ thống theo cách dễ hình dung nhất:

- Mỗi màn hình dùng để làm gì
- Người dùng đi từ bước nào sang bước nào
- Dữ liệu đi từ UI xuống service rồi xuống database ra sao
- Mỗi module liên kết với module nào
- Mỗi bảng dữ liệu đại diện cho nghiệp vụ gì
- Muốn đọc source thì nên bắt đầu từ đâu

Tài liệu không chỉ mô tả code, mà còn diễn giải theo **luồng chuyên môn của hệ thống**.

---

## 1. Mục tiêu của hệ thống

QLCM Pro là hệ thống quản lý chuyên môn bệnh viện. Nói ngắn gọn, hệ thống này dùng để:

1. Quản lý tài khoản, khoa/phòng, nhóm, vai trò.
2. Phân quyền ai được xem, tạo, sửa, duyệt, thực hiện từng chức năng.
3. Quản lý quy trình kỹ thuật theo phiên bản.
4. Quản lý danh mục kỹ thuật, vật tư, thuốc, thiết bị và định mức sử dụng.
5. Tạo chỉ định kỹ thuật cho bệnh nhân, kiểm tra nguồn lực, ghi nhận sử dụng thực tế.
6. Quản lý phác đồ lâm sàng, gợi ý phác đồ theo ICD.
7. Áp dụng phác đồ cho bệnh nhân, ký xác nhận, thu hồi chữ ký.
8. Gửi thông báo, ghi audit log, tạo báo cáo vận hành.

---

## 2. Cách nhìn hệ thống theo 4 tầng

## 2.1. Tầng giao diện

Đây là nơi người dùng bấm nút và nhìn dữ liệu:

- `Components/Pages/*`
- `Components/Layout/*`
- `Components/Admin/*`
- `Components/Chatbot/*`

Ví dụ:

- `Login.razor`: đăng nhập
- `QuyTrinhTaoMoiPage.razor`: tạo quy trình
- `OrderPage.razor`: tạo và xử lý chỉ định kỹ thuật
- `ClinicalPage.razor`: làm việc với bệnh nhân, lượt khám, áp dụng phác đồ

## 2.2. Tầng điều hướng, xác thực, kiểm soát

Đây là tầng quyết định:

- user là ai
- có quyền hay không
- có được vào route hay không
- thao tác có đúng quy trình đang hiệu lực hay không

File chính:

- `Services/Admin/Sql/CurrentUserContext.cs`
- `Services/Auth/CurrentUserAuthenticationStateProvider.cs`
- `Services/Admin/Sql/NavGate.cs`
- `Services/Admin/Sql/AdminActionGuard.cs`
- `Services/Admin/Sql/ProcedureRuntimeGuard.cs`

## 2.3. Tầng nghiệp vụ

Đây là nơi luật nghiệp vụ thật sự được xử lý:

- duyệt quyền
- publish quy trình
- đổi trạng thái chỉ định
- gợi ý phác đồ
- tạo báo cáo

File chính:

- `PermissionChangeRequestService.cs`
- `ProcedureLifecycleService.cs`
- `TechnicalOrderWorkflowService.cs`
- `InventoryAvailabilityService.cs`
- `ClinicalProtocolSuggestionService.cs`
- `SignatureService.cs`
- `SqlReportService.cs`
- `SignalRNotificationService.cs`
- `ChatbotService.cs`

## 2.4. Tầng dữ liệu

Đây là nơi map dữ liệu xuống SQL Server:

- `Data/MedDbContext.cs`
- `Services/Admin/Sql/MedDbDataStore.cs`

Database được chia làm 2 vùng chính:

- schema `auth`: dữ liệu nền cho ASP.NET Identity
- schema `med`: dữ liệu nghiệp vụ bệnh viện

---

## 3. Sơ đồ tổng thể dễ hiểu

```text
Người dùng trên trình duyệt
    |
    v
Razor Components / Layout / Pages
    |
    v
CurrentUserContext + Route Guard + Action Guard
    |
    v
Service nghiệp vụ
    |
    v
MedDbContext / IMedDataStore
    |
    v
SQL Server
```

Luồng phụ trợ:

```text
Service nghiệp vụ
    |----> Audit Logs
    |----> Notifications
    |----> Hangfire Jobs
    |----> SignalR realtime
    |----> Chatbot
```

---

## 4. Bảng bản đồ các module trong hệ thống

| Module | Route / Khu vực | Mục tiêu | Dữ liệu chính | Liên kết với |
|---|---|---|---|---|
| Giới thiệu hệ thống | `/` | Trang vào hệ thống | Không có dữ liệu nghiệp vụ | Login, Register |
| Đăng nhập | `/login` | Xác định user hiện tại | `med.users`, sessionStorage | NavGate, AdminLayout |
| Đăng ký | `/register` | Gửi yêu cầu tạo tài khoản | `med.users`, `med.notifications`, `med.audit_logs` | UsersPage |
| Tổ chức | `/admin/to-chuc/*` | Quản lý khoa/phòng, user, role, group | `departments`, `users`, `roles`, `groups` | Phân quyền, route guard |
| Dashboard | `/admin` | Xem nhanh tình hình hệ thống | `procedures`, `procedure_versions`, `clinical_protocols`, `notifications`, `audit_logs` | Báo cáo, Thông báo |
| Phân quyền | `/admin/phan-quyen` | Tạo yêu cầu thay đổi quyền | `permission_change_requests`, `permission_change_items` | InboxPage, EffectivePermissionResolver |
| Phê duyệt quyền | `/phe-duyet` | Duyệt/từ chối thay đổi quyền | như trên | Notifications, Audit |
| Quy trình kỹ thuật | `/admin/quy-trinh*` | Tạo, sửa, duyệt, publish quy trình | `professional_procedures`, `procedure_versions`, `procedure_steps`, `procedure_screen_mappings` | Runtime Guard, Order |
| Danh mục kỹ thuật | `/admin/danh-muc` | Quản lý dịch vụ kỹ thuật và định mức | `technical_services`, `technical_resource_norms` | Order, Report |
| Tài nguyên | `/tai-nguyen` | Quản lý vật tư, thuốc, thiết bị | `resource_catalog` | Danh mục, Order |
| Điều phối chỉ định | `/dieu-phoi` | Tạo và xử lý chỉ định kỹ thuật | `technical_orders`, `resource_availability_snapshots`, `actual_resource_usages` | Danh mục, Tài nguyên, Báo cáo |
| Phác đồ | `/admin/phac-do`, `/phac-do-pro` | Quản lý phác đồ và version | `clinical_protocols`, `clinical_protocol_versions`, `protocol_applicability_rules` | Clinical, LamSang |
| Lâm sàng | `/lam-sang`, `/admin/lam-sang` | Quản lý bệnh nhân, encounter, áp dụng phác đồ, ký | `patient_refs`, `encounter_refs`, `patient_protocol_applications`, `signature_records` | Protocol, Signature |
| Báo cáo | `/admin/bao-cao*` | KPI, hoạt động, tiêu thụ | `actual_resource_usages`, `audit_logs`, `notifications` | Order, Procedure |
| Thông báo | `/thong-bao` | Xem và đánh dấu đã đọc thông báo | `notifications`, `notification_preferences` | SignalR, mọi module |
| Hồ sơ | `/admin/ho-so` | Xem và cập nhật hồ sơ cá nhân | `med.users` | CurrentUserContext, Validation |
| Cài đặt | `/admin/cai-dat` | Đổi theme, animation, AI model, notification channel, logout | preference in memory + browser + `notification_preferences` | ThemeBus, TopBar, Chatbot |
| Chatbot | Panel trong shell | Trợ lý nội bộ | conversation memory, Gemini/Demo | Clinical, Procedure, Protocol |

---

## 5. Luồng tổng quát từ đầu đến cuối của hệ thống

Nếu nhìn toàn bộ hệ thống như một quy trình lớn, bạn có thể hiểu theo thứ tự sau:

1. Người dùng vào `/`.
2. Nếu chưa có tài khoản thì vào `/register`.
3. Admin vào module `Người dùng` để duyệt tài khoản.
4. User đã được kích hoạt đăng nhập qua `/login`.
5. Hệ thống xác định quyền, route được phép, shell được phép thấy.
6. Admin cấu hình:
   - khoa/phòng
   - vai trò
   - nhóm
   - quyền
7. Admin tạo quy trình kỹ thuật và publish version.
8. Admin tạo danh mục dịch vụ kỹ thuật, định mức nguồn lực.
9. Admin tạo tài nguyên vật tư/thuốc/thiết bị.
10. Admin tạo phác đồ lâm sàng và publish version.
11. Bác sĩ hoặc người dùng nghiệp vụ tạo chỉ định kỹ thuật cho bệnh nhân.
12. Hệ thống kiểm tra nguồn lực có đủ hay không.
13. Kỹ thuật viên / điều dưỡng thực hiện và ghi nhận sử dụng thực tế.
14. Bác sĩ áp dụng phác đồ cho bệnh nhân.
15. Hệ thống gợi ý phác đồ theo ICD nếu có dữ liệu phù hợp.
16. Người có quyền ký xác nhận hồ sơ.
17. Hệ thống gửi thông báo và ghi audit log cho các thay đổi quan trọng.
18. Cuối cùng, module báo cáo tổng hợp dữ liệu vận hành để xem KPI và tiêu thụ.

---

## 6. Luồng đăng nhập và xác định user hiện tại

## 6.1. Mục tiêu

Xác định ai đang dùng hệ thống, sau đó biết người đó được vào màn nào và được bấm nút nào.

## 6.2. Màn hình chính

- `Components/Pages/Login.razor`
- `Components/Pages/Register.razor`

## 6.3. Luồng đăng nhập

```text
User nhập username/password
-> Login.razor nhận dữ liệu
-> CurrentUserContext.LoginByUsernameDetailed()
-> tìm user trong med.users
-> kiểm tra PasswordHash
-> kiểm tra Status và OnboardingStatus
-> nếu hợp lệ: set CurrentUser
-> ghi audit login
-> lưu user id vào sessionStorage
-> điều hướng sang route đầu tiên user được phép vào
```

## 6.4. Dữ liệu vào

| Dữ liệu | Nguồn |
|---|---|
| Username | Form đăng nhập |
| Password | Form đăng nhập |

## 6.5. Dữ liệu xử lý

| Kiểm tra | Ý nghĩa |
|---|---|
| `Status == active` | Tài khoản có đang hoạt động không |
| `OnboardingStatus == active` | Tài khoản đã được duyệt chưa |
| `PasswordHash != null` | Có được phép đăng nhập bằng mật khẩu không |

## 6.6. Dữ liệu ra

| Kết quả | Hệ thống làm gì |
|---|---|
| Success | lưu `CurrentUser`, ghi audit, chuyển trang |
| InvalidCredentials | báo sai username/password |
| Inactive | báo chưa được kích hoạt |
| Rejected | báo tài khoản bị từ chối |
| PasswordNotSet | báo cần thiết lập lại mật khẩu |

## 6.7. File code quan trọng

- `Services/Admin/Sql/CurrentUserContext.cs`
- `Services/Admin/Sql/BrowserSessionService.cs`
- `Services/Auth/CurrentUserAuthenticationStateProvider.cs`
- `Services/Admin/Sql/NavGate.cs`

## 6.8. Liên kết với phần khác

- Sau login xong, `NavGate` quyết định route đầu tiên.
- `AdminLayout` và `PersonaLayout` sẽ restore session nếu circuit Blazor reload.

---

## 7. Luồng đăng ký và kích hoạt tài khoản

## 7.1. Mục tiêu

Cho người dùng mới gửi yêu cầu tạo tài khoản, nhưng không cho dùng ngay cho tới khi được admin duyệt.

## 7.2. Luồng đăng ký

```text
User mở Register
-> nhập họ tên, email, password
-> ValidationService + RegisterAccountValidator kiểm tra dữ liệu
-> tạo AppUser mới với:
   Status = inactive
   OnboardingStatus = submitted
-> ghi audit create user
-> gửi notification cho admin
-> user quay về trang login chờ duyệt
```

## 7.3. Luồng duyệt tài khoản

```text
Admin mở UsersPage
-> thấy các tài khoản onboarding submitted
-> ApproveUser():
   Status = active
   OnboardingStatus = active
-> hoặc RejectUser():
   Status = inactive
   OnboardingStatus = rejected
-> hoặc ResubmitUser():
   đưa rejected về submitted
```

## 7.4. Bảng dữ liệu liên quan

| Bảng | Vai trò |
|---|---|
| `med.users` | Lưu tài khoản nghiệp vụ |
| `med.notifications` | Báo cho admin có user chờ kích hoạt |
| `med.audit_logs` | Ghi lại hành động tạo / duyệt / từ chối |

---

## 8. Luồng tổ chức: khoa phòng, vai trò, nhóm, người dùng

## 8.1. Mục tiêu

Tạo cấu trúc tổ chức để sau đó gán quyền và áp dụng đúng chuyên môn theo khoa/phòng.

## 8.2. Các màn hình

| Màn hình | Route | Dùng để làm gì |
|---|---|---|
| DepartmentsPage | `/admin/to-chuc/khoa-phong` | Quản lý cây khoa/phòng |
| RolesPage | `/admin/to-chuc/vai-tro` | Quản lý vai trò nghiệp vụ |
| GroupsPage | `/admin/to-chuc/nhom` | Quản lý nhóm người dùng |
| UsersPage | `/admin/to-chuc/nguoi-dung` | Quản lý user, role, group |

## 8.3. Luồng khoa/phòng

```text
Admin tạo Department
-> nhập code, name, parent
-> MedDbDataStore validate:
   - code duy nhất
   - parent tồn tại
   - không tạo vòng lặp cây
-> lưu med.departments
-> SQL trigger cập nhật closure table
```

## 8.4. Luồng role

```text
Admin tạo Role
-> nhập code, name, mô tả
-> validate code không trùng
-> lưu med.roles
```

## 8.5. Luồng group

```text
Admin tạo Group
-> nhập code, name, department
-> validate department hợp lệ
-> lưu med.groups
-> có thể thêm member vào group
-> có thể thêm quyền trực tiếp cho group
```

## 8.6. Luồng user

```text
Admin tạo/sửa User
-> nhập fullname, username, email, department, status
-> lưu med.users
-> gán role qua user_roles
-> gán group qua user_group_members
```

## 8.7. Tại sao phần này quan trọng

Tất cả các phần sau đều phụ thuộc vào nó:

- phân quyền
- route guard
- runtime guard theo vai trò
- scope theo khoa/phòng

---

## 9. Luồng phân quyền và duyệt thay đổi quyền

## 9.1. Mục tiêu

Không sửa quyền nhạy cảm trực tiếp. Mọi thay đổi quan trọng đều đi qua quy trình yêu cầu và duyệt.

## 9.2. Thành phần chính

| Thành phần | Vai trò |
|---|---|
| `PhanQuyenPage` | tạo yêu cầu thay đổi quyền |
| `InboxPage` | người phê duyệt duyệt/từ chối |
| `PermissionChangeRequestService` | xử lý workflow thay đổi quyền |
| `EffectivePermissionResolver` | tính quyền hiệu lực cuối cùng |
| `Hangfire` | áp thay đổi theo lịch |

## 9.3. Luồng tạo yêu cầu thay đổi quyền

```text
Admin vào Phân quyền
-> chọn user/group/role cần thay đổi
-> chọn permission, effect, scope, reason
-> hệ thống CreateDraft()
-> AddItem()
-> SubmitForApproval()
-> request chuyển sang pending_approval
```

## 9.4. Luồng duyệt yêu cầu

```text
Approver vào InboxPage
-> chọn request pending_approval
-> Approve():
   - nếu áp ngay: ApplyItems() -> applied
   - nếu đặt lịch: scheduled
-> Reject():
   - request -> rejected
-> gửi notification cho các bên liên quan
-> ghi audit
```

## 9.5. Luồng áp theo lịch

```text
Hangfire chạy mỗi phút
-> ApplyDueScheduledRequests()
-> request scheduled tới hạn
-> apply vào role_permissions / group_permissions / user_permission_overrides
-> request -> applied
```

## 9.6. Resolver tính quyền cuối cùng như thế nào

Nguồn quyền:

1. từ role
2. từ group
3. từ user override

Quy tắc thắng:

1. priority cao hơn thắng
2. nếu bằng nhau thì `deny` thắng `allow`
3. nếu vẫn bằng thì `user > group > role`
4. nếu vẫn bằng thì bản ghi mới hơn thắng

## 9.7. Bảng dữ liệu liên quan

| Bảng | Ý nghĩa |
|---|---|
| `permissions` | danh mục quyền |
| `role_permissions` | quyền theo vai trò |
| `group_permissions` | quyền theo nhóm |
| `user_permission_overrides` | ghi đè theo user |
| `permission_change_requests` | phiếu yêu cầu thay đổi quyền |
| `permission_change_items` | chi tiết từng thay đổi trong phiếu |

---

## 10. Luồng điều hướng và chặn route

## 10.1. Mục tiêu

Người không có quyền thì không thấy menu, không vào được route, và không thực hiện được thao tác.

## 10.2. Luồng route guard

```text
User mở route
-> Router render route
-> Layout gọi NavGate.CanAccess(route)
-> NavGate hỏi CurrentUserContext.HasPermission()
-> nếu không có quyền:
   - route admin: chặn
   - route persona: chặn
-> nếu có quyền: cho render
```

## 10.3. Luồng action guard

```text
User bấm nút Create/Update/Approve/Execute
-> AdminActionGuard.CanDo(permissionCode)
-> check quyền
-> nếu có quyền tiếp tục gọi ProcedureRuntimeGuard
-> nếu runtime guard cho phép thì mới chạy nghiệp vụ
```

## 10.4. Kết quả

Hệ thống kiểm soát ở 3 lớp:

1. menu
2. route
3. button/action

---

## 10A. Luồng shell quản trị, dashboard, hồ sơ và cài đặt

## 10A.1. Mục tiêu

Phần này không tạo nghiệp vụ y tế trực tiếp, nhưng là nơi user vận hành hệ thống mỗi ngày:

- vào shell quản trị
- xem dashboard
- mở nhanh command palette
- xem thông báo ở top bar
- đổi giao diện
- cập nhật hồ sơ
- đổi mật khẩu
- logout an toàn

## 10A.2. Thành phần chính

| Thành phần | Vai trò |
|---|---|
| `AdminLayout.razor` | khung giao diện quản trị |
| `PersonaLayout.razor` | khung workspace nghiệp vụ |
| `AdminNavigationState.cs` | lưu state sidebar, palette, chatbot, notification badge |
| `AdminTopBar.razor` | top bar, user menu, flyout thông báo, theme, fullscreen |
| `AdminDashboard.razor` | trang tổng quan KPI |
| `ProfilePage.razor` | hồ sơ cá nhân, đổi mật khẩu |
| `CaiDatPage.razor` | cài đặt UI, AI, notification, logout |
| `ThemeBus.cs` | phát sự kiện đổi theme/motion/export |

## 10A.3. Luồng vào shell

```text
User mở route admin hoặc persona
-> Layout kiểm tra CurrentUser
-> nếu chưa có thì gọi BrowserSessionService.RestoreCurrentUserAsync()
-> nếu restore thành công thì render shell
-> AdminNavigationState sinh menu theo quyền
-> AdminTopBar khởi tạo HubConnection để nhận notification realtime
```

## 10A.4. Luồng dashboard

```text
User vào /admin
-> AdminDashboard lấy dữ liệu từ IMedDataStore
-> đếm:
   - procedure active
   - version active
   - user active
   - department active
   - order mở
   - cảnh báo tài nguyên
-> hiển thị KPI + activity gần đây
```

Dashboard là nơi đọc nhanh tình trạng hệ thống, không phải nơi sửa dữ liệu.

## 10A.5. Luồng command palette

```text
User bấm Ctrl+K
-> AdminNavigationState mở palette
-> palette hiển thị:
   - route được phép vào
   - hành động nhanh
-> user chọn một command
-> command sẽ:
   - điều hướng màn hình
   - mở chatbot
   - export báo cáo
   - đánh dấu tất cả notification đã đọc
   - đổi theme
```

## 10A.6. Luồng hồ sơ cá nhân

```text
User vào /admin/ho-so
-> ProfilePage load user hiện tại
-> hiển thị fullname, email, role, department
-> nếu đổi mật khẩu:
   - ValidationService kiểm tra dữ liệu
   - PasswordStrengthService chấm độ mạnh
   - cập nhật PasswordHash mới
```

## 10A.7. Luồng cài đặt

```text
User vào /admin/cai-dat
-> đọc preferences hiện tại
-> user đổi:
   - theme
   - density
   - animation
   - AI model
   - AI prompt
   - notification channel
-> UserPreferencesService cập nhật state
-> ThemeBus phát tín hiệu sang top bar / JS shell
-> một số cấu hình lưu trên browser/session của circuit
```

## 10A.8. Luồng logout

```text
User bấm logout
-> BrowserSessionService.SignOutAsync()
-> CurrentUserContext.SignOut()
-> xóa qlcm_uid khỏi sessionStorage
-> quay về /login
```

---

## 11. Luồng quy trình kỹ thuật theo version

## 11.1. Mục tiêu

Quản lý quy trình kỹ thuật theo phiên bản, không sửa trực tiếp bản đang hiệu lực.

## 11.2. Các màn hình

| Màn hình | Route | Mục tiêu |
|---|---|---|
| QuyTrinhTaoMoiPage | `/admin/quy-trinh/tao` | tạo quy trình và version |
| QuyTrinhKtPage | `/admin/quy-trinh` | xem, lọc, sửa, gửi duyệt |
| QuyTrinhPheDuyetPage | `/admin/quy-trinh/phe-duyet` | duyệt, từ chối, archive, restore |

## 11.3. Luồng tạo quy trình

```text
Admin tạo quy trình
-> tạo ProfessionalProcedure
-> tạo ProcedureVersion đầu tiên
-> thêm ProcedureStep
-> thêm ProcedureVersionResourceNorm
-> thêm ProcedureScreenMapping
-> thêm ProcedureAttachment
-> lưu draft hoặc pending_approval
```

## 11.4. Luồng gửi duyệt

```text
Version draft
-> ProcedureLifecycleService.Submit()
-> kiểm tra phải có ít nhất 1 step
-> status: draft -> pending_approval
-> ghi audit submit
```

## 11.5. Luồng publish

```text
Approver duyệt version
-> ProcedureLifecycleService.Publish()
-> version pending_approval -> active
-> version active cũ -> superseded
-> set approved/published/effective time
-> ghi workflow log + audit
```

## 11.6. Luồng từ chối hoặc thu hồi

| Tình huống | Chuyển trạng thái |
|---|---|
| từ chối bản chờ duyệt | `pending_approval -> rejected` |
| archive bản | `draft/pending_approval/rejected/active -> archived` theo rule guard |
| restore bản archive/rejected | `archived/rejected -> draft` |
| withdraw bản active | `active -> archived` |

## 11.7. Dữ liệu liên quan

| Bảng | Vai trò |
|---|---|
| `professional_procedures` | quy trình gốc |
| `procedure_versions` | từng phiên bản |
| `procedure_steps` | từng bước trong version |
| `procedure_screen_mappings` | map quy trình với màn hình/chức năng |
| `procedure_attachments` | SOP, file đính kèm |
| `procedure_version_resource_norms` | định mức tài nguyên theo version |

---

## 12. Luồng runtime guard theo quy trình chuyên môn

## 12.1. Mục tiêu

Không chỉ có quyền là đủ. Người dùng còn phải thao tác đúng vai trò được quy trình đang active yêu cầu.

## 12.2. Luồng xử lý

```text
User bấm một action
-> AdminActionGuard nhận permissionCode
-> ProcedureRuntimeGuard.EvaluatePermission(permissionCode)
-> tách screen code từ permissionCode
-> tìm ScreenCatalog đang active
-> tìm ProcedureScreenMapping của version active
-> tìm bước required đầu tiên
-> lấy actor role của bước đó
-> kiểm tra user hiện tại có role phù hợp không
-> nếu không:
   - warning: cảnh báo nhưng vẫn cho làm
   - block: chặn luôn
-> ghi audit deviation
```

## 12.3. Ý nghĩa nghiệp vụ

Ví dụ:

- user có quyền sửa chỉ định
- nhưng quy trình active quy định bước đầu chỉ bác sĩ trưởng khoa được thực hiện
- khi đó runtime guard sẽ cảnh báo hoặc chặn

Đây là điểm làm cho hệ thống bám sát “quy trình chuyên môn”, chứ không chỉ là CRUD đơn thuần.

---

## 13. Luồng danh mục kỹ thuật và định mức

## 13.1. Mục tiêu

Khai báo dịch vụ kỹ thuật và định mức nguồn lực cần dùng cho dịch vụ đó.

## 13.2. Màn hình

- `DanhMucPage.razor`

## 13.3. Luồng hoạt động

```text
Admin tạo TechnicalService
-> nhập code, name, type, department, procedure liên kết
-> lưu technical_services

Admin thêm norm
-> chọn resource
-> nhập standard quantity, unit, required, note
-> lưu technical_resource_norms
```

## 13.4. Dữ liệu vào

| Dữ liệu | Ý nghĩa |
|---|---|
| ServiceCode | mã dịch vụ kỹ thuật |
| Name | tên dịch vụ |
| Type | loại kỹ thuật |
| Department | khoa/phòng phụ trách |
| LinkedProcedureId | quy trình liên kết nếu có |
| Resource norms | định mức nguồn lực |

## 13.5. Dữ liệu ra

- danh mục kỹ thuật cho module chỉ định sử dụng
- định mức để so sánh với nguồn lực và tiêu hao thực tế

---

## 14. Luồng tài nguyên

## 14.1. Mục tiêu

Quản lý danh mục vật tư, thuốc, hóa chất, thiết bị để các module khác tra cứu.

## 14.2. Màn hình

- `ResourcePage.razor`

## 14.3. Luồng hoạt động

```text
Admin tạo ResourceCatalogItem
-> nhập code, name, type, unit mặc định
-> lưu resource_catalog
-> có thể archive tài nguyên
```

## 14.4. Phần này liên kết với đâu

- `DanhMucPage`: dùng để tạo norm
- `OrderPage`: dùng để tạo snapshot và ghi actual usage
- `SqlReportService`: dùng để hiển thị báo cáo tiêu thụ

---

## 15. Luồng chỉ định kỹ thuật và thực hiện kỹ thuật

## 15.1. Mục tiêu

Tạo chỉ định kỹ thuật cho bệnh nhân, theo dõi trạng thái, kiểm tra nguồn lực, ghi nhận tiêu hao thực tế.

## 15.2. Màn hình

- `OrderPage.razor`

## 15.3. Luồng tạo chỉ định

```text
User tạo chỉ định
-> chọn technical service
-> chọn patient hoặc encounter
-> chọn ordering department
-> nếu service có linked procedure
   -> tìm active procedure version
-> tạo TechnicalOrder:
   OrderStatus = ordered
-> ghi audit create_order
```

## 15.4. Luồng đổi trạng thái chỉ định

```text
ordered -> scheduled
scheduled -> in_progress
in_progress -> completed
ordered/scheduled/in_progress -> cancelled
```

Việc đổi trạng thái đi qua:

- `TechnicalOrderWorkflowService`
- `TechnicalOrderWorkflowGuard`

## 15.5. Luồng kiểm tra nguồn lực

```text
User bấm "Kiểm tra nguồn lực"
-> InventoryAvailabilityService.CreateMissingSnapshots(order)
-> lấy norm từ ProcedureVersionResourceNorm trước
-> nếu không có thì lấy từ TechnicalResourceNorm
-> tạo ResourceAvailabilitySnapshot cho từng resource
-> đánh dấu available hoặc insufficient
-> cảnh báo nếu thiếu
```

## 15.6. Luồng ghi nhận sử dụng thực tế

```text
User chọn resource đã dùng
-> nhập quantity, unit, reason
-> thêm ActualResourceUsage
-> hệ thống dùng dữ liệu này cho báo cáo tiêu thụ
```

## 15.7. Dữ liệu liên quan

| Bảng | Vai trò |
|---|---|
| `technical_orders` | phiếu chỉ định kỹ thuật |
| `resource_availability_snapshots` | snapshot đủ/thiếu nguồn lực |
| `actual_resource_usages` | tiêu hao thực tế |
| `technical_resource_norms` | định mức dịch vụ |
| `procedure_version_resource_norms` | định mức theo quy trình version |

---

## 16. Luồng phác đồ lâm sàng

## 16.1. Mục tiêu

Quản lý phác đồ lâm sàng theo version, tạo rule áp dụng, publish để dùng cho bệnh nhân.

## 16.2. Màn hình

- `PhacDoPage.razor`
- `ProtocolPage.razor`

## 16.3. Luồng tạo phác đồ

```text
Admin tạo ClinicalProtocol
-> lưu protocol gốc
-> tạo ClinicalProtocolVersion draft đầu tiên
```

## 16.4. Luồng sửa phác đồ đang active

```text
Nếu protocol đang có version active
-> không sửa trực tiếp bản active
-> tạo draft mới từ bản active
-> user chỉnh draft
-> gửi duyệt lại
```

## 16.5. Luồng publish phác đồ

```text
draft -> pending_approval
pending_approval -> active
active cũ -> superseded
```

## 16.6. Rule phác đồ

Rule được lưu trong `protocol_applicability_rules`, ví dụ:

- ICD cụ thể
- khoảng ICD
- giới tính
- khoa/phòng
- khoảng tuổi
- chống chỉ định

## 16.7. Dữ liệu liên quan

| Bảng | Vai trò |
|---|---|
| `clinical_protocols` | phác đồ gốc |
| `clinical_protocol_versions` | từng version |
| `clinical_protocol_procedures` | liên kết phác đồ với procedure version |
| `protocol_applicability_rules` | luật áp dụng / chống chỉ định |

---

## 17. Luồng bệnh nhân, lượt khám và áp dụng phác đồ

## 17.1. Mục tiêu

Cho bác sĩ làm việc với bệnh nhân, tạo encounter, gợi ý phác đồ, áp dụng phác đồ.

## 17.2. Màn hình

- `ClinicalPage.razor`

## 17.3. Luồng bệnh nhân

```text
User tạo hoặc sửa PatientRef
-> lưu mã bệnh nhân, tên hiển thị, ngày sinh, giới tính
```

## 17.4. Luồng lượt khám

```text
User chọn bệnh nhân
-> tạo EncounterRef
-> nhập encounter external id, department, thời điểm bắt đầu
```

## 17.5. Luồng gợi ý phác đồ

```text
User nhập ICD + chọn encounter
-> ClinicalProtocolSuggestionService.Suggest()
-> lấy các ClinicalProtocolVersion đang active
-> duyệt rule của từng version
-> tính score nếu rule khớp
-> loại protocol nếu gặp contraindication
-> trả danh sách gợi ý có điểm cao nhất trước
```

## 17.6. Luồng áp dụng phác đồ

```text
User chọn protocol version
-> AddPatientProtocolApplication()
-> lưu:
   - patient
   - encounter
   - protocol version
   - ICD
   - trạng thái
   - decision context
-> decision context ghi lại:
   - áp tay hay từ suggestion
   - score
   - reasons
   - warnings
```

## 17.7. Dữ liệu liên quan

| Bảng | Vai trò |
|---|---|
| `patient_refs` | bệnh nhân |
| `encounter_refs` | lượt khám |
| `patient_protocol_applications` | lịch sử áp dụng phác đồ cho bệnh nhân |

---

## 18. Luồng lâm sàng quản trị và chữ ký

## 18.1. Mục tiêu

Cho admin hoặc người có quyền chỉnh hồ sơ áp dụng phác đồ, đổi trạng thái và ký xác nhận.

## 18.2. Màn hình

- `LamSangPage.razor`

## 18.3. Luồng lưu dữ liệu lâm sàng

```text
Admin nhập patient + encounter + protocol version
-> nếu patient chưa có: tạo PatientRef
-> nếu encounter chưa có: tạo EncounterRef
-> tạo hoặc sửa PatientProtocolApplication
```

## 18.4. Luồng ký demo

```text
User có quyền ký
-> OpenSign()
-> SignatureService.CreateDemoSignatureAsync()
-> kiểm tra target có hợp lệ không
-> kiểm tra hồ sơ đang ở trạng thái có thể ký không
-> tạo SignatureRecord
-> application: applied -> signed
-> ghi audit sign
```

## 18.5. Luồng thu hồi chữ ký

```text
User có quyền thu hồi
-> nhập lý do
-> SignatureService.RevokeDemoSignatureAsync()
-> application: signed/applied -> revoked
-> ghi audit revoke
```

## 18.6. Dữ liệu liên quan

| Bảng | Vai trò |
|---|---|
| `patient_protocol_applications` | hồ sơ áp dụng phác đồ |
| `signature_records` | chữ ký demo |
| `audit_logs` | lịch sử ký / thu hồi |

---

## 19. Luồng thông báo

## 19.1. Mục tiêu

Thông báo cho người dùng khi có thay đổi quan trọng và vẫn lưu lại lịch sử để xem sau.

## 19.2. Thành phần

- `SignalRNotificationService`
- `SignalRNotificationRealtimePublisher`
- `NotificationHub`
- `AdminTopBar`
- `NotificationPage`

## 19.3. Luồng gửi thông báo

```text
Service nghiệp vụ muốn thông báo
-> SendToUserAsync()
-> persist vào med.notifications trước
-> gửi realtime qua SignalR group user:{userId}
-> AdminTopBar đang kết nối hub sẽ nhận toast ngay
```

## 19.4. Luồng xem thông báo

```text
User mở NotificationPage
-> thấy danh sách notification của chính mình
-> mở chi tiết
-> đánh dấu read
-> chỉnh preference theo loại thông báo và channel
```

## 19.5. Dữ liệu liên quan

| Bảng | Vai trò |
|---|---|
| `notifications` | thông báo user-targeted |
| `notification_preferences` | cài đặt nhận thông báo |
| `notification_delivery_attempts` | lịch sử thử gửi |

---

## 20. Luồng audit log

## 20.1. Mục tiêu

Ghi lại mọi thay đổi quan trọng để truy vết.

## 20.2. Hai nguồn audit chính

### Nguồn 1: tự động từ `MedDbContext`

Khi `SaveChanges()`:

- entity Added -> action `create`
- entity Modified -> action `update`
- entity Deleted -> action `delete`

`MedDbContext` tự thêm `AuditLog` mới bằng ChangeTracker.

### Nguồn 2: audit nghiệp vụ

Các service và page còn ghi thêm audit riêng cho các hành động quan trọng:

- login
- submit/approve/reject/publish quy trình
- create order / complete order
- sign / revoke
- apply permission change

## 20.3. Màn hình xem audit

- `AuditLogPage.razor`

---

## 21. Luồng báo cáo và dashboard

## 21.1. Mục tiêu

Biến dữ liệu nghiệp vụ thành số liệu quản trị và báo cáo vận hành.

## 21.2. Màn hình

| Màn hình | Route | Mục tiêu |
|---|---|---|
| AdminDashboard | `/admin` | xem KPI tổng quan |
| BaoCaoPage | `/admin/bao-cao` | xem tổng hợp chỉ số |
| BaoCaoTieuThuPage | `/admin/bao-cao/tieu-thu` | xem tiêu thụ so với định mức |

## 21.3. `SqlReportService` hoạt động như thế nào

### `GenerateConsumptionReportForDepartment`

Luồng xử lý:

1. nhận khoảng ngày và `departmentId`
2. đổi sang mốc UTC
3. resolve cây khoa/phòng từ `DepartmentClosure`
4. tải dictionaries:
   - `TechnicalServices`
   - `ResourceCatalog`
   - `TechnicalOrders`
   - `TechnicalResourceNorms`
   - `ProcedureVersionResourceNorms`
5. đọc `ActualResourceUsages` final trong khoảng ngày
6. với từng usage:
   - tìm order
   - tìm service
   - tìm resource
   - tìm định mức chuẩn:
     - ưu tiên `ProcedureVersionResourceNorm`
     - nếu không có thì dùng `TechnicalResourceNorm`
   - tính variance
   - tính variancePercent
7. sắp xếp theo độ lệch lớn nhất
8. trả về `ConsumptionReportRow`

### `GetDashboardKpis`

Tính:

- số quy trình active
- số version active
- số phác đồ active
- số thông báo chưa đọc
- số user active
- tỷ lệ tuân thủ quy trình

### `GetActivityFeed`

- lấy audit log mới nhất
- đổi sang `ActivityEntry`

### `GetActivityTrend`

- gom audit log theo ngày
- trả series để vẽ chart

## 21.4. Dữ liệu vào

| Dữ liệu | Nguồn |
|---|---|
| khoảng ngày | user chọn trên UI |
| khoa/phòng | user lọc |
| actual usages | từ Order module |
| norms | từ Danh mục và Quy trình |

## 21.5. Dữ liệu ra

| Dữ liệu ra | Ý nghĩa |
|---|---|
| KPI dashboard | tình hình hệ thống |
| Activity feed | thao tác gần đây |
| Activity trend | số thao tác theo ngày |
| Consumption report | chênh lệch tiêu hao so với định mức |

---

## 22. Luồng chatbot

## 22.1. Mục tiêu

Hỗ trợ người dùng hỏi nhanh trong hệ thống, nhưng vẫn giữ an toàn điều hướng.

## 22.2. Thành phần

- `ChatbotPanel.razor`
- `ChatbotService.cs`
- `GeminiChatbotClient.cs`
- `DemoChatbotClient.cs`
- `ChatActionParser.cs`

## 22.3. Luồng hoạt động

```text
User mở chatbot
-> nhập câu hỏi
-> ChatbotService lưu user message
-> tạo assistant placeholder
-> gọi StreamReplyAsync() của client
-> stream chunk về panel
-> panel hiển thị dần
```

## 22.4. Hai chế độ

| Chế độ | Khi nào dùng |
|---|---|
| Gemini | khi có API key |
| Demo | khi chưa cấu hình API key |

## 22.5. Cơ chế an toàn

- chỉ cho điều hướng tới whitelist route
- draft dữ liệu được để trong `sessionStorage`
- route chỉ mang `draft_nonce`
- trang lâm sàng đọc xong draft thì xóa

---

## 23. Luồng job nền và vận hành hệ thống

## 23.1. Hangfire

Job hiện tại nổi bật nhất:

- `qlcm-apply-scheduled-permission-changes`

Chạy mỗi phút để:

- tìm request đổi quyền đã tới thời điểm hiệu lực
- áp quyền
- cập nhật trạng thái request

## 23.2. Health check

Endpoint:

- `/health`

Dùng để kiểm tra:

- DB context
- SQL Server dependency

---

## 24. Bảng hướng dẫn sử dụng theo vai trò

## 24.1. Quản trị viên hệ thống

| Bước | Màn hình | Việc cần làm | Kết quả |
|---|---|---|---|
| 1 | Người dùng | duyệt tài khoản mới | user được kích hoạt |
| 2 | Khoa/phòng | tạo cây tổ chức | có scope nghiệp vụ |
| 3 | Vai trò | tạo vai trò | có role để gán |
| 4 | Nhóm | tạo nhóm | gom user theo đơn vị |
| 5 | Phân quyền | tạo yêu cầu đổi quyền | request chờ duyệt |
| 6 | Quy trình | tạo quy trình kỹ thuật | có version draft/pending |
| 7 | Phê duyệt quy trình | publish version | version active |
| 8 | Danh mục | tạo kỹ thuật và định mức | có norm cho chỉ định |
| 9 | Tài nguyên | tạo vật tư/thuốc/thiết bị | có resource catalog |
| 10 | Phác đồ | tạo phác đồ, rule, publish | có protocol active |
| 11 | Báo cáo | xem KPI và tiêu thụ | quản trị vận hành |

## 24.2. Người phê duyệt quyền

| Bước | Màn hình | Việc cần làm |
|---|---|---|
| 1 | `/phe-duyet` | xem request chờ duyệt |
| 2 | Chi tiết request | đối chiếu before/after, reason |
| 3 | Approve hoặc Reject | áp quyền hoặc trả lại |

## 24.3. Bác sĩ / người dùng lâm sàng

| Bước | Màn hình | Việc cần làm |
|---|---|---|
| 1 | Clinical | tìm hoặc tạo bệnh nhân |
| 2 | Clinical | tạo encounter |
| 3 | Clinical | nhập ICD |
| 4 | Clinical | nhận gợi ý phác đồ |
| 5 | Clinical | áp dụng phác đồ |
| 6 | Điều phối | tạo chỉ định kỹ thuật nếu cần |
| 7 | LamSang | ký xác nhận hồ sơ nếu có quyền |

## 24.4. Kỹ thuật viên / điều dưỡng

| Bước | Màn hình | Việc cần làm |
|---|---|---|
| 1 | Điều phối | xem chỉ định |
| 2 | Điều phối | chuyển trạng thái scheduled / in_progress |
| 3 | Điều phối | kiểm tra nguồn lực |
| 4 | Điều phối | ghi actual usage |
| 5 | Điều phối | hoàn tất kỹ thuật |

---

## 25. Bảng “mục này liên kết với mục nào”

| Mục | Liên kết đến | Lý do liên kết |
|---|---|---|
| User | Role, Group, Permission | để tính quyền hiệu lực |
| Department | User, Group, Procedure, Order, Protocol | để scope chuyên môn |
| Procedure | TechnicalService, ScreenMapping, RuntimeGuard | để kiểm soát thao tác |
| TechnicalService | ResourceNorm, TechnicalOrder | để tạo chỉ định |
| ResourceCatalog | Norm, Snapshot, ActualUsage, Report | để tính đủ/thiếu và tiêu hao |
| ClinicalProtocol | ApplicabilityRule, PatientProtocolApplication | để gợi ý và áp dụng |
| Notification | TopBar, NotificationPage, realtime hub | để thông báo tức thời và xem lại |
| AuditLog | Dashboard, AuditLogPage | để truy vết |
| PermissionChangeRequest | InboxPage, Hangfire, Notifications | để duyệt và áp quyền |

---

## 26. Nếu bạn muốn đọc source theo thứ tự ít bị ngợp nhất

## Chặng 1: hiểu bức tranh lớn

1. `README.md`
2. `docs/system-architecture.md`
3. `src/telemedicine-landing-page/Program.cs`
4. `src/telemedicine-landing-page/Infrastructure/QlcmServiceCollectionExtensions.cs`
5. `src/telemedicine-landing-page/Data/MedDbContext.cs`

## Chặng 2: hiểu xác thực và điều hướng

1. `Services/Admin/Sql/ICurrentUserContext.cs`
2. `Services/Admin/Sql/CurrentUserContext.cs`
3. `Services/Auth/CurrentUserAuthenticationStateProvider.cs`
4. `Services/Admin/Sql/NavGate.cs`
5. `Services/Admin/Sql/AdminActionGuard.cs`

## Chặng 3: hiểu phân quyền

1. `Services/Admin/Sql/EffectivePermissionResolver.cs`
2. `Components/Pages/Admin/PhanQuyenPage.razor`
3. `Components/Pages/PermissionApprover/InboxPage.razor`
4. `Services/Admin/Sql/PermissionChangeRequestService.cs`

## Chặng 4: hiểu quy trình kỹ thuật

1. `Components/Pages/Admin/QuyTrinhTaoMoiPage.razor`
2. `Services/Admin/Sql/ProcedureLifecycleService.cs`
3. `Services/Admin/Sql/ProcedureRuntimeGuard.cs`
4. `Components/Pages/Admin/QuyTrinhPheDuyetPage.razor`

## Chặng 5: hiểu chỉ định và tiêu hao

1. `Components/Pages/Order/OrderPage.razor`
2. `Services/Admin/Sql/TechnicalOrderWorkflowService.cs`
3. `Services/Admin/Sql/InventoryAvailabilityService.cs`
4. `Services/Admin/Sql/SqlReportService.cs`

## Chặng 6: hiểu phác đồ và lâm sàng

1. `Components/Pages/Admin/PhacDoPage.razor`
2. `Components/Pages/Clinical/ClinicalPage.razor`
3. `Components/Pages/Admin/LamSangPage.razor`
4. `Services/Admin/Sql/ClinicalProtocolSuggestionService.cs`
5. `Application/Signature/SignatureService.cs`

## Chặng 7: hiểu thông báo và chatbot

1. `Services/Notifications/SignalRNotificationService.cs`
2. `Hubs/NotificationHub.cs`
3. `Components/Pages/Notification/NotificationPage.razor`
4. `Services/Chatbot/ChatbotService.cs`

---

## 27. Kết luận dễ nhớ nhất

Bạn có thể nhớ hệ thống này bằng công thức:

```text
QLCM Pro
= Tài khoản và tổ chức
+ Phân quyền nhiều lớp
+ Quy trình kỹ thuật theo version
+ Danh mục và định mức tài nguyên
+ Chỉ định kỹ thuật và tiêu hao thực tế
+ Phác đồ lâm sàng và gợi ý theo ICD
+ Ký xác nhận hồ sơ
+ Thông báo + audit + báo cáo
```

Nếu chỉ nhớ một câu:

**Đây là hệ thống quản lý chuyên môn bệnh viện, trong đó mọi thao tác quan trọng đều đi qua 3 lớp kiểm soát: user là ai, user có quyền gì, và user có đang thao tác đúng quy trình chuyên môn đang hiệu lực hay không.**
