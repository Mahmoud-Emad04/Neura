using Neura.Core.Contracts.Announcement;

namespace Neura.Api.Validation.Announcement;

public class PostUpdateRequestValidator : AbstractValidator<PostUpdateRequest>
{
    public PostUpdateRequestValidator()
    {
        RuleFor(p => p.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(200)
            .WithMessage("Title cannot exceed 200 characters");

        RuleFor(p => p.Content)
            .NotEmpty()
            .WithMessage("Content is required")
            .MinimumLength(10)
            .WithMessage("Content must be at least 10 characters");

        RuleFor(p => p.IsPublic)
            .NotNull()
            .WithMessage("IsPublic is required");
    }
}