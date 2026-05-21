namespace TelemedicineLandingPage.Application.Validation;

public interface IPasswordStrengthService
{
    PasswordStrengthResult Evaluate(string password, string? username = null, string? currentPasswordHash = null);
}

public sealed record PasswordStrengthResult(
    int Score,
    int MaxScore,
    string Label,
    IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}
