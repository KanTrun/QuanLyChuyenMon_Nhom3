using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class ClinicalExportServiceTests
{
    [Fact]
    public void BuildWorkspaceHtmlReport_IncludesClinicalWorkspaceSections()
    {
        var store = new MedDataStore();
        store.AddPatientProtocolApplication(new PatientProtocolApplication
        {
            PatientRefId = MedDataStoreSeed.PatientMauId,
            EncounterRefId = MedDataStoreSeed.EncounterMauId,
            ClinicalProtocolVersionId = MedDataStoreSeed.ProtocolThaVersionId,
            DiagnosisCode = "I10",
            ApplicationStatus = "applied",
            AppliedAt = new DateTime(2026, 6, 2, 8, 0, 0, DateTimeKind.Utc),
            DecisionContextJson = "{\"source\":\"test\"}"
        });
        var service = new ClinicalExportService(store);

        var html = service.BuildWorkspaceHtmlReport(new DateTime(2026, 6, 2, 9, 0, 0, DateTimeKind.Utc));

        Assert.Contains("Bao cao tong hop lam sang", html);
        Assert.Contains("BN-2024-001", html);
        Assert.Contains("LK-2024-001", html);
        Assert.Contains("I10", html);
        Assert.Contains("DV-XN-CTM", html);
        Assert.Contains("Chi dinh ky thuat lien quan", html);
    }

    [Fact]
    public void BuildWorkspaceHtmlReport_EscapesPatientText()
    {
        var store = new MedDataStore();
        store.AddPatientRef(new PatientRef
        {
            ExternalPatientId = "EXT-XSS",
            PatientCode = "BN-XSS",
            DisplayName = "<script>alert(1)</script>"
        });
        var service = new ClinicalExportService(store);

        var html = service.BuildWorkspaceHtmlReport(new DateTime(2026, 6, 2, 9, 0, 0, DateTimeKind.Utc));

        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
        Assert.DoesNotContain("<script>alert(1)</script>", html);
    }
}
