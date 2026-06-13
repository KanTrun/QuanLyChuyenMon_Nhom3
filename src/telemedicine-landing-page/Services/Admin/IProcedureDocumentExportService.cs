namespace TelemedicineLandingPage.Services.Admin;

public interface IProcedureDocumentExportService
{
    string BuildProcedureDocumentHtml(Guid procedureVersionId, DateTime generatedAt);
}
