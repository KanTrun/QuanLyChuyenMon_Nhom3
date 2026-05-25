using System.Text.Json;
using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

public sealed record ClinicalProtocolSuggestion(
    ClinicalProtocol Protocol,
    ClinicalProtocolVersion Version,
    int Score,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> Warnings);

public interface IClinicalProtocolSuggestionService
{
    IReadOnlyList<ClinicalProtocolSuggestion> Suggest(Guid patientId, Guid? encounterId, string? diagnosisCode);
}

public sealed class ClinicalProtocolSuggestionService : IClinicalProtocolSuggestionService
{
    private readonly IMedDataStore _store;

    public ClinicalProtocolSuggestionService(IMedDataStore store)
    {
        _store = store;
    }

    public IReadOnlyList<ClinicalProtocolSuggestion> Suggest(Guid patientId, Guid? encounterId, string? diagnosisCode)
    {
        var patient = _store.PatientRefs.FirstOrDefault(p => p.PatientRefId == patientId);
        if (patient is null)
        {
            return Array.Empty<ClinicalProtocolSuggestion>();
        }

        var encounter = encounterId.HasValue
            ? _store.EncounterRefs.FirstOrDefault(e => e.EncounterRefId == encounterId.Value)
            : null;
        var normalizedIcd = NormalizeIcd(diagnosisCode);
        var activeVersions = _store.ClinicalProtocolVersions
            .Where(v => v.StatusCode == "active" &&
                (v.EffectiveFrom is null || v.EffectiveFrom <= DateTime.UtcNow) &&
                (v.EffectiveTo is null || v.EffectiveTo > DateTime.UtcNow))
            .ToList();

        return activeVersions
            .Select(v => EvaluateVersion(v, patient, encounter, normalizedIcd))
            .Where(s => s is not null)
            .Select(s => s!)
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.Protocol.Name)
            .ToList();
    }

    private ClinicalProtocolSuggestion? EvaluateVersion(
        ClinicalProtocolVersion version,
        PatientRef patient,
        EncounterRef? encounter,
        string? normalizedIcd)
    {
        var protocol = _store.ClinicalProtocols.FirstOrDefault(p =>
            p.ClinicalProtocolId == version.ClinicalProtocolId &&
            p.Status == "active");
        if (protocol is null)
        {
            return null;
        }

        var rules = _store.ProtocolApplicabilityRules
            .Where(r => r.ClinicalProtocolVersionId == version.ClinicalProtocolVersionId && r.IsActive)
            .OrderByDescending(r => r.Priority)
            .ToList();
        if (rules.Count == 0)
        {
            return new ClinicalProtocolSuggestion(protocol, version, 1, ["Phiên bản đang hiệu lực"], []);
        }

        var reasons = new List<string>();
        var warnings = new List<string>();
        var score = 0;
        foreach (var rule in rules)
        {
            var match = RuleMatches(rule, patient, encounter, normalizedIcd);
            if (rule.RuleType == "contraindication" && match)
            {
                warnings.Add($"Loại trừ bởi chống chỉ định: {FormatRule(rule)}");
                return null;
            }

            if (match)
            {
                score += Math.Max(1, rule.Priority);
                reasons.Add(FormatRule(rule));
            }
        }

        return score == 0 ? null : new ClinicalProtocolSuggestion(protocol, version, score, reasons, warnings);
    }

    private static bool RuleMatches(
        ProtocolApplicabilityRule rule,
        PatientRef patient,
        EncounterRef? encounter,
        string? normalizedIcd)
    {
        using var doc = JsonDocument.Parse(rule.RuleJson);
        var root = doc.RootElement;
        var hasRecognizedCriteria = false;
        if (root.TryGetProperty("icd", out var icd))
        {
            hasRecognizedCriteria = true;
            if (!IcdMatches(normalizedIcd, icd.GetString(), null))
                return false;
        }

        var hasIcdFrom = root.TryGetProperty("icdFrom", out var from);
        var hasIcdTo = root.TryGetProperty("icdTo", out var to);
        if (hasIcdFrom || hasIcdTo)
        {
            hasRecognizedCriteria = true;
            if (!IcdMatches(normalizedIcd, hasIcdFrom ? from.GetString() : to.GetString(), hasIcdTo ? to.GetString() : null))
                return false;
        }

        if (root.TryGetProperty("gender", out var gender))
        {
            hasRecognizedCriteria = true;
            if (!string.Equals(patient.GenderCode, gender.GetString(), StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (root.TryGetProperty("departmentId", out var deptId) && Guid.TryParse(deptId.GetString(), out var parsedDept))
        {
            hasRecognizedCriteria = true;
            if (encounter?.DepartmentId != parsedDept)
                return false;
        }

        var hasAgeFrom = root.TryGetProperty("ageFrom", out var ageFrom);
        var hasAgeTo = root.TryGetProperty("ageTo", out var ageTo);
        if (hasAgeFrom || hasAgeTo)
        {
            hasRecognizedCriteria = true;
            if (!AgeMatches(patient.BirthDate, hasAgeFrom ? ageFrom : default, hasAgeTo ? ageTo : default))
                return false;
        }

        if (root.TryGetProperty("contraindication", out var contraindication))
        {
            hasRecognizedCriteria = true;
            if (!string.Equals(normalizedIcd, NormalizeIcd(contraindication.GetString()), StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return hasRecognizedCriteria;
    }

    private static bool IcdMatches(string? actual, string? from, string? to)
    {
        if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(from)) return false;
        var normalizedFrom = NormalizeIcd(from);
        if (string.IsNullOrWhiteSpace(to))
            return string.Equals(actual, normalizedFrom, StringComparison.OrdinalIgnoreCase);
        return string.CompareOrdinal(actual, normalizedFrom) >= 0 &&
               string.CompareOrdinal(actual, NormalizeIcd(to)) <= 0;
    }

    private static bool AgeMatches(DateOnly? birthDate, JsonElement from, JsonElement to)
    {
        if (!birthDate.HasValue) return false;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - birthDate.Value.Year;
        if (birthDate.Value.AddYears(age) > today) age--;
        return (!TryGetJsonInt(from, out var min) || age >= min) &&
               (!TryGetJsonInt(to, out var max) || age <= max);
    }

    private static bool TryGetJsonInt(JsonElement element, out int value)
    {
        value = 0;
        if (element.ValueKind == JsonValueKind.Number)
        {
            return element.TryGetInt32(out value);
        }

        return element.ValueKind == JsonValueKind.String &&
               int.TryParse(element.GetString(), out value);
    }

    private static string? NormalizeIcd(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : new string(value.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static string FormatRule(ProtocolApplicabilityRule rule) => $"{rule.RuleType}: {rule.RuleJson}";
}
