# Phase 03 - Business Workflow Jobs

## Overview
- Priority: High
- Status: Planned
- Goal: Move state transitions and orchestration from components into testable services and introduce Hangfire for durable jobs.

## Requirements
- Add workflow guard/state machine for order and version lifecycle transitions.
- Extract business logic from oversized Razor pages into application services.
- Add Hangfire with SQL Server storage, dashboard guard and `IJobService` wrapper.

## Related Code Files
- `Components/Pages/Order/OrderPage.razor`
- `Components/Pages/Admin/QuyTrinh*.razor`
- `Components/Pages/Admin/PhacDoPage.razor`
- `Services/Admin/Sql/ProcedureLifecycleService.cs`
- `Services/Admin/Sql/PermissionChangeRequestService.cs`
- `Application/Workflow/*`
- `Application/Jobs/*`

## Implementation Steps
1. Define workflow abstractions and transition definitions.
2. Apply workflow guard to order and procedure/protocol lifecycle methods first.
3. Extract heavy command handlers from Razor pages where mutations currently happen.
4. Add Hangfire packages/config/dashboard and protected dashboard authorization.
5. Migrate scheduled permission effective-date work to Hangfire if a current hosted task exists; otherwise register wrapper for future scheduled jobs.
6. Add transition and job wrapper tests.

## Success Criteria
- Invalid state transitions are blocked server-side and audited.
- Razor components call services for domain mutations.
- Hangfire dashboard requires admin permission.

## Unresolved Questions
- No current `BackgroundService` found in initial scan; Hangfire may be introduced as infrastructure until scheduled jobs are added.
