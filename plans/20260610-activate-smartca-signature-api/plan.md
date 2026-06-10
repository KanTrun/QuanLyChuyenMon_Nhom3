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

1. Audit code hien tai: SmartCA client, service, UI, Docker, tests.
2. Them method service callback/finalize theo external transaction id/code.
3. Them minimal API group `/api/signatures/smartca`.
4. Them model request/response nho, khong tron UI Razor.
5. Them tests cho callback/API behavior co the test duoc.
6. Cap nhat README/deployment/API map.
7. Chay `dotnet build`, `dotnet test`, `docker compose config`, rebuild Docker web.

## Ranh gioi

- Khong hardcode VNPT credential.
- Khong commit `.env`.
- eContract la module rieng, khong tron vao SmartCA direct signing trong buoc nay.

## Cau hoi chua giai quyet

- Chua co credential SP sandbox that nen khong the test live voi VNPT.
- Callback secret/header cuoi cung phu thuoc cau hinh VNPT cap.
