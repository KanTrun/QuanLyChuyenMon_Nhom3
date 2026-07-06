# Fix logo and clinical signature

## Status
- Complete

## Scope
- Remove white logo frame/padding so the hospital logo fills its mark.
- Fix clinical signature permission mismatch between UI guard and service.
- Fix clinical signature DbContext concurrency crash during Blazor rerender.
- Add Vietnamese accents for clinical signature labels/messages and related seed text.
- Verify build/tests, update docs if needed, commit/push, then run Docker Compose.

## Steps
1. Patch logo CSS in intro/auth/sidebar contexts.
2. Patch clinical signature UI copy and service messages.
3. Align signature service permission aliases with admin guard.
4. Update signature SQL migration display text.
5. Run targeted tests and build.
6. Commit, push, compose up.

## Unresolved Questions
- None.
