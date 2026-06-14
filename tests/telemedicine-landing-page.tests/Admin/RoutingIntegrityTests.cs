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
    public void HomeRoute_IsQlcmIntroBeforeAuthentication()
    {
        var root = FindRepositoryRoot();
        var homePage = File.ReadAllText(Path.Combine(root, "src", "telemedicine-landing-page", "Components", "Pages", "Home.razor"));

        Assert.Contains("@page \"/\"", homePage);
        Assert.Contains("QLCM Pro", homePage);
        Assert.Contains("href=\"/login\"", homePage);
        Assert.Contains("href=\"/register\"", homePage);
        Assert.DoesNotContain("NavigateTo(\"/login\"", homePage);
        Assert.DoesNotContain("telemedicine", homePage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DashboardTrendPanel_HasExplicitStatusDistribution()
    {
        var root = FindRepositoryRoot();
        var dashboardPage = File.ReadAllText(Path.Combine(root, "src", "telemedicine-landing-page", "Components", "Pages", "Admin", "AdminDashboard.razor"));
        var shellCss = File.ReadAllText(Path.Combine(root, "src", "telemedicine-landing-page", "wwwroot", "css", "admin-shell.css"));

        Assert.Contains("admin-status-distribution", dashboardPage);
        Assert.Contains("Phân bố trạng thái phiên bản", dashboardPage);
        Assert.Contains("align-items: start;", shellCss);
        Assert.Contains(".admin-status-strip", shellCss);
    }

    [Fact]
    public void DashboardOperationsPanel_UsesRealOrderAndResourceData()
    {
        var root = FindRepositoryRoot();
        var dashboardPage = File.ReadAllText(Path.Combine(root, "src", "telemedicine-landing-page", "Components", "Pages", "Admin", "AdminDashboard.razor"));
        var shellCss = File.ReadAllText(Path.Combine(root, "src", "telemedicine-landing-page", "wwwroot", "css", "admin-shell.css"));

        Assert.Contains("admin-panel-operations", dashboardPage);
        Assert.Contains("Điều phối chỉ định", dashboardPage);
        Assert.Contains("DataStore.TechnicalOrders", dashboardPage);
        Assert.Contains("DataStore.ResourceAvailabilitySnapshots", dashboardPage);
        Assert.Contains("DataStore.ActualResourceUsages", dashboardPage);
        Assert.Contains(".admin-resource-ring", shellCss);
        Assert.Contains(".admin-order-status-track", shellCss);
    }

    [Fact]
    public void ResourcePage_ShowsArchiveFilterAndDefaultsToActive()
    {
        var root = FindRepositoryRoot();
        var resourcePage = File.ReadAllText(Path.Combine(root, "src", "telemedicine-landing-page", "Components", "Pages", "Resource", "ResourcePage.razor"));

        Assert.Contains("resource-status", resourcePage);
        Assert.Contains("ActiveResourceCount", resourcePage);
        Assert.Contains("ArchivedResourceCount", resourcePage);
        Assert.Contains("_statusFilter = \"active\"", resourcePage);
        Assert.Contains("_statusFilter == \"all\"", resourcePage);
    }

    [Fact]
    public void ProcedureList_ExposesProfessionalPrintPreview()
    {
        var root = FindRepositoryRoot();
        var procedurePage = File.ReadAllText(Path.Combine(root, "src", "telemedicine-landing-page", "Components", "Pages", "Admin", "QuyTrinhKtPage.razor"));
        var shellScript = File.ReadAllText(Path.Combine(root, "src", "telemedicine-landing-page", "wwwroot", "js", "admin-shell.js"));

        Assert.Contains(">In/PDF</button>", procedurePage);
        Assert.Contains("Xem bản in / PDF", procedurePage);
        Assert.Contains("qlcmShell.openPrintableHtml", procedurePage);
        Assert.Contains("openPrintableHtml: openPrintableHtml", shellScript);
        Assert.Contains("window.open(url, '_blank')", shellScript);
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
