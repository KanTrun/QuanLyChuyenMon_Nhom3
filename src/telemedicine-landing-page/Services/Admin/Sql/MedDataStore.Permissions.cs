using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>Phần quản lý màn hình, tính năng, quyền và gán quyền.</summary>
public sealed partial class MedDataStore
{
    public void AddScreen(ScreenCatalog screen)
    {
        lock (_lock)
        {
            if (_screens.Any(s => s.ScreenCode == screen.ScreenCode))
                throw MedDomainException.Constraint("UQ_screen_catalog_code", 2627, $"Mã màn hình '{screen.ScreenCode}' đã tồn tại.");
            _screens.Add(screen);
            RaiseStateChanged();
        }
    }

    public void AddFeature(FeatureCatalog feature)
    {
        lock (_lock)
        {
            if (_features.Any(f => f.ScreenId == feature.ScreenId && f.FeatureCode == feature.FeatureCode))
                throw MedDomainException.Constraint("UQ_feature_catalog_screen_code", 2627, $"Mã tính năng '{feature.FeatureCode}' đã tồn tại trong màn hình.");
            _features.Add(feature);
            RaiseStateChanged();
        }
    }

    public void AddPermission(MedPermission permission)
    {
        lock (_lock)
        {
            if (_permissions.Any(p => p.PermissionCode == permission.PermissionCode))
                throw MedDomainException.Constraint("UQ_permissions_code", 2627, $"Mã quyền '{permission.PermissionCode}' đã tồn tại.");
            _permissions.Add(permission);
            RaiseStateChanged();
        }
    }

    public void AddRolePermission(RolePermission rp)
    {
        lock (_lock)
        {
            ValidateDates(rp.EffectiveFrom, rp.EffectiveTo, "CK_role_permissions_dates");
            ValidateJson(rp.ScopeRuleJson, "scope_rule");
            _rolePermissions.Add(rp);
            RaiseStateChanged();
        }
    }

    public void AddGroupPermission(GroupPermission gp)
    {
        lock (_lock)
        {
            ValidateDates(gp.EffectiveFrom, gp.EffectiveTo, "CK_group_permissions_dates");
            ValidateJson(gp.ScopeRuleJson, "scope_rule");
            _groupPermissions.Add(gp);
            RaiseStateChanged();
        }
    }

    public void AddUserPermissionOverride(UserPermissionOverride upo)
    {
        lock (_lock)
        {
            ValidateDates(upo.EffectiveFrom, upo.EffectiveTo, "CK_user_permission_overrides_dates");
            ValidateJson(upo.ScopeRuleJson, "scope_rule");
            _userPermissionOverrides.Add(upo);
            RaiseStateChanged();
        }
    }
}
