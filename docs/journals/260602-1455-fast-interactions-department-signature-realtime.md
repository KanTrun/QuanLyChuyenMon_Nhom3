# Fast Interactions, Department Archive Filter, Signature Realtime

## Summary
- Reduced shared admin motion timings to make clicks, modals and drawers respond faster.
- Added Signature Pad canvas in clinical signing modal; blank signatures rejected.
- Added `IMedDataStore.Refresh()`; SQL store clears EF tracker then raises `StateChanged` after external signature mutations.
- Fixed scoped `IDbContextFactory<MedDbContext>` registration so runtime DI validation passes.
- Updated Khoa/Phong archive filter label and counts.

## Verification
- `dotnet build .\telemedicine-landing-page.sln -c Release` passed, 0 warnings, 0 errors.
- `dotnet test .\telemedicine-landing-page.sln -c Release --no-build --filter "FullyQualifiedName~SignatureServiceTests|FullyQualifiedName~MedDataStoreTests"` passed, 27/27.
- `dotnet test .\telemedicine-landing-page.sln -c Release --no-build` passed, 177/177.

## Unresolved Questions
- None.
