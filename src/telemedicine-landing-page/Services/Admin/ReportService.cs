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
        var rows = new List<ConsumptionReportRow>();
        foreach (var service in _catalogService.Search(new CatalogFilter(Department: department)))
        {
            foreach (var norm in service.ResourceNorms)
            {
                rows.Add(new ConsumptionReportRow(
                    service.Code,
                    service.Name,
                    norm.ResourceCode,
                    norm.ResourceName,
                    norm.Unit,
                    norm.StandardQuantity,
                    0m,
                    0m - norm.StandardQuantity,
                    norm.StandardQuantity == 0 ? 0m : -100m,
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
        var onlineCount = users.Count == 0
            ? 0
            : Math.Max(0, users.Count(u => u.IsActive && u.LastLogin is { } last && (DateTime.Now - last).TotalHours < 8));

        return new List<DashboardKpi>
        {
            new(
                Label: "Phác đồ lâm sàng",
                Value: protocols.Count.ToString(),
                TrendPercent: 0,
                TrendDirection: TrendDirection.Up,
                Tone: "tone-primary",
                Icon: "stethoscope",
                Sparkline: new[] { 0, 0, 0, 0, 0, 0, protocols.Count }),
            new(
                Label: "Thông báo chưa đọc",
                Value: unread.ToString(),
                TrendPercent: 0,
                TrendDirection: TrendDirection.Down,
                Tone: "tone-warning",
                Icon: "bell",
                Sparkline: new[] { 0, 0, 0, 0, 0, 0, unread }),
            new(
                Label: "Nhân viên trực tuyến",
                Value: onlineCount.ToString(),
                TrendPercent: 0,
                TrendDirection: TrendDirection.Up,
                Tone: "tone-success",
                Icon: "team",
                Sparkline: new[] { 0, 0, 0, 0, 0, 0, onlineCount }),
            new(
                Label: "Tuân thủ quy trình",
                Value: $"{compliance.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}%",
                TrendPercent: 0,
                TrendDirection: TrendDirection.Up,
                Tone: "tone-secondary",
                Icon: "check",
                Sparkline: new[] { 0, 0, 0, 0, 0, 0, (int)Math.Round(compliance) }),
        };
    }

    public IReadOnlyList<ActivityEntry> GetActivityFeed(int take)
    {
        if (take <= 0) take = 6;
        var entries = new List<ActivityEntry>();

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
                "Hệ thống",
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

        return entries
            .OrderByDescending(e => e.Timestamp)
            .Take(take)
            .ToList();
    }

    public IReadOnlyList<(DateOnly Day, int Count)> GetActivityTrend(int days)
    {
        if (days <= 0) days = 7;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var result = new List<(DateOnly Day, int Count)>(days);
        for (var i = days - 1; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            result.Add((day, 0));
        }
        return result;
    }
}
