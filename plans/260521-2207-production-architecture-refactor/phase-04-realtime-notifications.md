# Phase 04 - Realtime Notifications

## Overview
- Priority: Medium
- Status: Planned
- Goal: Add realtime notifications and collaboration presence.

## Requirements
- Add `NotificationHub`.
- Implement `INotificationService` methods for user/group/broadcast.
- Persist notifications in existing `med.notifications`.
- Add notification bell SignalR client updates and toast on new notification.
- Add presence hooks for record editing where practical.

## Related Code Files
- `Services/Admin/NotificationService.cs`
- `Services/Admin/INotificationService.cs`
- `Services/Admin/Sql/MedDataStore.Notifications.cs`
- `Components/Admin/AdminTopBar.razor`
- `Components/Pages/Notification/NotificationPage.razor`
- `Hubs/NotificationHub.cs`

## Implementation Steps
1. Add SignalR hub route `/hubs/notification`.
2. Extend notification service to persist then fan out events.
3. Update top-bar notification UI to subscribe and update badge/dropdown.
4. Add optional presence API for editing high-conflict records.
5. Add tests for persistence and hub service targeting logic.

## Success Criteria
- New notifications appear without refresh.
- Notification history still reads from DB.
- Multi-instance backplane remains a documented deployment option.

## Unresolved Questions
- Redis/SQL backplane choice for multi-instance production not specified.
