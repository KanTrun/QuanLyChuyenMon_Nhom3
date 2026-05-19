using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TelemedicineLandingPage.Components;
using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models;
using TelemedicineLandingPage.Models.Chatbot;
using TelemedicineLandingPage.Services;
using TelemedicineLandingPage.Services.Admin;
using TelemedicineLandingPage.Services.Admin.Sql;
using TelemedicineLandingPage.Services.Chatbot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton<ILandingPageContentService, LandingPageContentService>();

// QLCM Pro admin shell — kết nối SQL Server thật.
builder.Services.AddDbContext<MedDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MedDb")));
builder.Services.AddScoped<IMedDataStore, MedDbDataStore>();
builder.Services.AddScoped<EffectivePermissionResolver>();
builder.Services.AddScoped<AuditTrailService>();
builder.Services.AddScoped<PermissionChangeRequestService>();
builder.Services.AddScoped<ProcedureLifecycleService>();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddScoped<NavGate>();
builder.Services.AddSingleton<IProcedureService, ProcedureService>();
builder.Services.AddSingleton<IPermissionService, PermissionService>();
builder.Services.AddSingleton<ICatalogService, CatalogService>();
builder.Services.AddSingleton<IProtocolService, ProtocolService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddSingleton<IClinicService, ClinicService>();
builder.Services.AddSingleton<IUserPreferencesService, UserPreferencesService>();
builder.Services.AddScoped<IReportService, SqlReportService>();

// Per-circuit shell state.
builder.Services.AddScoped<LoadingService>();
builder.Services.AddScoped<IAdminNavigationState, AdminNavigationState>();
builder.Services.AddScoped<IThemeBus, ThemeBus>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddScoped<IConfirmDialogService, ConfirmDialogService>();

// Chatbot configuration + client. The client choice is driven by the configured
// provider plus the presence of an API key: an empty key falls back to a friendly
// demo stream so the UI exercises the same code path without external calls.
builder.Services.Configure<ChatbotOptions>(builder.Configuration.GetSection(ChatbotOptions.SectionName));
var chatbotOpts = builder.Configuration.GetSection(ChatbotOptions.SectionName).Get<ChatbotOptions>() ?? new ChatbotOptions();
if (string.IsNullOrWhiteSpace(chatbotOpts.ApiKey))
{
    builder.Services.AddSingleton<IChatbotClient, DemoChatbotClient>();
}
else if (string.Equals(chatbotOpts.Provider, "Anthropic", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<AnthropicChatbotClient>((sp, http) =>
    {
        var opts = sp.GetRequiredService<IOptions<ChatbotOptions>>().Value;
        if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
        {
            http.BaseAddress = new Uri(opts.BaseUrl);
        }
        http.Timeout = TimeSpan.FromSeconds(Math.Max(15, opts.RequestTimeoutSeconds));
    });
    builder.Services.AddSingleton<IChatbotClient>(sp => sp.GetRequiredService<AnthropicChatbotClient>());
}
else
{
    // Default provider is Google Gemini (free-tier friendly, picks up unknown providers too).
    builder.Services.AddHttpClient<GeminiChatbotClient>((sp, http) =>
    {
        var opts = sp.GetRequiredService<IOptions<ChatbotOptions>>().Value;
        if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
        {
            http.BaseAddress = new Uri(opts.BaseUrl);
        }
        http.Timeout = TimeSpan.FromSeconds(Math.Max(15, opts.RequestTimeoutSeconds));
    });
    builder.Services.AddSingleton<IChatbotClient>(sp => sp.GetRequiredService<GeminiChatbotClient>());
}
builder.Services.AddScoped<IChatbotConversationStore, ChatbotConversationStore>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();

builder.Services.AddOptions<LandingPageLinksOptions>()
    .Bind(builder.Configuration.GetSection(LandingPageLinksOptions.SectionName))
    .Validate(options => options.HasValidUrls(), "Landing page CTA URLs must be absolute URLs or in-page anchors.")
    .ValidateOnStart();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
