namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class ExternalCaRemovalTests
{
    [Fact]
    public void SourceDocsAndScripts_DoNotContainSmartCaOrVnptArtifacts()
    {
        var root = FindRepositoryRoot();
        var scannedFiles = Directory
            .EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(ShouldScan)
            .ToList();

        var matches = scannedFiles
            .SelectMany(file => BannedTerms
                .Where(term => File.ReadAllText(file).Contains(term, StringComparison.OrdinalIgnoreCase))
                .Select(term => $"{Path.GetRelativePath(root, file)} contains {term}"))
            .ToList();

        Assert.Empty(matches);
    }

    private static readonly string[] BannedTerms =
    [
        "SmartCA",
        "VNPT-CA",
        "VNPT CA",
        "VNPT SmartCA",
        "SMARTCA_"
    ];

    private static bool ShouldScan(string path)
    {
        var normalized = path.Replace(Path.DirectorySeparatorChar, '/');
        if (normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("/.git/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("/tests/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("/plans/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("/procedure-uploads/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("/procedure-source-pdfs/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("/docs/procedure-source-extraction/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return Path.GetExtension(path).ToLowerInvariant() is ".cs" or ".razor" or ".md" or ".json" or ".sql" or ".ps1" or ".yml" or ".yaml";
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "telemedicine-landing-page.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root not found.");
    }
}
