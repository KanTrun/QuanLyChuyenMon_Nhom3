using System.Text.Json;
using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin;

/// <summary>
/// Default singleton permission service. Keeps an append-only change log so the
/// Phân quyền page can render the Lịch sử thay đổi tab without hitting any
/// external storage.
/// </summary>
public sealed class PermissionService : IPermissionService
{
    private static readonly string[] s_modules =
    {
        "quy-trinh", "phan-quyen", "danh-muc", "phac-do", "bao-cao", "cai-dat", "lam-sang",
    };

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly object _gate = new();
    private readonly List<RoleRecord> _roles;
    private readonly List<UserAccountRecord> _users;
    private readonly List<PermissionChangeLog> _changeLog = new();

    public PermissionService()
    {
        (_roles, _users) = Seed();
        RecomputeMemberCounts();
    }

    public event Action? StateChanged;

    public IReadOnlyList<string> AdminModules => s_modules;

    public IReadOnlyList<RoleRecord> ListRoles()
    {
        lock (_gate) return _roles.OrderBy(r => r.Code).ToList();
    }

    public RoleRecord? GetRole(Guid id)
    {
        lock (_gate) return _roles.FirstOrDefault(r => r.Id == id);
    }

    public RoleRecord CreateRole(RoleRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var next = record with
        {
            Id = record.Id == Guid.Empty ? Guid.NewGuid() : record.Id,
            UpdatedAt = DateTime.Now,
        };
        lock (_gate)
        {
            _roles.Add(next);
        }
        Raise();
        return next;
    }

    public RoleRecord UpdateRole(Guid id, RoleRecord updated)
    {
        ArgumentNullException.ThrowIfNull(updated);
        RoleRecord next;
        lock (_gate)
        {
            var index = _roles.FindIndex(r => r.Id == id);
            if (index < 0) throw new KeyNotFoundException($"Không tìm thấy vai trò {id}.");
            next = updated with { Id = id, UpdatedAt = DateTime.Now };
            _roles[index] = next;
        }
        Raise();
        return next;
    }

    public void DeleteRole(Guid id)
    {
        lock (_gate)
        {
            _roles.RemoveAll(r => r.Id == id);
            for (var i = 0; i < _users.Count; i++)
            {
                if (_users[i].RoleIds.Contains(id))
                {
                    _users[i] = _users[i] with { RoleIds = _users[i].RoleIds.Where(rid => rid != id).ToList() };
                }
            }
            RecomputeMemberCountsLocked();
        }
        Raise();
    }

    public void UpdateRolePermissions(
        Guid roleId,
        IReadOnlyList<PermissionGrant> grants,
        string reason,
        DateTime effectiveAt,
        string changedBy)
    {
        ArgumentNullException.ThrowIfNull(grants);
        PermissionChangeLog? logEntry = null;
        lock (_gate)
        {
            var index = _roles.FindIndex(r => r.Id == roleId);
            if (index < 0) throw new KeyNotFoundException($"Không tìm thấy vai trò {roleId}.");
            var previous = _roles[index];
            var beforeJson = JsonSerializer.Serialize(previous.Permissions, s_jsonOptions);
            var afterJson = JsonSerializer.Serialize(grants, s_jsonOptions);

            _roles[index] = previous with
            {
                Permissions = grants.ToList(),
                UpdatedAt = DateTime.Now,
            };

            logEntry = new PermissionChangeLog(
                Id: Guid.NewGuid(),
                TargetType: PermissionTargetType.Role,
                TargetId: roleId,
                TargetLabel: previous.Name,
                BeforeJson: beforeJson,
                AfterJson: afterJson,
                Reason: string.IsNullOrWhiteSpace(reason) ? "Không có ghi chú" : reason,
                ChangedBy: string.IsNullOrWhiteSpace(changedBy) ? "Hệ thống" : changedBy,
                EffectiveAt: effectiveAt,
                AppliedAt: DateTime.Now);
            _changeLog.Add(logEntry);
        }
        Raise();
    }

    public IReadOnlyList<UserAccountRecord> ListUsers()
    {
        lock (_gate) return _users.OrderBy(u => u.FullName).ToList();
    }

    public void AssignUserRoles(Guid userId, IReadOnlyList<Guid> roleIds, string reason, string changedBy)
    {
        ArgumentNullException.ThrowIfNull(roleIds);
        lock (_gate)
        {
            var index = _users.FindIndex(u => u.Id == userId);
            if (index < 0) throw new KeyNotFoundException($"Không tìm thấy tài khoản {userId}.");
            var previous = _users[index];
            var beforeJson = JsonSerializer.Serialize(previous.RoleIds.Select(id => RoleNameLocked(id)).ToList(), s_jsonOptions);
            var afterJson = JsonSerializer.Serialize(roleIds.Select(id => RoleNameLocked(id)).ToList(), s_jsonOptions);

            _users[index] = previous with { RoleIds = roleIds.ToList() };

            _changeLog.Add(new PermissionChangeLog(
                Id: Guid.NewGuid(),
                TargetType: PermissionTargetType.User,
                TargetId: userId,
                TargetLabel: previous.FullName,
                BeforeJson: beforeJson,
                AfterJson: afterJson,
                Reason: string.IsNullOrWhiteSpace(reason) ? "Cập nhật phân quyền tài khoản" : reason,
                ChangedBy: string.IsNullOrWhiteSpace(changedBy) ? "Hệ thống" : changedBy,
                EffectiveAt: DateTime.Now,
                AppliedAt: DateTime.Now));

            RecomputeMemberCountsLocked();
        }
        Raise();
    }

    public IReadOnlyList<PermissionChangeLog> GetChangeLog(Guid? targetId = null)
    {
        lock (_gate)
        {
            IEnumerable<PermissionChangeLog> query = _changeLog;
            if (targetId is { } id)
            {
                query = query.Where(l => l.TargetId == id);
            }
            return query.OrderByDescending(l => l.AppliedAt).ToList();
        }
    }

    private void Raise() => StateChanged?.Invoke();

    private string RoleNameLocked(Guid roleId) =>
        _roles.FirstOrDefault(r => r.Id == roleId)?.Name ?? roleId.ToString();

    private void RecomputeMemberCounts()
    {
        lock (_gate)
        {
            RecomputeMemberCountsLocked();
        }
    }

    private void RecomputeMemberCountsLocked()
    {
        var counts = _users
            .SelectMany(u => u.RoleIds)
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());
        for (var i = 0; i < _roles.Count; i++)
        {
            counts.TryGetValue(_roles[i].Id, out var count);
            _roles[i] = _roles[i] with { MemberCount = count };
        }
    }

    private static (List<RoleRecord> Roles, List<UserAccountRecord> Users) Seed()
    {
        List<PermissionGrant> Grants(bool approve = false) => s_modules
            .Select(module => new PermissionGrant(
                module,
                CanView: true,
                CanCreate: module is not "bao-cao",
                CanUpdate: module is not "bao-cao",
                CanDelete: false,
                CanApprove: approve && (module is "quy-trinh" or "phan-quyen")))
            .ToList();

        var adminRole = new RoleRecord
        {
            Code = "ADMIN",
            Name = "Quản trị hệ thống",
            Description = "Toàn quyền cấu hình hệ thống",
            Department = Department.HanhChinh,
            Permissions = s_modules
                .Select(module => new PermissionGrant(module, true, true, true, true, true))
                .ToList(),
        };
        var doctorRole = new RoleRecord
        {
            Code = "BSDT",
            Name = "Bác sĩ điều trị",
            Description = "Thực hiện quy trình, chỉ định và theo dõi lâm sàng",
            Department = Department.NoiTiet,
            Permissions = Grants(),
        };
        var managerRole = new RoleRecord
        {
            Code = "TK",
            Name = "Trưởng khoa",
            Description = "Duyệt và quản lý dữ liệu trong khoa",
            Department = Department.NoiTiet,
            Permissions = Grants(approve: true),
        };

        var roles = new List<RoleRecord> { adminRole, doctorRole, managerRole };
        var users = new List<UserAccountRecord>
        {
            new()
            {
                FullName = "Quản trị hệ thống",
                Email = "admin@benhvien.vn",
                Department = Department.HanhChinh,
                RoleIds = new[] { adminRole.Id },
                LastLogin = DateTime.Now.AddMinutes(-20),
            },
            new()
            {
                FullName = "BS. Đỗ An Nhiên",
                Email = "bs.noi01@benhvien.vn",
                Department = Department.NoiTiet,
                RoleIds = new[] { doctorRole.Id },
                LastLogin = DateTime.Now.AddHours(-2),
            },
            new()
            {
                FullName = "TS. Nguyễn Minh Khang",
                Email = "tk.noi@benhvien.vn",
                Department = Department.NoiTiet,
                RoleIds = new[] { managerRole.Id },
                LastLogin = DateTime.Now.AddHours(-1),
            },
        };

        return (roles, users);
    }
}
