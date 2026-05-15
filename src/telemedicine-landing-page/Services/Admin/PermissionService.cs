using System.Text.Json;
using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin;

/// <summary>
/// Default singleton permission service. Seeds 5 roles and 12 users on startup
/// and keeps an append-only change log so the Phân quyền page can render the
/// Lịch sử thay đổi tab without hitting any external storage.
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
        PermissionGrant Allow(string module, bool view = true, bool create = false, bool update = false, bool delete = false, bool approve = false)
            => new(module, view, create, update, delete, approve);

        var admin = new RoleRecord
        {
            Code = "QTHT",
            Name = "Quản trị hệ thống",
            Description = "Toàn quyền quản trị, cấu hình hệ thống và phân quyền.",
            Department = Department.HanhChinh,
            Permissions = s_modules
                .Select(m => new PermissionGrant(m, true, true, true, true, true))
                .ToList(),
        };

        var lead = new RoleRecord
        {
            Code = "LDK",
            Name = "Lãnh đạo khoa",
            Description = "Phê duyệt quy trình và phác đồ chuyên môn của khoa.",
            Department = Department.NoiTiet,
            Permissions = new List<PermissionGrant>
            {
                Allow("quy-trinh", true, true, true, false, true),
                Allow("phac-do", true, true, true, false, true),
                Allow("bao-cao", true, false, false, false, false),
                Allow("danh-muc", true, false, true, false, false),
                Allow("lam-sang", true, false, true, false, false),
                Allow("phan-quyen", true, false, false, false, false),
                Allow("cai-dat", true, false, false, false, false),
            },
        };

        var doctor = new RoleRecord
        {
            Code = "BSDT",
            Name = "Bác sĩ điều trị",
            Description = "Tra cứu và áp dụng quy trình, phác đồ trong khám chữa bệnh.",
            Department = Department.NgoaiTongQuat,
            Permissions = new List<PermissionGrant>
            {
                Allow("quy-trinh", true, true, true, false, false),
                Allow("phac-do", true, true, true, false, false),
                Allow("lam-sang", true, true, true, false, false),
                Allow("danh-muc", true, false, false, false, false),
                Allow("bao-cao", true, false, false, false, false),
                Allow("phan-quyen", false, false, false, false, false),
                Allow("cai-dat", true, false, false, false, false),
            },
        };

        var nurse = new RoleRecord
        {
            Code = "DD",
            Name = "Điều dưỡng",
            Description = "Thực hiện quy trình và ghi nhận chăm sóc theo phác đồ.",
            Department = Department.NhiKhoa,
            Permissions = new List<PermissionGrant>
            {
                Allow("quy-trinh", true, false, false, false, false),
                Allow("phac-do", true, false, false, false, false),
                Allow("lam-sang", true, true, true, false, false),
                Allow("danh-muc", true, false, false, false, false),
                Allow("bao-cao", false, false, false, false, false),
                Allow("phan-quyen", false, false, false, false, false),
                Allow("cai-dat", true, false, false, false, false),
            },
        };

        var pharmacist = new RoleRecord
        {
            Code = "DSLS",
            Name = "Dược sĩ lâm sàng",
            Description = "Quản lý cấp phát thuốc, định mức và báo cáo dược.",
            Department = Department.DuocLamSang,
            Permissions = new List<PermissionGrant>
            {
                Allow("danh-muc", true, true, true, true, false),
                Allow("quy-trinh", true, false, true, false, false),
                Allow("phac-do", true, false, true, false, false),
                Allow("bao-cao", true, true, true, false, false),
                Allow("lam-sang", true, false, false, false, false),
                Allow("phan-quyen", false, false, false, false, false),
                Allow("cai-dat", true, false, false, false, false),
            },
        };

        var roles = new List<RoleRecord> { admin, lead, doctor, nurse, pharmacist };

        var users = new List<UserAccountRecord>
        {
            new() { FullName = "BS. Nguyễn Minh An", Email = "minh.an@qlcm.local", Department = Department.NoiTiet, RoleIds = new[] { lead.Id }, LastLogin = DateTime.Now.AddMinutes(-12) },
            new() { FullName = "ThS. Trần Phương Linh", Email = "phuong.linh@qlcm.local", Department = Department.XetNghiem, RoleIds = new[] { doctor.Id, lead.Id }, LastLogin = DateTime.Now.AddHours(-1) },
            new() { FullName = "BS. Lê Quang Huy", Email = "quang.huy@qlcm.local", Department = Department.TimMach, RoleIds = new[] { doctor.Id }, LastLogin = DateTime.Now.AddHours(-3) },
            new() { FullName = "BS. Phạm Thanh Hải", Email = "thanh.hai@qlcm.local", Department = Department.NgoaiTongQuat, RoleIds = new[] { doctor.Id }, LastLogin = DateTime.Now.AddDays(-1) },
            new() { FullName = "BS. Hoàng Bảo Châu", Email = "bao.chau@qlcm.local", Department = Department.SanPhuKhoa, RoleIds = new[] { lead.Id }, LastLogin = DateTime.Now.AddHours(-5) },
            new() { FullName = "ĐD. Mai Thị Lan", Email = "mai.lan@qlcm.local", Department = Department.NgoaiTongQuat, RoleIds = new[] { nurse.Id }, LastLogin = DateTime.Now.AddMinutes(-25) },
            new() { FullName = "ĐD. Vũ Hồng Hạnh", Email = "hong.hanh@qlcm.local", Department = Department.NhiKhoa, RoleIds = new[] { nurse.Id }, LastLogin = DateTime.Now.AddHours(-2) },
            new() { FullName = "DS. Đỗ Thanh Tùng", Email = "thanh.tung@qlcm.local", Department = Department.DuocLamSang, RoleIds = new[] { pharmacist.Id }, LastLogin = DateTime.Now.AddMinutes(-45) },
            new() { FullName = "DS. Bùi Quỳnh Anh", Email = "quynh.anh@qlcm.local", Department = Department.DuocLamSang, RoleIds = new[] { pharmacist.Id }, LastLogin = DateTime.Now.AddDays(-2) },
            new() { FullName = "KTV. Trần Đức Mạnh", Email = "duc.manh@qlcm.local", Department = Department.ChanDoanHinhAnh, RoleIds = new[] { doctor.Id }, LastLogin = DateTime.Now.AddHours(-7) },
            new() { FullName = "KTV. Phan Hà Linh", Email = "ha.linh@qlcm.local", Department = Department.XetNghiem, RoleIds = new[] { nurse.Id, doctor.Id }, LastLogin = DateTime.Now.AddMinutes(-6) },
            new() { FullName = "BS. Đặng Thái Sơn", Email = "thai.son@qlcm.local", Department = Department.HanhChinh, RoleIds = new[] { admin.Id }, LastLogin = DateTime.Now.AddMinutes(-2) },
        };

        return (roles, users);
    }
}
