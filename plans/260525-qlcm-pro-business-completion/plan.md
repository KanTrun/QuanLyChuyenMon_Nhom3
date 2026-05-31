# QLCM Pro Business Completion Plan

## Status
Complete

## Scope
- Remove obsolete public telemedicine landing surface from the current `feat/qlcm-pro-admin-shell-v2` flow.
- Strengthen business logic against the six requirement groups: procedure runtime control, RBAC changes, scheduled permission application, resource availability checks, protocol suggestions, and audit/notification.
- Keep changes minimal, testable, and compatible with existing SQL-backed Blazor architecture.

## Implementation Steps
1. Replace `/` landing experience with QLCM Pro login redirect and remove unused landing services/components/config/tests.
2. Add scheduled permission application service and register Hangfire recurring job.
3. Add workflow runtime guard service for procedure screen/action mappings and audit deviations.
4. Add inventory availability service that computes snapshots from internal resource catalog until external systems exist.
5. Add clinical protocol suggestion service matching active protocol rules by ICD/patient context and rationale.
6. Wire services into UI flows where current pages still did manual or placeholder behavior.
7. Add focused tests for new business behavior and update docs/changelog.
8. Run Release build/test, dependency vulnerability scan, commit, push, and rebuild Docker Compose web stack.

## Verification
| Command | Result |
|---|---|
| `dotnet build .\telemedicine-landing-page.sln -c Release` | Passed, 0 warnings, 0 errors |
| `dotnet test .\telemedicine-landing-page.sln -c Release` | Passed, 142 tests |
| `dotnet list .\telemedicine-landing-page.sln package --vulnerable --include-transitive` | No vulnerable packages |

## Unresolved Questions
- External inventory/pharmacy/equipment APIs are not available, so resource checks use an internal deterministic adapter with explicit `internal_catalog` source.
