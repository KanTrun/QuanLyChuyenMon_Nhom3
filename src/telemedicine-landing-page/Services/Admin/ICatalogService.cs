using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin;

/// <summary>
/// In-memory catalog service for technical services (Kỹ thuật chuyên môn) and
/// their resource norms. Supports CSV import / export with a stable column
/// order so a round-trip preserves rows verbatim.
/// </summary>
public interface ICatalogService
{
    IReadOnlyList<TechnicalServiceRecord> Search(CatalogFilter filter);
    TechnicalServiceRecord? GetById(Guid id);
    TechnicalServiceRecord Create(TechnicalServiceRecord record);
    TechnicalServiceRecord Update(Guid id, TechnicalServiceRecord updated);
    void Archive(Guid id);
    void AddResourceNorm(Guid serviceId, ResourceNorm norm);
    void RemoveResourceNorm(Guid serviceId, string resourceCode);

    /// <summary>
    /// Parses a UTF-8 CSV (with optional BOM) and appends new technical services.
    /// Returns the number of rows imported.
    /// </summary>
    int ImportFromCsv(string csv);

    /// <summary>
    /// Serialises the current catalog as CSV, keeping the same column layout used
    /// by <see cref="ImportFromCsv"/> so a round-trip preserves the rows.
    /// </summary>
    string ExportToCsv();

    event Action? StateChanged;
}
