using FluentValidation;

namespace Maemo.Application.Ncrs.Commands;

public class DeleteNcrCommandValidator : AbstractValidator<DeleteNcrCommand>
{
    public DeleteNcrCommandValidator()
    {
        RuleFor(x => x.NcrId).NotEmpty();
    }
}
