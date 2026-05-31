using FluentValidation;

namespace TelemedicineLandingPage.Application.Validation;

public sealed class ValidationService : IValidationService
{
    private readonly IServiceProvider _services;

    public ValidationService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<IReadOnlyList<string>> ValidateAsync<TModel>(
        TModel model,
        CancellationToken cancellationToken = default)
    {
        var validator = _services.GetService<IValidator<TModel>>();
        if (validator is null)
        {
            return Array.Empty<string>();
        }

        var result = await validator.ValidateAsync(model, cancellationToken);
        return result.Errors
            .Select(error => error.ErrorMessage)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .ToList();
    }
}
