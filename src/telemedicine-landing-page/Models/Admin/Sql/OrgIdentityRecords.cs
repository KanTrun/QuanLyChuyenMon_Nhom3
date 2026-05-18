namespace TelemedicineLandingPage.Models.Admin.Sql;

/// <summary>Khoa/phòng trong tổ chức.</summary>
public sealed record Department
{
    public Guid DepartmentId { get; init; } = Guid.NewGuid();
    public Guid? ParentDepartmentId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string Status { get; init; } = "active";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Cạnh trong cây đóng (closure table) của khoa/phòng.</summary>
public sealed record DepartmentClosureEdge
{
    public required Guid AncestorDepartmentId { get; init; }
    public required Guid DescendantDepartmentId { get; init; }
    public required int Depth { get; init; }
}

/// <summary>Người dùng hệ thống.</summary>
public sealed record AppUser
{
    public Guid UserId { get; init; } = Guid.NewGuid();
    public string? ExternalAuthId { get; init; }
    public required string Username { get; init; }
    public string? Email { get; init; }
    public required string FullName { get; init; }
    public Guid? PrimaryDepartmentId { get; init; }
    public string Status { get; init; } = "active";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; init; }
}

/// <summary>Vai trò trong hệ thống.</summary>
public sealed record Role
{
    public Guid RoleId { get; init; } = Guid.NewGuid();
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
    public string Status { get; init; } = "active";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Nhóm người dùng.</summary>
public sealed record Group
{
    public Guid GroupId { get; init; } = Guid.NewGuid();
    public required string Code { get; init; }
    public required string Name { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? Description { get; init; }
    public string Status { get; init; } = "active";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Gán vai trò cho người dùng (có thời hạn).</summary>
public sealed record UserRole
{
    public Guid UserRoleId { get; init; } = Guid.NewGuid();
    public required Guid UserId { get; init; }
    public required Guid RoleId { get; init; }
    public Guid? DepartmentId { get; init; }
    public DateTime EffectiveFrom { get; init; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Thành viên nhóm người dùng (có thời hạn).</summary>
public sealed record UserGroupMember
{
    public Guid UserGroupMemberId { get; init; } = Guid.NewGuid();
    public required Guid UserId { get; init; }
    public required Guid GroupId { get; init; }
    public DateTime EffectiveFrom { get; init; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
