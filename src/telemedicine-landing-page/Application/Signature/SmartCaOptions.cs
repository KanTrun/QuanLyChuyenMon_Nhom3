using System.Text.Json;
using System.Text.Json.Serialization;

namespace TelemedicineLandingPage.Application.Signature;

public sealed class SmartCaOptions
{
    public const string SectionName = "SmartCa";
    public const string SandboxProviderCode = "vnpt-smartca-sandbox";

    public bool Enabled { get; init; }
    public string CredentialMode { get; init; } = "Auto";
    public string BaseUrl { get; init; } = "https://rmgateway.vnptit.vn";
    public string ApiPrefix { get; init; } = "/sca/sp769";
    public string SpId { get; init; } = string.Empty;
    public string SpPassword { get; init; } = string.Empty;
    public string? MobileCode { get; init; }
    public string? OAuthClientId { get; init; }
    public string? OAuthClientSecret { get; init; }
    public string? OAuthRedirectUri { get; init; }
    public string? OAuthRefreshToken { get; init; }
    public string? OAuthUsername { get; init; }
    public string? OAuthPassword { get; init; }
    public string? OAuthCredentialId { get; init; }
    public string DefaultUserId { get; init; } = string.Empty;
    public string? DefaultSerialNumber { get; init; }
    public string? DefaultSignerUserId { get; init; }
    public string? DefaultSignerUsername { get; init; }
    public string? UserBindingsJson { get; init; }
    public string? CallbackUrl { get; init; }
    public string? CallbackSecret { get; init; }
    public int RequestTimeoutSeconds { get; init; } = 45;

    public bool IsOAuthMode =>
        string.Equals(CredentialMode, "OAuth", StringComparison.OrdinalIgnoreCase) ||
        (string.Equals(CredentialMode, "Auto", StringComparison.OrdinalIgnoreCase) &&
         (EndsWith(SpId, ".apps.smartcaapi.com") || EndsWith(OAuthClientId, ".apps.smartcaapi.com")));

    public string EffectiveCredentialMode => IsOAuthMode ? "OAuth" : "DirectSP";

    public bool IsReady =>
        Enabled &&
        !string.IsNullOrWhiteSpace(BaseUrl) &&
        HasSignerBinding() &&
        (IsOAuthMode ? HasOAuthCredential() && HasOAuthTokenSource() : HasDirectSpCredential());

    public IReadOnlyList<string> MissingFields()
    {
        var missing = new List<string>();
        if (!Enabled) missing.Add("SMARTCA_ENABLED");
        if (string.IsNullOrWhiteSpace(BaseUrl)) missing.Add("SMARTCA_BASE_URL");

        if (IsOAuthMode)
        {
            if (!HasOAuthCredential()) missing.Add("SMARTCA_OAUTH_CLIENT_ID/SECRET or SMARTCA_SP_ID/PASSWORD");
            if (!HasOAuthTokenSource()) missing.Add("SMARTCA_OAUTH_REFRESH_TOKEN or SMARTCA_OAUTH_USERNAME/PASSWORD");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(ApiPrefix)) missing.Add("SMARTCA_API_PREFIX");
            if (string.IsNullOrWhiteSpace(SpId)) missing.Add("SMARTCA_SP_ID");
            if (string.IsNullOrWhiteSpace(SpPassword)) missing.Add("SMARTCA_SP_PASSWORD");
        }

        if (!HasSignerBinding())
        {
            missing.Add(string.IsNullOrWhiteSpace(UserBindingsJson)
                ? "SMARTCA_DEFAULT_USER_ID + SMARTCA_SIGNER_USER_ID/SMARTCA_SIGNER_USERNAME"
                : "SMARTCA_USER_BINDINGS_JSON valid binding");
        }

        return missing;
    }

    public SmartCaSignerBinding? ResolveSigner(Guid appUserId, string appUsername)
    {
        var configuredBinding = ParseUserBindings()
            .FirstOrDefault(binding => binding.Matches(appUserId, appUsername));
        if (configuredBinding is not null)
            return configuredBinding.ToSignerBinding(appUserId, appUsername);

        if (string.IsNullOrWhiteSpace(DefaultUserId) || !DefaultBindingMatches(appUserId, appUsername))
            return null;

        return new SmartCaSignerBinding(
            appUserId,
            appUsername,
            DefaultUserId.Trim(),
            string.IsNullOrWhiteSpace(DefaultSerialNumber) ? null : DefaultSerialNumber.Trim());
    }

    public string ResolvedOAuthClientId()
        => !string.IsNullOrWhiteSpace(OAuthClientId) ? OAuthClientId.Trim() : SpId.Trim();

    public string ResolvedOAuthClientSecret()
        => !string.IsNullOrWhiteSpace(OAuthClientSecret) ? OAuthClientSecret.Trim() : SpPassword;

    public string TokenEndpoint()
        => $"{BaseUrl.TrimEnd('/')}/auth/token";

    public string AuthorizeEndpoint()
        => $"{BaseUrl.TrimEnd('/')}/auth/authorize";

    private bool HasSignerBinding()
        => (!string.IsNullOrWhiteSpace(DefaultUserId) &&
            (!string.IsNullOrWhiteSpace(DefaultSignerUserId) || !string.IsNullOrWhiteSpace(DefaultSignerUsername))) ||
           ParseUserBindings().Any(binding => !string.IsNullOrWhiteSpace(binding.SubscriberId));

    private bool HasDirectSpCredential()
        => !string.IsNullOrWhiteSpace(ApiPrefix) &&
           !string.IsNullOrWhiteSpace(SpId) &&
           !string.IsNullOrWhiteSpace(SpPassword);

    private bool HasOAuthCredential()
        => !string.IsNullOrWhiteSpace(ResolvedOAuthClientId()) &&
           !string.IsNullOrWhiteSpace(ResolvedOAuthClientSecret());

    private bool HasOAuthTokenSource()
        => !string.IsNullOrWhiteSpace(OAuthRefreshToken) ||
           (!string.IsNullOrWhiteSpace(OAuthUsername) && !string.IsNullOrWhiteSpace(OAuthPassword));

    private bool DefaultBindingMatches(Guid appUserId, string appUsername)
        => (!string.IsNullOrWhiteSpace(DefaultSignerUserId) &&
            Guid.TryParse(DefaultSignerUserId, out var configuredUserId) &&
            configuredUserId == appUserId) ||
           (!string.IsNullOrWhiteSpace(DefaultSignerUsername) &&
            string.Equals(DefaultSignerUsername.Trim(), appUsername, StringComparison.OrdinalIgnoreCase));

    private IReadOnlyList<SmartCaUserBinding> ParseUserBindings()
    {
        if (string.IsNullOrWhiteSpace(UserBindingsJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<SmartCaUserBinding>>(
                UserBindingsJson,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static bool EndsWith(string? value, string suffix)
        => !string.IsNullOrWhiteSpace(value) &&
           value.Trim().EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
}

public sealed record SmartCaReadiness(
    bool Enabled,
    bool Ready,
    string BaseUrl,
    string ApiPrefix,
    string? DefaultUserId,
    IReadOnlyList<string> MissingFields,
    string CredentialMode)
{
    public string DisplayStatus => Ready
        ? $"SmartCA sandbox {CredentialMode} ready"
        : Enabled
            ? $"SmartCA sandbox {CredentialMode} missing config"
            : "SmartCA sandbox disabled";
}

public sealed record SmartCaSignerBinding(
    Guid AppUserId,
    string AppUsername,
    string SubscriberId,
    string? SerialNumber);

public sealed record SmartCaUserBinding(
    [property: JsonPropertyName("appUserId")] Guid? AppUserId,
    [property: JsonPropertyName("appUsername")] string? AppUsername,
    [property: JsonPropertyName("subscriberId")] string SubscriberId,
    [property: JsonPropertyName("serialNumber")] string? SerialNumber)
{
    public bool Matches(Guid appUserId, string appUsername)
        => (AppUserId.HasValue && AppUserId.Value == appUserId) ||
           (!string.IsNullOrWhiteSpace(AppUsername) &&
            string.Equals(AppUsername.Trim(), appUsername, StringComparison.OrdinalIgnoreCase));

    public SmartCaSignerBinding ToSignerBinding(Guid appUserId, string appUsername)
        => new(
            appUserId,
            appUsername,
            SubscriberId.Trim(),
            string.IsNullOrWhiteSpace(SerialNumber) ? null : SerialNumber.Trim());
}
