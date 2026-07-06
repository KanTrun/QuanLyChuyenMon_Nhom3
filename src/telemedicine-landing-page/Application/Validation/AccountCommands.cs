namespace TelemedicineLandingPage.Application.Validation;

public sealed record RegisterAccountCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword);

public sealed record ProfilePasswordChangeCommand(
    string Username,
    string? CurrentPasswordHash,
    bool HasExistingPassword,
    string OldPassword,
    string NewPassword,
    string ConfirmPassword);
