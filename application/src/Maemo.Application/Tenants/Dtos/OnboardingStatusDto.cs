namespace Maemo.Application.Tenants.Dtos;

public sealed class OnboardingStepStatusDto
{
    public int Id { get; init; }
    public string Label { get; init; } = null!;
    public bool Complete { get; init; }
    public string Link { get; init; } = null!;
}

public sealed class OnboardingStatusDto
{
    public IReadOnlyList<OnboardingStepStatusDto> Steps { get; init; } = Array.Empty<OnboardingStepStatusDto>();
    public int CompletedCount { get; init; }
    public int TotalCount { get; init; }
    public bool AllComplete { get; init; }
    public bool Dismissed { get; init; }
}
