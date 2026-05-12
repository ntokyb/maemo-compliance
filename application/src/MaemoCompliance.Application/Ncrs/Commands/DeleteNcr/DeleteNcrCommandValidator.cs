using FluentValidation;

namespace MaemoCompliance.Application.Ncrs.Commands;

public class DeleteNcrCommandValidator : AbstractValidator<DeleteNcrCommand>
{
    public DeleteNcrCommandValidator()
    {
        RuleFor(x => x.NcrId).NotEmpty();
    }
}
