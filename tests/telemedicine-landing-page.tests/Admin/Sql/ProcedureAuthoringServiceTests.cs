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
        Assert.Equal(2, result.Version.RequiredWriterSignatures);
        Assert.Single(store.ProcedureDocumentSections, item => item.ProcedureVersionId == result.Version.ProcedureVersionId);
        Assert.Equal(2, store.ProcedureVersionAuthorAssignments.Count(item => item.ProcedureVersionId == result.Version.ProcedureVersionId));
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

    [Fact]
    public void CreateVersion_ThenSign_RecordsCurrentWriterSignoff()
    {
        var store = new MedDataStore();
        var snapshots = new ProcedureDocumentSnapshotService(store);
        var service = new ProcedureAuthoringService(store, snapshots);
        var signoffs = new ProcedureSignoffService(store, snapshots);
        var created = service.CreateVersion(CreateCommand());

        signoffs.Sign(
            created.Version.ProcedureVersionId,
            "writer",
            MedDataStoreSeed.AdminUserId,
            "admin",
            "Quản trị viên",
            ValidSignature);
        snapshots.PersistSnapshot(created.Version.ProcedureVersionId, "draft_signed", MedDataStoreSeed.AdminUserId);

        var snapshot = snapshots.GetSnapshot(created.Version.ProcedureVersionId);
        var writerSignoffs = snapshots.GetCurrentSignoffs(snapshot, "writer");
        Assert.Contains(writerSignoffs, item => item.SignerUserId == MedDataStoreSeed.AdminUserId);
        Assert.Equal(1, signoffs.GetOutstandingWriterSignatures(created.Version.ProcedureVersionId));
        Assert.False(signoffs.CanUserSign(created.Version.ProcedureVersionId, "writer", MedDataStoreSeed.AdminUserId, out _));
    }

    [Fact]
    public void UpdateDraft_AllowsSecondWriterToContinueEditingBeforeSigning()
    {
        var store = new MedDataStore();
        var snapshots = new ProcedureDocumentSnapshotService(store);
        var service = new ProcedureAuthoringService(store, snapshots);
        var signoffs = new ProcedureSignoffService(store, snapshots);
        var created = service.CreateVersion(CreateCommand());
        signoffs.Sign(
            created.Version.ProcedureVersionId,
            "writer",
            MedDataStoreSeed.AdminUserId,
            "admin",
            "Quản trị viên",
            ValidSignature);

        Assert.False(signoffs.CanUserEditDraft(created.Version.ProcedureVersionId, MedDataStoreSeed.AdminUserId, out _));
        Assert.True(signoffs.CanUserEditDraft(created.Version.ProcedureVersionId, MedDataStoreSeed.TruongKhoaNoiId, out _));

        var updated = service.UpdateDraft(CreateCommand(
            created.Procedure.ProcedureId,
            versionId: created.Version.ProcedureVersionId,
            content: "Nội dung v02 do người viết thứ hai"));

        Assert.Equal("Nội dung v02 do người viết thứ hai",
            store.ProcedureDocumentSections.Single(item => item.ProcedureVersionId == updated.Version.ProcedureVersionId).ContentText);
        Assert.False(signoffs.HasCurrentSignoff(updated.Version.ProcedureVersionId, "writer"));
    }

    [Fact]
    public void UpdateDraft_SecondWriterCanSaveAndSignAfterFirstWriter()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var store = new MedDbDataStore(db);
        var snapshots = new ProcedureDocumentSnapshotService(store);
        var audit = new AuditTrailService(db);
        var service = new ProcedureAuthoringService(store, snapshots);
        var signoffs = new ProcedureSignoffService(store, snapshots, audit);
        var created = service.CreateVersion(CreateCommand());
        signoffs.Sign(
            created.Version.ProcedureVersionId,
            "writer",
            MedDataStoreSeed.AdminUserId,
            "admin",
            "Quản trị viên",
            ValidSignature);

        var updated = service.UpdateDraft(
            CreateCommand(
                created.Procedure.ProcedureId,
                versionId: created.Version.ProcedureVersionId,
                content: "Nội dung do người viết thứ hai chỉnh sửa"),
            persistSnapshot: false);

        ProcedureSignoffRecord? secondSignoff = null;
        store.RunProcedureWriteBatch(() =>
        {
            secondSignoff = signoffs.Sign(
                updated.Version.ProcedureVersionId,
                "writer",
                MedDataStoreSeed.TruongKhoaNoiId,
                "truongkhoa.noi",
                "Trưởng khoa Nội",
                ValidSignature);
            snapshots.PersistSnapshot(updated.Version.ProcedureVersionId, "draft_signed", MedDataStoreSeed.TruongKhoaNoiId);
        });
        if (secondSignoff is not null)
            signoffs.RecordSignoffAudit(updated.Version.ProcedureVersionId, secondSignoff);

        Assert.Equal(1, signoffs.GetOutstandingWriterSignatures(updated.Version.ProcedureVersionId));
        Assert.Contains(
            store.ProcedureSignoffRecords,
            item => item.ProcedureVersionId == updated.Version.ProcedureVersionId
                && item.SignerUserId == MedDataStoreSeed.TruongKhoaNoiId);
    }

    [Fact]
    public void UpdateDraft_InWriteBatch_PersistsStepRoleAndLocationAssignments()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var store = new MedDbDataStore(db);
        var snapshots = new ProcedureDocumentSnapshotService(store);
        var service = new ProcedureAuthoringService(store, snapshots);
        var created = service.CreateVersion(CreateCommand(steps:
        [
            new ProcedureFlowStepDraft
            {
                Code = "BUOC-01",
                Name = "Bắt đầu",
                Responsibility = "KAN2",
                Description = "KAN2",
                RoleIds = [MedDataStoreSeed.RoleSysAdminId.ToString(), MedDataStoreSeed.RoleClinicalId.ToString()],
                LocationDepartmentIds = [MedDataStoreSeed.DeptNoiId.ToString(), MedDataStoreSeed.DeptNgoaiId.ToString()]
            },
            new ProcedureFlowStepDraft
            {
                Code = "BUOC-02",
                Name = "Kết thúc",
                Responsibility = "KAN2",
                Description = "KAN2",
                RoleIds = [MedDataStoreSeed.RoleNurseId.ToString()],
                LocationDepartmentIds = [MedDataStoreSeed.DeptXetNghiemId.ToString()]
            }
        ]));

        ProcedureAuthoringResult? updated = null;
        store.RunProcedureWriteBatch(() =>
        {
            updated = service.UpdateDraft(
                CreateCommand(
                    created.Procedure.ProcedureId,
                    versionId: created.Version.ProcedureVersionId,
                    content: "Nội dung chỉnh sửa người viết thứ hai",
                    steps:
                    [
                        new ProcedureFlowStepDraft
                        {
                            Code = "BUOC-01",
                            Name = "Bắt đầu",
                            Responsibility = "KAN2",
                            Description = "KAN2 chỉnh sửa",
                            RoleIds = [MedDataStoreSeed.RoleSysAdminId.ToString(), MedDataStoreSeed.RoleClinicalId.ToString()],
                            LocationDepartmentIds = [MedDataStoreSeed.DeptNoiId.ToString(), MedDataStoreSeed.DeptNgoaiId.ToString()]
                        },
                        new ProcedureFlowStepDraft
                        {
                            Code = "BUOC-02",
                            Name = "Kết thúc",
                            Responsibility = "KAN2",
                            Description = "KAN2 chỉnh sửa",
                            RoleIds = [MedDataStoreSeed.RoleNurseId.ToString()],
                            LocationDepartmentIds = [MedDataStoreSeed.DeptXetNghiemId.ToString()]
                        }
                    ]),
                persistSnapshot: false);
        });

        Assert.NotNull(updated);
        var stepIds = store.ProcedureSteps
            .Where(item => item.ProcedureVersionId == updated!.Version.ProcedureVersionId)
            .Select(item => item.ProcedureStepId)
            .ToHashSet();
        Assert.Equal(2, stepIds.Count);
        Assert.Equal(3, store.ProcedureStepRoleAssignments.Count(item => stepIds.Contains(item.ProcedureStepId)));
        Assert.Equal(3, store.ProcedureStepLocationAssignments.Count(item => stepIds.Contains(item.ProcedureStepId)));
    }

    private const string ValidSignature = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    [Fact]
    public void CreateVersion_LinksStepToUploadedFormAttachment()
    {
        var store = new MedDataStore();
        var service = new ProcedureAuthoringService(store);
        var sourceClientId = Guid.NewGuid();
        var formClientId = Guid.NewGuid();
        var command = CreateCommand(
            attachments:
            [
                new ProcedureStoredAttachmentDraft(sourceClientId, "source_pdf", "source.pdf", "test/source.pdf", "application/pdf", 1024, "ABC123"),
                new ProcedureStoredAttachmentDraft(formClientId, "form", "phieu-theo-doi.pdf", "test/phieu.pdf", "application/pdf", 512, "DEF456")
            ],
            steps:
            [
                new ProcedureFlowStepDraft
                {
                    Code = "BUOC-01",
                    Name = "Thực hiện",
                    Responsibility = "Điều dưỡng",
                    Description = "Thực hiện đúng hướng dẫn",
                    LinkedAttachmentClientId = formClientId
                }
            ]);

        var result = service.CreateVersion(command);
        var step = store.ProcedureSteps.Single(item => item.ProcedureVersionId == result.Version.ProcedureVersionId);
        var formAttachment = store.ProcedureAttachments.Single(item =>
            item.ProcedureVersionId == result.Version.ProcedureVersionId && item.AttachmentType == "form");

        Assert.Equal(formAttachment.ProcedureAttachmentId, step.FormAttachmentId);
        Assert.Single(store.ProcedureStepAttachmentAssignments, item => item.ProcedureStepId == step.ProcedureStepId);
    }

    private static ProcedureAuthoringCommand CreateCommand(
        Guid? procedureId = null,
        Guid? sourceVersionId = null,
        Guid? versionId = null,
        string? content = null,
        IReadOnlyList<ProcedureStoredAttachmentDraft>? attachments = null,
        IReadOnlyList<ProcedureFlowStepDraft>? steps = null)
    {
        var isUpdate = procedureId.HasValue;
        var sourceClientId = Guid.NewGuid();
        return new ProcedureAuthoringCommand(
            versionId ?? Guid.NewGuid(),
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
            [
                new ProcedureWriterAssignmentDraft { UserId = MedDataStoreSeed.AdminUserId.ToString() },
                new ProcedureWriterAssignmentDraft { UserId = MedDataStoreSeed.TruongKhoaNoiId.ToString() }
            ],
            [new ProcedureSectionDraft
            {
                Order = 1,
                Number = "I",
                Title = "Mục đích",
                Kind = "purpose",
                Content = content ?? (isUpdate ? "Nội dung v02" : "Nội dung v01")
            }],
            [new ProcedureRecipientDraft { Name = "Khoa Nội" }],
            [new ProcedureRevisionDraft { Summary = isUpdate ? "Cập nhật v02" : "Ban hành v01" }],
            steps ??
            [
                new ProcedureFlowStepDraft
                {
                    Code = "BUOC-01",
                    Name = "Thực hiện",
                    Responsibility = "Điều dưỡng",
                    Description = "Thực hiện đúng hướng dẫn"
                }
            ],
            attachments ??
            [new ProcedureStoredAttachmentDraft(sourceClientId, "source_pdf", "source.pdf", "test/source.pdf", "application/pdf", 1024, "ABC123")]);
    }
}
