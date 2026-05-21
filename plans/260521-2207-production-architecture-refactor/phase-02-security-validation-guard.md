# Phase 02 - Security Validation Guard

## Overview
- Priority: High
- Status: Planned
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
1. Replace inline password validation with validators and shared strength evaluator.
2. Add common-password resource file.
3. Add lock/reset setup behavior for admin-created accounts.
4. Add `AuthorizedComponentBase` for guarded components.
5. Add folder-level `_Imports.razor` guards where Blazor route auth can enforce policy.
6. Add tests for password weakness, username inclusion, null password, unauthorized route denial.

## Success Criteria
- No active null-password account can log in.
- Register/profile password flows show realtime strength and server validation.
- Admin/persona folders are guarded by policy.

## Unresolved Questions
- Email/reset-token delivery target not configured.
