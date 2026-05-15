using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Tests.Admin;

public sealed class AdminEnumLabelsTests
{
    [Fact]
    public void AllProcedureStatuses_HaveVietnameseLabel()
    {
        foreach (var status in Enum.GetValues<ProcedureStatus>())
        {
            var label = AdminEnumLabels.GetLabel(status);
            Assert.False(string.IsNullOrWhiteSpace(label));
            // The .ToString() fallback would not contain a space; real Vietnamese labels do.
            Assert.NotEqual(status.ToString(), label);
            Assert.Contains(' ', label);
        }
    }

    [Fact]
    public void DepartmentLabels_ContainsAllDepartments()
    {
        foreach (var dept in Enum.GetValues<Department>())
        {
            var label = AdminEnumLabels.GetLabel(dept);
            Assert.False(string.IsNullOrWhiteSpace(label));
            Assert.NotEqual(dept.ToString(), label);
        }
    }

    [Fact]
    public void GetTone_ReturnsKnownToneClasses()
    {
        var validTones = new HashSet<string>(StringComparer.Ordinal)
        {
            "tone-muted", "tone-warning", "tone-success", "tone-danger", "tone-secondary", "tone-primary",
        };

        foreach (var status in Enum.GetValues<ProcedureStatus>())
        {
            Assert.Contains(AdminEnumLabels.GetTone(status), validTones);
        }
        foreach (var status in Enum.GetValues<ClinicSessionStatus>())
        {
            Assert.Contains(AdminEnumLabels.GetTone(status), validTones);
        }
        foreach (var status in Enum.GetValues<CatalogStatus>())
        {
            Assert.Contains(AdminEnumLabels.GetTone(status), validTones);
        }
    }
}
