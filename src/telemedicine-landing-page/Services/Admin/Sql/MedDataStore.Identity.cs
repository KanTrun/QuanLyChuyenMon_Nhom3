using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>Phần quản lý người dùng, vai trò, nhóm.</summary>
public sealed partial class MedDataStore
{
    public void AddUser(AppUser user)
    {
        lock (_lock)
        {
            if (_users.Any(u => u.Username == user.Username))
                throw MedDomainException.Constraint(
                    "UQ_users_username", 2627, $"Tên đăng nhập '{user.Username}' đã tồn tại.");
            _users.Add(user);
            RaiseStateChanged();
        }
    }

    public void UpdateUser(AppUser user)
    {
        lock (_lock)
        {
            var idx = _users.FindIndex(u => u.UserId == user.UserId);
            if (idx < 0)
                throw MedDomainException.Constraint("PK_users", 547, "Người dùng không tồn tại.");

            var old = _users[idx];
            _users[idx] = user;

            // TR_users_expire_security_assignments: khi trạng thái chuyển khỏi active
            // hoặc DeletedAt được đặt, hết hạn tất cả gán quyền đang hoạt động
            if ((old.Status == "active" && user.Status != "active") || (old.DeletedAt is null && user.DeletedAt is not null))
            {
                var now = DateTime.UtcNow;
                for (int i = 0; i < _userRoles.Count; i++)
                {
                    if (_userRoles[i].UserId == user.UserId && _userRoles[i].EffectiveTo is null)
                        _userRoles[i] = _userRoles[i] with { EffectiveTo = now };
                }
                for (int i = 0; i < _userGroupMembers.Count; i++)
                {
                    if (_userGroupMembers[i].UserId == user.UserId && _userGroupMembers[i].EffectiveTo is null)
                        _userGroupMembers[i] = _userGroupMembers[i] with { EffectiveTo = now };
                }
                for (int i = 0; i < _userPermissionOverrides.Count; i++)
                {
                    if (_userPermissionOverrides[i].UserId == user.UserId && _userPermissionOverrides[i].EffectiveTo is null)
                        _userPermissionOverrides[i] = _userPermissionOverrides[i] with { EffectiveTo = now };
                }
            }
            RaiseStateChanged();
        }
    }

    public void AddRole(Role role)
    {
        lock (_lock)
        {
            if (_roles.Any(r => r.Code == role.Code))
                throw MedDomainException.Constraint("UQ_roles_code", 2627, $"Mã vai trò '{role.Code}' đã tồn tại.");
            _roles.Add(role);
            RaiseStateChanged();
        }
    }

    public void UpdateRole(Role role)
    {
        lock (_lock)
        {
            var idx = _roles.FindIndex(r => r.RoleId == role.RoleId);
            if (idx < 0)
                throw MedDomainException.Constraint("PK_roles", 547, "Vai trò không tồn tại.");
            if (_roles.Any(r => r.RoleId != role.RoleId && r.Code == role.Code))
                throw MedDomainException.Constraint("UQ_roles_code", 2627, $"Mã vai trò '{role.Code}' đã tồn tại.");

            var current = _roles[idx];
            _roles[idx] = role with
            {
                IsSystem = current.IsSystem,
                CreatedAt = current.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };
            RaiseStateChanged();
        }
    }

    public void ArchiveRole(Guid roleId)
    {
        lock (_lock)
        {
            var idx = _roles.FindIndex(r => r.RoleId == roleId);
            if (idx < 0)
                throw MedDomainException.Constraint("PK_roles", 547, "Vai trò không tồn tại.");
            if (_roles[idx].IsSystem)
                throw MedDomainException.Constraint("CK_roles_system_archive", 51030, "Không thể lưu trữ vai trò hệ thống.");

            _roles[idx] = _roles[idx] with { Status = "archived", UpdatedAt = DateTime.UtcNow };
            var now = DateTime.UtcNow;
            for (var i = 0; i < _userRoles.Count; i++)
            {
                if (_userRoles[i].RoleId == roleId && _userRoles[i].EffectiveTo is null)
                    _userRoles[i] = _userRoles[i] with { EffectiveTo = now };
            }
            RaiseStateChanged();
        }
    }

    public void AddGroup(Group group)
    {
        lock (_lock)
        {
            if (_groups.Any(g => g.Code == group.Code))
                throw MedDomainException.Constraint("UQ_groups_code", 2627, $"Mã nhóm '{group.Code}' đã tồn tại.");
            _groups.Add(group);
            RaiseStateChanged();
        }
    }

    public void AddUserRole(UserRole userRole)
    {
        lock (_lock)
        {
            ValidateDates(userRole.EffectiveFrom, userRole.EffectiveTo, "CK_user_roles_dates");
            _userRoles.Add(userRole);
            RaiseStateChanged();
        }
    }

    public void RemoveUserRole(Guid userRoleId)
    {
        lock (_lock)
        {
            var removed = _userRoles.RemoveAll(r => r.UserRoleId == userRoleId);
            if (removed == 0)
                throw MedDomainException.Constraint("PK_user_roles", 547, "Gán vai trò không tồn tại.");
            RaiseStateChanged();
        }
    }

    public void AddUserGroupMember(UserGroupMember member)
    {
        lock (_lock)
        {
            ValidateDates(member.EffectiveFrom, member.EffectiveTo, "CK_user_group_members_dates");
            _userGroupMembers.Add(member);
            RaiseStateChanged();
        }
    }

    public void RemoveUserGroupMember(Guid membershipId)
    {
        lock (_lock)
        {
            var removed = _userGroupMembers.RemoveAll(m => m.UserGroupMemberId == membershipId);
            if (removed == 0)
                throw MedDomainException.Constraint("PK_user_group_members", 547, "Thành viên nhóm không tồn tại.");
            RaiseStateChanged();
        }
    }
}
