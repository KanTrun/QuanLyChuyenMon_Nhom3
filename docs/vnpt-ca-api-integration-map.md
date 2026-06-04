# VNPT CA API Integration Map

Ngay cap nhat: 2026-06-04

## Muc tieu

File nay gom API CA/VNPT can biet de gan vao QLCM Pro. Tach 2 luong:

- SmartCA remote signing: ky hash ho so truc tiep voi CA. Luong nay da gan vao he thong.
- VNPT eContract: tao/gui/ky hop dong dien tu. Luong nay chua gan, chi nen them khi nghiep vu can hop dong nhieu ben, OTP, SmartCA app/HSM.

Nguon chinh thuc:

- SmartCA WebAPI: https://doitac-smartca.vnpt.vn/help/docs/tich-hop/ky-so/webapi/tai-lieu-tich-hop-chi-tiet/
- SmartCA PDF: https://doitac-smartca.vnpt.vn/help/document/Tai_lieu_tich_hop_smartca_v4.1.pdf
- eContract WebAPI: https://doitac-smartca.vnpt.vn/help/docs/tich-hop/hop-dong-dien-tu/webapi/tai-lieu-tich-hop-chi-tiet/
- eContract PDF: https://doitac-smartca.vnpt.vn/help/assets/files/API_VNPT_eContract_VNPT-5a9b8c1fb5c66f7ab294d8023bd2babf.pdf

## Sandbox/Production

| Nhom | Sandbox/POC | Production | Ghi chu |
| --- | --- | --- | --- |
| SmartCA ky so truc tiep | `https://rmgateway.vnptit.vn/sca/sp769` | `https://gwsca.vnpt.vn/sca/sp769` | Tat ca API dung `POST`, JSON. |
| eContract | `https://gateway-bus-econtract-v2-poc.vnpt.vn/` | `https://gateway-bus-econtract.vnpt.vn` | Dung Bearer JWT sau login. |

## SmartCA API da co the gan ngay

| API | Muc dich | Trang thai QLCM | Gan vao dau |
| --- | --- | --- | --- |
| `POST /sca/sp769/v1/credentials/get_certificate` | Lay chung thu so thue bao, serial, subject, han CTS. | Da co. | `Application/Signature/SmartCaClient.GetCertificateAsync`; bang cuoi `med.signature_records`. |
| `POST /sca/sp769/v1/signatures/sign` | Gui hash/file can ky cho CA, nguoi ky xac nhan tren app SmartCA. | Da co. He thong dang gui hash canonical cua ho so lam sang. | `SmartCaClient.StartHashSignatureAsync`; orchestrate o `SignatureService.StartSmartCaSignatureAsync`; UI `LamSangPage.razor`. |
| `POST /sca/sp769/v1/signatures/sign/{tranId}/status` | Poll trang thai, lay `signature_value`. | Da co, dang truyen `tran_code` VNPT tra ve sau API sign. Can test bang credential that. | `SmartCaClient.GetSignatureStatusAsync`; finalize o `SignatureService.RefreshSmartCaSignatureAsync`. |
| Webhook callback SmartCA | CA goi ve server khi ky xong. | Chua co endpoint callback. Hien dung poll status. | Them endpoint server-side, verify `sp_id`, `transaction_id`, `signed_files`, cap nhat `med.signature_transactions`. |
| `POST /sca/sp769/v2/signatures/sign` | SmartCA TH: ky tich hop bang password + OTP/TOTP, tra `sad`. | Chua co. Chi can neu mua goi SmartCA tich hop. | Mo rong `ISmartCaClient`, them model v2, UI can input OTP hoac co luong TOTP an toan. |
| `POST /sca/sp769/v2/signatures/confirm` | SmartCA TH confirm bang `sad`, tra chu ky ngay. | Chua co. | Gan sau v2 sign trong `SignatureService`, luu final evidence nhu SmartCA v1. |

## eContract API inventory

eContract la he hop dong dien tu rieng, khong thay the SmartCA direct signing. Neu ap dung vao QLCM, tao module moi vi no co auth/token, contract id, signer positions, file download rieng.

| Nhom API | Endpoint/huong dan chinh | Khi nao dung | Gan vao dau neu lam |
| --- | --- | --- | --- |
| Login | `POST /users-profile-service/auth/login`; `POST /users-profile-service/auth/login-ktk`; `POST /users-profile-service/auth/sso` | Lay JWT truoc khi goi eContract. | Tao `Application/Signature/EContractClient.cs`, config `EContract:*`, token cache server-side. |
| Tao hop dong tu mau | `POST /econtract-integration-service/vnpt-econtract/hopdong-b1-tumau` | Tao hop dong tu template VNPT va bien du lieu. | Service tao hop dong tu phieu/ho so, map bien tu DB. |
| Tao hop dong tu file | PDF muc `II.2.2` | Upload file/base64 de tao hop dong. | Can file generator truoc: PDF ho so/giay dong y/bao cao. |
| Gui ky hop dong | `POST /econtract-integration-service/vnpt-econtract/xu-ly-nguoi-ky-va-gui-hop-dong` | Gan danh sach nguoi ky, thu tu, vi tri ky, hinh thuc ky. | Orchestrator moi, bang pending eContract transactions. |
| Ky anh | `api/v1/tich-hop-ky/...` trong PDF muc `II.4.1` | Ky bang anh chu ky tay. | UI upload/draw signature, validate PNG/JPG, audit. |
| Email OTP | PDF muc `II.4.2`: gui OTP va xac nhan OTP. | Ben ky khong dung SmartCA app. | UI nhap OTP, service khong luu OTP. |
| SMS OTP | `POST /econtract-integration-service/api/v1/tich-hop-ky/sms-otp/hoan-thanh` va API gui OTP trong muc `II.4.3.1` | Ky bang SMS OTP. | Tuong tu email OTP, them phone masking/rate-limit. |
| SmartCA qua app | PDF muc `II.4.3`: ky hop dong bang SmartCA app, tra `signProviderTranId`. | Hop dong nhieu ben nhung xac thuc bang SmartCA app. | Dung khi quy trinh can eContract thay vi ky ho so noi bo. |
| SmartCA tu dong/HSM | `POST /econtract-integration-service/api/v1/tich-hop-ky/smartca/ky-tu-dong` | Ky tu dong, can tai khoan SmartCA nang cao/HSM. | Chi lam khi co goi dich vu va phe duyet bao mat. |
| Callback eContract | URL khach hang cung cap, header `X-APP-CB-KEY`, `X-APP-CB-SECRET` | Nhan trang thai hop dong tu VNPT. | Them API endpoint callback, verify secret, cap nhat transaction. |
| Tra cuu/phu tro | Them doi tac, huy/xoa hop dong, tai hop dong, chi tiet hop dong, vi tri ky, danh muc, tai khoan noi bo, login/danh sach SmartCA, mau hop dong, luong hop dong, tai base64, resend SMS OTP, chi tiet OTP. | Quan tri dong bo/troubleshoot. | Dat trong `EContractClient`; chi expose UI nhung cai can nghiep vu. |

## Map vao code hien tai

| Lop | File | Vai tro |
| --- | --- | --- |
| Raw CA client | `src/telemedicine-landing-page/Application/Signature/SmartCaClient.cs` | Goi endpoint VNPT SmartCA sandbox, parse envelope, map status. |
| Contract model | `src/telemedicine-landing-page/Application/Signature/SmartCaModels.cs` | Payload/response cho SmartCA v1. Them v2/eContract model o file moi khi can, tranh lam file qua lon. |
| Config/binding | `src/telemedicine-landing-page/Application/Signature/SmartCaOptions.cs` | Bind app user -> VNPT subscriber id/serial, check readiness. |
| Business orchestration | `src/telemedicine-landing-page/Application/Signature/SignatureService.cs` | Tao transaction, compute hash, poll, verify document id/certificate, tao chu ky hop phap. |
| UI nghiep vu | `src/telemedicine-landing-page/Components/Pages/Admin/LamSangPage.razor` | Modal ky ho so, hien readiness, transaction code, nut gui/poll. |
| Pending table | `src/telemedicine-landing-page/Models/Admin/Sql/SignatureTransactions.cs` | Luu giao dich CA dang cho ky. |
| Final evidence | `src/telemedicine-landing-page/Models/Admin/Sql/SignatureRecords.cs` | Luu chu ky cuoi, provider, subject/serial/expiry. |
| Docker/env | `docker-compose.yml`, `.env.example`, `docs/deployment-guide.md` | Map `SMARTCA_*` vao `SmartCa:*`. |

## Thiet ke UI/UX nen giu

- Tach ro "Ky phap ly SmartCA" va "Ky demo noi bo".
- Hien trang thai san sang: bat/tat, thieu SP credential, thieu binding nguoi ky.
- Khi gui ky, hien ma giao dich/ma tai lieu de nguoi ky doi chieu voi app SmartCA.
- Nut "Kiem tra trang thai" thay vi auto spam; co the them polling nhe sau nay.
- Final evidence phai hien provider, subject, serial, expiry, thoi gian ky.
- Khong hien SP secret, subscriber id day du, OTP, token eContract tren browser.

## Viec da lam

- SmartCA v1 sandbox da duong day server-side.
- Docker da co env `SMARTCA_*`.
- UI lam sang da co luong gui ky va poll trang thai.
- DB da co bang pending transaction va final signature evidence.
- Export ho so da co thong tin CA neu ky thanh cong.

## Viec nen lam tiep neu co credential

1. Dien `SMARTCA_ENABLED=true`, `SMARTCA_SP_ID`, `SMARTCA_SP_PASSWORD`.
2. Bind bac si/nguoi ky bang `SMARTCA_SIGNER_USERNAME` + `SMARTCA_DEFAULT_USER_ID`, hoac `SMARTCA_USER_BINDINGS_JSON`.
3. Test sandbox: gui ky, mo app SmartCA xac nhan, poll status, kiem tra `signature_records`.
4. Neu VNPT yeu cau callback: them endpoint callback sau khi co public URL va secret.
5. Neu nghiep vu can hop dong nhieu ben: thiet ke module eContract rieng, khong tron vao `SmartCaClient`.

## Cau hoi chua giai quyet

- Credential SP sandbox VNPT cua team la gi?
- Tenant cua minh dung `tran_code` hay `transaction_id` trong path `v1/signatures/sign/{tranId}/status`? Code dang dung `tran_code` vi response VNPT tra ma nay.
- QLCM can ky ho so lam sang truc tiep hay can them hop dong dien tu nhieu ben qua eContract?
- Co public callback URL de VNPT cau hinh webhook khong?
