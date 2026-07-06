# Procedure filter and session restore fix

## Summary
- Fixed procedure list filtering so selected status/department chooses the matching procedure version instead of always using newest version.
- Expanded procedure search to visible row fields: version label/title/summary, department and status.
- Expanded pending session restore auth state to `/qlcm` aliases and persona workspace routes.

## Verification
- `dotnet test .\telemedicine-landing-page.sln -c Release --filter "FullyQualifiedName~ProcedureVersionDisplaySelectorTests|FullyQualifiedName~CurrentUserAuthenticationStateProviderTests"` passed 12/12.
- `dotnet build .\telemedicine-landing-page.sln -c Release` passed 0 warnings, 0 errors.
- `dotnet test .\telemedicine-landing-page.sln -c Release --no-build` passed 189/189.

## Unresolved Questions
- None.
