using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>Phần quản lý phác đồ lâm sàng và áp dụng phác đồ.</summary>
public sealed partial class MedDataStore
{
    public void AddClinicalProtocol(ClinicalProtocol protocol)
    {
        lock (_lock)
        {
            if (_clinicalProtocols.Any(p => p.ProtocolCode == protocol.ProtocolCode))
                throw MedDomainException.Constraint("UQ_clinical_protocols_code", 2627, $"Mã phác đồ '{protocol.ProtocolCode}' đã tồn tại.");
            _clinicalProtocols.Add(protocol);
            RaiseStateChanged();
        }
    }

    public void AddClinicalProtocolVersion(ClinicalProtocolVersion ver)
    {
        lock (_lock)
        {
            ValidateJson(ver.ContentJson, "content");
            _clinicalProtocolVersions.Add(ver);
            RaiseStateChanged();
        }
    }

    public void UpdateClinicalProtocolVersion(ClinicalProtocolVersion ver)
    {
        lock (_lock)
        {
            ValidateJson(ver.ContentJson, "content");
            var idx = _clinicalProtocolVersions.FindIndex(v => v.ClinicalProtocolVersionId == ver.ClinicalProtocolVersionId);
            if (idx < 0)
                throw MedDomainException.Constraint("PK_clinical_protocol_versions", 547, "Phiên bản phác đồ không tồn tại.");
            _clinicalProtocolVersions[idx] = ver;
            RaiseStateChanged();
        }
    }

    public void AddClinicalProtocolProcedure(ClinicalProtocolProcedure cpp)
    {
        lock (_lock)
        {
            _clinicalProtocolProcedures.Add(cpp);
            RaiseStateChanged();
        }
    }

    public void RemoveClinicalProtocolProcedure(Guid clinicalProtocolProcedureId)
    {
        lock (_lock)
        {
            var removed = _clinicalProtocolProcedures.RemoveAll(p => p.ClinicalProtocolProcedureId == clinicalProtocolProcedureId);
            if (removed == 0)
                throw MedDomainException.Constraint("PK_clinical_protocol_procedures", 547, "Liên kết phác đồ - quy trình không tồn tại.");
            RaiseStateChanged();
        }
    }

    public void AddProtocolApplicabilityRule(ProtocolApplicabilityRule rule)
    {
        lock (_lock)
        {
            ValidateJson(rule.RuleJson, "rule");
            _protocolRules.Add(rule);
            RaiseStateChanged();
        }
    }

    public void RemoveProtocolApplicabilityRule(Guid ruleId)
    {
        lock (_lock)
        {
            var removed = _protocolRules.RemoveAll(r => r.ProtocolApplicabilityRuleId == ruleId);
            if (removed == 0)
                throw MedDomainException.Constraint("PK_protocol_applicability_rules", 547, "Quy tắc áp dụng không tồn tại.");
            RaiseStateChanged();
        }
    }

    public void AddPatientProtocolApplication(PatientProtocolApplication app)
    {
        lock (_lock)
        {
            ValidateJson(app.DecisionContextJson, "decision_context");
            _patientProtocolApps.Add(app);
            RaiseStateChanged();
        }
    }
}
