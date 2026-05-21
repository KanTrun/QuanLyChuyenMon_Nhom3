# Phase 02 - Security Validation Guard

## Overview
- Priority: High
- Status: Complete
- Goal: Enforce strong passwords, move validation out of Razor and guard all sensitive pages.

## Requirements
- Configure Identity password options.
- Add `PasswordStrengthValidator` with common-password and username checks.
- Block active users with null passwords and lock admin-created accounts until password setup.
- Add FluentValidation and service wrapper for forms/commands.
- Add admin/persona `_Imports.razor` authorization attributes and `/AccessDenied` page.

## Related Code Files
- `Components/Pages/Login.razor`
- `Components/Pages/Register.razor`
- `Components/Pages/Admin/ProfilePage.razor`
- `Components/Pages/Admin/ToChuc/UsersPage.razor`
- `Components/Layout/AdminLayout.razor`
- `Components/Layout/PersonaLayout.razor`
- `Application/Validators/*`

## Implementation Steps
1. Done - replace register/profile password checks with validators and shared strength evaluator.
2. Done - add common-password resource file and copy it to output.
3. Done - block null-password sign-in and lock admin-created no-password accounts as inactive.
4. Done - add `AuthorizedComponentBase` for policy checks.
5. Done - add folder-level `_Imports.razor` guard for admin routes and current-user auth state provider.
6. Done - add tests for weak/common/username/current-password rejection and Identity validator reuse.

## Success Criteria
- No active null-password account can log in.
- Register/profile password flows show realtime strength and server validation.
- Admin folder is guarded by `AdminAccess` policy.
- `dotnet test .\telemedicine-landing-page.sln -c Release` passes 116/116.

## Unresolved Questions
- Email/reset-token delivery target not configured.
