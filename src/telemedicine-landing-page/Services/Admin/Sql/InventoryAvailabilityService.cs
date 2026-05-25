using System.Text.Json;
using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

public sealed record InventorySnapshotResult(int Added, int Insufficient, int Existing);

public interface IInventoryAvailabilityService
{
    InventorySnapshotResult CreateMissingSnapshots(TechnicalOrder order);
}

public sealed class InventoryAvailabilityService : IInventoryAvailabilityService
{
    private readonly IMedDataStore _store;

    public InventoryAvailabilityService(IMedDataStore store)
    {
        _store = store;
    }

    public InventorySnapshotResult CreateMissingSnapshots(TechnicalOrder order)
    {
        var norms = ResolveNorms(order).ToList();
        var added = 0;
        var insufficient = 0;
        var existing = 0;

        foreach (var norm in norms)
        {
            if (_store.ResourceAvailabilitySnapshots.Any(s =>
                    s.TechnicalOrderId == order.TechnicalOrderId &&
                    s.ResourceId == norm.ResourceId))
            {
                existing++;
                continue;
            }

            var resource = _store.ResourceCatalog.FirstOrDefault(r => r.ResourceId == norm.ResourceId);
            var available = resource?.Status == "active" ? norm.Quantity : 0;
            var status = available >= norm.Quantity ? "available" : "insufficient";
            if (status == "insufficient")
            {
                insufficient++;
            }

            _store.AddResourceAvailabilitySnapshot(new ResourceAvailabilitySnapshot
            {
                TechnicalOrderId = order.TechnicalOrderId,
                ResourceId = norm.ResourceId,
                RequiredQuantity = norm.Quantity,
                AvailableQuantity = available,
                UnitCode = norm.UnitCode,
                AvailabilityStatus = status,
                ExternalPayloadJson = JsonSerializer.Serialize(new
                {
                    source = "internal_catalog",
                    resourceStatus = resource?.Status ?? "missing",
                    checkedBy = nameof(InventoryAvailabilityService)
                })
            });
            added++;
        }

        return new InventorySnapshotResult(added, insufficient, existing);
    }

    private IEnumerable<ResourceNorm> ResolveNorms(TechnicalOrder order)
    {
        if (order.ProcedureVersionId.HasValue)
        {
            foreach (var norm in _store.ProcedureVersionResourceNorms
                .Where(n => n.ProcedureVersionId == order.ProcedureVersionId.Value && n.IsRequired))
            {
                yield return new ResourceNorm(norm.ResourceId, norm.StandardQuantity, norm.UnitCode);
            }
        }

        foreach (var norm in _store.TechnicalResourceNorms
            .Where(n => n.TechnicalServiceId == order.TechnicalServiceId && n.IsRequired))
        {
            yield return new ResourceNorm(norm.ResourceId, norm.StandardQuantity, norm.UnitCode);
        }
    }

    private sealed record ResourceNorm(Guid ResourceId, decimal Quantity, string UnitCode);
}
