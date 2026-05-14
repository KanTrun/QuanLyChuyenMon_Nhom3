namespace TelemedicineLandingPage.Models;

public sealed class LandingPageLinksOptions
{
    public const string SectionName = "LandingPageLinks";

    public string StartVisitUrl { get; set; } = "#consultation";

    public string FindSpecialistUrl { get; set; } = "#specialists";

    public string AppStoreUrl { get; set; } = "https://www.apple.com/app-store/";

    public string GooglePlayUrl { get; set; } = "https://play.google.com/store";

    public string PrivacyUrl { get; set; } = "#privacy";

    public string ContactUrl { get; set; } = "mailto:telehealth@benhvien.local";

    public bool HasValidUrls()
    {
        var urls = new[]
        {
            StartVisitUrl,
            FindSpecialistUrl,
            AppStoreUrl,
            GooglePlayUrl,
            PrivacyUrl,
            ContactUrl
        };

        return urls.All(IsValidDestination);
    }

    private static bool IsValidDestination(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.StartsWith('#'))
        {
            return value.Length > 1 && value.Skip(1).All(character => char.IsLetterOrDigit(character) || character is '-' or '_');
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeMailto || uri.Scheme == "tel";
    }
}
