using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin;

/// <summary>In-memory clinical protocol service (Phác đồ điều trị).</summary>
public interface IProtocolService
{
    IReadOnlyList<ClinicalProtocolRecord> Search(string? query = null, ProtocolType? type = null, Department? specialty = null);
    ClinicalProtocolRecord? GetById(Guid id);
    ClinicalProtocolRecord Create(ClinicalProtocolRecord record);
    ClinicalProtocolRecord Update(Guid id, ClinicalProtocolRecord updated);
    void Archive(Guid id);
    ProtocolApplication RecordPatientApplication(Guid protocolId, string patientName, string outcome);
    IReadOnlyList<ProtocolApplication> GetApplications(Guid protocolId);

    event Action? StateChanged;
}
