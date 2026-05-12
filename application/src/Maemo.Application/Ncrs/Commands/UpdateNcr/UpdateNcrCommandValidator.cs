using FluentValidation;
using Maemo.Application.Ncrs.Dtos;

namespace Maemo.Application.Ncrs.Commands;

public class UpdateNcrCommandValidator : AbstractValidator<UpdateNcrCommand>
{
    public UpdateNcrCommandValidator()
    {
        RuleFor(x => x.NcrId).NotEmpty();
        RuleFor(x => x.Request).NotNull().SetValidator(new UpdateNcrRequestValidator());
    }
}
