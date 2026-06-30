using TelemedicineLandingPage.Application.Workflow;
using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class ProcedureDocumentLifecycleTests : IDisposable
{
    private const string ValidSignature = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";
    private readonly MedDbContext _db = TestDbHelper.CreateSeededContext();

    [Fact]
    public void Submit_RequiresCurrentWriterSignoff()
    {
        var (versionId, lifecycle, signoffs) = CreateCompleteDocument();

        var exception = Assert.Throws<MedDomainException>(() =>
            lifecycle.Submit(versionId, MedDataStoreSeed.AdminUserId));
        Assert.Equal(50027, exception.SqlErrorNumber);

        signoffs.Sign(versionId, "writer", MedDataStoreSeed.AdminUserId, "admin", "Quản trị viên", ValidSignature);
        lifecycle.Submit(versionId, MedDataStoreSeed.AdminUserId);

        Assert.Equal("pending_approval", _db.ProcedureVersions.Single(item => item.ProcedureVersionId == versionId).StatusCode);
    }

    [Fact]
    public void Publish_RequiresWriterCheckerAndApproverOnCurrentHash()
    {
        var (versionId, lifecycle, signoffs) = CreateCompleteDocument();
        signoffs.Sign(versionId, "writer", MedDataStoreSeed.AdminUserId, "admin", "Người soạn", ValidSignature);
        lifecycle.Submit(versionId, MedDataStoreSeed.AdminUserId);

        var exception = Assert.Throws<MedDomainException>(() =>
            lifecycle.Publish(versionId, MedDataStoreSeed.BacSiNoiId));
        Assert.Equal(50028, exception.SqlErrorNumber);

        signoffs.Sign(versionId, "checker", MedDataStoreSeed.TruongKhoaNoiId, "truongkhoa.noi", "Trưởng khoa Nội", ValidSignature);
        signoffs.Sign(versionId, "approver", MedDataStoreSeed.BacSiNoiId, "bacsi.noi", "Bác sĩ Nội", ValidSignature);
        lifecycle.Publish(versionId, MedDataStoreSeed.BacSiNoiId);

        Assert.Equal("active", _db.ProcedureVersions.Single(item => item.ProcedureVersionId == versionId).StatusCode);
    }

    [Fact]
    public void Sign_CheckerCannotBeSameUserAsWriter()
    {
        var (versionId, lifecycle, signoffs) = CreateCompleteDocument();
        signoffs.Sign(versionId, "writer", MedDataStoreSeed.AdminUserId, "admin", "Người soạn", ValidSignature);
        lifecycle.Submit(versionId, MedDataStoreSeed.AdminUserId);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            signoffs.Sign(versionId, "checker", MedDataStoreSeed.AdminUserId, "admin", "Người kiểm tra", ValidSignature));
        Assert.Contains("khác người viết", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Sign_ApproverCannotBeSameUserAsWriterOrChecker()
    {
        var (versionId, lifecycle, signoffs) = CreateCompleteDocument();
        signoffs.Sign(versionId, "writer", MedDataStoreSeed.AdminUserId, "admin", "Người soạn", ValidSignature);
        lifecycle.Submit(versionId, MedDataStoreSeed.AdminUserId);
        signoffs.Sign(versionId, "checker", MedDataStoreSeed.TruongKhoaNoiId, "truongkhoa.noi", "Trưởng khoa Nội", ValidSignature);

        var writerConflict = Assert.Throws<InvalidOperationException>(() =>
            signoffs.Sign(versionId, "approver", MedDataStoreSeed.AdminUserId, "admin", "Người phê duyệt", ValidSignature));
        Assert.Contains("khác người viết", writerConflict.Message, StringComparison.Ordinal);

        var checkerConflict = Assert.Throws<InvalidOperationException>(() =>
            signoffs.Sign(versionId, "approver", MedDataStoreSeed.TruongKhoaNoiId, "truongkhoa.noi", "Trưởng khoa Nội", ValidSignature));
        Assert.Contains("khác người kiểm tra", checkerConflict.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CanUserSign_HidesCheckerAndExposesApproverAfterCheckerCompleted()
    {
        var (versionId, lifecycle, signoffs) = CreateCompleteDocument();
        signoffs.Sign(versionId, "writer", MedDataStoreSeed.AdminUserId, "admin", "Người soạn", ValidSignature);
        lifecycle.Submit(versionId, MedDataStoreSeed.AdminUserId);
        signoffs.Sign(versionId, "checker", MedDataStoreSeed.TruongKhoaNoiId, "truongkhoa.noi", "Trưởng khoa Nội", ValidSignature);

        var canFourthUserSignChecker = signoffs.CanUserSign(versionId, "checker", MedDataStoreSeed.BacSiNoiId, out var checkerReason);
        var canFourthUserSignApprover = signoffs.CanUserSign(versionId, "approver", MedDataStoreSeed.BacSiNoiId, out var approverReason);

        Assert.False(canFourthUserSignChecker);
        Assert.Contains("đã được xác nhận", checkerReason, StringComparison.Ordinal);
        Assert.True(canFourthUserSignApprover);
        Assert.Null(approverReason);
    }

    public void Dispose() => _db.Dispose();

    private (Guid VersionId, ProcedureLifecycleService Lifecycle, ProcedureSignoffService Signoffs) CreateCompleteDocument()
    {
        var procedure = new ProfessionalProcedure
        {
            ProcedureCode = $"QT.LIFE.{Guid.NewGuid():N}"[..22],
            Name = "Quy trình vòng đời đầy đủ",
            ProcedureType = "technical",
            OwnerDepartmentId = MedDataStoreSeed.DeptNoiId,
            CreatedBy = MedDataStoreSeed.AdminUserId
        };
        var version = new ProcedureVersion
        {
            ProcedureId = procedure.ProcedureId,
            VersionNo = 1,
            VersionLabel = "v1.0",
            Title = procedure.Name,
            Summary = "{\"note\":\"test\"}",
            IssueDate = new DateTime(2026, 6, 13),
            IssueNumber = 1,
            SourcePdfFileName = "source.pdf",
            SourcePdfChecksumSha256 = "ABC123",
            CreatedBy = MedDataStoreSeed.AdminUserId
        };
        _db.Procedures.Add(procedure);
        _db.ProcedureVersions.Add(version);
        _db.ProcedureVersionAuthorAssignments.Add(new ProcedureVersionAuthorAssignment
        {
            ProcedureVersionId = version.ProcedureVersionId,
            DisplayOrder = 1,
            AssignedUserId = MedDataStoreSeed.AdminUserId,
            AssignedUsername = "admin",
            AssignedFullName = "Quản trị viên"
        });

        var kinds = new[] { "purpose", "scope", "basis", "definitions", "responsibilities", "procedure", "flowchart", "records", "appendices" };
        for (var index = 0; index < kinds.Length; index++)
            _db.ProcedureDocumentSections.Add(new ProcedureDocumentSection
            {
                ProcedureVersionId = version.ProcedureVersionId,
                SectionOrder = index + 1,
                SectionNumber = (index + 1).ToString(),
                Title = kinds[index],
                SectionKind = kinds[index],
                ContentText = "Nội dung đầy đủ"
            });

        _db.ProcedureDistributionRecipients.Add(new ProcedureDistributionRecipient
        {
            ProcedureVersionId = version.ProcedureVersionId,
            DisplayOrder = 1,
            RecipientName = "Khoa Nội"
        });
        _db.ProcedureRevisionEntries.Add(new ProcedureRevisionEntry
        {
            ProcedureVersionId = version.ProcedureVersionId,
            DisplayOrder = 1,
            Summary = "Ban hành lần đầu"
        });
        _db.ProcedureSteps.Add(new ProcedureStep
        {
            ProcedureVersionId = version.ProcedureVersionId,
            StepNo = 1,
            Name = "Thực hiện",
            Description = "Thực hiện đúng nội dung",
            ResponsibilityText = "Điều dưỡng",
            FlowShapeCode = "process"
        });
        _db.ProcedureAttachments.Add(new ProcedureAttachment
        {
            ProcedureVersionId = version.ProcedureVersionId,
            AttachmentType = "source_pdf",
            FileName = "source.pdf",
            FileUri = "test/source.pdf",
            ChecksumSha256 = "ABC123"
        });
        _db.SaveChanges();

        var store = new MedDbDataStore(_db);
        var snapshots = new ProcedureDocumentSnapshotService(store);
        var signoffs = new ProcedureSignoffService(store, snapshots);
        var audit = new AuditTrailService(_db);
        var lifecycle = new ProcedureLifecycleService(_db, audit, new ProcedureVersionWorkflowGuard(audit), snapshots);
        return (version.ProcedureVersionId, lifecycle, signoffs);
    }
}
