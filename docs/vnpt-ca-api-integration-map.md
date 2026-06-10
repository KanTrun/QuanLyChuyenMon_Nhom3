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
5. Tai PDF `SmartCA` va `SmartCA Tich hop`. QLCM hien dang dung luong SP truc tiep `/sca/sp769`, nam trong PDF `SmartCA Tich hop` / `Tai_lieu_tich_hop_smartca_v4.1.pdf`.
6. Neu can hop dong dien tu nhieu ben, mo trang eContract:
   `https://doitac-smartca.vnpt.vn/help/docs/tich-hop/hop-dong-dien-tu/webapi/tai-lieu-tich-hop-chi-tiet/`
7. Tai PDF eContract. Day la luong khac, chua gan vao code QLCM hien tai.

Noi ngan gon:

- Can ky ho so lam sang bang CA theo code hien tai: dung SmartCA SP truc tiep `/sca/sp769`.
- Can hop dong dien tu nhieu nguoi ky/OTP/email/SMS: dung eContract API.
- Neu chi co `ClientId` dang `*.apps.smartcaapi.com` kem `MobileCode`: dung SmartCA OAuth/Bearer, nhung van can refresh token hoac username/password SmartCA de lay Bearer token.
- QLCM hien da gan ca SmartCA SP `/sca/sp769` va SmartCA OAuth/Bearer `/csc/...`. eContract chua gan.

## Neu nhin trang/anh VNPT ma khong thay API

Khong phai ban nhin sai. Trang web VNPT khong hien truc tiep cac endpoint nhu
`POST /sca/sp769/v1/signatures/sign` tren man hinh. Trang do chi la trang dieu
huong/tai lieu. API nam trong file PDF tai ve.

Lam dung nhu sau:

1. O menu trai chon `I. Tai lieu tich hop ky so` -> `WEB API`.
2. Mo `Tai lieu tich hop chi tiet`.
3. Trong noi dung trang, neu bam link `SmartCA`, ban se tai PDF OAuth/Bearer `TichHopKySoSmartCA.pdf`, dung `client_id`, `client_secret`, `/auth/token`, `/csc/...`.
4. Link `SmartCA Tich hop` tai PDF `Tai_lieu_tich_hop_smartca_v4.1.pdf`. Day la PDF co `/sca/sp769`, `sp_id`, `sp_password`, `v1/credentials/get_certificate`, `v1/signatures/sign`; code QLCM dang dung luong nay.
5. Mo PDF vua tai, bam `Ctrl + F` va tim cac tu khoa:
   - `signatures/sign`
   - `get_certificate`
   - `status`
   - `tran_code`
6. Lay endpoint trong PDF roi doi chieu voi bang `API nao gan vao file nao` ben duoi.

Noi that ngan:

| Ban dang nhin thay gi trong anh/trang | Y nghia | Viec can lam |
| --- | --- | --- |
| Chi thay menu `WEB API`, khong thay endpoint | Binh thuong, endpoint nam trong PDF | Bam `Tai lieu tich hop chi tiet` -> tai dung PDF theo luong can dung. |
| Thay `ClientId` dang `*.apps.smartcaapi.com` va `MobileCode` | Day la credential SmartCA OAuth/Bearer | Dat `SMARTCA_CREDENTIAL_MODE=OAuth`; can them `SMARTCA_OAUTH_REFRESH_TOKEN` hoac `SMARTCA_OAUTH_USERNAME/PASSWORD`. |
| Thay `sp_id`/sample dang `*.apps.signserviceapi.com` | Day la credential SP truc tiep cho `/sca/sp769` | Dien vao `.env` bang `SMARTCA_SP_ID` va `SMARTCA_SP_PASSWORD`. |
| Thay 2 file `SmartCA` va `SmartCA Tich hop` | Co 2 luong API khac nhau | QLCM dang dung `SmartCA Tich hop` cho `/sca/sp769`; `SmartCA` la OAuth/Bearer `/csc/...`. |
| Thay tai lieu eContract | Day la hop dong dien tu, khac SmartCA ky ho so | Chua gan vao QLCM, chi lam khi can hop dong nhieu ben. |

API khong gan vao anh hay trang VNPT. API duoc gan vao code QLCM tai:

- Endpoint VNPT raw: `src/telemedicine-landing-page/Application/Signature/SmartCaClient.cs`
- Payload/response: `src/telemedicine-landing-page/Application/Signature/SmartCaModels.cs`
- Nghiep vu ky: `src/telemedicine-landing-page/Application/Signature/SignatureService.cs`
- Nut UI ky: `src/telemedicine-landing-page/Components/Pages/Admin/LamSangPage.razor`
- API Docker noi bo: `src/telemedicine-landing-page/Infrastructure/SmartCaSignatureEndpoints.cs`
- Credential Docker/local: `.env`, `.env.example`, `docker-compose.yml`

## Lay credential/API o trang nao

| Ban can lay | Vao dau | Muc tren trang/tai lieu | Dien vao dau |
| --- | --- | --- | --- |
| Tai khoan developer | `https://doitac-smartca.vnpt.vn/help/docs/tich-hop/ky-so/webapi/tao-tai-khoan-tich-hop/` | Buoc 1: dang nhap/dang ky SmartCA for Developers | Khong dien vao code; chi dung de tao he thong tich hop. |
| OAuth `ClientId` / `ClientSecret` | Link `SmartCA` / PDF `TichHopKySoSmartCA.pdf` | Email co `*.apps.smartcaapi.com`, `MobileCode`, dung `/auth/token` va `/csc/...` | `.env` -> `SMARTCA_CREDENTIAL_MODE=OAuth`, `SMARTCA_SP_ID`/`SMARTCA_SP_PASSWORD` hoac `SMARTCA_OAUTH_CLIENT_ID`/`SMARTCA_OAUTH_CLIENT_SECRET`. |
| OAuth user grant | PDF `TichHopKySoSmartCA.pdf` | Authorization code hoac password grant cua nguoi ky SmartCA | `.env` -> `SMARTCA_OAUTH_REFRESH_TOKEN` hoac `SMARTCA_OAUTH_USERNAME`/`SMARTCA_OAUTH_PASSWORD`. Khong co muc nay thi chua ky duoc. |
| Direct `SP_ID` / `SP_PASSWORD` | Link `SmartCA Tich hop` / PDF `Tai_lieu_tich_hop_smartca_v4.1.pdf` | Credential SP cho `/sca/sp769`, trong tai lieu mau co dang `*.apps.signserviceapi.com` | `.env` -> `SMARTCA_SP_ID`/`SMARTCA_SP_PASSWORD`; Docker map sang `SmartCa__SpId`/`SmartCa__SpPassword`; code doc o `SmartCaOptions.cs`. |
| Gateway sandbox | PDF `SmartCA Tich hop` | Base URL/API prefix sandbox cho `/sca/sp769` | `.env` -> `SMARTCA_BASE_URL=https://rmgateway.vnptit.vn`, `SMARTCA_API_PREFIX=/sca/sp769`. |
| So thue bao SmartCA/CCCD/MST nguoi ky | App demo SmartCA/VNPT sandbox account | Nguoi dung kich hoat chung thu so tren app demo | `.env` -> `SMARTCA_DEFAULT_USER_ID`; bind voi user app bang `SMARTCA_SIGNER_USERNAME=admin` hoac JSON. |
| Serial chung thu | API `get_certificate` hoac danh sach chung thu SmartCA trong tai lieu | Dung khi mot thue bao co nhieu chung thu | `.env` -> `SMARTCA_DEFAULT_SERIAL_NUMBER`, co the de trong neu VNPT khong yeu cau. |
| Callback URL | Server public cua minh, khong lay tu VNPT | Dang ky cho VNPT goi lai sau khi ky | `.env` -> `SMARTCA_CALLBACK_URL=https://domain/api/signatures/smartca/callback`; endpoint code o `SmartCaSignatureEndpoints.cs`. |
| Callback secret | Minh tu dat/sinh | Chia se voi gateway/cau hinh callback de verify request | `.env` -> `SMARTCA_CALLBACK_SECRET`; header can gui la `X-QLCM-SMARTCA-CALLBACK-SECRET`. |

Neu chua chac dien tay, chay:

```powershell
.\scripts\configure-smartca-env.ps1
docker compose up --build -d web
.\scripts\smoke-smartca-api.ps1
.\scripts\test-smartca-vnpt-credential.ps1
.\scripts\test-smartca-oauth-credential.ps1
```

Script tren chi tao/cap nhat `.env` local. File `.env` bi gitignore, khong day secret len GitHub.
Script `test-smartca-vnpt-credential.ps1` goi `v1/credentials/get_certificate` de kiem tra credential SP `/sca/sp769` va subscriber voi VNPT, nhung khong in password ra terminal. Neu `SMARTCA_SP_ID` co duoi `*.apps.smartcaapi.com`, script se canh bao day la credential OAuth/Bearer va VNPT co the tra `401 sp_id or sp_password invalid` tren luong SP truc tiep.
Script `test-smartca-oauth-credential.ps1` kiem tra OAuth: neu chi co `ClientId`/`ClientSecret`/`MobileCode`, script se bao thieu `SMARTCA_OAUTH_REFRESH_TOKEN` hoac `SMARTCA_OAUTH_USERNAME/PASSWORD`; neu da co token/user password thi script goi `/auth/token` va `/csc/credentials/list`.

## Credential ban vua gui thuoc loai nao

Gia tri dang:

```text
ClientId: 40b0-...apps.smartcaapi.com
ClientSecret: <khong ghi vao git>
MobileCode: VNPTSmartCAPartner-...
```

la dau hieu cua luong `SmartCA` OAuth/Bearer trong PDF `TichHopKySoSmartCA.pdf`, khong phai bo SP truc tiep `/sca/sp769`.

Voi credential nay co 2 duong:

| Duong | Can them gi | Trang thai QLCM |
| --- | --- | --- |
| Tiep tuc luong `/sca/sp769` | VNPT cap bo SP direct: `sp_id`/`sp_password`, thuong co dang `*.apps.signserviceapi.com` trong tai lieu mau | Da code, can credential SP dung. |
| Dung credential `*.apps.smartcaapi.com` hien co | Can OAuth redirect/auth code hoac username/password SmartCA theo PDF OAuth, sau do goi `/csc/credentials/list`, `/csc/signature/signhash` | Da gan vao QLCM. Voi thong tin hien co van thieu user grant/token nen chua ky live duoc. |

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
| `POST /sca/sp769/v1/signatures/sign` | SmartCA SP direct PDF | Da gan | `SmartCaClient.StartHashSignatureAsync()` -> `SignatureService.StartSmartCaSignatureAsync()` -> `LamSangPage.StartSmartCaSign()` |
| `POST /sca/sp769/v1/signatures/sign/{tranId}/status` | SmartCA WebAPI PDF | Da gan | `SmartCaClient.GetSignatureStatusAsync()` -> `SignatureService.RefreshSmartCaSignatureAsync()` -> `LamSangPage.RefreshSmartCaSign()` |
| `POST /sca/sp769/v1/credentials/get_certificate` | SmartCA WebAPI PDF | Da gan | `SmartCaClient.GetCertificateAsync()` -> `SignatureService.GetRequiredSmartCaCertificateAsync()` |
| `POST /auth/token` | SmartCA OAuth/Bearer PDF | Da gan | `SmartCaClient.GetOAuthAccessTokenAsync()`, dung refresh token hoac password grant. |
| `POST /csc/credentials/list` | SmartCA OAuth/Bearer PDF | Da gan | `SmartCaClient.ResolveOAuthCredentialIdAsync()` khi chua set `SMARTCA_OAUTH_CREDENTIAL_ID`. |
| `POST /csc/credentials/info` | SmartCA OAuth/Bearer PDF | Da gan | `SmartCaClient.GetOAuthCertificateAsync()` lay subject/serial/expiry. |
| `POST /csc/signature/signhash` | SmartCA OAuth/Bearer PDF | Da gan | `SmartCaClient.StartOAuthHashSignatureAsync()` gui hash ho so. |
| `POST /csc/credentials/gettraninfo` | SmartCA OAuth/Bearer PDF | Da gan | `SmartCaClient.GetOAuthSignatureStatusAsync()` poll trang thai. |
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

Neu dung credential OAuth `*.apps.smartcaapi.com` hien co:

```env
SMARTCA_ENABLED=true
SMARTCA_CREDENTIAL_MODE=OAuth
SMARTCA_BASE_URL=https://rmgateway.vnptit.vn
SMARTCA_SP_ID=<client-id>
SMARTCA_SP_PASSWORD=<secret>
SMARTCA_MOBILE_CODE=<mobile-code>
SMARTCA_SIGNER_USERNAME=admin
SMARTCA_DEFAULT_USER_ID=<CCCD/uid nguoi ky SmartCA>
SMARTCA_OAUTH_REFRESH_TOKEN=<token>
# hoac neu VNPT cho phep password grant:
SMARTCA_OAUTH_USERNAME=<Personal ID/uid nguoi ky>
SMARTCA_OAUTH_PASSWORD=<mat khau SmartCA nguoi ky>
SMARTCA_OAUTH_CREDENTIAL_ID=<optional, de trong de app goi /csc/credentials/list>
```

Khong commit `.env` that. Chi commit `.env.example`.

## Muc tieu

File nay gom API CA/VNPT can biet de gan vao QLCM Pro. Tach 2 luong:

- SmartCA remote signing: ky hash ho so truc tiep voi CA. Luong nay da gan vao he thong.
- VNPT eContract: tao/gui/ky hop dong dien tu. Luong nay chua gan, chi nen them khi nghiep vu can hop dong nhieu ben, OTP, SmartCA app/HSM.

Nguon chinh thuc:

- SmartCA WebAPI: https://doitac-smartca.vnpt.vn/help/docs/tich-hop/ky-so/webapi/tai-lieu-tich-hop-chi-tiet/
- SmartCA OAuth/Bearer PDF: https://doitac-smartca.vnpt.vn/help/document/TichHopKySoSmartCA.pdf
- SmartCA SP direct `/sca/sp769` PDF: https://doitac-smartca.vnpt.vn/help/document/Tai_lieu_tich_hop_smartca_v4.1.pdf
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
| Webhook callback SmartCA | CA goi ve server khi ky xong. | Da gan endpoint callback. | `POST /api/signatures/smartca/callback` trong `SmartCaSignatureEndpoints.cs`; verify `SMARTCA_CALLBACK_SECRET`, lay reference roi poll VNPT server-side truoc khi finalize `med.signature_transactions`/`med.signature_records`. |
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
3. Chay `.\scripts\test-smartca-vnpt-credential.ps1`; neu VNPT tra `status_code=200` va co certificate thi credential/subscriber dung.
4. Test sandbox: gui ky, mo app SmartCA xac nhan, poll status, kiem tra `signature_records`.
5. Neu VNPT yeu cau callback: cau hinh public URL `https://domain/api/signatures/smartca/callback` va header `X-QLCM-SMARTCA-CALLBACK-SECRET`.
6. Neu nghiep vu can hop dong nhieu ben: thiet ke module eContract rieng, khong tron vao `SmartCaClient`.

## Cau hoi chua giai quyet

- Credential SP sandbox VNPT cua team la gi?
- Credential doc tu anh/Gmail can copy exact `ClientSecret` dang text; cac gia tri doc duoc tu anh hien tra `401 sp_id or sp_password invalid`.
- Tenant cua minh dung `tran_code` hay `transaction_id` trong path `v1/signatures/sign/{tranId}/status`? Code dang dung `tran_code` vi response VNPT tra ma nay.
- QLCM can ky ho so lam sang truc tiep hay can them hop dong dien tu nhieu ben qua eContract?
- Co public callback URL de VNPT cau hinh webhook khong?
