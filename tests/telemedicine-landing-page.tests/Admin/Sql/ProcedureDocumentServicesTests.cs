using System.Net;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class ProcedureDocumentServicesTests
{
    private const string ValidSignature = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";
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
        signoffs.Sign(versionId, "writer", MedDataStoreSeed.AdminUserId, "admin", "Quản trị viên", ValidSignature);
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
            signoffs.Sign(versionId, "writer", null, null, null, ValidSignature));
        Assert.Throws<InvalidOperationException>(() =>
            signoffs.Sign(versionId, "checker", MedDataStoreSeed.AdminUserId, "admin", "Người kiểm tra", ValidSignature));
        Assert.Throws<InvalidOperationException>(() =>
            signoffs.Sign(versionId, "writer", MedDataStoreSeed.AdminUserId, "admin", "Người viết"));

        Assert.Throws<InvalidOperationException>(() =>
            signoffs.Sign(versionId, "writer", MedDataStoreSeed.AdminUserId, "admin", "Người viết", "data:image/png;base64,AAAA"));

        signoffs.Sign(versionId, "writer", MedDataStoreSeed.AdminUserId, "admin", "Người viết", ValidSignature);
        var version = store.ProcedureVersions.Single(item => item.ProcedureVersionId == versionId);
        store.UpdateProcedureVersion(version with { StatusCode = "pending_approval" });

        Assert.Throws<InvalidOperationException>(() =>
            signoffs.Sign(versionId, "approver", MedDataStoreSeed.BacSiNoiId, "bacsi.noi", "Người phê duyệt", ValidSignature));

        Assert.Throws<InvalidOperationException>(() =>
            signoffs.Sign(versionId, "checker", MedDataStoreSeed.AdminUserId, "admin", "Người kiểm tra", ValidSignature));

        signoffs.Sign(versionId, "checker", MedDataStoreSeed.TruongKhoaNoiId, "truongkhoa.noi", "Người kiểm tra", ValidSignature);
        var approval = signoffs.Sign(versionId, "approver", MedDataStoreSeed.BacSiNoiId, "bacsi.noi", "Người phê duyệt", ValidSignature);

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
            DisplayOrder = 1,
            SignerUsername = "nguyen.an",
            SignerFullName = "Điều dưỡng Nguyễn An",
            ContentHashSha256 = hash,
            SignatureImageDataUrl = ValidSignature
        });
        store.AddProcedureSignoffRecord(new ProcedureSignoffRecord
        {
            ProcedureVersionId = versionId,
            SignoffRole = "checker",
            DisplayOrder = 2,
            SignerUsername = "le.binh",
            SignerFullName = "BS. Lê Bình",
            ContentHashSha256 = hash
        });
        store.AddProcedureSignoffRecord(new ProcedureSignoffRecord
        {
            ProcedureVersionId = versionId,
            SignoffRole = "approver",
            DisplayOrder = 3,
            SignerUsername = "pham.chau",
            SignerFullName = "TS.BS. Phạm Châu",
            ContentHashSha256 = hash
        });
        store.AddProcedureSignoffRecord(new ProcedureSignoffRecord
        {
            ProcedureVersionId = versionId,
            SignoffRole = "writer",
            DisplayOrder = 1,
            SignerUsername = "old.writer",
            SignerFullName = "Người viết bản cũ",
            ContentHashSha256 = "stale-hash"
        });
        Assert.Equal(4, store.ProcedureSignoffRecords.Count(item => item.ProcedureVersionId == versionId));
        var html = new ProcedureDocumentExportService(snapshots)
            .BuildProcedureDocumentHtml(versionId, new DateTime(2026, 6, 13, 8, 0, 0, DateTimeKind.Utc));
        var visibleText = WebUtility.HtmlDecode(html);

        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("shape-decision", html);
        Assert.Contains("source.pdf", html);
        Assert.DoesNotContain("ABC123", html);
        Assert.Contains(store.Departments.First(item => item.DepartmentId == MedDataStoreSeed.DeptNoiId).Name, html);
        Assert.Contains("BỆNH VIỆN UNG BƯỚU", html);
        Assert.Contains("In / Lưu PDF", html);
        Assert.Contains("Điều dưỡng Nguyễn An", visibleText);
        Assert.Contains("BS. Lê Bình", visibleText);
        Assert.Contains("TS.BS. Phạm Châu", visibleText);
        Assert.Contains("Phân công và thẩm quyền xác nhận", visibleText);
        Assert.Contains("Soạn thảo và chịu trách nhiệm nội dung", visibleText);
        Assert.Contains("Còn hiệu lực", visibleText);
        Assert.Contains("Hết hiệu lực", visibleText);
        Assert.Contains("Tài khoản: nguyen.an", visibleText);
        Assert.DoesNotContain(hash, visibleText);
        Assert.Contains("Mã kiểm soát", visibleText);
        Assert.Contains("Tài liệu nguồn được lưu trong hồ sơ kiểm soát", visibleText);
        Assert.Contains(ValidSignature, html);
        Assert.Contains("<th>Trách nhiệm</th><th>Các bước thực hiện</th><th>Mô tả / Các biểu mẫu</th>", html);
        Assert.Contains("flow-symbol shape-terminator", html);
        Assert.Contains("Bước kiểm soát 1", visibleText);
        Assert.Contains("BM.KSNK.01", html);
        Assert.Contains("LƯU ĐỒ QUY TRÌNH <span class=\"continuation\">(1/1)</span>", html);
        Assert.DoesNotContain("OCR_PENDING", html);
        Assert.InRange(Count(html, "<section class=\"page\">"), 5, 8);
        Assert.Contains("Trang 1 /", html);
        Assert.Contains("height:297mm", html);
        Assert.Contains("<small>VIII · Đối chiếu hồ sơ và điều kiện thực hiện</small>", visibleText);
    }

    [Fact]
    public void Export_ShortRomanSections_FlowContinuouslyOnSameA4Page()
    {
        var (store, versionId) = CreateCompleteDocument();

        var html = new ProcedureDocumentExportService(new ProcedureDocumentSnapshotService(store))
            .BuildProcedureDocumentHtml(versionId, new DateTime(2026, 6, 15, 8, 0, 0, DateTimeKind.Utc));
        var visibleHtml = WebUtility.HtmlDecode(html);

        var purposeIndex = visibleHtml.IndexOf("I. Mục đích", StringComparison.Ordinal);
        var scopeIndex = visibleHtml.IndexOf("II. Phạm vi", StringComparison.Ordinal);
        Assert.True(purposeIndex >= 0 && scopeIndex > purposeIndex);
        Assert.Equal(
            visibleHtml.LastIndexOf("<section class=\"page\">", purposeIndex, StringComparison.Ordinal),
            visibleHtml.LastIndexOf("<section class=\"page\">", scopeIndex, StringComparison.Ordinal));
        Assert.Contains("section-stack", html);
        Assert.Contains("procedure-section", html);
    }

    [Fact]
    public void Export_LongRomanSection_SplitsIntoContinuationPages()
    {
        var (store, versionId) = CreateCompleteDocument();
        var purpose = store.ProcedureDocumentSections.First(item =>
            item.ProcedureVersionId == versionId && item.SectionNumber == "I");
        var manyLines = string.Join('\n', Enumerable.Range(1, 120)
            .Select(index => $"{index}. Nội dung kiểm soát chuyên môn và hồ sơ liên quan."));
        store.UpdateProcedureDocumentSection(purpose with { ContentText = manyLines });

        var html = new ProcedureDocumentExportService(new ProcedureDocumentSnapshotService(store))
            .BuildProcedureDocumentHtml(versionId, new DateTime(2026, 6, 15, 8, 0, 0, DateTimeKind.Utc));
        var visibleHtml = WebUtility.HtmlDecode(html);

        Assert.Contains("I. Mục đích <span class=\"continuation\">(1/", visibleHtml);
        Assert.True(Count(visibleHtml, "I. Mục đích <span class=\"continuation\">") >= 2);
        Assert.Contains("II. Phạm vi", visibleHtml);
    }

    [Fact]
    public void Export_LongFlowDescriptions_SplitAcrossA4Pages()
    {
        var longDescription = string.Join(' ', Enumerable.Repeat("Thực hiện thao tác chuyên môn, kiểm tra điều kiện và ghi nhận đầy đủ hồ sơ.", 25));
        var (store, versionId) = CreateCompleteDocument(stepDescription: longDescription);

        var html = new ProcedureDocumentExportService(new ProcedureDocumentSnapshotService(store))
            .BuildProcedureDocumentHtml(versionId, new DateTime(2026, 6, 15, 8, 0, 0, DateTimeKind.Utc));

        Assert.True(Count(html, "LƯU ĐỒ QUY TRÌNH") >= 2);
        Assert.Contains("Tiếp tục ở trang lưu đồ sau", html);
        Assert.Contains("Tiếp từ trang lưu đồ trước", html);
    }

    [Fact]
    public void Export_LongControlAndTraceabilityTables_CreateContinuationPages()
    {
        var (store, versionId) = CreateCompleteDocument();
        for (var index = 2; index <= 18; index++)
        {
            store.AddProcedureDistributionRecipient(new ProcedureDistributionRecipient
            {
                ProcedureVersionId = versionId,
                DisplayOrder = index,
                RecipientName = $"Khoa/phòng nhận bản kiểm soát {index:00}"
            });
            store.AddProcedureRevisionEntry(new ProcedureRevisionEntry
            {
                ProcedureVersionId = versionId,
                DisplayOrder = index,
                RevisionDate = new DateTime(2026, 6, 15),
                PageRef = $"Trang {index}",
                SectionRef = $"Mục {index}",
                Summary = $"Cập nhật nội dung kiểm soát hồ sơ và phân phối phiên bản {index:00}"
            });
            store.AddProcedureAttachment(new ProcedureAttachment
            {
                ProcedureVersionId = versionId,
                AttachmentType = "other",
                FileName = $"phu-luc-{index:00}.pdf",
                FileUri = $"test/phu-luc-{index:00}.pdf",
                MimeType = "application/pdf",
                FileSizeBytes = 2048,
                ChecksumSha256 = $"SHA{index:00}"
            });
            store.AddProcedureSignoffRecord(new ProcedureSignoffRecord
            {
                ProcedureVersionId = versionId,
                SignoffRole = index % 3 == 0 ? "approver" : index % 2 == 0 ? "checker" : "writer",
                DisplayOrder = index,
                SignerUsername = $"user{index:00}",
                SignerFullName = $"Người ký nội bộ {index:00}",
                ContentHashSha256 = $"stale-hash-{index:00}",
                SignatureImageDataUrl = ValidSignature
            });
        }

        var html = new ProcedureDocumentExportService(new ProcedureDocumentSnapshotService(store))
            .BuildProcedureDocumentHtml(versionId, new DateTime(2026, 6, 15, 8, 0, 0, DateTimeKind.Utc));
        var visibleHtml = WebUtility.HtmlDecode(html);

        Assert.Contains("NƠI NHẬN VÀ PHÂN PHỐI <span class=\"continuation\">", visibleHtml);
        Assert.Contains("THEO DÕI SỬA ĐỔI <span class=\"continuation\">", visibleHtml);
        Assert.Contains("TỆP GẮN KÈM <span class=\"continuation\">", visibleHtml);
        Assert.Contains("NHẬT KÝ CHỮ KÝ NỘI BỘ <span class=\"continuation\">", visibleHtml);
        Assert.Contains("Khoa/phòng nhận bản kiểm soát 18", visibleHtml);
        Assert.Contains("phu-luc-18.pdf", visibleHtml);
        Assert.Contains("Người ký nội bộ 18", visibleHtml);
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

    private static (MedDataStore Store, Guid VersionId) CreateCompleteDocument(
        string name = "Quy trình kiểm thử",
        string? stepDescription = null)
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
                Description = stepDescription ?? "Đối chiếu hồ sơ và điều kiện thực hiện",
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
