using FluentValidation;

namespace Maemo.Application.Documents.Dtos;

public class UpdateDocumentRequestValidator : AbstractValidator<UpdateDocumentRequest>
{
    public UpdateDocumentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage("Category must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Category));

        RuleFor(x => x.Department)
            .MaximumLength(100).WithMessage("Department must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Department));

        RuleFor(x => x.OwnerUserId)
            .MaximumLength(200).WithMessage("OwnerUserId must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.OwnerUserId));

        RuleFor(x => x.ReviewDate)
            .NotEmpty().WithMessage("ReviewDate is required.");

        // BBBEE validation
        RuleFor(x => x.BbbeeLevel)
            .InclusiveBetween(1, 8).WithMessage("BBBEE Level must be between 1 and 8.")
            .When(x => x.Category == "BBBEE Certificate" && x.BbbeeLevel.HasValue);

        RuleFor(x => x.BbbeeExpiryDate)
            .NotEmpty().WithMessage("BBBEE Expiry Date is required when category is BBBEE Certificate.")
            .When(x => x.Category == "BBBEE Certificate");
    }
}

