using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class SqlReportServiceTests
{
    [Fact]
    public void GenerateConsumptionReportForDepartment_FiltersByDepartmentTree()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var noiChildId = Guid.NewGuid();
        var serviceNoiId = Guid.NewGuid();
        var serviceNgoaiId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var orderNoiId = Guid.NewGuid();
        var orderNgoaiId = Guid.NewGuid();

        db.Departments.Add(new Department
        {
            DepartmentId = noiChildId,
            Code = "NOI-CHILD-REPORT",
            Name = "Đơn vị báo cáo khoa Nội",
            ParentDepartmentId = MedDataStoreSeed.DeptNoiId
        });
        db.DepartmentClosure.Add(new DepartmentClosureEdge
        {
            AncestorDepartmentId = noiChildId,
            DescendantDepartmentId = noiChildId,
            Depth = 0
        });
        db.DepartmentClosure.Add(new DepartmentClosureEdge
        {
            AncestorDepartmentId = MedDataStoreSeed.DeptNoiId,
            DescendantDepartmentId = noiChildId,
            Depth = 1
        });
        db.ResourceCatalog.Add(new ResourceCatalogItem
        {
            ResourceId = resourceId,
            ResourceCode = "VT-REPORT",
            ResourceType = "supply",
            Name = "Vật tư báo cáo",
            DefaultUnitCode = "piece"
        });
        db.TechnicalServices.AddRange(
            new TechnicalService
            {
                TechnicalServiceId = serviceNoiId,
                ServiceCode = "DV-NOI",
                Name = "Dịch vụ khoa Nội",
                ServiceType = "lab",
                DepartmentId = noiChildId
            },
            new TechnicalService
            {
                TechnicalServiceId = serviceNgoaiId,
                ServiceCode = "DV-NGOAI",
                Name = "Dịch vụ khoa Ngoại",
                ServiceType = "lab",
                DepartmentId = MedDataStoreSeed.DeptNgoaiId
            });
        db.TechnicalResourceNorms.AddRange(
            new TechnicalResourceNorm
            {
                TechnicalServiceId = serviceNoiId,
                ResourceId = resourceId,
                StandardQuantity = 2,
                UnitCode = "piece"
            },
            new TechnicalResourceNorm
            {
                TechnicalServiceId = serviceNgoaiId,
                ResourceId = resourceId,
                StandardQuantity = 3,
                UnitCode = "piece"
            });
        db.TechnicalOrders.AddRange(
            new TechnicalOrder
            {
                TechnicalOrderId = orderNoiId,
                TechnicalServiceId = serviceNoiId,
                OrderingDepartmentId = noiChildId,
                OrderedAt = DateTime.UtcNow
            },
            new TechnicalOrder
            {
                TechnicalOrderId = orderNgoaiId,
                TechnicalServiceId = serviceNgoaiId,
                OrderingDepartmentId = MedDataStoreSeed.DeptNgoaiId,
                OrderedAt = DateTime.UtcNow
            });
        db.ActualResourceUsages.AddRange(
            new ActualResourceUsage
            {
                TechnicalOrderId = orderNoiId,
                ResourceId = resourceId,
                ActualQuantity = 4,
                UnitCode = "piece",
                IsFinal = true,
                CapturedAt = DateTime.UtcNow
            },
            new ActualResourceUsage
            {
                TechnicalOrderId = orderNgoaiId,
                ResourceId = resourceId,
                ActualQuantity = 8,
                UnitCode = "piece",
                IsFinal = true,
                CapturedAt = DateTime.UtcNow
            });
        db.SaveChanges();

        var service = new SqlReportService(db);
        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var to = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        var allRows = service.GenerateConsumptionReportForDepartment(from, to, null);
        var noiRows = service.GenerateConsumptionReportForDepartment(from, to, MedDataStoreSeed.DeptNoiId);

        Assert.Contains(allRows, row => row.TechnicalServiceName == "Dịch vụ khoa Nội");
        Assert.Contains(allRows, row => row.TechnicalServiceName == "Dịch vụ khoa Ngoại");
        Assert.Single(noiRows);
        Assert.Equal("Dịch vụ khoa Nội", noiRows[0].TechnicalServiceName);
        Assert.Equal(2, noiRows[0].Variance);
        Assert.Equal(100, noiRows[0].VariancePercent);
    }
}
