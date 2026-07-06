using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Application.Workflow;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class TechnicalOrderWorkflowServiceTests : IDisposable
{
    private readonly MedDbContext _db;
    private readonly TechnicalOrderWorkflowService _svc;

    public TechnicalOrderWorkflowServiceTests()
    {
        _db = TestDbHelper.CreateSeededContext();
        var audit = new AuditTrailService(_db);
        _svc = new TechnicalOrderWorkflowService(
            new MedDbDataStore(_db),
            audit,
            new TechnicalOrderWorkflowGuard(audit));
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void TransitionStatus_OrderedToScheduled_SucceedsAndAudits()
    {
        var order = CreateOrder("ordered");

        _svc.TransitionStatus(order, "scheduled", MedDataStoreSeed.AdminUserId);

        var updated = _db.TechnicalOrders.First(o => o.TechnicalOrderId == order.TechnicalOrderId);
        Assert.Equal("scheduled", updated.OrderStatus);
        Assert.Contains(_db.AuditLogs, log =>
            log.TargetType == "technical_order" &&
            log.TargetId == order.TechnicalOrderId.ToString());
    }

    [Fact]
    public void TransitionStatus_OrderedToCompleted_IsBlocked()
    {
        var order = CreateOrder("ordered");

        var ex = Assert.Throws<MedDomainException>(() =>
            _svc.TransitionStatus(order, "completed", MedDataStoreSeed.AdminUserId));

        Assert.Equal(50027, ex.SqlErrorNumber);
    }

    private TechnicalOrder CreateOrder(string status)
    {
        var order = new TechnicalOrder
        {
            TechnicalServiceId = Guid.NewGuid(),
            OrderStatus = status
        };
        _db.TechnicalOrders.Add(order);
        _db.SaveChanges();
        return order;
    }
}
