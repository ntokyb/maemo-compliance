using FluentValidation;
using MaemoCompliance.Domain.Ncrs;

namespace MaemoCompliance.Application.Ncrs.Dtos;

public class UpdateNcrRequestValidator : AbstractValidator<UpdateNcrRequest>
{
    public UpdateNcrRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.Description)
            .NotEmpty();

        RuleFor(x => x.Severity)
            .IsInEnum();

        RuleFor(x => x.Category)
            .IsInEnum();

        RuleFor(x => x.DueDate)
            .Must(d => !d.HasValue || d.Value > DateTime.UtcNow)
            .WithMessage("Due date must be in the future.")
            .When(x => x.DueDate.HasValue);

        RuleFor(x => x.Department)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Department));

        RuleFor(x => x.OwnerUserId)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.OwnerUserId));

        RuleFor(x => x.EscalationLevel)
            .InclusiveBetween(0, 3);
    }
}
