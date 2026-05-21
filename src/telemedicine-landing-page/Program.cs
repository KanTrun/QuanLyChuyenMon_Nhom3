using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using TelemedicineLandingPage.Components;
using TelemedicineLandingPage.Hubs;
using TelemedicineLandingPage.Infrastructure;
using TelemedicineLandingPage.Models;
using TelemedicineLandingPage.Services;
using TelemedicineLandingPage.Services.Auth;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var medDbConnectionString = QlcmServiceCollectionExtensions.BuildResilientSqlConnectionString(
    builder.Configuration.GetConnectionString("MedDb")
        ?? throw new InvalidOperationException("Connection string 'MedDb' is missing."));

builder.Host.UseQlcmSerilog();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSignalR();
builder.Services.AddSingleton<ILandingPageContentService, LandingPageContentService>();
builder.Services.AddQlcmDatabase(medDbConnectionString);
builder.Services.AddQlcmIdentityAndAuthorization();
builder.Services.AddQlcmAdminServices();
builder.Services.AddQlcmHangfire(medDbConnectionString);
builder.Services.AddQlcmChatbot(builder.Configuration);

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

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.UseQlcmHangfireDashboard();
app.UseAntiforgery();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckJsonResponseWriter.WriteAsync
});
app.MapStaticAssets();
app.MapHub<NotificationHub>("/hubs/notification");
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
