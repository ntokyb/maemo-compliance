using FluentValidation;
using MaemoCompliance.Application.Ncrs.Dtos;

namespace MaemoCompliance.Application.Ncrs.Commands;

public class UpdateNcrCommandValidator : AbstractValidator<UpdateNcrCommand>
{
    public UpdateNcrCommandValidator()
    {
        RuleFor(x => x.NcrId).NotEmpty();
        RuleFor(x => x.Request).NotNull().SetValidator(new UpdateNcrRequestValidator());
    }
}
