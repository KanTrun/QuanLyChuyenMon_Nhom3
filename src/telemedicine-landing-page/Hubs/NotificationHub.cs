using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Services.Auth;

namespace TelemedicineLandingPage.Hubs;

public sealed class NotificationHub : Hub
{
    private readonly BrowserSessionTokenService _tokens;
    private readonly IDbContextFactory<MedDbContext> _dbFactory;

    public NotificationHub(
        BrowserSessionTokenService tokens,
        IDbContextFactory<MedDbContext> dbFactory)
    {
        _tokens = tokens;
        _dbFactory = dbFactory;
    }

    public async Task JoinUserGroup(string sessionToken)
    {
        var user = await ResolveActiveUserAsync(sessionToken);
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(user.UserId));
    }

    public async Task JoinDataSyncGroup(string sessionToken)
    {
        await ResolveActiveUserAsync(sessionToken);
        await Groups.AddToGroupAsync(Context.ConnectionId, DataSyncGroup);
    }

    public async Task LeaveUserGroup(string sessionToken)
    {
        var user = await ResolveActiveUserAsync(sessionToken);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroup(user.UserId));
    }

    public async Task JoinPresence(string sessionToken, string recordType, string recordId)
    {
        var user = await ResolveActiveUserAsync(sessionToken);
        var group = PresenceGroup(recordType, recordId);
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        await Clients.OthersInGroup(group).SendAsync("PresenceChanged", new
        {
            recordType = NormalizePresenceSegment(recordType),
            recordId = NormalizePresenceSegment(recordId),
            displayName = user.DisplayName,
            isEditing = true
        });
    }

    public async Task LeavePresence(string sessionToken, string recordType, string recordId)
    {
        var user = await ResolveActiveUserAsync(sessionToken);
        var group = PresenceGroup(recordType, recordId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        await Clients.OthersInGroup(group).SendAsync("PresenceChanged", new
        {
            recordType = NormalizePresenceSegment(recordType),
            recordId = NormalizePresenceSegment(recordId),
            displayName = user.DisplayName,
            isEditing = false
        });
    }

    public static string UserGroup(Guid userId) => UserGroup(userId.ToString("D"));

    public static string UserGroup(string userId) => $"user:{userId.Trim().ToLowerInvariant()}";

    public static string DataSyncGroup => "sync:all";

    public static string Group(string groupName) => $"group:{groupName.Trim().ToLowerInvariant()}";

    private static string PresenceGroup(string recordType, string recordId)
        => $"presence:{NormalizePresenceSegment(recordType)}:{NormalizePresenceSegment(recordId)}";

    private static string NormalizePresenceSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new HubException("Presence target is required.");
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > 120)
        {
            throw new HubException("Presence target is too long.");
        }

        if (normalized.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.')))
        {
            throw new HubException("Presence target is invalid.");
        }

        return normalized;
    }

    private async Task<(Guid UserId, string DisplayName)> ResolveActiveUserAsync(string sessionToken)
    {
        if (!_tokens.TryValidateToken(sessionToken, out BrowserSessionTokenService.BrowserSessionIdentity identity))
        {
            throw new HubException("Invalid or expired browser session.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(Context.ConnectionAborted);
        var user = await db.Users
            .Where(user => user.UserId == identity.UserId &&
                           user.Status == "active" &&
                           user.OnboardingStatus == "active" &&
                           user.DeletedAt == null &&
                           user.ActiveSessionId == identity.SessionId)
            .Select(user => new { user.UserId, user.FullName, user.Username, user.ActiveSessionId })
            .FirstOrDefaultAsync(Context.ConnectionAborted);
        if (user is null)
        {
            throw new HubException("Invalid or expired browser session.");
        }

        return (user.UserId, string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName);
    }
}
