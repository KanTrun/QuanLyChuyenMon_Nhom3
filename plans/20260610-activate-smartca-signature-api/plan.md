# Activate SmartCA Signature API Plan

Ngay: 2026-06-10

## Muc tieu

Hoan thien chu ky SmartCA sandbox de dung duoc qua Docker, khong chi qua UI Blazor.

## Pham vi

- Them HTTP API server-side cho start/poll/readiness/latest transaction.
- Them callback endpoint cho VNPT SmartCA cap nhat giao dich bang transaction id/code.
- Giu secret SmartCA trong server/Docker env, khong day ra browser.
- Cap nhat docs Docker/API.
- Build/test/docker verify, commit va push.

## Viec lam

1. [x] Audit code hien tai: SmartCA client, service, UI, Docker, tests.
2. [x] Them method service callback/finalize theo external transaction id/code.
3. [x] Them minimal API group `/api/signatures/smartca`.
4. [x] Them model request/response nho, khong tron UI Razor.
5. [x] Them tests cho callback/API behavior co the test duoc.
6. [x] Cap nhat README/deployment/API map.
7. [x] Chay `dotnet build`, `dotnet test`, `docker compose config`, rebuild Docker web.

## Ket qua 2026-06-10

- Build Release: pass, 0 warnings, 0 errors.
- Full test: pass, 228/228.
- Docker compose config: pass.
- Docker web rebuilt: `quanlychuyenmon_nhom3-web:latest`.
- Docker web health: healthy.
- Callback missing secret: HTTP 403.
- Added and ran `scripts/smoke-smartca-api.ps1`: pass; health HTTP 200, anonymous SmartCA readiness HTTP 302, callback-missing-secret HTTP 403.
- Added `scripts/configure-smartca-env.ps1` so operators can fill local `.env` SmartCA sandbox values without committing VNPT secrets.
- Added `scripts/test-smartca-vnpt-credential.ps1` so operators can verify VNPT `get_certificate` with local `.env` values without printing secrets.
- Updated chatbot/signature guidance so operators see VNPT SmartCA CA signing as the primary path and demo signing only as the fallback while credential is pending.
- Aligned revoke/config/PDR/architecture wording so SmartCA CA signing and internal demo fallback are clearly separated.
- Clarified for operators that VNPT web screenshots do not show endpoint paths; SmartCA endpoints are in the downloadable PDF and map to existing QLCM files.
- Local `.env` was configured from the provided Gmail screenshot, Docker loaded SmartCA env successfully, but VNPT `get_certificate` returned `401 sp_id or sp_password invalid`; exact `ClientSecret` text is still needed.

## Ranh gioi

- Khong hardcode VNPT credential.
- Khong commit `.env`.
- eContract la module rieng, khong tron vao SmartCA direct signing trong buoc nay.

## Cau hoi chua giai quyet

- Chua co credential SP sandbox that nen khong the test live voi VNPT.
- Callback secret/header cuoi cung phu thuoc cau hinh VNPT cap.
