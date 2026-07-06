using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin;

/// <summary>
/// Aggregates data from the other admin services into the dashboards and report
/// pages. Report rows are deterministic for a given input so the UI is stable
/// across page refreshes.
/// </summary>
public interface IReportService
{
    IReadOnlyList<ConsumptionReportRow> GenerateConsumptionReport(DateOnly from, DateOnly to, Department? department);
    IReadOnlyList<ConsumptionReportRow> GenerateConsumptionReportForDepartment(DateOnly from, DateOnly to, Guid? departmentId);
    IReadOnlyList<DashboardKpi> GetDashboardKpis();
    IReadOnlyList<ActivityEntry> GetActivityFeed(int take);
    IReadOnlyList<(DateOnly Day, int Count)> GetActivityTrend(int days);
}
