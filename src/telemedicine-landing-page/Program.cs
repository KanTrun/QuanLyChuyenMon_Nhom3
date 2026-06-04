using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using TelemedicineLandingPage.Components;
using TelemedicineLandingPage.Hubs;
using TelemedicineLandingPage.Infrastructure;
using TelemedicineLandingPage.Services.Auth;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var medDbConnectionString = QlcmServiceCollectionExtensions.BuildResilientSqlConnectionString(
    builder.Configuration.GetConnectionString("MedDb")
        ?? throw new InvalidOperationException("Connection string 'MedDb' is missing."));

builder.Host.UseQlcmSerilog();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSignalR();
builder.Services.AddQlcmDatabase(medDbConnectionString);
builder.Services.AddQlcmIdentityAndAuthorization();
builder.Services.AddQlcmAdminServices(builder.Configuration);
builder.Services.AddQlcmHangfire(medDbConnectionString);
builder.Services.AddQlcmChatbot(builder.Configuration);

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
app.UseQlcmRecurringJobs();
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
