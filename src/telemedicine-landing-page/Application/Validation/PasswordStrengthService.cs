using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Application.Validation;

public sealed class PasswordStrengthService : IPasswordStrengthService
{
    private const int RequiredLength = 8;
    private const int RequiredUniqueChars = 4;
    private readonly Lazy<HashSet<string>> _commonPasswords;

    public PasswordStrengthService(IHostEnvironment environment)
    {
        _commonPasswords = new Lazy<HashSet<string>>(() => LoadCommonPasswords(environment));
    }

    public PasswordStrengthResult Evaluate(string password, string? username = null, string? currentPasswordHash = null)
    {
        var errors = new List<string>();
        var value = password ?? string.Empty;
        if (value.Length < RequiredLength) errors.Add("Mat khau phai co toi thieu 8 ky tu.");
        if (!value.Any(char.IsUpper)) errors.Add("Mat khau phai co chu hoa.");
        if (!value.Any(char.IsLower)) errors.Add("Mat khau phai co chu thuong.");
        if (!value.Any(char.IsDigit)) errors.Add("Mat khau phai co chu so.");
        if (!value.Any(ch => !char.IsLetterOrDigit(ch))) errors.Add("Mat khau phai co ky tu dac biet.");
        if (value.Distinct().Count() < RequiredUniqueChars) errors.Add("Mat khau phai co it nhat 4 ky tu khac nhau.");

        var normalized = value.Trim().ToLowerInvariant();
        if (_commonPasswords.Value.Contains(normalized)) errors.Add("Mat khau nam trong danh sach pho bien.");

        var usernameKey = NormalizeUsername(username);
        if (!string.IsNullOrWhiteSpace(usernameKey) && normalized.Contains(usernameKey, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Mat khau khong duoc chua ten dang nhap.");
        }

        if (!string.IsNullOrWhiteSpace(currentPasswordHash) &&
            CurrentUserContext.HashPassword(value) == currentPasswordHash)
        {
            errors.Add("Mat khau moi khong duoc trung mat khau cu.");
        }

        var score = Math.Clamp(6 - errors.Count, 0, 5);
        return new PasswordStrengthResult(score, 5, LabelFor(score), errors);
    }

    private static HashSet<string> LoadCommonPasswords(IHostEnvironment environment)
    {
        var path = Path.Combine(environment.ContentRootPath, "Security", "common-passwords.txt");
        if (!File.Exists(path))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return File.ReadLines(path)
            .Select(line => line.Trim().ToLowerInvariant())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string? NormalizeUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;
        var trimmed = username.Trim().ToLowerInvariant();
        var at = trimmed.IndexOf('@');
        return at > 0 ? trimmed[..at] : trimmed;
    }

    private static string LabelFor(int score)
        => score switch
        {
            >= 5 => "Manh",
            4 => "Kha",
            3 => "Trung binh",
            2 => "Yeu",
            _ => "Rat yeu"
        };
}
