using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class ProcedureVersionDisplaySelectorTests
{
    [Fact]
    public void Select_StatusFilterShowsMatchingVersionInsteadOfNewestDraft()
    {
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, MedDataStoreSeed.DeptNoiId);
        var active = CreateVersion(procedureId, 1, "active", MedDataStoreSeed.DeptNoiId);
        var draft = CreateVersion(procedureId, 2, "draft", MedDataStoreSeed.DeptNoiId);

        var selection = ProcedureVersionDisplaySelector.Select(
            procedure,
            [active, draft],
            "active",
            departmentFilter: null);

        Assert.True(selection.MatchesFilters);
        Assert.Equal(active.ProcedureVersionId, selection.Version?.ProcedureVersionId);
        Assert.Equal(MedDataStoreSeed.DeptNoiId, selection.DepartmentId);
    }

    [Fact]
    public void Select_NoFiltersShowsNewestVersion()
    {
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, MedDataStoreSeed.DeptNoiId);
        var active = CreateVersion(procedureId, 1, "active", MedDataStoreSeed.DeptNoiId);
        var draft = CreateVersion(procedureId, 2, "draft", MedDataStoreSeed.DeptNoiId);

        var selection = ProcedureVersionDisplaySelector.Select(
            procedure,
            [active, draft],
            statusFilter: null,
            departmentFilter: null);

        Assert.True(selection.MatchesFilters);
        Assert.Equal(draft.ProcedureVersionId, selection.Version?.ProcedureVersionId);
    }

    [Fact]
    public void Select_ProcedureWithoutVersionsMatchesOwnerDepartmentFilter()
    {
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, MedDataStoreSeed.DeptNoiId);

        var selection = ProcedureVersionDisplaySelector.Select(
            procedure,
            [],
            statusFilter: null,
            MedDataStoreSeed.DeptNoiId);

        Assert.True(selection.MatchesFilters);
        Assert.Null(selection.Version);
        Assert.Equal(MedDataStoreSeed.DeptNoiId, selection.DepartmentId);
    }

    [Fact]
    public void Select_DepartmentFilterShowsNewestVersionInThatDepartment()
    {
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, MedDataStoreSeed.DeptNoiId);
        var noiVersion = CreateVersion(procedureId, 1, "active", MedDataStoreSeed.DeptNoiId);
        var ngoaiVersion = CreateVersion(procedureId, 2, "draft", MedDataStoreSeed.DeptNgoaiId);

        var selection = ProcedureVersionDisplaySelector.Select(
            procedure,
            [noiVersion, ngoaiVersion],
            statusFilter: null,
            MedDataStoreSeed.DeptNoiId);

        Assert.True(selection.MatchesFilters);
        Assert.Equal(noiVersion.ProcedureVersionId, selection.Version?.ProcedureVersionId);
        Assert.Equal(MedDataStoreSeed.DeptNoiId, selection.DepartmentId);
    }

    [Fact]
    public void Select_StatusAndDepartmentFiltersRequireSameVersionToMatch()
    {
        var procedureId = Guid.NewGuid();
        var procedure = CreateProcedure(procedureId, MedDataStoreSeed.DeptNoiId);
        var activeNoi = CreateVersion(procedureId, 1, "active", MedDataStoreSeed.DeptNoiId);
        var draftNgoai = CreateVersion(procedureId, 2, "draft", MedDataStoreSeed.DeptNgoaiId);

        var selection = ProcedureVersionDisplaySelector.Select(
            procedure,
            [activeNoi, draftNgoai],
            "active",
            MedDataStoreSeed.DeptNgoaiId);

        Assert.False(selection.MatchesFilters);
        Assert.Null(selection.Version);
    }

    private static ProfessionalProcedure CreateProcedure(Guid procedureId, Guid departmentId)
        => new()
        {
            ProcedureId = procedureId,
            ProcedureCode = "QT-TEST",
            Name = "Quy trinh test",
            ProcedureType = "technical",
            OwnerDepartmentId = departmentId
        };

    private static ProcedureVersion CreateVersion(Guid procedureId, int versionNo, string status, Guid departmentId)
        => new()
        {
            ProcedureId = procedureId,
            VersionNo = versionNo,
            VersionLabel = $"v{versionNo}.0",
            StatusCode = status,
            DepartmentId = departmentId,
            Title = $"Version {versionNo}",
            CreatedAt = new DateTime(2026, 6, versionNo, 0, 0, 0, DateTimeKind.Utc)
        };
}
