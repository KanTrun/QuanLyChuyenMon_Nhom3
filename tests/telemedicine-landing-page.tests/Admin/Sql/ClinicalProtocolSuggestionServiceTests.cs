using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class ClinicalProtocolSuggestionServiceTests
{
    [Fact]
    public void Suggest_IcdRangeMatch_ReturnsActiveProtocolVersion()
    {
        var store = new MedDataStore();
        var service = new ClinicalProtocolSuggestionService(store);

        var suggestions = service.Suggest(
            MedDataStoreSeed.PatientMauId,
            MedDataStoreSeed.EncounterMauId,
            "I10");

        var suggestion = Assert.Single(suggestions, s =>
            s.Version.ClinicalProtocolVersionId == MedDataStoreSeed.ProtocolThaVersionId);
        Assert.True(suggestion.Score > 0);
        Assert.Contains(suggestion.Reasons, reason => reason.Contains("icd", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Suggest_ContraindicationRule_RemovesProtocolFromCandidates()
    {
        var store = new MedDataStore();
        store.AddProtocolApplicabilityRule(new ProtocolApplicabilityRule
        {
            ClinicalProtocolVersionId = MedDataStoreSeed.ProtocolThaVersionId,
            RuleType = "contraindication",
            RuleJson = "{\"contraindication\":\"I10\"}",
            Priority = 200
        });
        var service = new ClinicalProtocolSuggestionService(store);

        var suggestions = service.Suggest(
            MedDataStoreSeed.PatientMauId,
            MedDataStoreSeed.EncounterMauId,
            "I10");

        Assert.DoesNotContain(suggestions, s =>
            s.Version.ClinicalProtocolVersionId == MedDataStoreSeed.ProtocolThaVersionId);
    }
}
