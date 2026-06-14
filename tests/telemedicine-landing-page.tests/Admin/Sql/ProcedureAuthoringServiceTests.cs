using TelemedicineLandingPage.Models.Admin;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class ProcedureAuthoringServiceTests
{
    [Fact]
    public void CreateVersion_NewProcedure_CreatesV01()
    {
        var store = new MedDataStore();
        var service = new ProcedureAuthoringService(store);

        var result = service.CreateVersion(CreateCommand());

        Assert.Equal(1, result.Version.VersionNo);
        Assert.Equal("v01", result.Version.VersionLabel);
        Assert.Equal("draft", result.Version.StatusCode);
        Assert.Single(store.ProcedureDocumentSections, item => item.ProcedureVersionId == result.Version.ProcedureVersionId);
    }

    [Fact]
    public void CreateVersion_FromDraft_CreatesV02AndArchivesV01WithoutCopyingSignoff()
    {
        var store = new MedDataStore();
        var service = new ProcedureAuthoringService(store);
        var original = service.CreateVersion(CreateCommand());
        store.AddProcedureSignoffRecord(new ProcedureSignoffRecord
        {
            ProcedureVersionId = original.Version.ProcedureVersionId,
            SignoffRole = "writer",
            ContentHashSha256 = "old-hash"
        });
        store.AddProcedureVersionResourceNorm(new ProcedureVersionResourceNorm
        {
            ProcedureVersionId = original.Version.ProcedureVersionId,
            ResourceId = store.ResourceCatalog.First().ResourceId,
            StandardQuantity = 1,
            UnitCode = "unit"
        });
        store.AddProcedureScreenMapping(new ProcedureScreenMapping
        {
            ProcedureVersionId = original.Version.ProcedureVersionId,
            ScreenId = store.Screens.First().ScreenId
        });

        var command = CreateCommand(original.Procedure.ProcedureId, original.Version.ProcedureVersionId);
        var updated = service.CreateVersion(command);

        Assert.Equal(2, updated.Version.VersionNo);
        Assert.Equal("v02", updated.Version.VersionLabel);
        Assert.Equal(2, store.ProcedureVersions.Count(item => item.ProcedureId == original.Procedure.ProcedureId));
        Assert.Equal("archived", store.ProcedureVersions.Single(item => item.ProcedureVersionId == original.Version.ProcedureVersionId).StatusCode);
        Assert.Equal("Nội dung v01", store.ProcedureDocumentSections.Single(item => item.ProcedureVersionId == original.Version.ProcedureVersionId).ContentText);
        Assert.Equal("Nội dung v02", store.ProcedureDocumentSections.Single(item => item.ProcedureVersionId == updated.Version.ProcedureVersionId).ContentText);
        Assert.Single(store.ProcedureAttachments, item => item.ProcedureVersionId == updated.Version.ProcedureVersionId);
        Assert.Single(store.ProcedureAttachments, item => item.ProcedureVersionId == original.Version.ProcedureVersionId);
        Assert.Single(store.ProcedureVersionResourceNorms, item => item.ProcedureVersionId == updated.Version.ProcedureVersionId);
        Assert.Single(store.ProcedureScreenMappings, item => item.ProcedureVersionId == updated.Version.ProcedureVersionId);
        Assert.DoesNotContain(store.ProcedureSignoffRecords, item => item.ProcedureVersionId == updated.Version.ProcedureVersionId);
    }

    [Fact]
    public void CreateVersion_FromActive_KeepsV01ActiveUntilV02IsPublished()
    {
        var store = new MedDataStore();
        var service = new ProcedureAuthoringService(store);
        var original = service.CreateVersion(CreateCommand());
        store.UpdateProcedureVersion(original.Version with { StatusCode = "active" });

        var updated = service.CreateVersion(CreateCommand(original.Procedure.ProcedureId, original.Version.ProcedureVersionId));

        Assert.Equal("active", store.ProcedureVersions.Single(item => item.ProcedureVersionId == original.Version.ProcedureVersionId).StatusCode);
        Assert.Equal("draft", updated.Version.StatusCode);
    }

    private static ProcedureAuthoringCommand CreateCommand(Guid? procedureId = null, Guid? sourceVersionId = null)
    {
        var isUpdate = procedureId.HasValue;
        return new ProcedureAuthoringCommand(
            Guid.NewGuid(),
            procedureId,
            sourceVersionId,
            "QT.TEST.VERSION",
            "Quy trình kiểm thử phiên bản",
            "technical",
            MedDataStoreSeed.DeptNoiId,
            "Mô tả",
            isUpdate ? "Cập nhật" : "Khởi tạo",
            new DateTime(2026, 6, 13),
            isUpdate ? 2 : 1,
            MedDataStoreSeed.AdminUserId,
            [new ProcedureSectionDraft
            {
                Order = 1,
                Number = "I",
                Title = "Mục đích",
                Kind = "purpose",
                Content = isUpdate ? "Nội dung v02" : "Nội dung v01"
            }],
            [new ProcedureRecipientDraft { Name = "Khoa Nội" }],
            [new ProcedureRevisionDraft { Summary = isUpdate ? "Cập nhật v02" : "Ban hành v01" }],
            [new ProcedureFlowStepDraft
            {
                Code = "BUOC-01",
                Name = "Thực hiện",
                Responsibility = "Điều dưỡng",
                Description = "Thực hiện đúng hướng dẫn"
            }],
            [new ProcedureStoredAttachmentDraft("source_pdf", "source.pdf", "test/source.pdf", "application/pdf", 1024, "ABC123")]);
    }
}
