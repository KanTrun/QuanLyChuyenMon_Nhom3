using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin;
using TelemedicineLandingPage.Services.Admin.Sql;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class ClinicalExportServiceTests
{
    private const string ValidPngDataUrl = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

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

    [Fact]
    public void BuildPatientDossierHtmlReport_IncludesHospitalBrandSectionsAndRevokedSignatureEvidence()
    {
        var store = new MedDataStore();
        var app = new PatientProtocolApplication
        {
            PatientRefId = MedDataStoreSeed.PatientMauId,
            EncounterRefId = MedDataStoreSeed.EncounterMauId,
            ClinicalProtocolVersionId = MedDataStoreSeed.ProtocolThaVersionId,
            DiagnosisCode = "I10",
            ApplicationStatus = "revoked",
            AppliedAt = new DateTime(2026, 6, 2, 8, 0, 0, DateTimeKind.Utc)
        };
        store.AddPatientProtocolApplication(app);
        var signedAt = new DateTime(2026, 6, 2, 8, 30, 0, DateTimeKind.Utc);
        var metadata = JsonSerializer.Serialize(new { SignatureImageDataUrl = ValidPngDataUrl });
        store.AddSignatureRecord(new SignatureRecord
        {
            TargetType = "patient_protocol_application",
            TargetId = app.PatientProtocolApplicationId,
            SignerUserId = MedDataStoreSeed.AdminUserId,
            SignerUsername = "admin",
            ProviderCode = "demo",
            IsLegallyValid = false,
            SignatureHash = MetadataBoundHash(app.PatientProtocolApplicationId, signedAt, metadata),
            SignedAt = signedAt,
            MetadataJson = metadata
        });
        var service = new ClinicalExportService(store);

        var html = service.BuildPatientDossierHtmlReport(
            MedDataStoreSeed.PatientMauId,
            new DateTime(2026, 6, 2, 9, 0, 0, DateTimeKind.Utc));

        Assert.Contains("data:image/jpeg;base64,", html);
        Assert.DoesNotContain("/brand/logo-hos.jpg", html);
        Assert.Contains("B\u1ec7nh vi\u1ec7n \u0110a khoa", html);
        Assert.Contains("H\u1ed2 S\u01a0 L\u00c2M S\u00c0NG", html);
        Assert.Contains("I. TH\u00d4NG TIN NG\u01af\u1edcI B\u1ec6NH", html);
        Assert.Contains("X\u00c1C NH\u1eacN \u0110\u00c3 THU H\u1ed2I", html);
        Assert.Contains(ValidPngDataUrl, html);
        Assert.DoesNotContain("ch\u1ee9ng th\u01b0", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Nh\u00e0 cung c\u1ea5p", html);
    }

    [Fact]
    public void BuildPatientDossierHtmlReport_IncludesSignatureEvidenceWhenSignatureExistsOnAppliedRecord()
    {
        var store = new MedDataStore();
        var app = new PatientProtocolApplication
        {
            PatientRefId = MedDataStoreSeed.PatientMauId,
            EncounterRefId = MedDataStoreSeed.EncounterMauId,
            ClinicalProtocolVersionId = MedDataStoreSeed.ProtocolThaVersionId,
            DiagnosisCode = "I10",
            ApplicationStatus = "applied",
            AppliedAt = new DateTime(2026, 6, 2, 8, 0, 0, DateTimeKind.Utc)
        };
        store.AddPatientProtocolApplication(app);
        var signedAt = new DateTime(2026, 6, 2, 8, 30, 0, DateTimeKind.Utc);
        var metadata = JsonSerializer.Serialize(new { SignatureImageDataUrl = ValidPngDataUrl });
        store.AddSignatureRecord(new SignatureRecord
        {
            TargetType = "patient_protocol_application",
            TargetId = app.PatientProtocolApplicationId,
            SignerUserId = MedDataStoreSeed.AdminUserId,
            SignerUsername = "admin",
            ProviderCode = "demo",
            IsLegallyValid = false,
            SignatureHash = MetadataBoundHash(app.PatientProtocolApplicationId, signedAt, metadata),
            SignedAt = signedAt,
            MetadataJson = metadata
        });
        var service = new ClinicalExportService(store);

        var html = service.BuildPatientDossierHtmlReport(
            MedDataStoreSeed.PatientMauId,
            new DateTime(2026, 6, 2, 9, 0, 0, DateTimeKind.Utc));

        Assert.Contains("X\u00e1c nh\u1eadn n\u1ed9i b\u1ed9", html);
        Assert.Contains(ValidPngDataUrl, html);
        Assert.Contains("signature-layout", html);
        Assert.Contains("signature-visual", html);
        Assert.Contains("Ch\u1eef k\u00fd tay n\u1ed9i b\u1ed9", html);
        Assert.Contains("signature-note", html);
        Assert.Contains("QLCM Pro", html);
        Assert.DoesNotContain(">demo<", html);
        Assert.DoesNotContain("k\u00fd \u0111i\u1ec7n t\u1eed", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ch\u1ee9ng th\u01b0", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildPatientDossierHtmlReport_RendersVisibleSignatureStampWhenImageIsMissing()
    {
        var store = new MedDataStore();
        var app = new PatientProtocolApplication
        {
            PatientRefId = MedDataStoreSeed.PatientMauId,
            EncounterRefId = MedDataStoreSeed.EncounterMauId,
            ClinicalProtocolVersionId = MedDataStoreSeed.ProtocolThaVersionId,
            DiagnosisCode = "I10",
            ApplicationStatus = "applied",
            AppliedAt = new DateTime(2026, 6, 2, 8, 0, 0, DateTimeKind.Utc)
        };
        store.AddPatientProtocolApplication(app);
        var signedAt = new DateTime(2026, 6, 2, 8, 30, 0, DateTimeKind.Utc);
        var legacyHash = LegacyHash(app.PatientProtocolApplicationId, signedAt);
        store.AddSignatureRecord(new SignatureRecord
        {
            TargetType = "patient_protocol_application",
            TargetId = app.PatientProtocolApplicationId,
            SignerUserId = MedDataStoreSeed.AdminUserId,
            SignerUsername = "admin",
            ProviderCode = "demo",
            IsLegallyValid = false,
            SignatureHash = legacyHash,
            SignedAt = signedAt
        });
        var service = new ClinicalExportService(store);

        var html = service.BuildPatientDossierHtmlReport(
            MedDataStoreSeed.PatientMauId,
            new DateTime(2026, 6, 2, 9, 0, 0, DateTimeKind.Utc));

        Assert.Contains("X\u00e1c nh\u1eadn n\u1ed9i b\u1ed9", html);
        Assert.Contains("\u0110\u00e3 x\u00e1c nh\u1eadn n\u1ed9i b\u1ed9", html);
        Assert.Contains("signature-stamp-name\">admin", html);
        Assert.DoesNotContain("k\u00fd \u0111i\u1ec7n t\u1eed", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildPatientDossierHtmlReport_RendersInternalSignatureProvider()
    {
        var store = new MedDataStore();
        var app = new PatientProtocolApplication
        {
            PatientRefId = MedDataStoreSeed.PatientMauId,
            EncounterRefId = MedDataStoreSeed.EncounterMauId,
            ClinicalProtocolVersionId = MedDataStoreSeed.ProtocolThaVersionId,
            DiagnosisCode = "I10",
            ApplicationStatus = "signed",
            AppliedAt = new DateTime(2026, 6, 4, 8, 0, 0, DateTimeKind.Utc)
        };
        store.AddPatientProtocolApplication(app);
        store.AddSignatureRecord(new SignatureRecord
        {
            TargetType = "patient_protocol_application",
            TargetId = app.PatientProtocolApplicationId,
            SignerUserId = MedDataStoreSeed.AdminUserId,
            SignerUsername = "admin",
            ProviderCode = "internal",
            IsLegallyValid = false,
            SignatureHash = "internal-hash",
            SignedAt = new DateTime(2026, 6, 4, 8, 30, 0, DateTimeKind.Utc),
            MetadataJson = "{\"Provider\":\"internal\"}"
        });
        var service = new ClinicalExportService(store);

        var html = service.BuildPatientDossierHtmlReport(
            MedDataStoreSeed.PatientMauId,
            new DateTime(2026, 6, 4, 9, 0, 0, DateTimeKind.Utc));

        Assert.Contains("K\u00fd tay tr\u1ef1c ti\u1ebfp n\u1ed9i b\u1ed9", html);
        Assert.DoesNotContain("Nh\u00e0 cung c\u1ea5p", html);
        Assert.DoesNotContain("ch\u1ee9ng th\u01b0", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("internal-hash", html);
    }
    [Fact]
    public void BuildPatientDossierHtmlReport_RendersSavedSignatureImageEvenWhenIntegrityHashDoesNotMatch()
    {
        var store = new MedDataStore();
        var app = new PatientProtocolApplication
        {
            PatientRefId = MedDataStoreSeed.PatientMauId,
            EncounterRefId = MedDataStoreSeed.EncounterMauId,
            ClinicalProtocolVersionId = MedDataStoreSeed.ProtocolThaVersionId,
            DiagnosisCode = "I10",
            ApplicationStatus = "signed",
            AppliedAt = new DateTime(2026, 6, 2, 8, 0, 0, DateTimeKind.Utc)
        };
        store.AddPatientProtocolApplication(app);
        var signedAt = new DateTime(2026, 6, 2, 8, 30, 0, DateTimeKind.Utc);
        var metadata = JsonSerializer.Serialize(new { SignatureImageDataUrl = ValidPngDataUrl });
        store.AddSignatureRecord(new SignatureRecord
        {
            TargetType = "patient_protocol_application",
            TargetId = app.PatientProtocolApplicationId,
            SignerUserId = MedDataStoreSeed.AdminUserId,
            SignerUsername = "admin",
            ProviderCode = "demo",
            IsLegallyValid = false,
            SignatureHash = "tampered-signature-hash",
            SignedAt = signedAt,
            MetadataJson = metadata
        });
        var service = new ClinicalExportService(store);

        var html = service.BuildPatientDossierHtmlReport(
            MedDataStoreSeed.PatientMauId,
            new DateTime(2026, 6, 2, 9, 0, 0, DateTimeKind.Utc));

        Assert.Contains(ValidPngDataUrl, html);
        Assert.Contains("filter:brightness(0)", html);
        Assert.Contains("signature-image-wrap", html);
        Assert.DoesNotContain("signature-warning", html);
        Assert.DoesNotContain("signature-stamp-name\">admin", html);
    }

    [Fact]
    public void BuildPatientDossierHtmlReport_EscapesPatientTextAndRejectsNonPngEvidence()
    {
        var store = new MedDataStore();
        var patient = new PatientRef
        {
            ExternalPatientId = "EXT-XSS-DOSSIER",
            PatientCode = "BN-XSS-DOSSIER",
            DisplayName = "<img src=x onerror=alert(1)>"
        };
        store.AddPatientRef(patient);
        var app = new PatientProtocolApplication
        {
            PatientRefId = patient.PatientRefId,
            ClinicalProtocolVersionId = MedDataStoreSeed.ProtocolThaVersionId,
            ApplicationStatus = "signed",
            AppliedAt = new DateTime(2026, 6, 2, 8, 0, 0, DateTimeKind.Utc)
        };
        store.AddPatientProtocolApplication(app);
        var signedAt = new DateTime(2026, 6, 2, 8, 30, 0, DateTimeKind.Utc);
        var metadata = JsonSerializer.Serialize(new { SignatureImageDataUrl = "data:image/svg+xml;base64,PHN2Zz48L3N2Zz4=" });
        store.AddSignatureRecord(new SignatureRecord
        {
            TargetType = "patient_protocol_application",
            TargetId = app.PatientProtocolApplicationId,
            SignerUserId = MedDataStoreSeed.AdminUserId,
            SignerUsername = "<script>alert(2)</script>",
            ProviderCode = "demo",
            IsLegallyValid = false,
            SignatureHash = MetadataBoundHash(app.PatientProtocolApplicationId, signedAt, metadata),
            SignedAt = signedAt,
            MetadataJson = metadata
        });
        var service = new ClinicalExportService(store);

        var html = service.BuildPatientDossierHtmlReport(
            patient.PatientRefId,
            new DateTime(2026, 6, 2, 9, 0, 0, DateTimeKind.Utc));

        Assert.Contains("&lt;img src=x onerror=alert(1)&gt;", html);
        Assert.Contains("&lt;script&gt;alert(2)&lt;/script&gt;", html);
        Assert.DoesNotContain("<img src=x onerror=alert(1)>", html);
        Assert.DoesNotContain("data:image/svg+xml", html);
    }

    private static string MetadataBoundHash(Guid targetId, DateTime signedAt, string metadataJson)
    {
        var payload = $"patient_protocol_application:{targetId}:{MedDataStoreSeed.AdminUserId}:{signedAt:O}:demo:{metadataJson}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static string LegacyHash(Guid targetId, DateTime signedAt)
    {
        var payload = $"patient_protocol_application:{targetId}:{MedDataStoreSeed.AdminUserId}:{signedAt:O}:demo";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }
}
