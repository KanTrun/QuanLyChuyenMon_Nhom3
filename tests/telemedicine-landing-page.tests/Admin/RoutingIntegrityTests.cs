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
        Assert.Contains("@layout AdminLayout", protocolWorkspacePage);
    }

    [Fact]
    public void AdminClinicalAndProtocolRoutes_AreRoutable()
    {
        var root = FindRepositoryRoot();
        var adminClinicalPage = File.ReadAllText(Path.Combine(root, "src", "telemedicine-landing-page", "Components", "Pages", "Admin", "LamSangPage.razor"));
        var adminProtocolPage = File.ReadAllText(Path.Combine(root, "src", "telemedicine-landing-page", "Components", "Pages", "Admin", "PhacDoPage.razor"));
        var clinicalWorkspacePage = File.ReadAllText(Path.Combine(root, "src", "telemedicine-landing-page", "Components", "Pages", "Clinical", "ClinicalPage.razor"));

        Assert.Contains("@page \"/admin/lam-sang\"", adminClinicalPage);
        Assert.Contains("@page \"/admin/phac-do\"", adminProtocolPage);
        Assert.DoesNotContain("@page \"/admin/lam-sang\"", clinicalWorkspacePage);
    }

    [Fact]
    public void RazorPageRoutes_AreUniqueAcrossComponents()
    {
        var root = FindRepositoryRoot();
        var pagesRoot = Path.Combine(root, "src", "telemedicine-landing-page", "Components", "Pages");
        var routes = Directory.EnumerateFiles(pagesRoot, "*.razor", SearchOption.AllDirectories)
            .SelectMany(file => File.ReadLines(file)
                .Where(line => line.TrimStart().StartsWith("@page ", StringComparison.Ordinal))
                .Select(line => new
                {
                    Route = line.Trim().Split('"', StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1),
                    File = Path.GetRelativePath(root, file)
                }))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Route))
            .ToList();

        var duplicateRoutes = routes
            .GroupBy(entry => entry.Route, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(entry => entry.File))}")
            .ToList();

        Assert.Empty(duplicateRoutes);
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
