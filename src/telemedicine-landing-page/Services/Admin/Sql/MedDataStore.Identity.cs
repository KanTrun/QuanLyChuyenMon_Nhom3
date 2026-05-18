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

    public void AddUserGroupMember(UserGroupMember member)
    {
        lock (_lock)
        {
            ValidateDates(member.EffectiveFrom, member.EffectiveTo, "CK_user_group_members_dates");
            _userGroupMembers.Add(member);
            RaiseStateChanged();
        }
    }
}
