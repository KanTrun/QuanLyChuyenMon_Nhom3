namespace TelemedicineLandingPage.Models.Admin.Sql;

public sealed record ProcedureVersionDisplaySelection(
    ProcedureVersion? Version,
    Guid? DepartmentId,
    bool MatchesFilters);

public static class ProcedureVersionDisplaySelector
{
    public static ProcedureVersionDisplaySelection Select(
        ProfessionalProcedure procedure,
        IEnumerable<ProcedureVersion> versions,
        string? statusFilter,
        Guid? departmentFilter)
    {
        var versionList = versions.ToList();
        var hasStatusFilter = !string.IsNullOrWhiteSpace(statusFilter);
        var normalizedStatus = statusFilter?.Trim();

        var candidates = versionList.AsEnumerable();
        if (hasStatusFilter)
        {
            candidates = candidates.Where(version =>
                string.Equals(version.StatusCode, normalizedStatus, StringComparison.OrdinalIgnoreCase));
        }

        if (departmentFilter.HasValue)
        {
            candidates = candidates.Where(version =>
                GetEffectiveDepartmentId(procedure, version) == departmentFilter.Value);
        }

        var selectedVersion = (hasStatusFilter || departmentFilter.HasValue ? candidates : versionList)
            .OrderByDescending(version => version.VersionNo)
            .ThenByDescending(version => version.CreatedAt)
            .FirstOrDefault();

        if (selectedVersion is not null)
        {
            return new ProcedureVersionDisplaySelection(
                selectedVersion,
                GetEffectiveDepartmentId(procedure, selectedVersion),
                MatchesFilters: true);
        }

        if (versionList.Count == 0 &&
            !hasStatusFilter &&
            (!departmentFilter.HasValue || procedure.OwnerDepartmentId == departmentFilter.Value))
        {
            return new ProcedureVersionDisplaySelection(null, procedure.OwnerDepartmentId, MatchesFilters: true);
        }

        return new ProcedureVersionDisplaySelection(null, procedure.OwnerDepartmentId, MatchesFilters: false);
    }

    public static Guid? GetEffectiveDepartmentId(ProfessionalProcedure procedure, ProcedureVersion version)
        => version.DepartmentId ?? procedure.OwnerDepartmentId;
}
