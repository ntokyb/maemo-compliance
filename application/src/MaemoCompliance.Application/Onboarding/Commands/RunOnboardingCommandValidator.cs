using FluentValidation;

namespace MaemoCompliance.Application.Onboarding.Commands;

/// <summary>
/// Validator for RunOnboardingCommand.
/// </summary>
public class RunOnboardingCommandValidator : AbstractValidator<RunOnboardingCommand>
{
    public RunOnboardingCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull().WithMessage("Onboarding request is required.");

        RuleFor(x => x.Request.IsoStandards)
            .NotEmpty().WithMessage("At least one ISO standard must be selected.")
            .Must(standards => standards.All(s => !string.IsNullOrWhiteSpace(s)))
            .WithMessage("ISO standards cannot contain empty values.");

        RuleFor(x => x.Request.Industry)
            .NotEmpty().WithMessage("Industry is required.")
            .MaximumLength(100).WithMessage("Industry must not exceed 100 characters.");

        RuleFor(x => x.Request.CompanySize)
            .NotEmpty().WithMessage("Company size is required.")
            .MaximumLength(50).WithMessage("Company size must not exceed 50 characters.");
    }
}

