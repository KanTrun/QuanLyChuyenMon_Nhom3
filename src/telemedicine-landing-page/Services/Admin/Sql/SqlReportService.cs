using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// SQL-backed report service. Consumption reports are computed from actual
/// usages plus service/procedure norms in MedicalProcedureManagement.
/// </summary>
public sealed class SqlReportService : IReportService
{
    private readonly MedDbContext _db;

    public SqlReportService(MedDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<ConsumptionReportRow> GenerateConsumptionReport(DateOnly from, DateOnly to, Department? department)
        => GenerateConsumptionReportForDepartment(from, to, null);

    public IReadOnlyList<ConsumptionReportRow> GenerateConsumptionReportForDepartment(DateOnly from, DateOnly to, Guid? departmentId)
    {
        var fromUtc = AdminDateTimeDisplay.DisplayDateStartUtc(from);
        var toUtc = AdminDateTimeDisplay.DisplayDateEndExclusiveUtc(to);
        var period = $"{from:dd/MM/yyyy} - {to:dd/MM/yyyy}";
        var departmentScope = ResolveDepartmentScope(departmentId);

        var services = _db.TechnicalServices.ToDictionary(s => s.TechnicalServiceId);
        var resources = _db.ResourceCatalog.ToDictionary(r => r.ResourceId);
        var orders = _db.TechnicalOrders
            .AsEnumerable()
            .Where(order => DepartmentMatches(order.OrderingDepartmentId, departmentScope))
            .ToDictionary(o => o.TechnicalOrderId);
        var serviceNorms = _db.TechnicalResourceNorms
            .AsEnumerable()
            .GroupBy(n => (n.TechnicalServiceId, n.ResourceId))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(n => n.CreatedAt).First());
        var procedureNorms = _db.ProcedureVersionResourceNorms
            .AsEnumerable()
            .GroupBy(n => (n.ProcedureVersionId, n.ResourceId))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(n => n.CreatedAt).First());

        return _db.ActualResourceUsages
            .Where(u => u.IsFinal && u.CapturedAt >= fromUtc && u.CapturedAt < toUtc)
            .AsEnumerable()
            .Select(u =>
            {
                if (!orders.TryGetValue(u.TechnicalOrderId, out var order) ||
                    !services.TryGetValue(order.TechnicalServiceId, out var service) ||
                    !resources.TryGetValue(u.ResourceId, out var resource))
                {
                    return null;
                }

                var standard = ResolveStandardQuantity(
                    order.ProcedureVersionId,
                    service.TechnicalServiceId,
                    u.ResourceId,
                    procedureNorms,
                    serviceNorms);
                var variance = u.ActualQuantity - standard;
                var variancePercent = standard == 0 ? 0 : Math.Round(variance * 100 / standard, 2, MidpointRounding.AwayFromZero);

                return new ConsumptionReportRow(
                    service.ServiceCode,
                    service.Name,
                    resource.ResourceCode,
                    resource.Name,
                    u.UnitCode,
                    standard,
                    u.ActualQuantity,
                    variance,
                    variancePercent,
                    period);
            })
            .Where(row => row is not null)
            .Select(row => row!)
            .OrderByDescending(row => Math.Abs(row.VariancePercent))
            .ToList();
    }

    public IReadOnlyList<DashboardKpi> GetDashboardKpis()
    {
        var procedureTotal = _db.Procedures.Count(p => p.Status == "active");
        var activeVersions = _db.ProcedureVersions.Count(v => v.StatusCode == "active");
        var protocolCount = _db.ClinicalProtocols.Count(p => p.Status == "active");
        var unreadNotifications = _db.Notifications.Count(n => n.ReadAt == null);
        var activeUsers = _db.Users.Count(u => u.Status == "active" && u.DeletedAt == null);
        var compliance = procedureTotal == 0
            ? 0
            : Math.Round(activeVersions * 100d / procedureTotal, 1, MidpointRounding.AwayFromZero);

        return new List<DashboardKpi>
        {
            new("Phác đồ lâm sàng", protocolCount.ToString(), 0, TrendDirection.Up, "tone-primary", "stethoscope", new[] { 0, 0, 0, 0, 0, 0, protocolCount }),
            new("Thông báo chưa đọc", unreadNotifications.ToString(), 0, TrendDirection.Down, "tone-warning", "bell", new[] { 0, 0, 0, 0, 0, 0, unreadNotifications }),
            new("Nhân viên hoạt động", activeUsers.ToString(), 0, TrendDirection.Up, "tone-success", "team", new[] { 0, 0, 0, 0, 0, 0, activeUsers }),
            new("Tuân thủ quy trình", $"{compliance.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}%", 0, TrendDirection.Up, "tone-secondary", "check", new[] { 0, 0, 0, 0, 0, 0, (int)Math.Round(compliance) }),
        };
    }

    public IReadOnlyList<ActivityEntry> GetActivityFeed(int take)
    {
        if (take <= 0)
        {
            take = 6;
        }

        return _db.AuditLogs
            .OrderByDescending(log => log.OccurredAt)
            .Take(take)
            .AsEnumerable()
            .Select(log => new ActivityEntry(
                log.OccurredAt,
                log.ActorUsername ?? "Hệ thống",
                log.ActionCode,
                string.IsNullOrWhiteSpace(log.TargetType) ? "Dữ liệu hệ thống" : log.TargetType,
                log.ActionCode is "delete" or "reject" ? ActivitySeverity.Warning : ActivitySeverity.Info))
            .ToList();
    }

    public IReadOnlyList<(DateOnly Day, int Count)> GetActivityTrend(int days)
    {
        if (days <= 0)
        {
            days = 7;
        }

        var today = AdminDateTimeDisplay.Today();
        var start = today.AddDays(-(days - 1));
        var startUtc = AdminDateTimeDisplay.DisplayDateStartUtc(start);

        var counts = _db.AuditLogs
            .Where(log => log.OccurredAt >= startUtc)
            .AsEnumerable()
            .GroupBy(log => DateOnly.FromDateTime(AdminDateTimeDisplay.ToDisplayTime(log.OccurredAt)))
            .ToDictionary(g => g.Key, g => g.Count());

        return Enumerable.Range(0, days)
            .Select(offset =>
            {
                var day = start.AddDays(offset);
                return (day, counts.TryGetValue(day, out var count) ? count : 0);
            })
            .ToList();
    }

    private static decimal ResolveStandardQuantity(
        Guid? procedureVersionId,
        Guid serviceId,
        Guid resourceId,
        IReadOnlyDictionary<(Guid ProcedureVersionId, Guid ResourceId), TelemedicineLandingPage.Models.Admin.Sql.ProcedureVersionResourceNorm> procedureNorms,
        IReadOnlyDictionary<(Guid TechnicalServiceId, Guid ResourceId), TelemedicineLandingPage.Models.Admin.Sql.TechnicalResourceNorm> serviceNorms)
    {
        if (procedureVersionId.HasValue &&
            procedureNorms.TryGetValue((procedureVersionId.Value, resourceId), out var procedureNorm))
        {
            return procedureNorm.StandardQuantity;
        }

        return serviceNorms.TryGetValue((serviceId, resourceId), out var serviceNorm)
            ? serviceNorm.StandardQuantity
            : 0;
    }

    private HashSet<Guid>? ResolveDepartmentScope(Guid? departmentId)
    {
        if (!departmentId.HasValue)
        {
            return null;
        }

        var ids = _db.DepartmentClosure
            .Where(edge => edge.AncestorDepartmentId == departmentId.Value)
            .Select(edge => edge.DescendantDepartmentId)
            .ToHashSet();
        ids.Add(departmentId.Value);
        return ids;
    }

    private static bool DepartmentMatches(Guid? orderDepartmentId, HashSet<Guid>? departmentScope)
        => departmentScope is null || (orderDepartmentId.HasValue && departmentScope.Contains(orderDepartmentId.Value));
}
