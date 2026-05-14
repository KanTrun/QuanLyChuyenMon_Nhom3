namespace TelemedicineLandingPage.Models;

public sealed record LandingStat(string Value, string Label);

public sealed record TrustSignal(string Value, string Label, string Detail);

public sealed record SpecialistProfile(
    string Name,
    string Specialty,
    string Department,
    string Availability,
    string Languages,
    string ToneClass);

public sealed record HealthMetric(
    string Label,
    string Value,
    string Context,
    int ProgressPercent,
    string ToneClass);
