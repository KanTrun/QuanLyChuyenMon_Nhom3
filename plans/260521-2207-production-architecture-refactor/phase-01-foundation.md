# Phase 01 - Foundation

## Overview
- Priority: Critical
- Status: Complete
- Goal: Add production-grade foundations that do not break the existing SQL-backed QLCM workflows.

## Requirements
- Add ASP.NET Core Identity packages and identity entities.
- Preserve dynamic RBAC from existing DB and expose it as custom permission claims.
- Add `IPermissionService`, `PermissionRequirement`, `PermissionAuthorizationHandler`.
- Configure EF Core SQL Server retry, command timeout and health checks.
- Add Serilog structured logging and request logging.

## Related Code Files
- `src/telemedicine-landing-page/Program.cs`
- `src/telemedicine-landing-page/telemedicine-landing-page.csproj`
- `src/telemedicine-landing-page/Data/MedDbContext.cs`
- `src/telemedicine-landing-page/Services/Admin/Sql/CurrentUserContext.cs`
- `src/telemedicine-landing-page/Services/Admin/Sql/EffectivePermissionResolver.cs`
- `src/telemedicine-landing-page/appsettings*.json`
- `tests/telemedicine-landing-page.tests/Admin/Sql/*.cs`

## Implementation Steps
1. Add required NuGet references compatible with `net9.0`.
2. Create identity models mapped separately from existing `med.users`.
3. Register Identity/cookie auth/cascading auth state without removing current RBAC storage.
4. Create claims transformation that loads current permissions into `permission` claims and caches by user id.
5. Register authorization policies for existing route/action permission codes.
6. Configure SQL retry and health endpoint `/health` with JSON output.
7. Configure Serilog console/file structured logging and request logging.
8. Add focused tests for permission claims and null-password denial foundation.

## Success Criteria
- Existing manual RBAC remains functional.
- Authorization services can check dynamic permissions through claims.
- `/health` reports app and DB health.
- Build/test pass.
- Release build passed with 0 warnings and 0 errors.
- Test suite passed with 112 tests.

## Security Considerations
- Do not hardcode secrets.
- Do not allow null/empty password login path to survive.
- Do not treat Identity static roles as RBAC source of truth.

## Unresolved Questions
- Whether production wants identity tables in `dbo` default or a dedicated `auth` schema.
