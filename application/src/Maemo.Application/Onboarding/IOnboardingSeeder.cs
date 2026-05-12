using Maemo.Application.Onboarding.Dtos;

namespace Maemo.Application.Onboarding;

/// <summary>
/// Service for seeding tenant-specific data during onboarding.
/// </summary>
public interface IOnboardingSeeder
{
    /// <summary>
    /// Seeds data for a tenant based on onboarding selections.
    /// </summary>
    Task SeedAsync(OnboardingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds data for an explicit tenant (e.g. immediately after self-service signup, before first login).
    /// </summary>
    Task SeedForTenantAsync(Guid tenantId, OnboardingRequest request, CancellationToken cancellationToken = default);
}

