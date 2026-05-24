using TelemedicineLandingPage.Models.Admin;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin;

public sealed class AdminBusinessDisplayTests
{
    [Fact]
    public void PermissionLabel_UsesBusinessNamesInsteadOfCodes()
    {
        var screen = new ScreenCatalog
        {
            ScreenId = Guid.NewGuid(),
            ScreenCode = "SCR_ORDERS",
            Name = "Chỉ định kỹ thuật",
            ModuleCode = "TECH"
        };
        var permission = new MedPermission
        {
            PermissionId = Guid.NewGuid(),
            PermissionCode = "SCR_ORDERS:CREATE",
            ScreenId = screen.ScreenId,
            ActionCode = "create"
        };

        var label = AdminBusinessDisplay.PermissionLabel(permission, new[] { screen }, Array.Empty<FeatureCatalog>());

        Assert.Contains("Chỉ định kỹ thuật", label);
        Assert.Contains("Tạo mới", label);
        Assert.DoesNotContain("SCR_ORDERS", label);
    }

    [Fact]
    public void UnitsCompatible_BlocksDifferentUnitGroups()
    {
        Assert.True(AdminBusinessDisplay.UnitsCompatible("ampoule", "vial"));
        Assert.False(AdminBusinessDisplay.UnitsCompatible("ampoule", "ml"));
    }

    [Fact]
    public void JsonSummary_FormatsKnownKeys()
    {
        var summary = AdminBusinessDisplay.JsonSummary("{\"source\":\"inventory_not_connected\",\"reason\":\"test\"}");

        Assert.Contains("Nguồn", summary);
        Assert.Contains("Lý do", summary);
        Assert.DoesNotContain("{", summary);
    }
}
