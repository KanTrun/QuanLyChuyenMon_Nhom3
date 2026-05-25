using FluentValidation;
using System.Net;
using TelemedicineLandingPage.Application.Validation;
using TelemedicineLandingPage.Application.Workflow;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.SystemConsole.Themes;
using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Auth;
using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Models.Chatbot;
using TelemedicineLandingPage.Services.Admin;
using TelemedicineLandingPage.Services.Admin.Sql;
using TelemedicineLandingPage.Services.Auth;
using TelemedicineLandingPage.Services.Chatbot;
using TelemedicineLandingPage.Services.Notifications;

namespace TelemedicineLandingPage.Infrastructure;

public static class QlcmServiceCollectionExtensions
{
    public static void UseQlcmSerilog(this ConfigureHostBuilder host)
    {
        host.UseSerilog((context, _, logger) =>
        {
            var seqUrl = context.Configuration["Serilog:SeqServerUrl"];

            logger
                .ReadFrom.Configuration(context.Configuration)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .WriteTo.File(new JsonFormatter(), "logs/app-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30);

            if (!string.IsNullOrWhiteSpace(seqUrl))
            {
                logger.WriteTo.Seq(seqUrl);
            }
        });
    }

    public static string BuildResilientSqlConnectionString(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!builder.ContainsKey("Min Pool Size")) builder.MinPoolSize = 5;
        if (!builder.ContainsKey("Max Pool Size")) builder.MaxPoolSize = 100;
        if (!builder.ContainsKey("Connect Timeout") && !builder.ContainsKey("Connection Timeout")) builder.ConnectTimeout = 30;
        return builder.ConnectionString;
    }

    public static IServiceCollection AddQlcmDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<MedDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(60);
            }));

        services.AddHealthChecks()
            .AddDbContextCheck<MedDbContext>("med-db")
            .AddSqlServer(connectionString, name: "sqlserver");

        return services;
    }

    public static IServiceCollection AddQlcmIdentityAndAuthorization(this IServiceCollection services)
    {
        services.AddCascadingAuthenticationState();
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredUniqueChars = 4;
                options.User.RequireUniqueEmail = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<MedDbContext>()
            .AddPasswordValidator<PasswordStrengthValidator>()
            .AddSignInManager<NullPasswordGuardSignInManager>()
            .AddDefaultTokenProviders();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/login";
            options.AccessDeniedPath = "/AccessDenied";
            options.SlidingExpiration = true;
        });

        services.AddAuthorization(PermissionPolicyCatalog.Register);
        services.AddScoped<IClaimsTransformation, DynamicPermissionClaimsTransformation>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<AuthenticationStateProvider, CurrentUserAuthenticationStateProvider>();
        services.AddScoped<Services.Auth.IPermissionService, ClaimsPermissionService>();
        services.AddScoped<IPasswordStrengthService, PasswordStrengthService>();
        services.AddScoped<IValidationService, ValidationService>();
        services.AddValidatorsFromAssemblyContaining<RegisterAccountValidator>();
        return services;
    }

    public static IServiceCollection AddQlcmAdminServices(this IServiceCollection services)
    {
        services.AddScoped<IMedDataStore, MedDbDataStore>();
        services.AddScoped<EffectivePermissionResolver>();
        services.AddScoped<AuditTrailService>();
        services.AddScoped<IWorkflowGuard<ProcedureVersion, string>, ProcedureVersionWorkflowGuard>();
        services.AddScoped<IWorkflowGuard<TechnicalOrder, string>, TechnicalOrderWorkflowGuard>();
        services.AddScoped<PermissionChangeRequestService>();
        services.AddScoped<ProcedureLifecycleService>();
        services.AddScoped<ITechnicalOrderWorkflowService, TechnicalOrderWorkflowService>();
        services.AddScoped<ProcedureRuntimeGuard>();
        services.AddScoped<IInventoryAvailabilityService, InventoryAvailabilityService>();
        services.AddScoped<IClinicalProtocolSuggestionService, ClinicalProtocolSuggestionService>();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddScoped<AdminActionGuard>();
        services.AddScoped<BrowserSessionService>();
        services.AddScoped<NavGate>();
        services.AddSingleton<IUserPreferencesService, UserPreferencesService>();
        services.AddScoped<IReportService, SqlReportService>();
        services.AddScoped<LoadingService>();
        services.AddScoped<IAdminNavigationState, AdminNavigationState>();
        services.AddScoped<IThemeBus, ThemeBus>();
        services.AddScoped<IToastService, ToastService>();
        services.AddScoped<IConfirmDialogService, ConfirmDialogService>();
        services.AddScoped<INotificationRealtimePublisher, SignalRNotificationRealtimePublisher>();
        services.AddScoped<Services.Notifications.INotificationService, SignalRNotificationService>();
        return services;
    }

    public static IServiceCollection AddQlcmChatbot(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ChatbotOptions>(configuration.GetSection(ChatbotOptions.SectionName));
        var opts = configuration.GetSection(ChatbotOptions.SectionName).Get<ChatbotOptions>() ?? new ChatbotOptions();
        if (string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            services.AddSingleton<IChatbotClient, DemoChatbotClient>();
        }
        else if (string.Equals(opts.Provider, "Anthropic", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<AnthropicChatbotClient>(ConfigureChatbotHttpClient)
                .AddQlcmExternalResilience();
            services.AddSingleton<IChatbotClient>(sp => sp.GetRequiredService<AnthropicChatbotClient>());
        }
        else
        {
            services.AddHttpClient<GeminiChatbotClient>(ConfigureChatbotHttpClient)
                .AddQlcmExternalResilience();
            services.AddSingleton<IChatbotClient>(sp => sp.GetRequiredService<GeminiChatbotClient>());
        }

        services.AddScoped<IChatbotConversationStore, ChatbotConversationStore>();
        services.AddScoped<IChatbotService, ChatbotService>();
        return services;
    }

    private static IHttpClientBuilder AddQlcmExternalResilience(this IHttpClientBuilder builder)
        => builder
            .AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError()
                .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))))
            .AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError()
                .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests)
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

    private static void ConfigureChatbotHttpClient(IServiceProvider sp, HttpClient http)
    {
        var opts = sp.GetRequiredService<IOptions<ChatbotOptions>>().Value;
        if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
        {
            http.BaseAddress = new Uri(opts.BaseUrl);
        }
        http.Timeout = TimeSpan.FromSeconds(Math.Max(15, opts.RequestTimeoutSeconds));
    }
}
