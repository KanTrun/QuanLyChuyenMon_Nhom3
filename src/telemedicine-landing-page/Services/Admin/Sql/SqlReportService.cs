using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// SQL-backed report service. Keeps the existing report UI contract while reading
/// operational tables from MedicalProcedureManagement instead of in-memory seeds.
/// </summary>
public sealed class SqlReportService : IReportService
{
    private readonly MedDbContext _db;

    public SqlReportService(MedDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<ConsumptionReportRow> GenerateConsumptionReport(DateOnly from, DateOnly to, Department? department)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var period = $"{from:dd/MM/yyyy} - {to:dd/MM/yyyy}";

        var services = _db.TechnicalServices.ToDictionary(s => s.TechnicalServiceId);
        var resources = _db.ResourceCatalog.ToDictionary(r => r.ResourceId);
        var orders = _db.TechnicalOrders.ToDictionary(o => o.TechnicalOrderId);
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

                var standard = ResolveStandardQuantity(order.ProcedureVersionId, service.TechnicalServiceId, u.ResourceId, procedureNorms, serviceNorms);
                var variance = u.ActualQuantity - standard;
                var variancePercent = standard == 0 ? 0 : variance * 100 / standard;
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
        var compliance = procedureTotal == 0 ? 0 : Math.Round(activeVersions * 100d / procedureTotal, 1, MidpointRounding.AwayFromZero);

        return new List<DashboardKpi>
        {
            new("PhÃ¡c Ä‘á»“ lÃ¢m sÃ ng", protocolCount.ToString(), 0, TrendDirection.Up, "tone-primary", "stethoscope", new[] { 0, 0, 0, 0, 0, 0, protocolCount }),
            new("ThÃ´ng bÃ¡o chÆ°a Ä‘á»c", unreadNotifications.ToString(), 0, TrendDirection.Down, "tone-warning", "bell", new[] { 0, 0, 0, 0, 0, 0, unreadNotifications }),
            new("NhÃ¢n viÃªn hoáº¡t Ä‘á»™ng", activeUsers.ToString(), 0, TrendDirection.Up, "tone-success", "team", new[] { 0, 0, 0, 0, 0, 0, activeUsers }),
            new("TuÃ¢n thá»§ quy trÃ¬nh", $"{compliance.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}%", 0, TrendDirection.Up, "tone-secondary", "check", new[] { 0, 0, 0, 0, 0, 0, (int)Math.Round(compliance) }),
        };
    }

    public IReadOnlyList<ActivityEntry> GetActivityFeed(int take)
    {
        if (take <= 0) take = 6;

        return _db.AuditLogs
            .OrderByDescending(log => log.OccurredAt)
            .Take(take)
            .AsEnumerable()
            .Select(log => new ActivityEntry(
                log.OccurredAt,
                log.ActorUsername ?? "Há»‡ thá»‘ng",
                log.ActionCode,
                string.IsNullOrWhiteSpace(log.TargetType) ? "Dá»¯ liá»‡u há»‡ thá»‘ng" : log.TargetType,
                log.ActionCode is "delete" or "reject" ? ActivitySeverity.Warning : ActivitySeverity.Info))
            .ToList();
    }

    public IReadOnlyList<(DateOnly Day, int Count)> GetActivityTrend(int days)
    {
        if (days <= 0) days = 7;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var start = today.AddDays(-(days - 1));
        var startUtc = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var counts = _db.AuditLogs
            .Where(log => log.OccurredAt >= startUtc)
            .AsEnumerable()
            .GroupBy(log => DateOnly.FromDateTime(log.OccurredAt.ToLocalTime()))
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
}
