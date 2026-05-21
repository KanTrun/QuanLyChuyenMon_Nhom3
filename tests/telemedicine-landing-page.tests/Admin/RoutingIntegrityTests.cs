namespace TelemedicineLandingPage.Tests.Admin;

public sealed class RoutingIntegrityTests
{
    [Fact]
    public void AdminProcedureRoutes_AreOwnedByAdminPagesOnly()
    {
        var root = FindRepositoryRoot();
        var adminProcedurePage = File.ReadAllText(Path.Combine(root, "src", "telemedicine-landing-page", "Components", "Pages", "Admin", "QuyTrinhKtPage.razor"));
        var approvalPage = File.ReadAllText(Path.Combine(root, "src", "telemedicine-landing-page", "Components", "Pages", "Admin", "QuyTrinhPheDuyetPage.razor"));
        var workspacePage = File.ReadAllText(Path.Combine(root, "src", "telemedicine-landing-page", "Components", "Pages", "Procedure", "ProListPage.razor"));
        var protocolWorkspacePage = File.ReadAllText(Path.Combine(root, "src", "telemedicine-landing-page", "Components", "Pages", "Protocol", "ProtocolPage.razor"));

        Assert.Contains("@page \"/admin/quy-trinh\"", adminProcedurePage);
        Assert.Contains("@page \"/admin/quy-trinh/phe-duyet\"", approvalPage);
        Assert.DoesNotContain("@page \"/admin/quy-trinh\"", workspacePage);
        Assert.DoesNotContain("@page \"/admin/quy-trinh/phe-duyet\"", workspacePage);
        Assert.DoesNotContain("@layout AdminLayout", workspacePage);
        Assert.DoesNotContain("@layout AdminLayout", protocolWorkspacePage);
    }

    [Fact]
    public void AdminClinicalAndProtocolRoutes_AreRoutable()
    {
        var root = FindRepositoryRoot();
        var adminClinicalPage = File.ReadAllText(Path.Combine(root, "src", "telemedicine-landing-page", "Components", "Pages", "Admin", "LamSangPage.razor"));
        var adminProtocolPage = File.ReadAllText(Path.Combine(root, "src", "telemedicine-landing-page", "Components", "Pages", "Admin", "PhacDoPage.razor"));

        Assert.Contains("@page \"/admin/lam-sang\"", adminClinicalPage);
        Assert.Contains("@page \"/admin/phac-do\"", adminProtocolPage);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "telemedicine-landing-page.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Cannot locate repository root from test output.");
    }
}
