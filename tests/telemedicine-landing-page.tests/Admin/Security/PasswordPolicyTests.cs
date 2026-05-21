using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;
using TelemedicineLandingPage.Application.Validation;
using TelemedicineLandingPage.Models.Auth;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Security;

public sealed class PasswordPolicyTests
{
    private static PasswordStrengthService CreateService()
        => new(new TestHostEnvironment());

    [Fact]
    public void PasswordStrength_RejectsWeakCommonAndUsernamePasswords()
    {
        var service = CreateService();

        Assert.Contains(service.Evaluate("password", "doctor").Errors, error => error.Contains("pho bien"));
        Assert.Contains(service.Evaluate("Doctor@2026", "doctor").Errors, error => error.Contains("ten dang nhap"));
        Assert.Contains(service.Evaluate("short", "doctor").Errors, error => error.Contains("toi thieu 8"));
    }

    [Fact]
    public void PasswordStrength_RejectsCurrentPasswordHash()
    {
        var service = CreateService();
        var hash = CurrentUserContext.HashPassword("Valid@2026");

        var result = service.Evaluate("Valid@2026", "doctor", hash);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("trung mat khau cu"));
    }

    [Fact]
    public async Task RegisterValidator_UsesSharedPasswordPolicy()
    {
        var validator = new RegisterAccountValidator(CreateService());

        var weak = await validator.ValidateAsync(new RegisterAccountCommand(
            "Nguyen",
            "An",
            "an@example.com",
            "password",
            "password"));
        var strong = await validator.ValidateAsync(new RegisterAccountCommand(
            "Nguyen",
            "An",
            "an@example.com",
            "DieuDuong@2026",
            "DieuDuong@2026"));

        Assert.False(weak.IsValid);
        Assert.True(strong.IsValid);
    }

    [Fact]
    public async Task IdentityPasswordValidator_UsesSharedPasswordPolicy()
    {
        var validator = new PasswordStrengthValidator(CreateService());

        var result = await validator.ValidateAsync(
            manager: null!,
            user: new ApplicationUser { UserName = "doctor" },
            password: "Doctor@2026");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Code == "PasswordStrength");
    }

    private sealed class TestHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = ResolveContentRoot();
        public string EnvironmentName { get; set; } = "Testing";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        private static string ResolveContentRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "src", "telemedicine-landing-page");
                if (File.Exists(Path.Combine(candidate, "Security", "common-passwords.txt")))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Cannot locate telemedicine-landing-page content root.");
        }
    }
}
