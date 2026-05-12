using FluentValidation;

namespace MaemoCompliance.Application.Documents.Commands;

public class ActivateDocumentCommandValidator : AbstractValidator<ActivateDocumentCommand>
{
    public ActivateDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentId)
            .NotEmpty().WithMessage("Document ID is required.");
    }
}

