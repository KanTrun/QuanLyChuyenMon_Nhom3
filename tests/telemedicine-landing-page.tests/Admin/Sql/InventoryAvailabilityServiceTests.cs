using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class InventoryAvailabilityServiceTests
{
    [Fact]
    public void CreateMissingSnapshots_UsesActiveInternalCatalogAsAvailable()
    {
        var store = new MedDataStore();
        var service = new InventoryAvailabilityService(store);
        var order = store.TechnicalOrders.First(o => o.TechnicalOrderId == MedDataStoreSeed.OrderCtmId);

        var result = service.CreateMissingSnapshots(order);

        Assert.Equal(2, result.Added);
        Assert.Equal(0, result.Insufficient);
        Assert.All(store.ResourceAvailabilitySnapshots.Where(s => s.TechnicalOrderId == order.TechnicalOrderId),
            snapshot => Assert.Equal("available", snapshot.AvailabilityStatus));
    }

    [Fact]
    public void CreateMissingSnapshots_MissingCatalogResourceIsInsufficient()
    {
        var store = new MedDataStore();
        var serviceId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var order = new TechnicalOrder { TechnicalServiceId = serviceId };
        store.AddTechnicalService(new TechnicalService
        {
            TechnicalServiceId = serviceId,
            ServiceCode = "DV-TEST-MISSING-RESOURCE",
            Name = "Dich vu thieu vat tu",
            ServiceType = "lab"
        });
        store.AddTechnicalResourceNorm(new TechnicalResourceNorm
        {
            TechnicalServiceId = serviceId,
            ResourceId = resourceId,
            StandardQuantity = 3,
            UnitCode = "piece"
        });
        store.AddTechnicalOrder(order);

        var result = new InventoryAvailabilityService(store).CreateMissingSnapshots(order);

        var snapshot = Assert.Single(store.ResourceAvailabilitySnapshots,
            s => s.TechnicalOrderId == order.TechnicalOrderId);
        Assert.Equal(1, result.Added);
        Assert.Equal(1, result.Insufficient);
        Assert.Equal("insufficient", snapshot.AvailabilityStatus);
        Assert.Equal(0, snapshot.AvailableQuantity);
    }
}
