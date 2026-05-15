using TelemedicineLandingPage.Components;
using TelemedicineLandingPage.Models;
using TelemedicineLandingPage.Services;
using TelemedicineLandingPage.Services.Admin;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton<ILandingPageContentService, LandingPageContentService>();
builder.Services.AddScoped<IAdminNavigationState, AdminNavigationState>();
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
