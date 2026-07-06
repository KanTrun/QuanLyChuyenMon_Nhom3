# Phase 01 - Security, Auth, Realtime

## Overview
Priority: Critical
Status: Complete

Replace forgeable browser identity restoration and arbitrary SignalR group joins. Add same-process cross-circuit refresh.

## Related Files
- `Services/Admin/Sql/BrowserSessionService.cs`
- `Services/Admin/Sql/CurrentUserContext.cs`
- `Hubs/NotificationHub.cs`
- `Components/Admin/AdminTopBar.razor`
- `Components/Layout/AdminLayout.razor`
- `Components/Layout/PersonaLayout.razor`
- `Services/Admin/Sql/MedDbDataStore.cs`
- `Infrastructure/QlcmServiceCollectionExtensions.cs`

## Implementation
1. Issue and validate expiring Data Protection session tokens; remove legacy `qlcm_uid`.
2. Make hub membership token-derived; do not trust arbitrary user ids or group names.
3. Add singleton in-process data-change bus and scoped bridge component.
4. Publish after SQL mutations and invalidate cached permissions by revision.
5. Subscribe profile page to restored user changes.

## Success Criteria
- Tampered session token cannot restore a user.
- Hub cannot join another user's group using a supplied GUID.
- F5 reload restores valid session without AccessDenied flash.
- Changes in one circuit trigger refresh in another circuit on one web instance.

## Risk
This is not distributed realtime. Multiple web replicas require a backplane later.
