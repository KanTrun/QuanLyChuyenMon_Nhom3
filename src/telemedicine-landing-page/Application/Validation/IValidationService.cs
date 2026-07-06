namespace TelemedicineLandingPage.Application.Validation;

public interface IValidationService
{
    Task<IReadOnlyList<string>> ValidateAsync<TModel>(TModel model, CancellationToken cancellationToken = default);
}
