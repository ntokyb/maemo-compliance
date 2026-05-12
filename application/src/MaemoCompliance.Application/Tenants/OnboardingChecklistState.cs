using System.Text.Json;
using MaemoCompliance.Domain.Tenants;

namespace MaemoCompliance.Application.Tenants;

public sealed class OnboardingChecklistState
{
    public bool Dismissed { get; set; }

    public static OnboardingChecklistState FromTenant(Tenant tenant)
    {
        if (string.IsNullOrWhiteSpace(tenant.OnboardingStepsCompletedJson))
        {
            return new OnboardingChecklistState();
        }

        try
        {
            return JsonSerializer.Deserialize<OnboardingChecklistState>(tenant.OnboardingStepsCompletedJson)
                   ?? new OnboardingChecklistState();
        }
        catch
        {
            return new OnboardingChecklistState();
        }
    }

    public static void ApplyToTenant(Tenant tenant, OnboardingChecklistState state)
    {
        tenant.OnboardingStepsCompletedJson = JsonSerializer.Serialize(state);
    }
}
