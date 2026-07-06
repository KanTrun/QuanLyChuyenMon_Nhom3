using TelemedicineLandingPage.Models.Admin;
using TelemedicineLandingPage.Services.Admin;

namespace TelemedicineLandingPage.Tests.Admin;

public sealed class ReportServiceTests
{
    private static IReportService Build()
    {
        var catalog = new CatalogService();
        var procedures = new ProcedureService();
        var protocols = new ProtocolService();
        var permissions = new PermissionService();
        var notifications = new NotificationService();
        return new ReportService(catalog, procedures, protocols, permissions, notifications);
    }

    [Fact]
    public void GenerateConsumptionReport_ReturnsExpectedRowsAndVariancePercent()
    {
        var report = Build();
        var from = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
        var to = DateOnly.FromDateTime(DateTime.Today);

        var rows = report.GenerateConsumptionReport(from, to, null);

        Assert.NotEmpty(rows);
        Assert.All(rows, row =>
        {
            Assert.False(string.IsNullOrWhiteSpace(row.TechnicalServiceCode));
            Assert.False(string.IsNullOrWhiteSpace(row.ResourceName));
            Assert.True(row.ActualQuantity >= 0);
            // Variance and VariancePercent must be self-consistent.
            Assert.Equal(row.ActualQuantity - row.StandardQuantity, row.Variance);
            if (row.StandardQuantity != 0)
            {
                var expected = Math.Round(row.Variance / row.StandardQuantity * 100m, 2, MidpointRounding.AwayFromZero);
                Assert.Equal(expected, row.VariancePercent);
            }
            Assert.Contains(from.ToString("dd/MM/yyyy"), row.Period);
        });

        // Department filter narrows the rowset.
        var noiTiet = report.GenerateConsumptionReport(from, to, Department.NoiTiet);
        Assert.NotEmpty(noiTiet);
        Assert.True(noiTiet.Count <= rows.Count);
    }

    [Fact]
    public void GetDashboardKpis_ReturnsFourTilesWithVietnameseLabels()
    {
        var report = Build();
        var kpis = report.GetDashboardKpis();
        Assert.Equal(4, kpis.Count);
        Assert.Contains(kpis, k => k.Label == "Phác đồ lâm sàng");
        Assert.Contains(kpis, k => k.Label == "Tuân thủ quy trình");
        Assert.All(kpis, k => Assert.NotEmpty(k.Sparkline));
    }

    [Fact]
    public void GetActivityTrend_ReturnsExactNumberOfDays()
    {
        var report = Build();
        var trend = report.GetActivityTrend(7);
        Assert.Equal(7, trend.Count);
        Assert.All(trend, point => Assert.True(point.Count >= 0));
    }
}
