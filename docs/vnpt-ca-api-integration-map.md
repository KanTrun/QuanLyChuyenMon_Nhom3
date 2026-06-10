# VNPT CA API Integration Map

Ngay cap nhat: 2026-06-10

## Doc nhanh cho nguoi moi

Neu ban khong biet lay API o dau, lam theo thu tu nay:

1. Mo trang dang ky tich hop SmartCA:
   `https://doitac-smartca.vnpt.vn/help/docs/tich-hop/ky-so/webapi/tao-tai-khoan-tich-hop/`
2. Dang nhap/dang ky tai khoan SmartCA for Developers.
3. Tao thong tin he thong tich hop. Sau khi VNPT duyet/test, ho gui ve email:
   `Client_Id`/`Client_Secret` hoac bo credential SP tuong duong.
4. Mo trang tai tai lieu SmartCA WebAPI:
   `https://doitac-smartca.vnpt.vn/help/docs/tich-hop/ky-so/webapi/tai-lieu-tich-hop-chi-tiet/`
5. Tai PDF `SmartCA` va `SmartCA Tich hop`. API ky so truc tiep cua QLCM nam trong PDF SmartCA.
6. Neu can hop dong dien tu nhieu ben, mo trang eContract:
   `https://doitac-smartca.vnpt.vn/help/docs/tich-hop/hop-dong-dien-tu/webapi/tai-lieu-tich-hop-chi-tiet/`
7. Tai PDF eContract. Day la luong khac, chua gan vao code QLCM hien tai.

Noi ngan gon:

- Can ky ho so lam sang bang CA: dung SmartCA WebAPI.
- Can hop dong dien tu nhieu nguoi ky/OTP/email/SMS: dung eContract API.
- QLCM hien da gan SmartCA WebAPI. eContract chua gan.

## Lay credential/API o trang nao

| Ban can lay | Vao dau | Muc tren trang/tai lieu | Dien vao dau |
| --- | --- | --- | --- |
| Tai khoan developer | `https://doitac-smartca.vnpt.vn/help/docs/tich-hop/ky-so/webapi/tao-tai-khoan-tich-hop/` | Buoc 1: dang nhap/dang ky SmartCA for Developers | Khong dien vao code; chi dung de tao he thong tich hop. |
| `Client_Id` / `SP_ID` | Trang tren | Buoc 2: Tao thong tin he thong tich hop, VNPT gui email sau khi dang ky thanh cong | `.env` -> `SMARTCA_SP_ID`; Docker map sang `SmartCa__SpId`; code doc o `SmartCaOptions.cs`. |
| `Client_Secret` / `SP_PASSWORD` | Trang tren | Email cua quan tri vien nhan tai khoan tich hop | `.env` -> `SMARTCA_SP_PASSWORD`; Docker map sang `SmartCa__SpPassword`; code doc o `SmartCaOptions.cs`. |
| Gateway sandbox | PDF SmartCA WebAPI | Base URL/API prefix sandbox | `.env` -> `SMARTCA_BASE_URL=https://rmgateway.vnptit.vn`, `SMARTCA_API_PREFIX=/sca/sp769`. |
| So thue bao SmartCA/CCCD/MST nguoi ky | App demo SmartCA/VNPT sandbox account | Nguoi dung kich hoat chung thu so tren app demo | `.env` -> `SMARTCA_DEFAULT_USER_ID`; bind voi user app bang `SMARTCA_SIGNER_USERNAME=admin` hoac JSON. |
| Serial chung thu | API `get_certificate` hoac danh sach chung thu SmartCA trong tai lieu | Dung khi mot thue bao co nhieu chung thu | `.env` -> `SMARTCA_DEFAULT_SERIAL_NUMBER`, co the de trong neu VNPT khong yeu cau. |
| Callback URL | Server public cua minh, khong lay tu VNPT | Dang ky cho VNPT goi lai sau khi ky | `.env` -> `SMARTCA_CALLBACK_URL=https://domain/api/signatures/smartca/callback`; endpoint code o `SmartCaSignatureEndpoints.cs`. |
| Callback secret | Minh tu dat/sinh | Chia se voi gateway/cau hinh callback de verify request | `.env` -> `SMARTCA_CALLBACK_SECRET`; header can gui la `X-QLCM-SMARTCA-CALLBACK-SECRET`. |

Neu chua chac dien tay, chay:

```powershell
.\scripts\configure-smartca-env.ps1
docker compose up --build -d web
.\scripts\smoke-smartca-api.ps1
```

Script tren chi tao/cap nhat `.env` local. File `.env` bi gitignore, khong day secret len GitHub.

## Ban do folder/file trong project

Project goc: `d:/BenhVienQuanLy_Nhom3/QuanLyChuyenMon_Nhom3`

| Viec can tim | Folder/file | Ban nen sua/kiem tra gi |
| --- | --- | --- |
| Noi goi API VNPT that | `src/telemedicine-landing-page/Application/Signature/SmartCaClient.cs` | Them/sua endpoint VNPT o day. |
| Interface cua client CA | `src/telemedicine-landing-page/Application/Signature/ISmartCaClient.cs` | Neu them API moi, them method o day truoc. |
| Payload/response JSON VNPT | `src/telemedicine-landing-page/Application/Signature/SmartCaModels.cs` | Them record model neu API moi co body/response moi. |
| Cau hinh sandbox/credential | `src/telemedicine-landing-page/Application/Signature/SmartCaOptions.cs` | Them bien cau hinh, bind user app voi user CA. |
| Nghiep vu ky ho so | `src/telemedicine-landing-page/Application/Signature/SignatureService.cs` | Gan luong UI -> API -> DB. Khong goi VNPT truc tiep trong UI. |
| Nut bam UI SmartCA | `src/telemedicine-landing-page/Components/Pages/Admin/LamSangPage.razor` | Man hinh Lam sang, modal ky, nut gui/kiem tra trang thai. |
| Dang ky DI/HttpClient | `src/telemedicine-landing-page/Infrastructure/QlcmServiceCollectionExtensions.cs` | Dang ky client, base URL, timeout. |
| Config mac dinh app | `src/telemedicine-landing-page/appsettings.json` | Chi de default rong/false, khong dat secret. |
| Config Docker | `docker-compose.yml` | Map `SMARTCA_*` vao `SmartCa__*`. |
| Mau env local | `.env.example` | Huong dan bien moi, khong ghi secret that. |
| HTTP API cho Docker/app khac | `src/telemedicine-landing-page/Infrastructure/SmartCaSignatureEndpoints.cs` | Expose `/api/signatures/smartca/*`, co auth/callback secret. |
| DB giao dich dang cho CA | `src/telemedicine-landing-page/Models/Admin/Sql/SignatureTransactions.cs` | Luu transaction CA cho den khi ky xong. |
| DB chu ky cuoi | `src/telemedicine-landing-page/Models/Admin/Sql/SignatureRecords.cs` | Luu evidence cuoi: provider, cert subject/serial/expiry. |
| Migration DB | `scripts/migrations/20260604-010-add-smartca-signature-transactions.sql` | Tao bang pending transaction. |
| Test SmartCA | `tests/telemedicine-landing-page.tests/Admin/Sql/SignatureServiceTests.cs` | Them test khi sua nghiep vu ky. |
| Export/phieu in | `src/telemedicine-landing-page/Services/Admin/ClinicalExportService.cs` | Hien provider/chung thu tren file export. |

## API nao gan vao file nao

| API tu tai lieu VNPT | Lay o trang nao | Da gan chua | File/method dang gan |
| --- | --- | --- | --- |
| `POST /sca/sp769/v1/signatures/sign` | SmartCA WebAPI PDF | Da gan | `SmartCaClient.StartHashSignatureAsync()` -> `SignatureService.StartSmartCaSignatureAsync()` -> `LamSangPage.StartSmartCaSign()` |
| `POST /sca/sp769/v1/signatures/sign/{tranId}/status` | SmartCA WebAPI PDF | Da gan | `SmartCaClient.GetSignatureStatusAsync()` -> `SignatureService.RefreshSmartCaSignatureAsync()` -> `LamSangPage.RefreshSmartCaSign()` |
| `POST /sca/sp769/v1/credentials/get_certificate` | SmartCA WebAPI PDF | Da gan | `SmartCaClient.GetCertificateAsync()` -> `SignatureService.GetRequiredSmartCaCertificateAsync()` |
| SmartCA callback/webhook | VNPT cau hinh khi co public URL | Da gan entrypoint | `POST /api/signatures/smartca/callback` trong `SmartCaSignatureEndpoints.cs`, goi `SignatureService.RefreshSmartCaSignatureByExternalReferenceAsync()` va poll VNPT truoc khi finalize. |
| SmartCA v2 `sign`/`confirm` | PDF SmartCA Tich hop | Chua gan | Them method vao `ISmartCaClient`, payload vao `SmartCaModels.cs`, orchestration vao `SignatureService.cs`. |
| eContract login | eContract PDF | Chua gan | Nen tao `Application/Signature/EContractClient.cs` va `EContractOptions.cs`. |
| eContract tao hop dong | eContract PDF | Chua gan | Nen tao service rieng, khong tron vao `SmartCaClient`. |
| eContract gui hop dong | eContract PDF | Chua gan | Gan vao service eContract + DB transaction rieng. |
| eContract ky OTP/SmartCA/HSM | eContract PDF | Chua gan | Can UI rieng, callback rieng, bao mat OTP/token rieng. |

## Bien Docker can dien khi co credential

File can xem: `.env.example` va `docker-compose.yml`.

```env
SMARTCA_ENABLED=true
SMARTCA_BASE_URL=https://rmgateway.vnptit.vn
SMARTCA_API_PREFIX=/sca/sp769
SMARTCA_SP_ID=<VNPT cap>
SMARTCA_SP_PASSWORD=<VNPT cap>
SMARTCA_SIGNER_USERNAME=admin
SMARTCA_DEFAULT_USER_ID=<so thue bao SmartCA sandbox>
SMARTCA_DEFAULT_SERIAL_NUMBER=<neu VNPT yeu cau>
SMARTCA_CALLBACK_URL=<de trong neu chua co public webhook>
SMARTCA_CALLBACK_SECRET=<secret rieng de verify callback>
```

Khong commit `.env` that. Chi commit `.env.example`.

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
- SmartCA da co API Docker/app khac: `GET /api/signatures/smartca/readiness`, `GET /api/signatures/smartca/transactions/latest`, `POST /api/signatures/smartca/start`, `POST /api/signatures/smartca/transactions/{id}/refresh`, `POST /api/signatures/smartca/callback`.
- Docker da co env `SMARTCA_*`.
- UI lam sang da co luong gui ky va poll trang thai.
- DB da co bang pending transaction va final signature evidence.
- Export ho so da co thong tin CA neu ky thanh cong.

## Viec nen lam tiep neu co credential

1. Dien `SMARTCA_ENABLED=true`, `SMARTCA_SP_ID`, `SMARTCA_SP_PASSWORD`.
2. Bind bac si/nguoi ky bang `SMARTCA_SIGNER_USERNAME` + `SMARTCA_DEFAULT_USER_ID`, hoac `SMARTCA_USER_BINDINGS_JSON`.
3. Test sandbox: gui ky, mo app SmartCA xac nhan, poll status, kiem tra `signature_records`.
4. Neu VNPT yeu cau callback: cau hinh public URL `https://domain/api/signatures/smartca/callback` va header `X-QLCM-SMARTCA-CALLBACK-SECRET`.
5. Neu nghiep vu can hop dong nhieu ben: thiet ke module eContract rieng, khong tron vao `SmartCaClient`.

## Cau hoi chua giai quyet

- Credential SP sandbox VNPT cua team la gi?
- Tenant cua minh dung `tran_code` hay `transaction_id` trong path `v1/signatures/sign/{tranId}/status`? Code dang dung `tran_code` vi response VNPT tra ma nay.
- QLCM can ky ho so lam sang truc tiep hay can them hop dong dien tu nhieu ben qua eContract?
- Co public callback URL de VNPT cau hinh webhook khong?
