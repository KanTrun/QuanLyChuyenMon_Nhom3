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

    public void UpdatePatientRef(PatientRef patient)
    {
        lock (_lock)
        {
            var idx = _patientRefs.FindIndex(p => p.PatientRefId == patient.PatientRefId);
            if (idx < 0)
                throw MedDomainException.Constraint("PK_patient_refs", 547, "Bệnh nhân không tồn tại.");
            _patientRefs[idx] = patient;
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

    public void UpdateEncounterRef(EncounterRef encounter)
    {
        lock (_lock)
        {
            if (!_patientRefs.Any(p => p.PatientRefId == encounter.PatientRefId))
                throw MedDomainException.Constraint("FK_encounter_refs_patient", 547, "Bệnh nhân tham chiếu không tồn tại.");
            ValidateDates(encounter.StartedAt, encounter.EndedAt, "CK_encounter_refs_dates");
            var idx = _encounterRefs.FindIndex(e => e.EncounterRefId == encounter.EncounterRefId);
            if (idx < 0)
                throw MedDomainException.Constraint("PK_encounter_refs", 547, "Lượt khám không tồn tại.");
            _encounterRefs[idx] = encounter;
            RaiseStateChanged();
        }
    }
}
