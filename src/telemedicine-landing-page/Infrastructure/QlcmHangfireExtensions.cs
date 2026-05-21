using Hangfire;
using Hangfire.SqlServer;
using TelemedicineLandingPage.Application.Jobs;
using TelemedicineLandingPage.Infrastructure.Jobs;

namespace TelemedicineLandingPage.Infrastructure;

public static class QlcmHangfireExtensions
{
    public static IServiceCollection AddQlcmHangfire(this IServiceCollection services, string connectionString)
    {
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.FromSeconds(15),
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 5;
            options.Queues = new[] { "critical", "default", "low" };
        });

        services.AddScoped<IJobService, HangfireJobService>();
        return services;
    }

    public static WebApplication UseQlcmHangfireDashboard(this WebApplication app)
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireAdminAuthorizationFilter() },
            DashboardTitle = "QLCM Pro Jobs"
        });
        return app;
    }
}
