using Microsoft.AspNetCore.SignalR;

namespace TelemedicineLandingPage.Hubs;

public sealed class NotificationHub : Hub
{
    public Task JoinUserGroup(string userId)
        => string.IsNullOrWhiteSpace(userId)
            ? Task.CompletedTask
            : Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));

    public Task LeaveUserGroup(string userId)
        => string.IsNullOrWhiteSpace(userId)
            ? Task.CompletedTask
            : Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroup(userId));

    public Task JoinGroup(string groupName)
        => string.IsNullOrWhiteSpace(groupName)
            ? Task.CompletedTask
            : Groups.AddToGroupAsync(Context.ConnectionId, Group(groupName));

    public async Task JoinPresence(string recordType, string recordId, string displayName)
    {
        var group = PresenceGroup(recordType, recordId);
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        await Clients.OthersInGroup(group).SendAsync("PresenceChanged", new
        {
            recordType,
            recordId,
            displayName,
            isEditing = true
        });
    }

    public async Task LeavePresence(string recordType, string recordId, string displayName)
    {
        var group = PresenceGroup(recordType, recordId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        await Clients.OthersInGroup(group).SendAsync("PresenceChanged", new
        {
            recordType,
            recordId,
            displayName,
            isEditing = false
        });
    }

    public static string UserGroup(Guid userId) => UserGroup(userId.ToString("D"));

    public static string UserGroup(string userId) => $"user:{userId.Trim().ToLowerInvariant()}";

    public static string Group(string groupName) => $"group:{groupName.Trim().ToLowerInvariant()}";

    private static string PresenceGroup(string recordType, string recordId)
        => $"presence:{recordType.Trim().ToLowerInvariant()}:{recordId.Trim().ToLowerInvariant()}";
}
