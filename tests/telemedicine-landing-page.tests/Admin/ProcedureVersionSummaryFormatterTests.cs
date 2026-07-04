using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin;

namespace TelemedicineLandingPage.Tests.Admin;

public sealed class ProcedureVersionSummaryFormatterTests
{
    [Fact]
    public void FormatVersionSummary_ShowsActiveAndPending()
    {
        var procedureId = Guid.NewGuid();
        var versions = new[]
        {
            new ProcedureVersion
            {
                ProcedureId = procedureId,
                VersionNo = 1,
                VersionLabel = "v1.0",
                StatusCode = "superseded",
                Title = "v1"
            },
            new ProcedureVersion
            {
                ProcedureId = procedureId,
                VersionNo = 2,
                VersionLabel = "v02",
                StatusCode = "active",
                Title = "v2"
            },
            new ProcedureVersion
            {
                ProcedureId = procedureId,
                VersionNo = 3,
                VersionLabel = "v03",
                StatusCode = "pending_approval",
                Title = "v3"
            }
        };

        var summary = ProcedureVersionSummaryFormatter.FormatVersionSummary(versions);

        Assert.Contains("v02 (hiệu lực)", summary);
        Assert.Contains("v03 (chờ duyệt)", summary);
        Assert.Contains("•", summary);
    }

    [Fact]
    public void ProcedureVersionHistoryPanel_ContainsRollbackAction()
    {
        var root = FindRepositoryRoot();
        var panel = File.ReadAllText(Path.Combine(
            root,
            "src",
            "telemedicine-landing-page",
            "Components",
            "Admin",
            "ProcedureVersionHistoryPanel.razor"));

        Assert.Contains("Khôi phục hiệu lực", panel);
        Assert.Contains("OnRollbackRequested", panel);
        Assert.Contains("CanRollback", panel);
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "telemedicine-landing-page.sln")) ||
                Directory.Exists(Path.Combine(dir.FullName, "src", "telemedicine-landing-page")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
