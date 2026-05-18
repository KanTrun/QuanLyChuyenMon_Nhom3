using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>Phần quản lý bệnh nhân và lượt khám tham chiếu.</summary>
public sealed partial class MedDataStore
{
    public void AddPatientRef(PatientRef patient)
    {
        lock (_lock)
        {
            _patientRefs.Add(patient);
            RaiseStateChanged();
        }
    }

    public void AddEncounterRef(EncounterRef encounter)
    {
        lock (_lock)
        {
            if (!_patientRefs.Any(p => p.PatientRefId == encounter.PatientRefId))
                throw MedDomainException.Constraint("FK_encounter_refs_patient", 547, "Bệnh nhân tham chiếu không tồn tại.");
            _encounterRefs.Add(encounter);
            RaiseStateChanged();
        }
    }
}
