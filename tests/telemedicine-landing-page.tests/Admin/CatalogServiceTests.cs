using TelemedicineLandingPage.Models.Admin;
using TelemedicineLandingPage.Services.Admin;

namespace TelemedicineLandingPage.Tests.Admin;

public sealed class CatalogServiceTests
{
    [Fact]
    public void ImportFromCsv_ParsesRowsAndAppendsToCatalog()
    {
        var service = new CatalogService();
        var beforeCount = service.Search(new CatalogFilter()).Count;

        const string csv = "ServiceCode,ServiceName,ServiceType,Department,Status,ResourceType,ResourceCode,ResourceName,Unit,StandardQuantity,Note\n" +
                           "KT-IMP-01,\"Tiêm phòng dịch vụ mới\",KyThuat,NoiTiet,HoatDong,Thuoc,TH-IMP-01,Vaccine cúm mùa,liều,1,Bảo quản 2-8 độ C\n" +
                           "KT-IMP-01,\"Tiêm phòng dịch vụ mới\",KyThuat,NoiTiet,HoatDong,VatTu,VT-IMP-02,Kim tiêm 23G,cái,1,\n" +
                           "KT-IMP-02,\"Đo huyết áp tự động\",ThuThuat,TimMach,HoatDong,ThietBi,TB-IMP-01,Máy đo điện tử,ca,1,\n";

        var imported = service.ImportFromCsv(csv);

        Assert.Equal(2, imported);
        var all = service.Search(new CatalogFilter());
        Assert.Equal(beforeCount + 2, all.Count);

        var inserted = all.First(s => s.Code == "KT-IMP-01");
        Assert.Equal("Tiêm phòng dịch vụ mới", inserted.Name);
        Assert.Equal(2, inserted.ResourceNorms.Count);
        Assert.Contains(inserted.ResourceNorms, n => n.ResourceCode == "TH-IMP-01" && n.ResourceName == "Vaccine cúm mùa");
        Assert.Contains(inserted.ResourceNorms, n => n.ResourceCode == "VT-IMP-02" && n.Unit == "cái");
    }

    [Fact]
    public void ExportImportRoundtrip_PreservesRows()
    {
        var primary = new CatalogService();

        // Add a unique row that the seed does not contain so we can detect the round-trip.
        primary.Create(new TechnicalServiceRecord
        {
            Code = "KT-RT-99",
            Name = "Kỹ thuật kiểm thử round-trip",
            ServiceType = ServiceType.KyThuat,
            Department = Department.NoiTiet,
            Status = CatalogStatus.HoatDong,
            ResourceNorms = new[]
            {
                new ResourceNorm(ResourceType.VatTu, "VT-RT-1", "Bông y tế", "cái", 4m, "Ghi chú kiểm thử"),
            },
        });

        var csv = primary.ExportToCsv();
        Assert.StartsWith("\uFEFF", csv); // UTF-8 BOM so Excel can render Vietnamese diacritics.

        var recipient = new CatalogService();
        var beforeCount = recipient.Search(new CatalogFilter()).Count;
        var imported = recipient.ImportFromCsv(csv);

        Assert.True(imported >= primary.Search(new CatalogFilter()).Count);
        var afterAll = recipient.Search(new CatalogFilter());

        // The unique code introduced before export must show up after the round-trip.
        var rt = afterAll.FirstOrDefault(s => s.Code == "KT-RT-99");
        Assert.NotNull(rt);
        Assert.Equal("Kỹ thuật kiểm thử round-trip", rt!.Name);
        Assert.Contains(rt.ResourceNorms, n => n.ResourceCode == "VT-RT-1" && n.ResourceName == "Bông y tế");
        Assert.True(afterAll.Count > beforeCount, "Round-trip should have appended the unique KT-RT-99 service.");

        // Importing the same CSV again is a no-op (rows merge by code).
        var totalBefore = recipient.Search(new CatalogFilter()).Count;
        recipient.ImportFromCsv(csv);
        Assert.Equal(totalBefore, recipient.Search(new CatalogFilter()).Count);
    }
}
