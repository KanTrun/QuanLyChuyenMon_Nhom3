# Phase 04 - Realtime Notifications

## Overview
- Priority: Medium
- Status: Complete
- Goal: Add realtime notifications and collaboration presence.

## Requirements
- Added `NotificationHub` at `/hubs/notification`.
- Implemented `INotificationService` methods for user/group/broadcast.
- Persisted notifications in existing `med.notifications`.
- Added notification bell SignalR client updates and toast on new notification.
- Added presence hooks for record editing.

## Related Code Files
- `Program.cs`
- `Infrastructure/QlcmServiceCollectionExtensions.cs`
- `Services/Notifications/INotificationService.cs`
- `Services/Notifications/SignalRNotificationService.cs`
- `Services/Notifications/SignalRNotificationRealtimePublisher.cs`
- `Services/Admin/Sql/MedDataStore.Notifications.cs`
- `Components/Admin/AdminTopBar.razor`
- `Components/Pages/Register.razor`
- `Hubs/NotificationHub.cs`
- `tests/telemedicine-landing-page.tests/Admin/Sql/NotificationRealtimeServiceTests.cs`

## Implementation Steps
1. Complete - Add SignalR hub route `/hubs/notification`.
2. Complete - Extend notification service to persist then fan out events.
3. Complete - Update top-bar notification UI to subscribe and update badge/dropdown.
4. Complete - Add optional presence API for editing high-conflict records.
5. Complete - Add tests for persistence and hub service targeting logic.

## Success Criteria
- New notifications appear without refresh.
- Notification history still reads from DB.
- Multi-instance backplane remains a documented deployment option.

## Verification
- `dotnet build .\telemedicine-landing-page.sln -c Release` passed with 0 warnings.
- `dotnet test .\telemedicine-landing-page.sln -c Release` passed, 124 tests.

## Unresolved Questions
- Redis/SQL backplane choice for multi-instance production not specified.
