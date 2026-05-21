# Phase 03 - Business Workflow Jobs

## Overview
- Priority: High
- Status: Complete
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
1. Done - define workflow abstractions and transition definitions.
2. Done - apply workflow guard to procedure version and technical order lifecycle methods.
3. Done - move technical order status mutation from Razor into `TechnicalOrderWorkflowService`.
4. Done - add Hangfire packages/config/dashboard and protected dashboard authorization.
5. Done - no current hosted task found; registered `IJobService` wrapper for durable jobs.
6. Done - add transition tests for procedure versions and technical orders.

## Success Criteria
- Invalid state transitions are blocked server-side and audited.
- `OrderPage` calls service for order status mutations.
- Hangfire dashboard requires authenticated admin permission claims.
- `dotnet test .\telemedicine-landing-page.sln -c Release` passes 121/121.

## Unresolved Questions
- No current `BackgroundService` found in initial scan; Hangfire may be introduced as infrastructure until scheduled jobs are added.
