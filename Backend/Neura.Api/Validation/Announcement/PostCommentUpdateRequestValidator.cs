using Neura.Core.Contracts.Announcement;

namespace Neura.Api.Validation.Announcement
{
	public class PostCommentUpdateRequestValidator : AbstractValidator<PostCommentUpdateRequest>
	{
		public PostCommentUpdateRequestValidator()
		{
			RuleFor(c => c.Content)
				.NotEmpty()
				.WithMessage("Comment content is required")
				.MinimumLength(1)
				.WithMessage("Comment must contain at least one character")
				.MaximumLength(5000)
				.WithMessage("Comment cannot exceed 5000 characters");
		}
	}
}

