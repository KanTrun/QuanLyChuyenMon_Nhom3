namespace TelemedicineLandingPage.Models.Admin;

/// <summary>Whether a permission change targets a single user or a role.</summary>
public enum PermissionTargetType
{
    User,
    Role,
}

/// <summary>Permission grant for a single module (e.g. quy-trinh, danh-muc, lam-sang).</summary>
public sealed record PermissionGrant(
    string Module,
    bool CanView,
    bool CanCreate,
    bool CanUpdate,
    bool CanDelete,
    bool CanApprove);

/// <summary>A role record bundling its permission matrix.</summary>
public sealed record RoleRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = string.Empty;
    public Department Department { get; init; }
    public IReadOnlyList<PermissionGrant> Permissions { get; init; } = Array.Empty<PermissionGrant>();
    public int MemberCount { get; init; }
    public DateTime UpdatedAt { get; init; } = DateTime.Now;
}

/// <summary>A user account assigned to one or more roles.</summary>
public sealed record UserAccountRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public Department Department { get; init; }
    public IReadOnlyList<Guid> RoleIds { get; init; } = Array.Empty<Guid>();
    public bool IsActive { get; init; } = true;
    public DateTime? LastLogin { get; init; }
}

/// <summary>One audit-log entry recorded whenever a permission grant or assignment changes.</summary>
public sealed record PermissionChangeLog(
    Guid Id,
    PermissionTargetType TargetType,
    Guid TargetId,
    string TargetLabel,
    string BeforeJson,
    string AfterJson,
    string Reason,
    string ChangedBy,
    DateTime EffectiveAt,
    DateTime AppliedAt);
