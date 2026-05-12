using FluentValidation;

namespace MaemoCompliance.Application.Documents.Commands;

public class SubmitDocumentForApprovalCommandValidator : AbstractValidator<SubmitDocumentForApprovalCommand>
{
    public SubmitDocumentForApprovalCommandValidator()
    {
        RuleFor(x => x.DocumentId)
            .NotEmpty().WithMessage("Document ID is required.");
    }
}

