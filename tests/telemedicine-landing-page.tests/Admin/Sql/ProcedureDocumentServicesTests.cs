using System.Net;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class ProcedureDocumentServicesTests
{
    private static readonly (string Number, string Title, string Kind)[] RequiredSections =
    [
        ("I", "Mục đích", "purpose"),
        ("II", "Phạm vi", "scope"),
        ("III", "Căn cứ", "basis"),
        ("IV", "Thuật ngữ", "definitions"),
        ("V", "Trách nhiệm", "responsibilities"),
        ("VIII", "Nội dung", "procedure"),
        ("IX", "Lưu đồ", "flowchart"),
        ("X", "Hồ sơ", "records"),
        ("XI", "Phụ lục", "appendices")
    ];

    [Fact]
    public void CheckReadiness_CompleteDocumentWithoutSignoff_IsReadyForWriter()
    {
        var (store, versionId) = CreateCompleteDocument();
        var snapshots = new ProcedureDocumentSnapshotService(store);

        var readiness = snapshots.CheckReadiness(versionId, requireSignoffs: false);

        Assert.True(readiness.IsReady, string.Join(", ", readiness.MissingItems));
    }

    [Fact]
    public void Sign_EditSignedContent_MakesWriterSignoffStale()
    {
        var (store, versionId) = CreateCompleteDocument();
        var snapshots = new ProcedureDocumentSnapshotService(store);
        var signoffs = new ProcedureSignoffService(store, snapshots);
        signoffs.Sign(versionId, "writer", MedDataStoreSeed.AdminUserId, "admin", "Quản trị viên");
        Assert.True(signoffs.HasCurrentSignoff(versionId, "writer"));

        var section = store.ProcedureDocumentSections.First(item => item.ProcedureVersionId == versionId);
        store.UpdateProcedureDocumentSection(section with { ContentText = "Nội dung đã thay đổi" });

        Assert.False(signoffs.HasCurrentSignoff(versionId, "writer"));
    }

    [Fact]
    public void Sign_RequiresAuthenticatedAccountAndCorrectWorkflowStage()
    {
        var (store, versionId) = CreateCompleteDocument();
        var signoffs = new ProcedureSignoffService(store, new ProcedureDocumentSnapshotService(store));

        Assert.Throws<InvalidOperationException>(() =>
            signoffs.Sign(versionId, "writer", null, null, null));
        Assert.Throws<InvalidOperationException>(() =>
            signoffs.Sign(versionId, "checker", MedDataStoreSeed.AdminUserId, "admin", "Người kiểm tra"));

        signoffs.Sign(versionId, "writer", MedDataStoreSeed.AdminUserId, "admin", "Người viết");
        var version = store.ProcedureVersions.Single(item => item.ProcedureVersionId == versionId);
        store.UpdateProcedureVersion(version with { StatusCode = "pending_approval" });

        Assert.Throws<InvalidOperationException>(() =>
            signoffs.Sign(versionId, "approver", MedDataStoreSeed.AdminUserId, "admin", "Người phê duyệt"));

        signoffs.Sign(versionId, "checker", MedDataStoreSeed.AdminUserId, "admin", "Người kiểm tra");
        var approval = signoffs.Sign(versionId, "approver", MedDataStoreSeed.AdminUserId, "admin", "Người phê duyệt");

        Assert.Equal("approver", approval.SignoffRole);
    }

    [Fact]
    public void CheckReadiness_OcrPendingMarker_BlocksSubmission()
    {
        var (store, versionId) = CreateCompleteDocument();
        var section = store.ProcedureDocumentSections.First(item => item.ProcedureVersionId == versionId);
        store.UpdateProcedureDocumentSection(section with { ContentText = "OCR_PENDING: cần đối chiếu" });

        var readiness = new ProcedureDocumentSnapshotService(store).CheckReadiness(versionId, requireSignoffs: false);

        Assert.False(readiness.IsReady);
        Assert.Contains(readiness.MissingItems, item => item.Contains("OCR", StringComparison.Ordinal));
    }

    [Fact]
    public void CheckReadiness_OcrPendingSummary_BlocksSubmissionWithoutPrintingMarker()
    {
        var (store, versionId) = CreateCompleteDocument();
        var version = store.ProcedureVersions.First(item => item.ProcedureVersionId == versionId);
        store.UpdateProcedureVersion(version with { Summary = "{\"ocrStatus\":\"OCR_PENDING\"}" });

        var readiness = new ProcedureDocumentSnapshotService(store).CheckReadiness(versionId, requireSignoffs: false);

        Assert.False(readiness.IsReady);
        Assert.Contains(readiness.MissingItems, item => item.Contains("OCR", StringComparison.Ordinal));
    }

    [Fact]
    public void CheckReadiness_MissingSignoffs_UsesVietnameseRoleLabels()
    {
        var (store, versionId) = CreateCompleteDocument();

        var readiness = new ProcedureDocumentSnapshotService(store).CheckReadiness(versionId, requireSignoffs: true);

        Assert.Contains("Chữ ký Người viết", readiness.MissingItems);
        Assert.Contains("Chữ ký Người kiểm tra", readiness.MissingItems);
        Assert.Contains("Chữ ký Người phê duyệt", readiness.MissingItems);
        Assert.DoesNotContain(readiness.MissingItems, item => item.Contains("writer", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Export_EscapesContentAndRendersFlowShapeAndSourceMetadata()
    {
        var (store, versionId) = CreateCompleteDocument("Quy trình <script>alert(1)</script>");
        var snapshots = new ProcedureDocumentSnapshotService(store);
        var hash = snapshots.ComputeContentHash(versionId);
        store.AddProcedureSignoffRecord(new ProcedureSignoffRecord
        {
            ProcedureVersionId = versionId,
            SignoffRole = "writer",
            SignerFullName = "Điều dưỡng Nguyễn An",
            ContentHashSha256 = hash,
            SignatureImageDataUrl = "data:image/png;base64,AAAA"
        });
        var html = new ProcedureDocumentExportService(snapshots)
            .BuildProcedureDocumentHtml(versionId, new DateTime(2026, 6, 13, 8, 0, 0, DateTimeKind.Utc));
        var visibleText = WebUtility.HtmlDecode(html);

        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("shape-decision", html);
        Assert.Contains("source.pdf", html);
        Assert.Contains("ABC123", html);
        Assert.Contains(store.Departments.First(item => item.DepartmentId == MedDataStoreSeed.DeptNoiId).Name, html);
        Assert.Contains("BỆNH VIỆN UNG BƯỚU", html);
        Assert.Contains("In / Lưu PDF", html);
        Assert.Contains("Điều dưỡng Nguyễn An", html);
        Assert.Contains("data:image/png;base64,AAAA", html);
        Assert.Contains("<th>Trách nhiệm</th><th>Các bước thực hiện</th><th>Mô tả / Các biểu mẫu</th>", html);
        Assert.Contains("flow-symbol shape-terminator", html);
        Assert.Contains("Bước kiểm soát 1", visibleText);
        Assert.Contains("BM.KSNK.01", html);
        Assert.Contains("LƯU ĐỒ QUY TRÌNH <span class=\"continuation\">(1/1)</span>", html);
        Assert.DoesNotContain("OCR_PENDING", html);
        Assert.True(Count(html, "<section class=\"page\">") >= 13);
        Assert.Contains("Trang 1 /", html);
    }

    [Theory]
    [InlineData("QT.KSNK.12", "BM.KSNK.12.10", "Vận hành máy hấp phù hợp")]
    [InlineData("QT.KSNK.16", "BM.KSNK.16.04", "hạn sử dụng 14 ngày")]
    [InlineData("QT.KSNK.17", "5.2.7", "10 - 15 giây với dầu bôi trơn")]
    public void Export_SeededKsnkFlowchart_RendersSourceCheckedContent(
        string procedureCode,
        string expectedReference,
        string expectedDescription)
    {
        var store = new MedDataStore();
        var procedure = store.Procedures.Single(item => item.ProcedureCode == procedureCode);
        var version = store.ProcedureVersions.Single(item => item.ProcedureId == procedure.ProcedureId);

        var html = new ProcedureDocumentExportService(new ProcedureDocumentSnapshotService(store))
            .BuildProcedureDocumentHtml(version.ProcedureVersionId, new DateTime(2026, 6, 15, 8, 0, 0, DateTimeKind.Utc));
        var visibleText = WebUtility.HtmlDecode(html);

        Assert.Contains("<th>Trách nhiệm</th><th>Các bước thực hiện</th><th>Mô tả / Các biểu mẫu</th>", html);
        Assert.Contains(expectedReference, visibleText);
        Assert.Contains(expectedDescription, visibleText);
        Assert.DoesNotContain("Biểu mẫu/phụ lục: đối chiếu theo PDF scan nguồn", visibleText);
    }

    private static (MedDataStore Store, Guid VersionId) CreateCompleteDocument(string name = "Quy trình kiểm thử")
    {
        var store = new MedDataStore();
        var procedure = new ProfessionalProcedure
        {
            ProcedureCode = $"QT.TEST.{Guid.NewGuid():N}"[..20],
            Name = name,
            ProcedureType = "technical",
            OwnerDepartmentId = MedDataStoreSeed.DeptNoiId,
            CreatedBy = MedDataStoreSeed.AdminUserId
        };
        store.AddProcedure(procedure);

        var version = new ProcedureVersion
        {
            ProcedureId = procedure.ProcedureId,
            VersionNo = 1,
            VersionLabel = "v1.0",
            Title = name,
            Summary = "{\"note\":\"test\"}",
            IssueDate = new DateTime(2026, 6, 13),
            IssueNumber = 1,
            SourcePdfFileName = "source.pdf",
            SourcePdfChecksumSha256 = "ABC123",
            CreatedBy = MedDataStoreSeed.AdminUserId
        };
        store.AddProcedureVersion(version);

        foreach (var item in RequiredSections.Select((section, index) => (section, index)))
            store.AddProcedureDocumentSection(new ProcedureDocumentSection
            {
                ProcedureVersionId = version.ProcedureVersionId,
                SectionOrder = item.index + 1,
                SectionNumber = item.section.Number,
                Title = item.section.Title,
                SectionKind = item.section.Kind,
                ContentText = "Nội dung đầy đủ"
            });

        store.AddProcedureDistributionRecipient(new ProcedureDistributionRecipient
        {
            ProcedureVersionId = version.ProcedureVersionId,
            DisplayOrder = 1,
            RecipientName = "Khoa Kiểm soát nhiễm khuẩn"
        });
        store.AddProcedureRevisionEntry(new ProcedureRevisionEntry
        {
            ProcedureVersionId = version.ProcedureVersionId,
            DisplayOrder = 1,
            RevisionDate = new DateTime(2026, 6, 13),
            Summary = "Ban hành lần đầu"
        });
        var shapes = new[] { "terminator", "process", "decision", "data", "document" };
        for (var i = 0; i < shapes.Length; i++)
        {
            store.AddProcedureStep(new ProcedureStep
            {
                ProcedureVersionId = version.ProcedureVersionId,
                StepNo = i + 1,
                StepCode = $"B{i + 1:00}",
                Name = $"Bước kiểm soát {i + 1}",
                Description = "Đối chiếu hồ sơ và điều kiện thực hiện",
                ResponsibilityText = "Điều dưỡng KSNK",
                FormReferenceText = "BM.KSNK.01",
                DetailSectionNumber = "VIII",
                StandardDurationMinutes = 10,
                FlowShapeCode = shapes[i]
            });
        }
        store.AddProcedureAttachment(new ProcedureAttachment
        {
            ProcedureVersionId = version.ProcedureVersionId,
            AttachmentType = "source_pdf",
            FileName = "source.pdf",
            FileUri = "test/source.pdf",
            MimeType = "application/pdf",
            FileSizeBytes = 1024,
            ChecksumSha256 = "ABC123"
        });

        return (store, version.ProcedureVersionId);
    }

    private static int Count(string value, string token)
        => (value.Length - value.Replace(token, string.Empty, StringComparison.Ordinal).Length) / token.Length;
}
