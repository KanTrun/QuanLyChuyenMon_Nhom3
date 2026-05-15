using TelemedicineLandingPage.Components;
using TelemedicineLandingPage.Models;
using TelemedicineLandingPage.Services;
using TelemedicineLandingPage.Services.Admin;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton<ILandingPageContentService, LandingPageContentService>();

// QLCM Pro admin shell — domain services seeded in-memory (no database for the demo).
builder.Services.AddSingleton<IProcedureService, ProcedureService>();
builder.Services.AddSingleton<IPermissionService, PermissionService>();
builder.Services.AddSingleton<ICatalogService, CatalogService>();
builder.Services.AddSingleton<IProtocolService, ProtocolService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddSingleton<IClinicService, ClinicService>();
builder.Services.AddSingleton<IUserPreferencesService, UserPreferencesService>();
builder.Services.AddSingleton<IReportService, ReportService>();

// Per-circuit shell state.
builder.Services.AddScoped<IAdminNavigationState, AdminNavigationState>();
builder.Services.AddScoped<IThemeBus, ThemeBus>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddScoped<IConfirmDialogService, ConfirmDialogService>();

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
