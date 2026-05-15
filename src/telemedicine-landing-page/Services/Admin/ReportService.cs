using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin;

public sealed class ReportService : IReportService
{
    private readonly ICatalogService _catalogService;
    private readonly IProcedureService _procedureService;
    private readonly IProtocolService _protocolService;
    private readonly IPermissionService _permissionService;
    private readonly INotificationService _notificationService;

    public ReportService(
        ICatalogService catalogService,
        IProcedureService procedureService,
        IProtocolService protocolService,
        IPermissionService permissionService,
        INotificationService notificationService)
    {
        _catalogService = catalogService;
        _procedureService = procedureService;
        _protocolService = protocolService;
        _permissionService = permissionService;
        _notificationService = notificationService;
    }

    public IReadOnlyList<ConsumptionReportRow> GenerateConsumptionReport(DateOnly from, DateOnly to, Department? department)
    {
        var period = $"{from:dd/MM/yyyy} - {to:dd/MM/yyyy}";
        // Stable seeded random so report values are reproducible across reloads.
        var seed = unchecked(from.GetHashCode() * 397) ^ to.GetHashCode() ^ (department?.GetHashCode() ?? 0);
        var random = new Random(seed);

        var rows = new List<ConsumptionReportRow>();
        foreach (var service in _catalogService.Search(new CatalogFilter(Department: department)))
        {
            foreach (var norm in service.ResourceNorms)
            {
                // Generate a believable "actual" within +/-30% of the standard, snapped to 2 decimals.
                var jitter = 1d + (random.NextDouble() - 0.5) * 0.6;
                var actualRaw = (double)norm.StandardQuantity * jitter;
                var actual = Math.Round((decimal)actualRaw, 2, MidpointRounding.AwayFromZero);
                if (actual < 0) actual = 0;
                var variance = actual - norm.StandardQuantity;
                decimal variancePercent = norm.StandardQuantity == 0
                    ? 0m
                    : Math.Round(variance / norm.StandardQuantity * 100m, 2, MidpointRounding.AwayFromZero);

                rows.Add(new ConsumptionReportRow(
                    service.Code,
                    service.Name,
                    norm.ResourceCode,
                    norm.ResourceName,
                    norm.Unit,
                    norm.StandardQuantity,
                    actual,
                    variance,
                    variancePercent,
                    period));
            }
        }
        return rows;
    }

    public IReadOnlyList<DashboardKpi> GetDashboardKpis()
    {
        var protocols = _protocolService.Search();
        var procedures = _procedureService.Search(new ProcedureFilter());
        var users = _permissionService.ListUsers();
        var unread = _notificationService.UnreadCount;
        var compliance = procedures.Count == 0
            ? 0
            : Math.Round(
                procedures.Count(p => p.Status == ProcedureStatus.DaBanHanh) * 100d / procedures.Count,
                1,
                MidpointRounding.AwayFromZero);
        var onlineCount = Math.Max(1, users.Count(u => u.IsActive && u.LastLogin is { } last && (DateTime.Now - last).TotalHours < 8));

        return new List<DashboardKpi>
        {
            new(
                Label: "Phác đồ lâm sàng",
                Value: protocols.Count.ToString(),
                TrendPercent: 8.5,
                TrendDirection: TrendDirection.Up,
                Tone: "tone-primary",
                Icon: "stethoscope",
                Sparkline: new[] { 18, 19, 19, 20, 21, 22, protocols.Count }),
            new(
                Label: "Thông báo chưa đọc",
                Value: unread.ToString(),
                TrendPercent: unread > 3 ? 12 : -8,
                TrendDirection: unread > 3 ? TrendDirection.Up : TrendDirection.Down,
                Tone: "tone-warning",
                Icon: "bell",
                Sparkline: new[] { 1, 2, 1, 3, 2, 3, unread }),
            new(
                Label: "Nhân viên trực tuyến",
                Value: onlineCount.ToString(),
                TrendPercent: 6,
                TrendDirection: TrendDirection.Up,
                Tone: "tone-success",
                Icon: "team",
                Sparkline: new[] { 12, 13, 14, 15, 16, 17, onlineCount }),
            new(
                Label: "Tuân thủ quy trình",
                Value: $"{compliance.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}%",
                TrendPercent: 1.2,
                TrendDirection: TrendDirection.Up,
                Tone: "tone-secondary",
                Icon: "check",
                Sparkline: new[] { 92, 93, 94, 94, 95, 96, (int)Math.Round(compliance) }),
        };
    }

    public IReadOnlyList<ActivityEntry> GetActivityFeed(int take)
    {
        if (take <= 0) take = 6;
        var entries = new List<ActivityEntry>();
        var now = DateTime.Now;

        foreach (var procedure in _procedureService.Search(new ProcedureFilter()).Take(3))
        {
            entries.Add(new ActivityEntry(
                procedure.UpdatedAt,
                string.IsNullOrEmpty(procedure.UpdatedBy) ? "Hệ thống" : procedure.UpdatedBy,
                procedure.Status switch
                {
                    ProcedureStatus.DangChoPheDuyet => "đã gửi phê duyệt",
                    ProcedureStatus.DaBanHanh => "ban hành",
                    ProcedureStatus.NgungSuDung => "ngừng sử dụng",
                    _ => "cập nhật",
                },
                procedure.Name,
                procedure.Status == ProcedureStatus.DangChoPheDuyet ? ActivitySeverity.Warning : ActivitySeverity.Info));
        }

        foreach (var protocol in _protocolService.Search().Take(2))
        {
            entries.Add(new ActivityEntry(
                protocol.UpdatedAt,
                "ThS. Trần Phương Linh",
                "cập nhật phác đồ",
                protocol.Name,
                ActivitySeverity.Info));
        }

        foreach (var change in _permissionService.GetChangeLog().Take(2))
        {
            entries.Add(new ActivityEntry(
                change.AppliedAt,
                change.ChangedBy,
                change.TargetType == PermissionTargetType.Role ? "cập nhật quyền vai trò" : "phân quyền tài khoản",
                change.TargetLabel,
                ActivitySeverity.Info));
        }

        if (entries.Count < take)
        {
            entries.Add(new ActivityEntry(now.AddHours(-2), "Hệ thống", "đồng bộ báo cáo", "Báo cáo tiêu thụ tuần 19", ActivitySeverity.Info));
            entries.Add(new ActivityEntry(now.AddHours(-3), "ĐD. Mai Thị Lan", "ghi nhận áp dụng phác đồ", "Phác đồ chăm sóc hậu phẫu cho BN nhi", ActivitySeverity.Warning));
        }

        return entries
            .OrderByDescending(e => e.Timestamp)
            .Take(take)
            .ToList();
    }

    public IReadOnlyList<(DateOnly Day, int Count)> GetActivityTrend(int days)
    {
        if (days <= 0) days = 7;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var rng = new Random(days * 13 + today.DayNumber);
        var result = new List<(DateOnly Day, int Count)>(days);
        for (var i = days - 1; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            // Realistic shape: lower at the weekend, higher mid-week.
            var basis = day.DayOfWeek switch
            {
                DayOfWeek.Sunday => 24,
                DayOfWeek.Saturday => 30,
                DayOfWeek.Monday => 52,
                DayOfWeek.Tuesday => 58,
                DayOfWeek.Wednesday => 64,
                DayOfWeek.Thursday => 60,
                _ => 56,
            };
            var jitter = rng.Next(-6, 7);
            result.Add((day, Math.Max(0, basis + jitter)));
        }
        return result;
    }
}
