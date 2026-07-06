using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Infrastructure;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class QlcmServiceCollectionTests
{
    [Fact]
    public void AddQlcmDatabase_BuildsWithScopeValidation()
    {
        const string connectionString =
            "Server=(localdb)\\mssqllocaldb;Database=qlcm_di_validation;Trusted_Connection=True;TrustServerCertificate=True";
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddQlcmDatabase(connectionString);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<MedDbContext>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDbContextFactory<MedDbContext>>());
    }
}
