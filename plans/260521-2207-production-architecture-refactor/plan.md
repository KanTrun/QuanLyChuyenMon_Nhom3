# Production Architecture Refactor

## Context
- Source request: 10-item production architecture hardening prompt for ASP.NET Core + Blazor.
- Branch: `feat/qlcm-pro-admin-shell-v2`
- Current app: Blazor Web App `net9.0`, SQL-backed QLCM Pro, manual per-circuit auth/RBAC, EF Core SQL Server.

## Scope Challenge
- This is a production-scale refactor, not one small patch.
- Identity must be introduced without deleting dynamic RBAC tables because current permissions, nav and admin guards depend on `med.users`, roles, groups and permissions.
- Swagger/OpenAPI is out of direct scope unless API endpoints are added; current surface is Razor Components plus minimal runtime endpoints.

## Phases
| Phase | Status | Goal |
|---|---|---|
| 01 | Complete | Production foundation: Identity-compatible auth bridge, custom permission claims, DB resiliency/health, Serilog |
| 02 | Complete | Security and validation: strong passwords, null password block, FluentValidation, admin route guards |
| 03 | Planned | Architecture/business guards: workflow state machine, service extraction, Hangfire wrapper |
| 04 | Planned | Realtime: SignalR notification hub, persisted realtime bell, collaboration presence hooks |

## Phase Files
- [Phase 01 - Foundation](phase-01-foundation.md)
- [Phase 02 - Security Validation Guard](phase-02-security-validation-guard.md)
- [Phase 03 - Business Workflow Jobs](phase-03-business-workflow-jobs.md)
- [Phase 04 - Realtime Notifications](phase-04-realtime-notifications.md)

## Current Constraints
- No existing EF migrations folder; schema is primarily managed through `MedicalProcedureManagement.sql`.
- Existing tests use `MedDbContext` in-memory; Identity changes must preserve test ergonomics.
- Current Blazor login is component-based; full cookie sign-in may require endpoint or SSR form path because response headers cannot always be mutated inside an interactive circuit.

## Definition of Done
- Build passes with 0 new warnings.
- Tests pass and new critical logic has unit/integration coverage. Phase 02: 116 tests passed.
- Docs updated: changelog, roadmap, architecture. Phase 02 complete.
- Phase commits are focused and conventional. Phase 01 committed as `a2d78e9`; Phase 02 committed as `a5eaf6d`.
- Branch pushed after verified phase completion. Phase 01 and Phase 02 pushed to `feat/qlcm-pro-admin-shell-v2`.

## Unresolved Questions
- Target merge branch says `develop` in prompt, but current working branch is `feat/qlcm-pro-admin-shell-v2`; merge to `develop` needs explicit instruction.
- Production email provider for password setup/reset links is not configured.
- Staging environment URL/credentials are not available in repo.
