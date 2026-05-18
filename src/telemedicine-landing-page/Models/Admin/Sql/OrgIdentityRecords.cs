using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelemedicineLandingPage.Models.Admin.Sql;

/// <summary>Khoa/phòng trong tổ chức.</summary>
[Table("departments", Schema = "med")]
public sealed record Department
{
    [Key]
    [Column("department_id")]
    public Guid DepartmentId { get; init; } = Guid.NewGuid();

    [Column("parent_department_id")]
    public Guid? ParentDepartmentId { get; init; }

    [Column("code")]
    public required string Code { get; init; }

    [Column("name")]
    public required string Name { get; init; }

    [Column("status")]
    public string Status { get; init; } = "active";

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Cạnh trong cây đóng (closure table) của khoa/phòng.</summary>
[Table("department_closure", Schema = "med")]
public sealed record DepartmentClosureEdge
{
    [Column("ancestor_department_id")]
    public required Guid AncestorDepartmentId { get; init; }

    [Column("descendant_department_id")]
    public required Guid DescendantDepartmentId { get; init; }

    [Column("depth")]
    public required int Depth { get; init; }
}

/// <summary>Người dùng hệ thống.</summary>
[Table("users", Schema = "med")]
public sealed record AppUser
{
    [Key]
    [Column("user_id")]
    public Guid UserId { get; init; } = Guid.NewGuid();

    [Column("external_auth_id")]
    public string? ExternalAuthId { get; init; }

    [Column("username")]
    public required string Username { get; init; }

    [Column("email")]
    public string? Email { get; init; }

    [Column("full_name")]
    public required string FullName { get; init; }

    [Column("primary_department_id")]
    public Guid? PrimaryDepartmentId { get; init; }

    [Column("status")]
    public string Status { get; init; } = "active";

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; init; }
}

/// <summary>Vai trò trong hệ thống.</summary>
[Table("roles", Schema = "med")]
public sealed record Role
{
    [Key]
    [Column("role_id")]
    public Guid RoleId { get; init; } = Guid.NewGuid();

    [Column("code")]
    public required string Code { get; init; }

    [Column("name")]
    public required string Name { get; init; }

    [Column("description")]
    public string? Description { get; init; }

    [Column("is_system")]
    public bool IsSystem { get; init; }

    [Column("status")]
    public string Status { get; init; } = "active";

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Nhóm người dùng.</summary>
[Table("groups", Schema = "med")]
public sealed record Group
{
    [Key]
    [Column("group_id")]
    public Guid GroupId { get; init; } = Guid.NewGuid();

    [Column("code")]
    public required string Code { get; init; }

    [Column("name")]
    public required string Name { get; init; }

    [Column("department_id")]
    public Guid? DepartmentId { get; init; }

    [Column("description")]
    public string? Description { get; init; }

    [Column("status")]
    public string Status { get; init; } = "active";

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Gán vai trò cho người dùng (có thời hạn).</summary>
[Table("user_roles", Schema = "med")]
public sealed record UserRole
{
    [Key]
    [Column("user_role_id")]
    public Guid UserRoleId { get; init; } = Guid.NewGuid();

    [Column("user_id")]
    public required Guid UserId { get; init; }

    [Column("role_id")]
    public required Guid RoleId { get; init; }

    [Column("department_id")]
    public Guid? DepartmentId { get; init; }

    [Column("effective_from")]
    public DateTime EffectiveFrom { get; init; } = DateTime.UtcNow;

    [Column("effective_to")]
    public DateTime? EffectiveTo { get; init; }

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Thành viên nhóm người dùng (có thời hạn).</summary>
[Table("user_group_members", Schema = "med")]
public sealed record UserGroupMember
{
    [Key]
    [Column("user_group_member_id")]
    public Guid UserGroupMemberId { get; init; } = Guid.NewGuid();

    [Column("user_id")]
    public required Guid UserId { get; init; }

    [Column("group_id")]
    public required Guid GroupId { get; init; }

    [Column("effective_from")]
    public DateTime EffectiveFrom { get; init; } = DateTime.UtcNow;

    [Column("effective_to")]
    public DateTime? EffectiveTo { get; init; }

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
