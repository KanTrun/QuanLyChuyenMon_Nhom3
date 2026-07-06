using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class ProcedureWorkflowGuardTests : IDisposable
{
    private readonly MedDbContext _db;
    private readonly Guid _writer2Id = Guid.Parse("f0000000-0000-0000-0000-000000000099");

    public ProcedureWorkflowGuardTests()
    {
        _db = TestDbHelper.CreateSeededContext();
        _db.Users.Add(new AppUser
        {
            UserId = _writer2Id,
            Username = "tiensi.writer2",
            FullName = "Tiến sĩ Writer 2",
            Status = "active",
            PrimaryDepartmentId = MedDataStoreSeed.DeptNoiId
        });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void AssignedWriter_CanSignAndEditDraft_WithoutGlobalUpdatePermission()
    {
        var store = new MedDbDataStore(_db);
        var snapshots = new ProcedureDocumentSnapshotService(store);
        var signoffs = new ProcedureSignoffService(store, snapshots);
        var authoring = new ProcedureAuthoringService(store, snapshots);
        var created = authoring.CreateVersion(CreateTwoWriterCommand(_writer2Id));

        signoffs.Sign(
            created.Version.ProcedureVersionId,
            "writer",
            MedDataStoreSeed.AdminUserId,
            "admin",
            "Quản trị viên",
            ValidSignature);

        var context = new CurrentUserContext(_db, new EffectivePermissionResolver(_db));
        context.SetCurrentUser(_writer2Id);
        var workflowGuard = CreateWorkflowGuard(store, context, signoffs);

        Assert.False(context.HasPermission("SCR_PROCEDURES:UPDATE"));
        Assert.True(signoffs.CanUserSign(created.Version.ProcedureVersionId, "writer", _writer2Id, out _));
        Assert.True(workflowGuard.CanSign(created.Version.ProcedureVersionId, "writer", _writer2Id));
        Assert.True(workflowGuard.CanEditDraft(created.Version.ProcedureVersionId, _writer2Id));
        Assert.True(workflowGuard.CanCreateOrUpdate(
            isUpdate: true,
            created.Version.ProcedureVersionId,
            _writer2Id,
            "chỉnh sửa bản nháp"));
    }

    [Fact]
    public void NonAssignedUser_StillRequiresGlobalUpdatePermission()
    {
        var store = new MedDbDataStore(_db);
        var snapshots = new ProcedureDocumentSnapshotService(store);
        var signoffs = new ProcedureSignoffService(store, snapshots);
        var authoring = new ProcedureAuthoringService(store, snapshots);
        var created = authoring.CreateVersion(CreateTwoWriterCommand(_writer2Id));

        var outsiderId = Guid.Parse("f0000000-0000-0000-0000-000000000098");
        _db.Users.Add(new AppUser
        {
            UserId = outsiderId,
            Username = "outsider",
            FullName = "Người ngoài",
            Status = "active",
            PrimaryDepartmentId = MedDataStoreSeed.DeptNoiId
        });
        _db.SaveChanges();

        var context = new CurrentUserContext(_db, new EffectivePermissionResolver(_db));
        context.SetCurrentUser(outsiderId);
        var workflowGuard = CreateWorkflowGuard(store, context, signoffs);

        Assert.False(signoffs.CanUserSign(created.Version.ProcedureVersionId, "writer", outsiderId, out _));
        Assert.False(workflowGuard.CanSign(created.Version.ProcedureVersionId, "writer", outsiderId));
    }

    private ProcedureWorkflowGuard CreateWorkflowGuard(
        IMedDataStore store,
        CurrentUserContext context,
        ProcedureSignoffService signoffs)
    {
        var runtimeGuard = new ProcedureRuntimeGuard(store, context, new AuditTrailService(_db));
        var actionGuard = new AdminActionGuard(context, new ToastService(), runtimeGuard);
        return new ProcedureWorkflowGuard(actionGuard, signoffs);
    }

    private static ProcedureAuthoringCommand CreateTwoWriterCommand(Guid secondWriterId)
        => new(
            Guid.NewGuid(),
            null,
            null,
            "QT.WORKFLOW.GUARD",
            "Quy trình kiểm thử workflow guard",
            "technical",
            MedDataStoreSeed.DeptNoiId,
            "Mô tả",
            "Khởi tạo",
            new DateTime(2026, 7, 6),
            1,
            MedDataStoreSeed.AdminUserId,
            [
                new ProcedureWriterAssignmentDraft { UserId = MedDataStoreSeed.AdminUserId.ToString() },
                new ProcedureWriterAssignmentDraft { UserId = secondWriterId.ToString() }
            ],
            [new ProcedureSectionDraft
            {
                Order = 1,
                Number = "I",
                Title = "Mục đích",
                Kind = "purpose",
                Content = "Nội dung v01"
            }],
            [new ProcedureRecipientDraft { Name = "Khoa Nội" }],
            [new ProcedureRevisionDraft { Summary = "Ban hành v01" }],
            [new ProcedureFlowStepDraft
            {
                Code = "BUOC-01",
                Name = "Thực hiện",
                Responsibility = "Điều dưỡng",
                Description = "Thực hiện đúng hướng dẫn"
            }],
            [new ProcedureStoredAttachmentDraft(Guid.NewGuid(), "source_pdf", "source.pdf", "test/source.pdf", "application/pdf", 1024, "ABC123")]);

    private const string ValidSignature =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
}
