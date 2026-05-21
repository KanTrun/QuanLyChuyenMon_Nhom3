# QLCM Pro Audit Remediation

## Context
- Source: `D:\Downloads\QLCM-Pro-Audit-Report.docx`
- Branch: `feat/qlcm-pro-admin-shell-v2`
- Scope: fix critical/high audit findings first, then medium/low UX/data issues that are safe inside current Blazor + SQL-backed architecture.

## Phases
| Phase | Status | Goal |
|---|---|---|
| 01 | Pending | Persist login across refresh and add action-level permission guard for dangerous mutations |
| 02 | Pending | Repair approval/business workflows: procedure/protocol publish, reject reason, archive confirmation, audit and reset password |
| 03 | Pending | Improve UX/data correctness: toast variants, filters, dashboard/report counts, audit target names, friendly rule JSON, pagination, social login |
| 04 | Pending | Update docs, build/test, commit by phase and push branch |

## Related Files
- `src/telemedicine-landing-page/Services/Admin/Sql/CurrentUserContext.cs`
- `src/telemedicine-landing-page/Components/Layout/AdminLayout.razor`
- `src/telemedicine-landing-page/Components/Pages/Login.razor`
- `src/telemedicine-landing-page/Components/Pages/Register.razor`
- `src/telemedicine-landing-page/Components/Pages/Admin/*.razor`
- `src/telemedicine-landing-page/Components/Pages/{Procedure,Order,Clinical,Resource,PermissionApprover}/*.razor`
- `src/telemedicine-landing-page/Components/Admin/PhanQuyen/RoleMatrix.razor`
- `docs/development-roadmap.md`
- `docs/project-changelog.md`

## Success Criteria
- Critical audit items C1-C4 have concrete code changes.
- Dangerous CRUD/publish/archive/status changes fail closed when user lacks permission.
- Protocol create starts as draft and needs submit/approve/publish.
- Refresh after login restores current user from browser session storage.
- Build and tests run fresh before final push.
- Each phase has a focused conventional commit and branch is pushed.

## Risks
- Existing SQL seed uses `SCR_*:ACTION`; older tests/in-memory seed use legacy `PERM_*` codes. Guard must support aliases.
- Full production auth should use HttpOnly cookies; this remediation uses `sessionStorage` because audit explicitly requested it and app is Blazor Server scoped per circuit.

## Unresolved Questions
- Production auth target cookie/token scheme not specified.
